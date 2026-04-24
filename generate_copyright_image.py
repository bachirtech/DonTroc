#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Genere des declarations de copyright en PNG (Huawei AppGallery accepte JPG/PNG/BMP)."""
from PIL import Image, ImageDraw, ImageFont
from datetime import date
from pathlib import Path
import textwrap
import sys

OUT_DIR = Path(__file__).parent / "store_assets"
OUT_DIR.mkdir(parents=True, exist_ok=True)

OWNER_NAME = "Bassirou Balde"
CITY = "Essaouira"
COUNTRY_EN = "Morocco"
COUNTRY_FR = "Maroc"
EMAIL = "bachirdev.pro@gmail.com"
APP_NAME = "DonTroc - Don & Troc d'Objets"
PACKAGE = "com.bachirdev.dontroc"
TODAY_EN = date.today().strftime("%B %d, %Y")
TODAY_FR = date.today().strftime("%d/%m/%Y")

# A4 portrait at 150 DPI
W, H = 1240, 1754
MARGIN = 90
PURPLE = (81, 43, 212)
DARK = (40, 40, 40)
GREY = (110, 110, 110)
LIGHT = (220, 220, 220)
WHITE = (255, 255, 255)


def load_font(size, bold=False):
    candidates_regular = [
        "/System/Library/Fonts/Supplemental/Arial.ttf",
        "/System/Library/Fonts/Helvetica.ttc",
        "/Library/Fonts/Arial.ttf",
        "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
    ]
    candidates_bold = [
        "/System/Library/Fonts/Supplemental/Arial Bold.ttf",
        "/System/Library/Fonts/HelveticaNeue.ttc",
        "/Library/Fonts/Arial Bold.ttf",
        "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
    ]
    for p in (candidates_bold if bold else candidates_regular):
        if Path(p).exists():
            try:
                return ImageFont.truetype(p, size)
            except Exception:
                continue
    return ImageFont.load_default()


F_TITLE = load_font(34, bold=True)
F_SUB = load_font(15)
F_LABEL = load_font(17, bold=True)
F_BODY = load_font(17)
F_BODY_B = load_font(17, bold=True)
F_SMALL = load_font(14)


def draw_wrapped(draw, text, x, y, font, max_width, fill=DARK, line_spacing=8):
    """Dessine un texte avec wrap. Retourne le y final."""
    words = text.split(" ")
    line = ""
    for word in words:
        test = (line + " " + word).strip()
        bbox = draw.textbbox((0, 0), test, font=font)
        if bbox[2] - bbox[0] <= max_width:
            line = test
        else:
            draw.text((x, y), line, font=font, fill=fill)
            y += (bbox[3] - bbox[1]) + line_spacing
            line = word
    if line:
        bbox = draw.textbbox((0, 0), line, font=font)
        draw.text((x, y), line, font=font, fill=fill)
        y += (bbox[3] - bbox[1]) + line_spacing
    return y


def render_declaration(out_path: Path, lang: str):
    img = Image.new("RGB", (W, H), WHITE)
    d = ImageDraw.Draw(img)

    if lang == "en":
        title = "COPYRIGHT OWNERSHIP DECLARATION"
        subtitle = "Submitted to Huawei AppGallery Connect"
        info = [
            ("Full name:", OWNER_NAME),
            ("City / Country:", f"{CITY}, {COUNTRY_EN}"),
            ("Email:", EMAIL),
            ("Application:", APP_NAME),
            ("Package name:", PACKAGE),
            ("Date:", TODAY_EN),
        ]
        intro = "I, the undersigned, hereby declare under oath that:"
        items = [
            f'1. I am the sole author and exclusive copyright owner of the mobile application "{APP_NAME}" (package name: {PACKAGE}).',
            "2. The application has been entirely designed and developed by myself using the .NET MAUI framework. It does not contain any source code, graphic asset, logo, brand name, or trademark belonging to any third party without proper authorization or appropriate open-source license.",
            "3. All graphic assets (icon, illustrations, screenshots, marketing material) were either created by myself or obtained from royalty-free sources and used in compliance with their respective licenses.",
            f'4. The application name "DonTroc" is an original brand created by myself and, to the best of my knowledge, does not infringe any registered trademark of a third party.',
            "5. I take full and exclusive legal responsibility for any copyright infringement claim that may arise in connection with this application, and I undertake to indemnify Huawei against any such claim.",
            "6. I authorize Huawei AppGallery to distribute this application worldwide through its store on Huawei devices.",
        ]
        signed = f"Signed in {CITY}, {COUNTRY_EN}, on {TODAY_EN}."
        sig_label = "Signature:"
    else:
        title = "DECLARATION DE DROITS D'AUTEUR"
        subtitle = "Document soumis a Huawei AppGallery Connect"
        info = [
            ("Nom complet :", OWNER_NAME),
            ("Ville / Pays :", f"{CITY}, {COUNTRY_FR}"),
            ("Email :", EMAIL),
            ("Application :", APP_NAME),
            ("Identifiant :", PACKAGE),
            ("Date :", TODAY_FR),
        ]
        intro = "Je soussigne, declare sur l'honneur ce qui suit :"
        items = [
            f"1. Je suis l'auteur unique et le titulaire exclusif des droits d'auteur de l'application mobile \"{APP_NAME}\" (identifiant : {PACKAGE}).",
            "2. L'application a ete integralement concue et developpee par moi-meme a l'aide du framework .NET MAUI. Elle ne contient aucun code source, element graphique, logo, nom commercial ou marque appartenant a un tiers sans autorisation valable ou licence open-source appropriee.",
            "3. Toutes les ressources graphiques (icone, illustrations, captures d'ecran, supports marketing) ont ete creees par mes soins ou proviennent de sources libres de droits et sont utilisees conformement a leurs licences respectives.",
            "4. Le nom \"DonTroc\" est une marque originale creee par mes soins et, a ma connaissance, ne porte atteinte a aucune marque deposee par un tiers.",
            "5. J'assume la pleine et entiere responsabilite juridique pour toute reclamation relative a la violation de droits d'auteur qui pourrait etre formulee a l'encontre de cette application, et je m'engage a indemniser Huawei contre toute reclamation de cette nature.",
            "6. J'autorise Huawei AppGallery a distribuer cette application dans le monde entier via sa plateforme sur les appareils Huawei.",
        ]
        signed = f"Fait a {CITY}, {COUNTRY_FR}, le {TODAY_FR}."
        sig_label = "Signature :"

    y = MARGIN
    # Titre
    bbox = d.textbbox((0, 0), title, font=F_TITLE)
    tw = bbox[2] - bbox[0]
    d.text(((W - tw) / 2, y), title, font=F_TITLE, fill=PURPLE)
    y += (bbox[3] - bbox[1]) + 8
    bbox = d.textbbox((0, 0), subtitle, font=F_SUB)
    sw = bbox[2] - bbox[0]
    d.text(((W - sw) / 2, y), subtitle, font=F_SUB, fill=GREY)
    y += (bbox[3] - bbox[1]) + 30

    # Bloc infos
    label_x = MARGIN
    value_x = MARGIN + 230
    for label, value in info:
        d.text((label_x, y), label, font=F_LABEL, fill=DARK)
        d.text((value_x, y), value, font=F_BODY, fill=DARK)
        bbox = d.textbbox((0, 0), label, font=F_LABEL)
        line_h = bbox[3] - bbox[1]
        y += line_h + 14
        d.line([(label_x, y - 6), (W - MARGIN, y - 6)], fill=LIGHT, width=1)
    y += 20

    # Intro
    y = draw_wrapped(d, intro, MARGIN, y, F_BODY_B, W - 2 * MARGIN)
    y += 10

    # Items
    for item in items:
        y = draw_wrapped(d, item, MARGIN, y, F_BODY, W - 2 * MARGIN)
        y += 8

    y += 30
    y = draw_wrapped(d, signed, MARGIN, y, F_BODY_B, W - 2 * MARGIN)
    y += 60

    # Signature
    d.text((MARGIN, y), sig_label, font=F_LABEL, fill=DARK)
    line_y = y + 25
    d.line([(MARGIN + 180, line_y), (MARGIN + 700, line_y)],
           fill=DARK, width=2)
    d.text((MARGIN + 180, line_y + 8), OWNER_NAME, font=F_BODY, fill=DARK)

    img.save(out_path, "PNG", optimize=True)
    print(f"OK -> {out_path}  ({out_path.stat().st_size // 1024} Ko)")


if __name__ == "__main__":
    try:
        render_declaration(OUT_DIR / "copyright_declaration_EN.png", "en")
        render_declaration(OUT_DIR / "copyright_declaration_FR.png", "fr")
    except Exception as e:
        print("ERREUR :", e, file=sys.stderr)
        sys.exit(1)

