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

The system SHALL extract type and member metadata from assemblies using MetadataLoadContext without loading code into the default AppDomain.

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

The system SHALL render method signatures using Roslyn SymbolDisplayFormat and inject XML documentation.

#### Scenario: Export methods with documentation
- **WHEN** `ExportMethods` is called with assembly paths
- **THEN** system extracts public methods with signatures
- **AND** includes summary, params, and returns from XML docs
- **AND** returns list of MethodInfo

#### Scenario: Export to JSON format
- **WHEN** `ExportToJson` is called with method list
- **THEN** system serializes using System.Text.Json
- **AND** uses camelCase property names
- **AND** returns valid JSON string

#### Scenario: Export to JSONL format
- **WHEN** `ExportToJsonL` is called with method list
- **THEN** system serializes each method to one JSON line
- **AND** returns newline-delimited JSON string

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

