using SecretsManager.Models;

namespace SecretsManager.Services
{
    public interface IDevContainerService
    {
        Task<DevContainerConfig?> LoadDevContainerConfigAsync(string filePath = ".devcontainer/devcontainer.json");
        Task ProcessDevContainerSecretsAsync(DevContainerConfig config);
        Dictionary<string, string> ExtractAwsSecretArns(Dictionary<string, string> environmentVariables);
        bool IsAwsSecretArn(string value);
    }
}
