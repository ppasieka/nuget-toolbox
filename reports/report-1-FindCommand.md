# FindCommand Analysis Report

## TL;DR
- FindCommand largely matches the doc's intent: resolve a NuGet package via NuGetPackageResolver and emit PackageInfo as camelCase JSON.
- Main gaps: sync-over-async usage, ad-hoc DI and file-logging provider not in dependencies, missing CancellationToken, potential schema drift (e.g., PackageInfo.Resolved), and minor testability issues.

## Detailed Analysis (Documented vs Implemented)

### Command Purpose and Functionality
- **Documented**: "Resolve package by ID + version" using NuGetPackageResolver; returns PackageInfo (resolved version, TFMs, nupkg path).
- **Implemented**: "find" command with --package (required), --version (optional), plus extra options --feed (override source) and --output. Calls NuGetPackageResolver.ResolvePackageAsync(packageId, version, feed). Emits JSON. This aligns with purpose; the extra options are useful but not documented.

### Usage of Services (NuGetPackageResolver)
- **Documented**: Core service; respects nuget.config source mappings and credentials. Return PackageInfo.
- **Implemented**: Resolves via DI (GetRequiredService<NuGetPackageResolver>), but if no provider is passed, it builds its own ServiceProvider with AddScoped<NuGetPackageResolver> (no explicit registration of resolver deps). This is workable if the resolver is self-contained, but bypasses central DI config and uses an unusual Scoped lifetime without a scope.

### Output Format (PackageInfo model)
- **Documented**: System.Text.Json camelCase; schemas exist (find.schema.json using models defs).
- **Implemented**: System.Text.Json with camelCase + indented + ignore nulls. Writes to stdout or a file. Good. It checks packageInfo.Resolved to decide success; if PackageInfo includes "resolved" and the schema does not, there is potential schema mismatch (to verify).

### Code Style Compliance
- **Nullable annotations**: Public APIs annotated (Create(IServiceProvider?), options typed as string?). Uses null-forgiving (!) on packageId, which could be avoided by stronger typing/GetValueForOption; still technically okay.
- **Naming**: OK (PascalCase for types/methods, camelCase for locals).
- **Async patterns**: ❌ Violates "no .Result/.Wait()" by using HandlerAsync(...).GetAwaiter().GetResult() in a sync handler.
- **Logging**: Uses ILoggerFactory.CreateLogger("FindCommand"); guideline prefers ILogger<T>. Also introduces builder.AddFile(...) (a file logging provider) not listed in dependencies.
- **JSON**: Uses System.Text.Json with camelCase. ✓ Compliant.
- **Imports**: Alphabetical and no wildcards. ✓ Compliant.
- **Errors**: No custom exceptions; generic catch writes Console.Error and returns 1. Guideline suggests custom exceptions with actionable messages.

## Gaps and Inconsistencies

### 1. Async Handling
Sync wrapper blocks on async (GetAwaiter().GetResult). Contrary to the async guideline; also prevents cooperative cancellation.

### 2. Logging Provider
Adds a non-standard file logger (builder.AddFile) not specified in Architecture & Dependencies. Risks compile/runtime issues if the package isn't referenced and diverges from "prefer minimal dependencies."

### 3. DI and Lifetime
- Command self-builds a ServiceProvider. Project structure suggests DI should be centrally configured (Program.cs). Building DI here fragments configuration and makes testing harder.
- Registers NuGetPackageResolver as Scoped but resolves from the root ServiceProvider (no scope). Either use a scope or change to Singleton/Transient.

### 4. Cancellation
No CancellationToken support or Ctrl+C integration. System.CommandLine can provide a token.

### 5. Schema Risk
Uses PackageInfo.Resolved property as a gate. If models-1.0.schema.json and find.schema.json don't include "resolved", emitted JSON may not validate. The docs do not list "resolved" as a PackageInfo field.

### 6. Testability
Writes to Console directly instead of using System.CommandLine IConsole/TextWriter abstraction.

### 7. Documentation Drift
--feed and --output are not referenced in AGENTS.md under FindCommand; Quick Start shows only --package. Either update docs or make flags discoverable with examples.

## Compliance with Architecture/Code Style Guidelines

### ✓ Compliant
- Purpose and service use intent
- JSON camelCase format
- Alphabetically sorted imports
- Naming conventions (PascalCase/camelCase)
- Nullable annotations on public signatures

### ❌ Not Compliant
- Async guideline (sync-over-async)
- Logging guideline (ILogger<T> and unlisted logging provider)
- Centralized DI pattern
- Custom exceptions guidance not followed
- No cancellation support

## Recommendations

### 1. Make the Handler Truly Async (Small Effort)
- Replace the sync Handler and GetAwaiter().GetResult() with an async Task<int> handler wired via System.CommandLine's SetHandler.
- Accept a CancellationToken and pass it through to NuGetPackageResolver.ResolvePackageAsync (update signature if needed).

### 2. Use Centrally Configured DI (Medium Effort)
- Prefer requiring a non-null IServiceProvider passed from Program.cs and failing fast if missing.
- If keeping internal DI, register NuGetPackageResolver as Singleton or create a scope when resolving a Scoped service.

### 3. Align Logging to Guidelines (Small Effort)
- Inject ILogger<FindCommand> from DI; avoid creating a logger via factory with string category.
- Remove builder.AddFile(...) unless it is already an approved dependency.

### 4. Improve Error Handling and Exit Codes (Small Effort)
- Distinguish "not found" (exit 1) from "unexpected error" (exit 2).
- Provide actionable error text (e.g., "Package 'X' not found in feed 'Y' or configured sources.").
- Consider custom exceptions in the resolver for predictable failure categories.

### 5. Ensure Schema Alignment (Small-Medium Effort)
- Verify find.schema.json/$defs.PackageInfo includes every property you serialize.
- If PackageInfo.Resolved is not in schema, either add it or stop emitting it.

### 6. Minor Cleanups (Small Effort)
- Use parseResult.GetValueForOption(...) and remove packageId! null-forgiving.
- Prefer IConsole (System.CommandLine) or TextWriter injection for stdout/stderr to ease testing.
- Add basic examples in command description.

## Minimal Code Sketch (Illustrative)

```csharp
// Wire async handler
command.SetHandler(async (InvocationContext ctx) =>
{
    var packageId = ctx.ParseResult.GetValueForOption(packageOption)!;
    var version = ctx.ParseResult.GetValueForOption(versionOption);
    var feed = ctx.ParseResult.GetValueForOption(feedOption);
    var output = ctx.ParseResult.GetValueForOption(outputOption);
    var sp = serviceProvider ?? throw new InvalidOperationException("ServiceProvider not configured.");
    var logger = sp.GetRequiredService<ILogger<FindCommand>>();
    using var scope = sp.CreateScope();
    return await HandlerAsync(packageId, version, feed, output, scope.ServiceProvider, ctx.GetCancellationToken());
});

// HandlerAsync signature
private static async Task<int> HandlerAsync(
    string packageId, 
    string? version, 
    string? feed, 
    string? output, 
    IServiceProvider services, 
    CancellationToken ct)
{
    var resolver = services.GetRequiredService<NuGetPackageResolver>();
    var packageInfo = await resolver.ResolvePackageAsync(packageId, version, feed, ct);
    // ...
}
```

## Scope Estimates
- Async handler + cancellation + logging tweaks: **Small** (<1h)
- DI centralization and lifetime cleanup: **Small-Medium** (1-3h)
- Schema verification and adjustments: **Small-Medium** (1-3h)

## Risks and Guardrails
- Changing handler wiring can break command registration if SetAction is a custom extension
- Removing file logging may reduce debug data (mitigate with console logging and optional verbose switch)
- If ResolvePackageAsync signature changes to add CancellationToken, update tests and callers accordingly
