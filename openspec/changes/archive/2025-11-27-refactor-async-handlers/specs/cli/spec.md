## MODIFIED Requirements

### Requirement: Find Package Command Handler

The `find` command handler SHALL invoke `NuGetPackageResolver.ResolvePackageAsync` asynchronously to resolve a NuGet package by ID and optional version, and output structured `PackageInfo` as JSON using `System.Text.Json` with camelCase property names. The handler SHALL support cancellation and return standardized exit codes.

#### Scenario: Resolve with package ID only
- **WHEN** user runs `find --package "Newtonsoft.Json"`
- **THEN** system invokes `ResolvePackageAsync` asynchronously with cancellation token
- **AND** system outputs JSON with resolved `PackageInfo`
- **AND** system returns exit code 0 (Success)

#### Scenario: Resolve with explicit version
- **WHEN** user runs `find --package "Newtonsoft.Json" --version "13.0.3"`
- **THEN** system invokes `ResolvePackageAsync` with specified version and cancellation token
- **AND** system outputs JSON with matching `PackageInfo`

#### Scenario: Resolve from custom feed
- **WHEN** user runs `find --package "Newtonsoft.Json" --feed "https://api.nuget.org/v3/index.json"`
- **THEN** system invokes `ResolvePackageAsync` with custom feed and cancellation token
- **AND** system outputs JSON with resolved `PackageInfo`

#### Scenario: Write output to file
- **WHEN** user runs `find --package "Newtonsoft.Json" --output "result.json"`
- **THEN** system writes JSON to specified file asynchronously
- **AND** file contains valid JSON matching `PackageInfo` schema

#### Scenario: Output to stdout by default
- **WHEN** user runs `find --package "Newtonsoft.Json"` without `--output`
- **THEN** system writes JSON to standard output
- **AND** stdout contains only valid JSON matching `PackageInfo` schema

#### Scenario: Cancellation support
- **WHEN** user cancels the operation (e.g., Ctrl+C)
- **THEN** system stops processing gracefully
- **AND** system cleans up any temporary resources
- **AND** system returns non-zero exit code

### Requirement: Error Handling

The `find` command handler SHALL report actionable errors when package resolution fails and use standardized exit codes.

#### Scenario: Package not found
- **WHEN** user runs `find --package "NonExistentPackage"`
- **THEN** system returns exit code 1 (NotFound)
- **AND** system logs descriptive error message identifying the missing package

#### Scenario: Invalid feed URL
- **WHEN** user runs `find --package "Newtonsoft.Json" --feed "http://invalid.local"`
- **THEN** system returns exit code 4 (NetworkError)
- **AND** system logs error indicating feed is unreachable or invalid

#### Scenario: Network failure
- **WHEN** network is unavailable during resolution
- **THEN** system returns exit code 4 (NetworkError)
- **AND** system logs error indicating network failure

## ADDED Requirements

### Requirement: Async Command Execution

All CLI commands SHALL be implemented using asynchronous handlers (`SetHandler`) and SHALL NOT use blocking calls (`.Wait()` or `.Result`) for I/O operations. All handlers SHALL accept and propagate `CancellationToken`.

#### Scenario: Async handler registration
- **WHEN** command is configured
- **THEN** it uses `SetHandler` with an async delegate
- **AND** the delegate accepts `InvocationContext` or `CancellationToken`

#### Scenario: Cancellation propagation
- **WHEN** a command is running
- **AND** a cancellation signal is received
- **THEN** the `CancellationToken` is triggered
- **AND** all downstream async services stop processing

### Requirement: Standardized Exit Codes

The CLI SHALL use standardized exit codes to indicate the result of an operation.

#### Scenario: Exit codes
- **WHEN** a command finishes
- **THEN** it returns one of the following exit codes:
  - 0: Success
  - 1: Package/Version not found
  - 2: Target Framework mismatch or not found
  - 3: Invalid options or arguments
  - 4: Network or Authentication error
  - 5: Unexpected runtime error
