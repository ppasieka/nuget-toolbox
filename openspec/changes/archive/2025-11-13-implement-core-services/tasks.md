## 1. NuGetPackageResolver Implementation
- [ ] 1.1 Implement ResolvePackageAsync using NuGet.Protocol V3
- [ ] 1.2 Implement DownloadPackageAsync with local caching
- [ ] 1.3 Add support for nuget.config feed resolution
- [ ] 1.4 Add support for credential providers
- [ ] 1.5 Write unit tests for package resolution

## 2. AssemblyInspector Implementation
- [ ] 2.1 Implement ExtractPublicTypes using MetadataLoadContext
- [ ] 2.2 Implement ExtractPublicTypesFromMultiple
- [ ] 2.3 Implement GetPublicMembers with filtering
- [ ] 2.4 Set up PathAssemblyResolver for reference assemblies
- [ ] 2.5 Write unit tests for assembly inspection

## 3. XmlDocumentationProvider Implementation
- [ ] 3.1 Implement LoadDocumentation to parse XML files
- [ ] 3.2 Implement GetSummary with documentation comment ID matching
- [ ] 3.3 Implement GetParameters to extract param tags
- [ ] 3.4 Implement GetReturns to extract returns tag
- [ ] 3.5 Use Roslyn DocumentationCommentId for canonical matching
- [ ] 3.6 Write unit tests for XML doc parsing

## 4. SignatureExporter Implementation
- [ ] 4.1 Implement ExportMethods using Roslyn SymbolDisplayFormat
- [ ] 4.2 Integrate XmlDocumentationProvider for doc injection
- [ ] 4.3 Implement ExportToJson with System.Text.Json
- [ ] 4.4 Implement ExportToJsonL for line-delimited output
- [ ] 4.5 Add optional namespace filtering
- [ ] 4.6 Write unit tests for signature export

## 5. ApiDiffAnalyzer Implementation
- [ ] 5.1 Implement CompareVersions to compare method sets
- [ ] 5.2 Implement IdentifyBreakingChanges logic
- [ ] 5.3 Detect added, removed, and modified types/methods
- [ ] 5.4 Return DiffResult with breaking change flags
- [ ] 5.5 Write unit tests for API diff analysis

## 6. Command Handlers Implementation
- [ ] 6.1 Implement FindCommand.InvokeAsync
- [ ] 6.2 Implement ListTypesCommand.InvokeAsync
- [ ] 6.3 Implement ExportSignaturesCommand.InvokeAsync
- [ ] 6.4 Implement DiffCommand.InvokeAsync
- [ ] 6.5 Add proper error handling and logging
- [ ] 6.6 Write integration tests for all commands

## 7. Testing & Validation
- [ ] 7.1 Run `dotnet build` and fix any errors
- [ ] 7.2 Run `dotnet test` and ensure all tests pass
- [ ] 7.3 Test CLI commands end-to-end with real packages
- [ ] 7.4 Validate JSON output format matches schema
