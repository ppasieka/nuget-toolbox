using System.Diagnostics;
using System.Text.Json;

namespace NuGetToolbox.Tests;

public class DeterministicOutputTests
{
    private readonly string _cliPath;

    public DeterministicOutputTests()
    {
        _cliPath = CliHelper.GetCliPath();
    }

    [Fact]
    public async Task ListTypes_MultipleRuns_ProduceIdenticalOutput()
    {
        // Run the command twice
        var output1 = await RunCliAsync("list-types", "--package", "Newtonsoft.Json", "--version", "13.0.1", "--tfm", "netstandard2.0");
        var output2 = await RunCliAsync("list-types", "--package", "Newtonsoft.Json", "--version", "13.0.1", "--tfm", "netstandard2.0");

        // Output should be byte-for-byte identical
        Assert.Equal(output1, output2);
    }

    [Fact]
    public async Task ExportSignatures_MultipleRuns_ProduceIdenticalOutput()
    {
        // Run with a namespace filter to keep output manageable
        var output1 = await RunCliAsync("export-signatures", "--package", "Newtonsoft.Json", "--version", "13.0.1", "--tfm", "netstandard2.0", "--filter", "Newtonsoft.Json.Linq");
        var output2 = await RunCliAsync("export-signatures", "--package", "Newtonsoft.Json", "--version", "13.0.1", "--tfm", "netstandard2.0", "--filter", "Newtonsoft.Json.Linq");

        // Output should be byte-for-byte identical
        Assert.Equal(output1, output2);
    }

    [Fact]
    public async Task ListTypes_OutputIsSortedByNamespaceThenName()
    {
        var output = await RunCliAsync("list-types", "--package", "Newtonsoft.Json", "--version", "13.0.1", "--tfm", "netstandard2.0");

        // Parse JSON output
        var types = JsonSerializer.Deserialize<List<TypeInfoDto>>(output);
        Assert.NotNull(types);
        Assert.True(types.Count > 1, "Should have multiple types");

        // Verify sorting: each type should be >= the previous one
        for (int i = 1; i < types.Count; i++)
        {
            var prev = types[i - 1];
            var curr = types[i];

            var cmp = string.Compare(prev.Namespace, curr.Namespace, StringComparison.Ordinal);
            if (cmp == 0)
            {
                cmp = string.Compare(prev.Name, curr.Name, StringComparison.Ordinal);
            }

            Assert.True(cmp <= 0, $"Types not sorted: {prev.Namespace}.{prev.Name} should come before {curr.Namespace}.{curr.Name}");
        }
    }

    private async Task<string> RunCliAsync(params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{_cliPath}\" {string.Join(" ", args)}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        Assert.NotNull(process);

        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        Assert.Equal(0, process.ExitCode);
        return output;
    }

    private record TypeInfoDto(string? Namespace, string? Name, string? Kind);
}
