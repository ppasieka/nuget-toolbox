## ADDED Requirements

### Requirement: Centralized Dependency Injection

The CLI SHALL configure all service dependencies in Program.cs and pass the `IServiceProvider` to each command's `Create()` method. Commands SHALL NOT create their own `ServiceProvider` instances.

#### Scenario: Program.cs configures DI container
- **WHEN** the CLI application starts
- **THEN** Program.cs creates a `ServiceCollection`
- **AND** registers logging, NuGetPackageResolver, AssemblyInspector, XmlDocumentationProvider, SignatureExporter, and ApiDiffAnalyzer
- **AND** builds a `ServiceProvider`
- **AND** passes the `ServiceProvider` to each command's `Create()` method

#### Scenario: Command receives required IServiceProvider
- **WHEN** a command's `Create()` method is called
- **THEN** it receives a non-null `IServiceProvider` parameter
- **AND** the command uses `GetRequiredService<T>()` to resolve dependencies

#### Scenario: No fallback ServiceProvider in commands
- **WHEN** a command handler executes
- **THEN** it does NOT contain any `CreateDefaultServiceProvider()` method
- **AND** it does NOT use null-coalescing (`??`) to create a fallback provider

### Requirement: Correct Service Lifetimes

The CLI SHALL register services with appropriate lifetimes to prevent memory leaks and ensure correct behavior.

#### Scenario: Singleton services
- **WHEN** DI container is configured
- **THEN** `NuGetPackageResolver` is registered as Singleton
- **AND** `ILoggerFactory` is registered as Singleton (default .NET behavior)

#### Scenario: Transient services
- **WHEN** DI container is configured
- **THEN** `AssemblyInspector` is registered as Transient
- **AND** `XmlDocumentationProvider` is registered as Transient
- **AND** `SignatureExporter` is registered as Transient
- **AND** `ApiDiffAnalyzer` is registered as Transient

#### Scenario: No Scoped services without scope
- **WHEN** DI container is configured
- **THEN** no services are registered as Scoped
- **OR** if Scoped services exist, they are resolved within a scope created per-command execution

### Requirement: Testable DI Configuration

The CLI DI configuration SHALL support unit testing by allowing mock service injection.

#### Scenario: Test with mock services
- **WHEN** a unit test creates a command with a test `IServiceProvider`
- **THEN** the command uses the injected mock services
- **AND** no real NuGet network calls are made

#### Scenario: E2E tests use real services
- **WHEN** an E2E test runs the CLI via Process.Start
- **THEN** the CLI uses the production `IServiceProvider` configured in Program.cs
