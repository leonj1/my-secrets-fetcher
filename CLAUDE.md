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
- **SecretsRepository** - Data access layer for secrets
- **Configuration classes** - Strongly typed configuration models

### DevContainer Integration Pattern
The application automatically detects AWS Secrets Manager ARNs in devcontainer.json:
- Searches for ARN patterns: `${arn:aws:secretsmanager:region:account:secret:name}`
- Supports containerEnv, build.args, and remoteEnv sections
- Fetches secrets at runtime and sets environment variables

### Key Implementation Details

1. **Secrets Fetching Flow**:
   - Parse devcontainer.json for ARNs (if present)
   - Extract secret IDs from ARNs
   - Fetch secrets from AWS Secrets Manager
   - Apply as environment variables and/or write to .env file

2. **Configuration Hierarchy**:
   - appsettings.json base configuration
   - Environment variable overrides (AWS__, SECRETSMANAGER__)
   - Command-line arguments (future enhancement)

3. **Output Modes**:
   - EnvironmentVariables: Sets process environment variables
   - EnvFile: Writes .env file to specified path
   - Both: Applies both methods (default)

### Testing Considerations
- 18 unit tests covering DevContainer functionality
- Integration testing via LocalStack
- ARN validation and extraction tests
- Error handling for malformed configurations

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