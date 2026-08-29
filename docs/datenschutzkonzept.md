# Datenschutzkonzept — Zeugnis-App

**Grundschule Wehringhausen, Hagen (NRW)** · Stand: Version 1.0 · Basis: Anforderungsdokument vom 16.08.2026

Dieses Konzept beschreibt die technischen und organisatorischen Maßnahmen (TOM) der Zeugnis-App
gemäß Art. 32 DSGVO. Es ist als Anlage zum Verzeichnis von Verarbeitungstätigkeiten der Schule
gedacht.

## 1. Verarbeitete Daten

| Kategorie | Beispiele | Zweck |
|---|---|---|
| Stammdaten Kinder | Vor-/Nachname, Geburtsdatum, Geschlecht, Klasse, Förderstatus, Förderschwerpunkt | Erstellung der Zeugnisse |
| Leistungs- und Beobachtungsdaten | Noten, Freitexte, Empfehlung, Bemerkungen | Erstellung der Zeugnisse |
| Fehlzeiten | Fehlstunden gesamt / unentschuldigt je Halbjahr | Ausweis auf dem Zeugnis |
| Schul-/Klassendaten | Schulname, Anschrift, Schulleitung, Klassenleitung, Termine | Kopf-/Fußdaten der Zeugnisse |

Es werden **keine** besonderen Kategorien nach Art. 9 DSGVO über den Förderstatus hinaus erfasst;
der Förderschwerpunkt ist ein Freitextfeld und nur bei sonderpädagogischer Unterstützung auszufüllen.

## 2. Verarbeitungsort und Verantwortlichkeit

- Die Anwendung läuft **vollständig lokal** auf einem Einzelgerät der Schule. Es gibt **keinen
  Server, keine Cloud, keine Netzwerkverbindung** und damit **keinen Auftragsverarbeiter**.
- Verantwortliche Stelle im Sinne der DSGVO ist die Schule.
- Der Quellcode enthält keine Telemetrie; die CI-Pipeline prüft, dass keine Netzwerk-APIs eingebunden werden.

## 3. Technische Maßnahmen

| Maßnahme | Umsetzung |
|---|---|
| Verschlüsselung im Ruhezustand | Die gesamte Datenbank (`daten.zeugnisapp`) ist mit **SQLCipher (AES-256)** verschlüsselt. |
| Schlüsselverwaltung | Ein zufälliger 32-Byte-Schlüssel wird pro Gerät erzeugt und mit **Windows DPAPI** an das Windows-Benutzerkonto gebunden gespeichert (`daten.key`). Auf dem eigenen Gerät ist keine Passworteingabe nötig. |
| Wiederherstellungsschlüssel | Der Schlüssel wird bei der Ersteinrichtung **einmalig angezeigt** (kopier-/ausdruckbar). Ohne DPAPI-Blob **und** ohne diesen Schlüssel sind die Daten nicht mehr lesbar – es gibt **keinen Zugang „durch die Hintertür“**. |
| Zugriffsschutz | Bindung an das Windows-Benutzerkonto über DPAPI. Ein zusätzlicher App-PIN für geteilte Geräte ist als Option vorgesehen (noch nicht umgesetzt). |
| Integrität / Ausfallsicherheit | SQLite im WAL-Modus (atomare Commits); Autospeichern in der Oberfläche. |
| Sicherung | Verschlüsselte Ein-Datei-Sicherung (`.zeugnisapp-backup`, Datenbank + Vorlagen + SHA-256-Prüfsummen). Wiederherstellung nur mit dem Wiederherstellungsschlüssel. |
| Löschkonzept | Löschungen sind zunächst „weich“ (Papierkorb). Nach Ablauf der Aufbewahrungsfrist (Standard **30 Tage**, in den Einstellungen konfigurierbar) werden sie beim Programmstart **endgültig** entfernt. |
| Protokollierung | Lokale, rollierende Log-Dateien (14 Tage) ohne personenbezogene Leistungsdaten – nur technische Ereignisse für die Fehlersuche. |

## 4. Organisatorische Maßnahmen

- Das Gerät ist mit einem Windows-Kennwort geschützt; automatische Bildschirmsperre aktiv.
- Der Wiederherstellungsschlüssel wird an einem vom Gerät getrennten, verschlossenen Ort aufbewahrt
  (z. B. Tresor im Sekretariat).
- Sicherungen werden regelmäßig erstellt und getrennt vom Gerät aufbewahrt (z. B. USB-Stick im Tresor).
- Zugriff nur durch die zuständige Lehrkraft / Klassenleitung.

## 5. Aufbewahrung und Löschung

- Zeugnisdaten werden gemäß den schulrechtlichen Aufbewahrungsfristen der Schule geführt.
- **Abgänger:innen:** Nach Verlassen der Schule und Ablauf der gesetzlichen Aufbewahrungsfrist werden
  Kind und zugehörige Zeugnisse über den Papierkorb gelöscht; nach der Aufbewahrungsfrist des
  Papierkorbs erfolgt die endgültige Entfernung automatisch.
- Sicherungsdateien, die abgelaufene Daten enthalten, sind entsprechend zu vernichten.

## 6. Betroffenenrechte

Auskunft, Berichtigung und Löschung erfolgen durch die Schule über die Oberfläche der Anwendung
(Bearbeiten / Löschen / Papierkorb). Da keine automatisierte Einzelentscheidung stattfindet und
keine Weitergabe an Dritte erfolgt, entfallen weitergehende Pflichten.

## 7. Offene Punkte

- Optionaler App-PIN für geteilte Geräte ist noch nicht implementiert.
- Die genaue Aufbewahrungsfrist ist mit der Schulleitung abzustimmen und im Feld
  „Papierkorb: Aufbewahrung (Tage)" zu hinterlegen.
