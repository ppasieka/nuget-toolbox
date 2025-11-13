# Schema Export Command Proposal

## Why

Users need to programmatically validate the JSON output of NuGetToolbox commands to ensure conformance to expected structures. Currently, output formats are only documented through C# models, requiring users to inspect source code or reverse-engineer schemas from examples. Providing official JSON Schema files enables better tooling integration, validation in CI/CD pipelines, and clearer documentation.

## What Changes

- Add new `schema` command that exports JSON Schema (Draft 2020-12) definitions for all CLI commands
- Generate individual schema files per command (find, list-types, export-signatures, diff)
- Create a shared models schema file containing reusable definitions ($defs) for PackageInfo, TypeInfo, MethodInfo, etc.
- Each command schema references the shared models schema using $ref
- Support `--command <name>` flag to export schema for a specific command
- Support `--all` flag to export all schemas
- Support `--output <path>` flag to write schemas to files
- Default behavior: print schema to stdout

## Impact

- Affected specs: cli
- Affected code:
  - New: src/NuGetToolbox.Cli/Commands/SchemaCommand.cs
  - New: src/NuGetToolbox.Cli/Schemas/ (embedded resource files for schema definitions)
  - Modified: src/NuGetToolbox.Cli/Program.cs (register new command)
- No breaking changes to existing commands
