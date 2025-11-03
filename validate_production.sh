#!/bin/zsh

# Script de validation de production pour DonTroc
# Ce script vérifie que l'application est prête pour la production

echo "🚀 Validation de production DonTroc"
echo "===================================="

# Configuration
PROJECT_ROOT="/Users/aa1/RiderProjects/DonTroc"
ERRORS=0

# Fonction pour afficher les erreurs
log_error() {
    echo "❌ ERREUR: $1"
    ((ERRORS++))
}

# Fonction pour afficher les succès
log_success() {
    echo "✅ $1"
}

# Fonction pour afficher les avertissements
log_warning() {
    echo "⚠️  AVERTISSEMENT: $1"
}

echo "\n📁 Vérification de la structure du projet..."

# Vérifier la présence des fichiers essentiels
if [[ -f "$PROJECT_ROOT/DonTroc/DonTroc.csproj" ]]; then
    log_success "Fichier projet trouvé"
else
    log_error "Fichier projet manquant"
fi

if [[ -f "$PROJECT_ROOT/DonTroc/Services/ConfigurationService.cs" ]]; then
    log_success "ConfigurationService présent"
else
    log_error "ConfigurationService manquant"
fi

echo "\n🔍 Vérification des fichiers indésirables..."

# Rechercher les fichiers .DS_Store
DS_STORE_COUNT=$(find "$PROJECT_ROOT" -name ".DS_Store" 2>/dev/null | wc -l)
if [[ $DS_STORE_COUNT -eq 0 ]]; then
    log_success "Aucun fichier .DS_Store trouvé"
else
    log_warning "$DS_STORE_COUNT fichier(s) .DS_Store trouvé(s) - ils seront supprimés"
    find "$PROJECT_ROOT" -name ".DS_Store" -delete
fi

# Rechercher les fichiers temporaires
TEMP_FILES=$(find "$PROJECT_ROOT" -name "*.tmp" -o -name "*~" -o -name "*.bak" 2>/dev/null | wc -l)
if [[ $TEMP_FILES -eq 0 ]]; then
    log_success "Aucun fichier temporaire trouvé"
else
    log_warning "$TEMP_FILES fichier(s) temporaire(s) trouvé(s)"
fi

echo "\n📦 Vérification des packages NuGet..."

# Vérifier la configuration du projet
if grep -q "AndroidEnableProfiledAot" "$PROJECT_ROOT/DonTroc/DonTroc.csproj"; then
    log_success "AOT Android configuré"
else
    log_warning "AOT Android non configuré"
fi

if grep -q "EnableLLVMCompiler" "$PROJECT_ROOT/DonTroc/DonTroc.csproj"; then
    log_success "Compilateur LLVM iOS configuré"
else
    log_warning "Compilateur LLVM iOS non configuré"
fi

echo "\n🔒 Vérification de la sécurité..."

# Vérifier qu'il n'y a pas de clés d'API en dur
API_KEYS=$(grep -r "AIzaSy" "$PROJECT_ROOT/DonTroc" --exclude-dir=bin --exclude-dir=obj 2>/dev/null | wc -l)
if [[ $API_KEYS -gt 1 ]]; then
    log_warning "Clés d'API détectées dans le code - vérifiez qu'elles sont sécurisées"
else
    log_success "Aucune clé d'API supplémentaire détectée"
fi

echo "\n⚡ Vérification des optimisations..."

# Vérifier la configuration Release
if grep -q "AndroidPackageFormat.*aab" "$PROJECT_ROOT/DonTroc/DonTroc.csproj"; then
    log_success "Format AAB configuré pour Android"
else
    log_warning "Format AAB non configuré - recommandé pour Google Play"
fi

if grep -q "PublishTrimmed.*true" "$PROJECT_ROOT/DonTroc/DonTroc.csproj"; then
    log_success "Trimming activé"
else
    log_warning "Trimming non activé"
fi

echo "\n🧹 Nettoyage des artefacts de build..."

# Nettoyer les dossiers bin et obj
if [[ -d "$PROJECT_ROOT/DonTroc/bin" ]]; then
    rm -rf "$PROJECT_ROOT/DonTroc/bin"
    log_success "Dossier bin nettoyé"
fi

if [[ -d "$PROJECT_ROOT/DonTroc/obj" ]]; then
    rm -rf "$PROJECT_ROOT/DonTroc/obj"
    log_success "Dossier obj nettoyé"
fi

echo "\n📊 Résumé de la validation..."

if [[ $ERRORS -eq 0 ]]; then
    echo "✅ SUCCÈS: L'application est prête pour la production!"
    echo "🎯 Prochaines étapes recommandées:"
    echo "   1. Testez en mode Release"
    echo "   2. Exécutez les tests automatisés"
    echo "   3. Vérifiez les performances"
    echo "   4. Générez les packages de production"
else
    echo "❌ ÉCHEC: $ERRORS erreur(s) détectée(s)"
    echo "🔧 Corrigez les erreurs avant de passer en production"
    exit 1
fi

echo "\n🏁 Validation terminée!"
