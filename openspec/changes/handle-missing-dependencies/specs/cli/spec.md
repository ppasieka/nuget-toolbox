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

The system SHALL extract direct package dependencies from a NuGet package's `.nuspec` file and present them grouped by target framework.

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

### Requirement: Dependency Guidance in CLI Output

The CLI SHALL display direct package dependencies and suggest inspection commands for dependency packages.

#### Scenario: Display dependencies with type listing
- **WHEN** user runs `list-types --package "PackageName"`
- **THEN** system outputs public types from the package
- **AND** system displays "Direct dependencies" section grouped by TFM
- **AND** system shows package ID and version range for each dependency

#### Scenario: Suggest dependency inspection
- **WHEN** package has direct dependencies
- **THEN** CLI output includes tip: "To inspect dependencies, run: nuget-toolbox list-types --package <DependencyId>"
- **AND** system does not automatically download or inspect dependencies

#### Scenario: Silent operation for packages without dependencies
- **WHEN** package has no dependencies
- **THEN** system does not display dependency section
- **AND** system proceeds with normal type listing
