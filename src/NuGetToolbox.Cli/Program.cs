using System.CommandLine;
using System.CommandLine.Parsing;
using NuGetToolbox.Cli.Commands;

var rootCommand = new RootCommand("NuGet Toolbox - Inspect NuGet package public APIs and extract method signatures");

// Add commands
rootCommand.AddCommand(FindCommand.Create());
rootCommand.AddCommand(ListTypesCommand.Create());
rootCommand.AddCommand(ExportSignaturesCommand.Create());
rootCommand.AddCommand(DiffCommand.Create());
rootCommand.AddCommand(SchemaCommand.Create());

return await rootCommand.InvokeAsync(args);
