using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SecretsManager.Models;

namespace SecretsManager.Services
{
    public class DevContainerService : IDevContainerService
    {
        private readonly ISecretsService _secretsService;
        private readonly ILogger<DevContainerService> _logger;
        private static readonly Regex AwsSecretArnRegex = new Regex(
            @"^\$\{arn:aws:secretsmanager:[^:]+:[^:]+:secret:[^}]+\}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        public DevContainerService(ISecretsService secretsService, ILogger<DevContainerService> logger)
        {
            _secretsService = secretsService;
            _logger = logger;
        }

        public async Task<DevContainerConfig?> LoadDevContainerConfigAsync(string filePath = ".devcontainer/devcontainer.json")
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("DevContainer config file not found at: {FilePath}", filePath);
                    return null;
                }

                var jsonContent = await File.ReadAllTextAsync(filePath);
                var config = JsonSerializer.Deserialize<DevContainerConfig>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                });

                _logger.LogInformation("Successfully loaded DevContainer config from: {FilePath}", filePath);
                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load DevContainer config from: {FilePath}", filePath);
                return null;
            }
        }

        public async Task ProcessDevContainerSecretsAsync(DevContainerConfig config)
        {
            if (config == null)
            {
                _logger.LogWarning("DevContainer config is null, skipping secret processing");
                return;
            }

            var secretsProcessed = 0;

            // Process build args
            if (config.Build?.Args != null)
            {
                _logger.LogInformation("Processing build args for AWS secrets...");
                var buildArgSecrets = ExtractAwsSecretArns(config.Build.Args);
                foreach (var (envVar, arn) in buildArgSecrets)
                {
                    await ProcessSecretArnAsync(envVar, arn);
                    secretsProcessed++;
                }
            }

            // Process container environment variables
            if (config.ContainerEnv != null)
            {
                _logger.LogInformation("Processing container environment variables for AWS secrets...");
                var containerEnvSecrets = ExtractAwsSecretArns(config.ContainerEnv);
                foreach (var (envVar, arn) in containerEnvSecrets)
                {
                    await ProcessSecretArnAsync(envVar, arn);
                    secretsProcessed++;
                }
            }

            // Process remote environment variables
            if (config.RemoteEnv != null)
            {
                _logger.LogInformation("Processing remote environment variables for AWS secrets...");
                var remoteEnvSecrets = ExtractAwsSecretArns(config.RemoteEnv);
                foreach (var (envVar, arn) in remoteEnvSecrets)
                {
                    await ProcessSecretArnAsync(envVar, arn);
                    secretsProcessed++;
                }
            }

            _logger.LogInformation("Processed {Count} AWS secrets from DevContainer config", secretsProcessed);
        }

        public Dictionary<string, string> ExtractAwsSecretArns(Dictionary<string, string> environmentVariables)
        {
            var secretArns = new Dictionary<string, string>();

            foreach (var (key, value) in environmentVariables)
            {
                if (IsAwsSecretArn(value))
                {
                    // Extract the ARN from the ${...} wrapper
                    var arn = value.Substring(2, value.Length - 3); // Remove ${ and }
                    secretArns[key] = arn;
                    _logger.LogDebug("Found AWS secret ARN for environment variable {EnvVar}: {Arn}", key, arn);
                }
            }

            return secretArns;
        }

        public bool IsAwsSecretArn(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return AwsSecretArnRegex.IsMatch(value);
        }

        private async Task ProcessSecretArnAsync(string environmentVariableName, string secretArn)
        {
            try
            {
                _logger.LogInformation("Processing secret ARN for environment variable {EnvVar}: {Arn}", 
                    environmentVariableName, secretArn);

                // Extract secret name/ID from ARN
                var secretId = ExtractSecretIdFromArn(secretArn);
                if (string.IsNullOrEmpty(secretId))
                {
                    _logger.LogError("Failed to extract secret ID from ARN: {Arn}", secretArn);
                    return;
                }

                // Fetch the secret value
                var secretValue = await _secretsService.GetSecretValueAsync(secretId);
                if (!string.IsNullOrEmpty(secretValue))
                {
                    // Set the environment variable
                    Environment.SetEnvironmentVariable(environmentVariableName, secretValue);
                    _logger.LogInformation("Successfully set environment variable {EnvVar} from AWS secret", 
                        environmentVariableName);
                }
                else
                {
                    _logger.LogWarning("Secret value is empty for ARN: {Arn}", secretArn);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process secret ARN {Arn} for environment variable {EnvVar}", 
                    secretArn, environmentVariableName);
            }
        }

        private string ExtractSecretIdFromArn(string arn)
        {
            // ARN format: arn:aws:secretsmanager:region:account:secret:name-suffix
            var parts = arn.Split(':');
            if (parts.Length >= 7)
            {
                // Return the secret name part (everything after "secret:")
                return string.Join(":", parts.Skip(6));
            }

            _logger.LogError("Invalid ARN format: {Arn}", arn);
            return string.Empty;
        }
    }
}
