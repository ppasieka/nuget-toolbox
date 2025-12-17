# Design: Fix Missing Public Types

## Context

The `list-types` command uses `MetadataLoadContext` to safely inspect assemblies without executing code. However, two issues cause public types to be silently excluded:

1. **IsPublic vs IsVisible**: .NET's `Type.IsPublic` returns `false` for nested public types. `Type.IsVisible` correctly identifies all externally-visible types.

2. **Missing Dependencies**: When checking `type.IsClass`, .NET must resolve the type's base class chain. If dependency packages aren't loaded into `MetadataLoadContext`, this throws `FileNotFoundException`.

## Goals / Non-Goals

**Goals:**
- List all public types visible to external consumers
- Handle packages with complex dependency trees gracefully
- Maintain deterministic, reproducible output

**Non-Goals:**
- Loading transitive dependencies (only direct dependencies for now)
- Supporting internal types via `[InternalsVisibleTo]`

## Decisions

### Decision 1: Use `Type.IsVisible` instead of `Type.IsPublic`

**Rationale:** `IsVisible` returns `true` for types accessible from outside the assembly, correctly handling:
- Top-level public types
- Nested public types in public containers
- Excludes nested public in internal containers

**Alternative considered:** `type.IsPublic || type.IsNestedPublic` - more brittle, doesn't handle the full containment chain.

### Decision 2: Load Direct Dependencies into MetadataLoadContext

**Rationale:** To resolve base type chains, we need dependency assemblies available. Loading direct dependencies covers most cases without excessive complexity.

**Implementation:**
1. After resolving main package, fetch its `.nuspec` dependencies
2. Download dependency `.nupkg` files to cache
3. Extract dependency assemblies and add to `PathAssemblyResolver`

**Alternative considered:** Ignore dependency resolution entirely and use fallback classification. This works but loses accuracy for types with complex inheritance.

### Decision 3: Fallback Type Classification

**Rationale:** Even with dependencies loaded, some may still fail. Use type attributes as fallback:

```csharp
private static string? GetTypeKindWithFallback(Type type)
{
    try
    {
        if (type.IsClass) return "class";
        if (type.IsInterface) return "interface";
        if (type.IsValueType && !type.IsEnum) return "struct";
        if (type.IsEnum) return "enum";
    }
    catch (FileNotFoundException)
    {
        // Base type not available - use attribute-based inference
        if (type.IsSealed && type.IsAbstract) return "static class"; // Note: static classes are sealed+abstract
        if ((type.Attributes & TypeAttributes.Interface) != 0) return "interface";
        if (type.IsValueType) return "struct";
        return "class"; // Default assumption
    }
    return null;
}
```

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| Loading dependencies increases execution time | Only load direct dependencies; cache in NuGet global packages folder |
| Some dependencies may require auth | Use existing NuGet credential providers |
| Private feeds may have different dependency versions | Use same feed configuration for main package and dependencies |

## Migration Plan

1. Fix is backward-compatible - no breaking changes
2. Users will see MORE types in output (the missing ones)
3. Document in changelog: "Fixed: `list-types` now includes nested public types and handles missing dependencies gracefully"

## Open Questions

1. Should we add a `--include-dependencies` flag to opt-in to dependency loading (default off)?
2. Should we limit dependency loading to a specific depth (e.g., direct only)?
