# Fix Missing Public Types Bug

## Why

The `list-types` command fails to list certain public types (e.g., `PanelNodeBase`, `WysiwigStyle`, `PanelPageMasters` from `Confirmit.SurveyLayout.Model v21.1.3`) due to two issues:

1. **Nested public types are excluded**: The code uses `type.IsPublic` which only returns `true` for top-level public types, not nested public types (e.g., `Container.NestedPublicClass`)
2. **Type classification fails silently for types with missing dependencies**: When `GetTypeKind(type)` checks `type.IsClass`, it requires resolving the base type chain, which throws `FileNotFoundException` if dependencies are not loaded in `MetadataLoadContext`

## What Changes

- **Fix visibility check**: Replace `type.IsPublic` with `type.IsVisible` to include nested public types where the entire containing chain is public
- **Improve dependency resolution**: Add package dependencies to `PathAssemblyResolver` so that type inheritance chains can be resolved
- **Enhanced error resilience**: Improve exception handling in `GetTypeKind()` to catch dependency-related failures and use fallback type classification

## Impact

- **Affected specs**: `cli` (Assembly Metadata Inspection, List Types Command)
- **Affected code**:
  - `src/NuGetToolbox.Cli/Services/AssemblyInspector.cs` - visibility filter + error handling
  - `src/NuGetToolbox.Cli/Commands/ListTypesCommand.cs` - dependency loading
  - `src/NuGetToolbox.Cli/Services/NuGetPackageResolver.cs` - dependency resolution helper
