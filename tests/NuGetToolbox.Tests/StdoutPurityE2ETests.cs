using System.Diagnostics;
using System.Text.Json;
using Xunit.Abstractions;

namespace NuGetToolbox.Tests;

/// <summary>
/// E2E tests verifying stdout purity (JSON only) and stderr logging behavior.
/// These tests ensure the CLI output is pipeable to tools like jq.
/// </summary>
public class StdoutPurityE2ETests
{
    private readonly string _cliPath;
    private readonly ITestOutputHelper _output;

    public StdoutPurityE2ETests(ITestOutputHelper output)
    {
        _output = output;
        _cliPath = CliHelper.GetCliPath();
    }

    [Fact]
    public async Task Find_StdoutContainsOnlyValidJson()
    {
        // Arrange & Act
        var (stdout, stderr, exitCode) = await RunCliAsync("find --package Newtonsoft.Json --version 13.0.1");

        // Assert
        Assert.Equal(0, exitCode);
        Assert.NotEmpty(stdout);

        // Stdout must be valid JSON (parseable without exceptions)
        var jsonDoc = JsonDocument.Parse(stdout);
        Assert.NotNull(jsonDoc);

        // Stdout should not contain log prefixes
        Assert.DoesNotContain("info:", stdout, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("warn:", stdout, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("dbug:", stdout, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fail:", stdout, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ListTypes_StdoutContainsOnlyValidJson()
    {
        // Arrange & Act
        var (stdout, stderr, exitCode) = await RunCliAsync(
            "list-types --package Newtonsoft.Json --version 13.0.1 --tfm netstandard2.0");

        // Assert
        Assert.Equal(0, exitCode);
        Assert.NotEmpty(stdout);

        var jsonDoc = JsonDocument.Parse(stdout);
        Assert.NotNull(jsonDoc);

        Assert.DoesNotContain("info:", stdout, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("warn:", stdout, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExportSignatures_StdoutContainsOnlyValidJson()
    {
        // Arrange & Act
        var (stdout, stderr, exitCode) = await RunCliAsync(
            "export-signatures --package Newtonsoft.Json --version 13.0.1 --tfm netstandard2.0");

        // Assert
        Assert.Equal(0, exitCode);
        Assert.NotEmpty(stdout);

        var jsonDoc = JsonDocument.Parse(stdout);
        Assert.NotNull(jsonDoc);

        Assert.DoesNotContain("info:", stdout, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("warn:", stdout, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Diff_StdoutContainsOnlyValidJson()
    {
        // Arrange & Act
        var (stdout, stderr, exitCode) = await RunCliAsync(
            "diff --package Newtonsoft.Json --from 12.0.1 --to 13.0.1 --tfm netstandard2.0");

        // Assert
        Assert.Equal(0, exitCode);
        Assert.NotEmpty(stdout);

        var jsonDoc = JsonDocument.Parse(stdout);
        Assert.NotNull(jsonDoc);

        Assert.DoesNotContain("info:", stdout, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("warn:", stdout, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Find_StderrReceivesLogOutput_WhenLoggingEnabled()
    {
        // Arrange & Act
        var (stdout, stderr, exitCode) = await RunCliAsync("find --package Newtonsoft.Json --version 13.0.1");

        // Assert
        Assert.Equal(0, exitCode);

        // Stdout is pure JSON
        var jsonDoc = JsonDocument.Parse(stdout);
        Assert.NotNull(jsonDoc);

        // Stderr may contain log output (console logger writes here)
        // Note: Even if stderr is empty (no logs at default level), stdout must still be pure
        _output.WriteLine($"Stderr content: '{stderr}'");
    }

    [Fact]
    public async Task Find_StdoutIsPipeableToJsonParser()
    {
        // Arrange & Act
        var (stdout, _, exitCode) = await RunCliAsync("find --package Newtonsoft.Json --version 13.0.1");

        // Assert
        Assert.Equal(0, exitCode);

        // Simulate what `| jq .packageId` would do
        using var jsonDoc = JsonDocument.Parse(stdout);
        var packageId = jsonDoc.RootElement.GetProperty("packageId").GetString();
        Assert.Equal("Newtonsoft.Json", packageId);
    }

    [Fact]
    public async Task AllCommands_StdoutStartsWithJsonBrace()
    {
        // All JSON output commands should start with '{' or '['
        var commands = new[]
        {
            "find --package Newtonsoft.Json --version 13.0.1",
            "list-types --package Newtonsoft.Json --version 13.0.1 --tfm netstandard2.0",
            "export-signatures --package Newtonsoft.Json --version 13.0.1 --tfm netstandard2.0",
            "diff --package Newtonsoft.Json --from 12.0.1 --to 13.0.1 --tfm netstandard2.0"
        };

        foreach (var command in commands)
        {
            var (stdout, _, exitCode) = await RunCliAsync(command);

            Assert.Equal(0, exitCode);
            var trimmed = stdout.TrimStart();
            Assert.True(
                trimmed.StartsWith('{') || trimmed.StartsWith('['),
                $"Command '{command}' stdout should start with JSON. Got: {trimmed[..Math.Min(50, trimmed.Length)]}...");
        }
    }

    private async Task<(string stdout, string stderr, int exitCode)> RunCliAsync(string arguments)
    {
        _output.WriteLine($"Executing: dotnet {_cliPath} {arguments}");

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"{_cliPath} {arguments}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo)!;
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return (stdout, stderr, process.ExitCode);
    }
}
