## Why

The CLI commands use non-standard logging patterns that violate AGENTS.md guidelines and Microsoft best practices:

1. **Stdout Purity Violations** (P0): Logs written to stdout contaminate JSON output, making it unparseable by downstream tools like `jq`
2. **Non-standard logging provider** (P1): Uses `Serilog.Extensions.Logging.File` (`AddFile`) which is undocumented in AGENTS.md runtime dependencies
3. **Non-typed loggers** (P2): Uses `ILoggerFactory.CreateLogger("string")` instead of `ILogger<T>` as required by AGENTS.md

**Microsoft Documentation References:**
- [ConsoleLoggerOptions.LogToStandardErrorThreshold](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.console.consoleloggeroptions.logtostandarderrorthreshold): "Gets or sets value indicating the minimum level of messages that get written to `Console.Error`." Setting to `LogLevel.Trace` sends ALL logs to stderr.
- [Logging in C# and .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging): "The recommended practice for log category names is to use the fully qualified name of the class."
- [Logging providers in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging-providers): "To configure a service that depends on `ILogger<T>`, use constructor injection."

These issues affect FindCommand, ListTypesCommand, ExportSignaturesCommand, and DiffCommand.

## What Changes

### 1. Guarantee Stdout Purity (P0 - Critical)
- Configure `LogToStandardErrorThreshold = LogLevel.Trace` to direct ALL log output to stderr
- This ensures stdout contains ONLY JSON output
- Enables clean piping: `dotnet run -- find ... | jq .`

### 2. Remove Undocumented Dependency
- Remove `Serilog.Extensions.Logging.File` package from NuGetToolbox.Cli.csproj
- Remove all `AddFile` logging configuration from commands

### 3. Adopt Microsoft-Recommended Logging Patterns
- Use `ILogger<T>` via dependency injection (per Microsoft best practices)
- Centralize logging configuration in Program.cs using `AddLogging(builder => builder.AddConsole(...))`

### 4. Implementation Pattern (from Microsoft documentation)
```csharp
// Program.cs - Centralized DI configuration
var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole(options =>
{
    // CRITICAL: This sends ALL log levels to stderr, keeping stdout clean
    options.LogToStandardErrorThreshold = LogLevel.Trace;
}));

// Command handlers - Use ILogger<T>
var logger = serviceProvider.GetRequiredService<ILogger<FindCommand>>();
```

## Impact

- Affected specs: `cli/spec.md` (File-Based Logging requirement needs modification)
- Affected code:
  - `src/NuGetToolbox.Cli/Program.cs` - centralized logging setup with stderr
  - `src/NuGetToolbox.Cli/Commands/FindCommand.cs` - typed logger
  - `src/NuGetToolbox.Cli/Commands/ListTypesCommand.cs` - typed logger
  - `src/NuGetToolbox.Cli/Commands/ExportSignaturesCommand.cs` - typed logger
  - `src/NuGetToolbox.Cli/Commands/DiffCommand.cs` - typed logger
  - `src/NuGetToolbox.Cli/NuGetToolbox.Cli.csproj` - remove Serilog package
