using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using SecretsManager.Services;
using Xunit;

namespace SecretsManager.Tests.Services
{
    public class EnvFileServiceTests : IDisposable
    {
        private readonly Mock<ISecretsService> _mockSecretsService;
        private readonly Mock<ILogger<EnvFileService>> _mockLogger;
        private readonly EnvFileService _envFileService;
        private readonly string _testDirectory;

        public EnvFileServiceTests()
        {
            _mockSecretsService = new Mock<ISecretsService>();
            _mockLogger = new Mock<ILogger<EnvFileService>>();
            _envFileService = new EnvFileService(_mockSecretsService.Object, _mockLogger.Object);
            _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
        }

        [Fact]
        public async Task LoadEnvExampleFileAsync_WhenFileDoesNotExist_ReturnsNull()
        {
            // Arrange
            var filePath = Path.Combine(_testDirectory, ".env.example");

            // Act
            var result = await _envFileService.LoadEnvExampleFileAsync(filePath);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task LoadEnvExampleFileAsync_WhenFileExists_ParsesCorrectly()
        {
            // Arrange
            var filePath = Path.Combine(_testDirectory, ".env.example");
            var content = @"# This is a comment
DATABASE_URL=${arn:aws:secretsmanager:us-east-1:123456789012:secret:db-url-abc123}
API_KEY=${arn:aws:secretsmanager:us-east-1:123456789012:secret:api-key-def456}
DEBUG_MODE=true
SERVICE_URL=""https://api.example.com""

# Another comment
EMPTY_VALUE=
";
            await File.WriteAllTextAsync(filePath, content);

            // Act
            var result = await _envFileService.LoadEnvExampleFileAsync(filePath);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.Count);
            Assert.Equal("${arn:aws:secretsmanager:us-east-1:123456789012:secret:db-url-abc123}", result["DATABASE_URL"]);
            Assert.Equal("${arn:aws:secretsmanager:us-east-1:123456789012:secret:api-key-def456}", result["API_KEY"]);
            Assert.Equal("true", result["DEBUG_MODE"]);
            Assert.Equal("https://api.example.com", result["SERVICE_URL"]);
            Assert.Equal("", result["EMPTY_VALUE"]);
        }

        [Fact]
        public async Task ProcessEnvFileSecretsAsync_WithMixedValues_ProcessesArnsOnly()
        {
            // Arrange
            var envVars = new Dictionary<string, string>
            {
                ["DATABASE_URL"] = "${arn:aws:secretsmanager:us-east-1:123456789012:secret:db-url-abc123}",
                ["API_KEY"] = "${arn:aws:secretsmanager:us-east-1:123456789012:secret:api-key-def456}",
                ["DEBUG_MODE"] = "true",
                ["SERVICE_URL"] = "https://api.example.com"
            };

            _mockSecretsService.Setup(s => s.GetSecretValueAsync("db-url-abc123"))
                .ReturnsAsync("postgresql://user:pass@host:5432/db");
            _mockSecretsService.Setup(s => s.GetSecretValueAsync("api-key-def456"))
                .ReturnsAsync("sk-1234567890abcdef");

            // Act
            var result = await _envFileService.ProcessEnvFileSecretsAsync(envVars);

            // Assert
            Assert.Equal(4, result.Count);
            Assert.Equal("postgresql://user:pass@host:5432/db", result["DATABASE_URL"]);
            Assert.Equal("sk-1234567890abcdef", result["API_KEY"]);
            Assert.Equal("true", result["DEBUG_MODE"]);
            Assert.Equal("https://api.example.com", result["SERVICE_URL"]);

            _mockSecretsService.Verify(s => s.GetSecretValueAsync("db-url-abc123"), Times.Once);
            _mockSecretsService.Verify(s => s.GetSecretValueAsync("api-key-def456"), Times.Once);
        }

        [Fact]
        public async Task ProcessEnvFileSecretsAsync_WhenSecretFetchFails_KeepsOriginalValue()
        {
            // Arrange
            var envVars = new Dictionary<string, string>
            {
                ["DATABASE_URL"] = "${arn:aws:secretsmanager:us-east-1:123456789012:secret:db-url-abc123}"
            };

            _mockSecretsService.Setup(s => s.GetSecretValueAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Secret not found"));

            // Act
            var result = await _envFileService.ProcessEnvFileSecretsAsync(envVars);

            // Assert
            Assert.Equal("${arn:aws:secretsmanager:us-east-1:123456789012:secret:db-url-abc123}", result["DATABASE_URL"]);
        }

        [Fact]
        public async Task WriteEnvFileAsync_CreatesFileWithCorrectFormat()
        {
            // Arrange
            var filePath = Path.Combine(_testDirectory, ".env");
            var envVars = new Dictionary<string, string>
            {
                ["DATABASE_URL"] = "postgresql://user:pass@host:5432/db",
                ["API_KEY"] = "sk-1234567890abcdef",
                ["DEBUG_MODE"] = "true",
                ["SERVICE_URL"] = "https://api.example.com",
                ["QUOTED_VALUE"] = "value with spaces"
            };

            // Act
            await _envFileService.WriteEnvFileAsync(envVars, filePath);

            // Assert
            Assert.True(File.Exists(filePath));
            var content = await File.ReadAllTextAsync(filePath);
            
            Assert.Contains("# Generated by SecretsManager", content);
            Assert.Contains("API_KEY=sk-1234567890abcdef", content);
            Assert.Contains("DATABASE_URL=postgresql://user:pass@host:5432/db", content);
            Assert.Contains("DEBUG_MODE=true", content);
            Assert.Contains("SERVICE_URL=https://api.example.com", content);
            Assert.Contains("QUOTED_VALUE=\"value with spaces\"", content);
        }

        [Fact]
        public async Task IsAwsSecretArn_ValidatesArnsCorrectly()
        {
            // Arrange & Act & Assert
            var service = new EnvFileService(_mockSecretsService.Object, _mockLogger.Object);

            // Valid ARNs
            Assert.True(IsAwsSecretArn("${arn:aws:secretsmanager:us-east-1:123456789012:secret:my-secret-abc123}"));
            Assert.True(IsAwsSecretArn("${arn:aws:secretsmanager:eu-west-1:987654321098:secret:api-key}"));

            // Invalid ARNs
            Assert.False(IsAwsSecretArn("arn:aws:secretsmanager:us-east-1:123456789012:secret:my-secret")); // No ${}
            Assert.False(IsAwsSecretArn("${not-an-arn}"));
            Assert.False(IsAwsSecretArn(""));
            Assert.False(IsAwsSecretArn(null));
        }

        // Helper method to test private IsAwsSecretArn via reflection
        private bool IsAwsSecretArn(string value)
        {
            var method = typeof(EnvFileService).GetMethod("IsAwsSecretArn", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (bool)method.Invoke(_envFileService, new object[] { value });
        }

        public void Dispose()
        {
            // Cleanup
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }
    }
}