using System;
using Xunit;
using SecretsManager.Models;

namespace SecretsManager.Tests.Models
{
    public class AwsConfigTests
    {
        [Fact]
        public void HasExplicitCredentials_WhenBothCredentialsProvided_ReturnsTrue()
        {
            var config = new AwsConfig
            {
                AccessKey = "test-key",
                SecretKey = "test-secret"
            };

            Assert.True(config.HasExplicitCredentials());
        }

        [Fact]
        public void HasExplicitCredentials_WhenOnlyAccessKeyProvided_ReturnsFalse()
        {
            var config = new AwsConfig
            {
                AccessKey = "test-key",
                SecretKey = null
            };

            Assert.False(config.HasExplicitCredentials());
        }

        [Fact]
        public void HasExplicitCredentials_WhenOnlySecretKeyProvided_ReturnsFalse()
        {
            var config = new AwsConfig
            {
                AccessKey = null,
                SecretKey = "test-secret"
            };

            Assert.False(config.HasExplicitCredentials());
        }

        [Fact]
        public void HasExplicitCredentials_WhenNoCredentialsProvided_ReturnsFalse()
        {
            var config = new AwsConfig
            {
                AccessKey = null,
                SecretKey = null
            };

            Assert.False(config.HasExplicitCredentials());
        }

        [Fact]
        public void HasExplicitCredentials_WhenEmptyStrings_ReturnsFalse()
        {
            var config = new AwsConfig
            {
                AccessKey = "",
                SecretKey = ""
            };

            Assert.False(config.HasExplicitCredentials());
        }

        [Fact]
        public void Validate_WhenRegionIsEmpty_ThrowsException()
        {
            var config = new AwsConfig
            {
                Region = "",
                AccessKey = "test-key",
                SecretKey = "test-secret"
            };

            var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
            Assert.Equal("AWS Region is required", exception.Message);
        }

        [Fact]
        public void Validate_WhenOnlyAccessKeyProvided_ThrowsException()
        {
            var config = new AwsConfig
            {
                Region = "us-east-1",
                AccessKey = "test-key",
                SecretKey = null
            };

            var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
            Assert.Equal("Both AccessKey and SecretKey must be provided together, or neither should be provided", exception.Message);
        }

        [Fact]
        public void Validate_WhenOnlySecretKeyProvided_ThrowsException()
        {
            var config = new AwsConfig
            {
                Region = "us-east-1",
                AccessKey = null,
                SecretKey = "test-secret"
            };

            var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
            Assert.Equal("Both AccessKey and SecretKey must be provided together, or neither should be provided", exception.Message);
        }

        [Fact]
        public void Validate_WhenBothCredentialsProvided_DoesNotThrow()
        {
            var config = new AwsConfig
            {
                Region = "us-east-1",
                AccessKey = "test-key",
                SecretKey = "test-secret"
            };

            // Should not throw
            config.Validate();
        }

        [Fact]
        public void Validate_WhenNoCredentialsProvided_DoesNotThrow()
        {
            var config = new AwsConfig
            {
                Region = "us-east-1",
                AccessKey = null,
                SecretKey = null
            };

            // Should not throw - allowing credential-less config for IAM roles
            config.Validate();
        }
    }
}