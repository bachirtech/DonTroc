# 🚀 IronSource/LevelPlay - En Attente de Validation

## 📧 État Actuel

**Votre compte IronSource Ads nécessite une validation par Unity.**

C'est pourquoi aucune publicité IronSource ne s'affiche pour l'instant. C'est un processus normal et obligatoire.

---

## ✅ Ce que vous devez faire MAINTENANT

### 1. Envoyez un email à Unity

**Destinataire:** `ironsource-account-review@unity3d.com`

**Sujet:** `ironSource account review details`

**Contenu de l'email:**
```
Bonjour,

Je souhaite activer mon compte ironSource Ads pour mon application.

Informations requises:
- Lien Play Store: https://play.google.com/store/apps/details?id=com.bachirdev.dontroc
- Email du compte ironSource: [VOTRE EMAIL]
- Numéro de ticket: 00880519

Merci de valider mon compte.

Cordialement,
[VOTRE NOM]
```

### 2. Attendez la validation

Unity examine les comptes manuellement. Le délai est généralement de **quelques jours ouvrables**.

---

## 🎯 Bonne nouvelle : LevelPlay Mediation fonctionne déjà!

Le message de Unity précise :

> "If you are using LevelPlay mediation, you can already start monetizing with the other mediated networks."

Cela signifie que vous pouvez **dès maintenant** :
1. Ajouter d'autres réseaux publicitaires dans LevelPlay (AdMob, Unity Ads, AppLovin, etc.)
2. Ces réseaux fonctionneront immédiatement sans attendre la validation IronSource

---

## 🔧 Une fois approuvé par Unity

Modifiez le fichier `DonTroc/Services/Providers/IronSourceProvider.cs` :

```csharp
// Changer cette ligne de false à true
private const bool ACCOUNT_VALIDATED = true;
```

Puis décommentez dans `DonTroc/DonTroc.csproj` :
```xml
<!-- Décommentez cette ligne -->
<ProjectReference Include="..\IronSource.Binding\IronSource.Binding.csproj" />
```

---

## 📊 Pourquoi IronSource/LevelPlay?

| Avantage | Description |
|----------|-------------|
| 🛡️ **Diversification** | Si AdMob suspend votre compte, vous avez d'autres réseaux |
| 💰 **Optimisation** | Enchères en temps réel entre plusieurs réseaux |
| 📈 **Analytics** | Dashboard unifié pour tous les réseaux |
| 🔄 **Fallback** | Si un réseau n'a pas de pub, un autre prend le relais |

---

## 📝 Configuration actuelle

- **App Key IronSource:** `2525f980d` ✅ Configurée
- **Binding SDK:** Désactivé temporairement (en attente de validation)
- **IronSourceProvider:** Mode stub (ne fait rien mais compile)

---

## ❓ Questions fréquentes

### Q: Pourquoi le binding IronSource est désactivé?
R: Pour permettre au projet de compiler rapidement. Une fois votre compte validé, on réactivera le SDK complet.

### Q: Puis-je utiliser AdMob en attendant?
R: Oui ! AdMob fonctionne indépendamment. Le système de médiation permettra d'utiliser les deux une fois IronSource activé.

### Q: Combien de temps dure la validation?
R: Généralement quelques jours ouvrables. Unity peut demander des informations supplémentaires.

---

## 📌 Checklist

- [ ] Envoyer l'email à ironsource-account-review@unity3d.com
- [ ] Attendre la réponse de Unity
- [ ] Une fois approuvé, changer `ACCOUNT_VALIDATED = true`
- [ ] Décommenter la référence au binding dans DonTroc.csproj
- [ ] Recompiler et tester

---

*Document créé le 5 février 2026*
