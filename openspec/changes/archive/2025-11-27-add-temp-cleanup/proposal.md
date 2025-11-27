# Proposal: Add Temp Directory Cleanup

## Why

Commands that extract package assemblies (ListTypesCommand, ExportSignaturesCommand, DiffCommand) create temporary directories that are never deleted, causing disk bloat over time (~1-10MB per invocation). This is a P0-Critical resource leak that particularly affects CI/CD environments.

## What Changes

- Add guaranteed cleanup of temp directories in `finally` blocks for all commands that create them
- Ensure cleanup occurs even on exception or cancellation
- Add logging for cleanup failures (warning level, non-fatal)
- Add E2E tests to verify no temp directories remain after execution

## Impact

- Affected specs: `cli`
- Affected code:
  - `src/NuGetToolbox.Cli/Commands/ListTypesCommand.cs` (L150-151)
  - `src/NuGetToolbox.Cli/Commands/ExportSignaturesCommand.cs` (L163-164)
  - `src/NuGetToolbox.Cli/Commands/DiffCommand.cs` (L167-168, L200 - two temp dirs)
- Risk: Low - straightforward addition of `finally` blocks with no behavioral changes to successful paths
