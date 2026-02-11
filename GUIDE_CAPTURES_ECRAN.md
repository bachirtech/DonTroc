# 📸 Guide Complet - Captures d'Écran App Store pour DonTroc

## 🎯 Objectif
Créer des captures d'écran professionnelles qui attirent l'attention et convertissent les visiteurs en utilisateurs.

---

## 📏 DIMENSIONS REQUISES

### Google Play Store (Android)
| Type | Dimensions | Format |
|------|------------|--------|
| Téléphone | 1080 x 1920 px (ou 16:9) | PNG/JPEG |
| Tablette 7" | 1200 x 1920 px | PNG/JPEG |
| Tablette 10" | 1600 x 2560 px | PNG/JPEG |

### Apple App Store (iOS)
| Appareil | Dimensions | Obligatoire |
|----------|------------|-------------|
| iPhone 6.7" (15 Pro Max) | 1290 x 2796 px | ✅ Oui |
| iPhone 6.5" (14 Plus) | 1284 x 2778 px | ✅ Oui |
| iPhone 5.5" (8 Plus) | 1242 x 2208 px | Optionnel |
| iPad Pro 12.9" | 2048 x 2732 px | Pour iPad |

---

## 🛠️ MÉTHODES POUR CRÉER VOS CAPTURES

### 1️⃣ **Méthode Simple : Capture depuis l'émulateur/simulateur**

#### Sur Android (Android Studio) :
```bash
# Lancer l'émulateur avec une résolution haute
emulator -avd Pixel_6_Pro -skin 1080x2400

# Capture d'écran via ADB
adb exec-out screencap -p > screenshot.png
```

#### Sur iOS (Xcode Simulator) :
```bash
# Capture depuis le simulateur
xcrun simctl io booted screenshot screenshot.png
```

#### Depuis Rider/Visual Studio :
1. Lancez l'app sur l'émulateur Android ou simulateur iOS
2. Dans l'émulateur Android : Cliquez sur l'icône caméra 📷
3. Sur iOS Simulator : `Cmd + S` pour capturer

---

### 2️⃣ **Méthode Intermédiaire : Capture depuis appareil réel**

#### Android :
- **Boutons** : Volume Bas + Power (simultanément)
- **Geste** : Balayer 3 doigts vers le bas (selon téléphone)
- **Assistant** : "OK Google, capture d'écran"

#### iPhone :
- **Face ID** : Bouton latéral + Volume Haut
- **Touch ID** : Bouton Home + Power

---

### 3️⃣ **Méthode Professionnelle : Mockups avec cadres d'appareils**

#### Outils en ligne GRATUITS :
| Outil | URL | Avantages |
|-------|-----|-----------|
| **Mockup World** | mockupworld.co | Mockups gratuits |
| **Smartmockups** | smartmockups.com | Simple d'utilisation |
| **Device Frames** | deviceframes.com | Cadres variés |
| **Previewed** | previewed.app | Templates modernes |
| **AppLaunchpad** | theapplaunchpad.com | Spécial App Store |
| **Placeit** | placeit.net | Large choix (payant) |

#### Outils de design :
| Outil | Prix | Niveau |
|-------|------|--------|
| **Canva** | Gratuit/Pro | Débutant ⭐ |
| **Figma** | Gratuit | Intermédiaire ⭐⭐ |
| **Adobe XD** | Gratuit | Intermédiaire ⭐⭐ |
| **Photoshop** | Payant | Avancé ⭐⭐⭐ |

---

## 🎨 STRUCTURE RECOMMANDÉE POUR CHAQUE CAPTURE

### Template de capture optimale :

```
┌─────────────────────────────────┐
│                                 │
│    📝 TITRE ACCROCHEUR          │  ← Texte court (3-5 mots)
│    "Donnez vos objets !"        │
│                                 │
│  ┌─────────────────────────┐    │
│  │                         │    │
│  │     📱 CAPTURE          │    │  ← Screenshot de l'app
│  │     DE L'APP            │    │     dans un cadre mobile
│  │                         │    │
│  │                         │    │
│  │                         │    │
│  └─────────────────────────┘    │
│                                 │
│    ✨ Sous-titre optionnel      │  ← Texte secondaire
│                                 │
└─────────────────────────────────┘
```

---

## 📱 LES 8 CAPTURES RECOMMANDÉES POUR DONTROC

### Capture 1 : **Accueil**
- **Titre** : "Bienvenue sur DonTroc !"
- **Écran** : Dashboard avec statistiques
- **Message** : Montrer l'interface principale accueillante

### Capture 2 : **Liste des annonces**
- **Titre** : "Trouvez des trésors près de vous"
- **Écran** : AnnoncesView avec grille d'objets
- **Message** : Variété d'objets disponibles

### Capture 3 : **Carte interactive**
- **Titre** : "Objets autour de vous 📍"
- **Écran** : MapView avec pins sur la carte
- **Message** : Géolocalisation et proximité

### Capture 4 : **Création d'annonce**
- **Titre** : "Publiez en 30 secondes ⚡"
- **Écran** : CreationAnnonceView avec photos
- **Message** : Simplicité de publication

### Capture 5 : **Messagerie**
- **Titre** : "Échangez facilement 💬"
- **Écran** : ChatView avec conversation
- **Message** : Communication directe

### Capture 6 : **Récompenses & Gamification**
- **Titre** : "Gagnez des récompenses 🏆"
- **Écran** : RewardsPage avec XP et badges
- **Message** : Aspect ludique et motivant

### Capture 7 : **Quiz écologique**
- **Titre** : "Testez vos connaissances 🧠"
- **Écran** : QuizPage avec question
- **Message** : Apprentissage fun

### Capture 8 : **Profil utilisateur**
- **Titre** : "Votre réputation compte ⭐"
- **Écran** : ProfilView avec badges et avis
- **Message** : Système de confiance

---

## ✅ BONNES PRATIQUES

### À FAIRE ✅
- [ ] Utiliser des couleurs cohérentes avec l'app (Terracotta, Vert Sauge)
- [ ] Textes courts et impactants (max 5-6 mots)
- [ ] Mettre en avant les fonctionnalités clés
- [ ] Utiliser des emojis pour attirer l'œil 👀
- [ ] Montrer des données réalistes dans les captures
- [ ] Assurer la lisibilité sur petits écrans
- [ ] Garder un style cohérent entre toutes les captures

### À ÉVITER ❌
- [ ] Trop de texte sur une capture
- [ ] Images floues ou basse résolution
- [ ] Barres de statut avec infos personnelles
- [ ] Notifications visibles en haut de l'écran
- [ ] Captures avec données vides ou "Lorem ipsum"
- [ ] Styles incohérents entre les captures

---

## 🎨 PALETTE DE COULEURS À UTILISER

```
Fond principal    : #FDF8F3 (Beige Clair)
Terracotta        : #D98C6A 
Vert Sauge        : #8FA878
Ardoise           : #4A5568
Blanc             : #FFFFFF
Texte sombre      : #2D3748
Accent            : #E65100 (Orange)
```

---

## 📝 SCRIPT RAPIDE - Préparer les captures

### Étapes pour préparer l'app avant capture :

1. **Nettoyer les données de test** :
   - Créer 5-6 annonces avec de belles photos
   - Avoir des conversations avec messages réalistes
   - Avoir des XP et badges débloqués

2. **Configurer l'appareil/émulateur** :
   - Mode avion ou masquer les notifications
   - Heure ronde (ex: 10:00)
   - Batterie pleine (100%)
   - WiFi et signal pleins

3. **Naviguer vers chaque écran** et capturer

---

## 🔧 WORKFLOW AVEC CANVA (GRATUIT)

### Étape par étape :

1. **Créer un compte Canva** : canva.com (gratuit)

2. **Nouveau design** : 
   - Dimensions personnalisées : 1290 x 2796 px (iPhone)
   - Ou chercher "App Store Screenshot"

3. **Ajouter le fond** :
   - Couleur unie (#FDF8F3) ou dégradé léger

4. **Importer votre capture** :
   - Glisser-déposer votre screenshot

5. **Ajouter un cadre de téléphone** :
   - Éléments → Chercher "phone mockup" ou "mobile frame"
   - Placer la capture à l'intérieur

6. **Ajouter le texte** :
   - Police moderne (Poppins, Montserrat, Inter)
   - Taille visible mais pas dominante

7. **Exporter** :
   - PNG haute qualité
   - Nommer : `01_accueil.png`, `02_annonces.png`, etc.

---

## 📦 STRUCTURE DES FICHIERS

```
DonTroc/
└── store_assets/
    ├── screenshots/
    │   ├── android/
    │   │   ├── phone/
    │   │   │   ├── 01_accueil.png
    │   │   │   ├── 02_annonces.png
    │   │   │   └── ...
    │   │   └── tablet/
    │   │       └── ...
    │   └── ios/
    │       ├── iphone_6.7/
    │       │   ├── 01_accueil.png
    │       │   └── ...
    │       └── ipad/
    │           └── ...
    ├── icon/
    │   ├── icon_512.png
    │   └── icon_1024.png
    └── feature_graphic/
        └── feature_1024x500.png
```

---

## 🎬 BONUS : Vidéo de présentation

### Spécifications :
- **Durée** : 15-30 secondes
- **Format** : MP4, MOV
- **Résolution** : 1080p minimum
- **Audio** : Optionnel (musique libre de droits)

### Outils pour vidéo :
- **Loom** (gratuit) : Enregistrement d'écran
- **OBS Studio** (gratuit) : Plus avancé
- **ScreenFlow** (Mac) : Professionnel

---

## 📋 CHECKLIST FINALE

- [ ] 8 captures d'écran créées pour chaque plateforme
- [ ] Dimensions correctes vérifiées
- [ ] Textes lisibles et accrocheurs
- [ ] Style cohérent entre toutes les captures
- [ ] Pas d'informations personnelles visibles
- [ ] Fichiers nommés correctement
- [ ] Sauvegarde des fichiers sources (Canva, Figma)

---

*Guide créé le 22 décembre 2025*
*Pour toute question : consulter la documentation officielle de Google Play et App Store Connect*

