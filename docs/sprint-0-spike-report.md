# Sprint 0 — Spike Report

Status: **both spikes PASS.** The two load-bearing technical decisions in `plan.md` are proven.
Re-run any time with `dotnet run --project spikes/Spike.OpenXmlFill` / `spikes/Spike.SqlCipher`;
CI runs both on every push.

---

## Spike A — OOXML content-control fill fidelity (plan §5)

**Question.** If we fill a `.docx` by replacing only the *text* inside content controls (SDT
elements) via the OpenXML SDK, does anything else in the document change?

**Method.** `spikes/Spike.OpenXmlFill` builds a small document (styled heading, bordered table,
a Plain-Text content control tagged `StudentName`), copies it, fills the control with a value, then
compares the filled file against the pristine original using a **canonical-XML** comparison — the
exact technique the Sprint 6 regression suite will use (plan §5.4):

- parse each relevant part (`document.xml`, `styles.xml`)
- mask the contents of every `<w:sdtContent>` so the filled-in value is not counted as a diff
- sort attributes, drop insignificant whitespace, drop namespace-declaration noise
- `XNode.DeepEquals`

**Result.**

```
Content controls filled: 1
  [OK]   document.xml byte-independent structure identical
  [OK]   styles.xml byte-independent structure identical
PASS — 2 document parts identical outside the content controls.
```

**Findings carried into the build.**

1. **Re-serialisation is real.** The SDK rewrites the whole package on save; the output is *not*
   byte-identical to the input even when nothing semantic changed. Fidelity must be checked by
   canonical-XML comparison, never a file hash. (This is why `plan.md` v1.1 dropped the
   "byte-for-byte" language.)
2. **Multi-run collapse is required.** A content control routinely holds several runs. The fill
   logic collapses them to a single run that carries the **first** original run's `rPr`, so
   formatting (font, weight, colour) is preserved. Implemented in the spike; to be reused verbatim
   by `ZeugnisApp.Reporting` in Sprint 6.
3. **Plain-Text SDTs are the safe target.** Rich-Text / date-picker SDTs carry extra structure;
   the template-preparation step (plan §5.1) standardises on Plain Text.

**Open item for Sprint 6.** Confirm the school's real templates are true OOXML `.docx`, not legacy
`.doc`. Extend the comparison to `numbering.xml`, `theme/*`, `settings.xml` and a media-part hash
inventory.

---

## Spike B — SQLCipher + EF Core provider stack (plan §2)

**Question.** Does the provider combination the plan mandates for Sprint 11 actually work together
on .NET 10 — `Microsoft.EntityFrameworkCore.Sqlite.Core` + `Microsoft.Data.Sqlite.Core` +
`SQLitePCLRaw.bundle_e_sqlcipher` + `PRAGMA key` + DPAPI-wrapped key material?

**Method.** `spikes/Spike.SqlCipher`:

1. create a DB through a keyed connection (`PRAGMA key = '…'` as the first statement)
2. read the raw file header off disk
3. try to open it with the wrong key
4. open it with the right key and round-trip data **through EF Core** (`IRelationalDatabaseCreator`
   + `SaveChanges` + LINQ query) as well as raw ADO.NET
5. DPAPI-protect the key (`CurrentUser`) and recover it

**Result.**

```
On-disk header bytes: 7C-B9-03-DF-...          (random — not "SQLite format 3")
  [OK]   file header is not 'SQLite format 3' -> encrypted at rest
  [OK]   wrong key rejected (SqliteException 'file is not a database')
  [OK]   EF Core rows:  Über EF Core geschrieben
  [OK]   ADO.NET rows:  Kind: Erika Mustermann
  [OK]   key DPAPI-protected (CurrentUser) and recovered
PASS
```

**Findings carried into the build.**

1. **`PRAGMA key` takes no bound parameters.** The passphrase must be inlined as a SQL string
   literal with `'` doubled. The Sprint 11 `DbConnection` interceptor will do this.
2. **Provider split confirmed.** Sprint 11 changes exactly two package references
   (`Microsoft.EntityFrameworkCore.Sqlite` → `…Sqlite.Core`, add `Microsoft.Data.Sqlite.Core` +
   `SQLitePCLRaw.bundle_e_sqlcipher`) and calls `SQLitePCL.Batteries_V2.Init()` at startup. No EF
   Core model or migration changes needed.
3. **DPAPI covers the "no prompt on the owner's machine" requirement** (plan §2). A separate
   printable **recovery key** (plan §8) is still required and is Sprint 11 scope — DPAPI alone
   gives no recovery path if the Windows profile is lost.

---

## Sprint 0 Definition of Done

| Item | State |
|---|---|
| Repository, solution structure, coding conventions | done |
| CI pipeline (build + unit tests + spikes) | `.github/workflows/ci.yml` |
| WPF + WPF-UI shell with placeholder navigation | done (5 areas + Einstellungen) |
| SQLite + EF Core wired up, initial migration, migrate-on-start | done (`20260829083829_InitialCreate`) |
| Spike A — proven filling approach + caveats documented | **PASS** (above) |
| Spike B — proven encryption stack + caveats documented | **PASS** (above) |
| Clickable low-fidelity prototype of the 5-screen navigation | the running shell serves as this; a Figma pass is optional before Sprint 2 |
| CI green | to verify on first push |

**Not done in this environment:** manual GUI smoke test (no display). Build + the Infrastructure
tests exercise the startup/migration path; a human should run `dotnet run --project src/ZeugnisApp.App`
once on a real desktop.
