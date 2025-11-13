# Implementation Tasks

## 1. Schema Design
- [x] 1.1 Create models-1.0.schema.json with $defs for all shared models (PackageInfo, TypeInfo, MethodInfo, ParameterInfo, DiffResult, DirectDependency)
- [x] 1.2 Add comprehensive "description" fields to every property in models schema
- [x] 1.3 Add "title" and "description" to root of models schema
- [x] 1.4 Add "examples" arrays to complex types in models schema
- [x] 1.5 Specify "format" keywords for string fields (e.g., uri, date-time, path)
- [x] 1.6 Create find.schema.json referencing models#$defs/PackageInfo with title, description, and examples
- [x] 1.7 Create list-types.schema.json (array of TypeInfo references) with full documentation
- [x] 1.8 Create export-signatures.schema.json (array of MethodInfo references) with full documentation
- [x] 1.9 Create diff.schema.json referencing models#$defs/DiffResult with full documentation
- [x] 1.10 Add top-level "examples" with real command outputs to each command schema
- [x] 1.11 Validate all schemas conform to JSON Schema Draft 2020-12

## 2. Implementation
- [x] 2.1 Create SchemaCommand.cs with System.CommandLine handler
- [x] 2.2 Add --command <name> option (values: find, list-types, export-signatures, diff)
- [x] 2.3 Add --all flag to export all schemas
- [x] 2.4 Add --output <path> option to write to file
- [x] 2.5 Embed schema files as resources in project
- [x] 2.6 Implement resource loading logic in SchemaCommand
- [x] 2.7 Register SchemaCommand in Program.cs

## 3. Testing
- [x] 3.1 Add unit tests for SchemaCommand (validate resource loading, option parsing)
- [x] 3.2 Add schema validation tests using real command outputs
- [x] 3.3 Verify all schemas include required documentation fields (title, description)
- [x] 3.4 Verify all properties have descriptions
- [x] 3.5 Verify examples are valid according to their schemas
- [x] 3.6 Test --command flag for each command
- [x] 3.7 Test --all flag
- [x] 3.8 Test --output flag (file write)
- [x] 3.9 Test default behavior (stdout)

## 4. Documentation
- [x] 4.1 Update README.md with schema command examples
- [x] 4.2 Add schema versioning policy to documentation
- [x] 4.3 Document $ref resolution for consumers
- [x] 4.4 Document schema annotation strategy (how descriptions aid AI agents)
- [x] 4.5 Provide examples of using schemas with LLM/AI tools
