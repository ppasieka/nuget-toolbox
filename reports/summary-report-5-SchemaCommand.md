# SchemaCommand Comprehensive Summary Report

## Executive Summary

SchemaCommand successfully loads and exports JSON Schema definitions from embedded resources but has **7 critical issues** affecting correctness, UX, and compliance. The command allows conflicting options, lacks output path validation, writes success messages to stdout (polluting JSON output), and violates AGENTS.md code style guidelines. All issues are fixable with small, targeted changes.

**Status:** ⚠️ Functional but needs fixes before production use  
**Priority Issues:** 3 High, 3 Medium, 1 Low  
**Estimated Fix Time:** 2-3 hours total

---

## Issues Summary Table

| # | Issue | Priority | Impact | Evidence | Fix Effort |
|---|-------|----------|--------|----------|------------|
| 1 | Mutual exclusivity not enforced | **HIGH** | UX confusion | [L56-65](#1-mutual-exclusivity-not-enforced) | Small |
| 2 | --all output path not validated | **HIGH** | Data loss risk | [L86-92](#2-all-output-path-not-validated) | Small |
| 3 | Success messages pollute stdout | **HIGH** | Breaks piping | [L109, L154](#3-success-messages-pollute-stdout) | Small |
| 4 | Style: No custom exceptions | **MEDIUM** | Non-compliant | [L76-80](#4-style-no-custom-exceptions) | Medium |
| 5 | --output help text misleading | **MEDIUM** | User confusion | [L33-36](#5-output-help-text-misleading) | Small |
| 6 | Case-sensitive command names | **MEDIUM** | UX friction | [L67-72](#6-case-sensitive-command-names) | Small |
| 7 | Undocumented default behavior | **LOW** | Documentation gap | [L61-65](#7-undocumented-default-behavior) | Small |

---

## Detailed Issues with Evidence

### 1. Mutual Exclusivity Not Enforced
**Priority:** HIGH  
**File:** [SchemaCommand.cs:56-65](file:///c:/dev/app/nuget-toolbox/src/NuGetToolbox.Cli/Commands/SchemaCommand.cs#L56-L65)

**Evidence:**
```csharp
if (all)
{
    return HandleAllSchemas(output);
}

if (string.IsNullOrEmpty(commandName))
{
    // Default: export models schema
    return ExportSchema("models", output);
}
```

**Problem:**  
User can specify both `--command find --all` with no error. The `--all` takes precedence silently (L56 check first), causing `--command` to be ignored. No validation prevents this confusing behavior.

**Impact:**  
- User expects specific schema but gets all schemas
- Silent failure mode - no warning that --command was ignored
- Wastes user time debugging unexpected output

**Fix:**
```csharp
// After L52, add validation:
if (all && !string.IsNullOrEmpty(commandName))
{
    Console.Error.WriteLine("Error: Cannot specify both --command and --all");
    return 1;
}
```

**Cross-reference:** [critique-report-5-SchemaCommand.md:96-123](file:///c:/dev/app/nuget-toolbox/reports/critique-report-5-SchemaCommand.md#L96-L123)

---

### 2. --all Output Path Not Validated
**Priority:** HIGH  
**File:** [SchemaCommand.cs:86-92](file:///c:/dev/app/nuget-toolbox/src/NuGetToolbox.Cli/Commands/SchemaCommand.cs#L86-L92)

**Evidence:**
```csharp
if (!string.IsNullOrEmpty(outputPath))
{
    // If output is specified, write to directory
    if (!Directory.Exists(outputPath))
    {
        Directory.CreateDirectory(outputPath);
    }
```

**Problem:**  
Code assumes `outputPath` is a directory but never validates. If user provides an **existing file path**, `Directory.CreateDirectory` silently does nothing (L91), then `Path.Combine(outputPath, fileName)` at L99 creates invalid path like `existing-file.json/find.schema.json`, causing write failure.

**Impact:**  
- Data loss risk: may overwrite existing files
- Confusing error messages from file system
- Undefined behavior when outputPath is a file

**Fix:**
```csharp
if (!string.IsNullOrEmpty(outputPath))
{
    if (File.Exists(outputPath))
    {
        Console.Error.WriteLine($"Error: --all requires a directory, but '{outputPath}' is a file");
        return 1;
    }
    Directory.CreateDirectory(outputPath);
```

**Cross-reference:** [critique-report-5-SchemaCommand.md:125-148](file:///c:/dev/app/nuget-toolbox/reports/critique-report-5-SchemaCommand.md#L125-L148)

---

### 3. Success Messages Pollute Stdout
**Priority:** HIGH  
**File:** [SchemaCommand.cs:109, 154](file:///c:/dev/app/nuget-toolbox/src/NuGetToolbox.Cli/Commands/SchemaCommand.cs#L109)

**Evidence:**
```csharp
// Line 109 (HandleAllSchemas)
Console.WriteLine($"Wrote {fileName}");

// Line 154 (ExportSchema)  
Console.WriteLine($"Wrote schema to {outputPath}");
```

**Problem:**  
Success messages written to stdout corrupt JSON output when piping: `dotnet run -- schema --command find | jq` fails because output contains both the message and JSON.

**Impact:**  
- Breaks automation/scripting
- Cannot pipe output to JSON parsers
- Violates Unix philosophy (data to stdout, messages to stderr)

**Fix:**
```csharp
// Line 109
Console.Error.WriteLine($"Wrote {fileName}");

// Line 154
Console.Error.WriteLine($"Wrote schema to {outputPath}");
```

**Related:** [L126-128](file:///c:/dev/app/nuget-toolbox/src/NuGetToolbox.Cli/Commands/SchemaCommand.cs#L126-L128) also pollutes stdout with `--- {commandName} ---` headers when using `--all` without output path.

**Cross-reference:** [critique-report-5-SchemaCommand.md:150-162](file:///c:/dev/app/nuget-toolbox/reports/critique-report-5-SchemaCommand.md#L150-L162)

---

### 4. Style: No Custom Exceptions
**Priority:** MEDIUM  
**File:** [SchemaCommand.cs:76-80](file:///c:/dev/app/nuget-toolbox/src/NuGetToolbox.Cli/Commands/SchemaCommand.cs#L76-L80)

**Evidence:**
```csharp
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}
```

**AGENTS.md Requirement:**
> Errors: Custom exceptions with actionable messages

**Problem:**  
Code uses generic `Exception` catch with `Console.Error` instead of custom exceptions. This violates AGENTS.md code style guidelines.

**Impact:**  
- Non-compliant with project standards
- Generic error handling less actionable
- Harder to distinguish error types in logs

**Fix (Low-priority, document deviation or create custom exceptions):**
```csharp
// Option 1: Create custom exceptions
public class SchemaNotFoundException : Exception { ... }
public class SchemaExportException : Exception { ... }

// Option 2: Document deviation in AGENTS.md
// "CLI commands use Console.Error for simplicity; custom exceptions for services only"
```

**Cross-reference:** [critique-report-5-SchemaCommand.md:15-23](file:///c:/dev/app/nuget-toolbox/reports/critique-report-5-SchemaCommand.md#L15-L23)

---

### 5. --output Help Text Misleading
**Priority:** MEDIUM  
**File:** [SchemaCommand.cs:33-36](file:///c:/dev/app/nuget-toolbox/src/NuGetToolbox.Cli/Commands/SchemaCommand.cs#L33-L36)

**Evidence:**
```csharp
var outputOption = new Option<string?>("--output", "-o")
{
    Description = "Output file or directory path (default: stdout)"
};
```

**Problem:**  
Description says "file **or directory**" but behavior differs:
- **--command:** Only accepts file path (L147-154); directory path causes `File.WriteAllText` to fail
- **--all:** Only accepts directory path (L86-92); file path causes undefined behavior

**Impact:**  
- User confusion about expected behavior
- Trial-and-error to discover correct usage
- Help text contradicts implementation

**Fix:**
```csharp
Description = "Output path. With --command, a file path; with --all, a directory (default: stdout)"
```

**Cross-reference:** [report-5-SchemaCommand.md:44-45](file:///c:/dev/app/nuget-toolbox/reports/report-5-SchemaCommand.md#L44-L45)

---

### 6. Case-Sensitive Command Names
**Priority:** MEDIUM  
**File:** [SchemaCommand.cs:67-72](file:///c:/dev/app/nuget-toolbox/src/NuGetToolbox.Cli/Commands/SchemaCommand.cs#L67-L72)

**Evidence:**
```csharp
if (!ValidCommands.Contains(commandName))
{
    Console.Error.WriteLine($"Error: Invalid command name '{commandName}'");
    Console.Error.WriteLine($"Valid commands: {string.Join(", ", ValidCommands)}");
    return 1;
}
```

**Problem:**  
`Contains` uses case-sensitive comparison. `--command Find` fails even though `find` is valid. No documentation warns users.

**Impact:**  
- UX friction (capitalization matters unexpectedly)
- Inconsistent with typical CLI tools (usually case-insensitive)
- Error message doesn't explain case sensitivity

**Fix:**
```csharp
// Line 11: Make ValidCommands a HashSet with case-insensitive comparer
private static readonly IReadOnlySet<string> ValidCommands = 
    new HashSet<string>(
        new[] { "find", "list-types", "export-signatures", "diff", "models" },
        StringComparer.OrdinalIgnoreCase
    );

// Line 67: Use Set.Contains (automatically case-insensitive)
if (!ValidCommands.Contains(commandName))
```

**Alternative:** Normalize input: `commandName = commandName?.ToLowerInvariant();`

**Cross-reference:** [report-5-SchemaCommand.md:53-54](file:///c:/dev/app/nuget-toolbox/reports/report-5-SchemaCommand.md#L53-L54)

---

### 7. Undocumented Default Behavior
**Priority:** LOW  
**File:** [SchemaCommand.cs:61-65](file:///c:/dev/app/nuget-toolbox/src/NuGetToolbox.Cli/Commands/SchemaCommand.cs#L61-L65)

**Evidence:**
```csharp
if (string.IsNullOrEmpty(commandName))
{
    // Default: export models schema
    return ExportSchema("models", output);
}
```

**Problem:**  
Running `dotnet run -- schema` (no flags) exports the `models` schema. This behavior is:
- Not mentioned in option descriptions
- Not documented in AGENTS.md or README
- Not shown in `--help` output

**Impact:**  
- Users may not discover this shortcut
- Surprising behavior when no arguments provided
- Inconsistent with other commands that require arguments

**Fix (Documentation only):**
```csharp
// Update command description (L38):
var command = new Command(
    "schema", 
    "Export JSON Schema definitions for command outputs (defaults to models schema)"
);
```

**Cross-reference:** [report-5-SchemaCommand.md:41-42](file:///c:/dev/app/nuget-toolbox/reports/report-5-SchemaCommand.md#L41-L42)

---

## Additional Observations (Not Critical)

### Schema Versioning Hardcoded
**File:** [SchemaCommand.cs:96-98](file:///c:/dev/app/nuget-toolbox/src/NuGetToolbox.Cli/Commands/SchemaCommand.cs#L96-L98)

**Evidence:**
```csharp
var fileName = commandName == "models"
    ? "models-1.0.schema.json"
    : $"{commandName}.schema.json";
```

**Issue:** Hardcodes version "1.0" for models but not other schemas. AGENTS.md states:
> Schema versioning follows semantic versioning (models-1.0, models-2.0, etc.)

**Impact:** Low - works for current version but fragile for future schema evolution.

**Cross-reference:** [critique-report-5-SchemaCommand.md:55-95](file:///c:/dev/app/nuget-toolbox/reports/critique-report-5-SchemaCommand.md#L55-L95)

---

### Synchronous I/O (Style Deviation)
**File:** [SchemaCommand.cs:108, 153, 180](file:///c:/dev/app/nuget-toolbox/src/NuGetToolbox.Cli/Commands/SchemaCommand.cs#L108)

**AGENTS.md Requirement:**
> Async: `async Task` for I/O; no `.Result` or `.Wait()`

**Evidence:**
```csharp
File.WriteAllText(filePath, schema);  // L108, L153
return reader.ReadToEnd();            // L180
```

**Impact:** Low - schema files are small, performance not critical for CLI tool.

**Cross-reference:** [report-5-SchemaCommand.md:35-37](file:///c:/dev/app/nuget-toolbox/reports/report-5-SchemaCommand.md#L35-L37)

---

## Prioritized Fix Roadmap

### Phase 1: Critical Correctness (HIGH Priority)
**Effort:** 1 hour | **Blockers:** Production use

1. **Mutual Exclusivity** - Add validation (L52-53)
2. **Stdout Pollution** - Move messages to stderr (L109, L154)
3. **--all Path Validation** - Check for file vs directory (L86-92)

**Acceptance Criteria:**
- [ ] `--command find --all` returns error exit code 1
- [ ] `--all --output existing-file.json` returns error
- [ ] Piping JSON output works: `... | jq` succeeds
- [ ] Success messages appear in stderr only

---

### Phase 2: UX Improvements (MEDIUM Priority)
**Effort:** 1 hour | **Benefits:** Better user experience

4. **Help Text Accuracy** - Update --output description (L35)
5. **Case-Insensitive Commands** - Use StringComparer (L11, L67)
6. **Style Compliance** - Document exception handling deviation or refactor

**Acceptance Criteria:**
- [ ] `--help` accurately describes --output behavior
- [ ] `--command Find` works (case-insensitive)
- [ ] AGENTS.md documents error handling approach

---

### Phase 3: Documentation (LOW Priority)
**Effort:** 15 minutes | **Benefits:** Clarity

7. **Default Behavior** - Document in command description (L38)

**Acceptance Criteria:**
- [ ] `--help` shows default behavior
- [ ] AGENTS.md documents "no flags → models schema"

---

## Testing Requirements

### Unit Tests (Add to SchemaCommandTests.cs)
```csharp
[Fact]
public void BothCommandAndAll_ReturnsError()
{
    var result = SchemaCommand.Execute("--command", "find", "--all");
    Assert.Equal(1, result);
}

[Fact]
public void AllWithFilePath_ReturnsError()
{
    File.WriteAllText("test.json", "{}");
    var result = SchemaCommand.Execute("--all", "--output", "test.json");
    Assert.Equal(1, result);
}

[Theory]
[InlineData("find")]
[InlineData("FIND")]
[InlineData("Find")]
public void CommandName_CaseInsensitive(string commandName)
{
    var result = SchemaCommand.Execute("--command", commandName);
    Assert.Equal(0, result);
}

[Fact]
public void NoArguments_ExportsModelsSchema()
{
    var result = SchemaCommand.Execute();
    Assert.Equal(0, result);
    // Assert stdout contains models schema
}
```

### E2E Tests (Add to SchemaCommandE2ETests.cs)
```bash
# Test stdout purity
dotnet run -- schema --command find | jq .

# Test mutual exclusivity
dotnet run -- schema --command find --all
# Expected: exit code 1, error message

# Test case insensitivity
dotnet run -- schema --command FIND
# Expected: exit code 0, valid JSON

# Test --all with file
touch test.json
dotnet run -- schema --all --output test.json
# Expected: exit code 1, error message
```

---

## Risk Assessment

### Implementation Risks

1. **Breaking Change - Mutual Exclusivity**
   - **Risk:** Scripts relying on precedence behavior will break
   - **Likelihood:** LOW (undocumented behavior, unlikely to be relied upon)
   - **Mitigation:** Document in release notes; behavior was never guaranteed

2. **Stderr Message Relocation**
   - **Risk:** Scripts parsing stdout may unexpectedly succeed
   - **Likelihood:** LOW (messages were polluting output anyway)
   - **Mitigation:** None needed; this is a fix not a breaking change

3. **Case-Insensitive Matching**
   - **Risk:** May conflict with future versioned schema names (Find-v1 vs find-v1)
   - **Likelihood:** LOW (schema names are controlled by project)
   - **Mitigation:** Document naming convention; use lowercase internally

---

## Cross-References

- **Original Report:** [report-5-SchemaCommand.md](file:///c:/dev/app/nuget-toolbox/reports/report-5-SchemaCommand.md)
- **Critique Report:** [critique-report-5-SchemaCommand.md](file:///c:/dev/app/nuget-toolbox/reports/critique-report-5-SchemaCommand.md)
- **Source Code:** [SchemaCommand.cs](file:///c:/dev/app/nuget-toolbox/src/NuGetToolbox.Cli/Commands/SchemaCommand.cs)
- **Project Guidelines:** [AGENTS.md](file:///c:/dev/app/nuget-toolbox/AGENTS.md)

---

## Conclusion

SchemaCommand is **functionally sound** for basic use cases but requires **7 targeted fixes** before production deployment. The three HIGH-priority issues (mutual exclusivity, path validation, stdout pollution) are critical for correct operation and can be resolved in ~1 hour. MEDIUM-priority issues improve UX and compliance. All issues have concrete fixes with code examples and line references.

**Recommended Action:** Implement Phase 1 fixes immediately, Phase 2 within sprint, Phase 3 as documentation debt.
