## 1. Program.cs Centralized DI Setup
- [x] 1.1 Add ServiceCollection and configure logging to stderr
- [x] 1.2 Register NuGetPackageResolver as Singleton
- [x] 1.3 Register AssemblyInspector as Transient
- [x] 1.4 Register XmlDocumentationProvider as Transient
- [x] 1.5 Register SignatureExporter as Transient
- [x] 1.6 Register ApiDiffAnalyzer as Transient
- [x] 1.7 Build ServiceProvider and pass to all Command.Create() methods

## 2. Update FindCommand
- [x] 2.1 Change Create() signature to require non-null IServiceProvider
- [x] 2.2 Remove CreateDefaultServiceProvider() method
- [x] 2.3 Remove null-coalescing fallback in HandlerAsync
- [x] 2.4 Verify tests still pass

## 3. Update DiffCommand
- [x] 3.1 Change Create() signature to require non-null IServiceProvider
- [x] 3.2 Remove CreateDefaultServiceProvider() method
- [x] 3.3 Remove null-coalescing fallback in HandlerAsync
- [x] 3.4 Verify tests still pass

## 4. Update ListTypesCommand
- [x] 4.1 Change Create() signature to require non-null IServiceProvider (if has ad-hoc DI)
- [x] 4.2 Remove CreateDefaultServiceProvider() if present
- [x] 4.3 Verify tests still pass

## 5. Update ExportSignaturesCommand
- [x] 5.1 Change Create() signature to require non-null IServiceProvider (if has ad-hoc DI)
- [x] 5.2 Remove CreateDefaultServiceProvider() if present
- [x] 5.3 Verify tests still pass

## 6. Update SchemaCommand
- [x] 6.1 Pass IServiceProvider for consistency (even if not currently used)

## 7. Testing & Validation
- [x] 7.1 Run `dotnet build` and fix any compilation errors
- [x] 7.2 Run `dotnet test` and ensure all tests pass
- [ ] 7.3 Add unit test verifying commands receive injected services
- [x] 7.4 Verify E2E tests still work
