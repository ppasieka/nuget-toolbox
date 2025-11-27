## Why
The current implementation of async command handlers uses the "sync-over-async" anti-pattern (`.GetAwaiter().GetResult()`), which violates project guidelines and introduces risks of thread pool starvation and deadlocks. Additionally, cancellation tokens are not correctly propagated, making operations uncancelable via `Ctrl+C`.

## What Changes
- Refactor all async command handlers to use `SetHandler` with `async/await` instead of `SetAction` or blocking calls.
- Propagate `CancellationToken` from the invocation context to all async services.
- Introduce standard exit codes for different failure modes.

## Impact
- **Affected specs**: `cli`
- **Affected code**:
  - `FindCommand.cs`
  - `ListTypesCommand.cs`
  - `ExportSignaturesCommand.cs`
  - `DiffCommand.cs`
  - `SchemaCommand.cs`
  - All service methods will need to accept `CancellationToken`.
