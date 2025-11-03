#!/bin/bash
set -e

cd /Users/aa1/RiderProjects/DonTroc

echo "=== Début du build $(date) ===" > build.log 2>&1

echo "=== Nettoyage ===" >> build.log 2>&1
rm -rf DonTroc/obj DonTroc/bin >> build.log 2>&1

echo "=== Restauration ===" >> build.log 2>&1
dotnet restore DonTroc/DonTroc.csproj >> build.log 2>&1

echo "=== Compilation Debug Android ===" >> build.log 2>&1
dotnet build DonTroc/DonTroc.csproj -f net8.0-android -c Debug >> build.log 2>&1

echo "=== Build terminé $(date) ===" >> build.log 2>&1
echo "Build réussi!"

