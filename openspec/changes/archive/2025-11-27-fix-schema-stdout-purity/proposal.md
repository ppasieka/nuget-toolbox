## Why
SchemaCommand writes success messages (`Wrote {fileName}`, `--- {commandName} ---`) to stdout, polluting JSON schema output and breaking machine consumption. Per the report (P0-HIGH, L109, L154), stdout MUST contain only valid JSON.

## What Changes
- Move success/progress messages from `Console.WriteLine` to `Console.Error.WriteLine` in SchemaCommand
- Remove decorative separators (`--- {commandName} ---`) from stdout when using `--all` without `--output`
- Ensure stdout contains only valid JSON Schema content

## Impact
- Affected specs: cli (Schema Export Command requirement)
- Affected code: `src/NuGetToolbox.Cli/Commands/SchemaCommand.cs` (L112, L129-131, L157, L162)
