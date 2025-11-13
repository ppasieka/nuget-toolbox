# NuGet Toolbox CLI

A **command-line tool** for inspecting NuGet package **public APIs** and extracting method signatures with XML documentation—designed as an Ampcode toolbox integration.

- **Find** a NuGet package by **name** and **version**
- **List** public types the package exposes (classes, interfaces, structs, enums)
- **Export** public method signatures with **XML documentation** (summary/params/returns)

Built with **.NET 8+** and **MetadataLoadContext** for safe assembly inspection (no code execution). Supports private feeds using standard **NuGet.Protocol** and credential providers (Azure Artifacts, GitHub Packages, Nexus/Artifactory, etc.).

**References:** NuGet Client SDK | MetadataLoadContext | XML docs | Roslyn symbol display | [anthropic.com](https://www.anthropic.com/news/model-context-protocol) | [modelcontextprotocol.github.io](https://modelcontextprotocol.github.io/)

---

## Why this tool?

- **Safe by design**: Assemblies opened for **metadata only**—no runtime load or user code execution
- **LLM-ready output**: Returns compact, structured JSON with type, method, signature, and doc text—ideal for feeding into AI contexts
- **Works with private feeds**: Honors your `nuget.config` and credential providers for authenticated registries
- **CLI-native**: Integrates with shell scripts, Ampcode workflows, and CI/CD pipelines
- **Cacheable outputs**: Generate and version control API signatures for diff-based API compatibility checks

---

## Features

- **Package resolution**: `packageId` + optional `version`. If version omitted, resolves latest via **NuGet.Protocol V3**
- **Assembly selection**: Extracts `/lib/<tfm>/*.dll` from `.nupkg` and auto-chooses best-fit TFM (configurable)
- **Public contract listing**: Namespace + public types (class/interface/struct/enum)
- **Signatures + docs**:
  - Method signatures rendered in idiomatic C# via Roslyn symbol display
  - XML docs (`<summary>`, `<param>`, `<returns>`) if package ships XML documentation file alongside DLL
  - Documentation lookups keyed by canonical **documentation comment IDs** using Roslyn `DocumentationCommentId` with normalization

---

## High-level architecture

1. **Resolve & download package**
   - Uses **NuGet.Protocol** (`Repository.Factory.GetCoreV3`, `FindPackageByIdResource.CopyNupkgToStreamAsync`)
   - Auth via `nuget.config`/credential provider; no secrets hard-coded

2. **Extract assemblies**
   - Unzip `.nupkg`, pick `/lib/<tfm>` by configured preference (e.g., `net8.0`, `netstandard2.0`)
   - Collect all DLLs from selected TFM

3. **Inspect metadata (no execution)**
   - Load DLLs into **`MetadataLoadContext`** with `PathAssemblyResolver` seeded with runtime BCL + extracted assemblies
   - Enumerate **public** types & methods and render **signatures**

4. **Attach XML docs**
   - If `<dll>.xml` is present, parse `<members>/<member name="...">`
   - Generate doc IDs from symbols via Roslyn `DocumentationCommentId` and normalize to match compiler XML

5. **Output to stdout/file**
   - Compact JSONL or JSON array with: `type`, `method`, `signature`, `summary`, `params`, `returns`

---

## CLI Commands

### 1) `nuget-toolbox find`

Resolve a package by ID and optional version.

```bash
nuget-toolbox find --package "Newtonsoft.Json"
nuget-toolbox find --package "Newtonsoft.Json" --version "13.0.3"
nuget-toolbox find --package "MyOrg.Widgets" --feed "https://myorg.pkgs.visualstudio.com/nuget/v3/index.json"
```

**Output**

```json
{
  "packageId": "Newtonsoft.Json",
  "version": "13.0.3",
  "resolved": true,
  "source": "https://api.nuget.org/v3/index.json",
  "nupkgPath": "file:///home/user/.nuget/cache/newtonsoft.json.13.0.3.nupkg",
  "tfms": ["net6.0", "net8.0", "netstandard2.0"]
}
```

---

### 2) `nuget-toolbox list-types`

List public **types** (namespace + name + kind) for a package.

```bash
nuget-toolbox list-types --package "Newtonsoft.Json" --version "13.0.3"
nuget-toolbox list-types --package "Newtonsoft.Json" --version "13.0.3" --tfm "net8.0"
nuget-toolbox list-types --package "Newtonsoft.Json" --version "13.0.3" --output "types.json"
```

**Output (JSON)**

```json
{
  "packageId": "Newtonsoft.Json",
  "version": "13.0.3",
  "tfm": "net8.0",
  "types": [
    { "namespace": "Newtonsoft.Json", "name": "JsonConvert", "kind": "class" },
    { "namespace": "Newtonsoft.Json.Linq", "name": "JObject", "kind": "class" },
    { "namespace": "Newtonsoft.Json.Serialization", "name": "JsonSerializerSettings", "kind": "class" }
  ]
}
```

---

### 3) `nuget-toolbox export-signatures`

Emit public methods with C# signatures and XML doc text (when available).

```bash
nuget-toolbox export-signatures --package "Newtonsoft.Json" --version "13.0.3" --tfm "net8.0"
nuget-toolbox export-signatures --package "Newtonsoft.Json" --version "13.0.3" --format "jsonl" --output "signatures.jsonl"
nuget-toolbox export-signatures --package "Newtonsoft.Json" --version "13.0.3" --filter "Newtonsoft.Json.Linq"
```

**Output (JSONL)**

```json
{"type":"Newtonsoft.Json.JsonConvert","method":"SerializeObject","signature":"string SerializeObject(object? value)","summary":"Serializes the specified object to a JSON string.","params":{"value":"The object to serialize."},"returns":"A JSON string representation of the object."}
{"type":"Newtonsoft.Json.JsonConvert","method":"DeserializeObject","signature":"object? DeserializeObject(string json)","summary":"Deserializes the JSON to a .NET object.","params":{"json":"The JSON string."},"returns":"The deserialized object."}
{"type":"Newtonsoft.Json.Linq.JObject","method":"Parse","signature":"static JObject Parse(string json)","summary":"Parses the specified JSON string into a JObject.","params":{"json":"The JSON string to parse."},"returns":"A JObject instance."}
```

---

### 4) `nuget-toolbox diff`

Compare public API between two versions (breaking/behavioral changes).

```bash
nuget-toolbox diff --package "Newtonsoft.Json" --from "12.0.0" --to "13.0.3" --tfm "net8.0"
nuget-toolbox diff --package "Newtonsoft.Json" --from "12.0.0" --to "13.0.3" --output "breaking-changes.json"
```

**Output (JSON)**

```json
{
  "packageId": "Newtonsoft.Json",
  "versionFrom": "12.0.0",
  "versionTo": "13.0.3",
  "tfm": "net8.0",
  "breaking": [
    { "type": "Newtonsoft.Json.JsonConvert", "method": "SerializeObject", "reason": "method removed" },
    { "type": "Newtonsoft.Json.Linq.JObject", "method": "Constructor", "signature": "changed from JObject(JObject other) to JObject()" }
  ],
  "added": [
    { "type": "Newtonsoft.Json.Serialization", "name": "JsonSerializerSettings", "kind": "class" }
  ],
  "removed": [],
  "compatible": true
}
```

---

## Requirements the tool **must** meet

1. **CLI Interface**
   - Expose commands via `dotnet run` or compiled `.exe`; support `--help`, `--version`
   - Support both positional and named arguments
   - Return structured JSON/JSONL output to stdout or file

2. **Package Discovery & Download**
   - Use **NuGet.Protocol V3** to resolve and download from configured sources
   - Respect `nuget.config` source mappings and credentials; do **not** embed secrets

3. **Assembly Inspection (no execution)**
   - All inspection must use **MetadataLoadContext**; never `Assembly.Load` into default context

4. **Public Contract Enumeration**
   - Enumerate and return **public** types (namespace, name, kind)
   - Hide compiler-generated/private/internal members by default

5. **Method Signatures & Documentation**
   - Emit **public methods** with C# signatures (Roslyn symbol display)
   - Attach XML docs when `.xml` file present; include `summary`, `param[name]`, `returns`
   - Generate member IDs using Roslyn `DocumentationCommentId` and normalize to match compiler XML IDs

6. **TFM Handling**
   - Support explicit `--tfm` input; otherwise apply deterministic preference order
   - Aggregate results across all relevant DLLs under chosen TFM

7. **Caching**
   - Cache downloaded `.nupkg` files and derived analyses by `(packageId, version, tfm)` with TTL controls
   - Support `--no-cache` flag to bypass cache

8. **Limits & Safety**
   - Enforce size/time limits (max nupkg size, max assemblies, max output rows) with actionable errors
   - Sanitize all file paths; never execute code from analyzed assemblies

9. **Observability**
   - Structured logs for: package resolution, download, extraction, analysis time, cache hits/misses
   - Support `--verbose` and `--debug` flags

10. **Determinism**
    - Given same `(packageId, version, tfm)` the output must be stable (ordering, formatting)

---

## Server runtime prerequisites

- **.NET 8+** runtime
- **NuGet packages**:
  - `NuGet.Protocol` (and `NuGet.Packaging`, `NuGet.Configuration` transitively)
  - `System.Reflection.MetadataLoadContext`
  - `Microsoft.CodeAnalysis.CSharp` (for symbol display & doc IDs)

---

## Example usage in Ampcode workflows

```bash
# Export latest API from internal package
nuget-toolbox export-signatures --package "MyCorp.Platform" --output "api-snapshot.jsonl"

# Generate diff between versions for breaking change detection
nuget-toolbox diff --package "MyCorp.Platform" --from "2.0.0" --to "2.1.0" > breaking-changes.json

# Feed into LLM context (e.g., for code generation)
nuget-toolbox export-signatures --package "MyCorp.Platform" --version "2.1.0" --format "json" | jq . > mcp-context.json
```

---

## Directory structure

```
nuget-toolbox/
├── src/
│   ├── NuGetToolbox.Cli/
│   │   ├── Program.cs              # Entry point & command setup
│   │   ├── Commands/
│   │   │   ├── FindCommand.cs
│   │   │   ├── ListTypesCommand.cs
│   │   │   ├── ExportSignaturesCommand.cs
│   │   │   └── DiffCommand.cs
│   │   ├── Services/
│   │   │   ├── NuGetPackageResolver.cs
│   │   │   ├── AssemblyInspector.cs
│   │   │   ├── XmlDocumentationProvider.cs
│   │   │   ├── SignatureExporter.cs
│   │   │   └── ApiDiffAnalyzer.cs
│   │   └── Models/
│   │       ├── PackageInfo.cs
│   │       ├── TypeInfo.cs
│   │       ├── MethodInfo.cs
│   │       └── DiffResult.cs
│   └── NuGetToolbox.Cli.csproj
├── tests/
│   ├── NuGetToolbox.Tests/
│   │   ├── PackageResolverTests.cs
│   │   ├── AssemblyInspectorTests.cs
│   │   └── SignatureExporterTests.cs
│   └── NuGetToolbox.Tests.csproj
├── README.md
├── nuget.config                    # Standard NuGet config (optional override)
└── NuGetToolbox.sln
```

---

## Output schema

### JSON (single output)

```json
{
  "packageId": "Newtonsoft.Json",
  "version": "13.0.3",
  "tfm": "net8.0",
  "exportedAt": "2025-11-13T10:30:00Z",
  "methods": [
    {
      "type": "Newtonsoft.Json.JsonConvert",
      "method": "SerializeObject",
      "signature": "string SerializeObject(object? value)",
      "summary": "Serializes the specified object to a JSON string.",
      "params": {
        "value": "The object to serialize."
      },
      "returns": "A JSON string representation of the object."
    }
  ]
}
```

### JSONL (one method per line)

```jsonl
{"type":"Newtonsoft.Json.JsonConvert","method":"SerializeObject","signature":"string SerializeObject(object? value)","summary":"Serializes the specified object...","params":{"value":"..."},"returns":"..."}
{"type":"Newtonsoft.Json.JsonConvert","method":"DeserializeObject","signature":"...","summary":"...","params":{},"returns":"..."}
```

---

## Configuration

### NuGet sources & authentication

- Configure feeds via standard `nuget.config` (machine/user/repo-local)
- For private feeds, use appropriate credential provider or environment variable macros
- Server respects all standard NuGet credential providers (Azure, GitHub, Nexus, etc.)

### TFM selection

- Prefer `net8.0` → `net7.0` → `netstandard2.0` → `net462` (configurable)
- Override with `--tfm` flag in any command

---

## Limitations

- XML docs appear **only if** package ships compiler-generated `.xml` file
- Obfuscated or mixed-mode (C++/CLI) assemblies may reduce readability
- Generic method doc ID edge cases are normalized; docs not in upstream XML cannot be synthesized

---

## Security & privacy

- **No execution** of third-party code; metadata-only inspection
- **Credentials never stored** by tool; NuGet acquires via standard providers/`nuget.config`
- Outputs contain public API names/summary text; treat according to your data governance

---

## Success criteria checklist

- ✅ CLI tool with `find`, `list-types`, `export-signatures`, `diff` commands
- ✅ Uses MetadataLoadContext (no code execution)
- ✅ Respects NuGet.Protocol V3 and credential providers
- ✅ JSON/JSONL output suitable for feeding into AI contexts or diff tools
- ✅ Caching by `(packageId, version, tfm)` with TTL and `--no-cache` override
- ✅ Structured logging with `--verbose` and `--debug` flags
- ✅ Deterministic output (stable ordering)
- ✅ Works with private feeds (Azure Artifacts, GitHub Packages, etc.)
- ✅ Size/time limits with actionable errors
- ✅ Comprehensive help text (`--help` on all commands)

---

## Next steps

1. Create `.sln` and project structure
2. Implement `NuGetPackageResolver` service (NuGet.Protocol integration)
3. Implement `AssemblyInspector` service (MetadataLoadContext setup)
4. Implement `XmlDocumentationProvider` (XML doc parsing & matching)
5. Implement CLI commands via System.CommandLine
6. Add caching layer (file-based with TTL)
7. Add comprehensive tests
8. Publish as NuGet tool or standalone executable
