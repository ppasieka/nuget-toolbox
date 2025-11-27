using System.CommandLine;
using System.CommandLine.Invocation;
using System.Reflection;

namespace NuGetToolbox.Cli.Commands;

/// <summary>
/// Schema command: Export JSON Schema definitions for CLI command outputs.
/// </summary>
public static class SchemaCommand
{
    private static readonly string[] ValidCommands = ["find", "list-types", "export-signatures", "diff", "models"];
    private static readonly Dictionary<string, string> SchemaResourceNames = new()
    {
        { "find", "NuGetToolbox.Cli.Schemas.find.schema.json" },
        { "list-types", "NuGetToolbox.Cli.Schemas.list-types.schema.json" },
        { "export-signatures", "NuGetToolbox.Cli.Schemas.export-signatures.schema.json" },
        { "diff", "NuGetToolbox.Cli.Schemas.diff.schema.json" },
        { "models", "NuGetToolbox.Cli.Schemas.models-1.0.schema.json" }
    };

    public static Command Create(IServiceProvider _)
    {
        var commandOption = new Option<string?>("--command", "-c")
        {
            Description = $"Command name to export schema for ({string.Join(", ", ValidCommands)})"
        };

        var allOption = new Option<bool>("--all", "-a")
        {
            Description = "Export all schemas (models + all commands)"
        };

        var outputOption = new Option<string?>("--output", "-o")
        {
            Description = "Output file or directory path (default: stdout)"
        };

        var command = new Command("schema", "Export JSON Schema definitions for command outputs")
        {
            commandOption,
            allOption,
            outputOption
        };

        command.SetHandler(async (InvocationContext ctx) =>
        {
            var commandName = ctx.ParseResult.GetValueForOption(commandOption);
            var all = ctx.ParseResult.GetValueForOption(allOption);
            var output = ctx.ParseResult.GetValueForOption(outputOption);
            var cancellationToken = ctx.GetCancellationToken();

            try
            {
                // Task 1.1: Enforce mutual exclusivity between --command and --all
                if (!string.IsNullOrEmpty(commandName) && all)
                {
                    Console.Error.WriteLine("Error: --command and --all are mutually exclusive");
                    ctx.ExitCode = ExitCodes.InvalidOptions;
                    return;
                }

                // Task 1.2: Validate --output with --all is a directory path
                if (all && !string.IsNullOrEmpty(output))
                {
                    var isDirectoryPath = output.EndsWith('/') || output.EndsWith('\\') || Directory.Exists(output);
                    if (!isDirectoryPath)
                    {
                        Console.Error.WriteLine("Error: --output with --all must be a directory path");
                        ctx.ExitCode = ExitCodes.InvalidOptions;
                        return;
                    }
                }

                if (all)
                {
                    ctx.ExitCode = await HandleAllSchemasAsync(output, cancellationToken);
                    return;
                }

                if (string.IsNullOrEmpty(commandName))
                {
                    // Default: export models schema
                    ctx.ExitCode = await ExportSchemaAsync("models", output, cancellationToken);
                    return;
                }

                // Task 1.3: Case-insensitive command name matching
                if (!ValidCommands.Contains(commandName, StringComparer.OrdinalIgnoreCase))
                {
                    Console.Error.WriteLine($"Error: Invalid command name '{commandName}'");
                    Console.Error.WriteLine($"Valid commands: {string.Join(", ", ValidCommands)}");
                    ctx.ExitCode = ExitCodes.InvalidOptions;
                    return;
                }

                commandName = commandName.ToLowerInvariant();
                ctx.ExitCode = await ExportSchemaAsync(commandName, output, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                ctx.ExitCode = ExitCodes.Error;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                ctx.ExitCode = ExitCodes.Error;
            }
        });

        return command;
    }

    private static async Task<int> HandleAllSchemasAsync(string? outputPath, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(outputPath))
        {
            // If output is specified, write to directory
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            foreach (var (commandName, _) in SchemaResourceNames)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var fileName = commandName == "models"
                    ? "models-1.0.schema.json"
                    : $"{commandName}.schema.json";
                var filePath = Path.Combine(outputPath, fileName);

                var schema = await LoadSchemaResourceAsync(commandName, cancellationToken);
                if (schema == null)
                {
                    Console.Error.WriteLine($"Warning: Could not load schema for '{commandName}'");
                    continue;
                }

                await File.WriteAllTextAsync(filePath, schema, cancellationToken);
                Console.Error.WriteLine($"Wrote {fileName}");
            }

            return ExitCodes.Success;
        }
        else
        {
            // Write all schemas to stdout as separate JSON documents
            foreach (var (commandName, _) in SchemaResourceNames)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var schema = await LoadSchemaResourceAsync(commandName, cancellationToken);
                if (schema == null)
                {
                    Console.Error.WriteLine($"Warning: Could not load schema for '{commandName}'");
                    continue;
                }

                Console.Error.WriteLine($"--- {commandName} ---");
                Console.WriteLine(schema);
                Console.Error.WriteLine();
            }

            return ExitCodes.Success;
        }
    }

    private static async Task<int> ExportSchemaAsync(string commandName, string? outputPath, CancellationToken cancellationToken)
    {
        var schema = await LoadSchemaResourceAsync(commandName, cancellationToken);
        if (schema == null)
        {
            Console.Error.WriteLine($"Error: Could not load schema resource for '{commandName}'");
            return ExitCodes.Error;
        }

        if (!string.IsNullOrEmpty(outputPath))
        {
            // Write to file
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(outputPath, schema, cancellationToken);
            Console.Error.WriteLine($"Wrote schema to {outputPath}");
        }
        else
        {
            // Write to stdout
            Console.WriteLine(schema);
        }

        return ExitCodes.Success;
    }

    private static async Task<string?> LoadSchemaResourceAsync(string commandName, CancellationToken cancellationToken)
    {
        if (!SchemaResourceNames.TryGetValue(commandName, out var resourceName))
        {
            return null;
        }

        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream == null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
