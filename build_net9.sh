#!/bin/bash
set -e

echo "=== Building DonTroc for .NET 9 Android ==="
echo "Date: $(date)"

cd /Users/aa1/RiderProjects/DonTroc/DonTroc

echo "Cleaning..."
rm -rf bin obj

echo "Restoring packages..."
/usr/local/share/dotnet/dotnet restore

echo "Building Release..."
/usr/local/share/dotnet/dotnet build -c Release -f net9.0-android

echo "=== Build Complete ==="

