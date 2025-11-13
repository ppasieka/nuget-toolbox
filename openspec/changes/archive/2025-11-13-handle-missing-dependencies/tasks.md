# Implementation Tasks

## 1. Update AssemblyInspector

- [x] 1.1 Add try/catch around `assembly.GetTypes()` to handle `ReflectionTypeLoadException`
- [x] 1.2 Use `ex.Types.Where(t => t != null)` to get partially-loaded types
- [x] 1.3 Add per-type try/catch in enumeration loop for individual failures
- [x] 1.4 Catch `TypeLoadException`, `FileNotFoundException`, `FileLoadException`, `NotSupportedException`
- [x] 1.5 Log loader exceptions at Debug level with descriptive messages
- [x] 1.6 Keep existing `BadImageFormatException` as hard error

## 2. Add Direct Dependency Reading

- [x] 2.1 Add `GetDirectDependenciesAsync` method to `NuGetPackageResolver`
- [x] 2.2 Use `PackageArchiveReader` or `PackageFolderReader` to read `.nuspec`
- [x] 2.3 Parse `NuspecReader.GetDependencyGroups()`
- [x] 2.4 Return list of dependencies with TFM, ID, and version range
- [x] 2.5 Create `DirectDependency` model with appropriate properties

## 3. Update Logging Configuration

- [x] 3.1 Configure file-based logging in all commands
- [x] 3.2 Set log output to `%TEMP%/nuget-toolbox/logs/nuget-toolbox-{date}.log`
- [x] 3.3 Remove console logging to keep stdout clean for JSON output
- [x] 3.4 Ensure exit code is 0 when partial results are obtained

## 4. Testing

- [x] 4.1 Add test for `ReflectionTypeLoadException` handling
- [x] 4.2 Add test for per-type exception handling
- [x] 4.3 Add test for reading direct dependencies from `.nuspec`
- [x] 4.4 Integration test with real package that has dependencies (e.g., `Microsoft.Extensions.Logging`)
- [x] 4.5 Verify Debug-level logging for missing dependencies

## 5. Documentation

- [x] 5.1 Update README with example of handling packages with dependencies
- [x] 5.2 Document that only the requested package is inspected
- [x] 5.3 Add troubleshooting section for partial type loading
