# e2e-testing Specification

## Purpose
Specify the requirements for End-to-End (E2E) testing of the NuGet Toolbox CLI, ensuring critical user workflows (find, list-types, export-signatures, diff) are validated against real NuGet packages with correct outputs, performance benchmarks, and isolation guarantees.
## Requirements
### Requirement: Find Command E2E Test
The test suite SHALL verify the `find` command resolves a real NuGet package and returns valid JSON output.

#### Scenario: Resolve Newtonsoft.Json 13.0.1
- **WHEN** the CLI executes `find --package "Newtonsoft.Json" --version "13.0.1"`
- **THEN** output SHALL be valid JSON containing `packageId`, `resolvedVersion` (13.0.1), `resolved` (true), `source`, `nupkgPath`, and `targetFrameworks` array

#### Scenario: JSON structure validation
- **WHEN** the find command produces output
- **THEN** all required fields SHALL be present with correct types (strings, booleans, arrays)

### Requirement: List-Types Command E2E Test
The test suite SHALL verify the `list-types` command enumerates public types from a real NuGet package.

#### Scenario: List types from Newtonsoft.Json 13.0.1
- **WHEN** the CLI executes `list-types --package "Newtonsoft.Json" --version "13.0.1"`
- **THEN** output SHALL be a JSON array containing at least 50 types

#### Scenario: Type information completeness
- **WHEN** the list-types command produces output
- **THEN** each type entry SHALL contain `namespace`, `name`, and `kind` fields with valid values

#### Scenario: Known types presence
- **WHEN** listing types from Newtonsoft.Json
- **THEN** output SHALL include `JsonConvert` class in `Newtonsoft.Json` namespace

### Requirement: Export-Signatures Command E2E Test
The test suite SHALL verify the `export-signatures` command exports method signatures with XML documentation.

#### Scenario: Export signatures from Newtonsoft.Json with filter
- **WHEN** the CLI executes `export-signatures --package "Newtonsoft.Json" --version "13.0.1" --filter "Newtonsoft.Json" --format jsonl`
- **THEN** output SHALL be JSONL format with at least one method signature

#### Scenario: Method signature structure
- **WHEN** export-signatures produces JSONL output
- **THEN** each line SHALL be valid JSON containing `type`, `method`, `signature`, and optional `summary`, `params`, `returns` fields

#### Scenario: XML documentation presence
- **WHEN** exporting signatures from documented package
- **THEN** at least 50% of exported methods SHALL have non-null `summary` field

### Requirement: Diff Command E2E Test
The test suite SHALL verify the `diff` command detects API changes between package versions.

#### Scenario: Compare Newtonsoft.Json versions
- **WHEN** the CLI executes `diff --package "Newtonsoft.Json" --from "13.0.1" --to "13.0.3"`
- **THEN** output SHALL be valid JSON containing `packageId`, `versionFrom`, `versionTo`, `tfm`, `added`, and `compatible` fields

#### Scenario: Diff output structure
- **WHEN** the diff command produces output
- **THEN** `added` and `removed` fields SHALL be arrays of type objects
- **THEN** `compatible` field SHALL be a boolean

#### Scenario: Same version comparison
- **WHEN** comparing identical versions
- **THEN** `added` and `removed` arrays SHALL be empty
- **THEN** `compatible` field SHALL be true

### Requirement: E2E Test Performance
All E2E tests SHALL complete within reasonable timeframe to enable fast CI/CD feedback.

#### Scenario: Test execution time
- **WHEN** running the complete E2E test suite
- **THEN** total execution time SHALL be less than 30 seconds (leveraging package cache)

### Requirement: E2E Test Isolation
E2E tests SHALL not interfere with each other or depend on execution order.

#### Scenario: Parallel test execution
- **WHEN** E2E tests run in parallel
- **THEN** all tests SHALL pass without race conditions or shared state conflicts

### Requirement: E2E Test Failure Messages
E2E test failures SHALL provide actionable diagnostic information.

#### Scenario: JSON validation failure
- **WHEN** an E2E test detects invalid JSON structure
- **THEN** the failure message SHALL include expected vs actual structure and sample output

