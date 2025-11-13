## 1. Implementation

- [x] 1.1 Inject `NuGetPackageResolver` and `ILogger<T>` into FindCommand handler
- [x] 1.2 Parse command-line arguments (package, version, feed, output)
- [x] 1.3 Call `NuGetPackageResolver.ResolveAsync()` with package ID and optional version
- [x] 1.4 Handle resolution errors (package not found, network failures, auth failures)
- [x] 1.5 Serialize resolved `PackageInfo` to JSON using `System.Text.Json`
- [x] 1.6 Write JSON to output file (if `--output` specified) or stdout

## 2. Testing

- [x] 2.1 Create `FindCommandTests.cs` with xUnit+AAA pattern
- [x] 2.2 Test success case: valid package ID resolves correctly
- [x] 2.3 Test with explicit version: package ID + version resolves
- [x] 2.4 Test with custom feed: feed URL parameter is respected
- [x] 2.5 Test output to file: JSON written to specified file path
- [x] 2.6 Test output to stdout: JSON returned when no `--output` specified
- [x] 2.7 Test error case: package not found returns appropriate error
- [x] 2.8 Test error case: invalid feed URL fails gracefully
- [x] 2.9 Mock `NuGetPackageResolver` for offline tests

## 3. Verification

- [x] 3.1 Run `dotnet build` – no compilation errors
- [x] 3.2 Run `dotnet test` – all tests pass
- [ ] 3.3 Manual test: `dotnet run --project src/NuGetToolbox.Cli -- find --package "Newtonsoft.Json"` returns JSON
- [ ] 3.4 Verify JSON output matches expected `PackageInfo` schema
