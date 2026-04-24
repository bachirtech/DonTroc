#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Genere un guide PDF pour deposer la marque DonTroc a l'OMPIC (Maroc)."""
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.units import cm
from reportlab.lib.enums import TA_JUSTIFY, TA_CENTER, TA_LEFT
from reportlab.platypus import (SimpleDocTemplate, Paragraph, Spacer,
                                Table, TableStyle, PageBreak, ListFlowable,
                                ListItem)
from reportlab.lib import colors
from datetime import date
from pathlib import Path

OUT = Path(__file__).parent / "store_assets" / "guide_protection_marque_DonTroc.pdf"
OUT.parent.mkdir(parents=True, exist_ok=True)

OWNER = "Bassirou Balde"
CITY = "Essaouira, Maroc"
EMAIL = "bachirdev.pro@gmail.com"
APP = "DonTroc - Don & Troc d'Objets"

doc = SimpleDocTemplate(str(OUT), pagesize=A4,
                        leftMargin=2*cm, rightMargin=2*cm,
                        topMargin=2*cm, bottomMargin=2*cm,
                        title="Guide protection marque DonTroc",
                        author=OWNER)
styles = getSampleStyleSheet()
PURPLE = colors.HexColor("#512BD4")
ORANGE = colors.HexColor("#D98C6A")

H1 = ParagraphStyle("H1", parent=styles["Title"], fontSize=20,
                    alignment=TA_CENTER, textColor=PURPLE, spaceAfter=10)
H2 = ParagraphStyle("H2", parent=styles["Heading2"], fontSize=14,
                    textColor=PURPLE, spaceBefore=14, spaceAfter=8)
H3 = ParagraphStyle("H3", parent=styles["Heading3"], fontSize=11.5,
                    textColor=ORANGE, spaceBefore=10, spaceAfter=4)
B = ParagraphStyle("B", parent=styles["BodyText"], fontSize=10.5,
                   leading=15, alignment=TA_JUSTIFY, spaceAfter=6)
SMALL = ParagraphStyle("SMALL", parent=styles["BodyText"], fontSize=9,
                       leading=12, textColor=colors.grey)


def p(t):
    return Paragraph(t, B)


def h2(t):
    return Paragraph(t, H2)


def h3(t):
    return Paragraph(t, H3)


def bullet(items, style=B):
    return ListFlowable(
        [ListItem(Paragraph(it, style), leftIndent=12) for it in items],
        bulletType="bullet", start="\u2022", leftIndent=14)


story = []

# ================= COUVERTURE =================
story.append(Paragraph("GUIDE PRATIQUE", H1))
story.append(Paragraph("Protection juridique de la marque <b>DonTroc</b>",
                       ParagraphStyle("sub", parent=H1, fontSize=15,
                                      textColor=colors.black, spaceAfter=20)))
story.append(Paragraph("Depot OMPIC (Maroc) + Extension internationale (OMPI Madrid)",
                       SMALL))
story.append(Spacer(1, 1*cm))

cover_data = [
    ["Titulaire :", OWNER],
    ["Adresse :", CITY],
    ["Email :", EMAIL],
    ["Application :", APP],
    ["Statut :", "Auto-Entrepreneur (Maroc)"],
    ["Document genere le :", date.today().strftime("%d/%m/%Y")],
]
t = Table(cover_data, colWidths=[5*cm, 11*cm])
t.setStyle(TableStyle([
    ("FONTNAME", (0, 0), (0, -1), "Helvetica-Bold"),
    ("FONTSIZE", (0, 0), (-1, -1), 10.5),
    ("BOTTOMPADDING", (0, 0), (-1, -1), 6),
    ("LINEBELOW", (0, 0), (-1, -1), 0.25, colors.lightgrey),
]))
story.append(t)
story.append(Spacer(1, 1*cm))
story.append(Paragraph(
    "<b>Ce guide vous accompagne pas a pas pour proteger juridiquement la marque "
    "\"DonTroc\" au Maroc et a l'international.</b> Suivez les etapes dans l'ordre. "
    "Le depot OMPIC est la priorite absolue : il vous donne un droit exclusif sur le nom "
    "et empeche tout tiers de l'utiliser pour une activite similaire.", B))

# ================= PARTIE 1 : VERIFICATION =================
story.append(PageBreak())
story.append(h2("Etape 1 - Verifier la disponibilite du nom (GRATUIT, 30 min)"))
story.append(p("Avant tout depot, il est <b>imperatif</b> de verifier qu'aucune marque "
               "identique ou similaire \"DonTroc\" n'a deja ete deposee. Sinon votre "
               "depot sera rejete et les frais ne seront pas rembourses."))
story.append(h3("1.1 Recherche sur la base OMPIC (Maroc)"))
story.append(p("Site : <b>www.ompic.ma</b> &gt; Acces direct &gt; Recherche sur les marques"))
story.append(bullet([
    "Tapez : <b>DonTroc</b>",
    "Cherchez aussi les variantes : Don Troc, Dontroc, DonTrok",
    "Verifiez les classes 9 (logiciels), 35 (publicite/commerce), 38 (telecoms), 42 (services informatiques)",
    "Resultat attendu : aucune marque deposee identique &gt; vous pouvez continuer",
]))
story.append(h3("1.2 Recherche internationale (OMPI - WIPO)"))
story.append(p("Site : <b>branddb.wipo.int</b> &mdash; base mondiale de toutes les marques deposees."))
story.append(p("Si \"DonTroc\" est deja prise dans certains pays, vous pourrez quand meme "
               "deposer au Maroc (la protection est territoriale). Mais cela limitera votre "
               "extension internationale future."))
story.append(h3("1.3 Recherche Union Europeenne (TMview)"))
story.append(p("Site : <b>www.tmdn.org/tmview</b> &mdash; couvre l'EUIPO et tous les offices "
               "nationaux europeens."))
story.append(h3("1.4 Verification du nom de domaine"))
story.append(p("Verifiez sur <b>www.namecheap.com</b> ou <b>www.gandi.net</b> que "
               "<b>dontroc.com</b>, <b>dontroc.ma</b>, <b>dontroc.app</b> sont disponibles. "
               "Si oui, achetez-les <b>maintenant</b> (~10-50 EUR/an chacun) AVANT le "
               "depot officiel pour eviter le cybersquatting."))

# ================= PARTIE 2 : DEPOT OMPIC =================
story.append(PageBreak())
story.append(h2("Etape 2 - Depot de la marque a l'OMPIC (Maroc)"))
story.append(p("L'OMPIC (Office Marocain de la Propriete Industrielle et Commerciale) est "
               "l'organisme officiel pour proteger une marque au Maroc. Le depot est "
               "<b>obligatoire</b> pour avoir un droit exclusif sur \"DonTroc\"."))
story.append(h3("2.1 Cout total estime"))
cost_data = [
    ["Element", "Cout (MAD)", "Notes"],
    ["Depot de base (1 classe)", "1 000", "Premiere classe incluse"],
    ["Classes supplementaires (x3)", "300 x 3 = 900", "Classes 35, 38, 42"],
    ["Publication au BO", "200", "Bulletin Officiel"],
    ["TOTAL DEPOT", "2 100 MAD", "~210 EUR"],
    ["", "", ""],
    ["Validite", "10 ans", "Renouvelable indefiniment"],
    ["Renouvellement", "1 200 MAD", "Tous les 10 ans"],
]
t = Table(cost_data, colWidths=[7*cm, 4*cm, 5*cm])
t.setStyle(TableStyle([
    ("BACKGROUND", (0, 0), (-1, 0), PURPLE),
    ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
    ("FONTNAME", (0, 0), (-1, 0), "Helvetica-Bold"),
    ("FONTSIZE", (0, 0), (-1, -1), 10),
    ("GRID", (0, 0), (-1, -1), 0.25, colors.lightgrey),
    ("BACKGROUND", (0, 4), (-1, 4), colors.HexColor("#FFE6D9")),
    ("FONTNAME", (0, 4), (-1, 4), "Helvetica-Bold"),
    ("PADDING", (0, 0), (-1, -1), 6),
]))
story.append(t)
story.append(Spacer(1, 0.4*cm))

story.append(h3("2.2 Classes de Nice a deposer pour DonTroc"))
story.append(p("La classification de Nice regroupe les produits/services en 45 classes. "
               "Pour une application mobile de don/troc, deposez les <b>4 classes "
               "suivantes</b> :"))
class_data = [
    ["Classe", "Couvre", "Pertinence DonTroc"],
    ["9", "Logiciels, applications mobiles", "INDISPENSABLE - votre app"],
    ["35", "Publicite, gestion d'affaires, commerce en ligne", "INDISPENSABLE - mise en relation"],
    ["38", "Telecommunications, messagerie", "Recommande - chat integre"],
    ["42", "Services informatiques, hebergement, SaaS", "Recommande - backend Firebase"],
]
t = Table(class_data, colWidths=[2*cm, 8*cm, 6*cm])
t.setStyle(TableStyle([
    ("BACKGROUND", (0, 0), (-1, 0), PURPLE),
    ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
    ("FONTNAME", (0, 0), (-1, 0), "Helvetica-Bold"),
    ("FONTSIZE", (0, 0), (-1, -1), 9.5),
    ("GRID", (0, 0), (-1, -1), 0.25, colors.lightgrey),
    ("VALIGN", (0, 0), (-1, -1), "TOP"),
    ("PADDING", (0, 0), (-1, -1), 5),
]))
story.append(t)

story.append(h3("2.3 Documents necessaires pour le depot"))
story.append(bullet([
    "Formulaire OMPIC M1 (telechargeable sur ompic.ma)",
    "Copie de votre <b>CIN marocaine</b> (recto-verso)",
    "Copie de votre <b>carte Auto-Entrepreneur</b>",
    "Logo de la marque en haute definition (PNG, 300 dpi minimum)",
    "Description claire des produits/services par classe",
    "Pouvoir si vous passez par un mandataire (cabinet de PI)",
    "Justificatif de paiement des taxes",
]))

story.append(h3("2.4 Procedure de depot - 3 options"))
story.append(p("<b>Option A - En ligne (recommande)</b><br/>"
               "Plateforme : <b>directmarques.ompic.ma</b><br/>"
               "Creez un compte, remplissez le formulaire, payez par carte. "
               "Vous recevez un numero de depot immediatement."))
story.append(p("<b>Option B - En personne</b><br/>"
               "Siege OMPIC : Route de Nouasseur, Casablanca<br/>"
               "Antenne Casa : 20 Rue de Tichka, Casablanca<br/>"
               "Antenne Rabat : Av. Mohammed VI"))
story.append(p("<b>Option C - Via un cabinet de propriete intellectuelle</b><br/>"
               "Cout : 3 000 a 6 000 MAD tout compris (depot + suivi). Recommande si "
               "vous voulez deleguer entierement et eviter les erreurs. Cabinets "
               "reconnus : Saba & Co, Hammad &amp; Co, Cabinet Kadiri."))

story.append(h3("2.5 Calendrier"))
timeline = [
    ["Jour 0", "Depot du dossier"],
    ["+ 1 mois", "Examen formel (verification administrative)"],
    ["+ 3-4 mois", "Publication au Bulletin Officiel de la PI"],
    ["+ 5-6 mois", "Periode d'opposition (2 mois pour les tiers)"],
    ["+ 6-8 mois", "Enregistrement definitif, certificat envoye"],
]
t = Table(timeline, colWidths=[3.5*cm, 12.5*cm])
t.setStyle(TableStyle([
    ("FONTNAME", (0, 0), (0, -1), "Helvetica-Bold"),
    ("FONTSIZE", (0, 0), (-1, -1), 10),
    ("BACKGROUND", (0, 0), (0, -1), colors.HexColor("#F0EBFA")),
    ("GRID", (0, 0), (-1, -1), 0.25, colors.lightgrey),
    ("PADDING", (0, 0), (-1, -1), 6),
]))
story.append(t)

# ================= PARTIE 3 : INTERNATIONAL =================
story.append(PageBreak())
story.append(h2("Etape 3 - Extension internationale (Systeme de Madrid - OMPI)"))
story.append(p("Une fois la marque deposee au Maroc (et au moins l'examen formel valide), "
               "vous pouvez l'etendre dans <b>130+ pays</b> avec un seul depot "
               "international via l'OMPI (Organisation Mondiale de la Propriete "
               "Intellectuelle)."))

story.append(h3("3.1 Quand le faire ?"))
story.append(p("<b>Conseil :</b> attendez d'avoir au moins <b>5 000 utilisateurs actifs</b> "
               "ou <b>500 EUR/mois de revenus</b> avant d'investir. La protection "
               "internationale coute cher et n'est utile que si vous avez deja une traction."))

story.append(h3("3.2 Cout estime (Systeme de Madrid)"))
intl_data = [
    ["Element", "Cout"],
    ["Taxe de base OMPI (marque en couleur)", "~903 CHF (~9 500 MAD)"],
    ["Taxe par pays designe (en moyenne)", "~100-300 CHF chacun"],
    ["Exemple : Maroc + France + UE + Senegal", "~1 800 CHF (~19 000 MAD)"],
    ["Exemple : couverture monde entier (15 pays)", "~4 000 CHF (~42 000 MAD)"],
]
t = Table(intl_data, colWidths=[10*cm, 6*cm])
t.setStyle(TableStyle([
    ("BACKGROUND", (0, 0), (-1, 0), PURPLE),
    ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
    ("FONTNAME", (0, 0), (-1, 0), "Helvetica-Bold"),
    ("FONTSIZE", (0, 0), (-1, -1), 10),
    ("GRID", (0, 0), (-1, -1), 0.25, colors.lightgrey),
    ("PADDING", (0, 0), (-1, -1), 6),
]))
story.append(t)

story.append(h3("3.3 Pays prioritaires recommandes pour DonTroc"))
story.append(bullet([
    "<b>France</b> : marche francophone n.1, beaucoup de Marocains residents",
    "<b>Union Europeenne</b> (EUIPO) : 27 pays en un seul depot",
    "<b>Senegal, Cote d'Ivoire</b> : marche OAPI (16 pays africains francophones en un)",
    "<b>Canada (Quebec)</b> : marche francophone americain",
    "<b>Belgique, Suisse</b> : francophonie europeenne",
]))

# ================= PARTIE 4 : ACTIONS COMPLEMENTAIRES =================
story.append(PageBreak())
story.append(h2("Etape 4 - Protection complementaire (a ne pas oublier)"))

story.append(h3("4.1 Reservation des noms de domaine"))
story.append(p("Achetez <b>maintenant</b> ces variantes pour eviter qu'un concurrent les "
               "prenne :"))
story.append(bullet([
    "dontroc.com, dontroc.ma, dontroc.app, dontroc.fr",
    "dontroc.org (futur association/ONG)",
    "donetroc.com, don-troc.com (variantes)",
]))
story.append(p("Cout total : environ <b>50-100 EUR/an</b> pour 6-8 domaines. "
               "Registrar recommande : <b>Namecheap</b> ou <b>OVH</b>."))

story.append(h3("4.2 Protection des reseaux sociaux"))
story.append(p("Reservez le handle <b>@dontroc</b> ou <b>@dontrocapp</b> sur :"))
story.append(bullet([
    "Instagram, Facebook, TikTok",
    "Twitter/X, LinkedIn, YouTube",
    "Threads, Bluesky",
]))
story.append(p("Meme si vous ne publiez pas tout de suite, cela empeche le squatting. "
               "Cout : 0 EUR, 30 minutes de travail."))

story.append(h3("4.3 Depot du logo (droit d'auteur)"))
story.append(p("Le logo et la charte graphique de DonTroc beneficient automatiquement du "
               "droit d'auteur des leur creation. Pour avoir une <b>preuve de date</b> "
               "(en cas de litige) :"))
story.append(bullet([
    "<b>Option gratuite</b> : commits Git horodates sur GitHub (deja fait)",
    "<b>Option officielle Maroc</b> : depot a l'OMPIC pour ~150 MAD",
    "<b>Option France</b> : enveloppe Soleau numerique INPI (~15 EUR, valable 5 ans)",
    "<b>Option blockchain</b> : Bernstein.io ou OriginStamp (~5 EUR/document, preuve immuable)",
]))

story.append(h3("4.4 Protection contre le copiage du code"))
story.append(p("Pour proteger le code source de DonTroc :"))
story.append(bullet([
    "Gardez le repo GitHub <b>prive</b> (vous l'avez deja)",
    "Ajoutez un fichier <b>LICENSE</b> proprietaire (\"All rights reserved\") a la racine",
    "Mettez un mirror prive sur <b>GitLab</b> en backup",
    "Conservez les commits historiques (ne supprimez jamais le repo) - c'est votre "
    "preuve d'anteriorite",
]))

# ================= PARTIE 5 : CHECKLIST =================
story.append(PageBreak())
story.append(h2("Checklist d'action - 30 jours"))

checklist = [
    ["#", "Action", "Cout", "Statut"],
    ["1", "Verifier disponibilite \"DonTroc\" sur OMPIC + WIPO", "0 MAD", "[ ]"],
    ["2", "Acheter dontroc.com / .ma / .app", "~500 MAD/an", "[ ]"],
    ["3", "Reserver @dontroc sur reseaux sociaux", "0 MAD", "[ ]"],
    ["4", "Preparer logo HD + description par classe", "0 MAD", "[ ]"],
    ["5", "Creer compte directmarques.ompic.ma", "0 MAD", "[ ]"],
    ["6", "Deposer la marque (4 classes : 9, 35, 38, 42)", "2 100 MAD", "[ ]"],
    ["7", "Ajouter LICENSE proprietaire au repo GitHub", "0 MAD", "[ ]"],
    ["8", "Backup du keystore Android (3 endroits chiffres)", "0 MAD", "[ ]"],
    ["9", "Mirror GitHub vers GitLab prive", "0 MAD", "[ ]"],
    ["10", "Declaration CNDP (donnees personnelles)", "0 MAD", "[ ]"],
    ["", "TOTAL Etape 1 (Maroc seul)", "~2 600 MAD", ""],
]
t = Table(checklist, colWidths=[1*cm, 9*cm, 3*cm, 2*cm])
t.setStyle(TableStyle([
    ("BACKGROUND", (0, 0), (-1, 0), PURPLE),
    ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
    ("FONTNAME", (0, 0), (-1, 0), "Helvetica-Bold"),
    ("FONTSIZE", (0, 0), (-1, -1), 9.5),
    ("GRID", (0, 0), (-1, -1), 0.25, colors.lightgrey),
    ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
    ("PADDING", (0, 0), (-1, -1), 5),
    ("BACKGROUND", (0, -1), (-1, -1), colors.HexColor("#FFE6D9")),
    ("FONTNAME", (0, -1), (-1, -1), "Helvetica-Bold"),
]))
story.append(t)

story.append(Spacer(1, 0.6*cm))
story.append(h2("Contacts utiles"))
story.append(bullet([
    "<b>OMPIC</b> : www.ompic.ma | tel : 0537 27 96 00 | info@ompic.ma",
    "<b>OMPI Madrid</b> : www.wipo.int/madrid",
    "<b>EUIPO</b> : euipo.europa.eu",
    "<b>OAPI</b> (Afrique francophone) : www.oapi.int",
    "<b>CNDP Maroc</b> (donnees perso) : www.cndp.ma",
    "<b>Maroc PME</b> (accompagnement gratuit AE) : www.marocpme.gov.ma",
]))

story.append(Spacer(1, 0.6*cm))
story.append(Paragraph(
    "<i>Ce guide est un document indicatif et ne se substitue pas a une consultation "
    "juridique professionnelle. Pour les depots complexes, consultez un cabinet de "
    "propriete intellectuelle agree par l'OMPIC.</i>", SMALL))
story.append(Spacer(1, 0.3*cm))
story.append(Paragraph(f"Document genere le {date.today().strftime('%d/%m/%Y')} "
                       f"pour {OWNER}.", SMALL))

doc.build(story)
print(f"OK -> {OUT}  ({OUT.stat().st_size // 1024} Ko)")

