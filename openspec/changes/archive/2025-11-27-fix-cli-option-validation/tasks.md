## 1. SchemaCommand Fixes

- [x] 1.1 Enforce mutual exclusivity between `--command` and `--all` flags
- [x] 1.2 Validate `--output` with `--all` is a directory path (not a file)
- [x] 1.3 Make command name matching case-insensitive
- [x] 1.4 Add unit tests for SchemaCommand option validation

## 2. ExportSignaturesCommand Fixes

- [x] 2.1 Add `FromAmong` validation for `--format` option (restrict to "json", "jsonl")
- [x] 2.2 Remove unused `--no-cache` option
- [x] 2.3 Add unit tests for format validation

## 3. Verification

- [x] 3.1 Run `dotnet build` to verify no compile errors
- [x] 3.2 Run `dotnet test` to verify all tests pass
- [x] 3.3 Run E2E tests to verify CLI behavior
