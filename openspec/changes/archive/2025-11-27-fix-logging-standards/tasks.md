## 1. Remove Serilog Dependency

- [x] 1.1 Remove `Serilog.Extensions.Logging.File` package from `NuGetToolbox.Cli.csproj`
- [x] 1.2 Ensure `Microsoft.Extensions.Logging.Console` is referenced (add if missing)

## 2. Centralize Logging in Program.cs with Stderr Output

- [x] 2.1 Configure console logging in Program.cs ServiceCollection with stderr threshold:
  ```csharp
  services.AddLogging(builder => builder.AddConsole(options =>
  {
      // CRITICAL: Send ALL log levels to stderr to keep stdout clean for JSON
      options.LogToStandardErrorThreshold = LogLevel.Trace;
  }));
  ```
- [x] 2.2 Verify logging is configured before building ServiceProvider

## 3. Update Command Logging to Use ILogger<T>

- [x] 3.1 FindCommand: Updated to use `GetRequiredService<ILogger<NuGetPackageResolver>>()`
- [x] 3.2 ListTypesCommand: Updated to use `nameof(ListTypesCommand)` (static class - can't use ILogger<T>)
- [x] 3.3 ExportSignaturesCommand: Updated to use `nameof(ExportSignaturesCommand)` (static class)
- [x] 3.4 DiffCommand: Updated to use `nameof(DiffCommand)` (static class)

## 4. Remove AddFile Calls from Commands

- [x] 4.1 FindCommand: Removed local logging config (now uses centralized Program.cs)
- [x] 4.2 ListTypesCommand: No AddFile was present
- [x] 4.3 ExportSignaturesCommand: No AddFile was present
- [x] 4.4 DiffCommand: No AddFile was present

## 5. Verification - Stdout Purity (Critical)

- [x] 5.1 Run `dotnet build` and verify no Serilog references remain
- [x] 5.2 **Stdout purity test** - JSON output pipes cleanly (verified)
- [x] 5.3 All 69 tests pass
- [x] 5.4 Verify log categories show full type names (e.g., `NuGetToolbox.Cli.Commands.FindCommand`)
- [x] 5.5 Add/update E2E tests to validate stdout purity for all commands (StdoutPurityE2ETests.cs - 7 tests)
