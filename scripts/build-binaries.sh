#!/bin/bash

# Build script for creating single-file binaries for multiple platforms
# This script builds self-contained, single-file executables for Windows, Linux, and macOS

set -e

echo "🚀 Building single-file binaries for multiple platforms..."
echo "=================================================="

# Create dist directory if it doesn't exist
mkdir -p dist

# Function to build for a specific platform
build_platform() {
    local platform=$1
    local runtime=$2
    local output_dir=$3
    local binary_name=$4
    
    echo "📦 Building for $platform ($runtime)..."
    cd src/SecretsManager
    
    dotnet publish \
        -c Release \
        -r $runtime \
        --self-contained true \
        -p:PublishSingleFile=true \
        -p:PublishTrimmed=true \
        -p:TrimMode=link \
        -p:PublishReadyToRun=true \
        -p:IncludeNativeLibrariesForSelfExtract=true \
        -o "../../dist/$output_dir"
    
    cd ../..
    
    # Show binary size
    if [ -f "dist/$output_dir/$binary_name" ]; then
        size=$(du -h "dist/$output_dir/$binary_name" | cut -f1)
        echo "✅ $platform binary created: $size"
    else
        echo "❌ Failed to create $platform binary"
        exit 1
    fi
}

# Build for all platforms
echo "Building Windows binary..."
build_platform "Windows" "win-x64" "windows" "SecretsManager.exe"

echo ""
echo "Building Linux binary..."
build_platform "Linux" "linux-x64" "linux" "SecretsManager"

echo ""
echo "Building macOS Intel binary..."
build_platform "macOS Intel" "osx-x64" "macos-intel" "SecretsManager"

echo ""
echo "Building macOS ARM binary..."
build_platform "macOS ARM" "osx-arm64" "macos-arm" "SecretsManager"

echo ""
echo "🎉 All binaries built successfully!"
echo "=================================================="
echo "Binaries are located in:"
echo "  📁 Windows (x64):     dist/windows/SecretsManager.exe"
echo "  📁 Linux (x64):       dist/linux/SecretsManager"
echo "  📁 macOS Intel (x64): dist/macos-intel/SecretsManager"
echo "  📁 macOS ARM (ARM64): dist/macos-arm/SecretsManager"
echo ""
echo "Binary sizes:"
ls -lh dist/*/SecretsManager* | awk '{print "  " $9 ": " $5}'
echo ""
echo "To run the binaries:"
echo "  Windows: ./dist/windows/SecretsManager.exe"
echo "  Linux:   ./dist/linux/SecretsManager"
echo "  macOS:   ./dist/macos-intel/SecretsManager (or macos-arm)"
