# Project Context

## Purpose

**NuGet Toolbox** is a CLI tool for analyzing public APIs in NuGet packages safely and efficiently. It enables:
- Package discovery and resolution via NuGet.Protocol V3
- Public API enumeration (types, methods, signatures)
- XML documentation extraction and attachment
- Breaking change detection between versions
- LLM-ready structured output for AI context injection

Primary use case: Feed LLM contexts with clean API signatures, generate API documentation, detect breaking changes in CI/CD pipelines, and validate private package compatibility.

## Tech Stack

**Runtime:**
- **.NET 8+** with `<Nullable>enable</Nullable>` (required)
- **C# 11+** (prefer C# 12 features if `.csproj` updated)

**Core Libraries:**
- `System.CommandLine` – CLI parsing with help/version support
- `NuGet.Protocol` – V3 feed resolution and authentication
- `NuGet.Packaging` – Nupkg inspection and assembly extraction
- `NuGet.Configuration` – nuget.config parsing and credential providers
- `System.Reflection.MetadataLoadContext` – Safe, isolated assembly inspection (no code execution)
- `Microsoft.CodeAnalysis.CSharp` – Roslyn symbol display for method signatures + DocumentationCommentId for XML doc matching

**Testing:**
- `xUnit` with Arrange-Act-Assert pattern
- Mock `NuGetPackageResolver` for offline/integration tests

**Output Formats:**
- `System.Text.Json` only (no Newtonsoft.Json); camelCase property names via `[JsonPropertyName]`

## Project Conventions

### Code Style

**General:**
- **Nullable annotations required** on all public APIs (`T` vs `T?`); `#pragma` allowed only for build-generated files
- **PascalCase** for types, methods, properties; **camelCase** for local variables, parameters
- **Async/await for I/O** – never `.Result` or `.Wait()`; all async methods return `Task` or `Task<T>`
- **Imports alphabetically sorted**; no wildcard imports
- **Line length:** Prefer ≤120 characters; no hard limit

**Naming:**
- Service classes: `{Domain}Service` or `{Domain}{Capability}` (e.g., `NuGetPackageResolver`, `XmlDocumentationProvider`)
- Command classes: `{Action}Command` (e.g., `FindCommand`, `ExportSignaturesCommand`)
- DTO/Model classes: `{Entity}Info`, `{Entity}Result`, `{Entity}Details`
- Private fields: `_camelCase`; local variables: `camelCase`

**Logging:**
- Inject `ILogger<T>` into constructors
- Use structured logging: `logger.LogInformation("Resolved {PackageId} to version {Version}", id, ver)`
- Avoid string concatenation in messages; use `{param}` placeholders

**Error Handling:**
- Custom exceptions inherit from `Exception`; include actionable messages
- Example: `throw new InvalidOperationException($"Package '{packageId}' not found in source '{source}'");`
- Do NOT suppress exceptions silently

### Architecture Patterns

**Layered with clear separation:**

```
Commands/ (System.CommandLine handlers)
  ↓
Services/ (Core business logic, stateless)
  ↓
Models/ (JSON-serializable DTOs)
```

**Service Characteristics:**
- **Stateless**: All state passed via parameters or DI
- **Single responsibility**: One service per domain concern
- **Async-first**: All I/O (network, file) uses `async Task`
- **Dependency injection**: Constructor-injected `ILogger<T>`, `NuGetPackageResolver`, etc.

**Key Services:**
1. **NuGetPackageResolver** – Handles NuGet.Protocol V3 resolution, auth, cache lookups
2. **AssemblyInspector** – MetadataLoadContext setup and type enumeration (never `Assembly.Load` to default context)
3. **XmlDocumentationProvider** – Parses `.xml` files and normalizes doc IDs via Roslyn
4. **SignatureExporter** – Renders C# signatures and injects docs
5. **ApiDiffAnalyzer** – Compares two MethodInfo[] arrays and identifies breaking changes

**Models:**
- All inherit from reference types (classes)
- Use `[JsonPropertyName("camelCase")]` for property serialization
- Include nullable annotations (`string?`, `IList<T>?`)
- Example:
  ```csharp
  public class MethodInfo
  {
      [JsonPropertyName("type")]
      public string Type { get; init; }  // "Namespace.ClassName"
      
      [JsonPropertyName("method")]
      public string Method { get; init; }
      
      [JsonPropertyName("signature")]
      public string Signature { get; init; }
      
      [JsonPropertyName("summary")]
      public string? Summary { get; init; }
  }
  ```

### Testing Strategy

**Pattern: Arrange-Act-Assert (AAA)**

```csharp
[Fact]
public async Task ExportSignatures_WithValidPackage_ReturnsMethodsWithDocs()
{
    // Arrange
    var resolver = new Mock<INuGetPackageResolver>();
    resolver.Setup(r => r.ResolveAsync("Newtonsoft.Json", "13.0.3"))
        .ReturnsAsync(new PackageInfo { ... });
    var exporter = new SignatureExporter(resolver.Object);

    // Act
    var result = await exporter.ExportAsync("Newtonsoft.Json", "13.0.3");

    // Assert
    Assert.NotEmpty(result.Methods);
    Assert.NotNull(result.Methods[0].Summary);
}
```

**Coverage:**
- Unit tests for `NuGetPackageResolver` (mocked sources), `AssemblyInspector`, `SignatureExporter`, `ApiDiffAnalyzer`
- Integration tests for end-to-end command execution (small real packages like `MinimalJson`)
- Mock or skip external NuGet feeds in unit tests

**Running Tests:**
```bash
dotnet test
dotnet test --filter PackageResolverTests
dotnet test --filter "ClassName and MethodName"
```

### Git Workflow

**Branching:**
- `master` – Production-ready, always buildable
- `feature/*` – Feature branches from `master`
- `bugfix/*` – Bug fixes
- `chore/*` – Dependency updates, documentation

**Commits:**
- **Conventional Commits** preferred:
  - `feat: add package caching layer`
  - `fix: correct doc ID normalization for generic methods`
  - `refactor: simplify MetadataLoadContext setup`
  - `chore: update NuGet.Protocol to 6.9.0`
  - `test: add tests for diff analyzer`
- **Imperative mood**: "add" not "added", "fix" not "fixed"
- **Link issue numbers**: `fixes #42`, `ref #101`

**Code Review:**
- All PRs require `dotnet build` and `dotnet test` to pass
- Run `dotnet format` before pushing
- Ensure nullable annotations are complete

## Domain Context

**NuGet Ecosystem Knowledge:**
- **Packages as ZIP archives**: `.nupkg` is a ZIP with `/lib/<tfm>/*.dll` and optional `/lib/<tfm>/<dll>.xml` for docs
- **TFM (Target Framework Moniker)**: `net8.0`, `net7.0`, `netstandard2.0`, `net462`, etc. Higher versions are preferred
- **NuGet.Protocol V3**: Modern API (vs deprecated V2); uses JSON metadata endpoints
- **Credential providers**: Azure Artifacts, GitHub Packages, Nexus, Artifactory—all supported via standard `nuget.config`

**Metadata-Only Inspection:**
- `MetadataLoadContext` allows reading assembly metadata without JIT compiling or executing code
- Used with `PathAssemblyResolver` to provide runtime assemblies
- **Never** use `Assembly.Load` or `Assembly.LoadFrom` to the default AppDomain

**Documentation Comment IDs:**
- C# compiler generates doc IDs for members: `M:Namespace.Type.Method(ParamTypes)`, `T:Namespace.Type`
- Roslyn's `DocumentationCommentId` normalizes these for XML doc matching
- Generic methods require special handling (e.g., `List{T}` → `List{`1`)

**Output for LLMs:**
- JSON/JSONL is preferred; compact, structured, no markdown escaping needed
- Include both signature (for syntax) and documentation (for semantics)
- Deterministic ordering (alphabetical by type, then method name) aids reproducibility

## Important Constraints

**Safety & Security:**
- ✅ **No code execution**: MetadataLoadContext only
- ✅ **No secrets embedded**: Credentials via environment variables or `nuget.config`
- ✅ **Sandboxed analysis**: Each package in isolated context

**Performance:**
- Large packages (>100MB) may require extended timeouts
- Cache results by `(packageId, version, tfm)` to avoid re-analysis
- Parallel package downloads (future) if needed

**Compatibility:**
- **Minimum .NET 8.0** – No downgrade to .NET 6 or 7
- **C# 11+ only** – Use newer language features freely
- Prefer `System.Text.Json` over external JSON libraries

**Observability:**
- Must support `--verbose` and `--debug` flags for troubleshooting
- Structured logging with `ILogger<T>`
- Exit codes: `0` for success, `1` for errors, `2` for validation failures

## External Dependencies

**NuGet.org & Feed Resolution:**
- `https://api.nuget.org/v3/index.json` (default public feed)
- Private feeds: Azure Artifacts, GitHub Packages, Nexus, Artifactory
- Authenticated via `nuget.config` or environment variables

**Roslyn (Microsoft.CodeAnalysis.CSharp):**
- Symbol display for idiomatic C# method signatures
- Documentation comment ID normalization
- Generic type parameter handling

**NuGet SDK (NuGet.Protocol, NuGet.Packaging):**
- V3 feed resolution and package download
- `.nupkg` extraction and assembly discovery
- Credential provider integration

**System Libraries:**
- `System.Reflection.MetadataLoadContext` – Safe metadata inspection
- `System.Text.Json` – JSON serialization
- `System.CommandLine` – CLI command parsing

**GitHub/Public Repositories (for testing):**
- Small public packages (e.g., `MinimalJson`, `Newtonsoft.Json`) for integration tests
- Must be publicly available and not require authentication
