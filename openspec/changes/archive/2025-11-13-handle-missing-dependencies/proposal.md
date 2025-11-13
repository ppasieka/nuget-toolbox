# Handle Missing NuGet Dependencies

## Why

When inspecting a NuGet package's types, MetadataLoadContext fails with `FileNotFoundException` if the package depends on other NuGet packages that aren't provided to the resolver. This prevents users from inspecting packages with external dependencies, which is common in real-world NuGet packages.

Example error:
```
System.IO.FileNotFoundException: Could not find assembly 'Confirmit.NetCore.Urn, Version=4.0.0.0, Culture=neutral, PublicKeyToken=8134450e5a05c0c1'.
```

The tool should focus on inspecting **only the requested package**, not its entire dependency tree. Users want to see "what's in this package" without needing to resolve all transitive dependencies.

## What Changes

- **Graceful error handling** in `AssemblyInspector.ExtractPublicTypes`:
  - Catch `ReflectionTypeLoadException` and use partially-loaded types from `ex.Types`
  - Skip individual types that fail with `TypeLoadException`, `FileNotFoundException`, `FileLoadException`
  - Log missing dependency issues at Debug level (not Warning/Error) to avoid noise
  - Continue extraction of successfully-loaded types

- **List direct dependencies** from package's `.nuspec`:
  - Add `GetDirectDependenciesAsync` to `NuGetPackageResolver`
  - Read dependency groups from `PackageArchiveReader.NuspecReader.GetDependencyGroups()`
  - Return structured list with target framework, package ID, and version range
  - Available for programmatic use (not displayed in CLI output)

- **File-based logging**:
  - All logging writes to `%TEMP%/nuget-toolbox/logs/nuget-toolbox-{date}.log`
  - Console stdout remains clean for JSON output
  - Exit code remains 0 if partial results are successful

## Impact

- **Affected specs**: `cli` (modifies Assembly Metadata Inspection requirement)
- **Affected code**:
  - `AssemblyInspector.cs` - Add try/catch handling around type extraction
  - `NuGetPackageResolver.cs` - Add method to read direct dependencies
  - `ListTypesCommand.cs` - Display dependency info to users
  - `Models/` - Add `DirectDependency` model if needed
