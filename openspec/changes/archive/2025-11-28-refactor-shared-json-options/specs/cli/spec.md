## ADDED Requirements

### Requirement: Shared JSON Serialization Configuration

The system SHALL provide a centralized `CommandOutput` utility class for JSON serialization and result output, ensuring consistent formatting across all CLI commands.

#### Scenario: Serialize object to JSON with standard options
- **WHEN** `CommandOutput.SerializeJson<T>` is called with any serializable object
- **THEN** the output uses camelCase property naming
- **AND** the output is indented for readability
- **AND** null values are omitted from output

#### Scenario: Write result to file
- **WHEN** `CommandOutput.WriteResultAsync` is called with a non-empty output path
- **THEN** the content is written to the specified file asynchronously
- **AND** the logger records the output path at Information level

#### Scenario: Write result to stdout
- **WHEN** `CommandOutput.WriteResultAsync` is called with null or empty output path
- **THEN** the content is written to standard output
- **AND** no file is created

#### Scenario: Cancellation support for file writes
- **WHEN** `CommandOutput.WriteResultAsync` is called with a CancellationToken
- **AND** the token is cancelled during write
- **THEN** the operation stops gracefully
- **AND** partial files may remain (OS-dependent)
