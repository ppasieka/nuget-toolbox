# CLI Spec Delta: Fix Missing Public Types

## MODIFIED Requirements

### Requirement: Assembly Metadata Inspection

The system SHALL extract type and member metadata from assemblies using MetadataLoadContext without loading code into the default AppDomain. The system SHALL gracefully handle missing dependencies by extracting partial results when dependency assemblies are unavailable. **The system SHALL use `Type.IsVisible` to identify public types, correctly including nested public types where the entire containment chain is public.**

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

#### Scenario: Include nested public types
- **WHEN** assembly contains public types nested inside other public types
- **THEN** system includes nested public types in output
- **AND** uses `Type.IsVisible` to determine visibility
- **AND** nested type name appears as `OuterType+NestedType` format

#### Scenario: Exclude nested public types in internal containers
- **WHEN** assembly contains public types nested inside internal types
- **THEN** system excludes those nested types from output
- **AND** `Type.IsVisible` correctly identifies them as not externally visible

#### Scenario: Use fallback type classification when base type unavailable
- **WHEN** `GetTypeKind` cannot resolve base type due to missing dependency
- **THEN** system uses type attributes to infer kind (interface, struct, class)
- **AND** system logs debug message about fallback classification
- **AND** system continues processing remaining types
