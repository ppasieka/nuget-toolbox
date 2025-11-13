## 1. Model Extension
- [x] 1.1 Add `Parameters` property to `MethodInfo.cs` (list of parameter type+name)
- [x] 1.2 Add `ReturnType` property to `MethodInfo.cs`
- [x] 1.3 Update JSON serialization attributes (camelCase)

## 2. Metadata Extraction
- [x] 2.1 Update `AssemblyInspector` to extract parameter metadata (type, name) from reflection
- [x] 2.2 Update `AssemblyInspector` to extract return type metadata
- [x] 2.3 Ensure extraction works when XML docs are absent

## 3. Signature Export
- [x] 3.1 Update `SignatureExporter` to populate new fields
- [x] 3.2 Ensure JSON output includes parameter types and names
- [x] 3.3 Ensure JSON output includes return type

## 4. Testing
- [x] 4.1 Add unit tests for parameter extraction
- [x] 4.2 Add unit tests for return type extraction
- [x] 4.3 Add tests for methods without XML documentation
- [x] 4.4 Update E2E tests to validate new JSON fields

## 5. Validation
- [x] 5.1 Run `dotnet build` and fix any issues
- [x] 5.2 Run `dotnet test` and ensure all tests pass
- [x] 5.3 Test with real package (Newtonsoft.Json) to verify output
