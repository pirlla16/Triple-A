# Triple A

Private Fischerei-App für Android (als PWA): Fischlexikon und Fischereiregeln der Kantone Aargau und Luzern.

- Technik: Blazor WebAssembly (.NET 10), installierbar als PWA, funktioniert offline
- Hosting: GitHub Pages, Adresse **https://pirlla16.github.io/Triple-A/**
- Daten: JSON-Dateien unter `src/TripleA/wwwroot/data`

## Ordnerstruktur

```
_einrichtung/deploy.yml         Wird nach .github/workflows/ verschoben; baut und veröffentlicht die App bei jedem Push auf "main"
src/TripleA/                    Die App
  Layout/                       Kopfzeile und Reiter unten
  Pages/                        Seiten: Fischen, Tiere, Art-Detail
  Shared/                       Wiederverwendbare Bausteine
  Models/                       Datenmodell (Art, Kanton, Gewässer, Regeln)
  Services/                     Laden der Daten, Zusammensetzen Bund → Kanton → Gewässer
  wwwroot/data/                 Alle Inhalte als JSON
    arten.json                  Lexikon (20 Arten)
    bund.json                   Vorgaben des Bundes
    kantone/                    index.json + eine Datei pro Kanton
    gewaesser/                  Eine Datei pro Gewässer (kann mehreren Kantonen gehören)
    meta.json                   Version und Datenstand
```

## Einmalige Einrichtung

1. **.NET 10 SDK** installieren: https://dotnet.microsoft.com/download
2. **Git** installieren: https://git-scm.com/download/win
3. Die Datei `_einrichtung/deploy.yml` nach `.github/workflows/deploy.yml` verschieben
   (Ordner `.github` und darin `workflows` anlegen). Sie baut und veröffentlicht die App bei jedem Push.
   Danach kann der Ordner `_einrichtung` gelöscht werden.
4. Im Ordner `workspace` eine Eingabeaufforderung öffnen und das Repository verbinden:

   ```
   git init -b main
   git add .
   git commit -m "Grundgerüst Triple A"
   git remote add origin https://github.com/pirlla16/Triple-A.git
   git push -u origin main
   ```

5. Auf GitHub: **Settings → Pages → Build and deployment → Source: "GitHub Actions"** wählen.
6. Unter **Actions** prüfen, ob der Lauf «Veröffentlichen auf GitHub Pages» grün ist (2–3 Minuten).
7. Auf dem Handy in Chrome **https://pirlla16.github.io/Triple-A/** öffnen → Menü (⋮) → **«Zum Startbildschirm hinzufügen»**.

## Lokal starten

```
dotnet run --project src/TripleA
```

Die Adresse steht in der Ausgabe (z.B. http://localhost:5xxx). Offline-Funktion und Installation gibt es nur in der veröffentlichten Version.

## Daten ändern

1. JSON-Datei unter `src/TripleA/wwwroot/data` anpassen. Jede Regel bekommt eine `quelle` mit Titel, Link und Stand.
2. Wenn ein Gewässer vollständig erfasst ist: `"erfasst": true` setzen (sonst zeigt die App einen Hinweis).
3. In `meta.json` Version und Stand erhöhen.
4. `git add . && git commit -m "Daten aktualisiert" && git push`

Nach dem Veröffentlichen holt sich die App die neue Version beim nächsten Öffnen; sichtbar wird sie spätestens nach einem zweiten Öffnen.

### Ebenen der Regeln

Pro Art wird zuerst die Regel des Bundes genommen, dann vom Kanton und zuletzt vom Gewässer überschrieben. Leere Felder werden von der Ebene darunter übernommen.

```json
{
  "artId": "hecht",
  "schonmassCm": 45,
  "schonzeitVon": "01-01",
  "schonzeitBis": "04-30",
  "ganzjaehrigGeschont": null,
  "fangLimiteProTag": 2,
  "bemerkung": null,
  "quelle": { "titel": "…", "url": "https://…", "stand": "2027-01-01" }
}
```

Die Zahlen im Beispiel sind Platzhalter, keine echten Vorschriften.

## Hinweis

Ohne Gewähr. Massgebend sind die offiziellen Vorschriften von Bund und Kanton.
