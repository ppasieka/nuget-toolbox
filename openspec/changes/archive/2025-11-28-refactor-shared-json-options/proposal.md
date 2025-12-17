## Why

Multiple CLI commands (FindCommand, ListTypesCommand, ExportSignaturesCommand) duplicate identical `JsonSerializerOptions` configuration across files, violating DRY. The report identifies this as a medium-priority refactoring opportunity that becomes worthwhile when touching multiple commands.

## What Changes

- Add new `CommandOutput` static class in `Services/` with shared JSON serialization options
- Add shared `WriteResultAsync` utility method for consistent output handling (stdout vs file)
- Update FindCommand, ListTypesCommand, and ExportSignaturesCommand to use shared utilities
- Remove duplicated JsonSerializerOptions instantiation from each command

## Impact

- Affected specs: cli
- Affected code:
  - `src/NuGetToolbox.Cli/Services/CommandOutput.cs` (new file)
  - `src/NuGetToolbox.Cli/Commands/FindCommand.cs`
  - `src/NuGetToolbox.Cli/Commands/ListTypesCommand.cs`
  - `src/NuGetToolbox.Cli/Commands/ExportSignaturesCommand.cs`
