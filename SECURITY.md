# Security Policy — DonTroc

## 📜 Open Source & Transparency

The full source code of **DonTroc** is publicly available in this repository
for transparency, security auditing, and community trust.

DonTroc is a peer-to-peer donation, swap, and second-hand sale application
designed to promote sustainability and reduce waste. The project is built
in good faith and strictly follows the Google Play Developer Program Policies.

## 🔒 Sensitive Data Handling

The following sensitive assets are **never** committed to this repository
and are excluded via `.gitignore`:

- Android signing keystore (`*.keystore`, `*.jks`)
- Signing credentials (`signing.properties`)
- Firebase configuration (`google-services.json`)
- Firebase Admin SDK service account JSON
- AdMob production unit IDs (`Local.Build.props`)
- Google Maps API keys (`maps.properties`)
- Any `.env` or `secrets.json` file

## 🛡️ Privacy & User Data

- All user data is stored in Firebase Realtime Database with strict
  per-user security rules (`firebase_rules.json` in this repo).
- The app requests only the permissions strictly necessary for its
  declared functionality (camera, location, storage, notifications).
- No user data is sold or shared with third parties beyond the services
  required for app functionality (Firebase, Cloudinary, Google AdMob).
- A full Privacy Policy is published at:
  https://bachirtech.github.io/DonTroc/privacy-policy.html

## 📨 Reporting a Vulnerability

If you discover a security issue, please report it privately to:
**bachirdev.contact@gmail.com**

Please do **not** open a public GitHub issue for security vulnerabilities.

## ✅ Compliance

DonTroc complies with:
- Google Play Developer Program Policies
- GDPR (General Data Protection Regulation)
- Google Play Families Policy (rated PEGI 3 / Everyone)
- AdMob Publisher Policies (no incentivized clicks, age-appropriate ads)

