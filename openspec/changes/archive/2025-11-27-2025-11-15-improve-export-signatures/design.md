# Design: Export-Signatures Improvements

## Architecture Overview

The export-signatures command follows a layered architecture:
```
CLI Command → SignatureExporter → AssemblyInspector → MetadataLoadContext
```

This design focuses on hardening the SignatureExporter layer while maintaining backward compatibility.

## Key Design Decisions

### 1. Type Visibility Filtering

**Current Issue**: `IsPublic || IsNestedPublic` includes nested types that may not be externally visible.

**Solution**: Use `Type.IsVisible` which properly evaluates accessibility from outside the assembly.

```csharp
// Before
.Where(t => t.IsPublic || t.IsNestedPublic)

// After  
.Where(t => t.IsVisible && (t.IsClass || t.IsInterface))
```

**Rationale**: `IsVisible` handles complex accessibility scenarios including nested types, generic constraints, and accessibility modifiers.

### 2. Partial Load Resilience

**Current Issue**: `ReflectionTypeLoadException` causes entire assembly to be skipped.

**Solution**: Catch the exception and process only successfully loaded types.

```csharp
Type[] types;
try 
{
    types = assembly.GetTypes();
}
catch (ReflectionTypeLoadException ex)
{
    types = ex.Types.Where(t => t != null).ToArray();
    _logger.LogWarning("Partial type load in {Assembly}: {LoadedCount}/{TotalCount} types loaded", 
        assemblyPath, types.Length, ex.Types.Length);
}
```

**Rationale**: Missing dependencies should not prevent analysis of available types. This approach maximizes coverage while maintaining stability.

### 3. Interface Method Inheritance

**Decision**: Keep current behavior (`DeclaredOnly`) for consistency and performance.

**Rationale**: 
- Including inherited interface methods can significantly increase output size
- Consumers typically need to know what a specific interface declares, not what it inherits
- Breaking change risk for existing consumers
- Can be made configurable later if needed

### 4. CLI Flag Compatibility

**Solution**: Add `--namespace` as alias to existing `--filter` option.

```csharp
var filterOption = new Option<string?>("--filter", "--namespace")
{
    Description = "Namespace filter (e.g., Newtonsoft.Json.Linq)"
};
```

**Rationale**: Maintains backward compatibility while aligning with specification.

## Error Handling Strategy

1. **Assembly-level failures**: Log error and continue with next assembly
2. **Type-level failures**: Skip individual types, log at debug level
3. **Method-level failures**: Skip individual methods, log at debug level
4. **Documentation failures**: Process methods without docs, log warning

## Performance Considerations

- `Type.IsVisible` has negligible overhead compared to current filtering
- Exception handling only adds overhead when partial loads occur
- No additional reflection calls or memory allocations

## Testing Strategy

### Unit Tests
1. **Visibility filtering**: Mock assembly with public/internal/nested types
2. **Partial load handling**: Simulate `ReflectionTypeLoadException` scenarios
3. **Interface inheritance**: Verify only declared methods are included
4. **CLI compatibility**: Test both flag variants produce identical results

### Integration Tests
1. **Real package**: Test with Newtonsoft.Json for end-to-end validation
2. **Missing deps**: Create test assembly with unresolvable dependencies
3. **Schema compliance**: Verify output matches JSON schema

## Backward Compatibility

- All existing CLI flags continue to work
- Output format unchanged
- No breaking changes to public APIs
- Existing behavior preserved except for bug fixes

## Future Extensibility

- Interface method inclusion can be made configurable via new flag
- Visibility filtering could be extended with custom criteria
- Partial load statistics could be exposed via verbose logging