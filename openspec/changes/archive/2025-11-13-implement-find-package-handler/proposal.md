## Why

The `find` command currently has a CLI handler defined but throws `NotImplementedException`. To deliver a functional NuGet package resolution feature, the handler must integrate with `NuGetPackageResolver` and output structured results via JSON.

## What Changes

- Implement `FindCommand` handler to resolve packages via `NuGetPackageResolver`
- Support optional version parameter (defaults to latest if omitted)
- Support optional feed URL parameter (if omitted, uses system-defined feed from nuget.config)
- Support optional output file parameter (defaults to stdout)
- Serialize `PackageInfo` to JSON and write to file or console
- Add comprehensive unit tests covering success, error, and edge cases

## Impact

- Affected specs: `cli/find-package` (new)
- Affected code: `src/NuGetToolbox.Cli/Commands/FindCommand.cs`, `tests/NuGetToolbox.Tests/FindCommandTests.cs`
- No breaking changes; new implementation completes stub
