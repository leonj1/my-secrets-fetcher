using Amazon.SecretsManager;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
                var config = host.Services.GetRequiredService<IOptions<SecretsManagerConfig>>().Value;
                logger.LogInformation("Starting Secrets Manager application");

                var allSecrets = new Dictionary<string, string>();

                // Process DevContainer secrets (if exists)
                var devContainerService = host.Services.GetRequiredService<IDevContainerService>();
                var devContainerConfig = await devContainerService.LoadDevContainerConfigAsync();
                if (devContainerConfig != null)
                {
                    logger.LogInformation("Processing DevContainer secrets...");
                    await devContainerService.ProcessDevContainerSecretsAsync(devContainerConfig);
                }

                // Process .env.example secrets (if exists) - INDEPENDENT OF DEVCONTAINER
                var envFileService = host.Services.GetRequiredService<IEnvFileService>();
                var envExampleVars = await envFileService.LoadEnvExampleFileAsync(config.EnvExamplePath);
                if (envExampleVars != null && envExampleVars.Any())
                {
                    logger.LogInformation("Processing .env.example secrets...");
                    var processedSecrets = await envFileService.ProcessEnvFileSecretsAsync(envExampleVars);
                    
                    // Merge with collected secrets
                    foreach (var secret in processedSecrets)
                    {
                        allSecrets[secret.Key] = secret.Value;
                        
                        // Also set as environment variables if configured
                        if (config.OutputMode == OutputMode.EnvironmentVariables || 
                            config.OutputMode == OutputMode.Both)
                        {
                            Environment.SetEnvironmentVariable(secret.Key, secret.Value);
                            logger.LogInformation("Set environment variable {Key} from .env.example", secret.Key);
                        }
                    }
                }

                // Write .env file if needed (based on OutputMode)
                if ((config.OutputMode == OutputMode.EnvFile || config.OutputMode == OutputMode.Both) && 
                    allSecrets.Any())
                {
                    await envFileService.WriteEnvFileAsync(allSecrets, config.EnvFilePath);
                }

                // Continue with existing app secrets processing
                var secretsService = host.Services.GetRequiredService<ISecretsService>();
                var secrets = await secretsService.GetSecretsAsync();

                logger.LogInformation("Successfully retrieved secrets from AWS Secrets Manager");
                
                // Display the retrieved secrets (in a real application, you would use these secrets)
                Console.WriteLine("Retrieved Application Secrets:");
                Console.WriteLine($"Database URL: {secrets.DatabaseUrl}");
                Console.WriteLine($"API Key: {MaskSecret(secrets.ApiKey)}");
                Console.WriteLine($"JWT Secret: {MaskSecret(secrets.JwtSecret)}");
                Console.WriteLine($"Redis URL: {secrets.RedisUrl}");

                // Display any environment variables that were set
                Console.WriteLine("\nEnvironment Variables Set:");
                var envVars = Environment.GetEnvironmentVariables();
                foreach (var key in envVars.Keys)
                {
                    var keyStr = key.ToString();
                    if (keyStr != null && (keyStr.Contains("TOKEN") || keyStr.Contains("SECRET") || keyStr.Contains("KEY") || allSecrets.ContainsKey(keyStr)))
                    {
                        var value = envVars[key]?.ToString() ?? "";
                        Console.WriteLine($"{keyStr}: {MaskSecret(value)}");
                    }
                }

                // Display .env file status
                if (config.OutputMode == OutputMode.EnvFile || config.OutputMode == OutputMode.Both)
                {
                    Console.WriteLine($"\n.env file written to: {config.EnvFilePath}");
                }

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
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices((context, services) =>
                {
                    var configuration = context.Configuration;

                    // Configure AWS settings
                    var awsConfig = configuration.GetSection("AWS").Get<AwsConfig>();
                    if (awsConfig == null)
                    {
                        awsConfig = new AwsConfig();
                    }

                    // Support standard AWS environment variables as fallback
                    if (string.IsNullOrEmpty(awsConfig.AccessKey))
                    {
                        awsConfig.AccessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
                    }
                    if (string.IsNullOrEmpty(awsConfig.SecretKey))
                    {
                        awsConfig.SecretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
                    }
                    if (string.IsNullOrEmpty(awsConfig.Region))
                    {
                        awsConfig.Region = Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION") 
                                        ?? Environment.GetEnvironmentVariable("AWS_REGION")
                                        ?? "us-east-1";
                    }

                    // Validate the configuration
                    awsConfig.Validate();

                    // Configure Secrets Manager settings
                    services.Configure<SecretsManagerConfig>(
                        configuration.GetSection("SecretsManager"));

                    // Register AWS Secrets Manager client
                    services.AddSingleton<IAmazonSecretsManager>(provider =>
                    {
                        var logger = provider.GetRequiredService<ILogger<Program>>();
                        var config = new AmazonSecretsManagerConfig
                        {
                            RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(awsConfig.Region),
                            ServiceURL = awsConfig.ServiceURL
                        };

                        // If explicit credentials are provided, use them
                        if (awsConfig.HasExplicitCredentials())
                        {
                            logger.LogInformation("Using explicit AWS credentials");
                            return new AmazonSecretsManagerClient(
                                awsConfig.AccessKey,
                                awsConfig.SecretKey,
                                config);
                        }
                        else
                        {
                            // Use AWS SDK credential chain (IAM roles, etc.)
                            logger.LogInformation("Using AWS SDK credential chain (IAM roles, environment variables, etc.)");
                            return new AmazonSecretsManagerClient(config);
                        }
                    });

                    // Register application services
                    services.AddScoped<ISecretsService, AwsSecretsService>();
                    services.AddScoped<IDevContainerService, DevContainerService>();
                    services.AddScoped<IEnvFileService, EnvFileService>();
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
