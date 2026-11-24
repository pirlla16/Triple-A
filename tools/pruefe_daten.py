#!/usr/bin/env python3
"""Prüft die JSON-Daten von Triple A vor dem Veröffentlichen.

Aufruf (im Ordner workspace):  python3 tools/pruefe_daten.py
Bei Fehlern endet das Skript mit Code 1, und GitHub veröffentlicht nichts.
"""
import json, os, re, sys
from datetime import date

BASIS = os.path.join(os.path.dirname(__file__), "..", "src", "TripleA", "wwwroot", "data")
fehler, warnungen = [], []

def laden(rel):
    pfad = os.path.join(BASIS, rel)
    try:
        with open(pfad, encoding="utf-8") as f:
            return json.load(f)
    except FileNotFoundError:
        fehler.append(f"{rel}: Datei fehlt")
    except json.JSONDecodeError as e:
        fehler.append(f"{rel}: kein gültiges JSON ({e})")
    return None

DATUM = re.compile(r"^(0[1-9]|1[0-2])-(0[1-9]|[12]\d|3[01])$")

def pruefe_quelle(ort, q, pflicht=True):
    if q is None:
        if pflicht: fehler.append(f"{ort}: Quelle fehlt")
        return
    if not str(q.get("titel", "")).strip():
        fehler.append(f"{ort}: Quelle ohne Titel")
    url = q.get("url")
    if url and not str(url).startswith("https://"):
        warnungen.append(f"{ort}: Link beginnt nicht mit https:// ({url})")

def pruefe_textregeln(ort, liste):
    for i, r in enumerate(liste or []):
        if not str(r.get("text", "")).strip():
            fehler.append(f"{ort}[{i}]: Regel ohne Text")
        pruefe_quelle(f"{ort}[{i}]", r.get("quelle"), pflicht=False)

def pruefe_artregeln(ort, liste, art_ids):
    gesehen = set()
    for i, r in enumerate(liste or []):
        o = f"{ort}[{i}] ({r.get('artId')})"
        if r.get("artId") not in art_ids:
            fehler.append(f"{o}: unbekannte Art-ID")
        if r.get("artId") in gesehen:
            fehler.append(f"{o}: Art doppelt erfasst")
        gesehen.add(r.get("artId"))
        for feld in ("schonzeitVon", "schonzeitBis"):
            v = r.get(feld)
            if v is not None and not DATUM.match(str(v)):
                fehler.append(f"{o}: {feld} '{v}' nicht im Format MM-TT")
        if (r.get("schonzeitVon") is None) != (r.get("schonzeitBis") is None):
            fehler.append(f"{o}: Schonzeit braucht Beginn und Ende")
        for feld in ("schonmassCm", "fangLimiteProTag"):
            v = r.get(feld)
            if v is not None and (not isinstance(v, int) or v <= 0):
                fehler.append(f"{o}: {feld} muss eine positive ganze Zahl sein")
        pruefe_quelle(o, r.get("quelle"))

# Arten
arten = laden("arten.json") or []
art_ids = set()
for a in arten:
    if a.get("id") in art_ids:
        fehler.append(f"arten.json: ID '{a.get('id')}' doppelt")
    art_ids.add(a.get("id"))
    if not str(a.get("name", "")).strip():
        fehler.append(f"arten.json: Art '{a.get('id')}' ohne Namen")
    if a.get("bild") and not a.get("bildQuelle"):
        fehler.append(f"arten.json: Art '{a.get('id')}' hat ein Bild ohne Bildquelle")

# Meta
meta = laden("meta.json") or {}
try:
    stand = date.fromisoformat(meta.get("standIso", ""))
    if stand > date.today():
        warnungen.append("meta.json: standIso liegt in der Zukunft")
except ValueError:
    fehler.append("meta.json: standIso fehlt oder ist kein Datum (JJJJ-MM-TT)")

# Bund
bund = laden("bund.json") or {}
pruefe_textregeln("bund.json/allgemeineRegeln", bund.get("allgemeineRegeln"))
pruefe_artregeln("bund.json/artRegeln", bund.get("artRegeln"), art_ids)

# Kantone und Gewässer
gew_genutzt = set()
for k in laden("kantone/index.json") or []:
    kid = k.get("id")
    kanton = laden(f"kantone/{kid}.json")
    if kanton is None:
        continue
    o = f"kantone/{kid}.json"
    pruefe_textregeln(f"{o}/hinweise", kanton.get("hinweise"))
    pruefe_textregeln(f"{o}/koederRegeln", kanton.get("koederRegeln"))
    pruefe_artregeln(f"{o}/artRegeln", kanton.get("artRegeln"), art_ids)
    for gid in kanton.get("gewaesserIds", []):
        gew_genutzt.add(gid)
        g = laden(f"gewaesser/{gid}.json")
        if g is None:
            continue
        og = f"gewaesser/{gid}.json"
        if g.get("id") != gid:
            fehler.append(f"{og}: id '{g.get('id')}' passt nicht zum Dateinamen")
        if kid not in g.get("kantone", []):
            fehler.append(f"{og}: Kanton '{kid}' fehlt in 'kantone'")
        if g.get("nurVerweis") and not (g.get("verweis") or g.get("bezug")):
            fehler.append(f"{og}: Pachtrevier ohne Verweis")
        if not g.get("nurVerweis") and not g.get("bezug"):
            warnungen.append(f"{og}: kein Link zum Bezug von Karte/Patent")
        pruefe_textregeln(f"{og}/hinweise", g.get("hinweise"))
        pruefe_textregeln(f"{og}/koederRegeln", g.get("koederRegeln"))
        pruefe_artregeln(f"{og}/artRegeln", g.get("artRegeln"), art_ids)

for f in os.listdir(os.path.join(BASIS, "gewaesser")):
    if f.endswith(".json") and f[:-5] not in gew_genutzt:
        warnungen.append(f"gewaesser/{f}: wird von keinem Kanton verwendet")

for w in warnungen:
    print("WARNUNG:", w)
for f in fehler:
    print("FEHLER: ", f)
print(f"\n{len(fehler)} Fehler, {len(warnungen)} Warnungen")
sys.exit(1 if fehler else 0)
