# ExportSignaturesCommand Analysis Report

## TL;DR
The command largely matches the documented purpose: resolve a NuGet package, pick a TFM, extract assemblies/XML, and export public method signatures as JSON or JSONL (with optional namespace filter). Main gaps: unused --no-cache option, blocking async pattern (.GetAwaiter().GetResult), temp directory not cleaned up, weak TFM selection logic, non-generic logger, and minor code-style deviations (usings order, option validation).

## Point-by-Point Comparison to AGENTS.md

### Command Purpose and Functionality
- **Documented**: Exports public method signatures with XML docs; supports namespace filter; outputs json/jsonl.
- **Implemented**: ✓ Matches. Delegates to SignatureExporter for rendering.

### Services Usage

#### NuGetPackageResolver
- **Documented**: Resolves package to nupkg path, respects nuget.config, returns PackageInfo incl. TFMs.
- **Implemented**: ✓ Used correctly to map package to nupkg.

#### AssemblyInspector, XmlDocumentationProvider
- **Documented**: Core services for type extraction and XML doc parsing.
- **Implemented**: Not directly used here but registered in DI; likely consumed by SignatureExporter as documented.

#### SignatureExporter
- **Documented**: Renders method signatures via Roslyn symbol display, injects XML docs, outputs JSON/JSONL with unescaped angle brackets.
- **Implemented**: ✓ Used to extract methods and render JSON/JSONL; aligns with spec.

### Output Format
- **Documented**: MethodInfo model with parameters/returnType, JSON/JSONL with unescaped angle brackets, camelCase.
- **Implemented**: ✓ Delegates to SignatureExporter for MethodInfo model and JSON/JSONL, consistent with expectations.

### Code Style Compliance

#### ✓ Compliant
- **Nullable annotations**: Acceptable on public API (IServiceProvider?; other option params are nullable).
- **Naming**: Conforms (PascalCase members, camelCase locals/params).

#### ❌ Not Compliant
- **Async**: Violates guideline by using GetAwaiter().GetResult() instead of async handler.
- **Logging**: Uses generic category string rather than ILogger<T>. Recommend switch to ILogger<ExportSignaturesCommand>. Also AddFile provider may not be documented; consider AddConsole.
- **Imports**: Not alphabetically sorted.

## Gaps and Discrepancies

### 1. Unused --no-cache Option
Option exists but is unused; either wire it to resolver or remove it (YAGNI).

### 2. Temp Directory Not Cleaned Up
Leads to accumulation of temp files across repeated runs.

### 3. Weak TFM Selection Logic
Simplistic (max Version); consider NuGetFrameworkUtility.GetNearest for correctness across framework families.

### 4. No Format Validation
Currently any value other than jsonl silently falls back to json; add validation and a clear error.

### 5. TFM Selection Not Logged
Does not report the selected TFM in logs beyond count; add a line that logs the chosen TFM short name for clarity.

### 6. No Cancellation Token
Acceptable for now per simplicity-first approach.

## Recommendations (Simple Path)

### 1. Fix Async Handler Usage
Replace the sync wrapper with a proper async System.CommandLine handler to avoid blocking:
```csharp
command.SetHandler(async (InvocationContext ctx) =>
{
    var parse = ctx.ParseResult;
    await HandlerAsync(
        parse.GetValue(packageOption)!,
        parse.GetValue(versionOption),
        parse.GetValue(tfmOption),
        parse.GetValue(formatOption) ?? "json",
        parse.GetValue(filterOption),
        parse.GetValue(outputOption),
        serviceProvider);
});
```

### 2. Clean Up Temp Extraction
Track the temp directory and delete it in a finally block after export completes:
```csharp
var tempDir = default(string);
try 
{ 
    tempDir = ExtractAssembliesAsync(...); 
    // ... use assemblies
}
finally 
{ 
    if (!string.IsNullOrEmpty(tempDir) && Directory.Exists(tempDir)) 
        Directory.Delete(tempDir, recursive: true); 
}
```

### 3. Use ILogger<ExportSignaturesCommand>
Request ILogger<ExportSignaturesCommand> (or create via loggerFactory.CreateLogger<ExportSignaturesCommand>()) for style compliance and clearer logging:
```csharp
var logger = loggerFactory.CreateLogger<ExportSignaturesCommand>();
```

### 4. Wire Up --no-cache or Remove It
If caching isn't implemented, remove the option for now (YAGNI). If caching exists in NuGetPackageResolver, pass the flag through:
```csharp
await resolver.ResolvePackageAsync(packageId, version, bypassCache: noCache)
```

### 5. Validate --format
Fail fast for unsupported values:
```csharp
var fmt = format.Trim().ToLowerInvariant();
if (fmt is not ("json" or "jsonl")) 
{ 
    logger.LogError("Unsupported format {Format}. Use json or jsonl.", format); 
    return 1; 
}
```

### 6. Improve TFM Selection Minimally
- If tfm is provided: match via GetShortFolderName() exactly (current behavior OK).
- If tfm is not provided: prefer a more robust selection (NuGetFrameworkUtility.GetNearest) if the NuGet.Frameworks dependency is already present; otherwise keep current but log which TFM was chosen for transparency.

### 7. Tidy Code Style
- Alphabetize using directives.
- Prefer AddConsole or AddSimpleConsole unless the AddFile provider is a known, committed dependency.

## Rationale and Trade-offs

### Why These Changes?
- **Async handler**: Removes the explicit blocking that violates the project's Async guideline and reduces deadlock risk.
- **Cleaning temp dirs**: Avoids resource leaks across repeated runs.
- **Generic logger**: Aligns with code style and improves log categorization.
- **Cache option**: Either wiring up or removing --no-cache avoids confusing, non-functional CLI surface.
- **Format validation**: Provides clearer UX and reduces accidental misuse.
- **TFM selection**: Enhancement provides more predictable results with diverse target groups, reflecting the "TFM selection" testing emphasis without a big refactor.

## Risks and Guardrails

1. **TFM selection**: If you switch to NuGetFrameworkUtility.GetNearest, verify the dependency is already present; otherwise, keep the simple selection and add logging of selected TFM to aid debugging.
2. **Temp directory deletion**: Ensure deletion happens after all reading is complete; wrap exporter calls in try/finally.
3. **Logging provider**: If AddFile is a custom extension not guaranteed in all environments, prefer AddConsole to avoid runtime failures.

## Scope Estimate
**Small-Medium** (1-3h), depending on whether you add NuGetFrameworks-based TFM heuristics.

## When to Consider Advanced Path

- Packages with many TFMs or complex compatibility cases produce wrong/undesired selection frequently.
- Very large JSON outputs cause memory pressure (consider streaming writers).
- Need to prefer ref assemblies to avoid type-loading friction and to represent public surface exactly.

## Optional Advanced Path (Only If Relevant)

### Robust TFM and Assembly Selection
- Prefer ref/ over lib/ when present for public API surface inspection.
- Use NuGetFrameworkUtility.GetNearest for TFM selection; allow fallback logic with explicit ordering (netX first, then netstandard, then netfx).
- Surface which TFM/assemblies were chosen in the output metadata (optional).

### Streaming JSON/JSONL
- For JSONL, stream line-by-line directly to file/stdout to reduce memory usage.
- For JSON, stream via Utf8JsonWriter if outputs become very large.

### Caching
- Implement file-based cache keyed by (packageId, version, tfm), storing extracted assemblies and XML; honor --no-cache to bypass and refresh.

## Overall Verdict
Functionally aligned and usable, but make the small fixes above to meet the project's code style and reliability expectations.
