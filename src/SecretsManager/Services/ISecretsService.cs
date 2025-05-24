using SecretsManager.Models;

namespace SecretsManager.Services
{
    public interface ISecretsService
    {
        Task<AppSecrets> GetSecretsAsync();
    }
}
