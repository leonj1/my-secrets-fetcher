namespace SecretsManager.Models
{
    public class AwsConfig
    {
        public string Region { get; set; } = string.Empty;
        public string ServiceURL { get; set; } = string.Empty;
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
    }
}
