## MODIFIED Requirements

### Requirement: File-Based Logging

The system SHALL direct all logging output to stderr using the standard .NET console logger with `LogToStandardErrorThreshold = LogLevel.Trace`, ensuring stdout contains ONLY valid JSON output. File-based logging SHALL NOT be used. Loggers SHALL be created using `ILogger<T>` pattern per Microsoft best practices.

#### Scenario: Stdout contains only JSON
- **WHEN** user runs any command that produces JSON output
- **THEN** stdout contains ONLY valid JSON (no log messages, no status messages)
- **AND** output can be successfully parsed by `jq` or any JSON parser
- **AND** piping works correctly: `dotnet run -- find ... | jq .`

#### Scenario: All logs directed to stderr
- **WHEN** CLI application starts
- **THEN** Program.cs configures console logger with `LogToStandardErrorThreshold = LogLevel.Trace`
- **AND** log messages at ALL levels (Trace, Debug, Information, Warning, Error, Critical) are written to stderr
- **AND** NO log messages are written to stdout

#### Scenario: Clean console output separation
- **WHEN** user runs commands with JSON output
- **THEN** JSON data is written to stdout
- **AND** all logging, progress, and diagnostic messages appear in stderr
- **AND** users can redirect stderr independently: `command 2>/dev/null | jq .`

#### Scenario: Typed logger creation
- **WHEN** commands create loggers
- **THEN** they use `ILogger<T>` pattern via `GetRequiredService<ILogger<T>>()`
- **AND** log categories show full type names (e.g., `NuGetToolbox.Cli.Commands.FindCommand`)
- **AND** loggers are NOT created using string-based `CreateLogger("string")` pattern

#### Scenario: Structured logging with placeholders
- **WHEN** commands log messages
- **THEN** they use structured logging with `{Placeholder}` syntax
- **AND** avoid string concatenation in log messages

## ADDED Requirements

### Requirement: Console Logger Configuration

The system SHALL configure the console logging provider using `Microsoft.Extensions.Logging.Console` package with stderr output for all log levels to maintain stdout purity.

#### Scenario: Console logger with stderr threshold
- **WHEN** Program.cs configures services
- **THEN** it calls `services.AddLogging(builder => builder.AddConsole(options => ...))`
- **AND** sets `options.LogToStandardErrorThreshold = LogLevel.Trace`
- **AND** this ensures ALL log levels write to stderr, not stdout

#### Scenario: Logging provider dependencies
- **WHEN** NuGetToolbox.Cli.csproj is examined
- **THEN** it references `Microsoft.Extensions.Logging.Console` package
- **AND** does NOT reference `Serilog.Extensions.Logging.File` package
- **AND** uses only built-in .NET logging infrastructure

#### Scenario: E2E stdout purity verification
- **WHEN** E2E tests run commands with JSON output
- **THEN** tests verify stdout can be parsed as valid JSON
- **AND** tests verify no log messages appear in stdout
- **AND** tests use pattern: `command 2>/dev/null | jq .` to validate
