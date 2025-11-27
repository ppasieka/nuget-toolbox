## Why

Commands (FindCommand, DiffCommand) create their own `ServiceProvider` instances via `CreateDefaultServiceProvider()` instead of using centralized DI from Program.cs. This fragments configuration, causes inconsistent service lifetimes, makes testing harder (cannot inject mocks), and violates AGENTS.md guidelines ("Move DI setup to Program.cs").

## What Changes

- **BREAKING**: Commands now require non-null `IServiceProvider` in `Create()` method
- Remove `CreateDefaultServiceProvider()` from all commands (FindCommand, DiffCommand, ListTypesCommand, ExportSignaturesCommand)
- Centralize all DI registration in Program.cs
- Use correct service lifetimes (Singleton for stateless services, Transient for per-operation services)
- Fix DI lifetime misuse: Scoped services resolved without scope creation

## Impact

- Affected specs: `cli`
- Affected code:
  - `src/NuGetToolbox.Cli/Program.cs` - Add centralized DI setup
  - `src/NuGetToolbox.Cli/Commands/FindCommand.cs` - Remove CreateDefaultServiceProvider
  - `src/NuGetToolbox.Cli/Commands/DiffCommand.cs` - Remove CreateDefaultServiceProvider
  - `src/NuGetToolbox.Cli/Commands/ListTypesCommand.cs` - Remove CreateDefaultServiceProvider (if present)
  - `src/NuGetToolbox.Cli/Commands/ExportSignaturesCommand.cs` - Remove CreateDefaultServiceProvider (if present)
