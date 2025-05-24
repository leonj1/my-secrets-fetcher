#!/bin/bash

set -e

echo "=== LocalStack Health Check ==="
if command -v jq &> /dev/null; then
    curl -s http://localhost:4566/health | jq . 2>/dev/null || curl -s http://localhost:4566/health
else
    echo "jq not found, showing raw response:"
    curl -s http://localhost:4566/health
fi

echo -e "\n=== LocalStack Service Status ==="
curl -s http://localhost:4566/_localstack/health 2>/dev/null || echo "LocalStack health endpoint not available"

echo -e "\n=== Testing Direct Secret Retrieval (using curl) ==="
echo "Attempting to retrieve secret using direct HTTP call..."
curl -X POST "http://localhost:4566/" \
    -H "Content-Type: application/x-amz-json-1.1" \
    -H "X-Amz-Target: secretsmanager.GetSecretValue" \
    -H "Authorization: AWS4-HMAC-SHA256 Credential=test/20250524/us-east-1/secretsmanager/aws4_request, SignedHeaders=host;x-amz-date, Signature=test" \
    -d '{"SecretId": "dotnet-app-secrets"}' 2>/dev/null || echo "Direct secret retrieval failed"

echo -e "\n=== AWS CLI Test (if available) ==="
if command -v aws &> /dev/null; then
    echo "AWS CLI found, testing..."
    export AWS_ACCESS_KEY_ID=test
    export AWS_SECRET_ACCESS_KEY=test
    export AWS_DEFAULT_REGION=us-east-1
    
    aws --endpoint-url=http://localhost:4566 secretsmanager list-secrets --region us-east-1 2>/dev/null || echo "AWS CLI test failed"
    
    echo "Attempting secret retrieval with AWS CLI..."
    aws --endpoint-url=http://localhost:4566 secretsmanager get-secret-value \
        --secret-id dotnet-app-secrets --region us-east-1 2>/dev/null || echo "AWS CLI secret retrieval failed"
else
    echo "AWS CLI not available, skipping AWS CLI tests"
fi

echo -e "\n=== Docker Container Status ==="
docker ps --filter "name=localstack" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" 2>/dev/null || echo "Docker not available or no LocalStack container running"

echo -e "\n=== Health Check Summary ==="
if curl -s http://localhost:4566/health > /dev/null 2>&1; then
    echo "✅ LocalStack is responding"
else
    echo "❌ LocalStack is not responding"
fi

if curl -s http://localhost:4566/_localstack/health > /dev/null 2>&1; then
    echo "✅ LocalStack health endpoint is accessible"
else
    echo "❌ LocalStack health endpoint is not accessible"
fi

echo "Health check completed."
