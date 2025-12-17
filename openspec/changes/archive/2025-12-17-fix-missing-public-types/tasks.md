# Implementation Tasks

## 1. Investigation (Complete)

- [x] 1.1 Reproduce the bug with `Confirmit.SurveyLayout.Model v21.1.3`
- [x] 1.2 Identify root cause: `type.IsPublic` vs `type.IsVisible`
- [x] 1.3 Identify secondary cause: `FileNotFoundException` when resolving base types from missing dependencies
- [x] 1.4 Consult oracle for recommended fix approach

## 2. Fix Visibility Check

- [x] 2.1 Change `if (!type.IsPublic)` to `if (!type.IsVisible)` in `AssemblyInspector.ExtractPublicTypes()`
- [x] 2.2 Add unit test with nested public types (verify `Container+NestedPublic` is included)
- [x] 2.3 Add unit test for nested public in internal container (verify excluded)

## 3. Fix Dependency Resolution for Type Classification

- [ ] 3.1 Create helper method to download/resolve direct dependencies of a package (optional enhancement)
- [ ] 3.2 Add dependency assemblies to `PathAssemblyResolver` in `AssemblyInspector` (optional enhancement)
- [ ] 3.3 Update `ListTypesCommand` to pass dependency assembly paths (optional enhancement)
- [x] 3.4 Add fallback type classification when base type resolution fails

## 4. Enhanced Error Handling in GetTypeKind

- [x] 4.1 Wrap `type.IsClass`, `type.IsInterface`, etc. in try-catch for `FileNotFoundException`
- [x] 4.2 When base type resolution fails, attempt to infer kind from type attributes/flags
- [ ] 4.3 Log debug message when fallback classification is used (skipped - keeps code simple)

## 5. Testing

- [x] 5.1 Add E2E test for nested public types with `Newtonsoft.Json` (replaced Confirmit test - private package)
- [x] 5.2 Add unit test for nested types (public container and internal container scenarios)
- [x] 5.3 Verify existing tests pass

## 6. Validation

- [x] 6.1 Run `dotnet build` - no errors
- [x] 6.2 Run `dotnet test` - all tests pass (107 passed, 2 skipped)
- [ ] 6.3 Manual verification with problem package (requires private feed access)
