#!/usr/bin/env python3
# Regenerates the bundled first-run phrase seed from the school's real spreadsheet.
#   python build/generate-floskeln-seed.py
# Reads ./data/Floskeln.xlsx (git-ignored, supplied by the school) and writes the
# tab-separated embedded resource that FloskelSeed / DatabaseInitializer load on first run.
# Keep the Gruppe/Fach -> Bereich and Jahrgang -> Stufe maps in sync with
# ZeugnisApp.Domain.Floskeln.FloskelBereichKatalog and ImportValueParser.TryParseStufe.
import io, os, re, zipfile
from collections import Counter
from xml.etree import ElementTree as ET

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "data", "Floskeln.xlsx")
OUT = os.path.join(ROOT, "src", "ZeugnisApp.Infrastructure", "Persistence", "SeedData", "floskeln.tsv")

z = zipfile.ZipFile(SRC); ns="{http://schemas.openxmlformats.org/spreadsheetml/2006/main}"
shared=[]; root=ET.fromstring(z.read("xl/sharedStrings.xml"))
for si in root.findall(f"{ns}si"): shared.append("".join(t.text or "" for t in si.iter(f"{ns}t")))
def cn(r): return re.match(r"[A-Z]+", r).group()
rows=[]
for s in sorted(n for n in z.namelist() if re.match(r"xl/worksheets/sheet\d+\.xml",n)):
    r=ET.fromstring(z.read(s))
    for row in r.iter(f"{ns}row"):
        d={}
        for c in row.findall(f"{ns}c"):
            v=c.find(f"{ns}v")
            if v is None: continue
            d[cn(c.get("r"))]=shared[int(v.text)] if c.get("t")=="s" else v.text
        if d: rows.append(d)
data=rows[1:]

FACH = {
 "D":"Deutsch","SG":"Sprachgebrauch","LE":"Lesen","RS":"Rechtschreiben",
 "M":"Mathematik","E":"Englisch","SU":"Sachunterricht","SP":"Sport",
 "MU":"Musik","KU":"Kunst","ER":"Evangelische Religionslehre","KR":"Katholische Religionslehre",
}
def bereich(gruppe, fach):
    g=(gruppe or "").strip().upper(); f=(fach or "").strip()
    if g=="ASV": return "Arbeits- und Sozialverhalten"
    if g=="FSP": return "Förderschwerpunkt"
    if g=="ZB":  return "Bemerkungen"
    if f: return FACH.get(f.upper(), f)
    return ""
def stufe(jg):
    t=(jg or "").strip().upper()
    if t in ("E1","1"): return 1
    if t in ("E2","2"): return 2
    if t in ("E3","3","03"): return 3
    if t in ("E4","4","04"): return 4
    return 0
def gruppe_band(s):
    if s in (1,2): return "Eingangsstufe"
    if s in (3,4): return "Folgestufe"
    return "Beide"
def niveau(e):
    t=(e or "").strip()
    return int(t) if t.isdigit() and 0<=int(t)<=5 else 0

seen=set(); out=[]
for r in data:
    text=(r.get("B") or "").strip()
    if not text: continue
    kz=(r.get("A") or "").strip()
    st=stufe(r.get("F"))
    b=bereich(r.get("C"), r.get("D"))
    nv=niveau(r.get("E"))
    key=(kz, text)
    if key in seen: continue
    seen.add(key)
    out.append((kz, text, b, str(st), str(nv), gruppe_band(st)))

os.makedirs(os.path.dirname(OUT), exist_ok=True)
with io.open(OUT,"w",encoding="utf-8",newline="\n") as fh:
    fh.write("Kuerzel\tText\tBereich\tStufe\tNiveau\tKlassengruppe\n")
    for t in out:
        assert all("\t" not in x and "\n" not in x for x in t), t
        fh.write("\t".join(t)+"\n")


print("rows:", len(out))
print("bereich:", dict(Counter(o[2] for o in out)))
print("stufe:", dict(Counter(o[3] for o in out)))
print("niveau:", dict(Counter(o[4] for o in out)))
print("sample:")
for o in out[:3]+out[70:72]: print("  ", o)
