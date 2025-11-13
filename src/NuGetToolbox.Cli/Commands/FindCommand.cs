using System.CommandLine;

namespace NuGetToolbox.Cli.Commands;

/// <summary>
/// Find command: Resolve a NuGet package by ID and optional version.
/// </summary>
public static class FindCommand
{
    public static Command Create()
    {
        var packageOption = new Option<string>("--package", "-p")
        {
            Description = "Package ID to search for",
            Required = true
        };

        var versionOption = new Option<string?>("--version", "-v")
        {
            Description = "Package version (if omitted, uses latest)"
        };

        var feedOption = new Option<string?>("--feed", "-f")
        {
            Description = "NuGet feed URL (default: nuget.org)"
        };

        var outputOption = new Option<string?>("--output", "-o")
        {
            Description = "Output file path (default: stdout)"
        };

        var command = new Command("find", "Resolve a NuGet package by ID and version")
        {
            packageOption,
            versionOption,
            feedOption,
            outputOption
        };

        command.SetAction(Handler);
        return command;

        int Handler(ParseResult parseResult)
        {
            var package = parseResult.GetValue(packageOption);
            var version = parseResult.GetValue(versionOption);
            var feed = parseResult.GetValue(feedOption);
            var output = parseResult.GetValue(outputOption);

            throw new NotImplementedException();
        }
    }
}
