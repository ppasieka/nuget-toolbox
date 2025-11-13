## Why
All core service implementations are currently stubs throwing `NotImplementedException`. The CLI commands cannot function without working NuGet package resolution, assembly inspection, XML documentation parsing, signature export, and API diff analysis.

## What Changes
- Implement `NuGetPackageResolver` using NuGet.Protocol V3 API
- Implement `AssemblyInspector` using MetadataLoadContext for safe assembly metadata extraction
- Implement `XmlDocumentationProvider` to parse compiler-generated XML documentation files
- Implement `SignatureExporter` using Roslyn symbol display for C# signature rendering
- Implement `ApiDiffAnalyzer` to compare API versions and identify breaking changes
- Implement command handlers (`FindCommand`, `ListTypesCommand`, `ExportSignaturesCommand`, `DiffCommand`)

## Impact
- Affected specs: `cli`
- Affected code:
  - `src/NuGetToolbox.Cli/Services/NuGetPackageResolver.cs`
  - `src/NuGetToolbox.Cli/Services/AssemblyInspector.cs`
  - `src/NuGetToolbox.Cli/Services/XmlDocumentationProvider.cs`
  - `src/NuGetToolbox.Cli/Services/SignatureExporter.cs`
  - `src/NuGetToolbox.Cli/Services/ApiDiffAnalyzer.cs`
  - `src/NuGetToolbox.Cli/Commands/FindCommand.cs`
  - `src/NuGetToolbox.Cli/Commands/ListTypesCommand.cs`
  - `src/NuGetToolbox.Cli/Commands/ExportSignaturesCommand.cs`
  - `src/NuGetToolbox.Cli/Commands/DiffCommand.cs`
