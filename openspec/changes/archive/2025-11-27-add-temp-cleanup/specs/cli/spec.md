## ADDED Requirements

### Requirement: Temporary Directory Cleanup

Commands that extract package assemblies to temporary directories SHALL ensure cleanup of those directories on all execution paths (success, error, and cancellation).

#### Scenario: Cleanup after successful execution
- **WHEN** command completes successfully
- **THEN** all temporary directories created during execution are deleted
- **AND** no orphan files remain in the temp location

#### Scenario: Cleanup after exception
- **WHEN** command fails with an exception during processing
- **THEN** all temporary directories created before the failure are deleted
- **AND** cleanup logic runs in a finally block

#### Scenario: Cleanup after cancellation
- **WHEN** user cancels command execution (e.g., Ctrl+C)
- **THEN** all temporary directories created before cancellation are deleted
- **AND** cleanup runs even when CancellationToken is triggered

#### Scenario: Cleanup failure is non-fatal
- **WHEN** temporary directory deletion fails (e.g., file locked)
- **THEN** command logs a warning with the failure reason
- **AND** command does not throw or change its exit code due to cleanup failure
- **AND** command continues normal error handling

#### Scenario: DiffCommand cleans up both temp directories
- **WHEN** diff command creates temp directories for both "from" and "to" packages
- **THEN** both directories are tracked for cleanup
- **AND** both directories are deleted in the finally block
