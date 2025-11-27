# Proposal: Improve Assembly Selection

## Why

The current assembly selection logic has three issues identified in the final-aggregated-report.md:
1. **Weak TFM selection** (P1): Naive `OrderByDescending(Version)` fails across framework families—may prefer `net4.8` over `netstandard2.0` when running on `net8.0`
2. **No ref/ assembly preference** (P2): Only checks `lib/` assemblies, ignoring cleaner `ref/` reference assemblies designed for metadata inspection
3. **Non-deterministic output** (P3): Output arrays not sorted, causing different JSON byte layouts between identical runs

## What Changes

- Integrate `NuGet.Frameworks.FrameworkReducer` for compatibility-based TFM selection
- Prefer `ref/` assemblies when available, fall back to `lib/`
- Sort all output arrays (types, methods, diff results) for deterministic JSON output
- Add `--tfm` explicit selection with available TFM listing on mismatch

## Impact

- Affected specs: `specs/cli/spec.md`
- Affected code:
  - `ListTypesCommand.cs` (L140-142, L95-100)
  - `ExportSignaturesCommand.cs` (L153-155)
  - `DiffCommand.cs` (L155-159)
  - `ApiDiffAnalyzer.cs` (L37-39)
  - Shared assembly extraction logic
