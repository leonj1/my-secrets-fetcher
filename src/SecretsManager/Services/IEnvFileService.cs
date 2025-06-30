namespace SecretsManager.Services
{
    public interface IEnvFileService
    {
        Task<Dictionary<string, string>?> LoadEnvExampleFileAsync(string filePath = ".env.example");
        Task<Dictionary<string, string>> ProcessEnvFileSecretsAsync(Dictionary<string, string> envVars);
        Task WriteEnvFileAsync(Dictionary<string, string> envVars, string filePath = ".env");
    }
}