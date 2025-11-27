## ADDED Requirements

### Requirement: Enhanced Type Visibility Filtering

The export-signatures command SHALL filter types using `Type.IsVisible` and limit to classes and interfaces only.

#### Scenario: Public nested type in public outer type
- **WHEN** processing an assembly with a public nested class inside a public outer class
- **THEN** the nested class methods are included in output
- **AND** the nested class is identified as visible via `IsVisible`

#### Scenario: Public nested type in internal outer type
- **WHEN** processing an assembly with a public nested class inside an internal outer class
- **THEN** the nested class methods are excluded from output
- **AND** the nested class is identified as not visible via `IsVisible`

#### Scenario: Struct and enum filtering
- **WHEN** processing an assembly with public structs and enums
- **THEN** struct and enum methods are excluded from output
- **AND** only classes and interfaces are processed

### Requirement: Partial Load Resilience

The export-signatures command SHALL handle `ReflectionTypeLoadException` gracefully and continue processing successfully loaded types.

#### Scenario: Missing dependency causes partial load failure
- **WHEN** assembly references missing dependencies causing `ReflectionTypeLoadException`
- **THEN** command processes all successfully loaded types
- **AND** logs warning about partial load with counts
- **AND** does not crash or skip entire assembly

#### Scenario: All types fail to load
- **WHEN** all types in assembly fail to load
- **THEN** command logs error and continues with next assembly
- **AND** returns empty method list for that assembly

### Requirement: CLI Flag Compatibility

The export-signatures command SHALL accept both `--filter` and `--namespace` flags with identical behavior.

#### Scenario: Using --filter flag
- **WHEN** user runs `export-signatures --package "Newtonsoft.Json" --filter "Newtonsoft.Json.Linq"`
- **THEN** command filters methods by specified namespace
- **AND** produces expected filtered output

#### Scenario: Using --namespace flag
- **WHEN** user runs `export-signatures --package "Newtonsoft.Json" --namespace "Newtonsoft.Json.Linq"`
- **THEN** command filters methods by specified namespace
- **AND** produces identical output to --filter flag

#### Scenario: Help text shows both flags
- **WHEN** user runs `export-signatures --help`
- **THEN** help text shows both --filter and --namespace options
- **AND** indicates they are aliases


### Requirement: Interface Method Processing

The export-signatures command SHALL process only declared methods for interface types (maintaining current behavior).

#### Scenario: Interface with inherited methods
- **WHEN** processing an interface that inherits from another interface
- **THEN** only methods declared directly on the interface are included
- **AND** inherited interface methods are excluded
- **AND** behavior matches current implementation

#### Scenario: Class implementing interface
- **WHEN** processing a class that implements interfaces
- **THEN** all public methods (including interface implementations) are included
- **AND** filtering respects `DeclaredOnly` for interface types only
