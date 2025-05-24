using System.Text.Json.Serialization;

namespace SecretsManager.Models
{
    public class AppSecrets
    {
        [JsonPropertyName("database_url")]
        public string DatabaseUrl { get; set; } = string.Empty;
        
        [JsonPropertyName("api_key")]
        public string ApiKey { get; set; } = string.Empty;
        
        [JsonPropertyName("jwt_secret")]
        public string JwtSecret { get; set; } = string.Empty;
        
        [JsonPropertyName("redis_url")]
        public string RedisUrl { get; set; } = string.Empty;
    }
}
