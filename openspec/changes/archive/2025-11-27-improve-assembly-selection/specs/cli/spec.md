# CLI Specification Delta: Improve Assembly Selection

## ADDED Requirements

### Requirement: Framework Compatibility Selection

The system SHALL select target frameworks using NuGet's `FrameworkReducer.GetNearest()` algorithm to ensure correct framework compatibility across framework families (e.g., netstandard, netcoreapp, net).

#### Scenario: Select netstandard2.0 for net8.0 runtime
- **WHEN** package contains `netstandard2.0` and `net4.8` frameworks
- **AND** runtime is `net8.0`
- **THEN** system selects `netstandard2.0` as the nearest compatible framework
- **AND** does NOT select `net4.8` despite higher version number

#### Scenario: Select exact match when available
- **WHEN** package contains `net8.0`, `net6.0`, and `netstandard2.0`
- **AND** runtime is `net8.0`
- **THEN** system selects `net8.0` as exact match

#### Scenario: Select nearest when no exact match
- **WHEN** package contains `net6.0` and `netstandard2.1`
- **AND** runtime is `net8.0`
- **THEN** system selects `net6.0` as nearest compatible framework

#### Scenario: List available TFMs on mismatch
- **WHEN** no compatible framework is found
- **THEN** system returns exit code 2 (TFM not found)
- **AND** error message lists all available TFMs in the package
- **AND** error message suggests using `--tfm` to specify explicitly

### Requirement: Reference Assembly Preference

The system SHALL prefer reference assemblies (`ref/`) over implementation assemblies (`lib/`) when extracting type metadata, falling back to `lib/` when `ref/` is not available.

#### Scenario: Use ref/ assemblies when available
- **WHEN** package contains both `ref/` and `lib/` folders for selected TFM
- **THEN** system uses assemblies from `ref/` folder
- **AND** logs at Debug level that ref/ assemblies are being used

#### Scenario: Fall back to lib/ when ref/ is empty
- **WHEN** package contains only `lib/` folder (no `ref/`)
- **THEN** system uses assemblies from `lib/` folder
- **AND** logs at Debug level that lib/ assemblies are being used as fallback

#### Scenario: Handle mixed ref/lib availability per TFM
- **WHEN** package has `ref/net6.0/` but no `ref/net8.0/`
- **AND** selected TFM is `net8.0`
- **THEN** system checks `ref/net8.0/` first (empty)
- **AND** falls back to `lib/net8.0/`

### Requirement: Deterministic Output Ordering

The system SHALL sort all output arrays to ensure identical JSON output for identical inputs across multiple runs.

#### Scenario: TypeInfo sorted by namespace and name
- **WHEN** list-types command outputs TypeInfo array
- **THEN** types are sorted by `namespace` ascending
- **AND** within same namespace, sorted by `name` ascending

#### Scenario: MethodInfo sorted by type, method, signature
- **WHEN** export-signatures command outputs MethodInfo array
- **THEN** methods are sorted by `type` ascending
- **AND** within same type, sorted by `method` ascending
- **AND** within same method, sorted by `signature` ascending

#### Scenario: DiffResult arrays sorted consistently
- **WHEN** diff command outputs DiffResult
- **THEN** `breaking` array is sorted by `type`, then `signature`
- **AND** `added` array is sorted by `type`, then `signature`
- **AND** `removed` array is sorted by `type`, then `signature`

#### Scenario: Identical runs produce identical JSON
- **WHEN** same command is run twice with identical inputs
- **THEN** stdout output is byte-for-byte identical
- **AND** JSON can be compared with simple string equality
