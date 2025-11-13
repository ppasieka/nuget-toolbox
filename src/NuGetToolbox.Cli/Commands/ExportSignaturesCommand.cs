using System.CommandLine;

namespace NuGetToolbox.Cli.Commands;

/// <summary>
/// ExportSignatures command: Export public method signatures with XML documentation.
/// </summary>
public static class ExportSignaturesCommand
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

        var formatOption = new Option<string>("--format")
        {
            Description = "Output format: json or jsonl",
            DefaultValueFactory = _ => "json"
        };

        var filterOption = new Option<string?>("--filter")
        {
            Description = "Namespace filter (e.g., Newtonsoft.Json.Linq)"
        };

        var outputOption = new Option<string?>("--output", "-o")
        {
            Description = "Output file path (default: stdout)"
        };

        var noCacheOption = new Option<bool>("--no-cache")
        {
            Description = "Bypass cache",
            DefaultValueFactory = _ => false
        };

        var command = new Command("export-signatures", "Export public method signatures with XML documentation")
        {
            packageOption,
            versionOption,
            tfmOption,
            formatOption,
            filterOption,
            outputOption,
            noCacheOption
        };

        command.SetAction(Handler);
        return command;

        int Handler(ParseResult parseResult)
        {
            var package = parseResult.GetValue(packageOption);
            var version = parseResult.GetValue(versionOption);
            var tfm = parseResult.GetValue(tfmOption);
            var format = parseResult.GetValue(formatOption) ?? "json";
            var filter = parseResult.GetValue(filterOption);
            var output = parseResult.GetValue(outputOption);
            var noCache = parseResult.GetValue(noCacheOption);

            throw new NotImplementedException();
        }
    }
}
