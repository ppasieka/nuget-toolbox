## 1. Implementation

- [ ] 1.1 Create `Services/CommandOutput.cs` with shared `JsonSerializerOptions` and `WriteResultAsync` method
- [ ] 1.2 Update `FindCommand.cs` to use `CommandOutput.SerializeJson` and `CommandOutput.WriteResultAsync`
- [ ] 1.3 Update `ListTypesCommand.cs` to use `CommandOutput.SerializeJson` and `CommandOutput.WriteResultAsync`
- [ ] 1.4 Update `ExportSignaturesCommand.cs` to use `CommandOutput.SerializeJson` and `CommandOutput.WriteResultAsync`
- [ ] 1.5 Run `dotnet build` and ensure no compilation errors
- [ ] 1.6 Run `dotnet test` and ensure all tests pass
- [ ] 1.7 Run E2E tests to verify JSON output unchanged
