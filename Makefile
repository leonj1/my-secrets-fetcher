.PHONY: start stop restart status terraform-build terraform-init terraform-plan terraform-apply terraform-destroy setup-infrastructure build-app run-app test-app clean localstack-status dotnet-build dotnet-run dotnet-clean build-windows build-linux build-macos build-macos-arm build-all clean-dist full-setup health-check cleanup

start:
	docker-compose up -d
	@echo "LocalStack started"

stop:
	docker-compose down
	@echo "LocalStack stopped"

restart: stop start
	@echo "LocalStack restarted"

status:
	docker-compose ps

terraform-build:
	docker build -f docker/Dockerfile.terraform -t terraform-local .

terraform-init: terraform-build
	docker run --rm --network host -v $(PWD)/terraform:/terraform terraform-local init

terraform-plan: terraform-init
	docker run --rm --network host -v $(PWD)/terraform:/terraform terraform-local plan

terraform-apply: terraform-init
	docker run --rm --network host -v $(PWD)/terraform:/terraform terraform-local apply -auto-approve

terraform-destroy:
	docker run --rm --network host -v $(PWD)/terraform:/terraform terraform-local destroy -auto-approve

setup-infrastructure: start terraform-apply
	@echo "Infrastructure setup complete"

# Development and Testing
build-app:
	cd src/SecretsManager && dotnet build

run-app: build-app
	cd src/SecretsManager && dotnet run

test-app: setup-infrastructure build-app
	@echo "Testing application..."
	cd src/SecretsManager && dotnet run
	@echo "Application test complete"

# .NET specific targets (as specified in Step 22)
dotnet-build:
	cd src/SecretsManager && dotnet build

dotnet-run: dotnet-build
	cd src/SecretsManager && dotnet run

dotnet-clean:
	cd src/SecretsManager && dotnet clean

# Single-file binary builds for multiple platforms
build-windows:
	@echo "Building single-file binary for Windows..."
	cd src/SecretsManager && dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ../../dist/windows

build-linux:
	@echo "Building single-file binary for Linux..."
	cd src/SecretsManager && dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o ../../dist/linux

build-macos:
	@echo "Building single-file binary for macOS (Intel)..."
	cd src/SecretsManager && dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true -o ../../dist/macos-intel

build-macos-arm:
	@echo "Building single-file binary for macOS (Apple Silicon)..."
	cd src/SecretsManager && dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -o ../../dist/macos-arm

build-all: build-windows build-linux build-macos build-macos-arm
	@echo "All platform binaries built successfully!"
	@echo "Binaries are located in:"
	@echo "  - Windows: dist/windows/SecretsManager.exe"
	@echo "  - Linux: dist/linux/SecretsManager"
	@echo "  - macOS Intel: dist/macos-intel/SecretsManager"
	@echo "  - macOS ARM: dist/macos-arm/SecretsManager"

clean-dist:
	@echo "Cleaning distribution directory..."
	rm -rf dist/

full-setup: setup-infrastructure dotnet-build
	@echo "Full setup complete"

# Enhanced test target with validation (as specified in Step 22)
test-app-enhanced: full-setup dotnet-run
	@echo "Testing application..."
	@echo "Checking environment variables:"
	@env | grep -E "(DATABASE_URL|API_KEY|JWT_SECRET|REDIS_URL)" || echo "No secrets found in environment"
	@echo "Checking .env file:"
	@cat src/SecretsManager/.env 2>/dev/null || echo ".env file not found"

# Utilities
localstack-status:
	@echo "=== LocalStack Health Check ==="
	curl -s http://localhost:4566/_localstack/health | jq . || echo "LocalStack not responding or jq not installed"

health-check:
	@echo "Running comprehensive health check..."
	./scripts/health-check.sh

test-secrets:
	@echo "=== Testing Secret Retrieval ==="
	curl -X POST "http://localhost:4566/" \
		-H "Content-Type: application/x-amz-json-1.1" \
		-H "X-Amz-Target: secretsmanager.GetSecretValue" \
		-H "Authorization: AWS4-HMAC-SHA256 Credential=test/20250524/us-east-1/secretsmanager/aws4_request, SignedHeaders=host;x-amz-date, Signature=test" \
		-d '{"SecretId": "dotnet-app-secrets"}' | jq . || echo "Failed to retrieve secrets"

# Clean up
clean:
	docker-compose down -v
	docker system prune -f
	rm -rf terraform/.terraform
	rm -f terraform/terraform.tfstate*
	cd src/SecretsManager && dotnet clean

cleanup:
	@echo "Running comprehensive cleanup..."
	./scripts/cleanup.sh
