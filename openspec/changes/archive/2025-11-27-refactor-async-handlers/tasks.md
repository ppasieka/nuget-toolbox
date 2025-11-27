## 1. Service Layer Updates
- [x] 1.1 Update `NuGetPackageResolver.ResolvePackageAsync` to accept `CancellationToken`.
- [x] 1.2 Update `NuGetPackageResolver.DownloadPackageAsync` to accept `CancellationToken`.
- [x] 1.3 Update `SignatureExporter.ExportAsync` to accept `CancellationToken`.
- [x] 1.4 Update `ApiDiffAnalyzer` methods to accept `CancellationToken` where applicable.
- [x] 1.5 Update `PackageArchiveReader` usage to pass `CancellationToken`.

## 2. Command Handler Refactoring
- [x] 2.1 Refactor `FindCommand` to use `command.SetHandler(async (InvocationContext ctx) => ...)`
- [x] 2.2 Refactor `ListTypesCommand` to use `command.SetHandler(async (InvocationContext ctx) => ...)`
- [x] 2.3 Refactor `ExportSignaturesCommand` to use `command.SetHandler(async (InvocationContext ctx) => ...)`
- [x] 2.4 Refactor `DiffCommand` to use `command.SetHandler(async (InvocationContext ctx) => ...)`
- [x] 2.5 Ensure `SchemaCommand` handles async operations correctly (if any).

## 3. Exit Codes & Cancellation
- [x] 3.1 Implement `ExitCode` constants (Success=0, NotFound=1, TfmMismatch=2, InvalidOptions=3, NetworkError=4, Error=5).
- [x] 3.2 Update all handlers to return appropriate exit codes.
- [x] 3.3 Verify `Ctrl+C` cleanly cancels operations and cleans up resources.
