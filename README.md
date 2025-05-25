# .NET Secrets Manager Application

This application demonstrates how to integrate .NET Core with AWS Secrets Manager using LocalStack for local development. It provides a complete workflow for managing secrets in a containerized development environment with Infrastructure as Code (IaC) using Terraform.

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

## 🚀 Quick Start

1. **Start the infrastructure**: `make setup-infrastructure`
2. **Build and run the application**: `make test-app`
3. **Check the generated `.env` file and environment variables**

## 📋 Prerequisites

- Docker and Docker Compose
- .NET 7.0 SDK or later
- Make utility
- Git

## 🏗️ Architecture Overview

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
│ │ Environment │ │    │ │ IAM/STS     │ │    │                 │
│ │ Manager     │ │    │ │ Services    │ │    │                 │
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
- `make test-app` - Run end-to-end application test
- `make health-check` - Run comprehensive health diagnostics
- `make localstack-status` - Check LocalStack service status
- `make test-secrets` - Test direct secret retrieval

### Terraform Operations
- `make terraform-build` - Build Terraform Docker image
- `make terraform-init` - Initialize Terraform
- `make terraform-plan` - Plan Terraform changes
- `make terraform-apply` - Apply Terraform configuration
- `make terraform-destroy` - Destroy Terraform infrastructure

### Cleanup
- `make clean` - Basic cleanup (containers, build artifacts)
- `make cleanup` - Comprehensive cleanup (everything + Docker pruning)

## 📁 Project Structure

```
my-secrets-fetcher/
├── .devcontainer/              # VS Code dev container configuration
│   ├── devcontainer.json
│   ├── docker-compose.yml
│   ├── Dockerfile
│   ├── setup.sh
│   └── README.md
├── docker/                     # Docker configurations
│   └── Dockerfile.terraform    # Terraform container
├── scripts/                    # Utility scripts
│   ├── health-check.sh        # Health diagnostics
│   └── cleanup.sh             # Environment cleanup
├── src/SecretsManager/         # .NET application
│   ├── Models/                # Data models
│   │   ├── AppSecrets.cs
│   │   ├── AwsConfig.cs
│   │   └── SecretsManagerConfig.cs
│   ├── Services/              # Business logic
│   │   ├── ISecretsService.cs
│   │   └── AwsSecretsService.cs
│   ├── Program.cs             # Application entry point
│   ├── appsettings.json       # Configuration
│   ├── appsettings.Development.json
│   └── SecretsManager.csproj  # Project file
├── terraform/                  # Infrastructure as Code
│   ├── main.tf               # Main Terraform configuration
│   ├── variables.tf          # Variable definitions
│   ├── outputs.tf            # Output definitions
│   └── provider.tf           # Provider configuration
├── docker-compose.yml         # LocalStack services
├── Makefile                   # Build automation
└── README.md                  # This file
```

## ⚙️ Configuration

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

## 🔧 Development Workflow

### 1. Initial Setup
```bash
# Clone the repository
git clone <repository-url>
cd my-secrets-fetcher

# Start the complete environment
make setup-infrastructure
```

### 2. Development Cycle
```bash
# Build and test the application
make dotnet-build
make dotnet-run

# Run health checks
make health-check

# Test secret retrieval
make test-secrets
```

### 3. Making Changes
```bash
# After code changes
make dotnet-build
make dotnet-run

# After infrastructure changes
make terraform-plan
make terraform-apply
```

### 4. Cleanup
```bash
# Quick cleanup
make clean

# Complete cleanup (frees significant disk space)
make cleanup
```

## 🐳 Container Development

This project includes a complete VS Code dev container setup:

```bash
# Open in VS Code with dev containers extension
code .

# Or use the dev container directly
docker-compose -f .devcontainer/docker-compose.yml up -d
```

The dev container includes:
- .NET 8.0 SDK
- AWS CLI
- Terraform
- Docker CLI
- All necessary development tools

## 🔍 Troubleshooting

### Common Issues

**LocalStack not responding:**
```bash
make health-check
make restart
```

**Terraform state issues:**
```bash
make terraform-destroy
make cleanup
make setup-infrastructure
```

**Build failures:**
```bash
make dotnet-clean
make dotnet-build
```

**Permission errors during cleanup:**
```bash
sudo make cleanup
# or
sudo ./scripts/cleanup.sh
```

### Health Diagnostics

The health check script provides comprehensive diagnostics:

```bash
make health-check
```

This will check:
- ✅ LocalStack service status
- ✅ Secrets Manager availability
- ✅ Secret retrieval functionality
- ✅ Docker container health
- ✅ Network connectivity

### Logs and Debugging

**View LocalStack logs:**
```bash
docker-compose logs localstack
```

**View application logs:**
```bash
make dotnet-run
```

**Debug Terraform:**
```bash
make terraform-plan
```

## 🚀 Production Considerations

### Security
- Replace default credentials in production
- Use proper AWS IAM roles and policies
- Implement secret rotation
- Enable encryption at rest and in transit

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

## 📚 Additional Resources

- [AWS Secrets Manager Documentation](https://docs.aws.amazon.com/secretsmanager/)
- [LocalStack Documentation](https://docs.localstack.cloud/)
- [Terraform AWS Provider](https://registry.terraform.io/providers/hashicorp/aws/latest/docs)
- [.NET Configuration Documentation](https://docs.microsoft.com/en-us/dotnet/core/extensions/configuration)

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Run tests: `make test-app`
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🆘 Support

For support and questions:
- Check the troubleshooting section above
- Run `make health-check` for diagnostics
- Review the logs using the debugging commands
- Open an issue in the repository

---

**Happy coding! 🎉**
