#!/bin/bash
# ==============================================================================
# CICS Modernization Workbench - Devin IDE Environment Setup Script
# ==============================================================================
set -e

echo "=== [Devin IDE] Initializing CICS Modernization Environment ==="

# 1. Verify / Install .NET 9 SDK
if ! command -v dotnet &> /dev/null; then
    echo "[-] .NET SDK not detected. Installing .NET 9.0 SDK..."
    sudo apt-get update && sudo apt-get install -y wget ca-certificates
    wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
    sudo dpkg -i packages-microsoft-prod.deb
    rm packages-microsoft-prod.deb
    sudo apt-get update
    sudo apt-get install -y dotnet-sdk-9.0
else
    echo "[+] .NET SDK found: $(dotnet --version)"
fi

# 2. Node.js & NPM verification
if ! command -v node &> /dev/null; then
    echo "[-] Node.js not detected. Installing Node.js LTS..."
    curl -fsSL https://deb.nodesource.com/setup_20.x | sudo -E bash -
    sudo apt-get install -y nodejs
else
    echo "[+] Node.js found: $(node --version)"
fi

# 3. Setup database adapters dependencies
if [ -d "db-adapters" ]; then
    echo "[+] Installing db-adapters npm dependencies..."
    (cd db-adapters && npm install --silent)
fi

# 4. Setup Playwright & Test Runner dependencies
echo "[+] Ensuring Playwright dependencies for Phase 8 automated UI testing..."
if command -v npx &> /dev/null; then
    npx -y playwright install --with-deps chromium 2>/dev/null || echo "[!] Notice: Playwright browser setup skipped or completed"
fi

# 5. Set Environment Variables
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
export ASPNETCORE_ENVIRONMENT=Development
export PATH="$PATH:$HOME/.dotnet:$HOME/.dotnet/tools"

echo "=== [Devin IDE] Environment Setup Complete. Ready for Modernization Pipeline. ==="
