# DiffCommand Comprehensive Summary Report

## Executive Summary

**Purpose**: Compare public API between two NuGet package versions and emit structured `DiffResult` JSON identifying breaking changes, additions, and removals.

**Verdict**: Command meets core functional requirements but has **8 high-priority issues** blocking production readiness, including critical stdout purity violations, missing resource cleanup, and non-standard async patterns. E2E tests exist but lack schema validation and cancellation coverage.

**Recommendation**: Address P0 issues (stdout purity, temp cleanup, async handler) before release. Schema validation and cancellation support required for P1. DI scoping and TFM improvements recommended for maintainability.

---

## Issues Matrix

| # | Issue | Severity | Impact | Effort | Status | Evidence |
|---|-------|----------|--------|--------|--------|----------|
| 1 | **Stdout Purity** | P0-CRITICAL | Breaks machine consumers | Small | ❌ Open | Lines 86, 88, 93, 109, 131, 197 |
| 2 | **Temp Directory Cleanup** | P0-CRITICAL | Disk bloat over time | Small | ❌ Open | Lines 167-168, 200 |
| 3 | **Exit Code Standardization** | P0 | Poor error UX | Small | ❌ Open | Lines 94, 101, 110, 146 |
| 4 | **Async Handler Pattern** | P1 | Deadlock risk | Small | ❌ Open | Lines 55-67 |
| 5 | **Schema Conformance** | P1 | Output contract unclear | Medium | ❌ Open | Lines 119-136 |
| 6 | **Cancellation Support** | P1 | Partial files on Ctrl+C | Small | ❌ Open | Lines 70-148, 155 |
| 7 | **DI Scoping** | P2 | Lifetime misuse | Small | ❌ Open | Lines 80-84 |
| 8 | **TFM Selection Logic** | P2 | Incorrect framework picks | Medium | ❌ Open | Lines 157-159 |
| 9 | **Asset Selection (ref vs lib)** | P2 | Noisier API surface | Medium | ❌ Open | Lines 155-165 |
| 10 | **Resource Disposal** | P2 | File handle leaks | Small | ⚠️ Partial | Lines 154, 175-178, 190-193 |
| 11 | **Deterministic Ordering** | P3 | Non-reproducible outputs | Small | ❌ Open | ApiDiffAnalyzer.cs:37-39 |
| 12 | **Logging Provider** | P3 | Non-standard dependency | Small | ⚠️ Known | Line 210, NuGetToolbox.Cli.csproj:12 |

---

## Issue Details with Evidence

### 1. Stdout Purity (P0 - CRITICAL)

**Problem**: Logs interleaved with JSON output break machine consumers and violate CLI best practices.

**Evidence**:
```csharp
// DiffCommand.cs:86
var logger = loggerFactory.CreateLogger("DiffCommand");

// Lines 88, 93, 109, 131, 197
logger.LogInformation("Comparing {PackageId} versions {From} -> {To}", ...);
logger.LogError("Package {PackageId} version {Version} not found", ...);
logger.LogWarning("No assemblies found in one or both package versions...");
logger.LogInformation("Diff result written to {OutputPath}", output);
logger.LogInformation("Extracted {Count} assemblies from {Tfm}", ...);

// Line 135 - stdout used for JSON
Console.WriteLine(json);
```

Current logging goes to file (`AddFile` at line 210) but also potentially to console, risking mixed output.

**Fix**:
```csharp
// Option A: Disable console logging when outputting to stdout
services.AddLogging(builder =>
{
    if (string.IsNullOrEmpty(output)) // stdout mode
    {
        builder.ClearProviders(); // logs to file only
    }
    builder.AddFile(logFile, minimumLevel: LogLevel.Debug);
});

// Option B: Add --verbose flag to control logging
```

**Acceptance**:
- ✅ E2E test validates stdout contains only valid JSON
- ✅ All logs appear in file or stderr only
- ✅ `dotnet run -- diff ... | jq .` succeeds

---

### 2. Temp Directory Cleanup (P0 - CRITICAL)

**Problem**: Extracted assemblies/XML files never deleted; each diff leaks ~1-10MB to disk.

**Evidence**:
```csharp
// DiffCommand.cs:167-168
var tempDir = Path.Combine(Path.GetTempPath(), $"nuget-toolbox-{Guid.NewGuid()}");
Directory.CreateDirectory(tempDir);
// ...extraction...
// Line 200 - method returns, tempDir never cleaned
return assemblies;
```

**Fix**:
```csharp
private static async Task<int> HandlerAsync(...)
{
    var tempDirs = new List<string>();
    try
    {
        // ...resolution and extraction...
        var fromAssemblies = await ExtractAssembliesAsync(..., tempDirs);
        var toAssemblies = await ExtractAssembliesAsync(..., tempDirs);
        // ...diff and output...
    }
    finally
    {
        foreach (var dir in tempDirs)
        {
            try { Directory.Delete(dir, recursive: true); }
            catch { /* log warning */ }
        }
    }
}

private static async Task<List<string>> ExtractAssembliesAsync(
    string nupkgPath, string? tfm, ILogger logger, List<string> tempDirs)
{
    var tempDir = Path.Combine(Path.GetTempPath(), $"nuget-toolbox-{Guid.NewGuid()}");
    tempDirs.Add(tempDir);
    // ...
}
```

**Acceptance**:
- ✅ No temp directories remain after successful diff
- ✅ Cleanup occurs even on exception
- ✅ Warning logged if cleanup fails (file in use)

---

### 3. Exit Code Standardization (P0)

**Problem**: All errors return exit code 1; user cannot distinguish failure modes.

**Evidence**:
```csharp
// DiffCommand.cs:94, 101, 110
return 1; // package not found
return 1; // no assemblies found
return 1; // generic exception
```

**Fix**:
```csharp
// Exit codes:
// 0: Success
// 1: Package/version not found
// 2: TFM not found / no assemblies
// 3: Comparison failed
// 4: Unexpected error

// Actionable error messages:
Console.Error.WriteLine(
    $"Package '{packageId}' version '{fromVersion}' not found. " +
    $"Use 'dotnet run -- find --package {packageId}' to see available versions."
);

Console.Error.WriteLine(
    $"No assemblies found for TFM '{tfm}'. " +
    $"Available TFMs: {string.Join(", ", fromPackage.Tfms)}"
);
```

**Acceptance**:
- ✅ Exit codes documented in README
- ✅ Error messages include next steps
- ✅ E2E tests validate error codes

---

### 4. Async Handler Pattern (P1)

**Problem**: Synchronous wrapper blocks async work; violates AGENTS.md ("no .Result/.Wait()").

**Evidence**:
```csharp
// DiffCommand.cs:55-67
command.SetAction(Handler);

int Handler(ParseResult parseResult)
{
    // ...
    return HandlerAsync(...).GetAwaiter().GetResult(); // ❌ BLOCKING
}
```

**Fix**:
```csharp
command.SetHandler(
    async (string package, string from, string to, string? tfm, string? output) =>
    {
        var exitCode = await HandlerAsync(package, from, to, tfm, output, serviceProvider);
        Environment.ExitCode = exitCode;
    },
    packageOption, fromOption, toOption, tfmOption, outputOption
);
```

**Acceptance**:
- ✅ No blocking async calls
- ✅ CancellationToken passed to async operations
- ✅ E2E tests pass

---

### 5. Schema Conformance (P1)

**Problem**: Report claims "likely compliant" without validation; output may drift from schema.

**Evidence**:
```csharp
// DiffCommand.cs:119-126
var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};
// Missing: Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
```

Schema at `Schemas/diff.schema.json` defines contract but no validation test exists.

**Fix**:
```csharp
// Add to E2E test:
var schemaJson = await File.ReadAllTextAsync("Schemas/diff.schema.json");
// Use NJsonSchema or similar to validate output against schema

// Add UnsafeRelaxedJsonEscaping if generic signatures need unescaped brackets
Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
```

**Acceptance**:
- ✅ E2E test validates output against embedded schema
- ✅ Sample output snippet in README
- ✅ Schema version tracked in output (future)

---

### 6. Cancellation Support (P1)

**Problem**: Ctrl+C during diff may leave partial files or uncleaned temps.

**Evidence**:
```csharp
// DiffCommand.cs:155
await packageReader.GetLibItemsAsync(CancellationToken.None); // ❌ Not cancellable

// Line 70 - HandlerAsync signature lacks CancellationToken
private static async Task<int> HandlerAsync(..., IServiceProvider? serviceProvider)

// Lines 90, 97 - resolver calls not cancellable
var fromPackage = await resolver.ResolvePackageAsync(packageId, fromVersion);
```

**Fix**:
```csharp
private static async Task<int> HandlerAsync(
    ..., 
    IServiceProvider? serviceProvider,
    CancellationToken cancellationToken = default)
{
    // Pass to all async calls
    await resolver.ResolvePackageAsync(..., cancellationToken);
    await packageReader.GetLibItemsAsync(cancellationToken);
}

// System.CommandLine provides cancellation token automatically
command.SetHandler(async (InvocationContext context) =>
{
    var ct = context.GetCancellationToken();
    await HandlerAsync(..., serviceProvider, ct);
});
```

**Acceptance**:
- ✅ Ctrl+C produces non-zero exit
- ✅ No partial files left
- ✅ Temp cleanup occurs on cancellation
- ✅ E2E test simulates cancellation

---

### 7. DI Scoping (P2)

**Problem**: Scoped services resolved from root provider without scope; may cause leaks.

**Evidence**:
```csharp
// DiffCommand.cs:80-84
serviceProvider ??= CreateDefaultServiceProvider();

var resolver = serviceProvider.GetRequiredService<NuGetPackageResolver>();
var exporter = serviceProvider.GetRequiredService<SignatureExporter>();
var analyzer = serviceProvider.GetRequiredService<ApiDiffAnalyzer>();
// All registered as Scoped (lines 212-216) but no scope created
```

**Fix**:
```csharp
using var scope = serviceProvider.CreateScope();
var scopedProvider = scope.ServiceProvider;

var resolver = scopedProvider.GetRequiredService<NuGetPackageResolver>();
var exporter = scopedProvider.GetRequiredService<SignatureExporter>();
var analyzer = scopedProvider.GetRequiredService<ApiDiffAnalyzer>();
```

**Acceptance**:
- ✅ Scoped services disposed after command
- ✅ No memory leaks in repeated calls

---

### 8. TFM Selection Logic (P2)

**Problem**: Simplistic version comparison fails across disparate frameworks (netstandard2.0 vs net8.0).

**Evidence**:
```csharp
// DiffCommand.cs:157-159
var targetGroup = string.IsNullOrEmpty(tfm)
    ? libItems.OrderByDescending(g => g.TargetFramework.Version).FirstOrDefault()
    : libItems.FirstOrDefault(g => g.TargetFramework.GetShortFolderName() == tfm);
```

This picks highest `Version` numerically, which doesn't account for framework compatibility (e.g., `netstandard2.0` may be more compatible than `net4.8` for net8.0 consumers).

**Fix**:
```csharp
using NuGet.Frameworks;

var reducer = new FrameworkReducer();
var currentFramework = NuGetFramework.Parse("net8.0"); // or from args
var compatible = libItems
    .Where(g => reducer.IsCompatible(currentFramework, g.TargetFramework))
    .ToList();

var targetGroup = string.IsNullOrEmpty(tfm)
    ? compatible.OrderBy(g => reducer.GetNearest(currentFramework, 
        compatible.Select(c => c.TargetFramework))).FirstOrDefault()
    : libItems.FirstOrDefault(g => g.TargetFramework.GetShortFolderName() == tfm);
```

**Acceptance**:
- ✅ E2E test with multi-target package validates correct TFM selection
- ✅ Error lists available TFMs on mismatch

---

### 9. Asset Selection (ref vs lib) (P2)

**Problem**: Uses `lib/` assemblies only; `ref/` assemblies (when present) provide cleaner public API.

**Evidence**:
```csharp
// DiffCommand.cs:155
var libItems = await packageReader.GetLibItemsAsync(CancellationToken.None);
// No check for GetReferenceItemsAsync()
```

**Fix**:
```csharp
var refItems = (await packageReader.GetReferenceItemsAsync(cancellationToken)).ToList();
var libItems = (await packageReader.GetLibItemsAsync(cancellationToken)).ToList();

// Prefer ref, fallback to lib
var targetGroup = (refItems.Any() ? refItems : libItems)
    .FirstOrDefault(...);
```

**Acceptance**:
- ✅ Prefers ref/ when available
- ✅ Falls back to lib/ gracefully
- ✅ Test with package containing both (e.g., System.Text.Json)

---

### 10. Resource Disposal (P2)

**Problem**: `PackageArchiveReader` disposed, but file streams may not be in error paths.

**Evidence**:
```csharp
// DiffCommand.cs:154 - ✅ GOOD
using var packageReader = new PackageArchiveReader(nupkgPath);

// Lines 175-178, 190-193 - ✅ GOOD
using (var stream = packageReader.GetStream(item))
using (var fileStream = File.Create(destPath))
{
    await stream.CopyToAsync(fileStream);
}
```

**Status**: ⚠️ **PARTIAL** - mostly correct but verify MetadataLoadContext disposal in AssemblyInspector.

**Acceptance**:
- ✅ All disposables used in try/finally or using
- ✅ Temp cleanup succeeds even if handles open

---

### 11. Deterministic Ordering (P3)

**Problem**: Lists not sorted; output varies between runs, complicating diffs and tests.

**Evidence**:
```csharp
// ApiDiffAnalyzer.cs:37-39
var removed = new List<DiffItem>();
var added = new List<TypeInfo>();
var breaking = new List<DiffItem>();
// No sorting before return (line 80-95)
```

**Fix**:
```csharp
return new DiffResult
{
    // ...
    Breaking = breaking.Count > 0 
        ? breaking.OrderBy(b => b.Type).ThenBy(b => b.Signature).ToList() 
        : null,
    Added = added.Count > 0 
        ? added.OrderBy(a => $"{a.Namespace}.{a.Name}").ToList() 
        : null,
    // ...
};
```

**Acceptance**:
- ✅ Repeated runs produce identical output
- ✅ Tests assert array order

---

### 12. Logging Provider (P3)

**Problem**: `AddFile` extension from `Serilog.Extensions.Logging.File` (non-standard).

**Evidence**:
```csharp
// DiffCommand.cs:210
builder.AddFile(logFile, minimumLevel: LogLevel.Debug);

// NuGetToolbox.Cli.csproj:12
<PackageReference Include="Serilog.Extensions.Logging.File" Version="3.0.0" />
```

**Status**: ⚠️ **KNOWN** - dependency declared but not mentioned in AGENTS.md.

**Fix**: Document in AGENTS.md or replace with standard console logger to stderr:
```csharp
builder.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
```

**Acceptance**:
- ✅ Logging dependency documented
- ✅ Fallback to console if file logging unavailable

---

## Prioritized Action Plan

### Phase 1: P0 (Required for Release)
1. **Stdout Purity** (30 min)
   - Clear console provider when `output == null`
   - E2E test validates stdout is valid JSON

2. **Temp Cleanup** (20 min)
   - Add finally block with cleanup
   - Track temp dirs in list passed to ExtractAssembliesAsync

3. **Exit Codes** (20 min)
   - Define 0/1/2/3/4 codes with meanings
   - Add actionable error messages to stderr

**Total**: ~70 min

### Phase 2: P1 (Reliability & UX)
4. **Async Handler** (15 min)
   - Replace SetAction with SetHandler async lambda

5. **Schema Validation** (30 min)
   - Add E2E test with schema validation
   - Add UnsafeRelaxedJsonEscaping if needed

6. **Cancellation** (20 min)
   - Pass CancellationToken through call chain
   - E2E test with simulated cancellation

**Total**: ~65 min

### Phase 3: P2 (Code Quality)
7. **DI Scoping** (10 min)
8. **TFM Selection** (40 min - with FrameworkReducer)
9. **Asset Selection** (20 min - ref/ preference)
10. **Resource Disposal** (15 min - verify)
11. **Deterministic Ordering** (15 min)

**Total**: ~100 min

### Phase 4: P3 (Polish)
12. **Logging Provider** (10 min - document)

**Total**: ~10 min

**Overall Estimate**: Small-Medium (4-5 hours total)

---

## Test Coverage Requirements

### E2E Tests Needed

| Test | Priority | Description | Validates |
|------|----------|-------------|-----------|
| ✅ Happy path | P0 | 13.0.1 → 13.0.3 | Basic flow, JSON structure |
| ✅ Same version | P0 | 13.0.1 → 13.0.1 | Empty changes, compatible=true |
| ❌ Stdout purity | P0 | Pipe to jq | JSON-only output |
| ❌ Schema validation | P1 | Deserialize + validate | Contract conformance |
| ❌ Cancellation | P1 | Cancel mid-run | Graceful exit, cleanup |
| ❌ TFM explicit | P1 | --tfm net8.0 | TFM selection |
| ❌ TFM not found | P1 | --tfm fake | Error lists available TFMs |
| ❌ Breaking changes | P2 | Known breaking pair | breaking[] populated |
| ❌ Temp cleanup | P2 | Verify no leaks | Temp dirs deleted |

---

## Cross-References

### Related Reports
- [report-4-DiffCommand.md](file:///c:/dev/app/nuget-toolbox/reports/report-4-DiffCommand.md) - Initial analysis
- [critique-report-4-DiffCommand.md](file:///c:/dev/app/nuget-toolbox/reports/critique-report-4-DiffCommand.md) - Critique identifying gaps

### Code Files
- [DiffCommand.cs](file:///c:/dev/app/nuget-toolbox/src/NuGetToolbox.Cli/Commands/DiffCommand.cs#L1-L219)
- [ApiDiffAnalyzer.cs](file:///c:/dev/app/nuget-toolbox/src/NuGetToolbox.Cli/Services/ApiDiffAnalyzer.cs#L1-L159)
- [DiffResult.cs](file:///c:/dev/app/nuget-toolbox/src/NuGetToolbox.Cli/Models/DiffResult.cs#L1-L51)
- [diff.schema.json](file:///c:/dev/app/nuget-toolbox/src/NuGetToolbox.Cli/Schemas/diff.schema.json#L1-L54)
- [DiffCommandE2ETests.cs](file:///c:/dev/app/nuget-toolbox/tests/NuGetToolbox.Tests/DiffCommandE2ETests.cs#L1-L112)

### AGENTS.md Traceability

| Issue | AGENTS.md Section | Rule Violated |
|-------|-------------------|---------------|
| Async blocking | Code Style → Async | "no .Result/.Wait()" |
| Logging | Code Style → Logging | "Use ILogger<T>" |
| Exit codes | Code Style → Errors | "Custom exceptions with actionable messages" |
| Cancellation | Code Style → Async | "async Task for I/O" |
| DI scoping | Best Practices | Scoped service lifetime |

---

## Implementation Guardrails

1. **Stdout Changes**: Run all E2E tests; add `| jq .` validation
2. **TFM Changes**: Golden test with Newtonsoft.Json before/after
3. **Temp Cleanup**: Log warning if cleanup fails (file in use)
4. **Exit Code Changes**: Document in README/CHANGELOG
5. **Schema Updates**: Increment schema version (models-2.0)
6. **Cancellation**: Ensure finally blocks run on CancellationToken

---

## Advanced Path Triggers

Implement advanced features when:
- **TFM issues**: >10% of diffs select wrong TFM (tracked via logs)
- **Performance**: Diff time >30s for packages with >1000 types
- **Caching**: Same package diffed >5x/day (usage metrics)

**Advanced Implementation**:
- Asset resolution: FrameworkReducer.GetNearest with compatibility matrix
- Caching: Store in `%TEMP%/nuget-toolbox/cache/{id}/{version}/{tfm}/`
- Parallelization: Process assemblies with SemaphoreSlim(ProcessorCount)

---

## Summary

**Strengths**:
- ✅ Core functionality implemented correctly
- ✅ Service integration follows architecture
- ✅ Basic E2E tests exist
- ✅ Schema defined and embedded

**Critical Gaps**:
- ❌ Stdout purity breaks CLI consumers
- ❌ Temp cleanup leaks disk space
- ❌ Exit codes not actionable
- ❌ Async pattern violates guidelines
- ❌ Schema validation missing
- ❌ Cancellation not supported

**Bottom Line**: With 4-5 hours of focused work addressing P0/P1 issues, DiffCommand will meet production-ready standards for CLI tools. Current implementation is ~60% complete.
