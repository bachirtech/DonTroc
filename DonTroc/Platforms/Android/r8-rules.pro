# Règles R8/D8 pour résoudre les conflits de classes dupliquées
# Entre Firebase Analytics et Google Play Services Ads

# SOLUTION AU CONFLIT: Accepter les classes dupliquées measurement
-dontwarn com.google.android.gms.internal.measurement.**
-dontnote com.google.android.gms.internal.measurement.**

# Ignorer TOUS les warnings et notes de duplication
-ignorewarnings
-dontnote **

# Désactiver l'optimisation pour éviter les problèmes de compilation
-dontoptimize
-dontobfuscate
-dontpreverify

# Ne pas vérifier les références pour GMS et Firebase
-dontwarn com.google.android.gms.**
-dontwarn com.google.firebase.**
-dontwarn com.google.android.play.**

# Garder toutes les classes Google sans modification
-keep class com.google.android.gms.** { *; }
-keep class com.google.firebase.** { *; }
-keep class com.google.android.play.** { *; }

# Règles spécifiques pour AdMob
-keep public class com.google.android.gms.ads.** { public *; }
-keep public class com.google.ads.** { public *; }

# Garder les interfaces de callback AdMob
-keep class * extends com.google.android.gms.ads.AdListener { *; }
-keep class * implements com.google.android.gms.ads.rewarded.OnUserEarnedRewardListener { *; }

# Règles pour les méthodes natives
-keepclasseswithmembernames class * {
    native <methods>;
}

# Configuration pour accepter explicitement les duplications
-keeppackagenames com.google.android.gms.internal.measurement
-keeppackagenames com.google.firebase.analytics

