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

