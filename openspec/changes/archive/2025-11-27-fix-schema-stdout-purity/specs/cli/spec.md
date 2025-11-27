## MODIFIED Requirements

### Requirement: Schema Export Command

The `schema` command SHALL export JSON Schema (Draft 2020-12) definitions for all CLI command outputs. All success messages and progress indicators SHALL be written to stderr, ensuring stdout contains ONLY valid JSON Schema content.

#### Scenario: Export schema for specific command
- **WHEN** user runs `schema --command find`
- **THEN** system outputs find.schema.json to stdout
- **AND** schema conforms to JSON Schema Draft 2020-12
- **AND** stdout contains ONLY the JSON schema (no messages)

#### Scenario: Export all schemas
- **WHEN** user runs `schema --all`
- **THEN** system outputs all command schemas (find, list-types, export-signatures, diff) and models schema to stdout
- **AND** each schema is valid JSON Schema Draft 2020-12
- **AND** stdout contains ONLY JSON content (no decorative separators)

#### Scenario: Write schema to file
- **WHEN** user runs `schema --command find --output "find.schema.json"`
- **THEN** system writes schema to specified file
- **AND** file contains valid JSON Schema
- **AND** success message is written to stderr (not stdout)

#### Scenario: Write all schemas to directory
- **WHEN** user runs `schema --all --output "schemas/"`
- **THEN** system writes all schema files to the directory
- **AND** directory contains: models-1.0.schema.json, find.schema.json, list-types.schema.json, export-signatures.schema.json, diff.schema.json
- **AND** success messages for each file are written to stderr (not stdout)

#### Scenario: Invalid command name
- **WHEN** user runs `schema --command invalid-name`
- **THEN** system returns non-zero exit code
- **AND** system logs error indicating valid command names to stderr

#### Scenario: Default behavior (no flags)
- **WHEN** user runs `schema` without flags
- **THEN** system outputs models schema to stdout
- **AND** stdout contains ONLY valid JSON Schema

#### Scenario: Stdout purity for piping
- **WHEN** user runs `schema --command find | jq .`
- **THEN** jq successfully parses the output
- **AND** no log messages or success indicators appear in stdout
