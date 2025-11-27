# Design: Improve Assembly Selection

## Context

The current implementation uses naive version sorting (`OrderByDescending(g => g.TargetFramework.Version)`) which fails across framework families. For example, when running on .NET 8.0, the system might incorrectly prefer `net4.8` (version 4.8) over `netstandard2.0` (version 2.0), even though `netstandard2.0` is more compatible with `net8.0`.

Additionally, packages often contain both `ref/` (reference assemblies) and `lib/` (implementation assemblies). Reference assemblies are specifically designed for metadata inspection and provide a cleaner public API surface without implementation details.

## Goals / Non-Goals

**Goals:**
- Use NuGet's built-in framework compatibility logic for TFM selection
- Prefer reference assemblies (`ref/`) for cleaner API surface
- Ensure deterministic JSON output for caching and testing
- Provide helpful error messages listing available TFMs on mismatch

**Non-Goals:**
- Runtime assembly resolution (we only need metadata)
- Custom framework compatibility rules
- Supporting obsolete framework monikers

## Decisions

### Decision 1: Use FrameworkReducer for TFM Selection

Use `NuGet.Frameworks.FrameworkReducer.GetNearest()` which implements the official NuGet framework compatibility matrix.

```csharp
using NuGet.Frameworks;

public class FrameworkSelector
{
    private readonly FrameworkReducer _reducer = new();
    
    public NuGetFramework? SelectNearest(
        NuGetFramework target,
        IEnumerable<NuGetFramework> available)
    {
        return _reducer.GetNearest(target, available);
    }
}
```

**Rationale:** NuGet.Frameworks is already a dependency and provides well-tested compatibility logic.

### Decision 2: Prefer ref/ Over lib/

Check `GetReferenceItemsAsync()` first; if empty, fall back to `GetLibItemsAsync()`.

```csharp
var refItems = (await reader.GetReferenceItemsAsync(ct)).ToList();
var libItems = (await reader.GetLibItemsAsync(ct)).ToList();
var items = refItems.Count > 0 ? refItems : libItems;
```

**Rationale:** Reference assemblies are designed for metadata inspection and exclude internal implementation types that would add noise to the output.

### Decision 3: Sort Output Arrays

Apply consistent sorting before JSON serialization:

- `TypeInfo`: Sort by `Namespace`, then `Name`
- `MethodInfo`: Sort by `Type`, then `Method`, then `Signature`
- `DiffResult` arrays: Sort by `Type`, then `Signature`

**Rationale:** Deterministic output enables byte-for-byte comparison, caching, and simpler test assertions.

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| Different assembly selected after TFM fix | Add golden tests before/after to verify expected behavior |
| ref/ assemblies may have fewer types | This is expected—log when falling back to lib/ |
| Sorting adds overhead | Negligible for typical package sizes (<10k types) |

## Migration Plan

1. Implement FrameworkSelector service
2. Update commands one at a time with tests
3. Run E2E tests to verify output changes
4. Document behavior changes in changelog

## Open Questions

- Should we expose `--prefer-lib` flag for users who want implementation assemblies?
- Should the default target framework be configurable (currently hardcoded to detect runtime)?
