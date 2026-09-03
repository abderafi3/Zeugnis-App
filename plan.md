# Zeugnis-App — Konzept & Umsetzungsplan

**Grundschule Wehringhausen, Hagen (NRW)**
Technical concept, architecture, technology choices, UI/UX design and sprint plan
Version 1.1 · Basis: Anforderungsdokument vom 16.08.2026
Changelog v1.0 → v1.1: see §13 (Review Notes) — formatting-fidelity claims made precise, overflow detection reframed, .NET 10 LTS, SQLCipher/EF Core integration caveat, Sprint 7/9 reordered, buffer sprint added, code-signing / passphrase-recovery / DSGVO-concept deliverables added.

---

## 1. Goals & Scope

The Zeugnis-App is an **offline Windows desktop application** for primary-school teachers. It manages classes, students, absences and reusable text snippets, and its core value proposition is filling the school's **real, official Word report-card templates** with data — without altering formatting, tables, fonts or the school logo — and producing a ready-to-print `.docx` file.

Design constraints that drive every technology decision below:

| Constraint | Implication |
|---|---|
| Runs fully offline, no server, no cloud | Local, embedded, single-file data storage; no client/server split |
| Non-technical end users (teachers) | UI must be simple, forgiving, and guided; heavy use of previews and inline validation |
| Must preserve official Word formatting **exactly** | Cannot "rebuild" documents visually — must edit the real OOXML of the real files |
| Personal data (children's data) is processed | Data-protection (encryption, access control) must be part of the architecture, not bolted on later |
| Single teacher, single laptop today; multi-device "maybe later" | Architecture should not block a future sync layer, but must not delay MVP |

Everything in the German requirements document (Kapitel 1–9) is treated as in-scope for the plan; the "Offene Punkte" (Kapitel 9) are scheduled explicitly as their own sprints rather than left informal.

---

## 2. Technology Stack

| Layer | Technology | Why |
|---|---|---|
| **UI shell** | **.NET 10 (LTS) + WPF**, MVVM (CommunityToolkit.Mvvm) | Mature, stable, best tooling for a solo/small-team Windows desktop build; large ecosystem of ready-made controls. **.NET 10** (LTS, released Nov 2025) is targeted rather than .NET 8, whose LTS window closes around the time this project ships (~Q1 2027); .NET 10 also carries active WPF investment. WinUI 3 was considered but rejected for MVP: less mature packaging/tooling, smaller ecosystem for the Office-document work this app depends on. |
| **UI styling / design language** | **WPF-UI** (Fluent Design controls, Mica/Acrylic, Windows 11 look) + custom design tokens | Gives the app a modern, native-feeling Windows UI out of the box (rounded corners, Fluent icons, light/dark mode) without hand-building a design system. |
| **Local data storage** | **SQLite** (single `.zeugnisapp` file) via **EF Core** (`Microsoft.EntityFrameworkCore.Sqlite`) | Matches the "one file, no server" requirement exactly; transactional, well understood, trivial to back up (it's just a file); EF Core gives migrations, which matters because the schema *will* evolve (Sprint 10–11 add fields). |
| **Word template filling** | **DocumentFormat.OpenXml SDK** (Microsoft's official OOXML library), driven by **Content Controls (Structured Document Tags)** placed in the official templates | This is the single most important technical decision. The OpenXML SDK edits the real XML inside the `.docx` **in place** — it never re-renders or re-lays-out the document. Only the *text* inside a content control is replaced; the run's formatting properties (`rPr`), and every part of the document outside the content controls, are left as-is, so **there is no layout reflow** and fonts, tables, logo and page layout stay visually identical. (Note: the SDK re-serialises the whole package on save, so the output is *not* byte-for-byte identical to the input — attribute order and whitespace can differ; fidelity is instead proven by the canonical-XML regression tests in §5.4.) Content Controls give named, typed insertion points ("StudentName", "Grade_Deutsch", …) that a non-developer template-editor UI can bind to later (Sprint 10). Word interop (automating an installed Word application) was rejected: it requires Word installed on every teacher's laptop, is slow, and is fragile in unattended/offline use. |
| **Excel import/export** | **ClosedXML** | Free, MIT-style license, no Excel installation required, reads/writes `.xlsx` reliably; used both for class-list import and phrase-library import. |
| **Word (as data source) import** | **DocumentFormat.OpenXml SDK** (same library, read path) | Reused for reading class lists that arrive as `.docx` tables. |
| **CSV import** | **CsvHelper** | De-facto standard, handles German locale (`;` separators, `dd.MM.yyyy` dates) reliably. |
| **Clipboard import** | Native WPF clipboard access + shared "tabular text parser" | Reuses the same column-detection engine as Excel/CSV import (see §6). |
| **Data protection at rest** | **SQLCipher** for full-database AES-256 encryption, app-level PIN/passphrase | Encrypts the single data file transparently; if the laptop or file is lost, the data is unreadable without the passphrase. Windows DPAPI (`ProtectedData`, `CurrentUser` scope) is used additionally to protect the stored key material to the Windows user account, so the teacher isn't forced to re-enter a password on every launch on their own machine. **Integration caveat:** `Microsoft.EntityFrameworkCore.Sqlite` bundles `SQLitePCLRaw.bundle_e_sqlite3`; adding `bundle_e_sqlcipher` alongside it causes a duplicate-provider clash at runtime. The working combination is `Microsoft.EntityFrameworkCore.Sqlite.Core` + `Microsoft.Data.Sqlite.Core` + `SQLitePCLRaw.bundle_e_sqlcipher`, with `PRAGMA key` issued on the connection (via a `DbConnection` interceptor) before any command runs. This is spiked in Sprint 0 alongside the OOXML spike so it is not discovered in Sprint 11. |
| **Backup / restore** | Custom "export bundle" (`.zeugnisapp-backup`, a zip of the encrypted DB + template files, itself checksum-verified) | Keeps backup as simple as "save this one file to a USB stick / OneDrive folder", fitting the offline, non-technical-user context. The DB is copied into the bundle via SQLite's **online-backup API / `VACUUM INTO`** (with a WAL checkpoint first), never a raw file copy of a live WAL-mode database, which can produce a corrupt restore. |
| **Logging / diagnostics** | **Serilog**, rolling local log files only (no telemetry, per offline/privacy requirement) | Needed for support ("it crashed while generating a report") without violating the no-cloud constraint. |
| **Testing** | **xUnit** for logic/services, **OpenXML round-trip tests** for the document engine (generate → reopen → assert content-control values are correct **and** a *canonical-XML* comparison of everything outside the content controls is unchanged — see §5.4), manual UAT with real teachers for UI/UX | The document-generation engine is the highest-risk part of the system and gets the most rigorous automated testing. |
| **Packaging / installer** | **.NET self-contained publish** + **Inno Setup** (or MSIX as a stretch goal), **Authenticode code-signing certificate** | Produces a single installer `.exe` a teacher can run without installing .NET separately. The installer and app `.exe` must be **code-signed** (OV/standard certificate, ~€200–400/yr) or Windows SmartScreen warns on every teacher machine at first run; if the school declines the certificate cost, that trade-off is accepted in writing. MSIX considered for future Microsoft Store / auto-update distribution. |
| **Version control / CI** | Git + GitHub Actions (build, run unit tests, produce installer artifact on tag) | Standard, low-overhead CI for a small solo/duo project. |

**Licensing:** every dependency above is under a permissive, commercially-usable licence (OpenXML SDK, ClosedXML, WPF-UI, CommunityToolkit.Mvvm — MIT; Serilog, SQLitePCLRaw — Apache-2.0; CsvHelper — MS-PL/Apache; Inno Setup — free for commercial use). No copyleft (GPL/LGPL) obligations are introduced. This is noted explicitly because the plan doubles as the scoping reference between a freelance developer and the school.

---

## 3. High-Level Architecture

Layered, single-process desktop architecture — no network layer at all:

```
┌─────────────────────────────────────────────────────────┐
│  Presentation Layer (WPF, MVVM, WPF-UI Fluent controls)  │
│  Views + ViewModels, one per screen (see §7)              │
└───────────────────────────┬────────────────────────────┘
                             │ commands / bindings
┌───────────────────────────▼────────────────────────────┐
│  Application / Service Layer                             │
│  - ClassService, StudentService, PhraseService            │
│  - ReportCardService, AbsenceService, TemplateService      │
│  - ImportService (Excel/Word/CSV/Clipboard)                │
│  - ReportGenerationEngine (OpenXML fill + overflow check)  │
│  - BackupService, SecurityService                          │
└───────────────────────────┬────────────────────────────┘
                             │ EF Core DbContext
┌───────────────────────────▼────────────────────────────┐
│  Data Layer                                               │
│  SQLite (SQLCipher-encrypted), single .zeugnisapp file     │
│  + a Templates folder holding the official .docx files     │
└─────────────────────────────────────────────────────────┘
```

Key architectural principles:

- **No UI logic in services, no business logic in views.** Every screen is testable without a UI by testing its ViewModel + Service.
- **The report-generation engine is isolated** behind a single interface (`IReportGenerationEngine.Generate(ReportCard) → GeneratedDocumentResult`) so it can be unit-tested against real template files independent of the rest of the app, and so the Sprint 10 "custom template" work only touches this one module.
- **Import and export share one tabular-parsing core** (column detection, row validation) so Excel/Word/CSV/Clipboard import and the Excel phrase-import all reuse one component instead of four parallel implementations.
- **Everything is local-first**: every service call is synchronous-fast (SQLite on local disk), so the UI never needs spinners for data operations — only for import parsing and document generation, which get explicit progress UI.

---

## 4. Data Model (Core Entities)

| Entity | Key Fields | Notes |
|---|---|---|
| **SchoolSettings** | Name, Zusatzbezeichnung, Adresse, **Schulnummer**, Schulleitung, Schuljahr, Halbjahr, Zeugnisausgabedatum, Konferenzdatum, Datum Unterrichts-Wiederbeginn | Singleton row. Pre-seeded on first run with Grundschule Wehringhausen's identity (Schulnummer 100189, Minervastr. 37, 58089 Hagen, Schulleitung J. Rohn). |
| **Klasse** | Name, Jahrgang (1–4, now a *fallback hint*), Klassenleitung, Zeugnisdatum (nullable → falls back to SchoolSettings), Wochenstundenplan (Mo–Fr) | Cascade-deletes Kinder/Fehlzeiten/Zeugnisse with confirmation. The school runs jahrgangsübergreifende Klassen (E1, E2, E3, F1 …) that mix Schulbesuchsjahr 1–4, so `Klasse.Jahrgang` no longer drives report/phrase choice — `Kind.Schulbesuchsjahr` does, with `Klasse.Jahrgang` used only when a child's year is unset. |
| **Kind (Student)** | Vorname, Nachname, Geburtsdatum, Geschlecht (m / w / d), KlasseId, **Schulbesuchsjahr (1–4, 0 = unset)**, Förderstatus (enum: Regel / zielgleich / zieldifferent-Lernen / zieldifferent-GE), Förderschwerpunkt (Freitext), ManuellesFormularId (nullable) | `EffektivesSchulbesuchsjahr` (own year, else class year) drives auto-template-suggestion and the phrase Stufe filter. `Geschlecht = d` ("divers") has no grammatical pronoun form — the placeholder engine (§ Sprint 4) falls back to repeating the child's name for every placeholder (app `[…]` and school `&m%w&` alike). **Open question for the school** — the school's real phrase file has only m/w. |
| **Floskel (Phrase)** | Text, Kürzel (school's stable id, e.g. `#D11.00` — de-dupes re-import), Bereich (free text), Stufe (1–4, 0 = alle), **Niveau (1–5 differentiation level, 0 = neutral)**, Klassengruppe (E/F/beide) | Text stores placeholders in either the app syntax (`[Name]`, `[sein/ihr]`) or the school's native syntax (`$Vorname$` / `$VORNAME$`, `&Er%Sie&`, `&seine/ihre&`, `&Pate%Patin&`) — both resolved by the placeholder engine. Grundschule Wehringhausen's real ~2 300-phrase file is bundled and seeded on first run. |
| **Formular (Template)** | Anzeigename, Überschrift, Fächerliste, Freitextfelder (name + Hinweistext), Schalter: Notenblock, begründeteEmpfehlung, Versetzungsvermerk, zieldifferent, WordFilePath, FieldMappingJson | `FieldMappingJson` = mapping of app fields → Content Control tags, populated in Sprint 6/10 |
| **Fehlzeit (Absence)** | KindId, Schuljahr/Halbjahr, StundenGesamt, StundenUnentschuldigt | Simple table variant; calendar variant modeled but unlinked in UI |
| **Zeugnis (ReportCard)** | KindId, FormularId, Schuljahr, Halbjahr, Freitexte (per field), Noten (per Fach), Empfehlung + Begründung, Fehlstunden (copied, editable), Versetzungsvermerk-Text, Bemerkungen, Fertiggestellt (bool), GeneratedFilePath (nullable) | Central transactional entity. **Unique constraint** on (KindId, FormularId, Schuljahr, Halbjahr) — one report card per child per form per term. |
| **AuditTrail / RecycleBin** (Sprint 11) | EntityType, EntityId, SnapshotJson, DeletedAt | Backs the undo/recycle-bin feature. Retention window for permanent purge is configurable (default 30 days) and documented in the DSGVO concept (§ Sprint 12). |

**Multi-year use (open question for the school).** A teacher reusing the app in a following school year will expect last year's classes/children to carry forward (class promotion E1→E2, Schuljahr archived) without re-importing. Whether a **year-rollover / class-promotion** feature is in scope for MVP or deferred is left for the school to decide; the schema (`Schuljahr` on `Klasse`/`Zeugnis`/`Fehlzeit`) does not block adding it later. Listed again under §10 "Beyond Sprint 12".

---

## 5. Core Technical Approach: Filling the Official Word Templates

This is the highest-risk, highest-value part of the system, so it gets its own section.

### 5.1 One-time preparation per official template

Done by the developer for the 7 existing forms, later doable by a teacher for new ones (Sprint 10): open the real `.docx` in Word, and at each place data must appear, insert a **Content Control** (Rich Text or Plain Text, via Word's Developer tab) with a stable **Tag**, e.g. `tag="StudentName"`, `tag="Note_Deutsch"`, `tag="Freitext_Sozialverhalten"`. This is a Word-native feature — it does not touch layout, fonts, or the logo.

Sprint 0 must also **confirm the supplied templates are true `.docx` (OOXML)**, not legacy binary `.doc` — the latter needs a one-time conversion step that is otherwise unbudgeted.

### 5.2 Generation time

The app:
- Opens the original `.docx` as a `WordprocessingDocument` via OpenXML SDK (never through Word itself).
- Walks the document's `SdtElement` (content control) tree, matches each `Tag` against the `Zeugnis` data via the template's `FieldMappingJson`, and replaces the control's inner text.
- **Run handling:** a content control often holds *several* runs (spell-check splits a sentence, tracked formatting, etc.). The engine collapses the control's content to a **single run that carries the first original run's `rPr`** and sets the new text on it — it does not blindly write into run 0 and leave stale runs behind. Formatting (font, size, bold) is preserved because `rPr` is copied, not regenerated.
- Leaves every part of the document outside the content controls (tables, images, headers/footers, `styles.xml`, `numbering.xml`, theme, media) semantically untouched. On save the SDK re-serialises the package, so the bytes differ from the original even where nothing semantic changed; fidelity is verified by §5.4, not by a raw file hash.
- Saves the result to a new file at the location the teacher chooses, and opens it with the OS default handler (normally Word) for review. *(If the teacher has no Word/OOXML-capable viewer installed the file still generates correctly; only the auto-open preview degrades. A Word install is assumed in practice for a school.)*

### 5.3 Overflow protection

The OpenXML SDK does not render or lay out the document, so the space actually available for a text field on the printed form **cannot be measured automatically**. Instead each long free-text field has a **configurable per-field budget** (max characters, and/or max lines) that is **calibrated manually once per template**: the developer generates test documents with progressively longer text and records the largest amount that still fits the official form without pushing a page. At editing time (§7), text exceeding the budget is highlighted orange so the teacher can shorten it — writing is **never blocked**. Re-calibration is part of the Sprint 7 work and is repeated whenever a template changes.

### 5.4 Automated regression test (the formatting-fidelity gate)

For every one of the 7 templates, a test generates a report from fixed sample data and asserts:
- **(a)** all expected values appear, correctly, in the right content controls (assert on the generated document's SDT contents);
- **(b)** everything *outside* the content controls is unchanged versus the pristine original, compared by **canonical XML**, not a byte or naïve hash: each relevant part (`document.xml` with all SDT inner content masked out, plus `styles.xml`, `numbering.xml`, `theme/*`, `settings.xml`, and a media-part inventory with hashes) is loaded, attributes sorted, insignificant whitespace normalised, and then diffed. Any structural difference fails the build.

This suite is the release gate for the "formatting must stay identical" requirement and is built in Sprint 6 before other features depend on the engine.

---

## 6. Import Engine (Classes, Students, Phrases)

A single **Tabular Import Pipeline** is shared across Excel, Word-table, CSV and clipboard-paste import:

1. **Parse** the source into a generic grid of rows/columns (ClosedXML for Excel, OpenXML SDK for Word tables, CsvHelper for CSV, a delimiter-sniffing parser for pasted clipboard text).
2. **Detect columns** using header-name heuristics (`Vorname`/`First name`, `Nachname`/`Surname`, `Geburtsdatum`/`DOB`, `Geschlecht`/`Gender`) plus content heuristics (date-shaped columns → Geburtsdatum; values in `{m, w, d, Junge, Mädchen, …}` → Geschlecht) if headers are missing or ambiguous.
3. **Preview screen**: every row rendered in an editable grid; rows missing a required field are highlighted red, exactly as the requirements specify, and can be corrected inline before commit.
4. **Commit**: only fully valid rows are written to the database in one transaction; the teacher sees a short summary ("18 Kinder importiert, 2 übersprungen").

The same pipeline, with a different target-column set (Bereich, Stufe, Klassengruppe, Text), powers the **Floskeln-Excel-Import**.

---

## 7. UI/UX Concept

### 7.1 Design principles

- **Fluent, Windows-native look** (WPF-UI): rounded cards, Mica background, Segoe UI Variable, native light/dark mode following the OS setting — the app should feel like it belongs on Windows 11, not like a generic cross-platform tool.
- **Large, unambiguous tiles over menus.** The 5 main areas are big tappable cards on the start screen (touch-laptop friendly), each with an icon, a one-line description, and a live count ("3 Klassen", "42 Kinder").
- **Consistent status colors** used everywhere: 🔴 red = missing/required data, 🟠 orange = warning (e.g. text overflow), 🟢 green = complete/ready, 🔵 blue = informational/auto-filled. Teachers learn this vocabulary once and it applies in every screen.
- **Guided, not gatekept.** The app never blocks typing (e.g. long free-text fields) — it warns, but lets the teacher decide, matching the "Schreiben bleibt jederzeit uneingeschränkt möglich" requirement.
- **Autosave everywhere** with a small unobtrusive "Gespeichert um 14:32" indicator — teachers should never need to remember to click Save, and should never lose work if the laptop sleeps or a window is closed.
- **Previews before commitment**: import preview, phrase live-preview with resolved name/pronoun, generated-document auto-open — the teacher always sees the real result before or immediately after an irreversible action.
- **Breadcrumb navigation** (`Start > Klasse E1 > Max Mustermann > Zeugnis 1. Halbjahr`) so teachers always know where they are and can jump back without hunting for a "Back" button.
- **Accessibility**: minimum 4.5:1 contrast, full keyboard navigation, scalable UI (respects Windows text-scaling settings), all interactive elements have accessible names for screen readers.
- **German-first UI language**, using the exact official terminology from the requirements document (Floskeln, Zeugnis, Fehlzeiten, etc.) so it matches how teachers already think and talk about these things.

### 7.2 Screen-by-screen outline

| Screen | Purpose | Key UI elements |
|---|---|---|
| **Startbildschirm** | Entry point, orientation | 5 large tiles (Klassen, Zeugnisse, Formulare, Floskeln, Fehlzeiten), current Schuljahr/Halbjahr/Schulname banner, gear icon → Einstellungen |
| **Klassenübersicht** | List/manage classes | Card list of classes with Jahrgang badge, child count; "+ Neue Klasse" button |
| **Klasse anlegen/bearbeiten** | Edit one class | Form with inline validation; Wochenstundenplan as a compact Mo–Fr number-entry row |
| **Kinderliste (per Klasse)** | Manage students in a class | Sortable/filterable table; red badge for incomplete records; row actions (edit/delete); "Importieren" and "+ Kind" buttons |
| **Kind anlegen/bearbeiten** | Edit one student | Two-column form; Förderstatus as a segmented control that conditionally reveals Förderschwerpunkt and the manual-template override |
| **Import-Assistent** | Bulk import wizard | 3-step wizard (Quelle wählen → Spalten prüfen → Vorschau/Korrektur); drag-and-drop file zone; paste-from-clipboard button |
| **Floskeln-Bibliothek** | Browse/manage phrases | Left filter panel (Bereich, Stufe, Klassengruppe) + searchable list; inline edit; "Aus Excel importieren" |
| **Floskel-Auswahl (Dialog, from Zeugnis-Editor)** | Insert a phrase | Filtered suggestion list with live name/pronoun preview before insertion |
| **Formularübersicht** | Manage the 7+ official templates | List with status ("Word-Vorlage hinterlegt" ✅ / "fehlt" 🔴); "+ Neues Formular" |
| **Formular bearbeiten** | Configure a template | Tabs: Felder, Fächer, Schalter (Notenblock/Empfehlung/Versetzung/zieldifferent), Word-Datei (Sprint 10) |
| **Fehlzeiten-Tabelle (per Klasse)** | Enter absence totals | Spreadsheet-like grid, two columns per child, inline validation (unentschuldigt ≤ gesamt) |
| **Zeugnisübersicht (per Kind or Klasse)** | List a child's/class's report cards | Status chips (Entwurf/Fertiggestellt), quick "Erzeugen" action |
| **Zeugnis-Editor** | Fill one report card | Dynamic form driven by the selected Formular; per-field character counters with orange overflow highlight; embedded Floskel-picker buttons; grade dropdowns; "Zeugnis erzeugen" primary button fixed at the bottom |
| **Zeugnis erzeugen (Ergebnis)** | Confirm generation | Save-location picker → progress → success screen with "Öffnen" (auto-opens Word) and "Zum Zeugnis zurück" |
| **Einstellungen** | School-wide settings | Simple form; also hosts Backup/Restore and Data-Protection controls (Sprint 11) |
| **Papierkorb** (Sprint 11) | Recover deleted items | List of recently deleted classes/students/report cards with "Wiederherstellen" |

A clickable low-fidelity prototype (Figma or a WPF-UI storyboard) is recommended as the first deliverable of Sprint 0/1 so teachers can react to the flow before code is committed to it.

---

## 8. Non-Functional Requirements

| Area | Requirement | Approach |
|---|---|---|
| **Offline** | No feature may require internet access | No network calls anywhere in the codebase; CI enforces this via a static-analysis rule blocking `HttpClient`/socket usage outside of update-checking (if ever added) |
| **Performance** | Instant feel for a single school's data volume (few hundred students) | SQLite is vastly over-provisioned for this scale; no pagination needed, but virtualization used on lists as good practice |
| **Data protection** | Personal data of minors must be protected | SQLCipher full-DB encryption + Windows-account-bound key (DPAPI) + optional app PIN for shared laptops (Sprint 11) |
| **Data-protection documentation** | School needs DSGVO paperwork, not just a technical measure | Deliverable in Sprint 12: a short **Datenschutzkonzept** — description of the technical & organisational measures (TOM), the Papierkorb retention window, "data stays on one local device, no processor involved", and guidance on deleting leavers' data |
| **Passphrase recovery** | Forgotten passphrase must have a defined outcome | **By design there is no backdoor**: a lost passphrase means lost data. Mitigation: on first setup the app generates a one-time **printable recovery key**, and the school signs off on the "no recovery without it" policy. Documented in the Datenschutzkonzept and the quick-start guide. |
| **Reliability** | No data loss on crash/power loss | SQLite WAL mode (atomic commits) + autosave + periodic automatic local backup snapshots (taken via the SQLite online-backup API, not a raw file copy) |
| **Recoverability** | Accidental deletion must be recoverable | Soft-delete + Papierkorb screen (Sprint 11), configurable retention window (default 30 days) |
| **Backup** | Teacher must be able to move/protect their data | One-click encrypted export bundle + restore wizard (Sprint 11); DB captured via SQLite online-backup / `VACUUM INTO` after a WAL checkpoint |
| **Template currency** | New official forms must be addable without a developer, eventually | Template upload + field-mapping wizard (Sprint 10) |
| **Usability for non-technical users** | Zero training should be required for core flows | UAT with real teachers at the end of Sprints 2, 7, 9, 12; iterate on findings |
| **Localization** | German only for MVP, but avoid hard-coded strings | All UI text in `.resx` resource files from day one, so future localization is cheap even though not required now |

---

## 9. Risks & How the Plan Mitigates Them

| Risk | Mitigation |
|---|---|
| Word-formatting subtly breaks after filling | Content-control-based OpenXML editing (§5), single-run collapse preserving `rPr`, + canonical-XML regression tests (§5.4) done early (Sprint 6) before other features build on top of it |
| Regression test gives false failures because the SDK re-serialises the package | Comparison is canonical-XML (attributes sorted, whitespace normalised, SDT contents masked) per document part, never a raw byte/naïve hash — specified in §5.4 |
| "Text fits the form" can't be checked automatically (no layout engine) | Per-field character/line budgets calibrated manually against the real templates (§5.3); orange warning, never a hard block; re-calibrated on template change |
| Unsigned installer blocked/flagged by SmartScreen on teacher laptops | Authenticode code-signing certificate budgeted in Sprint 12 (§2); if the school declines the cost, the trade-off is accepted in writing |
| Teacher forgets the DB passphrase | One-time printable recovery key on setup + signed-off "no backdoor" policy (§8) |
| Column auto-detection misreads a real, messy school Excel file | Preview-and-correct step is mandatory before commit (§6); pipeline built and tested against real sample files from the school, not synthetic data |
| Teachers find the app confusing | Design-first (clickable prototype before Sprint 2), status-color vocabulary, UAT checkpoints every few sprints, not just at the end |
| Encrypted DB adds complexity/bugs late in the project | Data-protection sprint (11) budgeted explicitly rather than squeezed in; DPAPI key-wrapping avoids forcing a password prompt on every launch |
| "Custom template upload" (biggest known gap) turns out much harder than expected | Isolated behind `IReportGenerationEngine`/`FieldMappingJson` from the start (§3), so it is additive, not a rewrite, when tackled in Sprint 10 |

---

## 10. Sprint Plan

Assumptions: **2-week sprints**, a small team (1–2 developers + part-time teacher liaison for UAT), **15 sprints total (~30 weeks / ~7 months)** from kickoff to release-ready installer. Sprints 0–9 deliver a fully usable MVP that already covers Kapitel 1–8 of the requirements; Sprints 10–12 close the "Offene Punkte" from Kapitel 9; Sprints 13–14 are explicit contingency/release buffer.

**Timeline risk.** This scope for 1–2 developers is tight. Sprints 0–9 (the MVP) are the firm commitment. **Sprints 10–12 are timeline-risk and may extend** — Sprint 10 (teacher-operated content-control mapping) is the biggest unknown and the plan already allows it to ship developer-assisted first. Sprint 13 is a dedicated buffer that is expected to be partly consumed earlier by Sprints 3, 8 and 10. UAT depends on the part-time teacher liaison being available at the end of Sprints 2, 8, 9 and 12/14 — a scheduling dependency to lock in early.

| # | Sprint | Goal |
|---|---|---|
| 0 | Setup & Architecture Spike | Foundations + de-risk the two hardest technical unknowns early (OOXML fill **and** SQLCipher/EF Core) |
| 1 | Data Layer & Shell | Start screen, navigation, settings, DB schema |
| 2 | Klassen & Kinder | Class/student CRUD |
| 3 | Import-Assistent | Bulk import from Excel/Word/CSV/Clipboard |
| 4 | Floskeln | Phrase library + placeholder engine |
| 5 | Formulare (Metadata) | Template metadata management for the 7 official forms |
| 6 | Word-Generierungs-Engine (Kern) | Content-control fill engine + canonical-XML regression tests |
| 7 | Fehlzeiten | Absence table + auto-transfer into report cards |
| 8 | Zeugnis-Editor | Full dynamic report-card editor tying everything together |
| 9 | Word-Generierung (Integration) | Wire "Zeugnis erzeugen" into the editor; overflow warnings; MVP UAT |
| 10 | Eigene Formulare | Custom-template upload + field-mapping wizard |
| 11 | Datenschutz, Backup, Papierkorb | Encryption, backup/restore, undo/recycle bin |
| 12 | Härtung & Release | Polish, packaging, code-signing, UAT, documentation, rollout |
| 13 | Buffer / Contingency | Absorb overruns from Sprints 3 / 8 / 10; no new scope |
| 14 | Release Buffer & Final UAT | Final teacher UAT round, bug-fix buffer, quick-start guide sign-off |

### Sprint 0 — Setup & Architecture Spike
- Repository, solution structure, coding conventions, CI pipeline (build + unit tests). Target framework **.NET 10 (LTS)**.
- WPF + WPF-UI shell skeleton with placeholder navigation.
- SQLite + EF Core wired up with an initial (empty) schema and migration flow.
- **Spike A (OOXML fill)**: take one real official `.docx` template, first **confirm it is true OOXML `.docx`, not legacy `.doc`**; insert content controls by hand; write a throwaway console app that fills it via OpenXML SDK (including the multi-run→single-run collapse preserving `rPr`); run a canonical-XML diff of everything outside the content controls against the original to prove the "no formatting drift" approach before it becomes load-bearing.
- **Spike B (encryption)**: prove `Microsoft.EntityFrameworkCore.Sqlite.Core` + `Microsoft.Data.Sqlite.Core` + `SQLitePCLRaw.bundle_e_sqlcipher` open an encrypted DB with `PRAGMA key` via a connection interceptor, run EF Core migrations against it, and wrap the key with DPAPI — confirming the provider combination before Sprint 11 depends on it.
- Clickable low-fidelity prototype of the 5-screen navigation for early teacher feedback.
- **DoD**: CI green; spike A report with a documented, proven filling approach and any Word-version/`.doc` caveats; spike B report confirming the encrypted-EF-Core stack.

### Sprint 1 — Data Layer & Application Shell
- Full EF Core schema for all core entities (§4), initial migration.
- Startbildschirm with 5 tiles + live counts, gear-icon → Einstellungen.
- Schuleinstellungen screen (school name, address, Schulleitung, Schuljahr/Halbjahr, dates).
- App-wide autosave + "Gespeichert um …" indicator component (reused everywhere after this).
- **DoD**: A fresh install lets a teacher set up school settings and see an empty, navigable app.

### Sprint 2 — Klassen & Kinder
- Klassenübersicht, Klasse anlegen/bearbeiten/löschen (with confirmation + cascade).
- Kinderliste with red incomplete-record badges, Kind anlegen/bearbeiten with conditional Förderstatus fields.
- Manual template-override field on the student (data only — no template logic engine yet, target of Sprint 5).
- **DoD**: A teacher can fully set up classes and students by hand, including edge cases (delete class with children, incomplete records flagged).

### Sprint 3 — Import-Assistent
- Tabular import pipeline (Excel via ClosedXML, Word-table via OpenXML, CSV via CsvHelper, clipboard paste).
- Column auto-detection heuristics + 3-step wizard UI + red-flagged preview/correction grid.
- Tested against real, messy sample files provided by the school (not just clean synthetic data).
- **DoD**: A real school Excel class list imports correctly end-to-end, including at least one deliberately messy file.

### Sprint 4 — Floskeln
- Floskeln CRUD (Bereich, Stufe, Niveau, Klassengruppe), filterable/searchable library screen.
- Placeholder engine — two syntaxes side by side:
  - App syntax: `[Name]`, `[sein/ihr]` etc., resolved from the student's Geschlecht, with the "first sentence gets the real name, later sentences get pronouns" rule.
  - **School syntax** (Grundschule Wehringhausen's real phrase file): `$Vorname$` (`$VORNAME$` / `$vorname$` control the casing) and a generic gender pair `&maskulin%feminin&` or `&maskulin/feminin&` — the chosen side is emitted verbatim, capitalised at sentence start.
  - For `Geschlecht = d` ("divers") there is no pronoun form — the engine falls back to repeating the name for every placeholder; confirm the desired rule with the school.
- Floskel-picker dialog with live preview (usable standalone in this sprint; wired into the Zeugnis-Editor in Sprint 8).
- Excel bulk import of phrases, reusing the Sprint 3 pipeline. Understands the school's real six-column layout (`Kürzel · Floskeltext · Gruppe · Fach · Niveau · Jahrgang`): Bereich is derived from Gruppe+Fach, Klassengruppe from the Jahrgang, and re-import de-dupes on Kürzel. The full file is also bundled and seeded on first run.
- **Slash-choice split** (`FloskelVariantExpander`, used by both the seed and the import): a phrase that packs a graded pick-one scale into slashes — `(oft/meist/teilweise)` or `freudig/oft/teilweise` — becomes one Floskel per combination (cartesian product), Kürzel suffixed a/b/c. Only parenthesised groups and tight runs of ≥3 single lower-case words are split, so gender pairs (`sie/er`), compound terms (`Wörterbuch/PC`) and spaced multi-word runs are left intact.
- **DoD**: A teacher can build and search a phrase library and see an accurate live preview of name/pronoun substitution.

### Sprint 5 — Formulare (Metadata)
- Formularübersicht + Formular-bearbeiten screens for the official templates' **metadata** (Anzeigename, Überschrift, Fächerliste, Freitextfelder, the four switches).
- Auto-suggestion logic for which Formular fits a given Kind + Klasse + Halbjahr (per Kap. 5.2 of the *requirements document*), with manual override respected.
- **DoD**: All templates are represented and correctly auto-suggested in test scenarios; no Word-file linkage yet (that's Sprint 6).
- **Aligned with the real documents** (`Zeugnisformulare_GS Wehringhausen_gesamt.docx`): the 7 seeded forms are now *Schuleingangsphase – 1./2. Schulbesuchsjahr* (Bericht, Schuljahresende), *Klasse 3 – 1. Halbjahr / Schuljahresende* (Noten), *Klasse 4 – 1. Halbjahr* (Noten + begründete Empfehlung) *· Schuljahresende* (Noten + Versetzungsvermerk), and *Zeugnis zum Gemeinsamen Lernen* (Bericht, zieldifferent, `Jahrgang = 0` = any year, both terms — only suggested for zieldifferente Kinder). Grade forms carry 11 rows: Religionslehre, Deutsch, Deutsch Sprachgebrauch, Deutsch Lesen, Deutsch Rechtschreiben, Englisch, Sachunterricht, Mathematik, Sport, Musik, Kunst (the sub-grade names map straight onto the template tags `Note_Deutsch_Sprachgebrauch` …). The Schuleingangsphase and Klasse 3 forms carry Arbeits- und Sozialverhalten + Lernentwicklung (Teil 1/2); the Klasse 4 forms carry only Arbeits- und Sozialverhalten (grades + Bemerkungen); Gemeinsames Lernen carries one undivided Lernentwicklung. Bemerkungen/Empfehlung stay on the dedicated `Zeugnis` fields.
- **Templates linked out of the box (Sprint 6).** The school's real, content-control-tagged `.docx` (7 Zeugnis forms + `Foerderempfehlung.docx`) are embedded in `ZeugnisApp.App` (`Resources/Templates/`). On first run `TemplateBootstrapper` extracts the file for each standard `Formular` into the Vorlagen directory and stores the hand-authored `FieldMapping` (`AmtlicheVorlagen`); a custom form still gets a generated placeholder. Nothing is overwritten once linked — "Vorlage hochladen" is only for replacing a form. Tag scheme in the `.docx`: `StudentName`, `Geburtsdatum`, `Klasse`, `Schuljahr`, `Fehlstunden_Gesamt/Unentschuldigt`, `Freitext_ArbeitsUndSozialverhalten`, `Freitext_Lernentwicklung_Teil1/2` (or `Freitext_Lernentwicklung`), `Freitext_Bemerkungen`, `Freitext_BegruendungSchulformempfehlung`, `Note_<Fach>`, `Datum_Zeugnisausstellung`. The year-end forms have no dedicated Versetzungsvermerk control in the real `.docx`, so `ReportValues` folds `Zeugnis.VersetzungsvermerkText` into the generated Bemerkungen text (still exposed on `zeugnis.versetzungsvermerk` for any template that maps it separately; the field-mapping wizard lists it as optional, not required).
- The separate **Lern- und Förderempfehlung** sheet (per-subject supplement, not a report card) has its own model: entity `Foerderempfehlung` (Kind + Schuljahr + Halbjahr + Fach; sections Beobachtungen / Schulische Maßnahmen / Empfehlungen für Zuhause; `Fertiggestellt`), `IFoerderempfehlungService` (GetForKind / GetOrCreate / Save / Delete), unique per (Kind, term, Fach). Editor screen `FoerderempfehlungenView` is reached from the Zeugnis-Editor ("Lern- und Förderempfehlung …"): one autosaving card per subject, add via a Fach picklist, delete with confirmation. `.docx` generation still to come.

### Sprint 6 — Word-Generierungs-Engine (Kern)
- Prepare content-controlled versions of all 7 official `.docx` files (developer task, using the Sprint 0 spike's proven method).
- Build `IReportGenerationEngine` + `FieldMappingJson` linking each Formular's fields to content-control tags; implement the multi-run→single-run collapse that preserves `rPr`.
- Automated regression test suite (§5.4): generate from fixed sample data for each of the 7 templates; assert content-control values are correct **and** a canonical-XML comparison of every part outside the content controls is unchanged versus the pristine original.
- **DoD**: For every one of the 7 templates, generation produces a correct `.docx` with no layout reflow, proven by the canonical-XML tests — this is the technical core of the whole project and is treated as the release gate for MVP.

### Sprint 7 — Fehlzeiten
- Fehlzeiten-Tabelle per class (Stunden gesamt / unentschuldigt), inline validation.
- **Fehlzeiten import** ("Fehlzeiten importieren …" on the Fehlzeiten screen): the shared import wizard on an absence profile, reading the school's SchILD *Schüler-Abwesenheit – Zusammenfassung* PDF (`PdfTableReader`, PdfPig — rebuilds rows/columns from word positions, rejoins two-line names) or an Excel sheet. Each row's name is matched onto a child already in the class ("Vorname Nachname" / "Nachname, Vorname"); unmatched names are listed back, never created. Only *Fehlstunden gesamt* is imported (the export has no excused split); *unentschuldigt* stays a manual entry and is preserved.
- A report **mirrors** the class table: `GetOrCreateAsync` re-pulls the current totals every time the editor opens, until the teacher edits the numbers on that report (`Zeugnis.FehlstundenManuellUeberschrieben` then pins them). Fixes absences entered/imported *after* a report was first created never showing up.
- Calendar-based absence data model implemented and unit-tested but intentionally left unlinked from the menu, exactly as specified.
- **DoD**: Absence numbers entered or imported flow into report cards (new and existing) and can still be pinned per report.

### Sprint 8 — Zeugnis-Editor
- Full dynamic editor: fields shown depend on the selected Formular's switches (Notenblock, Empfehlung+Begründung, Versetzungsvermerk, zieldifferent).
- Integrates the Sprint 4 Floskel-picker, Sprint 7 Fehlzeiten data, and the legally-required special-support text auto-insertion for zieldifferente Kinder.
- "Zeugnis fertiggestellt" status, Zeugnisübersicht per child/class.
- **DoD**: A teacher can create and fully fill a Zeugnis for a realistic set of test students covering every Förderstatus and every template (generation itself is wired in Sprint 9).

### Sprint 9 — Word-Generierung (Integration)
- Wire the Sprint 6 engine into the Sprint 8 editor: "Zeugnis erzeugen" button, save-location picker, progress indicator, auto-open of the result.
- Character-count display + orange overflow highlighting on long free-text fields, using per-field budgets **calibrated manually** against the real templates (§5.3); calibration pass for all 7 templates is part of this sprint.
- **DoD**: End-to-end flow from an empty Zeugnis to an opened, correct, non-reflowed Word document works entirely inside the app for all 7 templates and every Förderstatus.
- **UAT checkpoint** with real teachers on the full Klassen → Zeugnis → Erzeugen flow — the MVP is functionally complete at this point.

### Sprint 10 — Eigene Formulare (the biggest known gap)
- UI for uploading a new official `.docx` for a self-created Formular.
- Field-mapping wizard: teacher (or developer, if still too technical for a first version) inserts/selects content controls and maps them to the app's field list, reusing the Sprint 6 engine unchanged.
- **DoD**: Creating a brand-new Formular and generating a real Zeugnis from it works without a developer touching code, closing the single most-cited open issue.

### Sprint 11 — Datenschutz, Backup, Papierkorb
- SQLCipher encryption of the database file (using the provider combination proven in Sprint 0 spike B); DPAPI-wrapped key; optional app PIN for shared devices.
- **One-time printable recovery key** generated at first setup; "no backdoor / no recovery without the key" flow, worded for a non-technical teacher.
- Backup export (encrypted, checksummed bundle of DB + templates) and restore wizard; DB captured via the SQLite online-backup API / `VACUUM INTO` after a WAL checkpoint (never a raw copy of the live file).
- Soft-delete infrastructure + Papierkorb screen for classes/students/report cards, with a configurable retention window (default 30 days).
- **DoD**: Data is encrypted at rest, a backup taken on one laptop restores correctly on another, accidental deletions are recoverable within the retention window, and the recovery-key flow works.

### Sprint 12 — Härtung & Release
- Accessibility pass (contrast, keyboard nav, screen-reader labels), performance pass on realistic multi-year data volumes.
- Installer (Inno Setup, self-contained .NET publish); **Authenticode code-signing** of the app and installer `.exe` (or a signed-off written acceptance if the school declines the certificate); optional MSIX packaging stretch goal.
- **Datenschutzkonzept** deliverable: technical & organisational measures, Papierkorb retention, "local single-device, no processor", deleting leavers' data, and the passphrase/recovery-key policy.
- Workflow/documentation for "school receives a new official form" (still developer-assisted for MVP, but documented step-by-step using the Sprint 10 wizard).
- Bug-fix buffer, user-facing quick-start guide.
- **DoD**: Signed (or sign-off-accepted) installer, passing full regression suite (incl. the Sprint 6 canonical-XML fidelity tests), teacher-facing documentation + Datenschutzkonzept delivered.

### Sprints 13–14 — Buffer / Contingency & Final UAT
- No new scope. Absorb overruns from the high-risk sprints (3 Import, 8 Editor, 10 Eigene Formulare) and from UAT findings.
- Final teacher UAT round on the complete product; bug-fix buffer; quick-start guide sign-off.
- **DoD**: All UAT blockers closed; full regression suite green; release candidate accepted by the school.

### Beyond Sprint 14 (explicitly out of scope, documented for later)
- Multi-device data sharing/sync (open, needs alignment with the school first, per the requirements document itself).
- Cloud backup option (currently rejected by the offline-only requirement; would need explicit school sign-off on data residency).
- **Year rollover / class promotion** (E1→E2, archive a Schuljahr, carry children forward without re-import) — scope decision deferred to the school; schema does not block it.

---

## 11. Definition of Ready / Definition of Done (applies every sprint)

**Definition of Ready** — a sprint's work doesn't start until:
- The relevant section of the requirements document (and this plan) has been read against it for gaps.
- UI flow for any new screen has a prototype or wireframe reviewed at least informally.

**Definition of Done** — a sprint isn't closed until:
- Automated tests pass in CI (unit tests for services; regression tests for anything touching the document engine).
- The feature has been exercised through the actual UI, not just via unit tests.
- Any new persisted field has a reversible EF Core migration.
- No feature added a network dependency.

---

## 12. Deliverable of This Document

This plan is intended as the shared reference between a freelance developer and the school for scoping, estimating and tracking the build. The **Word-generation engine (Sprints 0 and 6)** is correctly identified as the technical core and highest-risk item in the original requirements document, and this plan places it early and gives it dedicated regression testing precisely because everything else in the app — classes, students, phrases, absences — only matters insofar as it correctly feeds that engine.

---

## 13. Review Notes (v1.0 → v1.1)

Feasibility review of v1.0. Verdict: **the plan is feasible and well-sequenced**; the central bet (OpenXML SDK + Content Controls, no Word automation) is the right choice and is correctly de-risked early. The following corrections and additions were folded in:

**Corrected over-stated guarantees**
1. "Byte-for-byte / mathematically guaranteed identical" → the SDK re-serialises the package on save, so output is not byte-identical. Reworded throughout to "no layout reflow", with fidelity proven by a **canonical-XML** comparison (§5.4), not a raw/naïve hash.
2. Added the **multi-run → single-run collapse preserving `rPr`** as an explicit engine requirement (§5.2) — a content control routinely holds several runs.
3. **Overflow detection cannot measure available space** (no layout engine). Reframed as **manually calibrated per-field character/line budgets** (§5.3), with the calibration pass moved into Sprint 9.

**Technology / integration**
4. Target **.NET 10 (LTS)** instead of .NET 8, whose support window closes around ship date (§2, Sprint 0).
5. Documented the **`Microsoft.EntityFrameworkCore.Sqlite` vs `bundle_e_sqlcipher` provider clash** and the working `*.Core` combination; added **Sprint 0 spike B** to prove it before Sprint 11 (§2).
6. Backups must use the **SQLite online-backup API / `VACUUM INTO`**, not a raw copy of a live WAL database (§2, §8, Sprint 11).
7. Added a **licensing** statement — all dependencies permissive, no copyleft (§2).

**Sprint plan**
8. **Reordered** so the Zeugnis-Editor (now Sprint 8) is built before generation is wired into it (now Sprint 9); Fehlzeiten moved to Sprint 7. Removes a forward dependency in the old Sprint 7.
9. Added **Sprints 13–14 as explicit buffer / final UAT**; flagged **Sprints 10–12 as timeline-risk**; total is now 15 sprints (~7 months). Called out the **teacher-liaison availability** dependency for UAT.
10. Sprint 0 now also verifies the supplied templates are true **OOXML `.docx`, not legacy `.doc`**.

**Missing deliverables added**
11. **Authenticode code-signing** of the installer (or written acceptance of the SmartScreen trade-off) — Sprint 12 (§2, §9).
12. **Passphrase-recovery policy**: one-time printable recovery key + signed-off "no backdoor" statement — Sprint 11 (§8, §9).
13. **Datenschutzkonzept / TOM** document as a Sprint 12 deliverable (§8).
14. **Papierkorb retention window** made explicit and configurable (default 30 days) (§4, §8, Sprint 11).

**Open questions for the school** (do not block the plan)
15. Pronoun handling for `Geschlecht = d` ("divers") — engine falls back to the name by default; confirm desired rule (§4, Sprint 4).
16. **Year rollover / class promotion** — in scope for MVP or deferred? Currently listed under "Beyond Sprint 14"; schema does not block it (§4, §10).
17. Uniqueness of a `Zeugnis` per (Kind, Formular, Schuljahr, Halbjahr) — assumed and enforced via a DB constraint (§4).