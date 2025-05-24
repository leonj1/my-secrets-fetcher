.PHONY: start stop restart status terraform-build terraform-init terraform-plan terraform-apply terraform-destroy setup-infrastructure build-app run-app test-app clean localstack-status dotnet-build dotnet-run dotnet-clean full-setup health-check cleanup

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
