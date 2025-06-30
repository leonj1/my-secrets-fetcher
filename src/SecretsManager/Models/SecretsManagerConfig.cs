namespace SecretsManager.Models
{
    public class SecretsManagerConfig
    {
        public string SecretName { get; set; } = string.Empty;
        public OutputMode OutputMode { get; set; } = OutputMode.Both;
        public string EnvFilePath { get; set; } = ".env";
        public string EnvExamplePath { get; set; } = ".env.example";
    }
}
