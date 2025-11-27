# Tasks: Improve Assembly Selection

## 1. Framework Selection Service

- [x] 1.1 Create `FrameworkSelector` service using `NuGet.Frameworks.FrameworkReducer`
- [x] 1.2 Add method to select nearest compatible TFM from available frameworks
- [x] 1.3 Add method to list available TFMs for error messaging
- [x] 1.4 Register service in Program.cs DI container

## 2. Reference Assembly Preference

- [x] 2.1 Update assembly extraction to check `GetReferenceItemsAsync()` first
- [x] 2.2 Fall back to `GetLibItemsAsync()` when ref/ is empty
- [x] 2.3 Extract shared logic to avoid duplication across commands

## 3. Deterministic Output Sorting

- [x] 3.1 Sort TypeInfo output by Namespace, then Name in ListTypesCommand
- [x] 3.2 Sort MethodInfo output by Type, then Method, then Signature in ExportSignaturesCommand
- [x] 3.3 Sort DiffResult arrays (breaking, added, removed) by Type, then Signature in ApiDiffAnalyzer

## 4. Command Updates

- [x] 4.1 Update ListTypesCommand to use FrameworkSelector and ref/ preference
- [x] 4.2 Update ExportSignaturesCommand to use FrameworkSelector and ref/ preference
- [x] 4.3 Update DiffCommand to use FrameworkSelector and ref/ preference
- [x] 4.4 Add available TFM listing to error messages when TFM not found

## 5. Testing

- [x] 5.1 Add unit tests for FrameworkSelector with cross-family scenarios (net8.0 vs netstandard2.0)
- [x] 5.2 Add E2E test verifying ref/ assembly preference with package containing both
- [x] 5.3 Add E2E test verifying deterministic output (multiple runs produce identical JSON)
- [x] 5.4 Add E2E test verifying TFM selection with multi-target package

## 6. Documentation

- [x] 6.1 Update AGENTS.md with FrameworkSelector service
- [x] 6.2 Document ref/ vs lib/ behavior in README
