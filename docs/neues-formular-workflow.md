# Workflow: Die Schule erhält ein neues offizielles Zeugnisformular

Wenn das Land oder die Schulaufsicht ein neues oder geändertes Word-Zeugnisformular herausgibt,
wird es so in die Zeugnis-App übernommen. Schritte 1–2 erfordern derzeit noch Word-Kenntnisse
(Entwicklertools); Schritte 3–5 macht die Lehrkraft selbst.

## 1. Inhaltssteuerelemente in die Word-Datei einfügen (einmalig je Formular)

1. Die offizielle `.docx` in Word öffnen.
2. Registerkarte **Entwicklertools** einblenden (Datei → Optionen → Menüband anpassen → „Entwicklertools").
3. An **jeder Stelle, an der Daten erscheinen sollen**, den Cursor platzieren und ein
   **Inhaltssteuerelement „Nur-Text"** einfügen.
4. Für jedes Steuerelement über **Eigenschaften** ein stabiles **Tag** vergeben, z. B.
   `Kind_Name`, `Note_Deutsch`, `Freitext_Sozialverhalten`, `Schule_Name`.
   Das Tag ist der technische Schlüssel; der optionale „Titel" ist der Klartext, der in der App
   in der Auswahlliste erscheint.
5. **Layout, Schriftarten, Tabellen und Logo nicht verändern.** Die App ersetzt später nur den
   Text innerhalb der Steuerelemente.
6. Datei speichern.

> Tipp: Als Vorlage kann die von der App beim ersten Start erzeugte Platzhalter-Datei im Ordner
> `%LOCALAPPDATA%\Zeugnis-App\Vorlagen` dienen – dort sind bereits passende Tags vergeben.

## 2. (Nur bei einem neuen Formular-Typ) Formular in der App anlegen

- **Formulare → Neues Formular**.
- Anzeigename, Jahrgang, „Gilt für" (Halbjahr), Schalter (Notenblock / Empfehlung / Versetzung /
  zieldifferent), Fächerliste und Freitextfelder eintragen.
- Bei einem geänderten bestehenden Formular diesen Schritt überspringen.

## 3. Word-Vorlage hochladen

- **Formulare → das Formular öffnen → „Vorlage hochladen …"**.
- Die vorbereitete `.docx` auswählen.

## 4. Felder zuordnen

- Der Assistent zeigt links jedes App-Feld (Schule, Klasse, Kind, Zeugnis, Noten, Freitexte) und
  rechts eine Auswahlliste der in der Datei gefundenen Inhaltssteuerelemente.
- Bekannte Tags werden **automatisch vorbelegt**. Fehlende Zuordnungen (Kind-Name, jedes Fach,
  jedes Freitextfeld) müssen gesetzt werden – erst dann lässt sich „Übernehmen" klicken.
- **Übernehmen** kopiert die Datei in den App-Vorlagenordner und speichert die Zuordnung.

## 5. Prüfen

- Ein Test-Zeugnis für ein Kind erzeugen (**Zeugnisse → öffnen → Zeugnis erzeugen**) und das
  entstandene Word-Dokument optisch mit dem Original vergleichen.
- Die automatische Regressionsprüfung (`ZeugnisApp.Reporting.Tests`) stellt sicher, dass außerhalb
  der Steuerelemente **nichts** verändert wurde.

## Bei Problemen

- „In der Datei wurden keine Inhaltssteuerelemente gefunden" → Schritt 1 wurde nicht ausgeführt
  oder die Tags fehlen.
- „Die Datei ist keine gültige Word-Datei (.docx)" → es handelt sich um eine alte `.doc`-Datei;
  in Word als `.docx` speichern.
