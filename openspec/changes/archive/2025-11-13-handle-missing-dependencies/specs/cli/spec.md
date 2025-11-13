# CLI Specification Delta

## MODIFIED Requirements

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

## ADDED Requirements

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
