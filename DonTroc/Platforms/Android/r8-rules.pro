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

# Ignorer les conflits AndroidX Compose (dépendances transitives)
-dontwarn androidx.compose.**
-dontnote androidx.compose.**
-keep class androidx.compose.** { *; }

# Ignorer les conflits AndroidX SavedState (dépendances transitives)
-dontwarn androidx.savedstate.**
-dontnote androidx.savedstate.**
-keep class androidx.savedstate.** { *; }

# Ignorer les conflits AndroidX Lifecycle Ktx
-dontwarn androidx.lifecycle.**
-dontnote androidx.lifecycle.**
-keep class androidx.lifecycle.** { *; }

# Ignorer les conflits AndroidX Activity/Fragment Ktx
-dontwarn androidx.activity.**
-dontwarn androidx.fragment.**
-dontnote androidx.activity.**
-dontnote androidx.fragment.**

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

# ===== Google Maps =====
-keep class com.google.android.gms.maps.** { *; }
-keep interface com.google.android.gms.maps.** { *; }
-dontwarn com.google.android.gms.maps.**

# ===== Kotlin (utilisé par les SDK modernes) =====
-keep class kotlin.** { *; }
-keep class kotlinx.** { *; }
-dontwarn kotlin.**
-dontwarn kotlinx.**

# ===== OkHttp / OkIO =====
-keep class okhttp3.** { *; }
-keep class okio.** { *; }
-dontwarn okhttp3.**
-dontwarn okio.**

# ===== Protobuf =====
-keep class com.google.protobuf.** { *; }
-dontwarn com.google.protobuf.**

# ===== InMobi SDK =====
-keep class com.inmobi.** { *; }
-dontwarn com.inmobi.**
-keep class com.iab.omid.library.inmobi.** { *; }
-dontwarn com.iab.omid.library.inmobi.**

# ===== Picasso (requis par InMobi) =====
-keep class com.squareup.picasso.** { *; }
-dontwarn com.squareup.picasso.**

