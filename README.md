# My Secrets Fetcher

A comprehensive demonstration of secure secrets management using AWS Secrets Manager with LocalStack for local development. This project showcases infrastructure as code, containerized development environments, and secure application configuration.

## 🏗️ Architecture Overview

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Dev Container │    │    LocalStack   │    │  .NET App       │
│                 │    │                 │    │                 │
│ • Terraform     │───▶│ • Secrets Mgr   │◀───│ • AWS SDK       │
│ • AWS CLI       │    │ • S3            │    │ • Config Mgmt   │
│ • .NET SDK      │    │ • IAM           │    │ • DI Container  │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

## 🚀 Features

- **🔐 Secure Secrets Management**: AWS Secrets Manager integration
- **🏠 Local Development**: LocalStack for AWS service emulation
- **📦 Containerized Environment**: Complete dev container setup
- **🏗️ Infrastructure as Code**: Terraform for resource provisioning
- **⚙️ Modern .NET**: Dependency injection, configuration, and logging
- **🛡️ Security Best Practices**: Secret masking and secure handling

## 📋 Prerequisites

- **Docker**: For running LocalStack and dev containers
- **VS Code**: With Dev Containers extension (recommended)
- **Git**: For version control

## 🛠️ Quick Start

### 1. Clone and Setup

```bash
git clone <repository-url>
cd my-secrets-fetcher
```

### 2. Open in Dev Container

```bash
# Using VS Code
code .
# Then: Ctrl+Shift+P → "Dev Containers: Reopen in Container"

# Or using CLI
devcontainer up --workspace-folder .
```

### 3. Deploy Infrastructure

```bash
# Start LocalStack and deploy secrets
make setup-infrastructure
```

### 4. Run the Application

```bash
# Build and run the .NET application
cd src/SecretsManager
dotnet run
```

## 📁 Project Structure

```
my-secrets-fetcher/
├── .devcontainer/              # Development container configuration
│   ├── devcontainer.json       # Dev container settings
│   ├── docker-compose.yml      # LocalStack and services
│   ├── Dockerfile              # Development environment
│   └── setup.sh               # Environment setup script
├── docker/                     # Docker configurations
│   └── Dockerfile.terraform    # Terraform container
├── terraform/                  # Infrastructure as Code
│   ├── main.tf                # Main Terraform configuration
│   ├── variables.tf           # Variable definitions
│   ├── outputs.tf             # Output definitions
│   └── provider.tf            # AWS provider configuration
├── src/SecretsManager/         # .NET Application
│   ├── Models/                # Configuration models
│   │   ├── AwsConfig.cs       # AWS configuration
│   │   ├── SecretsManagerConfig.cs # Secrets Manager config
│   │   └── AppSecrets.cs      # Application secrets model
│   ├── Services/              # Application services
│   │   ├── ISecretsService.cs # Service interface
│   │   └── AwsSecretsService.cs # AWS implementation
│   ├── Program.cs             # Application entry point
│   ├── appsettings.json       # Application configuration
│   └── SecretsManager.csproj  # Project file
├── Makefile                   # Build and deployment commands
└── README.md                  # This file
```

## 🔧 Configuration

### LocalStack Configuration

The project uses LocalStack to emulate AWS services locally:

- **Endpoint**: `http://localhost:4566`
- **Region**: `us-east-1`
- **Credentials**: `test` / `test` (LocalStack defaults)

### Application Configuration

```json
{
  "AWS": {
    "Region": "us-east-1",
    "ServiceURL": "http://localhost:4566",
    "AccessKey": "test",
    "SecretKey": "test"
  },
  "SecretsManager": {
    "SecretName": "dotnet-app-secrets"
  }
}
```

### Secret Structure

The application expects secrets in the following JSON format:

```json
{
  "database_url": "postgresql://localhost:5432/mydb",
  "api_key": "your-api-key-here",
  "jwt_secret": "your-jwt-secret",
  "redis_url": "redis://localhost:6379"
}
```

## 🏗️ Infrastructure Components

### Terraform Resources

- **AWS Secrets Manager Secret**: Stores application configuration
- **Secret Version**: Contains the actual secret data
- **IAM Policies**: (Future enhancement for production)

### LocalStack Services

- **Secrets Manager**: Secret storage and retrieval
- **S3**: Terraform state backend
- **IAM**: Identity and access management

## 🔨 Available Commands

```bash
# Infrastructure Management
make setup-infrastructure    # Deploy infrastructure to LocalStack
make destroy-infrastructure  # Destroy all infrastructure
make terraform-plan         # Show Terraform execution plan

# Development
make build-app              # Build the .NET application
make run-app               # Run the .NET application
make test-secrets          # Test secret retrieval

# Utilities
make localstack-status     # Check LocalStack health
make clean                # Clean build artifacts
```

## 🧪 Testing

### Manual Testing

1. **Infrastructure Deployment**:
   ```bash
   make setup-infrastructure
   ```

2. **Secret Verification**:
   ```bash
   # Check LocalStack health
   curl -s http://localhost:4566/_localstack/health | jq

   # List secrets
   aws --endpoint-url=http://localhost:4566 secretsmanager list-secrets
   ```

3. **Application Testing**:
   ```bash
   cd src/SecretsManager
   dotnet run
   ```

### Expected Output

```
info: SecretsManager.Program[0]
      Starting Secrets Manager application
info: SecretsManager.Services.AwsSecretsService[0]
      Retrieving secret: dotnet-app-secrets
info: SecretsManager.Services.AwsSecretsService[0]
      Successfully retrieved secret from AWS Secrets Manager
Retrieved Application Secrets:
Database URL: postgresql://localhost:5432/mydb
API Key: yo*************re
JWT Secret: yo***********et
Redis URL: redis://localhost:6379
info: SecretsManager.Program[0]
      Application completed successfully
```

## 🔐 Security Features

### Secret Masking

The application implements secure secret display:
- Shows first 2 and last 2 characters
- Masks middle characters with asterisks
- Prevents accidental secret exposure in logs

### Configuration Security

- Secrets stored in AWS Secrets Manager (not in code)
- Environment-specific configuration
- Secure credential handling

## 🏭 Production Considerations

### AWS Configuration

For production deployment:

1. **Update Provider Configuration**:
   ```hcl
   provider "aws" {
     region = var.aws_region
     # Remove LocalStack endpoint
   }
   ```

2. **Configure Real AWS Credentials**:
   ```json
   {
     "AWS": {
       "Region": "us-west-2",
       "AccessKey": "${AWS_ACCESS_KEY_ID}",
       "SecretKey": "${AWS_SECRET_ACCESS_KEY}"
     }
   }
   ```

3. **Add IAM Policies**:
   - Least privilege access
   - Resource-specific permissions
   - Cross-account access if needed

### Security Enhancements

- **Encryption**: Enable KMS encryption for secrets
- **Rotation**: Implement automatic secret rotation
- **Monitoring**: Add CloudWatch logging and monitoring
- **Access Control**: Implement fine-grained IAM policies

## 🐛 Troubleshooting

### Common Issues

1. **LocalStack Not Running**:
   ```bash
   docker-compose -f .devcontainer/docker-compose.yml up -d
   ```

2. **Terraform State Issues**:
   ```bash
   make destroy-infrastructure
   make setup-infrastructure
   ```

3. **Secret Not Found**:
   ```bash
   # Verify secret exists
   aws --endpoint-url=http://localhost:4566 secretsmanager describe-secret --secret-id dotnet-app-secrets
   ```

4. **Connection Issues**:
   - Check LocalStack health: `curl http://localhost:4566/_localstack/health`
   - Verify network connectivity
   - Check firewall settings

### Debug Mode

Enable detailed logging by updating `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Information"
    }
  }
}
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🙏 Acknowledgments

- **LocalStack**: For providing excellent AWS service emulation
- **Terraform**: For infrastructure as code capabilities
- **AWS SDK**: For seamless cloud integration
- **.NET Community**: For excellent tooling and libraries

## 📚 Additional Resources

- [AWS Secrets Manager Documentation](https://docs.aws.amazon.com/secretsmanager/)
- [LocalStack Documentation](https://docs.localstack.cloud/)
- [Terraform AWS Provider](https://registry.terraform.io/providers/hashicorp/aws/latest/docs)
- [.NET Configuration Documentation](https://docs.microsoft.com/en-us/dotnet/core/extensions/configuration)

---

**Built with ❤️ for secure, scalable application development**
