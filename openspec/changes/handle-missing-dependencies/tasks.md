# Implementation Tasks

## 1. Update AssemblyInspector

- [ ] 1.1 Add try/catch around `assembly.GetTypes()` to handle `ReflectionTypeLoadException`
- [ ] 1.2 Use `ex.Types.Where(t => t != null)` to get partially-loaded types
- [ ] 1.3 Add per-type try/catch in enumeration loop for individual failures
- [ ] 1.4 Catch `TypeLoadException`, `FileNotFoundException`, `FileLoadException`, `NotSupportedException`
- [ ] 1.5 Log loader exceptions at Debug level with descriptive messages
- [ ] 1.6 Keep existing `BadImageFormatException` as hard error

## 2. Add Direct Dependency Reading

- [ ] 2.1 Add `GetDirectDependenciesAsync` method to `NuGetPackageResolver`
- [ ] 2.2 Use `PackageArchiveReader` or `PackageFolderReader` to read `.nuspec`
- [ ] 2.3 Parse `NuspecReader.GetDependencyGroups()`
- [ ] 2.4 Return list of dependencies with TFM, ID, and version range
- [ ] 2.5 Create `DirectDependency` model with appropriate properties

## 3. Update CLI Output

- [ ] 3.1 Modify `ListTypesCommand` to call `GetDirectDependenciesAsync`
- [ ] 3.2 Display direct dependencies grouped by target framework
- [ ] 3.3 Add user tip: "To inspect dependencies, run: nuget-toolbox list-types --package <Id>"
- [ ] 3.4 Ensure exit code is 0 when partial results are obtained

## 4. Testing

- [ ] 4.1 Add test for `ReflectionTypeLoadException` handling
- [ ] 4.2 Add test for per-type exception handling
- [ ] 4.3 Add test for reading direct dependencies from `.nuspec`
- [ ] 4.4 Integration test with real package that has dependencies (e.g., `Microsoft.Extensions.Logging`)
- [ ] 4.5 Verify Debug-level logging for missing dependencies

## 5. Documentation

- [ ] 5.1 Update README with example of handling packages with dependencies
- [ ] 5.2 Document that only the requested package is inspected
- [ ] 5.3 Add troubleshooting section for partial type loading
