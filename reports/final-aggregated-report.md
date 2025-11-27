# NuGet Toolbox - Final Aggregated Analysis Report

**Generated:** 2025-11-16  
**Scope:** All commands (Find, ListTypes, ExportSignatures, Diff, Schema)  
**Status:** ❌ Not production-ready - 38 issues identified (17 P0, 13 P1, 8 P2/P3)

---

## Executive Summary

Analysis of all five commands reveals **consistent architectural issues** across the codebase that must be addressed before production deployment. While core functionality is implemented correctly, critical CLI hygiene violations, resource management issues, and code style non-compliance create significant reliability and usability risks.

**Key Findings:**
- ✅ **Strengths:** Core NuGet package resolution, assembly inspection, and JSON serialization work correctly
- ❌ **Critical Gaps:** Stdout pollution, async anti-patterns, resource leaks, missing cancellation support
- ⚠️ **Inconsistencies:** Ad-hoc DI patterns, non-standard logging, weak TFM selection logic

**Overall Assessment:** ~60% production-ready. Estimated **12-20 hours** of focused work required to address all P0/P1 issues.

---

## Cross-Cutting Issues (Affect Multiple Commands)

### 1. Stdout Purity Violations (P0-CRITICAL)
**Affects:** FindCommand, ListTypesCommand, ExportSignaturesCommand, DiffCommand, SchemaCommand  
**Severity:** P0 (Blocks machine consumption)

**Problem:**
Logs and success messages written to stdout contaminate JSON output, making it unparseable by downstream tools.

**Evidence:**
| Command | Lines | Issue |
|---------|-------|-------|
| FindCommand | L77, L101, L115 | Logs to stdout during JSON output |
| ListTypesCommand | L77, L118 | LogInformation to stdout |
| ExportSignaturesCommand | L98, L120 | Logs interleaved with JSON |
| DiffCommand | L86, L88, L93, L109, L131 | Multiple LogInformation calls |
| SchemaCommand | L109, L154 | Success messages to stdout |

**Impact:**
- Breaks piping to `jq`, JSON parsers, automation scripts
- Violates Unix philosophy (data to stdout, messages to stderr)
- Makes schema validation impossible

**Fix Strategy:**
```csharp
// Option 1: Console logger to stderr
services.AddLogging(builder => 
    builder.AddConsole(opts => 
        opts.LogToStandardErrorThreshold = LogLevel.Trace));

// Option 2: Success messages to stderr
Console.Error.WriteLine($"Success: ...");

// Option 3: Add --verbose flag to control logging
```

**Acceptance Criteria:**
- ✅ `dotnet run -- <command> ... | jq .` succeeds
- ✅ Stdout contains ONLY JSON output
- ✅ All logs/messages appear in stderr

**Estimated Effort:** 1-2 hours across all commands

---

### 2. Async Handler Anti-Pattern (P0-CRITICAL)
**Affects:** FindCommand, ListTypesCommand, ExportSignaturesCommand, DiffCommand  
**Severity:** P0 (Deadlock risk)

**Problem:**
All commands use sync-over-async pattern with `.GetAwaiter().GetResult()`, violating AGENTS.md guidelines and risking thread pool starvation.

**Evidence:**
| Command | Line | Code |
|---------|------|------|
| FindCommand | L56 | `HandlerAsync(...).GetAwaiter().GetResult()` |
| ListTypesCommand | L57 | `HandlerAsync(...).GetAwaiter().GetResult()` |
| ExportSignaturesCommand | L79 | `HandlerAsync(...).GetAwaiter().GetResult()` |
| DiffCommand | L67 | `HandlerAsync(...).GetAwaiter().GetResult()` |

**AGENTS.md Violation:**
> "Async: `async Task` for I/O; no `.Result` or `.Wait()`"

**Fix:**
```csharp
// Replace SetAction with SetHandler
command.SetHandler(async (InvocationContext ctx) =>
{
    var exitCode = await HandlerAsync(..., ctx.GetCancellationToken());
    ctx.ExitCode = exitCode;
});
```

**Acceptance Criteria:**
- ✅ No `.GetAwaiter().GetResult()` in codebase
- ✅ All handlers use async SetHandler
- ✅ Exit codes set via ctx.ExitCode

**Estimated Effort:** 1-2 hours (consistent pattern across commands)

---

### 3. Temp Directory Cleanup Missing (P0-CRITICAL)
**Affects:** ListTypesCommand, ExportSignaturesCommand, DiffCommand  
**Severity:** P0 (Resource leak)

**Problem:**
Extracted package assemblies never deleted, causing disk bloat over time.

**Evidence:**
| Command | Lines | Issue |
|---------|-------|-------|
| ListTypesCommand | L150-151 | tempDir created, never deleted |
| ExportSignaturesCommand | L163-164 | No cleanup logic |
| DiffCommand | L167-168, L200 | Two temp dirs, both leak |

**Impact:**
- ~1-10MB leaked per command invocation
- Fills %TEMP% directory over days/weeks
- CI/CD environments particularly affected

**Fix:**
```csharp
private static async Task<int> HandlerAsync(...)
{
    string? tempDir = null;
    try
    {
        var assemblies = await ExtractAssembliesAsync(..., out tempDir);
        // ... processing ...
        return 0;
    }
    finally
    {
        if (!string.IsNullOrEmpty(tempDir) && Directory.Exists(tempDir))
        {
            try { Directory.Delete(tempDir, recursive: true); }
            catch (Exception ex) 
            { 
                logger?.LogWarning(ex, "Failed to clean up {TempDir}", tempDir); 
            }
        }
    }
}
```

**Acceptance Criteria:**
- ✅ No temp directories remain after execution
- ✅ Cleanup occurs even on exception/cancellation
- ✅ E2E tests verify cleanup

**Estimated Effort:** 1-2 hours across affected commands

---

### 4. Missing Cancellation Support (P1-HIGH)
**Affects:** FindCommand, ListTypesCommand, ExportSignaturesCommand, DiffCommand  
**Severity:** P1 (Poor UX, partial files)

**Problem:**
No CancellationToken propagation from System.CommandLine context to async operations.

**Evidence:**
| Command | Issue |
|---------|-------|
| FindCommand | No token in HandlerAsync signature |
| ListTypesCommand | `CancellationToken.None` hardcoded (L138) |
| ExportSignaturesCommand | Token not passed to resolver |
| DiffCommand | `CancellationToken.None` at L155 |

**Impact:**
- Ctrl+C doesn't cancel long-running operations
- Partial files may be written
- Resource waste (continued processing after cancel)

**Fix:**
```csharp
// In handler:
var ct = ctx.GetCancellationToken();
await HandlerAsync(..., ct);

// In methods:
await resolver.ResolvePackageAsync(..., cancellationToken);
await packageReader.GetLibItemsAsync(cancellationToken);
await File.WriteAllTextAsync(..., cancellationToken);
```

**Acceptance Criteria:**
- ✅ Ctrl+C exits with non-zero code
- ✅ No partial output files
- ✅ Temp cleanup still occurs
- ✅ E2E test simulates cancellation

**Estimated Effort:** 1-2 hours (thread CancellationToken through call chains)

---

### 5. Ad-hoc DI Configuration (P0-CRITICAL)
**Affects:** FindCommand, DiffCommand  
**Severity:** P0 (Architecture violation)

**Problem:**
Commands create their own `ServiceProvider` instead of using centralized DI from Program.cs.

**Evidence:**
| Command | Lines | Code |
|---------|-------|------|
| FindCommand | L70, L120-132 | `CreateDefaultServiceProvider()` |
| DiffCommand | L80-84 | Ad-hoc ServiceProvider creation |

**AGENTS.md Guideline:**
> "Move DI setup to Program.cs; require non-null IServiceProvider in Create()"

**Impact:**
- Fragments configuration across commands
- Inconsistent service lifetimes
- Harder to test (can't inject mocks easily)

**Fix:**
```csharp
// Program.cs
var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole());
services.AddSingleton<NuGetPackageResolver>();
services.AddTransient<SignatureExporter>();
services.AddTransient<ApiDiffAnalyzer>();
var serviceProvider = services.BuildServiceProvider();

// Commands
rootCommand.Subcommands.Add(FindCommand.Create(serviceProvider));
rootCommand.Subcommands.Add(DiffCommand.Create(serviceProvider));
```

**Acceptance Criteria:**
- ✅ All DI configuration in Program.cs
- ✅ Commands receive IServiceProvider via Create()
- ✅ No `new ServiceCollection()` in commands
- ✅ Unit tests can inject mock services

**Estimated Effort:** 1-2 hours (centralize DI setup)

---

### 6. DI Lifetime Misuse (P1-HIGH)
**Affects:** FindCommand, DiffCommand  
**Severity:** P1 (Memory leaks)

**Problem:**
Services registered as Scoped but resolved from root provider without creating a scope.

**Evidence:**
| Command | Lines | Issue |
|---------|-------|-------|
| FindCommand | L130, L72 | AddScoped<NuGetPackageResolver>, resolved from root |
| DiffCommand | L212-216, L82 | Scoped services resolved without scope |

**Impact:**
- Scoped services never disposed
- Memory leaks in long-running scenarios
- Non-deterministic lifetime behavior

**Fix:**
```csharp
// Option 1: Use Singleton/Transient instead of Scoped
services.AddSingleton<NuGetPackageResolver>();

// Option 2: Create scope in handler
using var scope = serviceProvider.CreateScope();
var resolver = scope.ServiceProvider.GetRequiredService<NuGetPackageResolver>();
```

**Acceptance Criteria:**
- ✅ Scoped services created within scope
- ✅ No memory leaks in repeated invocations
- ✅ Service lifetimes documented

**Estimated Effort:** 30 minutes - 1 hour

---

### 7. Non-Standard Logging Provider (P1-HIGH)
**Affects:** FindCommand, ListTypesCommand, ExportSignaturesCommand, DiffCommand  
**Severity:** P1 (Undocumented dependency)

**Problem:**
Uses `Serilog.Extensions.Logging.File` (AddFile) which is not documented in AGENTS.md runtime dependencies.

**Evidence:**
| Command | Line | Code |
|---------|------|------|
| FindCommand | L128 | `builder.AddFile(logFile, minimumLevel: LogLevel.Debug)` |
| ListTypesCommand | L177-180 | AddFile with temp log path |
| ExportSignaturesCommand | L206 | AddFile provider |
| DiffCommand | L210 | AddFile provider |
| Project | NuGetToolbox.Cli.csproj:12 | PackageReference Serilog.Extensions.Logging.File |

**AGENTS.md Runtime Dependencies:**
Lists `ILogger<T>` but no file logging provider mentioned.

**Impact:**
- Undocumented dependency
- Stdout pollution risk if misconfigured
- Inconsistent with project standards

**Fix:**
```csharp
// Option 1: Remove AddFile, use Console to stderr only
services.AddLogging(builder => 
    builder.AddConsole(opts => opts.LogToStandardErrorThreshold = LogLevel.Trace));

// Option 2: Document in AGENTS.md
// Add to Runtime section: "Serilog.Extensions.Logging.File for file-based audit logs"
```

**Acceptance Criteria:**
- ✅ Logging providers documented in AGENTS.md
- ✅ Console logger writes to stderr
- ✅ File logging optional or removed

**Estimated Effort:** 30 minutes (remove or document)

---

### 8. Non-Typed Logger Creation (P2-MEDIUM)
**Affects:** FindCommand, ListTypesCommand, ExportSignaturesCommand, DiffCommand  
**Severity:** P2 (Code style)

**Problem:**
Uses `ILoggerFactory.CreateLogger("string")` instead of `ILogger<T>`.

**Evidence:**
| Command | Lines | Code |
|---------|-------|------|
| FindCommand | L73-74 | `CreateLogger("FindCommand")` |
| ListTypesCommand | L74-75 | `CreateLogger("ListTypesCommand")` |
| ExportSignaturesCommand | L98 | `CreateLogger("ExportSignaturesCommand")` |
| DiffCommand | L86 | `CreateLogger("DiffCommand")` |

**AGENTS.md Requirement:**
> "Logging: Use `ILogger<T>` with structured context"

**Fix:**
```csharp
var logger = serviceProvider.GetRequiredService<ILogger<FindCommand>>();
// OR
var logger = loggerFactory.CreateLogger<FindCommand>();
```

**Acceptance Criteria:**
- ✅ All loggers use `ILogger<T>` pattern
- ✅ Log categories show full type names

**Estimated Effort:** 15-30 minutes

---

### 9. Weak TFM Selection Logic (P1-HIGH)
**Affects:** ListTypesCommand, ExportSignaturesCommand, DiffCommand  
**Severity:** P1 (Incorrect framework picks)

**Problem:**
Uses naive `OrderByDescending(g => g.TargetFramework.Version)` which fails across framework families.

**Evidence:**
| Command | Lines | Issue |
|---------|-------|-------|
| ListTypesCommand | L140-142 | OrderByDescending Version |
| ExportSignaturesCommand | L153-155 | Simple Version sort |
| DiffCommand | L157-159 | OrderByDescending Version |

**Impact:**
- May prefer `net4.8` over `netstandard2.0` when `net8.0` is more compatible with netstandard
- Users get wrong assemblies for their runtime
- Confusing errors with multi-target packages

**Fix:**
```csharp
using NuGet.Frameworks;

var reducer = new FrameworkReducer();
var currentFramework = NuGetFramework.Parse("net8.0"); // or from runtime
var availableFrameworks = libItems.Select(g => g.TargetFramework);
var nearest = reducer.GetNearest(currentFramework, availableFrameworks);
var targetGroup = libItems.FirstOrDefault(g => g.TargetFramework == nearest);
```

**Acceptance Criteria:**
- ✅ Uses FrameworkReducer for compatibility
- ✅ E2E test with netstandard package on net8.0 runtime
- ✅ Error lists available TFMs on mismatch

**Estimated Effort:** 1-2 hours (requires NuGet.Frameworks integration)

---

### 10. No ref/ Assembly Preference (P2-MEDIUM)
**Affects:** ListTypesCommand, ExportSignaturesCommand, DiffCommand  
**Severity:** P2 (Noisier API surface)

**Problem:**
Only checks `lib/` assemblies; ignores `ref/` (reference assemblies) which provide cleaner public API surface.

**Evidence:**
| Command | Line | Code |
|---------|------|------|
| ListTypesCommand | L138 | `GetLibItemsAsync()` only |
| ExportSignaturesCommand | L152 | `GetLibItemsAsync()` only |
| DiffCommand | L155 | `GetLibItemsAsync()` only |

**Impact:**
- Includes implementation details from lib/ assemblies
- Noisier output (private types in lib/)
- ref/ assemblies are designed for metadata inspection

**Fix:**
```csharp
var refItems = (await packageReader.GetReferenceItemsAsync(ct)).ToList();
var libItems = (await packageReader.GetLibItemsAsync(ct)).ToList();

// Prefer ref, fallback to lib
var items = refItems.Any() ? refItems : libItems;
var targetGroup = SelectTargetFramework(items, tfm);
```

**Acceptance Criteria:**
- ✅ Prefers ref/ when available
- ✅ Falls back to lib/ gracefully
- ✅ Test with package containing both

**Estimated Effort:** 30 minutes - 1 hour

---

### 11. Exit Code Standardization Missing (P1-HIGH)
**Affects:** FindCommand, DiffCommand  
**Severity:** P1 (Poor error UX)

**Problem:**
All errors return exit code 1; users cannot distinguish failure modes (not found vs network error vs TFM mismatch).

**Evidence:**
| Command | Lines | Issue |
|---------|-------|-------|
| FindCommand | L84, L116 | Both return 1 |
| DiffCommand | L94, L101, L110 | All return 1 |

**AGENTS.md Guideline:**
> "Errors: Custom exceptions with actionable messages"

**Recommended Exit Codes:**
- 0: Success
- 1: Package/version not found
- 2: TFM not found / no assemblies
- 3: Invalid options
- 4: Network/auth errors
- 5: Unexpected error

**Fix:**
```csharp
// Define constants
const int ExitSuccess = 0;
const int ExitNotFound = 1;
const int ExitTfmMismatch = 2;
const int ExitInvalidOptions = 3;
const int ExitError = 5;

// Use in error handling
catch (PackageNotFoundException)
{
    Console.Error.WriteLine($"Package '{id}' not found. Try 'dotnet run -- find --package {id}' to list versions.");
    return ExitNotFound;
}
```

**Acceptance Criteria:**
- ✅ Exit codes documented in README
- ✅ Error messages include next steps
- ✅ E2E tests verify error codes

**Estimated Effort:** 1 hour

---

### 12. Non-Deterministic Output (P3-LOW)
**Affects:** ListTypesCommand, DiffCommand  
**Severity:** P3 (Testing friction)

**Problem:**
Output arrays not sorted, causing non-reproducible results between runs.

**Evidence:**
| Command | Lines | Issue |
|---------|-------|-------|
| ListTypesCommand | L95-100 | No sorting before serialization |
| DiffCommand (ApiDiffAnalyzer) | L37-39 | breaking[], added[], removed[] unsorted |

**Impact:**
- Identical inputs produce different JSON byte layouts
- Harder to diff outputs
- Test assertions more complex

**Fix:**
```csharp
// ListTypesCommand
allTypes = allTypes
    .OrderBy(t => t.Namespace)
    .ThenBy(t => t.Name)
    .ToList();

// DiffCommand (ApiDiffAnalyzer)
Breaking = breaking
    .OrderBy(b => b.Type)
    .ThenBy(b => b.Signature)
    .ToList()
```

**Acceptance Criteria:**
- ✅ Repeated runs produce identical JSON
- ✅ E2E tests verify byte-for-byte equality

**Estimated Effort:** 30 minutes

---

## Command-Specific Issues

### FindCommand

| # | Issue | Severity | Effort | Lines |
|---|-------|----------|--------|-------|
| 1 | Security: Credential logging risk | P1-HIGH | Small | L76-77 |
| 2 | File output safety (no atomic write) | P2-MEDIUM | Small | L98-101 |
| 3 | Missing IConsole abstraction | P2-MEDIUM | Small | L105, L115 |
| 4 | Null-forgiving operator misuse | P2-LOW | Small | L56 |
| 5 | Schema conformance | ✅ VERIFIED | - | PackageInfo.Resolved in schema |

**FindCommand-Specific Effort:** 2-3 hours

---

### ListTypesCommand

| # | Issue | Severity | Effort | Lines |
|---|-------|----------|--------|-------|
| 1 | Schema conformance | ✅ VERIFIED | - | Matches list-types.schema.json |
| 2 | Resource disposal | ✅ VERIFIED | - | PackageArchiveReader, MetadataLoadContext |

**ListTypesCommand-Specific Effort:** Minimal (cross-cutting issues only)

---

### ExportSignaturesCommand

| # | Issue | Severity | Effort | Lines |
|---|-------|----------|--------|-------|
| 1 | Unused --no-cache option (dead code) | P0-HIGH | Small | L48-52, L76 |
| 2 | Missing format validation (FromAmong) | P1-HIGH | Small | L32-36 |
| 3 | TFM selection not logged | P2-MEDIUM | Small | L193 |
| 4 | Namespace filter semantics undocumented | P2-MEDIUM | Small | L38-41 |
| 5 | JSON escaping | ✅ VERIFIED | - | UnsafeRelaxedJsonEscaping configured |
| 6 | Resource disposal | ✅ VERIFIED | - | Using statements present |

**ExportSignaturesCommand-Specific Effort:** 1-2 hours

---

### DiffCommand

| # | Issue | Severity | Effort | Lines |
|---|-------|----------|--------|-------|
| 1 | Schema conformance (unvalidated) | P1-HIGH | Medium | L119-126 missing encoder |
| 2 | Resource disposal | ⚠️ PARTIAL | Small | Verify MetadataLoadContext in service |

**DiffCommand-Specific Effort:** 1-2 hours

---

### SchemaCommand

| # | Issue | Severity | Effort | Lines |
|---|-------|----------|--------|-------|
| 1 | Mutual exclusivity not enforced | P0-HIGH | Small | L56-65 |
| 2 | --all output path not validated | P0-HIGH | Small | L86-92 |
| 3 | Success messages pollute stdout | P0-HIGH | Small | L109, L154 |
| 4 | --output help text misleading | P2-MEDIUM | Small | L33-36 |
| 5 | Case-sensitive command names | P2-MEDIUM | Small | L67-72 |
| 6 | Style: No custom exceptions | P2-MEDIUM | Medium | L76-80 |
| 7 | Undocumented default behavior | P3-LOW | Small | L61-65 |

**SchemaCommand-Specific Effort:** 2-3 hours

---

## Aggregated Issue Count by Severity

### P0-CRITICAL (Must Fix Before Production)
| # | Issue | Commands Affected | Total Effort |
|---|-------|-------------------|--------------|
| 1 | Stdout purity violations | 5/5 | 1-2h |
| 2 | Async handler anti-pattern | 4/5 | 1-2h |
| 3 | Temp directory cleanup missing | 3/5 | 1-2h |
| 4 | Ad-hoc DI configuration | 2/5 | 1-2h |
| 5 | Unused --no-cache option | 1/5 | 15min |
| 6 | Mutual exclusivity (SchemaCommand) | 1/5 | 15min |
| 7 | Output path validation (SchemaCommand) | 1/5 | 15min |

**Total P0 Issues:** 17 instances across 7 issue types  
**P0 Estimated Effort:** 6-10 hours

---

### P1-HIGH (Fix Next Sprint)
| # | Issue | Commands Affected | Total Effort |
|---|-------|-------------------|--------------|
| 1 | Missing cancellation support | 4/5 | 1-2h |
| 2 | DI lifetime misuse | 2/5 | 30min-1h |
| 3 | Non-standard logging provider | 4/5 | 30min |
| 4 | Weak TFM selection logic | 3/5 | 1-2h |
| 5 | Exit code standardization | 2/5 | 1h |
| 6 | Schema conformance (DiffCommand) | 1/5 | 30min |
| 7 | Format validation (ExportSignatures) | 1/5 | 15min |
| 8 | Credential logging risk (FindCommand) | 1/5 | 20min |

**Total P1 Issues:** 13 instances across 8 issue types  
**P1 Estimated Effort:** 4-7 hours

---

### P2-MEDIUM / P3-LOW (Technical Debt)
| # | Issue | Commands Affected | Total Effort |
|---|-------|-------------------|--------------|
| 1 | Non-typed logger creation | 4/5 | 30min |
| 2 | No ref/ assembly preference | 3/5 | 1h |
| 3 | Non-deterministic output | 2/5 | 30min |
| 4 | File output safety (FindCommand) | 1/5 | 30min |
| 5 | IConsole abstraction (FindCommand) | 1/5 | 20min |
| 6 | TFM logging (ExportSignatures) | 1/5 | 15min |
| 7 | Namespace filter docs (ExportSignatures) | 1/5 | 15min |
| 8 | Help text accuracy (SchemaCommand) | 1/5 | 15min |
| 9 | Case sensitivity (SchemaCommand) | 1/5 | 15min |

**Total P2/P3 Issues:** 8 issue types  
**P2/P3 Estimated Effort:** 3-4 hours

---

## Total Effort Estimate by Command

| Command | P0 Effort | P1 Effort | P2/P3 Effort | Total |
|---------|-----------|-----------|--------------|-------|
| FindCommand | 2-3h | 1-2h | 1h | 4-6h |
| ListTypesCommand | 1-2h | 1-2h | 30min | 3-5h |
| ExportSignaturesCommand | 1-2h | 1-2h | 1h | 3-5h |
| DiffCommand | 2-3h | 1-2h | 1h | 4-6h |
| SchemaCommand | 1-2h | - | 1h | 2-3h |
| **TOTAL** | **6-10h** | **4-7h** | **3-4h** | **13-21h** |

---

## Recommended Implementation Roadmap

### Phase 1: Critical Fixes (P0) - Week 1
**Goal:** Make commands production-ready for machine consumption  
**Effort:** 6-10 hours

1. **Stdout Purity** (All commands except SchemaCommand async handler)
   - Configure Console logger to stderr
   - Move success messages to stderr
   - E2E test: `| jq .` validation

2. **Async Handler Pattern** (All async commands)
   - Replace SetAction with SetHandler
   - Remove .GetAwaiter().GetResult()
   - Pass CancellationToken from context

3. **Temp Directory Cleanup** (ListTypes, ExportSignatures, Diff)
   - Add finally blocks
   - Track temp dirs in handler
   - Cleanup even on exception

4. **Centralize DI** (FindCommand, DiffCommand)
   - Move ServiceCollection to Program.cs
   - Require IServiceProvider in Create()
   - Remove CreateDefaultServiceProvider()

5. **SchemaCommand Fixes**
   - Enforce mutual exclusivity
   - Validate --all output path
   - Move success messages to stderr

**Acceptance:**
- ✅ All commands pass `| jq .` test
- ✅ No temp directories leak
- ✅ No async blocking
- ✅ All DI centralized

---

### Phase 2: Reliability & UX (P1) - Week 2
**Goal:** Improve error handling, cancellation, TFM selection  
**Effort:** 4-7 hours

1. **Cancellation Support**
   - Thread CancellationToken through all async calls
   - E2E tests simulate Ctrl+C
   - Verify cleanup on cancel

2. **Exit Code Standardization**
   - Define exit code constants
   - Map error types correctly
   - Add actionable error messages

3. **TFM Selection**
   - Integrate NuGet.Frameworks.FrameworkReducer
   - Add compatibility-based selection
   - List available TFMs on mismatch

4. **Logging Cleanup**
   - Remove AddFile or document in AGENTS.md
   - Use ILogger<T> everywhere
   - Fix DI lifetime (Singleton vs Scoped)

5. **Schema Validation**
   - E2E tests validate against embedded schemas
   - Add UnsafeRelaxedJsonEscaping where needed

**Acceptance:**
- ✅ Ctrl+C works correctly
- ✅ Error messages actionable
- ✅ TFM selection robust
- ✅ Schema validation passes

---

### Phase 3: Code Quality (P2/P3) - Week 3
**Goal:** Align with AGENTS.md code style  
**Effort:** 3-4 hours

1. **ref/ Assembly Preference**
2. **Deterministic Output Sorting**
3. **File Output Safety (Atomic Write)**
4. **IConsole Abstraction**
5. **Documentation Updates**

**Acceptance:**
- ✅ Code review passes AGENTS.md compliance
- ✅ All tests pass
- ✅ README updated with exit codes

---

## Testing Requirements

### E2E Test Coverage (Missing Tests)

| Command | Test | Priority | Description |
|---------|------|----------|-------------|
| FindCommand | Stdout purity | P0 | Pipe to jq validation |
| FindCommand | Cancellation | P1 | No partial files on Ctrl+C |
| FindCommand | Exit codes | P1 | Verify 0/1/2 mapping |
| ListTypesCommand | Temp cleanup | P0 | No leak even on error |
| ListTypesCommand | TFM fallback | P1 | netstandard on net8.0 works |
| ExportSignatures | Format validation | P1 | Invalid format returns error |
| ExportSignatures | JSONL line count | P1 | Each line valid JSON |
| DiffCommand | Schema validation | P1 | Output matches diff.schema.json |
| DiffCommand | TFM explicit | P1 | --tfm selection correct |
| SchemaCommand | Mutual exclusivity | P0 | --command --all returns error |
| SchemaCommand | --all with file | P0 | Error on file path |

**Total Missing E2E Tests:** ~20 scenarios across 5 commands

---

## Risk Assessment

### High-Risk Changes

| Change | Risk | Mitigation |
|--------|------|------------|
| Exit code changes | May break existing scripts | Document in CHANGELOG; bump version |
| TFM selection changes | Different assemblies selected | Add golden tests before/after |
| Logging changes | May lose debug info | Add --verbose flag; keep file logs optional |
| DI centralization | Breaking API change | Update all command registrations atomically |

### Medium-Risk Changes

| Change | Risk | Mitigation |
|--------|------|------------|
| Async handler refactor | System.CommandLine API differences | Pin version; test thoroughly |
| Temp cleanup in finally | Cleanup may fail on locked files | Log warning; retry logic |
| Schema validation | Breaking output format | Version schemas (models-2.0) |

---

## Cross-References

### Summary Reports
- [summary-report-1-FindCommand.md](file:///c:/dev/app/nuget-toolbox/reports/summary-report-1-FindCommand.md)
- [summary-report-2-ListTypesCommand.md](file:///c:/dev/app/nuget-toolbox/reports/summary-report-2-ListTypesCommand.md)
- [summary-report-3-ExportSignaturesCommand.md](file:///c:/dev/app/nuget-toolbox/reports/summary-report-3-ExportSignaturesCommand.md)
- [summary-report-4-DiffCommand.md](file:///c:/dev/app/nuget-toolbox/reports/summary-report-4-DiffCommand.md)
- [summary-report-5-SchemaCommand.md](file:///c:/dev/app/nuget-toolbox/reports/summary-report-5-SchemaCommand.md)

### Guidelines
- [AGENTS.md](file:///c:/dev/app/nuget-toolbox/AGENTS.md) - Project development guidelines
- [README.md](file:///c:/dev/app/nuget-toolbox/README.md) - Project documentation

---

## Conclusion

NuGet Toolbox has **solid core functionality** but requires **systematic fixes across all commands** to meet production standards. The issues fall into clear patterns:

**Architectural Issues (Systemic):**
- Stdout pollution
- Async anti-patterns
- Resource leaks
- Inconsistent DI

**Code Quality Issues (Widespread):**
- Non-standard logging
- Weak TFM selection
- Missing cancellation
- No exit code standardization

**Command-Specific Issues (Isolated):**
- SchemaCommand: Mutual exclusivity, path validation
- ExportSignaturesCommand: Dead code, format validation
- FindCommand: Security (credential logging), file safety

**Recommended Action:**
1. ✅ **Approve 3-week roadmap** (Phase 1→2→3)
2. ✅ **Create GitHub issues** for P0 items with acceptance criteria
3. ✅ **Implement fixes incrementally** with tests per phase
4. ✅ **Run full test suite** after each phase
5. ✅ **Update AGENTS.md** with lessons learned

**Bottom Line:** With focused effort following this roadmap, NuGet Toolbox will be **production-ready within 3 weeks** (13-21 hours of development time).
