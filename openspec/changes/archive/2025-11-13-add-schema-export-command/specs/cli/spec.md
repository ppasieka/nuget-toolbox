## ADDED Requirements

### Requirement: Schema Export Command

The `schema` command SHALL export JSON Schema (Draft 2020-12) definitions for all CLI command outputs.

#### Scenario: Export schema for specific command
- **WHEN** user runs `schema --command find`
- **THEN** system outputs find.schema.json to stdout
- **AND** schema conforms to JSON Schema Draft 2020-12

#### Scenario: Export all schemas
- **WHEN** user runs `schema --all`
- **THEN** system outputs all command schemas (find, list-types, export-signatures, diff) and models schema
- **AND** each schema is valid JSON Schema Draft 2020-12

#### Scenario: Write schema to file
- **WHEN** user runs `schema --command find --output "find.schema.json"`
- **THEN** system writes schema to specified file
- **AND** file contains valid JSON Schema

#### Scenario: Write all schemas to directory
- **WHEN** user runs `schema --all --output "schemas/"`
- **THEN** system writes all schema files to the directory
- **AND** directory contains: models-1.0.schema.json, find.schema.json, list-types.schema.json, export-signatures.schema.json, diff.schema.json

#### Scenario: Invalid command name
- **WHEN** user runs `schema --command invalid-name`
- **THEN** system returns non-zero exit code
- **AND** system logs error indicating valid command names

#### Scenario: Default behavior (no flags)
- **WHEN** user runs `schema` without flags
- **THEN** system outputs models schema or shows help message

### Requirement: Shared Models Schema

The system SHALL provide a shared models schema (models-1.0.schema.json) containing $defs for all common data structures.

#### Scenario: Models schema includes all types
- **WHEN** models schema is exported
- **THEN** schema contains $defs for: PackageInfo, TypeInfo, MethodInfo, ParameterInfo, DiffResult, DirectDependency
- **AND** each $def matches the structure of corresponding C# model

#### Scenario: $defs are valid JSON Schema objects
- **WHEN** models schema is validated
- **THEN** each $def includes "type": "object"
- **AND** includes "properties" with field definitions
- **AND** includes "required" array for mandatory fields

### Requirement: Per-Command Schemas

The system SHALL provide individual schemas for each CLI command (find, list-types, export-signatures, diff) that reference the shared models schema.

#### Scenario: Find command schema
- **WHEN** find schema is exported
- **THEN** schema uses $ref to models-1.0.schema.json#/$defs/PackageInfo
- **AND** schema declares $schema as "https://json-schema.org/draft/2020-12/schema"

#### Scenario: List-types command schema
- **WHEN** list-types schema is exported
- **THEN** schema defines type: "array"
- **AND** items use $ref to models-1.0.schema.json#/$defs/TypeInfo

#### Scenario: Export-signatures command schema
- **WHEN** export-signatures schema is exported
- **THEN** schema defines type: "array"
- **AND** items use $ref to models-1.0.schema.json#/$defs/MethodInfo

#### Scenario: Diff command schema
- **WHEN** diff schema is exported
- **THEN** schema uses $ref to models-1.0.schema.json#/$defs/DiffResult

### Requirement: Schema Versioning

The system SHALL version schemas using semantic versioning embedded in filenames.

#### Scenario: Schema filenames include version
- **WHEN** schemas are exported
- **THEN** shared models schema is named "models-1.0.schema.json"
- **AND** version reflects schema structure version, not CLI version

#### Scenario: $id includes version
- **WHEN** schemas are validated
- **THEN** each schema includes $id with version (e.g., "https://nuget-toolbox.local/schemas/models-1.0.schema.json")
- **AND** $id is stable across releases for the same schema version

### Requirement: Schema Documentation and Annotations

The system SHALL include comprehensive documentation and annotations in all schemas to support LLM/AI agent consumption and human understanding.

#### Scenario: Root schema includes metadata
- **WHEN** any schema is exported
- **THEN** schema includes "title" describing the schema purpose
- **AND** includes "description" explaining the schema's role and usage
- **AND** includes "$comment" with additional context if needed

#### Scenario: All properties have descriptions
- **WHEN** models schema is exported
- **THEN** every property in every $def includes a "description" field
- **AND** descriptions explain the property's purpose, format, and constraints
- **AND** descriptions are clear and actionable for AI agents

#### Scenario: Complex types include examples
- **WHEN** models schema includes complex types (objects, arrays)
- **THEN** schema includes "examples" array with representative data
- **AND** examples match the schema structure
- **AND** examples reflect real-world usage patterns

#### Scenario: Enumerations include descriptions
- **WHEN** schema includes enum fields
- **THEN** schema includes "description" explaining valid values
- **AND** optionally uses "oneOf" with "const" and "title" for semantic enum descriptions

#### Scenario: Required fields are documented
- **WHEN** schema includes "required" array
- **THEN** descriptions explain why fields are required
- **AND** optional fields indicate when they may be null or absent

#### Scenario: Field formats are specified
- **WHEN** schema includes string fields with specific formats
- **THEN** schema uses "format" keyword (e.g., "uri", "date-time")
- **AND** includes "pattern" for custom formats
- **AND** description explains format expectations

#### Scenario: Array items are documented
- **WHEN** schema includes array properties
- **THEN** "items" definition includes description
- **AND** description explains what each array element represents
- **AND** includes constraints like "minItems", "maxItems" if applicable

#### Scenario: Schema includes usage examples
- **WHEN** command-specific schemas are exported
- **THEN** schema includes top-level "examples" with complete command output samples
- **AND** examples show typical successful responses
- **AND** examples demonstrate optional fields when present
