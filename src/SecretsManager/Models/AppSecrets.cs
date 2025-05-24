namespace SecretsManager.Models
{
    public class AppSecrets
    {
        public string DatabaseUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string JwtSecret { get; set; } = string.Empty;
        public string RedisUrl { get; set; } = string.Empty;
    }
}
