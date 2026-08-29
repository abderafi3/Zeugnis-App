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
