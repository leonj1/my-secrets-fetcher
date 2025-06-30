using Amazon.SecretsManager;
using CommandLine;
using CommandLine.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecretsManager.Models;
using SecretsManager.Services;
using System.Reflection;

namespace SecretsManager
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Parse command line arguments
            var parser = new Parser(with => with.HelpWriter = null);
            var parserResult = parser.ParseArguments<CommandLineOptions>(args);

            await parserResult.WithParsedAsync(async options =>
            {
                // Handle help
                if (options.Help)
                {
                    DisplayHelp(parserResult);
                    return;
                }

                // Handle version
                if (options.Version)
                {
                    DisplayVersion();
                    return;
                }

                // Build and run the application with parsed options
                var host = CreateHostBuilder(args, options).Build();
            
            try
            {
                var logger = host.Services.GetRequiredService<ILogger<Program>>();
                var config = host.Services.GetRequiredService<IOptions<SecretsManagerConfig>>().Value;
                var cmdOptions = host.Services.GetRequiredService<CommandLineOptions>();
                
                if (!cmdOptions.Quiet)
                    logger.LogInformation("Starting Secrets Manager application");

                var allSecrets = new Dictionary<string, string>();

                // Process DevContainer secrets (if exists)
                var devContainerService = host.Services.GetRequiredService<IDevContainerService>();
                var devContainerConfig = await devContainerService.LoadDevContainerConfigAsync();
                if (devContainerConfig != null)
                {
                    if (!cmdOptions.Quiet)
                        logger.LogInformation("Processing DevContainer secrets...");
                    
                    if (!cmdOptions.DryRun)
                        await devContainerService.ProcessDevContainerSecretsAsync(devContainerConfig);
                    else if (!cmdOptions.Quiet)
                        Console.WriteLine("[DRY RUN] Would process DevContainer secrets");
                }

                // Process .env.example secrets (if exists) - INDEPENDENT OF DEVCONTAINER
                var envFileService = host.Services.GetRequiredService<IEnvFileService>();
                var envExampleVars = await envFileService.LoadEnvExampleFileAsync(config.EnvExamplePath);
                if (envExampleVars != null && envExampleVars.Any())
                {
                    if (!cmdOptions.Quiet)
                        logger.LogInformation("Processing .env.example secrets...");
                    
                    Dictionary<string, string> processedSecrets;
                    if (!cmdOptions.DryRun)
                    {
                        processedSecrets = await envFileService.ProcessEnvFileSecretsAsync(envExampleVars);
                    }
                    else
                    {
                        processedSecrets = new Dictionary<string, string>();
                        if (!cmdOptions.Quiet)
                        {
                            Console.WriteLine("[DRY RUN] Would process the following .env.example secrets:");
                            foreach (var arn in envExampleVars.Where(kv => kv.Value.StartsWith("${arn:")))
                            {
                                Console.WriteLine($"  {arn.Key}: {arn.Value}");
                            }
                        }
                    }
                    
                    // Merge with collected secrets
                    foreach (var secret in processedSecrets)
                    {
                        allSecrets[secret.Key] = secret.Value;
                        
                        // Also set as environment variables if configured
                        if (config.OutputMode == OutputMode.EnvironmentVariables || 
                            config.OutputMode == OutputMode.Both)
                        {
                            if (!cmdOptions.DryRun)
                            {
                                Environment.SetEnvironmentVariable(secret.Key, secret.Value);
                                if (!cmdOptions.Quiet)
                                    logger.LogInformation("Set environment variable {Key} from .env.example", secret.Key);
                            }
                            else if (!cmdOptions.Quiet)
                            {
                                Console.WriteLine($"[DRY RUN] Would set environment variable: {secret.Key}");
                            }
                        }
                    }
                }

                // Write .env file if needed (based on OutputMode)
                if ((config.OutputMode == OutputMode.EnvFile || config.OutputMode == OutputMode.Both) && 
                    allSecrets.Any())
                {
                    if (!cmdOptions.DryRun)
                    {
                        await envFileService.WriteEnvFileAsync(allSecrets, config.EnvFilePath);
                    }
                    else if (!cmdOptions.Quiet)
                    {
                        Console.WriteLine($"[DRY RUN] Would write .env file to: {config.EnvFilePath}");
                        Console.WriteLine("[DRY RUN] Would write the following variables:");
                        foreach (var secret in allSecrets)
                        {
                            Console.WriteLine($"  {secret.Key}={MaskSecret(secret.Value)}");
                        }
                    }
                }

                // Continue with existing app secrets processing
                var secretsService = host.Services.GetRequiredService<ISecretsService>();
                AppSecrets? secrets = null;
                
                if (!cmdOptions.DryRun)
                {
                    secrets = await secretsService.GetSecretsAsync();
                    if (!cmdOptions.Quiet)
                        logger.LogInformation("Successfully retrieved secrets from AWS Secrets Manager");
                }
                else if (!cmdOptions.Quiet)
                {
                    Console.WriteLine($"[DRY RUN] Would fetch secret: {config.SecretName} from region: {host.Services.GetRequiredService<IOptions<AwsConfig>>().Value.Region}");
                }
                
                // Display the retrieved secrets (in a real application, you would use these secrets)
                if (!cmdOptions.Quiet && secrets != null)
                {
                    Console.WriteLine("Retrieved Application Secrets:");
                    Console.WriteLine($"Database URL: {secrets.DatabaseUrl}");
                    Console.WriteLine($"API Key: {MaskSecret(secrets.ApiKey)}");
                    Console.WriteLine($"JWT Secret: {MaskSecret(secrets.JwtSecret)}");
                    Console.WriteLine($"Redis URL: {secrets.RedisUrl}");
                }

                // Display any environment variables that were set
                if (!cmdOptions.Quiet && !cmdOptions.DryRun)
                {
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
                }

                // Display .env file status
                if (!cmdOptions.Quiet && !cmdOptions.DryRun && (config.OutputMode == OutputMode.EnvFile || config.OutputMode == OutputMode.Both))
                {
                    Console.WriteLine($"\n.env file written to: {config.EnvFilePath}");
                }

                if (!cmdOptions.Quiet)
                    logger.LogInformation("Application completed successfully");
            }
            catch (Exception ex)
            {
                var logger = host.Services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Application failed with error");
                Console.WriteLine($"Error: {ex.Message}");
                Environment.Exit(1);
            }
            });

            // Handle parsing errors
            await parserResult.WithNotParsedAsync(async errors =>
            {
                if (errors.IsHelp() || errors.IsVersion())
                {
                    DisplayHelp(parserResult);
                }
                else
                {
                    Console.WriteLine("Error parsing command line arguments.");
                    DisplayHelp(parserResult);
                    Environment.Exit(1);
                }
            });
        }

        static IHostBuilder CreateHostBuilder(string[] args, CommandLineOptions options) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                    
                    // Add command line options as configuration
                    var cmdLineConfig = new Dictionary<string, string?>();
                    if (!string.IsNullOrEmpty(options.SecretName))
                        cmdLineConfig["SecretsManager:SecretName"] = options.SecretName;
                    if (!string.IsNullOrEmpty(options.Region))
                        cmdLineConfig["AWS:Region"] = options.Region;
                    if (!string.IsNullOrEmpty(options.AccessKey))
                        cmdLineConfig["AWS:AccessKey"] = options.AccessKey;
                    if (!string.IsNullOrEmpty(options.SecretKey))
                        cmdLineConfig["AWS:SecretKey"] = options.SecretKey;
                    if (!string.IsNullOrEmpty(options.OutputMode))
                        cmdLineConfig["SecretsManager:OutputMode"] = options.OutputMode;
                    if (!string.IsNullOrEmpty(options.EnvFilePath))
                        cmdLineConfig["SecretsManager:EnvFilePath"] = options.EnvFilePath;
                    if (!string.IsNullOrEmpty(options.EnvExamplePath))
                        cmdLineConfig["SecretsManager:EnvExamplePath"] = options.EnvExamplePath;
                    if (!string.IsNullOrEmpty(options.DevContainerPath))
                        cmdLineConfig["SecretsManager:DevContainerPath"] = options.DevContainerPath;
                        
                    if (cmdLineConfig.Any())
                        config.AddInMemoryCollection(cmdLineConfig!);
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
                    
                    // Register command line options
                    services.AddSingleton(options);
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

        private static void DisplayHelp(ParserResult<CommandLineOptions> result)
        {
            Console.WriteLine("Universal Secrets Manager");
            Console.WriteLine("A .NET application that fetches secrets from AWS Secrets Manager\n");
            
            var helpText = CommandLine.Text.HelpText.AutoBuild(result, h =>
            {
                h.AdditionalNewLineAfterOption = false;
                h.MaximumDisplayWidth = 120;
                h.AutoHelp = false;
                h.AutoVersion = false;
                return CommandLine.Text.HelpText.DefaultParsingErrorsHandler(result, h);
            }, e => e);
            
            Console.WriteLine(helpText);
            
            Console.WriteLine("\nEnvironment Variables:");
            Console.WriteLine("  AWS Credentials:");
            Console.WriteLine("    AWS_ACCESS_KEY_ID               - AWS access key ID");
            Console.WriteLine("    AWS_SECRET_ACCESS_KEY           - AWS secret access key");
            Console.WriteLine("    AWS_DEFAULT_REGION              - Default AWS region (e.g., us-east-1)");
            Console.WriteLine("    AWS_REGION                      - Alternative region variable\n");
            
            Console.WriteLine("  .NET Configuration Style:");
            Console.WriteLine("    AWS__AccessKey                  - AWS access key ID (.NET config format)");
            Console.WriteLine("    AWS__SecretKey                  - AWS secret access key (.NET config format)");
            Console.WriteLine("    AWS__Region                     - AWS region (.NET config format)\n");
            
            Console.WriteLine("  Application Settings:");
            Console.WriteLine("    SecretsManager__SecretName      - Override secret name from config");
            Console.WriteLine("    SecretsManager__OutputMode      - Override output mode (env/file/both)");
            Console.WriteLine("    SecretsManager__EnvFilePath     - Override .env file path");
            Console.WriteLine("    SecretsManager__EnvExamplePath  - Override .env.example path\n");
            
            Console.WriteLine("Examples:");
            Console.WriteLine("  Fetch secrets using configuration:");
            Console.WriteLine("    ./SecretsManager\n");
            Console.WriteLine("  Fetch a specific secret:");
            Console.WriteLine("    ./SecretsManager --secret-name my-app-secrets --region us-west-2\n");
            Console.WriteLine("  Use explicit AWS credentials:");
            Console.WriteLine("    ./SecretsManager --access-key AKIAIOSFODNN7EXAMPLE --secret-key wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY\n");
            Console.WriteLine("  Use environment variables:");
            Console.WriteLine("    export AWS_ACCESS_KEY_ID=AKIAIOSFODNN7EXAMPLE");
            Console.WriteLine("    export AWS_SECRET_ACCESS_KEY=wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY");
            Console.WriteLine("    export AWS_DEFAULT_REGION=us-west-2");
            Console.WriteLine("    ./SecretsManager\n");
            Console.WriteLine("  Override secret name via environment:");
            Console.WriteLine("    export SecretsManager__SecretName=my-app-secrets");
            Console.WriteLine("    ./SecretsManager\n");
            Console.WriteLine("  Create .env file only:");
            Console.WriteLine("    ./SecretsManager --output-mode file --env-file ./config/.env\n");
            Console.WriteLine("  Parse custom .env.example:");
            Console.WriteLine("    ./SecretsManager --env-example ./config/.env.example\n");
            
            Console.WriteLine("Configuration Priority (highest to lowest):");
            Console.WriteLine("  1. Command line arguments");
            Console.WriteLine("  2. Environment variables");
            Console.WriteLine("  3. appsettings.json");
            Console.WriteLine("  4. AWS SDK credential chain (IAM roles, etc.)\n");
        }

        private static void DisplayVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version?.ToString() ?? "1.0.0";
            var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? version;
            
            Console.WriteLine($"Universal Secrets Manager v{informationalVersion}");
            Console.WriteLine($"Runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
            Console.WriteLine($"Platform: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
        }
    }
}
