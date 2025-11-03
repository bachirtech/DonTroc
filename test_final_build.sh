#!/bin/bash
echo "🔧 Test de build DonTroc avec correction AndroidX Activity"
echo "============================================================"

cd /Users/aa1/RiderProjects/DonTroc

echo ""
echo "📦 Étape 1 : Nettoyage complet..."
rm -rf DonTroc/obj DonTroc/bin
echo "✅ Nettoyage terminé"

echo ""
echo "📦 Étape 2 : Restauration des packages..."
dotnet restore DonTroc.sln --verbosity quiet
if [ $? -eq 0 ]; then
    echo "✅ Restauration réussie"
else
    echo "❌ Restauration échouée"
    exit 1
fi

echo ""
echo "🔨 Étape 3 : Compilation C# Debug..."
dotnet build DonTroc/DonTroc.csproj -f net8.0-android -c Debug --verbosity quiet --no-restore
BUILD_RESULT=$?

echo ""
if [ $BUILD_RESULT -eq 0 ]; then
    echo "✅✅✅ BUILD RÉUSSI ! ✅✅✅"
    echo ""
    echo "Le projet compile sans erreur !"
else
    echo "❌ BUILD ÉCHOUÉ"
    echo ""
    echo "Consultez les logs pour plus de détails."
fi

exit $BUILD_RESULT

