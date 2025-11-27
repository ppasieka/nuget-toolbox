# Implementation Tasks

## Phase 1: Core Logic Improvements

1. **Update SignatureExporter type filtering**
   - Replace `IsPublic || IsNestedPublic` with `IsVisible && (IsClass || IsInterface)`
   - Add logging for visible type count per assembly
   - File: `src/NuGetToolbox.Cli/Services/SignatureExporter.cs:65-73`

2. **Add partial load exception handling**
   - Wrap `assembly.GetTypes()` in try-catch for `ReflectionTypeLoadException`
   - Process only non-null types from exception
   - Add warning log with load statistics
   - File: `src/NuGetToolbox.Cli/Services/SignatureExporter.cs:57-66`

3. **Validate interface method behavior**
   - Confirm current `DeclaredOnly` behavior is preserved
   - Add comment explaining design decision
   - File: `src/NuGetToolbox.Cli/Services/SignatureExporter.cs:74-83`

## Phase 2: CLI Compatibility

4. **Add namespace flag alias**
   - Update `--filter` option to include `--namespace` alias
   - Update help text to show both flags
   - Maintain backward compatibility
   - File: `src/NuGetToolbox.Cli/Commands/ExportSignaturesCommand.cs:38-41`

## Phase 3: Testing

5. **Create unit test for visibility filtering**
   - Test assembly with public nested in public (included)
   - Test assembly with public nested in internal (excluded)
   - Test assembly with structs/enums (excluded)
   - File: `tests/NuGetToolbox.Tests/SignatureExporterTests.cs`

6. **Create unit test for partial load handling**
   - Mock assembly that throws `ReflectionTypeLoadException`
   - Verify successful types are processed
   - Verify appropriate logging occurs
   - File: `tests/NuGetToolbox.Tests/SignatureExporterTests.cs`

7. **Create unit test for CLI flag compatibility**
   - Test both `--filter` and `--namespace` produce identical results
   - Test help text shows both options
   - File: `tests/NuGetToolbox.Tests/ExportSignaturesCommandTests.cs`

8. **Create integration test with real package**
   - Test with Newtonsoft.Json package
   - Verify expected types and methods are present
   - Verify output schema compliance
   - File: `tests/NuGetToolbox.Tests/ExportSignaturesCommandE2ETests.cs`

## Phase 4: Validation

9. **Run full test suite**
   - Execute `dotnet test` to ensure no regressions
   - Verify all new tests pass
   - Check code coverage improvements

10. **Validate schema compliance**
    - Run export-signatures on test package
    - Validate output against JSON schema
    - Ensure no breaking changes to output format

11. **Manual verification**
    - Test command with various flag combinations
    - Verify logging output in debug/verbose modes
    - Test error scenarios and recovery

## Phase 5: Cleanup

12. **Remove files**
   - Remove the PLAN.md file

## Dependencies

- Phase 1 tasks are independent and can be done in parallel
- Phase 2 depends on Phase 1 completion
- Phase 3 depends on Phase 1 and 2 completion
- Phase 4 requires all previous phases complete
- Phase 5 requires all previous phases complete

## Validation Criteria

- All existing tests continue to pass
- New unit tests achieve >90% code coverage for modified methods
- E2E test runs successfully on Newtonsoft.Json package
- Output validates against existing JSON schema
- CLI help shows both flag options
- No performance regression in typical usage scenarios