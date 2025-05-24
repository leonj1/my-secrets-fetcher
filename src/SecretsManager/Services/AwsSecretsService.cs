using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SecretsManager.Models;

namespace SecretsManager.Services
{
    public class AwsSecretsService : ISecretsService
    {
        private readonly IAmazonSecretsManager _secretsManager;
        private readonly SecretsManagerConfig _config;
        private readonly ILogger<AwsSecretsService> _logger;

        public AwsSecretsService(
            IAmazonSecretsManager secretsManager,
            IOptions<SecretsManagerConfig> config,
            ILogger<AwsSecretsService> logger)
        {
            _secretsManager = secretsManager;
            _config = config.Value;
            _logger = logger;
        }

        public async Task<AppSecrets> GetSecretsAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving secret: {SecretName}", _config.SecretName);

                var request = new GetSecretValueRequest
                {
                    SecretId = _config.SecretName
                };

                var response = await _secretsManager.GetSecretValueAsync(request);
                
                _logger.LogInformation("Successfully retrieved secret from AWS Secrets Manager");

                var secrets = JsonConvert.DeserializeObject<AppSecrets>(response.SecretString);
                
                if (secrets == null)
                {
                    _logger.LogError("Failed to deserialize secrets from JSON");
                    throw new InvalidOperationException("Failed to deserialize secrets from JSON");
                }

                _logger.LogInformation("Successfully parsed secrets");
                return secrets;
            }
            catch (ResourceNotFoundException ex)
            {
                _logger.LogError(ex, "Secret not found: {SecretName}", _config.SecretName);
                throw new InvalidOperationException($"Secret '{_config.SecretName}' not found", ex);
            }
            catch (InvalidRequestException ex)
            {
                _logger.LogError(ex, "Invalid request for secret: {SecretName}", _config.SecretName);
                throw new InvalidOperationException($"Invalid request for secret '{_config.SecretName}'", ex);
            }
            catch (InvalidParameterException ex)
            {
                _logger.LogError(ex, "Invalid parameter for secret: {SecretName}", _config.SecretName);
                throw new InvalidOperationException($"Invalid parameter for secret '{_config.SecretName}'", ex);
            }
            catch (DecryptionFailureException ex)
            {
                _logger.LogError(ex, "Decryption failed for secret: {SecretName}", _config.SecretName);
                throw new InvalidOperationException($"Decryption failed for secret '{_config.SecretName}'", ex);
            }
            catch (InternalServiceErrorException ex)
            {
                _logger.LogError(ex, "Internal service error retrieving secret: {SecretName}", _config.SecretName);
                throw new InvalidOperationException($"Internal service error retrieving secret '{_config.SecretName}'", ex);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse JSON from secret: {SecretName}", _config.SecretName);
                throw new InvalidOperationException($"Failed to parse JSON from secret '{_config.SecretName}'", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving secret: {SecretName}", _config.SecretName);
                throw new InvalidOperationException($"Unexpected error retrieving secret '{_config.SecretName}'", ex);
            }
        }
    }
}
