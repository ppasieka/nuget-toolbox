# Schema Export Command Design

## Context

NuGetToolbox produces structured JSON output for all commands, but users have no formal contract for validating outputs. JSON Schema provides a standardized, tooling-friendly way to define and validate JSON structures. This design adds a `schema` command to export JSON Schema (Draft 2020-12) definitions for all CLI outputs.

## Goals / Non-Goals

**Goals:**
- Provide JSON Schema files for all command outputs
- Use latest stable JSON Schema standard (Draft 2020-12)
- Enable per-command schema access for targeted validation
- Support schema reuse via shared $defs for common models
- Keep schemas synchronized with code via embedded resources
- Simple implementation (<100 lines of new code, no external dependencies)
- **Comprehensive documentation/annotations for LLM/AI agent consumption**
- Include descriptions, examples, and format constraints for all fields

**Non-Goals:**
- Runtime schema generation from C# types (use static schema files)
- Auto-validation of command outputs (users opt-in via --validate flag or external tools)
- Schema versioning infrastructure (v1.0 only for now)
- Bundled/compound schemas (only per-command + shared models)

## Decisions

### Decision 1: Per-Command Schemas with Shared Models
Generate individual schema files per command, with a shared models schema containing $defs.

**Rationale:**
- **Usability:** Consumers know which command they ran and can validate against that specific schema
- **Maintainability:** Changes to one command's output only touch its schema
- **Common pattern:** Matches standard practice in CLI tools (e.g., kubectl, terraform)
- **Versioning:** Per-command versioning avoids breaking all schemas for a single change

**Alternatives considered:**
- Single unified schema with discriminator → Harder to use, requires out-of-band knowledge
- Inline all definitions in each schema → Duplication, maintenance burden

### Decision 2: JSON Schema Draft 2020-12
Use JSON Schema Draft 2020-12 as the target specification.

**Rationale:**
- Latest stable standard (published June 2022)
- Wide tooling support across languages
- Modern features ($defs, $ref resolution, vocabularies)

**Alternatives considered:**
- Draft-07 → Older, missing modern features
- Draft 2019-09 → Superseded by 2020-12

### Decision 3: Static Schema Files (Embedded Resources)
Store schema files as JSON and embed them as resources in the assembly.

**Rationale:**
- Simplicity: No reflection, no code generation, no external dependencies
- Reliability: Schemas are versioned with the assembly
- Performance: Zero runtime overhead

**Alternatives considered:**
- Runtime generation from C# types → Complex, requires reflection or source generators, error-prone
- External schema files → Deployment complexity, versioning issues

### Decision 4: $ref URLs Using Relative Paths
Use relative $ref paths for cross-schema references (e.g., `"$ref": "models-1.0.schema.json#/$defs/PackageInfo"`).

**Rationale:**
- Simple for consumers to resolve locally
- No hard-coded URLs or base URIs
- Works with standard JSON Schema validators

**Alternatives considered:**
- HTTP(S) URLs → Requires hosting, network dependency
- URN/custom scheme → Non-standard, tooling issues

### Decision 5: Command Flags
- `--command <name>` → Export schema for specific command (find, list-types, export-signatures, diff)
- `--all` → Export all schemas (prints multiple JSON documents or writes to directory if --output is a directory)
- `--output <path>` → Write to file or directory

**Rationale:**
- Consistent with existing CLI patterns (--package, --version, --output)
- Flexible for different use cases (single schema, all schemas, file output)

## Implementation Details

### Schema File Structure

```
src/NuGetToolbox.Cli/Schemas/
├── models-1.0.schema.json        # Shared $defs
├── find.schema.json              # Find command output
├── list-types.schema.json        # List-types command output
├── export-signatures.schema.json # Export-signatures command output
└── diff.schema.json              # Diff command output
```

### Schema Format (Example: find.schema.json)

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "https://nuget-toolbox.local/schemas/find-1.0.schema.json",
  "title": "NuGetToolbox Find Command Output",
  "type": "object",
  "$ref": "models-1.0.schema.json#/$defs/PackageInfo"
}
```

### Shared Models Schema (models-1.0.schema.json)

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "https://nuget-toolbox.local/schemas/models-1.0.schema.json",
  "title": "NuGetToolbox Shared Models",
  "description": "Common data models used across all NuGetToolbox CLI commands. These models represent NuGet packages, types, methods, and API differences.",
  "$defs": {
    "PackageInfo": {
      "type": "object",
      "title": "Package Information",
      "description": "Metadata about a resolved NuGet package including version, target frameworks, and local cache path.",
      "properties": {
        "packageId": {
          "type": "string",
          "description": "The unique NuGet package identifier (e.g., 'Newtonsoft.Json'). Case-insensitive."
        },
        "resolvedVersion": {
          "type": "string",
          "description": "The resolved semantic version of the package (e.g., '13.0.3'). May differ from requested version if 'latest' was requested.",
          "pattern": "^\\d+\\.\\d+\\.\\d+(-[a-zA-Z0-9.]+)?$"
        },
        "targetFrameworks": {
          "type": "array",
          "description": "List of Target Framework Monikers (TFMs) supported by this package (e.g., ['net8.0', 'netstandard2.0']). Extracted from package lib/ directory.",
          "items": {
            "type": "string",
            "description": "A Target Framework Moniker (TFM) such as 'net8.0', 'netstandard2.0', or 'net472'."
          },
          "minItems": 1
        },
        "nupkgPath": {
          "type": "string",
          "description": "Absolute file system path to the downloaded .nupkg file in the NuGet global packages cache.",
          "format": "uri-reference"
        }
      },
      "required": ["packageId", "resolvedVersion", "targetFrameworks", "nupkgPath"],
      "examples": [
        {
          "packageId": "Newtonsoft.Json",
          "resolvedVersion": "13.0.3",
          "targetFrameworks": ["net8.0", "netstandard2.0"],
          "nupkgPath": "C:\\Users\\user\\.nuget\\packages\\newtonsoft.json\\13.0.3\\newtonsoft.json.13.0.3.nupkg"
        }
      ]
    },
    "TypeInfo": {
      "type": "object",
      "title": "Type Information",
      "description": "Metadata about a public type (class, interface, struct, or enum) extracted from an assembly.",
      "properties": {
        "namespace": {
          "type": "string",
          "description": "The fully-qualified namespace of the type (e.g., 'System.Collections.Generic'). Empty string for global namespace."
        },
        "name": {
          "type": "string",
          "description": "The simple name of the type without namespace (e.g., 'List'). Does not include generic arity markers."
        },
        "kind": {
          "type": "string",
          "description": "The kind of type declaration.",
          "enum": ["class", "interface", "struct", "enum"],
          "examples": ["class"]
        }
      },
      "required": ["namespace", "name", "kind"]
    },
    "MethodInfo": {
      "type": "object",
      "title": "Method Information",
      "description": "Metadata about a public method including its signature, documentation, and parameter/return type information.",
      "properties": {
        "type": {
          "type": "string",
          "description": "Fully-qualified type name containing this method (e.g., 'Newtonsoft.Json.JsonConvert')."
        },
        "method": {
          "type": "string",
          "description": "The simple method name without parameters or return type (e.g., 'SerializeObject')."
        },
        "signature": {
          "type": "string",
          "description": "The full C# method signature as rendered by Roslyn, including return type, method name, and parameters (e.g., 'string SerializeObject(object value)')."
        },
        "summary": {
          "type": ["string", "null"],
          "description": "XML documentation summary extracted from the package's .xml doc file. Null if no documentation is available."
        },
        "params": {
          "type": ["object", "null"],
          "description": "Dictionary of parameter names to their XML documentation descriptions. Null if no parameter docs available.",
          "additionalProperties": { "type": "string" }
        },
        "returns": {
          "type": ["string", "null"],
          "description": "XML documentation describing the return value. Null if no return documentation available."
        },
        "parameters": {
          "type": "array",
          "description": "Structured list of method parameters with type and name information extracted via reflection.",
          "items": { "$ref": "#/$defs/ParameterInfo" }
        },
        "returnType": {
          "type": "string",
          "description": "The fully-qualified return type of the method (e.g., 'System.String', 'void')."
        }
      },
      "required": ["type", "method", "signature", "parameters", "returnType"]
    },
    "ParameterInfo": {
      "type": "object",
      "title": "Parameter Information",
      "description": "Metadata about a method parameter including its name and type.",
      "properties": {
        "name": {
          "type": "string",
          "description": "The parameter name as declared in the method signature (e.g., 'value', 'formatting')."
        },
        "type": {
          "type": "string",
          "description": "The fully-qualified type of the parameter (e.g., 'System.Object', 'Newtonsoft.Json.Formatting')."
        }
      },
      "required": ["name", "type"]
    },
    "DiffResult": {
      "type": "object",
      "title": "API Diff Result",
      "description": "Result of comparing two versions of a package's public API, identifying breaking changes, additions, and removals.",
      "properties": {
        "breaking": {
          "type": "array",
          "description": "List of breaking changes detected between versions (removed types, changed signatures).",
          "items": { "type": "string" }
        },
        "added": {
          "type": "array",
          "description": "List of new types or methods added in the newer version.",
          "items": { "type": "string" }
        },
        "removed": {
          "type": "array",
          "description": "List of types or methods removed in the newer version (subset of breaking changes).",
          "items": { "type": "string" }
        },
        "compatible": {
          "type": "boolean",
          "description": "True if the API change is backward-compatible (no breaking changes), false otherwise."
        }
      },
      "required": ["breaking", "added", "removed", "compatible"]
    }
  }
}
```

### Command Implementation

```csharp
// SchemaCommand.cs
public class SchemaCommand
{
    public static Command Create()
    {
        var commandOption = new Option<string?>(
            "--command",
            "Command name (find, list-types, export-signatures, diff)");
        var allOption = new Option<bool>(
            "--all",
            "Export all schemas");
        var outputOption = new Option<string?>(
            "--output",
            "Output file or directory path");

        var command = new Command("schema", "Export JSON Schema definitions");
        command.AddOption(commandOption);
        command.AddOption(allOption);
        command.AddOption(outputOption);

        command.SetHandler(async (commandName, all, output) =>
        {
            // Load embedded resources and output
        }, commandOption, allOption, outputOption);

        return command;
    }
}
```

### Resource Embedding (csproj)

```xml
<ItemGroup>
  <EmbeddedResource Include="Schemas\*.schema.json" />
</ItemGroup>
```

## Risks / Trade-offs

### Risk: Schema drift from code changes
**Mitigation:** Add schema validation tests that run actual command outputs through JSON Schema validators. Make these tests part of CI.

### Risk: $ref resolution complexity for consumers
**Mitigation:** Document $ref resolution in README; provide example validation commands using popular validators (ajv, jsonschema).

### Risk: Versioning breaking changes
**Mitigation:** Adopt semantic versioning for schema files (models-1.0, models-2.0). Bump major version on breaking changes. Document migration path in release notes.

## Migration Plan

1. Implement schema command and embed initial v1.0 schemas
2. Add schema validation tests to CI
3. Update README with usage examples
4. Release with feature flag or beta label if needed
5. Gather feedback, iterate on schema structure
6. Stabilize v1.0 schemas in next major release

## LLM/AI Agent Optimization

### Annotation Strategy

Schemas are designed to be consumed by LLM/AI agents for:
1. **Understanding output structure** - Agents can parse schemas to know what fields to expect
2. **Field semantics** - Descriptions explain what each field means and how to use it
3. **Validation constraints** - Format, pattern, and range constraints guide agents in generating or validating data
4. **Examples** - Concrete samples help agents understand real-world usage

### Required Annotations
- **Every property**: Must have "description" explaining purpose and semantics
- **Complex types**: Must include "examples" showing representative data
- **Root schemas**: Must include "title" and "description" for context
- **String fields**: Should specify "format" (uri, path, date-time) or "pattern" where applicable
- **Enums**: Should explain valid values and their meanings
- **Arrays**: Should document item semantics and constraints (minItems, maxItems)

### Example: LLM-Friendly Property Definition
```json
"packageId": {
  "type": "string",
  "description": "The unique NuGet package identifier (e.g., 'Newtonsoft.Json'). Case-insensitive. Used to locate packages on NuGet feeds.",
  "examples": ["Newtonsoft.Json", "Microsoft.Extensions.Logging"]
}
```

This provides:
- Type information (string)
- Semantic meaning (package identifier)
- Format guidance (case-insensitive)
- Examples for pattern recognition

## Open Questions

- Should we publish schemas to a public URL (e.g., GitHub Pages) for easier $ref resolution?
  - Answer: Defer to later; start with relative paths for simplicity
- Should we support older JSON Schema drafts (Draft-07) for broader compatibility?
  - Answer: No, Draft 2020-12 is widely supported; don't add complexity
