# Tasks: Add Temp Directory Cleanup

## 1. Implementation

- [x] 1.1 Add temp cleanup to ListTypesCommand
  - Wrap extraction and processing in try/finally
  - Delete temp directory in finally block
  - Log warning if cleanup fails

- [x] 1.2 Add temp cleanup to ExportSignaturesCommand
  - Wrap extraction and processing in try/finally
  - Delete temp directory in finally block
  - Log warning if cleanup fails

- [x] 1.3 Add temp cleanup to DiffCommand
  - Wrap extraction and processing in try/finally for both temp directories
  - Delete both temp directories in finally block
  - Log warning if cleanup fails

## 2. Testing

- [x] 2.1 Add E2E test for ListTypesCommand temp cleanup
  - Verify no temp directory remains after successful execution
  - Verify cleanup occurs on error/exception

- [x] 2.2 Add E2E test for ExportSignaturesCommand temp cleanup
  - Verify no temp directory remains after successful execution
  - Verify cleanup occurs on error/exception

- [x] 2.3 Add E2E test for DiffCommand temp cleanup
  - Verify no temp directories remain after successful execution
  - Verify cleanup occurs on error/exception

## 3. Verification

- [x] 3.1 Run all tests and verify they pass
- [x] 3.2 Run build and verify no errors/warnings
