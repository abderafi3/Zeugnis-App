# Zeugnis-App

Offline Windows desktop application for primary-school teachers (Grundschule Wehringhausen, Hagen).
Manages classes, students, absences and reusable text snippets, and fills the school's **real official
Word report-card templates** with data — without altering their formatting — to produce a
ready-to-print `.docx`.

See [`plan.md`](plan.md) for the full concept, architecture and sprint plan. This README covers only
how to build and work in the repository.

## Requirements

- Windows 10 or 11
- .NET SDK **10.0.400+** (pinned in `global.json`)
- Building needs internet the first time (NuGet restore). The **app itself makes no network calls** —
  that is enforced by design and by review (plan §8).

## Solution layout

| Project | Target | Purpose |
|---|---|---|
| `src/ZeugnisApp.Domain` | `net10.0` | Entities, enums, domain rules. No dependencies. |
| `src/ZeugnisApp.Application` | `net10.0` | Service interfaces, DTOs, the `IReportGenerationEngine` seam. |
| `src/ZeugnisApp.Infrastructure` | `net10.0` | EF Core + SQLite, migrations, service implementations. |
| `src/ZeugnisApp.Reporting` | `net10.0` | OOXML report-generation engine (built out in Sprint 6). |
| `src/ZeugnisApp.App` | `net10.0-windows` | WPF UI (MVVM, WPF-UI Fluent controls). |
| `tests/*` | `net10.0` / `net10.0-windows` | xUnit tests (Domain, Infrastructure, App view-models). |
| `spikes/Spike.OpenXmlFill` | `net10.0` | Sprint 0 Spike A — proves content-control fill fidelity (plan §5.4). |
| `spikes/Spike.SqlCipher` | `net10.0` | Sprint 0 Spike B — proves the SQLCipher + EF Core stack (plan §2). |

## Common commands

```bash
dotnet build Zeugnis-App.slnx                 # build everything
dotnet test  Zeugnis-App.slnx                 # run all tests
dotnet run   --project src/ZeugnisApp.App     # launch the app

dotnet run   --project spikes/Spike.OpenXmlFill   # Sprint 0 Spike A
dotnet run   --project spikes/Spike.SqlCipher     # Sprint 0 Spike B
```

### Database migrations

`dotnet-ef` is pinned as a local tool (`dotnet tool restore` once after cloning).

```bash
dotnet dotnet-ef migrations add <Name> --project src/ZeugnisApp.Infrastructure --output-dir Persistence/Migrations
```

Migrations are applied automatically on app start (`DatabaseInitializer`). Never `EnsureCreated`.

## Runtime data locations

Everything lives under `%LOCALAPPDATA%\Zeugnis-App\` (no elevation, bound to the Windows account):

- `daten.zeugnisapp` — the single SQLite data file (encrypted at rest from Sprint 11)
- `Vorlagen\` — the official `.docx` templates
- `logs\` — rolling Serilog files (local only, no telemetry)

## Conventions

- Central Package Management: all versions in `Directory.Packages.props`.
- MVVM with `CommunityToolkit.Mvvm`; no business logic in views, no UI types in services.
- All user-facing strings are German; keep them out of code where practical (`.resx` from Sprint 1+).

## Localization

All user-facing text lives in `src/ZeugnisApp.App/Resources/Strings.resx` (German = neutral culture).
XAML reads it via `{x:Static res:Strings.Key}`; view models via `Strings.Key`. Add
`Strings.<culture>.resx` satellites to localise. `Strings.Designer.cs` is hand-maintained (so plain
`dotnet build` works) — add a property when you add a key.

## Sprint status

- **Sprint 0 — Setup & Architecture Spike:** done. Solution skeleton, CI, EF Core schema + initial
  migration, WPF shell with five-area navigation, both spikes passing. See
  [`docs/sprint-0-spike-report.md`](docs/sprint-0-spike-report.md).
- **Sprint 1 — Data Layer & Shell:** done. Full schema, start screen with live counts,
  school-settings screen with debounced autosave, "Gespeichert um …" indicator, `.resx` localization.
- **Sprint 2 — Klassen & Kinder:** done. Class overview (cards, Jahrgang badge, child count,
  incomplete badge), class create/edit with inline validation + autosave, per-class child list
  (DataGrid, red "unvollständig" status), child create/edit with conditional Förderschwerpunkt and
  manual template override, soft-delete with cascade + confirmation dialog. Master/detail runs
  inside `KlassenPage` via a hosted `ContentControl` (no NavigationView drill-down).
- **Sprint 3 — Import-Assistent:** done. One shared tabular pipeline (`ITabularImport`): Excel
  (ClosedXML), Word table (OpenXML SDK), CSV (CsvHelper, delimiter sniffing), pasted clipboard text.
  `ColumnDetector` (header aliases + content heuristics), `ImportSession` (editable mapping,
  per-row validation). 3-step wizard (`KinderImportViewModel`): Quelle wählen (buttons +
  drag-and-drop) → Spalten prüfen → Vorschau/Korrektur (editable DataGrid, invalid rows red,
  skipped on commit) → one-transaction commit with an "N importiert, M übersprungen" summary.
  Reusable for the Floskeln import (Sprint 4) via a different `ImportProfile`.
- **Sprint 4 — Floskeln:** done. `PlaceholderEngine` (Domain): `[Name]` → real name first, pronoun
  after; `[er/sie]`, `[ihn/sie]`, `[ihm/ihr]`, `[sein/ihr]`, `[seine/ihre]`; auto-capitalisation at
  sentence start; `divers`/unknown falls back to the name. `IFloskelnService` (filter by
  Bereich/Stufe/Klassengruppe + text search). Phrase-library screen (`FloskelnPage`): filter panel +
  editable DataGrid with inline autosave, "Neue Floskel", Excel import (the Sprint 3 wizard, now
  profile-generic, in a modal `ImportWizardWindow`). Standalone Floskel-Auswahl dialog
  (`FloskelPickerWindow`) with a live preview resolved for a sample child — reused by the
  Zeugnis-Editor in Sprint 9.
- **Sprint 5 — Formulare (metadata):** done. `Formular` gains `Jahrgang` + `GiltFuerHalbjahr`.
  Seven standard NRW templates auto-seeded once into an empty DB (`FormularSeed`, no re-seed after
  deletion). `IFormulareService` (CRUD + owned-`Freitextfelder` replace) and the pure
  `FormularSuggester` — manual override wins, else match by year group + term, preferring a
  zieldifferent template for a zieldifferent child and falling back to the regular one.
  `FormularePage` workspace: overview cards with a ready/"Vorlage fehlt" status chip; edit form
  with the four switches, Fächer (line per subject), an editable Freitextfelder list, autosave.
  No `.docx` linkage yet — that is Sprint 6.
- **Sprint 6 — Word-Generierungs-Engine (Kern):** done — the MVP release gate (plan §5, §12).
  `IReportGenerationEngine` / `OpenXmlReportGenerationEngine` in `ZeugnisApp.Reporting`: works on a
  copy of the template, fills content controls by tag via `ContentControlFiller` (multi-run →
  single-run collapse preserving the first run's `rPr`, from Spike A), covers body + headers +
  footers, never opens the template for editing. `FieldMapping` (value-key → tag, JSON in
  `Formular.FieldMappingJson`, `Default(formular)` builder). `ReportValues` (pure request → value
  map). `OoxmlCanonicalizer.CompareStructure` — the §5.4 canonical-XML comparison (attributes
  sorted, whitespace normalised, `<w:sdtContent>` masked) over `document.xml`, `styles.xml`,
  `numbering.xml`, `settings.xml`, `theme`, headers, footers + a media-hash inventory.
  **Regression suite (17 tests):** for each of the 7 standard templates, generate from fixed sample
  data and assert the values landed in the right tags **and** nothing outside the content controls
  changed. Real school `.docx` files replace the synthetic `TemplateFixture` when available.
- **Sprint 7 — Fehlzeiten:** done. `IFehlzeitenService` — per-class rows for the current term
  (zeros when nothing entered), upsert with `unentschuldigt ≤ gesamt` validation.
  `IZeugnisService.GetOrCreateAsync` — creates the report card if absent (template auto-suggested,
  manual override respected) and **copies the current absence totals into it once**; a later
  correction of the class table does not touch an already-created report (plan §Sprint 7 DoD).
  `FehlzeitenPage` — class picker + term banner + editable DataGrid, invalid rows shaded red,
  debounced autosave. Calendar variant (`Fehltag` + `FehltageAggregator`, pure, 3 tests) modelled
  and migrated but deliberately not surfaced in the UI.
- **Sprint 8 — Zeugnis-Editor:** done. `IZeugnisService` extended (`GetWithContextAsync`,
  `SaveAsync` with owned-collection replace, `SetFertiggestelltAsync`, `GetUebersichtForKlasseAsync`).
  `ZeugnissePage` workspace: per-class **Zeugnisübersicht** (status chip Entwurf / fertiggestellt /
  kein Formular, quick done↔reopen) → **dynamic editor** whose sections appear per the Formular's
  switches (Noten / Empfehlung+Begründung / Versetzungsvermerk); one editor per Freitextfeld with a
  char counter + orange overflow flag + "Floskel einfügen" (opens the Sprint 4 picker with the
  child as the sample); grade dropdowns per subject; Fehlstunden pre-filled from Sprint 7 and
  editable; the mandatory **zieldifferent remark** auto-inserted into Bemerkungen for zieldifferente
  Kinder; "Zeugnis fertiggestellt" toggle; debounced autosave. Generation into a `.docx` is wired
  into this editor in Sprint 9.
- **Sprint 9 — Word-Generierung (Integration):** done — **MVP functionally complete**.
  "Zeugnis erzeugen" in the editor: flush → assemble `ReportRenderRequest` (Zeugnis + Kind + Klasse
  + SchoolSettings) → save-location dialog → `IReportGenerationEngine.GenerateAsync` → store
  `GeneratedFilePath` → open with the OS handler; progress ring + result/overflow status; button
  disabled without a linked template. `PlaceholderTemplateBuilder` promoted from the test fixture
  into `ZeugnisApp.Reporting` (covered by the 17-test regression); `TemplateBootstrapper` generates
  a placeholder `.docx` per Formular on startup so generation works before the school's official
  files are uploaded (Sprint 10). `StandardFormulare` now carry **estimated** `MaxZeichen` budgets
  (§5.3 — recalibrate against the real templates). End-to-end test: DB-backed Zeugnis → real engine
  → filled `.docx` with correct values and unchanged structure. Fixed: `FormulareService.SaveAsync`
  was dropping `WordFilePath` / `FieldMappingJson`.
