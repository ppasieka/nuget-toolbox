## MODIFIED Requirements

### Requirement: C# Signature Export
The system SHALL render method signatures using Roslyn SymbolDisplayFormat, inject XML documentation, and extract parameter/return type metadata from reflection independent of XML documentation availability.

#### Scenario: Export with XML documentation
- **WHEN** method has complete XML documentation (summary, params, returns)
- **THEN** output includes all documentation fields plus parameter types/names and return type

#### Scenario: Export without XML documentation
- **WHEN** method lacks XML documentation
- **THEN** output includes parameter types/names and return type from reflection metadata

#### Scenario: Export with partial XML documentation
- **WHEN** method has summary but missing params or returns in XML
- **THEN** output includes summary from XML plus parameter/return type info from reflection

#### Scenario: JSON output structure
- **WHEN** exporting method signatures
- **THEN** each method includes:
  - `type` - containing type name
  - `method` - method name
  - `signature` - full method signature
  - `summary` - documentation summary (if available)
  - `params` - dictionary of parameter names to documentation (if available)
  - `returns` - return value documentation (if available)
  - `parameters` - array of {name, type} objects from reflection
  - `returnType` - return type from reflection

## ADDED Requirements

### Requirement: Parameter Metadata Extraction
The system SHALL extract parameter metadata (type and name) from assembly reflection for all public methods.

#### Scenario: Simple parameter types
- **WHEN** method has value type or string parameters
- **THEN** output includes accurate type names (e.g., "System.Int32", "System.String")

#### Scenario: Complex parameter types
- **WHEN** method has generic, array, or reference type parameters
- **THEN** output includes full type names with namespace and generic arguments

#### Scenario: Parameter names
- **WHEN** extracting parameters
- **THEN** output preserves original parameter names from metadata

### Requirement: Return Type Metadata Extraction
The system SHALL extract return type metadata from assembly reflection for all public methods.

#### Scenario: Void return type
- **WHEN** method returns void
- **THEN** `returnType` field contains "System.Void"

#### Scenario: Value type return
- **WHEN** method returns value type
- **THEN** `returnType` field contains full type name with namespace

#### Scenario: Generic return type
- **WHEN** method returns generic type
- **THEN** `returnType` field contains full generic type notation
