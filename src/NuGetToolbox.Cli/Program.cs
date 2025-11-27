using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NuGetToolbox.Cli.Commands;
using NuGetToolbox.Cli.Services;

// Configure centralized DI
var services = new ServiceCollection();

// Logging: write to stderr via file logging (keeps stdout clean for JSON)
services.AddLogging(builder =>
{
    var logDir = Path.Combine(Path.GetTempPath(), "nuget-toolbox", "logs");
    Directory.CreateDirectory(logDir);
    var logFile = Path.Combine(logDir, $"nuget-toolbox-{DateTime.UtcNow:yyyyMMdd}.log");
    builder.AddFile(logFile, minimumLevel: LogLevel.Debug);
});

// Service registrations with correct lifetimes
services.AddSingleton<NuGetPackageResolver>();  // Stateless, reusable
services.AddTransient<AssemblyInspector>();     // Uses MetadataLoadContext per-call
services.AddTransient<XmlDocumentationProvider>(); // Loads docs per-assembly
services.AddTransient<SignatureExporter>();     // Per-operation processing
services.AddTransient<ApiDiffAnalyzer>();       // Per-comparison processing

var serviceProvider = services.BuildServiceProvider();

var rootCommand = new RootCommand("NuGet Toolbox - Inspect NuGet package public APIs and extract method signatures");

// Add commands with centralized DI
rootCommand.AddCommand(FindCommand.Create(serviceProvider));
rootCommand.AddCommand(ListTypesCommand.Create(serviceProvider));
rootCommand.AddCommand(ExportSignaturesCommand.Create(serviceProvider));
rootCommand.AddCommand(DiffCommand.Create(serviceProvider));
rootCommand.AddCommand(SchemaCommand.Create(serviceProvider));

return await rootCommand.InvokeAsync(args);
