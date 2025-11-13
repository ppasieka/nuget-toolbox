using System.CommandLine;

namespace NuGetToolbox.Cli.Commands;

/// <summary>
/// ListTypes command: List public types from a NuGet package.
/// </summary>
public static class ListTypesCommand
{
    public static Command Create()
    {
        var packageOption = new Option<string>("--package", "-p")
        {
            Description = "Package ID",
            Required = true
        };

        var versionOption = new Option<string?>("--version", "-v")
        {
            Description = "Package version (if omitted, uses latest)"
        };

        var tfmOption = new Option<string?>("--tfm")
        {
            Description = "Target framework moniker (e.g., net8.0, netstandard2.0)"
        };

        var outputOption = new Option<string?>("--output", "-o")
        {
            Description = "Output file path (default: stdout)"
        };

        var command = new Command("list-types", "List public types (classes, interfaces, structs, enums) from a package")
        {
            packageOption,
            versionOption,
            tfmOption,
            outputOption
        };

        command.SetAction(Handler);
        return command;

        int Handler(ParseResult parseResult)
        {
            var package = parseResult.GetValue(packageOption);
            var version = parseResult.GetValue(versionOption);
            var tfm = parseResult.GetValue(tfmOption);
            var output = parseResult.GetValue(outputOption);

            throw new NotImplementedException();
        }
    }
}
