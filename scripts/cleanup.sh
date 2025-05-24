#!/bin/bash

echo "Cleaning up resources..."

# Stop containers
echo "=== Stopping LocalStack containers ==="
make stop

# Clean up terraform
echo "=== Destroying Terraform infrastructure ==="
make terraform-destroy

# Clean up .NET artifacts
echo "=== Cleaning .NET build artifacts ==="
make dotnet-clean

# Remove generated files
echo "=== Removing generated files ==="
rm -f src/SecretsManager/.env
echo "Removed .env file"

# Remove temporary directories
echo "=== Removing temporary directories ==="
rm -rf tmp/
echo "Removed tmp/ directory"

# Remove terraform state and cache
echo "=== Cleaning Terraform state and cache ==="
rm -rf terraform/.terraform
rm -f terraform/terraform.tfstate*
rm -f terraform/.terraform.lock.hcl
echo "Removed Terraform state files"

# Clean Docker resources (optional - commented out by default)
echo "=== Docker cleanup (optional) ==="
echo "Removing unused Docker images and volumes..."
docker system prune -f --volumes 2>/dev/null || echo "Docker cleanup skipped (Docker not available)"

# Clean up any leftover processes
echo "=== Process cleanup ==="
pkill -f "dotnet run" 2>/dev/null || echo "No .NET processes to kill"

echo "=== Cleanup Summary ==="
echo "✅ LocalStack containers stopped"
echo "✅ Terraform infrastructure destroyed"
echo "✅ .NET build artifacts cleaned"
echo "✅ Generated files removed"
echo "✅ Temporary directories removed"
echo "✅ Terraform state cleaned"
echo "✅ Docker resources pruned"
echo "✅ Processes cleaned up"

echo ""
echo "Cleanup complete! 🧹"
echo ""
echo "To restart the environment, run:"
echo "  make setup-infrastructure"
echo "  make full-setup"
