# cli Specification

## Purpose
TBD - created by archiving change implement-find-package-handler. Update Purpose after archive.
## Requirements
### Requirement: Find Package Command Handler

The `find` command handler SHALL resolve a NuGet package by ID and optional version using `NuGetPackageResolver`, and output structured package metadata as JSON.

#### Scenario: Resolve with package ID only
- **WHEN** user runs `find --package "Newtonsoft.Json"`
- **THEN** system resolves to the latest available version
- **AND** system outputs JSON with resolved `PackageInfo` (version, TFMs, nupkg path)

#### Scenario: Resolve with explicit version
- **WHEN** user runs `find --package "Newtonsoft.Json" --version "13.0.3"`
- **THEN** system resolves to the specified version
- **AND** system outputs JSON with matching `PackageInfo`

#### Scenario: Resolve from custom feed
- **WHEN** user runs `find --package "Newtonsoft.Json" --feed "https://api.nuget.org/v3/index.json"`
- **THEN** system resolves from the specified feed
- **AND** system outputs JSON with resolved `PackageInfo`

#### Scenario: Resolve from system-defined feed
- **WHEN** user runs `find --package "Newtonsoft.Json"` without `--feed` parameter
- **THEN** system resolves from feed(s) defined in `nuget.config`
- **AND** system outputs JSON with resolved `PackageInfo`

#### Scenario: Write output to file
- **WHEN** user runs `find --package "Newtonsoft.Json" --output "result.json"`
- **THEN** system writes JSON to specified file
- **AND** file contains valid JSON matching `PackageInfo` schema

#### Scenario: Output to stdout by default
- **WHEN** user runs `find --package "Newtonsoft.Json"` without `--output`
- **THEN** system writes JSON to standard output
- **AND** output is valid JSON matching `PackageInfo` schema

### Requirement: Error Handling

The `find` command handler SHALL report actionable errors when package resolution fails.

#### Scenario: Package not found
- **WHEN** user runs `find --package "NonExistentPackage"`
- **THEN** system returns non-zero exit code
- **AND** system logs descriptive error message identifying the missing package

#### Scenario: Invalid feed URL
- **WHEN** user runs `find --package "Newtonsoft.Json" --feed "http://invalid.local"`
- **THEN** system returns non-zero exit code
- **AND** system logs error indicating feed is unreachable or invalid

#### Scenario: Network failure
- **WHEN** network is unavailable during resolution
- **THEN** system returns non-zero exit code
- **AND** system logs error indicating network failure

### Requirement: JSON Output Format

The `find` command handler SHALL serialize `PackageInfo` using `System.Text.Json` with camelCase property names.

#### Scenario: Valid JSON schema
- **WHEN** user runs `find --package "Newtonsoft.Json"`
- **THEN** output JSON contains fields: `packageId`, `resolvedVersion`, `targetFrameworks`, `nupkgPath`
- **AND** field names match camelCase convention

#### Scenario: All resolved fields present
- **WHEN** package is resolved successfully
- **THEN** JSON includes all metadata from `PackageInfo` (version, TFMs, path)
- **AND** no fields are omitted or null unless intentional

### Requirement: NuGet Package Resolution

The system SHALL resolve NuGet packages using the NuGet.Protocol V3 API with support for nuget.config sources and credential providers.

#### Scenario: Resolve package from default feed
- **WHEN** `ResolvePackageAsync` is called with package ID and no feed
- **THEN** system queries feeds from nuget.config
- **AND** returns PackageInfo with resolved version, TFMs, and nupkg path

#### Scenario: Resolve package from custom feed
- **WHEN** `ResolvePackageAsync` is called with package ID and custom feed URL
- **THEN** system queries the specified feed
- **AND** returns PackageInfo from that feed

#### Scenario: Download package to local cache
- **WHEN** `DownloadPackageAsync` is called
- **THEN** system downloads .nupkg to NuGet global packages folder
- **AND** returns absolute path to cached file

### Requirement: Assembly Metadata Inspection

The system SHALL extract type and member metadata from assemblies using MetadataLoadContext without loading code into the default AppDomain. The system SHALL gracefully handle missing dependencies by extracting partial results when dependency assemblies are unavailable.

#### Scenario: Extract public types from assembly
- **WHEN** `ExtractPublicTypes` is called with assembly path
- **THEN** system uses MetadataLoadContext with PathAssemblyResolver
- **AND** returns list of TypeInfo for public classes, interfaces, structs, and enums

#### Scenario: Extract types from multiple assemblies
- **WHEN** `ExtractPublicTypesFromMultiple` is called with multiple paths
- **THEN** system extracts types from all assemblies
- **AND** returns combined list of TypeInfo

#### Scenario: Get public members from type
- **WHEN** `GetPublicMembers` is called with Type
- **THEN** system returns public methods, properties, and fields
- **AND** filters out private/internal members

#### Scenario: Handle missing dependency assemblies
- **WHEN** target assembly references types from missing dependency packages
- **THEN** system catches `ReflectionTypeLoadException` and uses partially-loaded types
- **AND** system continues extracting successfully-loaded types
- **AND** system logs missing dependencies at Debug level
- **AND** system returns TypeInfo for all loadable types

#### Scenario: Skip individual unloadable types
- **WHEN** individual types fail to load due to missing references
- **THEN** system catches `TypeLoadException`, `FileNotFoundException`, `FileLoadException`
- **AND** system logs the failure at Debug level
- **AND** system continues processing remaining types
- **AND** system returns TypeInfo for successfully-loaded types only

#### Scenario: Fail on corrupted assembly
- **WHEN** assembly file is invalid or corrupted
- **THEN** system throws `InvalidOperationException` with descriptive message
- **AND** system does not return partial results

### Requirement: XML Documentation Parsing

The system SHALL parse compiler-generated XML documentation files and match members using Roslyn canonical documentation IDs.

#### Scenario: Load XML documentation file
- **WHEN** `LoadDocumentation` is called with .xml path
- **THEN** system parses <members> elements
- **AND** indexes by documentation comment ID

#### Scenario: Get summary for member
- **WHEN** `GetSummary` is called with valid documentation ID
- **THEN** system returns <summary> text content
- **AND** returns null if ID not found

#### Scenario: Get parameter documentation
- **WHEN** `GetParameters` is called with method documentation ID
- **THEN** system returns dictionary of param names to descriptions
- **AND** returns empty dictionary if no params documented

#### Scenario: Get returns documentation
- **WHEN** `GetReturns` is called with method documentation ID
- **THEN** system returns <returns> text content
- **AND** returns null if not documented

### Requirement: C# Signature Export
The system SHALL render method signatures using Roslyn SymbolDisplayFormat, inject XML documentation, and extract parameter/return type metadata from reflection independent of XML documentation availability.

#### Scenario: Export with XML documentation
- **WHEN** method has complete XML documentation (summary, params, returns)
- **THEN** output includes all documentation fields plus parameter types/names and return type

#### Scenario: Export without XML documentation
- **WHEN** method lacks XML documentation
- **THEN** output includes parameter types/names and return type from reflection metadata

#### Scenario: Export with partial XML documentation
- **WHEN** method has summary but missing params or returns in XML
- **THEN** output includes summary from XML plus parameter/return type info from reflection

#### Scenario: JSON output structure
- **WHEN** exporting method signatures
- **THEN** each method includes:
  - `type` - containing type name
  - `method` - method name
  - `signature` - full method signature
  - `summary` - documentation summary (if available)
  - `params` - dictionary of parameter names to documentation (if available)
  - `returns` - return value documentation (if available)
  - `parameters` - array of {name, type} objects from reflection
  - `returnType` - return type from reflection

### Requirement: API Diff Analysis

The system SHALL compare API versions and identify breaking changes, additions, and removals.

#### Scenario: Compare two API versions
- **WHEN** `CompareVersions` is called with two MethodInfo lists
- **THEN** system identifies added, removed, and modified members
- **AND** returns DiffResult with breaking[] array

#### Scenario: Identify breaking changes
- **WHEN** comparing versions
- **THEN** system flags removed public types/methods as breaking
- **AND** flags modified signatures as breaking
- **AND** does not flag additions as breaking

### Requirement: List Types Command

The CLI SHALL provide a `list-types` command that outputs public types from a resolved package.

#### Scenario: List types from package
- **WHEN** user runs `list-types --package "Newtonsoft.Json"`
- **THEN** system resolves package, extracts assemblies
- **AND** outputs JSON array of TypeInfo

#### Scenario: List types with version
- **WHEN** user runs `list-types --package "Newtonsoft.Json" --version "13.0.3"`
- **THEN** system resolves specific version
- **AND** outputs TypeInfo for that version

### Requirement: Export Signatures Command

The CLI SHALL provide an `export-signatures` command that outputs method signatures with documentation.

#### Scenario: Export signatures from package
- **WHEN** user runs `export-signatures --package "Newtonsoft.Json"`
- **THEN** system resolves package, exports methods
- **AND** outputs JSON with MethodInfo including signatures and docs

#### Scenario: Export to JSONL format
- **WHEN** user runs `export-signatures --package "Newtonsoft.Json" --format jsonl`
- **THEN** system outputs JSONL format (one method per line)

#### Scenario: Filter by namespace
- **WHEN** user runs `export-signatures --package "Newtonsoft.Json" --namespace "Newtonsoft.Json.Linq"`
- **THEN** system exports only methods from matching namespace

### Requirement: Diff Command

The CLI SHALL provide a `diff` command that compares API versions and identifies breaking changes.

#### Scenario: Compare two package versions
- **WHEN** user runs `diff --package "Newtonsoft.Json" --from "12.0.0" --to "13.0.0"`
- **THEN** system resolves both versions, compares APIs
- **AND** outputs DiffResult JSON with breaking changes

#### Scenario: Specify target framework
- **WHEN** user runs `diff --package "Newtonsoft.Json" --from "12.0.0" --to "13.0.0" --tfm "net8.0"`
- **THEN** system compares APIs for specified TFM only

#### Scenario: Output compatibility summary
- **WHEN** comparing versions
- **THEN** DiffResult includes `compatible` boolean flag
- **AND** flag is false if breaking changes exist

### Requirement: Direct Dependency Listing

The system SHALL extract direct package dependencies from a NuGet package's `.nuspec` file.

#### Scenario: Read dependencies from package
- **WHEN** `GetDirectDependenciesAsync` is called with nupkg path
- **THEN** system reads `.nuspec` using `NuspecReader`
- **AND** system returns dependency groups with target framework, package ID, and version range
- **AND** system handles packages with no dependencies by returning empty list

#### Scenario: Parse multi-framework dependencies
- **WHEN** package has different dependencies per target framework
- **THEN** system returns separate entries for each framework's dependency group
- **AND** system preserves target framework moniker (TFM) for each group

#### Scenario: Handle missing nuspec file
- **WHEN** `.nuspec` file is not accessible or package is corrupted
- **THEN** system logs warning and returns empty dependency list
- **AND** system does not fail the overall operation

### Requirement: File-Based Logging

The system SHALL write all logging output to a file instead of console to ensure clean JSON output on stdout.

#### Scenario: Initialize file logging
- **WHEN** CLI commands are executed
- **THEN** system creates log directory in temp folder
- **AND** system writes logs to daily log file with format `nuget-toolbox-{yyyyMMdd}.log`
- **AND** system sets minimum log level to Debug

#### Scenario: Clean console output
- **WHEN** user runs commands with JSON output
- **THEN** only JSON is written to stdout
- **AND** no logging messages interfere with JSON output
- **AND** output can be piped to tools like `jq` without errors

### Requirement: Parameter Metadata Extraction
The system SHALL extract parameter metadata (type and name) from assembly reflection for all public methods.

#### Scenario: Simple parameter types
- **WHEN** method has value type or string parameters
- **THEN** output includes accurate type names (e.g., "System.Int32", "System.String")

#### Scenario: Complex parameter types
- **WHEN** method has generic, array, or reference type parameters
- **THEN** output includes full type names with namespace and generic arguments

#### Scenario: Parameter names
- **WHEN** extracting parameters
- **THEN** output preserves original parameter names from metadata

### Requirement: Return Type Metadata Extraction
The system SHALL extract return type metadata from assembly reflection for all public methods.

#### Scenario: Void return type
- **WHEN** method returns void
- **THEN** `returnType` field contains "System.Void"

#### Scenario: Value type return
- **WHEN** method returns value type
- **THEN** `returnType` field contains full type name with namespace

#### Scenario: Generic return type
- **WHEN** method returns generic type
- **THEN** `returnType` field contains full generic type notation

### Requirement: Schema Export Command

The `schema` command SHALL export JSON Schema (Draft 2020-12) definitions for all CLI command outputs.

#### Scenario: Export schema for specific command
- **WHEN** user runs `schema --command find`
- **THEN** system outputs find.schema.json to stdout
- **AND** schema conforms to JSON Schema Draft 2020-12

#### Scenario: Export all schemas
- **WHEN** user runs `schema --all`
- **THEN** system outputs all command schemas (find, list-types, export-signatures, diff) and models schema
- **AND** each schema is valid JSON Schema Draft 2020-12

#### Scenario: Write schema to file
- **WHEN** user runs `schema --command find --output "find.schema.json"`
- **THEN** system writes schema to specified file
- **AND** file contains valid JSON Schema

#### Scenario: Write all schemas to directory
- **WHEN** user runs `schema --all --output "schemas/"`
- **THEN** system writes all schema files to the directory
- **AND** directory contains: models-1.0.schema.json, find.schema.json, list-types.schema.json, export-signatures.schema.json, diff.schema.json

#### Scenario: Invalid command name
- **WHEN** user runs `schema --command invalid-name`
- **THEN** system returns non-zero exit code
- **AND** system logs error indicating valid command names

#### Scenario: Default behavior (no flags)
- **WHEN** user runs `schema` without flags
- **THEN** system outputs models schema or shows help message

### Requirement: Shared Models Schema

The system SHALL provide a shared models schema (models-1.0.schema.json) containing $defs for all common data structures.

#### Scenario: Models schema includes all types
- **WHEN** models schema is exported
- **THEN** schema contains $defs for: PackageInfo, TypeInfo, MethodInfo, ParameterInfo, DiffResult, DirectDependency
- **AND** each $def matches the structure of corresponding C# model

#### Scenario: $defs are valid JSON Schema objects
- **WHEN** models schema is validated
- **THEN** each $def includes "type": "object"
- **AND** includes "properties" with field definitions
- **AND** includes "required" array for mandatory fields

### Requirement: Per-Command Schemas

The system SHALL provide individual schemas for each CLI command (find, list-types, export-signatures, diff) that reference the shared models schema.

#### Scenario: Find command schema
- **WHEN** find schema is exported
- **THEN** schema uses $ref to models-1.0.schema.json#/$defs/PackageInfo
- **AND** schema declares $schema as "https://json-schema.org/draft/2020-12/schema"

#### Scenario: List-types command schema
- **WHEN** list-types schema is exported
- **THEN** schema defines type: "array"
- **AND** items use $ref to models-1.0.schema.json#/$defs/TypeInfo

#### Scenario: Export-signatures command schema
- **WHEN** export-signatures schema is exported
- **THEN** schema defines type: "array"
- **AND** items use $ref to models-1.0.schema.json#/$defs/MethodInfo

#### Scenario: Diff command schema
- **WHEN** diff schema is exported
- **THEN** schema uses $ref to models-1.0.schema.json#/$defs/DiffResult

### Requirement: Schema Versioning

The system SHALL version schemas using semantic versioning embedded in filenames.

#### Scenario: Schema filenames include version
- **WHEN** schemas are exported
- **THEN** shared models schema is named "models-1.0.schema.json"
- **AND** version reflects schema structure version, not CLI version

#### Scenario: $id includes version
- **WHEN** schemas are validated
- **THEN** each schema includes $id with version (e.g., "https://nuget-toolbox.local/schemas/models-1.0.schema.json")
- **AND** $id is stable across releases for the same schema version

### Requirement: Schema Documentation and Annotations

The system SHALL include comprehensive documentation and annotations in all schemas to support LLM/AI agent consumption and human understanding.

#### Scenario: Root schema includes metadata
- **WHEN** any schema is exported
- **THEN** schema includes "title" describing the schema purpose
- **AND** includes "description" explaining the schema's role and usage
- **AND** includes "$comment" with additional context if needed

#### Scenario: All properties have descriptions
- **WHEN** models schema is exported
- **THEN** every property in every $def includes a "description" field
- **AND** descriptions explain the property's purpose, format, and constraints
- **AND** descriptions are clear and actionable for AI agents

#### Scenario: Complex types include examples
- **WHEN** models schema includes complex types (objects, arrays)
- **THEN** schema includes "examples" array with representative data
- **AND** examples match the schema structure
- **AND** examples reflect real-world usage patterns

#### Scenario: Enumerations include descriptions
- **WHEN** schema includes enum fields
- **THEN** schema includes "description" explaining valid values
- **AND** optionally uses "oneOf" with "const" and "title" for semantic enum descriptions

#### Scenario: Required fields are documented
- **WHEN** schema includes "required" array
- **THEN** descriptions explain why fields are required
- **AND** optional fields indicate when they may be null or absent

#### Scenario: Field formats are specified
- **WHEN** schema includes string fields with specific formats
- **THEN** schema uses "format" keyword (e.g., "uri", "date-time")
- **AND** includes "pattern" for custom formats
- **AND** description explains format expectations

#### Scenario: Array items are documented
- **WHEN** schema includes array properties
- **THEN** "items" definition includes description
- **AND** description explains what each array element represents
- **AND** includes constraints like "minItems", "maxItems" if applicable

#### Scenario: Schema includes usage examples
- **WHEN** command-specific schemas are exported
- **THEN** schema includes top-level "examples" with complete command output samples
- **AND** examples show typical successful responses
- **AND** examples demonstrate optional fields when present

