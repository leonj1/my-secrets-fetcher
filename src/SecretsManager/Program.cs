using Amazon.SecretsManager;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SecretsManager.Models;
using SecretsManager.Services;

namespace SecretsManager
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            
            try
            {
                var logger = host.Services.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Starting Secrets Manager application");

                var secretsService = host.Services.GetRequiredService<ISecretsService>();
                var secrets = await secretsService.GetSecretsAsync();

                logger.LogInformation("Successfully retrieved secrets from AWS Secrets Manager");
                
                // Display the retrieved secrets (in a real application, you would use these secrets)
                Console.WriteLine("Retrieved Application Secrets:");
                Console.WriteLine($"Database URL: {secrets.DatabaseUrl}");
                Console.WriteLine($"API Key: {MaskSecret(secrets.ApiKey)}");
                Console.WriteLine($"JWT Secret: {MaskSecret(secrets.JwtSecret)}");
                Console.WriteLine($"Redis URL: {secrets.RedisUrl}");

                logger.LogInformation("Application completed successfully");
            }
            catch (Exception ex)
            {
                var logger = host.Services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Application failed with error");
                Console.WriteLine($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    var configuration = context.Configuration;

                    // Configure AWS settings
                    var awsConfig = configuration.GetSection("AWS").Get<AwsConfig>();
                    if (awsConfig == null)
                    {
                        throw new InvalidOperationException("AWS configuration is missing");
                    }

                    // Configure Secrets Manager settings
                    services.Configure<SecretsManagerConfig>(
                        configuration.GetSection("SecretsManager"));

                    // Register AWS Secrets Manager client
                    services.AddSingleton<IAmazonSecretsManager>(provider =>
                    {
                        var config = new AmazonSecretsManagerConfig
                        {
                            RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(awsConfig.Region),
                            ServiceURL = awsConfig.ServiceURL
                        };

                        return new AmazonSecretsManagerClient(
                            awsConfig.AccessKey,
                            awsConfig.SecretKey,
                            config);
                    });

                    // Register application services
                    services.AddScoped<ISecretsService, AwsSecretsService>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                });

        private static string MaskSecret(string secret)
        {
            if (string.IsNullOrEmpty(secret) || secret.Length <= 4)
                return "****";
            
            return secret.Substring(0, 2) + new string('*', secret.Length - 4) + secret.Substring(secret.Length - 2);
        }
    }
}
