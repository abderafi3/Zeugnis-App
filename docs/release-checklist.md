# Release-Checkliste (Sprint 12 — Härtung & Release)

Definition of Done aus `plan.md` §Sprint 12.

## Vor dem Build

- [ ] `dotnet test Zeugnis-App.slnx -c Release` — **alle Tests grün**, inkl. der Fidelitäts-Regression
      in `ZeugnisApp.Reporting.Tests` (plan §5.4 / §6, MVP-Release-Gate).
- [ ] Version in `Directory.Build.props` (`<Version>`) und `build/ZeugnisApp.iss` (`#define AppVersion`) angeglichen.
- [ ] `docs/datenschutzkonzept.md`, `docs/schnellstart.md`, `docs/neues-formular-workflow.md` von der Schule gegengelesen.
- [ ] Offene Punkte für die Schule geklärt (siehe unten).

## Build & Paket

- [ ] `pwsh build/publish.ps1` — self-contained `win-x64` nach `publish/win-x64`.
- [ ] `iscc build/ZeugnisApp.iss` (Inno Setup 6+) — erzeugt `ZeugnisApp-Setup-<version>.exe`.
- [ ] **Authenticode-Signatur** des Setups **und** von `ZeugnisApp.exe` mit einem OV-/Standard-Code-Signing-Zertifikat
      (`signtool sign /fd sha256 /tr <RFC3161-URL> /td sha256 …`).
      **Falls die Schule das Zertifikat ablehnt:** schriftliche Abnahme, dass die SmartScreen-Warnung
      beim ersten Start akzeptiert wird, ist Teil der Übergabe.
- [ ] Installer auf einem sauberen Windows 10 **und** Windows 11 testen:
      Installation → erster Start → Wiederherstellungsschlüssel wird angezeigt → Schule einrichten →
      Klasse/Kind anlegen → Zeugnis erzeugen → Deinstallation (Nutzerdaten bleiben erhalten).

## Manuelle Prüfungen (nicht automatisiert)

- [ ] **Sicherung/Wiederherstellung über zwei Geräte:** Sicherung auf Gerät A erstellen → auf Gerät B
      (bzw. neues Windows-Profil) wiederherstellen → mit Wiederherstellungsschlüssel entsperren → Daten vollständig.
- [ ] **Barrierefreiheit:** Tab-Navigation durch alle Screens; Screenreader (Narrator) liest die
      Symbol-Schaltflächen vor; Kontrast der Statusfarben bei hellem und dunklem Windows-Design.
- [ ] **Echte Schulvorlagen:** die 7 offiziellen `.docx` nach `docs/neues-formular-workflow.md`
      einbinden und je ein Test-Zeugnis optisch prüfen.
- [ ] **Zeichenbudgets** (`MaxZeichen`) an den echten Vorlagen kalibrieren (aktuell Schätzwerte).

## Offene Punkte / bewusste Auslassungen (MVP)

- Optionaler App-PIN für geteilte Geräte — nicht umgesetzt (plan: optional).
- Theme folgt dem Windows-Design; erste Sekunde ggf. im Hell-Design, bevor umgeschaltet wird.
- Der Wiederherstellungsschlüssel wird angezeigt/kopierbar, kein formatierter Ausdruck.
- `ZieldifferentHinweis`-Wortlaut und die Floskel-Platzhalter-Liste mit der Schule bestätigen.

## Übergabe

- [ ] Signierter (oder abgenommener) Installer.
- [ ] `docs/` als PDF an die Schule.
- [ ] Kurze Einweisung der zuständigen Lehrkraft.
