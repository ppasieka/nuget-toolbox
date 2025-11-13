<!-- OPENSPEC:START -->
# OpenSpec Instructions

These instructions are for AI assistants working in this project.

Always open `@/openspec/AGENTS.md` when the request:
- Mentions planning or proposals (words like proposal, spec, change, plan)
- Introduces new capabilities, breaking changes, architecture shifts, or big performance/security work
- Sounds ambiguous and you need the authoritative spec before coding

Use `@/openspec/AGENTS.md` to learn:
- How to create and apply change proposals
- Spec format and conventions
- Project structure and guidelines

Keep this managed block so 'openspec update' can refresh the instructions.

<!-- OPENSPEC:END -->

# AGENTS.md - NuGet Toolbox Development Guide

## Quick Start

```bash
# Build solution
dotnet build

# Run all tests
dotnet test

# Run CLI directly
dotnet run --project src/NuGetToolbox.Cli -- find --package "Newtonsoft.Json"

# Format code
dotnet format
```

### Common Commands

| Task | Command |
|------|---------|
| Build | `dotnet build` |
| Test all | `dotnet test` |
| Test by class | `dotnet test --filter PackageResolverTests` |
| Run CLI | `dotnet run --project src/NuGetToolbox.Cli -- <command> <options>` |
| Format | `dotnet format` |
| Check diagnostics | `dotnet build --no-restore` |

## Project Structure

```
src/NuGetToolbox.Cli/
├── Commands/              # System.CommandLine handlers
│   ├── FindCommand.cs     # Resolve package by ID + version
│   ├── ListTypesCommand.cs
│   ├── ExportSignaturesCommand.cs
│   └── DiffCommand.cs
├── Services/              # Core business logic
│   ├── NuGetPackageResolver.cs    # V3 API integration
│   ├── AssemblyInspector.cs       # MetadataLoadContext wrapper
│   ├── SignatureExporter.cs       # C# signature rendering
│   ├── XmlDocumentationProvider.cs
│   └── ApiDiffAnalyzer.cs
├── Models/                # JSON-serializable DTOs
│   ├── PackageInfo.cs
│   ├── TypeInfo.cs
│   ├── MethodInfo.cs
│   └── DiffResult.cs
└── Program.cs             # CLI root setup

tests/NuGetToolbox.Tests/
├── PackageResolverTests.cs
└── UnitTest1.cs
```

## Architecture & Dependencies

**Runtime:**
- `.NET 8+` with `<Nullable>enable</Nullable>`
- `System.CommandLine` (CLI parsing)
- `NuGet.Protocol` + `NuGet.Packaging` + `NuGet.Configuration` (V3 feeds, auth)
- `System.Reflection.MetadataLoadContext` (safe assembly metadata)
- `Microsoft.CodeAnalysis.CSharp` (Roslyn symbol display + doc IDs)

**Testing:**
- `xUnit` with Arrange-Act-Assert pattern
- Mock NuGet sources for offline tests

## Implementation Notes

### Services

**NuGetPackageResolver**
- Resolves `(packageId, version?)` to `.nupkg` via NuGet.Protocol V3
- Respects `nuget.config` source mappings & credential providers
- Returns: `PackageInfo` with resolved version, TFMs, nupkg path

**AssemblyInspector**
- Extracts `List<TypeInfo>` from assembly paths
- Uses `MetadataLoadContext` with `PathAssemblyResolver`
- Never loads into default AppDomain
- Filters public types only (class/interface/struct/enum)

**XmlDocumentationProvider**
- Loads compiler-generated `.xml` (MSBuild doc files)
- Parses `<members>/<member name="...">` elements
- Matches via Roslyn `DocumentationCommentId` (normalized)
- Returns: `Summary`, `Params` dict, `Returns` text

**SignatureExporter**
- Renders method signatures via Roslyn symbol display
- Injects XML docs from provider
- Outputs JSON / JSONL formats
- Filters by namespace (optional)

**ApiDiffAnalyzer**
- Compares two `List<MethodInfo>` sets
- Identifies: breaking changes, added types, removed types
- Returns: `DiffResult` with breaking[] array

### Models

All models use `System.Text.Json` with camelCase `[JsonPropertyName]`:
- `PackageInfo` - package metadata
- `TypeInfo` - { namespace, name, kind }
- `MethodInfo` - { type, method, signature, summary, params, returns }
- `DiffResult` - { breaking[], added[], removed[], compatible }

## Code Style

- **C# 11+** only; C# 12 features OK if `.csproj` updated
- **Nullable:** All public APIs must be annotated (`T`, `T?`)
- **Naming:** `PascalCase` (types/methods), `camelCase` (locals/params)
- **Async:** `async Task` for I/O; no `.Result` or `.Wait()`
- **Logging:** Use `ILogger<T>` with structured context
- **JSON:** `System.Text.Json` only; camelCase property names
- **Imports:** Alphabetically sorted; no wildcards
- **Errors:** Custom exceptions with actionable messages

## Testing Strategy

- **Unit tests:** PackageResolver, AssemblyInspector, SignatureExporter
- **Pattern:** Arrange-Act-Assert
- **Mocking:** Mock `NuGetPackageResolver` for offline tests
- **Coverage:** Focus on TFM selection, doc ID matching, breaking change detection

## Key Decisions Made

1. **MetadataLoadContext only** – Safe, no code execution
2. **Roslyn doc IDs** – Canonical matching for XML docs
3. **camelCase JSON** – LLM-friendly, standard REST API style
4. **System.CommandLine** – Native .NET CLI, no external frameworks
5. **File-based caching** (future) – Versioned by (packageId, version, tfm)
6. **NuGet.Protocol V3** – Modern, supports all auth providers
