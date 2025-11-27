## Why

The CLI has several option handling bugs identified in the aggregated report: mutual exclusivity not enforced for SchemaCommand flags, missing format validation in ExportSignaturesCommand, unused --no-cache option (dead code), and inconsistent case handling for command names.

## What Changes

- **SchemaCommand**: Enforce mutual exclusivity between `--command` and `--all` flags (return error if both specified)
- **SchemaCommand**: Validate that `--output` with `--all` is a directory path (ends with `/` or `\`, or is an existing directory)
- **SchemaCommand**: Make command name matching case-insensitive
- **ExportSignaturesCommand**: Add `FromAmong` validation for `--format` option (json, jsonl)
- **ExportSignaturesCommand**: Remove unused `--no-cache` option (dead code)

## Impact

- Affected specs: cli
- Affected code: SchemaCommand.cs, ExportSignaturesCommand.cs
