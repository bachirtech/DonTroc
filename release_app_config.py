#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Script d'automatisation pour publier une nouvelle version DonTroc."""
import argparse
import json
import re
import subprocess
import sys
from pathlib import Path
SCRIPT_DIR = Path(__file__).parent
CSPROJ = SCRIPT_DIR / "DonTroc" / "DonTroc.csproj"
IOS_PLIST = SCRIPT_DIR / "DonTroc" / "Platforms" / "iOS" / "Info.plist"
CONFIG_FILE = SCRIPT_DIR / "app_config.json"
PROJECT_ID = "dontroc-55570"
DEFAULT_RELEASE_NOTES = (
    "- \u2728 Nouvelles fonctionnalit\u00e9s\n"
    "- \ud83d\ude80 Am\u00e9liorations de performance\n"
    "- \ud83d\udc1b Corrections de bugs"
)
DEFAULT_UPDATE_MESSAGE = (
    "Une nouvelle version de DonTroc est disponible avec des am\u00e9liorations et corrections."
)
# URL par defaut pour Android : page GitHub Pages hebergeant l'APK officiel
# (DonTroc n'est plus distribuee via le Play Store).
DEFAULT_ANDROID_UPDATE_URL = "https://bachirtech.github.io/DonTroc/download.html"
def read_csproj_versions():
    content = CSPROJ.read_text(encoding="utf-8")
    code_match = re.search(r"<ApplicationVersion>(\d+)</ApplicationVersion>", content)
    name_match = re.search(r"<ApplicationDisplayVersion>([^<]+)</ApplicationDisplayVersion>", content)
    if not code_match or not name_match:
        print("Impossible de lire les versions dans le csproj", file=sys.stderr)
        sys.exit(1)
    return int(code_match.group(1)), name_match.group(1).strip()
def normalize_ios_version_name(version_name: str) -> str:
    if "." not in version_name:
        return f"{version_name}.0"
    return version_name
def sync_ios_plist(version_code: int, version_name: str) -> bool:
    if not IOS_PLIST.exists():
        print(f"iOS Info.plist introuvable : {IOS_PLIST}")
        return False
    ios_name = normalize_ios_version_name(version_name)
    content = IOS_PLIST.read_text(encoding="utf-8")
    original = content
    content = re.sub(
        r"(<key>CFBundleVersion</key>\s*<string>)[^<]*(</string>)",
        rf"\g<1>{version_code}\g<2>", content, count=1)
    content = re.sub(
        r"(<key>CFBundleShortVersionString</key>\s*<string>)[^<]*(</string>)",
        rf"\g<1>{ios_name}\g<2>", content, count=1)
    if content != original:
        IOS_PLIST.write_text(content, encoding="utf-8")
        print(f"iOS Info.plist synchronise : {ios_name} (build {version_code})")
        return True
    print(f"iOS Info.plist deja a jour ({ios_name} / build {version_code})")
    return False
def main():
    parser = argparse.ArgumentParser(description="Publier la config de version DonTroc")
    parser.add_argument("--notes", default=None)
    parser.add_argument("--message", default=DEFAULT_UPDATE_MESSAGE)
    parser.add_argument("--force-update", action="store_true")
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--android-url", default=DEFAULT_ANDROID_UPDATE_URL,
                        help="URL custom (page download APK) pour Android. Vide = fallback Play Store.")
    parser.add_argument("--ios-url", default="",
                        help="URL custom pour iOS. Vide = fallback App Store.")
    parser.add_argument("--sync-only", action="store_true",
                        help="Synchronise uniquement iOS Info.plist depuis le csproj")
    args = parser.parse_args()
    version_code, version_name = read_csproj_versions()
    print(f"Version detectee dans csproj : {version_name} (build {version_code})")
    sync_ios_plist(version_code, version_name)
    if args.sync_only:
        print("--sync-only : termine, pas de publication Firebase.")
        return 0
    release_notes = args.notes.replace("\\n", "\n") if args.notes else DEFAULT_RELEASE_NOTES
    min_required = version_code if args.force_update else 0
    if args.force_update:
        print(f"MODE FORCE UPDATE : builds < {version_code} bloques jusqu'a MAJ")
    platform_cfg = {
        "latest_version_code": version_code,
        "latest_version_name": version_name,
        "min_required_version_code": min_required,
        "update_message": args.message,
        "release_notes": release_notes,
    }
    android_cfg = dict(platform_cfg, update_url=args.android_url)
    ios_cfg = dict(platform_cfg, update_url=args.ios_url)
    data = {"android": android_cfg, "ios": ios_cfg}
    CONFIG_FILE.write_text(
        json.dumps(data, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"{CONFIG_FILE.name} regenere en UTF-8")
    if args.dry_run:
        print("Dry-run : pas d'envoi Firebase.")
        print(json.dumps(data, ensure_ascii=False, indent=2))
        return 0
    print(f"Envoi vers Firebase (projet {PROJECT_ID})...")
    result = subprocess.run(
        ["firebase", "database:update", "/app_config", str(CONFIG_FILE),
         "--project", PROJECT_ID, "-f"],
        cwd=SCRIPT_DIR)
    if result.returncode == 0:
        print("Configuration deployee ! Les utilisateurs des versions anterieures "
              "verront la popup au prochain lancement.")
    return result.returncode
if __name__ == "__main__":
    sys.exit(main())
