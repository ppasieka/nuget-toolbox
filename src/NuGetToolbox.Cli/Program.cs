using System.CommandLine;
using NuGetToolbox.Cli.Commands;

var rootCommand = new RootCommand("NuGet Toolbox - Inspect NuGet package public APIs and extract method signatures");

// Add commands
rootCommand.Subcommands.Add(FindCommand.Create());
rootCommand.Subcommands.Add(ListTypesCommand.Create());
rootCommand.Subcommands.Add(ExportSignaturesCommand.Create());
rootCommand.Subcommands.Add(DiffCommand.Create());

return rootCommand.Parse(args).Invoke();
