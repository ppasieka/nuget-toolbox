# 📦 NuGet Toolbox

> **Inspect NuGet package APIs safely. Extract signatures. Detect breaking changes. Feed LLMs.**

A **CLI tool** for analyzing public APIs in NuGet packages without executing code. Built with **.NET 8+** and **MetadataLoadContext** for secure, deterministic metadata inspection.

---

## ✨ Features

- 🔍 **Find packages** by ID/version via NuGet.Protocol V3
- 📋 **List public types** (classes, interfaces, structs, enums) with namespaces
- 📝 **Export method signatures** with XML documentation and full type metadata (parameters, return types)
- ⚡ **Compare versions** for breaking changes and API diffs
- 🔐 **Safe by design** – metadata-only inspection, zero code execution
- 🔗 **Private feed support** – respects `nuget.config`, Azure Artifacts, GitHub Packages, etc.
- 📊 **LLM-ready output** – compact JSON/JSONL for AI context injection
- 💾 **Smart caching** – versioned by (packageId, version, tfm) with TTL controls
- 🧵 **Async I/O** – non-blocking execution with robust cancellation (`Ctrl+C`)


---

## 🚀 Quick Start

### Installation

```bash
git clone https://github.com/ppasieka/nuget-toolbox.git
cd nuget-toolbox
dotnet build
```

### Usage

```bash
# Find a package
dotnet run --project src/NuGetToolbox.Cli -- find --package "Newtonsoft.Json"

# List types in a package
dotnet run --project src/NuGetToolbox.Cli -- list-types \
  --package "Newtonsoft.Json" --version "13.0.3"

# Export method signatures with docs
dotnet run --project src/NuGetToolbox.Cli -- export-signatures \
  --package "Newtonsoft.Json" --version "13.0.3" --format "jsonl"

# Compare versions for breaking changes
dotnet run --project src/NuGetToolbox.Cli -- diff \
  --package "Newtonsoft.Json" --from "12.0.0" --to "13.0.3"
```

---

## 📖 Commands

### `find` – Resolve Package

Resolve a NuGet package by ID and optional version.

```bash
nuget-toolbox find --package "Newtonsoft.Json"
nuget-toolbox find --package "Newtonsoft.Json" --version "13.0.3"
nuget-toolbox find --package "MyOrg.Widgets" --feed "https://myorg.pkgs.visualstudio.com/nuget/v3/index.json"
```

**Output:**
```json
{
  "packageId": "Newtonsoft.Json",
  "version": "13.0.3",
  "resolved": true,
  "source": "https://api.nuget.org/v3/index.json",
  "nupkgPath": "/home/user/.nuget/cache/newtonsoft.json.13.0.3.nupkg",
  "tfms": ["net6.0", "net8.0", "netstandard2.0"]
}
```

**Options:**
- `--package, -p` **[required]** – Package ID to search
- `--version, -v` – Package version (default: latest)
- `--feed, -f` – NuGet feed URL (default: nuget.org)
- `--output, -o` – Output file path (default: stdout)

---

### `list-types` – Enumerate Types

List all public types (namespace + name + kind) in a package.

```bash
nuget-toolbox list-types --package "Newtonsoft.Json" --version "13.0.3"
nuget-toolbox list-types --package "Newtonsoft.Json" --version "13.0.3" --tfm "net8.0"
nuget-toolbox list-types --package "Newtonsoft.Json" --output "types.json"
```

**Output:**
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

Direct dependencies:
  [net8.0]
    - System.Runtime (>= 4.3.0)
  [netstandard2.0]
    - Microsoft.CSharp (>= 4.3.0)

Tip: To inspect dependencies, run:
  nuget-toolbox list-types --package <DependencyId>
```

**Partial Results:** If a package has missing dependencies, the tool will extract and return all successfully-loaded types. Missing dependencies are logged at Debug level and direct dependencies are displayed to help you inspect them separately.

**Options:**
- `--package, -p` **[required]** – Package ID
- `--version, -v` – Package version (default: latest)
- `--tfm` – Target framework (default: net8.0 → net7.0 → netstandard2.0 → net462)
- `--output, -o` – Output file path (default: stdout)

---

### `export-signatures` – Extract Method Signatures

Export public method signatures with XML documentation.

```bash
nuget-toolbox export-signatures --package "Newtonsoft.Json" --version "13.0.3"
nuget-toolbox export-signatures \
  --package "Newtonsoft.Json" --version "13.0.3" \
  --format "jsonl" --output "signatures.jsonl"
nuget-toolbox export-signatures \
  --package "Newtonsoft.Json" --version "13.0.3" \
  --filter "Newtonsoft.Json.Linq"
```

**Output (JSON):**
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
      "params": { "value": "The object to serialize." },
      "returns": "A JSON string representation of the object."
    }
  ]
}
```

**Output (JSONL):**
```jsonl
{"type":"Newtonsoft.Json.JsonConvert","method":"SerializeObject","signature":"string SerializeObject(object? value)","summary":"Serializes the specified object...","params":{"value":"..."},"returns":"...","parameters":[{"name":"value","type":"System.Object"}],"returnType":"System.String"}
{"type":"Newtonsoft.Json.JsonConvert","method":"DeserializeObject","signature":"object? DeserializeObject(string json)","summary":"...","params":{"json":"..."},"returns":"...","parameters":[{"name":"json","type":"System.String"}],"returnType":"System.Object"}
```

**Options:**
- `--package, -p` **[required]** – Package ID
- `--version, -v` – Package version (default: latest)
- `--tfm` – Target framework (default: auto-detect best fit)
- `--format` – Output format: `json` or `jsonl` (default: json)
- `--filter` – Namespace filter (e.g., `Newtonsoft.Json.Linq`). Alias: `--namespace`.
- `--output, -o` – Output file path (default: stdout)
- `--no-cache` – Bypass cache

---

### `diff` – Compare API Versions

Compare public APIs between two versions to detect breaking changes.

```bash
nuget-toolbox diff --package "Newtonsoft.Json" --from "12.0.0" --to "13.0.3"
nuget-toolbox diff \
  --package "Newtonsoft.Json" --from "12.0.0" --to "13.0.3" \
  --tfm "net8.0" --output "breaking-changes.json"
```

**Output:**
```json
{
  "packageId": "Newtonsoft.Json",
  "versionFrom": "12.0.0",
  "versionTo": "13.0.3",
  "tfm": "net8.0",
  "breaking": [
    {
      "type": "Newtonsoft.Json.JsonConvert",
      "method": "SerializeObject",
      "reason": "method removed"
    },
    {
      "type": "Newtonsoft.Json.Linq.JObject",
      "method": "Constructor",
      "signature": "changed from JObject(JObject other) to JObject()"
    }
  ],
  "added": [
    { "namespace": "Newtonsoft.Json.Serialization", "name": "JsonSerializerSettings", "kind": "class" }
  ],
  "removed": [],
  "compatible": false
}
```

**Options:**
- `--package, -p` **[required]** – Package ID
- `--from` **[required]** – From version
- `--to` **[required]** – To version
- `--tfm` – Target framework (default: auto-detect)
- `--output, -o` – Output file path (default: stdout)

---

### `schema` – Export JSON Schemas

Export JSON Schema definitions for command outputs, optimized for validation and LLM/AI consumption.

```bash
# Export schema for a specific command
nuget-toolbox schema --command find

# Export schema for all commands
nuget-toolbox schema --all

# Export all schemas to a directory
nuget-toolbox schema --all --output schemas/

# Export models schema (shared definitions)
nuget-toolbox schema --command models
```

**Schema Features:**
- ✅ **JSON Schema Draft 2020-12** – Latest standard
- ✅ **Comprehensive documentation** – Every field has descriptions, examples, and format constraints
- ✅ **LLM/AI optimized** – Designed for consumption by AI agents with semantic annotations
- ✅ **Shared models** – Reusable definitions via `$ref` to models schema
- ✅ **Validation ready** – Use with any JSON Schema validator

**Output (find.schema.json):**
```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "https://nuget-toolbox.local/schemas/find-1.0.schema.json",
  "title": "NuGetToolbox Find Command Output",
  "description": "JSON output schema for the 'find' command...",
  "$ref": "models-1.0.schema.json#/$defs/PackageInfo",
  "examples": [...]
}
```

**Options:**
- `--command, -c` – Command name (find, list-types, export-signatures, diff, models)
- `--all, -a` – Export all schemas
- `--output, -o` – Output file or directory path (default: stdout)

**Validating command outputs:**
```bash
# Using ajv-cli
npm install -g ajv-cli
nuget-toolbox find --package "Newtonsoft.Json" > result.json
nuget-toolbox schema --command find > find.schema.json
ajv validate -s find.schema.json -d result.json

# Using Python jsonschema
pip install jsonschema
python -c "
import json, jsonschema
with open('find.schema.json') as sf, open('result.json') as df:
    jsonschema.validate(json.load(df), json.load(sf))
"
```

---

## 🏗️ Architecture

```
NuGetToolbox.Cli
├── Commands/              # System.CommandLine handlers
│   ├── FindCommand.cs
│   ├── ListTypesCommand.cs
│   ├── ExportSignaturesCommand.cs
│   └── DiffCommand.cs
├── Services/              # Core business logic
│   ├── NuGetPackageResolver       # V3 API integration
│   ├── AssemblyInspector          # MetadataLoadContext wrapper
│   ├── SignatureExporter          # Roslyn symbol display + XML docs
│   ├── XmlDocumentationProvider   # XML doc parsing & matching
│   └── ApiDiffAnalyzer            # Breaking change detection
├── Models/                # JSON-serializable DTOs
│   ├── PackageInfo
│   ├── TypeInfo
│   ├── MethodInfo
│   └── DiffResult
└── Program.cs             # CLI root setup
```

### Key Services

| Service | Purpose |
|---------|---------|
| **NuGetPackageResolver** | Resolves & downloads `.nupkg` via NuGet.Protocol V3; respects `nuget.config` & credential providers |
| **AssemblyInspector** | Extracts public types using `MetadataLoadContext` (safe, no execution) |
| **XmlDocumentationProvider** | Parses compiler-generated `.xml` files; matches docs via Roslyn `DocumentationCommentId` |
| **SignatureExporter** | Renders C# method signatures via Roslyn symbol display; injects XML docs |
| **ApiDiffAnalyzer** | Compares two API snapshots; identifies breaking changes, additions, removals |

---

## 🔐 Security

- ✅ **No code execution** – MetadataLoadContext only, never `Assembly.Load`
- ✅ **Credentials safe** – NuGet acquires via standard credential providers/`nuget.config`
- ✅ **No secrets embedded** – Configuration via environment variables or local config
- ✅ **Sandboxed analysis** – Each package inspected in isolated metadata context

---

## 📦 Private Feed Support

Configure private feeds via standard `nuget.config`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="MyOrg" value="https://myorg.pkgs.visualstudio.com/nuget/v3/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <MyOrg>
      <add key="Username" value="oauth" />
      <add key="ClearTextPassword" value="[PAT or token]" />
    </MyOrg>
  </packageSourceCredentials>
</configuration>
```

Or use environment variables (Azure DevOps, GitHub, Nexus, etc.):
```bash
export VSS_NUGET_EXTERNAL_FEED_ENDPOINTS='{"endpointCredentials":[...]}'
```

---

## 💾 Caching

Results are cached by `(packageId, version, tfm)` with TTL. Override:

```bash
nuget-toolbox export-signatures --package "Newtonsoft.Json" --no-cache
```

Cache location: `~/.nuget-toolbox/cache/`

---

## 🛠️ Development

### Prerequisites

- **.NET 8+** SDK
- **C# 11+** compiler

### Build & Test

```bash
# Build
dotnet build

# Run all tests
dotnet test

# Run specific test class
dotnet test --filter PackageResolverTests

# Format code
dotnet format

# Run CLI
dotnet run --project src/NuGetToolbox.Cli -- find --package "Newtonsoft.Json"
```

### Code Style

- **Nullable** annotations on all public APIs
- **PascalCase** for types/methods; **camelCase** for locals/params
- **Async/await** for I/O; no `.Result` or `.Wait()`
- **Structured logging** via `ILogger<T>`
- **JSON** via `System.Text.Json` with camelCase property names
- **Imports** alphabetically sorted; no wildcards

---

## 📊 Real-World Examples

### Generate API snapshot for docs

```bash
nuget-toolbox export-signatures \
  --package "MyOrg.Platform" \
  --version "2.1.0" \
  --format "json" > api-snapshot.json

# Feed into LLM context for code generation
cat api-snapshot.json | jq . > mcp-context.json
```

### Detect breaking changes in CI/CD

```bash
nuget-toolbox diff \
  --package "MyOrg.Platform" \
  --from "2.0.0" \
  --to "2.1.0" \
  --output "breaking-changes.json"

# Exit non-zero if breaking changes found
if grep -q '"compatible": false' breaking-changes.json; then
  echo "⚠️ Breaking changes detected!"
  exit 1
fi
```

### Analyze multiple versions

```bash
for version in "1.0.0" "1.5.0" "2.0.0"; do
  nuget-toolbox export-signatures \
    --package "MyOrg.Core" \
    --version "$version" \
    --output "api-$version.jsonl"
done
```

### Filter by namespace

```bash
nuget-toolbox export-signatures \
  --package "Newtonsoft.Json" \
  --version "13.0.3" \
  --filter "Newtonsoft.Json.Linq" \
  --format "jsonl"
```

---

## 🎯 Use Cases

| Use Case | Command |
|----------|---------|
| Generate API docs | `export-signatures --format json` |
| Feed LLM context | `export-signatures \| jq .methods` |
| Breaking change detection | `diff --from X --to Y` |
| Type inventory | `list-types --tfm net8.0` |
| Private feed validation | `find --package MyOrg.Pkg --feed https://...` |
| CI/CD integration | Pipe output to JSON parser/validator |

---

## 🔧 Configuration

### Exit Codes

- `0` - Success
- `1` - Package/Version not found
- `2` - Target Framework mismatch or not found
- `3` - Invalid options or arguments
- `4` - Network or Authentication error
- `5` - Unexpected runtime error

### TFM Selection

The tool uses NuGet's `FrameworkReducer.GetNearest()` for intelligent TFM selection:

- **Automatic selection**: Based on runtime (e.g., running on .NET 8 prefers net8.0 > net6.0 > netstandard2.0)
- **Cross-family compatibility**: Correctly handles `netstandard2.0` for `net8.0` runtime (unlike naive version sorting)
- **Override**: Use `--tfm net6.0` to specify explicitly

### Reference Assembly Preference

When extracting assemblies from packages:

- **ref/ preferred**: Reference assemblies (`ref/`) are used when available for cleaner API surface
- **lib/ fallback**: Falls back to `lib/` when `ref/` is empty
- **XML docs**: Retrieved from `lib/` even when using `ref/` assemblies (ref/ typically doesn't include XML docs)

### Deterministic Output

All output arrays are sorted for consistent, reproducible results:

- `TypeInfo`: sorted by `namespace`, then `name`
- `MethodInfo`: sorted by `type`, then `method`, then `signature`  
- `DiffResult` arrays: sorted by `type`, then `signature`

### Logging Levels

```bash
dotnet run --project src/NuGetToolbox.Cli -- \
  export-signatures --package "MyPkg" --verbose

dotnet run --project src/NuGetToolbox.Cli -- \
  export-signatures --package "MyPkg" --debug
```

---

## ⚠️ Limitations

- **XML docs** appear only if package ships `.xml` file alongside DLL
- **Generic method doc IDs** edge cases normalized; unmapped docs not synthesized
- **Obfuscated/mixed-mode** assemblies may reduce signature readability
- **Large packages** (>100MB) may require `--no-cache` due to extraction overhead
- **Missing dependencies**: The tool only inspects the requested package, not its dependency tree. If types reference missing dependencies, those types are skipped and logged at Debug level. Direct dependencies are listed to help you inspect them separately.

---

## 📝 Output Schema

### MethodInfo

```json
{
  "type": "Namespace.ClassName",
  "method": "MethodName",
  "signature": "ReturnType MethodName(param1, param2)",
  "summary": "Description from XML docs",
  "params": {
    "paramName": "Description"
  },
  "returns": "Return value description",
  "parameters": [
    { "name": "param1", "type": "System.String" },
    { "name": "param2", "type": "System.Int32" }
  ],
  "returnType": "System.Boolean"
}
```

### DiffResult

```json
{
  "packageId": "...",
  "versionFrom": "1.0.0",
  "versionTo": "2.0.0",
  "tfm": "net8.0",
  "breaking": [ { "type": "...", "method": "...", "reason": "..." } ],
  "added": [ { "namespace": "...", "name": "...", "kind": "..." } ],
  "removed": [],
  "compatible": false
}
```

---

## 🤝 Contributing

1. Fork & clone
2. Create feature branch (`git checkout -b feature/xyz`)
3. Code with style (see AGENTS.md)
4. Add tests (Arrange-Act-Assert)
5. Format: `dotnet format`
6. Commit & push
7. PR with description

---

## 📄 License

MIT – See LICENSE file

---

## 🚀 What's Next?

- [ ] File-based caching with TTL controls
- [ ] Structured logging output (JSON logs)
- [ ] Config file support (yaml/toml)
- [ ] NuGet tool publish (`dotnet tool install nuget-toolbox`)
- [ ] Parallel package resolution
- [ ] Web UI dashboard
- [ ] CI/CD integrations (GitHub Actions, Azure Pipelines)

---

## 🤔 FAQ

**Q: Is this tool safe to use with untrusted packages?**  
A: Yes! We use `MetadataLoadContext` exclusively—zero code execution. Only metadata is inspected.

**Q: Can I use this offline?**  
A: After initial download, yes. Use `--no-cache` to skip previous caches, but package must be downloaded first.

**Q: Does this respect authentication?**  
A: Yes! Standard NuGet credential providers (Azure, GitHub, Nexus, etc.) are supported via `nuget.config`.

**Q: What target frameworks are supported?**  
A: Any .NET framework TFM in the package. We prefer modern first (net8.0 → netstandard2.0).

**Q: How large can packages be?**  
A: Tested on packages up to 100MB. Larger packages may require extended timeouts.

**Q: What happens if a package has missing dependencies?**  
A: The tool gracefully handles missing dependencies by extracting partial results. It will:
- Return all successfully-loaded types
- Log missing dependencies at Debug level (not shown by default)
- Display direct dependencies from the package's `.nuspec` file
- Suggest commands to inspect those dependencies separately
- Exit with code 0 if partial results were obtained successfully

---

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/ppasieka/nuget-toolbox/issues)
- **Discussions**: [GitHub Discussions](https://github.com/ppasieka/nuget-toolbox/discussions)

---

**Made with ❤️ for the .NET community**
