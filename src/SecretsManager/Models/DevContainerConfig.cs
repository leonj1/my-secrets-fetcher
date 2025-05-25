using System.Text.Json.Serialization;

namespace SecretsManager.Models
{
    public class DevContainerConfig
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("build")]
        public BuildConfig? Build { get; set; }

        [JsonPropertyName("containerEnv")]
        public Dictionary<string, string>? ContainerEnv { get; set; }

        [JsonPropertyName("remoteEnv")]
        public Dictionary<string, string>? RemoteEnv { get; set; }
    }

    public class BuildConfig
    {
        [JsonPropertyName("dockerfile")]
        public string? Dockerfile { get; set; }

        [JsonPropertyName("args")]
        public Dictionary<string, string>? Args { get; set; }
    }
}
