# AGENTS.md - NuGet Toolbox Development Guide

## Build, Test, Run Commands

```bash
# Build solution
dotnet build

# Run single test file
dotnet test --filter ClassName

# Run all tests
dotnet test

# Run CLI directly
dotnet run --project src/NuGetToolbox.Cli -- find --package "Newtonsoft.Json"

# Format code
dotnet format
```

## Architecture & Structure

**.NET 8+ CLI tool** using **System.CommandLine** for argument parsing.

**Core Projects:**
- `NuGetToolbox.Cli` - CLI entry point with 4 commands: `find`, `list-types`, `export-signatures`, `diff`
- `NuGetToolbox.Tests` - xUnit tests

**Key Services:**
- `NuGetPackageResolver` - NuGet.Protocol V3 integration for package download
- `AssemblyInspector` - MetadataLoadContext for safe metadata-only inspection (never code execution)
- `XmlDocumentationProvider` - Parse/match compiler-generated `.xml` docs using Roslyn `DocumentationCommentId`
- `SignatureExporter` - Render C# signatures via Roslyn symbol display
- `ApiDiffAnalyzer` - Compare versions for breaking changes

**Key Dependencies:**
- `NuGet.Protocol`, `NuGet.Packaging`, `NuGet.Configuration`
- `System.Reflection.MetadataLoadContext`
- `Microsoft.CodeAnalysis.CSharp` (symbol display & doc IDs)

## Code Style & Conventions

- **Language:** C# 11+ (.NET 8+)
- **Nullable:** Enable `<Nullable>enable</Nullable>` project-wide; annotate all public APIs
- **Naming:** PascalCase types/methods, camelCase locals/params; no Hungarian notation
- **Imports:** `using` at top, alphabetically sorted; no wildcard imports
- **Errors:** Use custom exceptions inheriting `Exception`; throw with actionable messages
- **JSON:** Use `System.Text.Json` with JsonSerializerOptions (camelCase property names)
- **Async:** Mark I/O operations `async Task`; avoid `.Result` or `.Wait()`
- **Logging:** Structured logs via ILogger; include context (packageId, version, tfm, duration)
- **Testing:** Arrange-Act-Assert pattern; mock NuGet sources for offline tests
