# DevContainer Setup for .NET Secrets Manager

This devcontainer provides a complete development environment for the .NET Secrets Manager application with AWS LocalStack integration.

## What's Included

### Development Tools
- **.NET 8 SDK** - For building and running the .NET Core application
- **Docker CLI** - For container operations and LocalStack management
- **Terraform** - For infrastructure as code management
- **AWS CLI v2** - For interacting with LocalStack AWS services
- **Make** - For build automation
- **Node.js** - For additional tooling and package management
- **Git** - Version control with pre-configured settings

### VS Code Extensions
- **C# Dev Kit** - Complete C# development experience
- **Terraform** - Syntax highlighting and IntelliSense for Terraform files
- **Docker** - Docker container management
- **AWS Toolkit** - AWS service integration
- **Makefile Tools** - Makefile syntax support
- **YAML** - YAML file support for configuration files

### Services
- **LocalStack** - Local AWS services simulation (Secrets Manager, STS, IAM)
- **Development Container** - Isolated development environment

## Quick Start

1. **Open in VS Code**: Open this project in VS Code and select "Reopen in Container" when prompted
2. **Wait for Setup**: The container will build and run the setup script automatically
3. **Verify Setup**: Check that LocalStack is running with `make status`
4. **Start Development**: Follow the steps in `steps.txt` to build the application

## Port Forwarding

- **4566** - LocalStack (AWS services endpoint)
- **5000** - .NET HTTP (for web applications)
- **5001** - .NET HTTPS (for web applications)

## Environment Variables

The container automatically configures:
- `AWS_ACCESS_KEY_ID=test`
- `AWS_SECRET_ACCESS_KEY=test`
- `AWS_DEFAULT_REGION=us-east-1`
- `AWS_ENDPOINT_URL=http://localstack:4566`

## Useful Commands

### LocalStack Management
```bash
make start          # Start LocalStack
make stop           # Stop LocalStack
make status         # Check LocalStack status
```

### AWS CLI with LocalStack
```bash
# List secrets
aws --endpoint-url=http://localstack:4566 secretsmanager list-secrets

# Get secret value
aws --endpoint-url=http://localstack:4566 secretsmanager get-secret-value --secret-id dotnet-app-secrets

# Check LocalStack health
curl http://localstack:4566/health
```

### .NET Development
```bash
dotnet build        # Build the application
dotnet run          # Run the application
dotnet test         # Run tests
```

### Terraform
```bash
terraform -chdir=terraform init     # Initialize Terraform
terraform -chdir=terraform plan     # Plan infrastructure changes
terraform -chdir=terraform apply    # Apply infrastructure changes
```

## Troubleshooting

### LocalStack Not Ready
If LocalStack isn't responding, try:
```bash
docker-compose -f .devcontainer/docker-compose.yml restart localstack
```

### Permission Issues
If you encounter Docker permission issues:
```bash
sudo usermod -aG docker vscode
```

### AWS CLI Configuration
To reconfigure AWS CLI for LocalStack:
```bash
aws configure set aws_access_key_id test
aws configure set aws_secret_access_key test
aws configure set default.region us-east-1
```

## File Structure

```
.devcontainer/
├── devcontainer.json     # Main devcontainer configuration
├── docker-compose.yml    # Multi-service setup with LocalStack
├── Dockerfile           # Custom development image
├── setup.sh            # Post-creation setup script
└── README.md           # This file
```

## Next Steps

After the devcontainer is ready:

1. Follow the steps in `steps.txt` to implement the .NET application
2. Use `make setup-infrastructure` to deploy secrets to LocalStack
3. Build and test the .NET Secrets Manager application
4. Develop and debug using the integrated VS Code tools

The devcontainer provides everything needed to work on this project without any local setup requirements.
