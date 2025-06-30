namespace SecretsManager.Models
{
    public class AwsConfig
    {
        public string Region { get; set; } = string.Empty;
        public string ServiceURL { get; set; } = string.Empty;
        public string? AccessKey { get; set; }
        public string? SecretKey { get; set; }

        /// <summary>
        /// Determines if explicit credentials are provided
        /// </summary>
        public bool HasExplicitCredentials()
        {
            return !string.IsNullOrEmpty(AccessKey) && !string.IsNullOrEmpty(SecretKey);
        }

        /// <summary>
        /// Validates the configuration based on whether explicit credentials are required
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrEmpty(Region))
            {
                throw new InvalidOperationException("AWS Region is required");
            }

            // If one credential is provided, both must be provided
            var hasAccessKey = !string.IsNullOrEmpty(AccessKey);
            var hasSecretKey = !string.IsNullOrEmpty(SecretKey);
            
            if (hasAccessKey != hasSecretKey)
            {
                throw new InvalidOperationException("Both AccessKey and SecretKey must be provided together, or neither should be provided");
            }
        }
    }
}
