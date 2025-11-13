# Add E2E Tests Proposal

## Why
End-to-end tests verify that all CLI commands work correctly with actual NuGet packages, ensuring output format stability and correct integration with NuGet V3 API, MetadataLoadContext, and JSON serialization.

## What Changes
- Add E2E test class for each CLI command (find, list-types, export-signatures, diff)
- Use `Newtonsoft.Json` version 13.0.1 as the test subject (stable, well-documented, widely-used package)
- Validate JSON output structure and content against expected schemas
- Ensure tests run quickly (<5 seconds) by caching package downloads

## Impact
- Affected specs: new capability `e2e-testing`
- Affected code: `tests/NuGetToolbox.Tests/` (new test files)
- Dependencies: xUnit, existing CLI commands
