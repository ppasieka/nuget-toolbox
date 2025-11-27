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

## Development workflow

- use .NET 8+ and the `dotnet build` command
- Windows with Powershell 7+ to execute and spawn processes (not CMD, Bash, etc.)
- Git for version control - do not commit on your own, but you may use it to show status or inspect commits
- make sure that build is green after you finish your work
- run tests every time you make a change
- in case of missing knowledge you can always use the microsoft documentation tools

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
| Export schema | `dotnet run --project src/NuGetToolbox.Cli -- schema --command <name>` |
| Export all schemas | `dotnet run --project src/NuGetToolbox.Cli -- schema --all --output schemas/` |

## Project Structure

```
src/NuGetToolbox.Cli/
├── Commands/              # System.CommandLine handlers
│   ├── FindCommand.cs     # Resolve package by ID + version
│   ├── ListTypesCommand.cs
│   ├── ExportSignaturesCommand.cs
│   ├── DiffCommand.cs
│   └── SchemaCommand.cs   # Export JSON Schema definitions
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
├── Schemas/               # JSON Schema definitions (embedded resources)
│   ├── models-1.0.schema.json          # Shared model definitions
│   ├── find.schema.json                # Find command output schema
│   ├── list-types.schema.json          # List-types command output schema
│   ├── export-signatures.schema.json   # Export-signatures command output schema
│   └── diff.schema.json                # Diff command output schema
└── Program.cs             # CLI root setup

tests/NuGetToolbox.Tests/
├── PackageResolverTests.cs
├── SchemaCommandTests.cs
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
- Extracts parameter metadata (type + name) and return types from reflection
- Outputs JSON / JSONL formats with unescaped angle brackets for generic types
- Filters by namespace (optional)

**ApiDiffAnalyzer**
- Compares two `List<MethodInfo>` sets
- Identifies: breaking changes, added types, removed types
- Returns: `DiffResult` with breaking[] array

**SchemaCommand**
- Exports JSON Schema (Draft 2020-12) definitions for all CLI command outputs
- Loads schemas from embedded resources (no external files needed)
- Supports `--command <name>` for specific schemas, `--all` for batch export
- Optimized for LLM/AI consumption with comprehensive field descriptions and examples

### Schemas

All schemas are JSON Schema Draft 2020-12 compliant and include:
- **Comprehensive documentation** - Every property has `description`, `examples`, and format constraints
- **Shared definitions** - models-1.0.schema.json contains $defs for PackageInfo, TypeInfo, MethodInfo, etc.
- **Command-specific schemas** - find.schema.json, list-types.schema.json, export-signatures.schema.json, diff.schema.json
- **$ref resolution** - Command schemas reference models schema for reusable definitions
- **LLM/AI optimization** - Semantic annotations, pattern constraints, and examples for AI agent consumption

Schema versioning follows semantic versioning (models-1.0, models-2.0, etc.) with filenames including version numbers.

### Models

All models use `System.Text.Json` with camelCase `[JsonPropertyName]`:
- `PackageInfo` - package metadata
- `TypeInfo` - { namespace, name, kind }
- `MethodInfo` - { type, method, signature, summary, params, returns, parameters, returnType }
  - `parameters` - array of { name, type } from reflection
  - `returnType` - return type from reflection
- `ParameterInfo` - { name, type }
- `DiffResult` - { breaking[], added[], removed[], compatible }

## Code Style

- **C# 11+** only; C# 12 features OK if `.csproj` updated
- Use collection expressions for arrays and lists ( use [ .. ] instead of new [] { .. })
- **Nullable:** All public APIs must be annotated (`T`, `T?`)
- **Naming:** `PascalCase` (types/methods), `camelCase` (locals/params)
- **Async:** `async Task` for I/O; no `.Result` or `.Wait()`
- **Handlers:** Use `command.SetHandler(...)` with `InvocationContext` or `CancellationToken` for cancellation support
- **Logging:** Use `ILogger<T>` with structured context
- **JSON:** `System.Text.Json` only; camelCase property names
- **Imports:** Alphabetically sorted; no wildcards
- **Errors:** Custom exceptions with actionable messages
- **Exit Codes:**
  - 0: Success
  - 1: Package/Version not found
  - 2: Target Framework mismatch or not found
  - 3: Invalid options or arguments
  - 4: Network or Authentication error
  - 5: Unexpected runtime error

## Testing Strategy

- **Unit tests:** PackageResolver, AssemblyInspector, SignatureExporter
- **E2E tests:** End-to-end command execution tests 
- **Pattern:** Arrange-Act-Assert
- **Mocking:** Mock `NuGetPackageResolver` for offline tests
- **Coverage:** Focus on TFM selection, doc ID matching, breaking change detection

### E2E Test Conventions
- E2E tests are in separate `*E2ETests.cs` files (FindCommandE2ETests, ListTypesCommandE2ETests, etc.)
- E2E tests invoke the CLI via `Process.Start` with `dotnet <cli-path> <command> <args>`
- Test subject: `Newtonsoft.Json` version 13.0.1 (stable, well-documented, widely-used)
- Validation: JSON output structure, content accuracy, expected field presence

## Key Decisions Made

1. **MetadataLoadContext only** – Safe, no code execution
2. **Roslyn doc IDs** – Canonical matching for XML docs
3. **camelCase JSON** – LLM-friendly, standard REST API style
4. **System.CommandLine** – Native .NET CLI, no external frameworks
5. **File-based caching** (future) – Versioned by (packageId, version, tfm)
6. **NuGet.Protocol V3** – Modern, supports all auth providers
