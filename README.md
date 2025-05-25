# .NET Secrets Manager Application

This application demonstrates how to integrate .NET Core with AWS Secrets Manager using LocalStack for local development. It provides a complete workflow for managing secrets in a containerized development environment with Infrastructure as Code (IaC) using Terraform.

## 🆕 NEW: DevContainer Secrets Management

**Automatically fetch AWS secrets in your DevContainer environment!**

This project now includes advanced DevContainer integration that automatically detects AWS Secrets Manager ARNs in your DevContainer configuration and fetches the actual secret values at container startup.

### ✨ Key Features

- **🔍 Automatic ARN Detection**: Scans `devcontainer.json` for AWS Secrets Manager ARNs
- **🔄 Real-time Secret Fetching**: Retrieves actual values from AWS Secrets Manager
- **🌍 Environment Variable Injection**: Automatically sets environment variables with secret values
- **🛡️ Secure by Design**: Secrets are never stored in configuration files
- **📍 Multi-Location Support**: Works with `containerEnv`, `build.args`, and `remoteEnv`

### 🚀 How It Works

Simply add AWS Secrets Manager ARNs to your DevContainer configuration:

```json
{
  "containerEnv": {
    "GIT_TOKEN": "${arn:aws:secretsmanager:us-east-1:000000000000:secret:my-secret-abc123}",
    "API_KEY": "${arn:aws:secretsmanager:us-east-1:000000000000:secret:api-keys-def456}",
    "REGULAR_VAR": "not-a-secret"
  }
}
```

The system automatically:
1. Detects ARN patterns in your DevContainer config
2. Fetches the actual secret values from AWS Secrets Manager
3. Sets environment variables with the retrieved values
4. Your application can access secrets via standard environment variables

### 🧪 Comprehensive Testing

- **18 Unit Tests** covering all functionality
- **ARN Validation** and extraction testing
- **Multi-configuration** support testing
- **Error Handling** and edge case coverage

# Use Case

This project fetches secrets from AWS Secrets Manager and applies them to the local development environment as environment variables or in an `.env` file. This allows developers to easily access sensitive information without exposing it in their source code.

# How to Use
1. Create secrets in AWS Secrets Manager
2. Work in a project that expects secrets to be available as environment variables or in an `.env` file
3. ./SecretsManager fetches the secrets from AWS Secrets Manager and applies them to the local development environment

```
# validate the value
env | grep -E "(DATABASE_URL|API_KEY|JWT_SECRET|REDIS_URL)"
# or
cat src/SecretsManager/.env
```

## 🚀 Quick Start to Validate this Project

1. **Start the infrastructure**: `make setup-infrastructure`
2. **Build and run the application**: `make test-app`
3. **Check the generated `.env` file and environment variables**

## 🏗️ Project Architecture Overview

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   .NET App      │    │   LocalStack    │    │   Terraform     │
│                 │    │                 │    │                 │
│ ┌─────────────┐ │    │ ┌─────────────┐ │    │ ┌─────────────┐ │
│ │ Secrets     │ │◄──►│ │ Secrets     │ │◄──►│ │ IaC         │ │
│ │ Service     │ │    │ │ Manager     │ │    │ │ Deployment  │ │
│ └─────────────┘ │    │ └─────────────┘ │    │ └─────────────┘ │
│                 │    │                 │    │                 │
│ ┌─────────────┐ │    │ ┌─────────────┐ │    │                 │
│ │ DevContainer│ │    │ │ IAM/STS     │ │    │                 │
│ │ Service     │ │    │ │ Services    │ │    │                 │
│ └─────────────┘ │    │ └─────────────┘ │    │                 │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

## 🛠️ Available Commands

### Infrastructure Management
- `make start` - Start LocalStack containers
- `make stop` - Stop LocalStack containers
- `make restart` - Restart LocalStack containers
- `make status` - Show container status
- `make setup-infrastructure` - Deploy complete infrastructure (LocalStack + Terraform)

### Development Commands
- `make dotnet-build` - Build the .NET application
- `make dotnet-run` - Build and run the .NET application
- `make dotnet-clean` - Clean .NET build artifacts
- `make full-setup` - Complete setup (infrastructure + application build)

### Testing & Validation
- `make test` - Run unit tests
- `make dotnet-test` - Run unit tests (alternative)
- `make test-app` - Run end-to-end application test
- `make health-check` - Run comprehensive health diagnostics
- `make localstack-status` - Check LocalStack service status
- `make test-secrets` - Test direct secret retrieval

# Example Usage
## ⚙️ Configuration

### DevContainer Secrets Configuration

This project assumes your devcontainer will have access to AWS Secrets Manager ARNs.
This can be achieved by your runtime already having authenticated to AWS via OIDC, or by authenticating to AWS from within the devcontainer.
Configure AWS Secrets Manager ARNs directly in your `devcontainer.json`:

```json
{
  "name": "My Development Environment",
  "containerEnv": {
    "DATABASE_PASSWORD": "${arn:aws:secretsmanager:us-east-1:123456789012:secret:db-password-abc123}",
    "API_TOKEN": "${arn:aws:secretsmanager:us-east-1:123456789012:secret:api-token-def456}",
    "REGULAR_CONFIG": "not-a-secret"
  },
  "build": {
    "args": {
      "BUILD_SECRET": "${arn:aws:secretsmanager:us-east-1:123456789012:secret:build-secret-ghi789}"
    }
  },
  "remoteEnv": {
    "REMOTE_KEY": "${arn:aws:secretsmanager:us-east-1:123456789012:secret:remote-key-jkl012}"
  }
}
```

**Supported ARN Locations:**
- `containerEnv` - Environment variables for the container
- `build.args` - Build-time arguments
- `remoteEnv` - Remote environment variables

### Application Configuration

The application can be configured through `appsettings.json`:

```json
{
  "SecretConfiguration": {
    "SecretName": "dotnet-app-secrets",
    "Region": "us-east-1",
    "ServiceUrl": "http://localhost:4566",
    "OutputMode": "Both",
    "EnvFilePath": ".env"
  }
}
```

**Configuration Options:**
- `SecretName`: Name of the secret in AWS Secrets Manager
- `Region`: AWS region (default: us-east-1)
- `ServiceUrl`: LocalStack endpoint URL
- `OutputMode`: How to output secrets (`EnvironmentVariables`, `EnvFile`, or `Both`)
- `EnvFilePath`: Path for the .env file output

### Secret Management

Secrets are managed through Terraform variables in `terraform/variables.tf`:

```hcl
variable "secrets" {
  description = "Map of secrets to create"
  type = map(object({
    name        = string
    description = string
    secret_data = map(string)
  }))
  default = {
    app_secrets = {
      name        = "dotnet-app-secrets"
      description = "Application secrets for .NET app"
      secret_data = {
        database_url    = "postgresql://localhost:5432/mydb"
        api_key        = "your-api-key-here"
        jwt_secret     = "your-jwt-secret"
        redis_url      = "redis://localhost:6379"
      }
    }
  }
}
```

```bash
make health-check
```

This will check:
- ✅ LocalStack service status
- ✅ Secrets Manager availability
- ✅ Secret retrieval functionality
- ✅ Docker container health
- ✅ Network connectivity
- ✅ DevContainer configuration validation


## 🚀 Production Considerations

### Security
- Replace default credentials in production
- Use proper AWS IAM roles and policies
- Implement secret rotation
- Enable encryption at rest and in transit
- **✨ DevContainer secrets are fetched at runtime, never stored in config**

### Scalability
- Consider using AWS ECS/EKS for container orchestration
- Implement proper logging and monitoring
- Use AWS Application Load Balancer for high availability
- Implement circuit breakers and retry policies

### Monitoring
- Add health check endpoints
- Implement structured logging
- Use AWS CloudWatch for monitoring
- Set up alerting for secret retrieval failures
- **✨ Monitor DevContainer secret fetch operations**

## 📚 Additional Resources

- [AWS Secrets Manager Documentation](https://docs.aws.amazon.com/secretsmanager/)
- [LocalStack Documentation](https://docs.localstack.cloud/)
- [Terraform AWS Provider](https://registry.terraform.io/providers/hashicorp/aws/latest/docs)
- [.NET Configuration Documentation](https://docs.microsoft.com/en-us/dotnet/core/extensions/configuration)
- [VS Code Dev Containers](https://code.visualstudio.com/docs/devcontainers/containers)

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

---

**Happy coding! 🎉**
