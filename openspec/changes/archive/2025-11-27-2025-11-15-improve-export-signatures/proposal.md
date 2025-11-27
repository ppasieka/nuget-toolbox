# Improve Export-Signatures Command

## Why
The current implementation has several issues that affect the reliability and completeness of public API extraction:

1. **Incomplete visibility filtering** - Uses `IsPublic || IsNestedPublic` instead of `IsVisible`, potentially including non-public nested types
2. **No resilience to partial load failures** - `ReflectionTypeLoadException` can cause entire assembly to be skipped
3. **CLI flag mismatch** - Spec shows `--namespace` but implementation uses `--filter`
4. **Ambiguous interface method handling** - Unclear whether inherited interface methods should be included

## What Changes
- Harden type enumeration with proper visibility checks and error handling
- Add CLI flag alias for backward compatibility
- Clarify and implement consistent interface method handling
- Improve logging for better observability
- Add comprehensive unit tests for edge cases


## Files to be modified

- `src/NuGetToolbox.Cli/Services/SignatureExporter.cs` - Core logic improvements
- `src/NuGetToolbox.Cli/Commands/ExportSignaturesCommand.cs` - CLI flag alias
- `tests/NuGetToolbox.Tests/SignatureExporterTests.cs` - New test coverage

## Acceptance Criteria

- Exporter correctly filters using `Type.IsVisible` and limits to classes/interfaces
- Command handles `ReflectionTypeLoadException` gracefully and continues processing
- CLI accepts both `--filter` and `--namespace` flags with identical behavior
- Unit tests cover visibility filtering, partial load failures, and interface inheritance
- Output remains compliant with existing JSON schema
- No breaking changes to existing functionality