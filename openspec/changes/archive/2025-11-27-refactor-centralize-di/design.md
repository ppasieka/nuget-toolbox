## Context

Currently, FindCommand and DiffCommand each create their own `ServiceProvider` via `CreateDefaultServiceProvider()`. This ad-hoc pattern:
- Fragments DI configuration across multiple files
- Causes inconsistent service lifetimes (Scoped resolved without scope)
- Makes unit testing harder (cannot inject mocks without optional parameter)
- Violates AGENTS.md: "Move DI setup to Program.cs"

## Goals / Non-Goals

**Goals:**
- Centralize all DI registration in Program.cs
- Use correct service lifetimes (Singleton for stateless, Transient for per-operation)
- Enable easier unit testing via DI injection
- Align with AGENTS.md guidelines

**Non-Goals:**
- Add new features or change command behavior
- Modify logging output format
- Change exit codes or error handling

## Decisions

### Decision 1: Centralize DI in Program.cs
All services are registered once in Program.cs and the built `ServiceProvider` is passed to each command's `Create()` method.

**Alternatives considered:**
- Generic host builder (`IHostBuilder`) - Overkill for CLI; adds complexity
- Static service locator - Anti-pattern; harder to test
- Keep optional `IServiceProvider` - Current state; fragments config

### Decision 2: Service Lifetimes
| Service | Lifetime | Rationale |
|---------|----------|-----------|
| NuGetPackageResolver | Singleton | Stateless, can be reused |
| AssemblyInspector | Transient | Uses MetadataLoadContext per-call |
| XmlDocumentationProvider | Transient | Loads docs per-assembly |
| SignatureExporter | Transient | Per-operation processing |
| ApiDiffAnalyzer | Transient | Per-comparison processing |
| ILoggerFactory | Singleton | Standard .NET behavior |

**Rationale:** Scoped is incorrect for CLI (no scope boundaries); Singleton for truly stateless services, Transient for services that hold per-operation state.

### Decision 3: Required IServiceProvider in Create()
Change signature from `Create(IServiceProvider? serviceProvider = null)` to `Create(IServiceProvider serviceProvider)`.

**Rationale:** Forces Program.cs to configure DI; prevents hidden fallback paths.

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| Breaking change for tests using `Create()` without args | Update all test callsites to provide mock IServiceProvider |
| Larger Program.cs file | Keep DI setup concise; extract to extension method if >30 lines |
| Circular dependency potential | Register in dependency order; no cycles in current design |

## Migration Plan

1. Add centralized DI to Program.cs (additive, no breaks)
2. Update each command to use required IServiceProvider
3. Remove `CreateDefaultServiceProvider()` from each command
4. Update tests to inject mock services
5. Run full test suite to verify

**Rollback:** Revert to optional parameter pattern if blocking issues found.

## Open Questions

- None at this time. Design is straightforward refactoring.
