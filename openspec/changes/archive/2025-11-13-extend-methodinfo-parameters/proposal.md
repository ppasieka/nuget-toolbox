## Why
MethodInfo currently only includes documentation from XML comments. When XML documentation is missing or incomplete (no params/returns), consumers lack essential type information about method parameters and return types, reducing the usefulness of the exported signatures.

## What Changes
- Extend `MethodInfo` model with structured parameter information (type + name)
- Add return type information to `MethodInfo`
- Ensure parameter and return type data is always populated from reflection, regardless of XML documentation availability
- Update `AssemblyInspector` to extract parameter/return type metadata
- Update `SignatureExporter` to include the new fields in JSON output

## Impact
- Affected specs: cli (export-signatures command)
- Affected code: 
  - `src/NuGetToolbox.Cli/Models/MethodInfo.cs` - Model extension
  - `src/NuGetToolbox.Cli/Services/AssemblyInspector.cs` - Metadata extraction
  - `src/NuGetToolbox.Cli/Services/SignatureExporter.cs` - JSON serialization
  - `tests/NuGetToolbox.Tests/` - Test updates
