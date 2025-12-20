#!/bin/bash
# Script de test de compilation DonTroc .NET 9

echo "=== Test de compilation DonTroc ==="
echo "Date: $(date)"
echo ""

cd /Users/aa1/RiderProjects/DonTroc/DonTroc

echo "1. Version de .NET:"
/usr/local/share/dotnet/dotnet --version

echo ""
echo "2. Nettoyage..."
rm -rf bin obj

echo ""
echo "3. Restauration des packages..."
/usr/local/share/dotnet/dotnet restore --verbosity minimal
RESTORE_EXIT=$?
echo "Code de sortie restore: $RESTORE_EXIT"

if [ $RESTORE_EXIT -eq 0 ]; then
    echo ""
    echo "4. Compilation en Release..."
    /usr/local/share/dotnet/dotnet build -c Release -f net9.0-android --no-restore --verbosity minimal
    BUILD_EXIT=$?
    echo "Code de sortie build: $BUILD_EXIT"
    
    if [ $BUILD_EXIT -eq 0 ]; then
        echo ""
        echo "✅ COMPILATION RÉUSSIE!"
    else
        echo ""
        echo "❌ ERREUR DE COMPILATION"
    fi
else
    echo ""
    echo "❌ ERREUR DE RESTAURATION"
fi

