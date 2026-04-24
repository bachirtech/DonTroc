#!/usr/bin/env python3
"""Ecrit le README et le SHA-256 dans ~/Backups/DonTroc/."""
from pathlib import Path

backup_dir = Path.home() / "Backups" / "DonTroc"
backup_dir.mkdir(parents=True, exist_ok=True)

readme = """# Backups secrets DonTroc

## Contenu de chaque ZIP
- dontroc-release.keystore        : keystore Android (PERTE = impossible de mettre a jour l'app)
- signing.properties              : alias + mots de passe du keystore
- maps.properties                 : cle Google Maps
- dontroc-55570-fbbe4cc1107f.json : service account Firebase Admin

## Mot de passe du ZIP
DonTroc2024!1007-Backup

## A FAIRE - Copier ce ZIP sur 3 endroits differents

Un backup local SEUL est INUTILE si le Mac plante / vol / disque mort.

1) Cloud chiffre (recommande)
   - ProtonDrive : https://drive.proton.me
   - 1Password / Bitwarden : en piece jointe d'un Secure Note
   - iCloud Drive (Documents) - chiffre sur disque

2) Cle USB physique rangee chez toi (PAS dans le sac avec le Mac)
   cp ~/Backups/DonTroc/dontroc-secrets-LATEST.zip /Volumes/USBKEY/

3) Email a soi-meme vers un compte secondaire (joindre le ZIP)

## Refaire un backup apres rotation de cle

    cd ~/RiderProjects/DonTroc
    DATE=$(date +%Y%m%d)
    zip -j -P 'DonTroc2024!1007-Backup' \\
      ~/Backups/DonTroc/dontroc-secrets-$DATE.zip \\
      keystore/*.keystore keystore/*.properties \\
      DonTroc/dontroc-*.json
    cp ~/Backups/DonTroc/dontroc-secrets-$DATE.zip \\
       ~/Backups/DonTroc/dontroc-secrets-LATEST.zip

## Verifier l'integrite

    unzip -l ~/Backups/DonTroc/dontroc-secrets-LATEST.zip
    unzip -P 'DonTroc2024!1007-Backup' -t ~/Backups/DonTroc/dontroc-secrets-LATEST.zip
"""

(backup_dir / "README.md").write_text(readme, encoding="utf-8")
(backup_dir / "dontroc-secrets-20260424.zip.sha256").write_text(
    "91def0c9011d9fbbdd3af1390165edba726018fd35c8de3b307944075159b8fb  dontroc-secrets-20260424.zip\n",
    encoding="utf-8")

print("OK -> README.md + .sha256 ecrits dans", backup_dir)
for f in sorted(backup_dir.iterdir()):
    print(f"  {f.stat().st_size:>6} bytes  {f.name}")

