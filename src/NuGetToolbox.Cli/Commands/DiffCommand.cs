using System.CommandLine;

namespace NuGetToolbox.Cli.Commands;

/// <summary>
/// Diff command: Compare public API between two package versions.
/// </summary>
public static class DiffCommand
{
    public static Command Create()
    {
        var packageOption = new Option<string>("--package", "-p")
        {
            Description = "Package ID",
            Required = true
        };

        var fromOption = new Option<string>("--from")
        {
            Description = "From version",
            Required = true
        };

        var toOption = new Option<string>("--to")
        {
            Description = "To version",
            Required = true
        };

        var tfmOption = new Option<string?>("--tfm")
        {
            Description = "Target framework moniker (e.g., net8.0, netstandard2.0)"
        };

        var outputOption = new Option<string?>("--output", "-o")
        {
            Description = "Output file path (default: stdout)"
        };

        var command = new Command("diff", "Compare public API between two package versions")
        {
            packageOption,
            fromOption,
            toOption,
            tfmOption,
            outputOption
        };

        command.SetAction(Handler);
        return command;

        int Handler(ParseResult parseResult)
        {
            var package = parseResult.GetValue(packageOption);
            var from = parseResult.GetValue(fromOption);
            var to = parseResult.GetValue(toOption);
            var tfm = parseResult.GetValue(tfmOption);
            var output = parseResult.GetValue(outputOption);

            throw new NotImplementedException();
        }
    }
}
