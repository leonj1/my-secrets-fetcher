# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Universal Secrets Manager - A .NET 7.0 application that integrates with AWS Secrets Manager to fetch secrets and make them available to applications through environment variables or .env files. Key features include DevContainer integration with automatic ARN detection and multi-platform support.

## Essential Commands

### Build & Run
```bash
# Build the application
make dotnet-build
# or directly:
cd src/SecretsManager && dotnet build

# Run the application
make dotnet-run
# or directly:
cd src/SecretsManager && dotnet run

# Build single-file executables for all platforms
make build-all
# Individual platforms:
make build-linux
make build-windows
make build-macos
make build-macos-arm
```

### Testing
```bash
# Run unit tests
make test
# or directly:
cd src/SecretsManager.Tests && dotnet test

# Run end-to-end test with LocalStack
make test-app

# Run comprehensive health check
make health-check
```

### Infrastructure Management
```bash
# Start LocalStack and provision secrets
make setup-infrastructure

# Individual operations:
make start              # Start LocalStack
make stop               # Stop LocalStack
make terraform-apply    # Provision secrets in LocalStack
make localstack-status  # Check LocalStack health
```

### Development Workflow
```bash
# Full setup (infrastructure + build)
make full-setup

# Clean everything
make clean
make clean-dist
```

## Architecture & Code Structure

### Service Architecture
The application follows a service-oriented architecture with interface-based design:

- **ISecretsService** - Handles AWS Secrets Manager operations
- **IDevContainerService** - Manages DevContainer configuration parsing and ARN extraction
- **IEnvFileService** - Handles .env.example parsing and .env file creation
- **SecretsRepository** - Data access layer for secrets
- **Configuration classes** - Strongly typed configuration models (now with nullable credential support)

### DevContainer Integration Pattern
The application automatically detects AWS Secrets Manager ARNs in devcontainer.json:
- Searches for ARN patterns: `${arn:aws:secretsmanager:region:account:secret:name}`
- Supports containerEnv, build.args, and remoteEnv sections
- Fetches secrets at runtime and sets environment variables

### .env.example Integration Pattern
The application also supports ARN detection in .env.example files:
- Same ARN pattern as DevContainer: `${arn:aws:secretsmanager:region:account:secret:name}`
- Parses standard .env format with comments and quoted values
- Creates/updates .env file with fetched secret values
- Preserves non-ARN values from .env.example

### Key Implementation Details

1. **Secrets Fetching Flow**:
   - Parse devcontainer.json for ARNs (if present)
   - Parse .env.example for ARNs (if present)
   - Both files are processed independently
   - Extract secret IDs from ARNs
   - Fetch secrets from AWS Secrets Manager
   - Apply as environment variables and/or write to .env file based on OutputMode

2. **Configuration Hierarchy**:
   - Environment variable overrides (highest priority)
     - .NET-style: `AWS__Region`, `AWS__AccessKey`, `AWS__SecretKey`, `SecretsManager__SecretName`
     - AWS SDK standard: `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, `AWS_DEFAULT_REGION`
   - appsettings.json base configuration
   - AWS SDK credential chain (IAM roles, profiles) when no explicit credentials provided

3. **Output Modes** (configurable via OutputMode setting):
   - EnvironmentVariables: Sets process environment variables
   - EnvFile: Writes .env file to specified path
   - Both: Applies both methods (default)

### AWS Authentication Methods
The application supports multiple authentication methods:

1. **Environment Variables** (recommended for containers/CI)
   - Standard AWS: `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`
   - .NET configuration: `AWS__AccessKey`, `AWS__SecretKey`
   - Automatic region detection from `AWS_DEFAULT_REGION` or `AWS_REGION`

2. **Configuration File** (`appsettings.json`)
   - Used when environment variables are not present
   - Good for local development with LocalStack

3. **IAM Roles** (recommended for production)
   - Automatically used when no explicit credentials provided
   - Supports EC2 instance profiles, ECS task roles, Lambda execution roles
   - Most secure option - no credentials to manage

### Testing Considerations
- 34 unit tests including:
  - 18 tests for DevContainer functionality
  - 10 tests for AWS configuration and validation
  - 6 tests for EnvFileService
- Integration testing via LocalStack
- ARN validation and extraction tests
- .env.example parsing and .env file creation tests
- Error handling for malformed configurations
- Credential precedence testing

## LocalStack Configuration

The project uses LocalStack to emulate AWS services locally:
- Services: Secrets Manager, STS, IAM
- Port: 4566
- Credentials: test/test (for local development only)
- Terraform provisions secrets defined in terraform/variables.tf

## CI/CD Pipeline

- **PR Tests** (`.github/workflows/pr-tests.yml`): Runs on pull requests, executes unit tests
- **Release Builds** (`.github/workflows/release.yml`): Creates multi-platform binaries on push to main/master

## Security Considerations

- Secrets are never stored in configuration files
- ARNs in devcontainer.json are replaced with actual values at runtime
- Support for AWS IAM/STS authentication in production
- Environment variable isolation prevents secret leakage

## Common Development Tasks

When implementing new features:
1. Check existing service interfaces before creating new ones
2. Follow the established pattern of interface + implementation
3. Add unit tests for new functionality
4. Test with LocalStack integration before production deployment
5. Update README.md for any new Python integration examples

## Python Integration Support

The application provides first-class Python support:
- Standalone binaries require no .NET runtime
- Compatible with python-dotenv for .env file reading
- Supports standard os.environ access patterns
- Can be integrated into Docker containers and CI/CD pipelines