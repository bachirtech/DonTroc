# DonTroc 🔄

Application mobile de troc et don d'objets développée avec .NET MAUI.

## 📱 Description

DonTroc est une application mobile permettant aux utilisateurs de :
- **Donner** des objets dont ils n'ont plus besoin
- **Troquer** des objets avec d'autres utilisateurs
- **Découvrir** des objets près de chez eux grâce à la géolocalisation

## ✨ Fonctionnalités

### 🎁 Gestion des annonces
- Création d'annonces avec photos
- Catégorisation des objets
- Recherche et filtres avancés
- Géolocalisation des annonces

### 💬 Communication
- Messagerie intégrée entre utilisateurs
- Notifications push en temps réel
- Système de transactions sécurisé

### 🎮 Gamification
- **Système de points XP** et niveaux
- **Quiz thématiques** sur l'écologie et le recyclage
- **Roue de la fortune** quotidienne
- **Défis** quotidiens et hebdomadaires
- **Badges** et récompenses à débloquer

### 💰 Monétisation
- Publicités AdMob (bannières, interstitiels, récompensées)
- Système de boost pour les annonces
- Support au développeur (donations)

## 🛠️ Technologies utilisées

- **.NET 9** avec **MAUI** (Multi-platform App UI)
- **Firebase** (Authentication, Firestore, Cloud Messaging)
- **Google AdMob** pour la monétisation
- **Cloudinary** pour le stockage des images
- **Google Maps** pour la géolocalisation

## 📋 Prérequis

- .NET 9 SDK
- Visual Studio 2022 ou JetBrains Rider
- Android SDK (pour le développement Android)
- Xcode (pour le développement iOS, macOS uniquement)

## 🚀 Installation

1. Cloner le repository :
```bash
git clone https://github.com/VOTRE_USERNAME/DonTroc.git
cd DonTroc
```

2. Restaurer les packages NuGet :
```bash
dotnet restore
```

3. Configurer les fichiers de sécurité (voir section Configuration)

4. Compiler et exécuter :
```bash
dotnet build
dotnet run --project DonTroc
```

## ⚙️ Configuration

### Fichiers requis (non inclus dans le repo pour des raisons de sécurité) :

1. **google-services.json** - Configuration Firebase pour Android
2. **GoogleService-Info.plist** - Configuration Firebase pour iOS
3. **keystore/signing.properties** - Clés de signature Android
4. **keystore/maps.properties** - Clé API Google Maps

Consultez les guides dans le dossier racine pour la configuration détaillée.

## 📁 Structure du projet

```
DonTroc/
├── DonTroc/                 # Projet principal MAUI
│   ├── Models/              # Modèles de données
│   ├── Views/               # Pages XAML
│   ├── ViewModels/          # ViewModels (MVVM)
│   ├── Services/            # Services métier
│   ├── Converters/          # Convertisseurs XAML
│   └── Resources/           # Ressources (images, styles)
├── DonTroc.sln              # Solution Visual Studio
└── README.md                # Ce fichier
```

## 🤝 Contribution

Les contributions sont les bienvenues ! N'hésitez pas à :
1. Fork le projet
2. Créer une branche (`git checkout -b feature/AmazingFeature`)
3. Commit vos changements (`git commit -m 'Add some AmazingFeature'`)
4. Push sur la branche (`git push origin feature/AmazingFeature`)
5. Ouvrir une Pull Request

## 📄 Licence

Ce projet est sous licence privée. Tous droits réservés.

## 👨‍💻 Auteur

Développé avec ❤️ au Maroc 🇲🇦

---

⭐ Si vous aimez ce projet, n'hésitez pas à lui donner une étoile sur GitHub !

