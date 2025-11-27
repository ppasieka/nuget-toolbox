## ADDED Requirements

### Requirement: Schema Command Option Validation

The `schema` command SHALL validate option combinations and provide clear error messages for invalid inputs.

#### Scenario: Mutual exclusivity of --command and --all
- **WHEN** user runs `schema --command find --all`
- **THEN** system returns exit code 3 (InvalidOptions)
- **AND** system writes error to stderr: "Error: --command and --all are mutually exclusive"

#### Scenario: --all requires directory output path
- **WHEN** user runs `schema --all --output "single-file.json"`
- **AND** the path does not end with `/` or `\` and is not an existing directory
- **THEN** system returns exit code 3 (InvalidOptions)
- **AND** system writes error to stderr: "Error: --output with --all must be a directory path"

#### Scenario: --all with valid directory path
- **WHEN** user runs `schema --all --output "schemas/"`
- **THEN** system creates directory if needed
- **AND** system writes all schema files to the directory
- **AND** system returns exit code 0

#### Scenario: Case-insensitive command names
- **WHEN** user runs `schema --command FIND`
- **THEN** system treats "FIND" as equivalent to "find"
- **AND** system exports the find.schema.json
- **AND** system returns exit code 0

#### Scenario: Case-insensitive command names with mixed case
- **WHEN** user runs `schema --command List-Types`
- **THEN** system treats "List-Types" as equivalent to "list-types"
- **AND** system exports the list-types.schema.json
- **AND** system returns exit code 0

### Requirement: Export Signatures Format Validation

The `export-signatures` command SHALL validate the `--format` option value and reject invalid formats.

#### Scenario: Valid format json
- **WHEN** user runs `export-signatures --package "Pkg" --format json`
- **THEN** system accepts the format
- **AND** system outputs JSON array

#### Scenario: Valid format jsonl
- **WHEN** user runs `export-signatures --package "Pkg" --format jsonl`
- **THEN** system accepts the format
- **AND** system outputs JSONL (one JSON object per line)

#### Scenario: Invalid format rejected
- **WHEN** user runs `export-signatures --package "Pkg" --format xml`
- **THEN** system returns exit code 3 (InvalidOptions)
- **AND** system writes error to stderr indicating valid formats are "json" or "jsonl"

#### Scenario: Format option uses FromAmong constraint
- **WHEN** user runs `export-signatures --help`
- **THEN** help text shows `--format` with allowed values: json, jsonl


