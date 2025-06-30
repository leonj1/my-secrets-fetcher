# AWS Environment Variables Support - Implementation Checklist

## Overview
This checklist tracks the implementation of AWS environment variable support for the Universal Secrets Manager, allowing credentials to be provided via environment variables in addition to appsettings.json.

## Phase 1: Configuration Infrastructure
- [x] Update `Program.cs` to add environment variable configuration provider
  - [x] Add `.AddEnvironmentVariables()` to configuration builder
  - [ ] Test that environment variables override appsettings.json values
- [x] Support .NET-style nested environment variables
  - [x] `AWS__Region` maps to `AWS:Region`
  - [x] `AWS__AccessKey` maps to `AWS:AccessKey`
  - [x] `AWS__SecretKey` maps to `AWS:SecretKey`
  - [x] `AWS__ServiceURL` maps to `AWS:ServiceURL`
  - [x] `SecretsManager__SecretName` maps to `SecretsManager:SecretName`

## Phase 2: AWS SDK Standard Environment Variables
- [x] Add support for standard AWS environment variables
  - [x] Map `AWS_ACCESS_KEY_ID` to `AWS:AccessKey`
  - [x] Map `AWS_SECRET_ACCESS_KEY` to `AWS:SecretKey`
  - [x] Map `AWS_DEFAULT_REGION` to `AWS:Region`
  - [x] Map `AWS_REGION` as alternative to `AWS_DEFAULT_REGION`
- [x] Implement precedence order:
  1. .NET-style variables (AWS__*)
  2. AWS SDK standard variables (AWS_*)
  3. appsettings.json values

## Phase 3: Configuration Model Updates
- [x] Modify `AwsConfig` class
  - [x] Make `AccessKey` property nullable (`string?`)
  - [x] Make `SecretKey` property nullable (`string?`)
  - [x] Add validation method to ensure credentials are available
- [x] Update `SecretsManagerConfig` if needed
  - [x] Ensure all properties work with environment variables

## Phase 4: Authentication Logic Updates
- [x] Update `SecretsService` constructor
  - [x] Add logic to check if explicit credentials are provided
  - [x] Fall back to AWS SDK credential chain if not
  - [x] Support credential-less initialization for IAM roles
- [x] Modify AWS client initialization
  - [x] Use `AmazonSecretsManagerClient()` constructor when no explicit credentials
  - [x] Use existing constructor when credentials are provided
  - [x] Handle ServiceURL for LocalStack compatibility

## Phase 5: Testing
- [ ] Update existing unit tests
  - [ ] Make tests work with nullable credentials
  - [ ] Add mock environment variables where needed
- [ ] Add new test cases
  - [ ] Test environment variable override of appsettings.json
  - [ ] Test .NET-style environment variables
  - [ ] Test AWS SDK standard environment variables
  - [ ] Test precedence order
  - [ ] Test missing credentials scenario
- [ ] LocalStack integration tests
  - [ ] Ensure LocalStack continues to work with test credentials
  - [ ] Test with environment variable-based credentials

## Phase 6: Error Handling
- [ ] Add meaningful error messages
  - [ ] When no credentials are found from any source
  - [ ] When partial credentials are provided (e.g., key but no secret)
- [ ] Add logging for credential source
  - [ ] Log which credential source is being used (for debugging)
  - [ ] Ensure sensitive data is not logged

## Phase 7: Documentation
- [x] Update README.md
  - [x] Add section on environment variable configuration
  - [x] Provide examples for both .NET and AWS standard formats
  - [x] Add Docker and Kubernetes examples
- [x] Update CLAUDE.md
  - [x] Document new configuration options
  - [x] Add notes about credential precedence
- [x] Create security best practices section
  - [x] Recommend environment variables over config files for production
  - [x] Document IAM role usage for EC2/ECS/EKS
  - [x] Add warning about not committing credentials

## Phase 8: Examples and Integration
- [ ] Update Python integration examples
  - [ ] Show how to set environment variables before running
  - [ ] Demonstrate Docker usage with env vars
- [ ] Add CI/CD examples
  - [ ] GitHub Actions with secrets
  - [ ] GitLab CI with variables
  - [ ] Jenkins with credentials plugin

## Testing Scenarios

### Local Development
- [ ] Test with LocalStack using environment variables
- [ ] Test with LocalStack using appsettings.json (backward compatibility)

### Docker
- [ ] Test with environment variables passed to container
- [ ] Test with .env file and docker-compose

### Cloud Environments
- [ ] Document testing with real AWS credentials (security note)
- [ ] Document IAM role testing on EC2/ECS

## Success Criteria
- [ ] Application works with credentials from environment variables
- [ ] Application works with credentials from appsettings.json (backward compatible)
- [ ] Clear precedence order is established and documented
- [ ] All existing tests pass
- [ ] New tests cover environment variable scenarios
- [ ] Documentation is comprehensive and includes examples
- [ ] Security best practices are documented

## Notes
- Maintain backward compatibility throughout implementation
- Consider adding a configuration source logger for debugging
- Ensure LocalStack compatibility is preserved