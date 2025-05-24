#!/bin/bash

echo "🚀 Setting up .NET Secrets Manager development environment..."

# Wait for LocalStack to be ready
echo "⏳ Waiting for LocalStack to be ready..."
until curl -s http://localstack:4566/health > /dev/null 2>&1; do
    echo "   LocalStack not ready yet, waiting..."
    sleep 2
done
echo "✅ LocalStack is ready!"

# Configure AWS CLI for LocalStack
echo "🔧 Configuring AWS CLI for LocalStack..."
aws configure set aws_access_key_id test
aws configure set aws_secret_access_key test
aws configure set default.region us-east-1
aws configure set default.output json

# Test LocalStack connection
echo "🧪 Testing LocalStack connection..."
aws --endpoint-url=http://localstack:4566 sts get-caller-identity

# Create necessary directories
echo "📁 Creating project directories..."
mkdir -p src terraform docker scripts tmp/localstack

# Set up git configuration if not already set
if [ -z "$(git config --global user.name)" ]; then
    echo "⚙️  Setting up git configuration..."
    git config --global user.name "Developer"
    git config --global user.email "developer@example.com"
    git config --global init.defaultBranch main
fi

# Install additional .NET templates
echo "📦 Installing .NET templates..."
dotnet new install Microsoft.AspNetCore.SPA.ProjectTemplates
dotnet new install Microsoft.DotNet.Web.ProjectTemplates

echo "✨ Development environment setup complete!"
echo ""
echo "🎯 Next steps:"
echo "   1. Run 'make setup-infrastructure' to deploy secrets to LocalStack"
echo "   2. Run 'make dotnet-build' to build the .NET application"
echo "   3. Run 'make test-app' to test the complete workflow"
echo ""
echo "🔗 Useful commands:"
echo "   - make start          # Start LocalStack"
echo "   - make stop           # Stop LocalStack"
echo "   - aws --endpoint-url=http://localstack:4566 secretsmanager list-secrets"
echo "   - terraform -chdir=terraform plan"
echo ""
