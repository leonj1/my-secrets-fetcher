using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using SecretsManager.Models;
using SecretsManager.Services;
using Xunit;

namespace SecretsManager.Tests
{
    public class DevContainerServiceTests
    {
        private readonly Mock<ISecretsService> _mockSecretsService;
        private readonly Mock<ILogger<DevContainerService>> _mockLogger;
        private readonly DevContainerService _service;

        public DevContainerServiceTests()
        {
            _mockSecretsService = new Mock<ISecretsService>();
            _mockLogger = new Mock<ILogger<DevContainerService>>();
            _service = new DevContainerService(_mockSecretsService.Object, _mockLogger.Object);
        }

        [Fact]
        public void IsAwsSecretArn_ValidArn_ReturnsTrue()
        {
            // Arrange
            var validArn = "${arn:aws:secretsmanager:us-east-1:123456789012:secret:MyDatabaseSecret-abcdef}";

            // Act
            var result = _service.IsAwsSecretArn(validArn);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsAwsSecretArn_InvalidArn_ReturnsFalse()
        {
            // Arrange
            var invalidArn = "not-an-arn";

            // Act
            var result = _service.IsAwsSecretArn(invalidArn);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsAwsSecretArn_EmptyString_ReturnsFalse()
        {
            // Arrange
            var emptyArn = "";

            // Act
            var result = _service.IsAwsSecretArn(emptyArn);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsAwsSecretArn_NullString_ReturnsFalse()
        {
            // Arrange
            string? nullArn = null;

            // Act
            var result = _service.IsAwsSecretArn(nullArn!);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ExtractAwsSecretArns_WithValidArns_ReturnsCorrectMapping()
        {
            // Arrange
            var environmentVariables = new Dictionary<string, string>
            {
                { "GIT_TOKEN", "${arn:aws:secretsmanager:us-east-1:123456789012:secret:MyDatabaseSecret-abcdef}" },
                { "API_SECRET", "${arn:aws:secretsmanager:us-east-1:123456789012:secret:AnotherSecret-xyz123}" },
                { "REGULAR_VAR", "not-a-secret" }
            };

            // Act
            var result = _service.ExtractAwsSecretArns(environmentVariables);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("arn:aws:secretsmanager:us-east-1:123456789012:secret:MyDatabaseSecret-abcdef", result["GIT_TOKEN"]);
            Assert.Equal("arn:aws:secretsmanager:us-east-1:123456789012:secret:AnotherSecret-xyz123", result["API_SECRET"]);
            Assert.False(result.ContainsKey("REGULAR_VAR"));
        }

        [Fact]
        public void ExtractAwsSecretArns_WithNoValidArns_ReturnsEmptyDictionary()
        {
            // Arrange
            var environmentVariables = new Dictionary<string, string>
            {
                { "VAR1", "value1" },
                { "VAR2", "value2" }
            };

            // Act
            var result = _service.ExtractAwsSecretArns(environmentVariables);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task LoadDevContainerConfigAsync_ValidFile_ReturnsConfig()
        {
            // Arrange
            var testConfig = new DevContainerConfig
            {
                Name = "Test Project",
                Build = new BuildConfig
                {
                    Dockerfile = "Dockerfile",
                    Args = new Dictionary<string, string>
                    {
                        { "GIT_TOKEN", "${arn:aws:secretsmanager:us-east-1:123456789012:secret:MyDatabaseSecret-abcdef}" }
                    }
                }
            };

            var jsonContent = JsonSerializer.Serialize(testConfig, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, jsonContent);

            try
            {
                // Act
                var result = await _service.LoadDevContainerConfigAsync(tempFile);

                // Assert
                Assert.NotNull(result);
                Assert.Equal("Test Project", result.Name);
                Assert.NotNull(result.Build);
                Assert.NotNull(result.Build.Args);
                Assert.Equal("${arn:aws:secretsmanager:us-east-1:123456789012:secret:MyDatabaseSecret-abcdef}", 
                    result.Build.Args["GIT_TOKEN"]);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task LoadDevContainerConfigAsync_NonExistentFile_ReturnsNull()
        {
            // Arrange
            var nonExistentFile = "non-existent-file.json";

            // Act
            var result = await _service.LoadDevContainerConfigAsync(nonExistentFile);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ProcessDevContainerSecretsAsync_WithBuildArgs_ProcessesSecrets()
        {
            // Arrange
            var config = new DevContainerConfig
            {
                Name = "Test Project",
                Build = new BuildConfig
                {
                    Args = new Dictionary<string, string>
                    {
                        { "GIT_TOKEN", "${arn:aws:secretsmanager:us-east-1:123456789012:secret:MyDatabaseSecret-abcdef}" },
                        { "REGULAR_VAR", "not-a-secret" }
                    }
                }
            };

            _mockSecretsService
                .Setup(s => s.GetSecretValueAsync("MyDatabaseSecret-abcdef"))
                .ReturnsAsync("secret-token-value");

            // Act
            await _service.ProcessDevContainerSecretsAsync(config);

            // Assert
            _mockSecretsService.Verify(s => s.GetSecretValueAsync("MyDatabaseSecret-abcdef"), Times.Once);
            Assert.Equal("secret-token-value", Environment.GetEnvironmentVariable("GIT_TOKEN"));
        }

        [Fact]
        public async Task ProcessDevContainerSecretsAsync_WithContainerEnv_ProcessesSecrets()
        {
            // Arrange
            var config = new DevContainerConfig
            {
                Name = "Test Project",
                ContainerEnv = new Dictionary<string, string>
                {
                    { "API_SECRET", "${arn:aws:secretsmanager:us-east-1:123456789012:secret:AnotherSecret-xyz123}" }
                }
            };

            _mockSecretsService
                .Setup(s => s.GetSecretValueAsync("AnotherSecret-xyz123"))
                .ReturnsAsync("api-secret-value");

            // Act
            await _service.ProcessDevContainerSecretsAsync(config);

            // Assert
            _mockSecretsService.Verify(s => s.GetSecretValueAsync("AnotherSecret-xyz123"), Times.Once);
            Assert.Equal("api-secret-value", Environment.GetEnvironmentVariable("API_SECRET"));
        }

        [Fact]
        public async Task ProcessDevContainerSecretsAsync_WithRemoteEnv_ProcessesSecrets()
        {
            // Arrange
            var config = new DevContainerConfig
            {
                Name = "Test Project",
                RemoteEnv = new Dictionary<string, string>
                {
                    { "REMOTE_SECRET", "${arn:aws:secretsmanager:us-east-1:123456789012:secret:RemoteSecret-def456}" }
                }
            };

            _mockSecretsService
                .Setup(s => s.GetSecretValueAsync("RemoteSecret-def456"))
                .ReturnsAsync("remote-secret-value");

            // Act
            await _service.ProcessDevContainerSecretsAsync(config);

            // Assert
            _mockSecretsService.Verify(s => s.GetSecretValueAsync("RemoteSecret-def456"), Times.Once);
            Assert.Equal("remote-secret-value", Environment.GetEnvironmentVariable("REMOTE_SECRET"));
        }

        [Fact]
        public async Task ProcessDevContainerSecretsAsync_NullConfig_DoesNotThrow()
        {
            // Arrange
            DevContainerConfig? config = null;

            // Act & Assert
            await _service.ProcessDevContainerSecretsAsync(config!);
            
            // Verify no secrets service calls were made
            _mockSecretsService.Verify(s => s.GetSecretValueAsync(It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData("${arn:aws:secretsmanager:us-east-1:123456789012:secret:MySecret-abcdef}", true)]
        [InlineData("${arn:aws:secretsmanager:eu-west-1:987654321098:secret:AnotherSecret-xyz123}", true)]
        [InlineData("arn:aws:secretsmanager:us-east-1:123456789012:secret:MySecret-abcdef", false)]
        [InlineData("${not-an-arn}", false)]
        [InlineData("regular-value", false)]
        [InlineData("", false)]
        public void IsAwsSecretArn_VariousInputs_ReturnsExpectedResult(string input, bool expected)
        {
            // Act
            var result = _service.IsAwsSecretArn(input);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
