## 1. Implementation

- [ ] 1.1 Change `Console.WriteLine($"Wrote {fileName}")` to `Console.Error.WriteLine` in `HandleAllSchemasAsync` (L112)
- [ ] 1.2 Remove or move `Console.WriteLine($"--- {commandName} ---")` separator line to stderr (L129)
- [ ] 1.3 Remove decorative empty line `Console.WriteLine()` after schema output (L131)
- [ ] 1.4 Change `Console.WriteLine($"Wrote schema to {outputPath}")` to `Console.Error.WriteLine` in `ExportSchemaAsync` (L157)

## 2. Testing

- [ ] 2.1 Add E2E test verifying `schema --command find | jq .` succeeds
- [ ] 2.2 Add E2E test verifying `schema --all` stdout is parseable (JSONL or pure schemas)
- [ ] 2.3 Verify existing SchemaCommandTests still pass

## 3. Verification

- [ ] 3.1 Run `dotnet build` and ensure no errors
- [ ] 3.2 Run `dotnet test` and ensure all tests pass
- [ ] 3.3 Manual test: `dotnet run --project src/NuGetToolbox.Cli -- schema --command find | jq .`
