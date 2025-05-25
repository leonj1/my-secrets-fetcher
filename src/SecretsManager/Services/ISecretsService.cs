using SecretsManager.Models;

namespace SecretsManager.Services
{
    public interface ISecretsService
    {
        Task<AppSecrets> GetSecretsAsync();
        Task<string> GetSecretValueAsync(string secretId);
    }
}
