# ExportSignaturesCommand - Comprehensive Summary Report

## Executive Summary

**Overall Status**: Functionally complete with 7 high-priority and 4 medium-priority issues requiring fixes.

The ExportSignaturesCommand resolves NuGet packages, extracts assemblies with XML docs, and exports public method signatures as JSON/JSONL with optional namespace filtering. Core functionality aligns with specifications, but implementation has critical code quality and reliability issues that violate project standards.

**Key Strengths:**
- ✅ JSON escaping properly configured (UnsafeRelaxedJsonEscaping)
- ✅ Proper service dependency injection
- ✅ Comprehensive error handling structure
- ✅ Generic logger usage (ILogger<SignatureExporter>)
- ✅ Proper async patterns in services

**Critical Issues:**
- ❌ Blocking async handler pattern (.GetAwaiter().GetResult)
- ❌ Unused --no-cache option (dead code)
- ❌ No temp directory cleanup (resource leak)
- ❌ Missing format validation (uses FromAmong pattern incorrectly)
- ❌ Non-generic logger in command handler
- ❌ Weak TFM selection (simple max Version)
- ❌ TFM selection not logged for transparency

---

## Detailed Issues with Evidence

### 1. Blocking Async Handler Pattern (CRITICAL)

**Severity**: HIGH | **Effort**: SMALL | **Priority**: MUST FIX

**Evidence**: [ExportSignaturesCommand.cs:68-79](file:///c:/dev/app/nuget-toolbox/src/NuGetToolbox.Cli/Commands/ExportSignaturesCommand.cs#L68-L79)
```csharp
int Handler(ParseResult parseResult)
{
    var package = parseResult.GetValue(packageOption);
    var version = parseResult.GetValue(versionOption);
    var tfm = parseResult.GetValue(tfmOption);
    var format = parseResult.GetValue(formatOption) ?? "json";
    var filter = parseResult.GetValue(filterOption);
    var output = parseResult.GetValue(outputOption);
    var noCache = parseResult.GetValue(noCacheOption);

    return HandlerAsync(package!, version, tfm, format, filter, output, serviceProvider).GetAwaiter().GetResult();
}
```

**Impact**: Violates AGENTS.md guideline "Async: `async Task` for I/O; no `.Result` or `.Wait()`". Risks deadlocks and thread pool starvation.

**Fix**:
```csharp
command.SetHandler(async (InvocationContext ctx) =>
{
    var parse = ctx.ParseResult;
    return await HandlerAsync(
        parse.GetValue(packageOption)!,
        parse.GetValue(versionOption),
        parse.GetValue(tfmOption),
        parse.GetValue(formatOption) ?? "json",
        parse.GetValue(filterOption),
        parse.GetValue(outputOption),
        serviceProvider);
});
```

**Cross-references**: Same issue identified in FindCommand and ListTypesCommand reports.

---

### 2. Unused --no-cache Option (HIGH)

**Severity**: HIGH | **Effort**: SMALL | **Priority**: MUST FIX

**Evidence**: [ExportSignaturesCommand.cs:48-52](file:///c:/dev/app/nuget-toolbox/src/NuGetToolbox.Cli/Commands/ExportSignaturesCommand.cs#L48-L52)
```csharp
var noCacheOption = new Option<bool>("--no-cache")
{
    Description = "Bypass cache",
    DefaultValueFactory = _ => false
};
```

**Evidence**: [ExportSignaturesCommand.cs:76](file:///c:/dev/app/nuget-toolbox/src/NuGetToolbox.Cli/Commands/ExportSignaturesCommand.cs#L76)
```csharp
var noCache = parseResult.GetValue(noCacheOption);  // Read but never used
```

**Evidence**: [NuGetPackageResolver.cs:30-34](file:///c:/dev/app/nuget-toolbox/src/NuGetToolbox.Cli/Services/NuGetPackageResolver.cs#L30-L34)
```csharp
public async Task<PackageInfo> ResolvePackageAsync(
    string packageId,
    string? version = null,
    string? feedUrl = null,
    CancellationToken cancellationToken = default)
    // No bypassCache or noCache parameter exists
```

**Impact**: Confusing CLI surface; option has no effect, misleading users.

**Fix**: Remove option (YAGNI principle) or implement caching infrastructure:
```csharp
// Option 1: Remove the option entirely
// Delete lines 48-52 and 76

// Option 2: Wire to resolver (requires resolver changes)
await resolver.ResolvePackageAsync(packageId, version, bypassCache: noCache);
```

**Cross-references**: Consistent with FindCommand report findings; NuGetPackageResolver does not support cache bypass.

---

### 3. Temp Directory Not Cleaned Up (HIGH)

**Severity**: HIGH | **Effort**: SMALL | **Priority**: MUST FIX

**Evidence**: [ExportSignaturesCommand.cs:163-164](file:///c:/dev/app/nuget-toolbox/src/NuGetToolbox.Cli/Commands/ExportSignaturesCommand.cs#L163-L164)
```csharp
var tempDir = Path.Combine(Path.GetTempPath(), $"nuget-toolbox-{Guid.NewGuid()}");
Directory.CreateDirectory(tempDir);
```

**Evidence**: No cleanup code in ExtractAssembliesAsync or HandlerAsync - temp directory returned but never deleted.

**Impact**: Resource leak; repeated runs accumulate temp directories in %TEMP%, potentially filling disk.

**Fix**:
```csharp
private static async Task<int> HandlerAsync(...)
{
    string? tempDir = null;
    try
    {
        // ... existing code ...
        var assemblyPaths = await ExtractAssembliesAsync(packageInfo.NupkgPath, tfm, logger, out tempDir);
        
        var methods = exporter.ExportMethods(assemblyPaths, namespaceFilter);
        // ... export logic ...
        return 0;
    }
    finally
    {
        if (!string.IsNullOrEmpty(tempDir) && Directory.Exists(tempDir))
        {
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to clean up temp directory {TempDir}", tempDir);
            }
        }
    }
}

private static async Task<List<string>> ExtractAssembliesAsync(
    string nupkgPath, string? tfm, ILogger logger, out string tempDir)
{
    // ... existing code that sets tempDir ...
}
```

**Cross-references**: Same pattern issue in ListTypesCommand.

---

### 4. Missing Format Validation (HIGH)

**Severity**: HIGH | **Effort**: SMALL | **Priority**: MUST FIX

**Evidence**: [ExportSignaturesCommand.cs:32-36](file:///c:/dev/app/nuget-toolbox/src/NuGetToolbox.Cli/Commands/ExportSignaturesCommand.cs#L32-L36)
```csharp
var formatOption = new Option<string>("--format")
{
    Description = "Output format: json or jsonl",
    DefaultValueFactory = _ => "json"
};
```

**Evidence**: [ExportSignaturesCommand.cs:120-122](file:///c:/dev/app/nuget-toolbox/src/NuGetToolbox.Cli/Commands/ExportSignaturesCommand.cs#L120-L122)
```csharp
var result = format.ToLowerInvariant() == "jsonl"
    ? exporter.ExportToJsonL(methods)
    : exporter.ExportToJson(methods);
```

**Impact**: Any invalid value (e.g., `--format xml`) silently falls back to JSON. No validation or error message.

**Fix** (System.CommandLine idiomatic approach):
```csharp
var formatOption = new Option<string>("--format", () => "json")
{
    Description = "Output format: json or jsonl"
}.FromAmong("json", "jsonl");
```

This provides built-in validation and generates help text automatically.

**Cross-references**: Critique report identifies FromAmong as idiomatic pattern; missing in current implementation.

---

### 5. Non-Generic Logger in Command Handler (MEDIUM)

**Severity**: MEDIUM | **Effort**: SMALL | **Priority**: SHOULD FIX

**Evidence**: [ExportSignaturesCommand.cs:98](file:///c:/dev/app/nuget-toolbox/src/NuGetToolbox.Cli/Commands/ExportSignaturesCommand.cs#L98)
```csharp
var logger = loggerFactory.CreateLogger("ExportSignaturesCommand");  // String-based category
```

**Evidence**: [SignatureExporter.cs:16](file:///c:/dev/app/nuget-toolbox/src/NuGetToolbox.Cli/Services/SignatureExporter.cs#L16)
```csharp
private readonly ILogger<SignatureExporter> _logger;  // Generic logger (correct)
```

**Impact**: Violates AGENTS.md code style: "Logging: Use `ILogger<T>` with structured context". Inconsistent with service layer.

**Fix**:
```csharp
var logger = loggerFactory.CreateLogger<ExportSignaturesCommand>();
```

**Cross-references**: SignatureExporter correctly uses `ILogger<SignatureExporter>`; command should follow same pattern.

---

### 6. Weak TFM Selection Logic (MEDIUM)

**Severity**: MEDIUM | **Effort**: MEDIUM | **Priority**: SHOULD FIX

**Evidence**: [ExportSignaturesCommand.cs:153-155](file:///c:/dev/app/nuget-toolbox/src/NuGetToolbox.Cli/Commands/ExportSignaturesCommand.cs#L153-L155)
```csharp
var targetGroup = string.IsNullOrEmpty(tfm)
    ? libItems.OrderByDescending(g => g.TargetFramework.Version).FirstOrDefault()
    : libItems.FirstOrDefault(g => g.TargetFramework.GetShortFolderName() == tfm);
```

**Impact**: Simplistic max Version selection fails across framework families (e.g., prefers net462 over netstandard2.0, even when netstandard is more compatible).

**Fix** (if NuGet.Frameworks dependency exists):
```csharp
using NuGet.Frameworks;

var targetGroup = string.IsNullOrEmpty(tfm)
    ? NuGetFrameworkUtility.GetNearest(
        libItems.Select(g => g.TargetFramework),
        NuGetFramework.Parse("net8.0"))  // Or current runtime TFM
    : libItems.FirstOrDefault(g => g.TargetFramework.GetShortFolderName() == tfm);
```

**Fallback** (if dependency not available):
```csharp
// Prefer netX.0 > netstandard > netfx
var targetGroup = string.IsNullOrEmpty(tfm)
    ? libItems
        .OrderByDescending(g => g.TargetFramework.Framework.StartsWith("net") && !g.TargetFramework.Framework.StartsWith("netstandard"))
        .ThenByDescending(g => g.TargetFramework.Version)
        .FirstOrDefault()
    : libItems.FirstOrDefault(g => g.TargetFramework.GetShortFolderName() == tfm);
```

**Cross-references**: Testing strategy emphasizes "TFM selection" validation; current logic insufficient for complex packages.

---

### 7. TFM Selection Not Logged (MEDIUM)

**Severity**: MEDIUM | **Effort**: SMALL | **Priority**: SHOULD FIX

**Evidence**: [ExportSignaturesCommand.cs:193](file:///c:/dev/app/nuget-toolbox/src/NuGetToolbox.Cli/Commands/ExportSignaturesCommand.cs#L193)
```csharp
logger.LogInformation("Extracted {Count} assemblies from {Tfm}", assemblies.Count, targetGroup.TargetFramework.GetShortFolderName());
```

This logs AFTER selection, but doesn't clearly indicate when auto-selection occurs vs user-specified TFM.

**Impact**: When --tfm is omitted, users have no visibility into which TFM was auto-selected until after extraction. Reduces transparency and debugging capability.

**Fix**:
```csharp
if (targetGroup == null)
{
    logger.LogWarning("No lib items found for TFM {Tfm}", tfm ?? "any");
    return assemblies;
}

var selectedTfm = targetGroup.TargetFramework.GetShortFolderName();
if (string.IsNullOrEmpty(tfm))
{
    logger.LogInformation("Auto-selected TFM: {Tfm} (from {Count} available)", 
        selectedTfm, libItems.Count());
}
else
{
    logger.LogInformation("Using specified TFM: {Tfm}", selectedTfm);
}
```

**Cross-references**: Critique report identifies this as key transparency gap.

---

### 8. Imports Not Alphabetically Sorted (LOW)

**Severity**: LOW | **Effort**: SMALL | **Priority**: NICE TO HAVE

**Evidence**: [ExportSignaturesCommand.cs:1-5](file:///c:/dev/app/nuget-toolbox/src/NuGetToolbox.Cli/Commands/ExportSignaturesCommand.cs#L1-L5)
```csharp
using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NuGet.Packaging;
using NuGetToolbox.Cli.Services;
```

**Impact**: Violates AGENTS.md code style: "Imports: Alphabetically sorted". Minor style issue.

**Fix**: Run `dotnet format` or manually reorder:
```csharp
using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NuGet.Packaging;
using NuGetToolbox.Cli.Services;
```

Actually, the current order IS alphabetically sorted. **This issue is INVALID** - no fix needed.

---

### 9. ✅ JSON Escaping Correctly Configured (VERIFIED)

**Evidence**: [SignatureExporter.cs:114-119](file:///c:/dev/app/nuget-toolbox/src/NuGetToolbox.Cli/Services/SignatureExporter.cs#L114-L119)
```csharp
var options = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping  // ✅ Correct
};
```

**Evidence**: [SignatureExporter.cs:129-134](file:///c:/dev/app/nuget-toolbox/src/NuGetToolbox.Cli/Services/SignatureExporter.cs#L129-L134)
```csharp
var options = new JsonSerializerOptions
{
    WriteIndented = false,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping  // ✅ Correct
};
```

**Status**: ✅ COMPLIANT - Generic types like `List<T>` will render with unescaped angle brackets as required.

**Cross-references**: Critique report raised this as unverified claim; now confirmed correct.

---

### 10. ✅ Resource Disposal Correctly Implemented (VERIFIED)

**Evidence**: [ExportSignaturesCommand.cs:150](file:///c:/dev/app/nuget-toolbox/src/NuGetToolbox.Cli/Commands/ExportSignaturesCommand.cs#L150)
```csharp
using var packageReader = new PackageArchiveReader(nupkgPath);
```

**Evidence**: [ExportSignaturesCommand.cs:171-173](file:///c:/dev/app/nuget-toolbox/src/NuGetToolbox.Cli/Commands/ExportSignaturesCommand.cs#L171-L173)
```csharp
using (var stream = packageReader.GetStream(item))
using (var fileStream = File.Create(destPath))
{
    await stream.CopyToAsync(fileStream);
}
```

**Status**: ✅ COMPLIANT - Proper using statements for all IDisposable resources.

**Note**: Temp cleanup still required (Issue #3), but resource disposal pattern is correct.

---

### 11. Namespace Filter Semantics Undocumented (MEDIUM)

**Severity**: MEDIUM | **Effort**: SMALL | **Priority**: SHOULD FIX

**Evidence**: [ExportSignaturesCommand.cs:38-41](file:///c:/dev/app/nuget-toolbox/src/NuGetToolbox.Cli/Commands/ExportSignaturesCommand.cs#L38-L41)
```csharp
var filterOption = new Option<string?>("--filter", new[] { "--namespace" })
{
    Description = "Namespace filter (e.g., Newtonsoft.Json.Linq)"
};
```

**Evidence**: [SignatureExporter.cs:80-84](file:///c:/dev/app/nuget-toolbox/src/NuGetToolbox.Cli/Services/SignatureExporter.cs#L80-L84)
```csharp
if (namespaceFilter != null)
{
    visibleTypes = visibleTypes.Where(t =>
        t.Namespace != null && t.Namespace.StartsWith(namespaceFilter, StringComparison.Ordinal));
}
```

**Impact**: Behavior is prefix matching, case-sensitive, but not documented. Users may expect exact match or case-insensitive behavior.

**Fix**:
```csharp
var filterOption = new Option<string?>("--filter", new[] { "--namespace" })
{
    Description = "Namespace prefix filter (case-sensitive, e.g., Newtonsoft.Json.Linq matches Newtonsoft.Json.Linq.*)"
};
```

**Cross-references**: Critique report identifies semantic ambiguity; implementation uses StartsWith prefix matching.

---

## Priority Matrix

| # | Issue | Severity | Effort | Priority | Acceptance Criteria |
|---|-------|----------|--------|----------|---------------------|
| 1 | Blocking async handler | HIGH | S | MUST FIX | Handler returns Task; no GetAwaiter().GetResult() |
| 2 | Unused --no-cache | HIGH | S | MUST FIX | Option removed or wired to resolver |
| 3 | Temp cleanup | HIGH | S | MUST FIX | Finally block deletes temp dir |
| 4 | Format validation | HIGH | S | MUST FIX | Uses FromAmong("json","jsonl") |
| 5 | Non-generic logger | MEDIUM | S | SHOULD FIX | CreateLogger\<ExportSignaturesCommand>() |
| 6 | TFM selection | MEDIUM | M | SHOULD FIX | Uses NuGetFrameworkUtility or priority heuristic |
| 7 | TFM logging | MEDIUM | S | SHOULD FIX | Logs auto-selected vs specified TFM clearly |
| 11 | Namespace filter docs | MEDIUM | S | SHOULD FIX | Description documents prefix + case sensitivity |
| 8 | ~~Imports sorting~~ | LOW | S | ~~INVALID~~ | Already sorted correctly |
| 9 | JSON escaping | - | - | ✅ VERIFIED | UnsafeRelaxedJsonEscaping configured |
| 10 | Resource disposal | - | - | ✅ VERIFIED | Using statements present |

**Effort Key**: S = Small (< 1h), M = Medium (1-3h), L = Large (> 3h)

---

## Recommended Fix Sequence

### Phase 1: Critical Fixes (Must Fix - 2-3 hours)
1. **Async handler** (30 min) - Prevents thread pool exhaustion
2. **Format validation** (15 min) - Improves UX and error handling
3. **Temp cleanup** (45 min) - Prevents resource leaks
4. **Remove --no-cache** (15 min) - Eliminates confusing dead code

### Phase 2: Quality Improvements (Should Fix - 1-2 hours)
5. **Generic logger** (10 min) - Code style compliance
6. **TFM logging** (20 min) - Transparency and debugging
7. **Namespace filter docs** (10 min) - User clarity
8. **TFM selection** (1 hour) - Robustness across framework families

---

## Testing Recommendations

### Must Add E2E Tests
Based on [AGENTS.md E2E conventions](file:///c:/dev/app/nuget-toolbox/AGENTS.md#L127-L136):

```csharp
[Fact]
public async Task ExportSignaturesCommand_NewtonsoftJson_ProducesValidJsonOutput()
{
    var result = await RunCliAsync("export-signatures", 
        "--package", "Newtonsoft.Json", 
        "--version", "13.0.1",
        "--format", "json");
    
    Assert.Equal(0, result.ExitCode);
    var methods = JsonSerializer.Deserialize<List<MethodInfo>>(result.Output);
    Assert.NotNull(methods);
    Assert.True(methods.Count > 0);
    Assert.Contains(methods, m => m.Type.StartsWith("Newtonsoft.Json"));
}

[Fact]
public async Task ExportSignaturesCommand_JsonLFormat_ProducesMultipleLines()
{
    var result = await RunCliAsync("export-signatures",
        "--package", "Newtonsoft.Json",
        "--version", "13.0.1", 
        "--format", "jsonl");
    
    var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    Assert.True(lines.Length > 10);
    
    // Each line is valid JSON
    foreach (var line in lines)
    {
        var method = JsonSerializer.Deserialize<MethodInfo>(line);
        Assert.NotNull(method);
    }
}

[Fact]
public async Task ExportSignaturesCommand_InvalidFormat_ReturnsError()
{
    var result = await RunCliAsync("export-signatures",
        "--package", "Newtonsoft.Json",
        "--version", "13.0.1",
        "--format", "xml");
    
    Assert.Equal(1, result.ExitCode);
    Assert.Contains("Unsupported format", result.Error);
}

[Fact]
public async Task ExportSignaturesCommand_NamespaceFilter_FiltersCorrectly()
{
    var result = await RunCliAsync("export-signatures",
        "--package", "Newtonsoft.Json",
        "--version", "13.0.1",
        "--filter", "Newtonsoft.Json.Linq");
    
    var methods = JsonSerializer.Deserialize<List<MethodInfo>>(result.Output);
    Assert.All(methods, m => Assert.StartsWith("Newtonsoft.Json.Linq", m.Type));
}
```

### Unit Test Coverage Needed
```csharp
[Theory]
[InlineData("json", true)]
[InlineData("jsonl", true)]
[InlineData("xml", false)]
[InlineData("", false)]
public void FormatOption_ValidatesCorrectly(string format, bool shouldBeValid)
{
    // Test FromAmong validation
}

[Fact]
public void NamespaceFilter_IsCaseSensitive()
{
    // Verify StringComparison.Ordinal behavior
}

[Fact]
public void TempDirectory_IsCleanedUpOnSuccess()
{
    // Verify finally block cleanup
}

[Fact]
public void TempDirectory_IsCleanedUpOnFailure()
{
    // Verify cleanup even on exceptions
}
```

---

## Schema Conformance Status

**Schema File**: [export-signatures.schema.json](file:///c:/dev/app/nuget-toolbox/src/NuGetToolbox.Cli/Schemas/export-signatures.schema.json)

**Current Status**: ✅ LIKELY COMPLIANT (based on code analysis)

**Evidence**:
- Output model: `List<MethodInfo>` with camelCase serialization
- Field mappings verified in SignatureExporter.CreateMethodInfo()
- JSON options set PropertyNamingPolicy.CamelCase

**Recommendation**: Add E2E test that deserializes output against schema to guarantee conformance:
```csharp
[Fact]
public async Task ExportSignaturesCommand_OutputMatchesSchema()
{
    var result = await RunCliAsync("export-signatures", "--package", "Newtonsoft.Json", "--version", "13.0.1");
    
    // Deserialize to schema-defined model
    var methods = JsonSerializer.Deserialize<List<MethodInfo>>(result.Output);
    
    // Validate structure
    Assert.All(methods, m => {
        Assert.NotNull(m.Type);
        Assert.NotNull(m.Method);
        Assert.NotNull(m.Signature);
        Assert.NotNull(m.Parameters);
        Assert.NotNull(m.ReturnType);
    });
}
```

---

## Risk Assessment

### High Risk Items
1. **Temp cleanup in finally block**
   - **Risk**: Deletion before all streams closed → access denied errors
   - **Mitigation**: Ensure PackageArchiveReader disposed before cleanup; add try/catch in finally
   
2. **TFM selection changes**
   - **Risk**: Different assemblies selected → breaking output changes
   - **Mitigation**: Add baseline E2E test before changes; version output if breaking

3. **Format validation with FromAmong**
   - **Risk**: System.CommandLine version compatibility
   - **Mitigation**: Verify FromAmong exists in current System.CommandLine version; fallback to manual validation if needed

### Medium Risk Items
1. **Async handler refactor**
   - **Risk**: SetHandler API differences across System.CommandLine versions
   - **Mitigation**: Test on actual CLI version; verify InvocationContext API

2. **Logger category change**
   - **Risk**: Log filtering rules may break
   - **Mitigation**: Update any log filtering configuration to use new category

---

## Advanced Path Considerations

### When to Implement
- Packages with > 5 TFMs show incorrect auto-selection frequently
- Output files regularly exceed 50MB causing memory pressure
- Users request ref/ assembly preference for accurate API surface

### Specific Improvements

#### 1. Streaming JSON/JSONL
**Threshold**: Output > 10MB or > 10,000 methods

**Implementation**:
```csharp
// JSONL streaming
public async Task ExportToJsonLStreamAsync(List<MethodInfo> methods, Stream outputStream)
{
    using var writer = new StreamWriter(outputStream, leaveOpen: true);
    var options = new JsonSerializerOptions { ... };
    
    foreach (var method in methods)
    {
        var json = JsonSerializer.Serialize(method, options);
        await writer.WriteLineAsync(json);
    }
}

// JSON streaming
public async Task ExportToJsonStreamAsync(List<MethodInfo> methods, Stream outputStream)
{
    using var writer = new Utf8JsonWriter(outputStream, new JsonWriterOptions { Indented = true });
    writer.WriteStartArray();
    
    foreach (var method in methods)
    {
        JsonSerializer.Serialize(writer, method, options);
    }
    
    writer.WriteEndArray();
}
```

#### 2. Ref Assembly Preference
**Value**: Represents exact public API surface without implementation details

**Implementation**:
```csharp
private static async Task<List<string>> ExtractAssembliesAsync(...)
{
    var refItems = await packageReader.GetRefItemsAsync(CancellationToken.None);
    var libItems = await packageReader.GetLibItemsAsync(CancellationToken.None);
    
    // Prefer ref/ over lib/ when available
    var items = refItems.Any() ? refItems : libItems;
    
    var targetGroup = SelectTargetFramework(items, tfm);
    // ... extraction logic
}
```

#### 3. File-Based Caching
**Key**: `{packageId}/{version}/{tfm}/signatures.json`

**Implementation**:
```csharp
private static async Task<List<MethodInfo>> GetOrExportMethodsAsync(
    string packageId, string version, string tfm, 
    SignatureExporter exporter, bool noCache)
{
    var cacheKey = $"{packageId}/{version}/{tfm}";
    var cachePath = Path.Combine(GetCacheDirectory(), cacheKey, "signatures.json");
    
    if (!noCache && File.Exists(cachePath))
    {
        var json = await File.ReadAllTextAsync(cachePath);
        return JsonSerializer.Deserialize<List<MethodInfo>>(json)!;
    }
    
    var methods = exporter.ExportMethods(...);
    
    Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
    await File.WriteAllTextAsync(cachePath, JsonSerializer.Serialize(methods));
    
    return methods;
}
```

---

## Cross-Command Patterns

### Consistent Issues Across Commands
1. **Blocking async handlers** - Found in FindCommand, ListTypesCommand, ExportSignaturesCommand
2. **Unused --no-cache** - Found in all commands
3. **Temp cleanup** - Found in ListTypesCommand, ExportSignaturesCommand
4. **TFM selection** - Found in ListTypesCommand, ExportSignaturesCommand

### Recommended Global Fixes
1. Create shared `AsyncCommandHandler` base class
2. Implement shared `TfmSelector` utility with robust logic
3. Create `TempDirectoryScope` IDisposable wrapper for automatic cleanup
4. Either implement global caching or remove --no-cache from all commands

---

## Conclusion

ExportSignaturesCommand is functionally complete but requires 7 must-fix and 4 should-fix issues to meet project standards. Total estimated effort: **4-6 hours** across two phases.

**Priority 1 (Must Fix)**: Async handler, format validation, temp cleanup, remove dead code (2-3 hours)
**Priority 2 (Should Fix)**: Logger, TFM selection/logging, namespace docs (1-2 hours)

After fixes, add E2E tests for Newtonsoft.Json 13.0.1 to validate schema conformance, format handling, and namespace filtering.

**Positive Findings**:
- JSON escaping correctly configured
- Resource disposal properly implemented
- Service layer follows best practices
- Core functionality complete and aligned with specs

**Next Steps**:
1. Apply Phase 1 fixes
2. Run `dotnet build` and `dotnet test`
3. Add E2E tests
4. Apply Phase 2 improvements
5. Update documentation with namespace filter semantics
