#!/bin/bash
cd /Users/aa1/RiderProjects/DonTroc

echo "=== Nettoyage ==="
rm -rf DonTroc/obj DonTroc/bin

echo "=== Restauration ==="
dotnet restore DonTroc/DonTroc.csproj

echo "=== Construction Debug ==="
dotnet build DonTroc/DonTroc.csproj -f net8.0-android -c Debug -v n
