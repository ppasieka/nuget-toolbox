using System.Diagnostics;
using System.Text.Json;
using Xunit.Abstractions;

namespace NuGetToolbox.Tests;

/// <summary>
/// E2E tests verifying stdout purity for the schema command.
/// These tests ensure schema output is pure JSON, suitable for piping to tools like jq.
/// </summary>
public class SchemaCommandE2ETests
{
    private readonly string _cliPath;
    private readonly ITestOutputHelper _output;

    public SchemaCommandE2ETests(ITestOutputHelper output)
    {
        _output = output;
        _cliPath = CliHelper.GetCliPath();
    }

    [Fact]
    public async Task Schema_SingleCommand_OutputsValidJsonSchema()
    {
        // Arrange & Act
        var (stdout, stderr, exitCode) = await RunCliAsync("schema --command find");

        // Assert
        Assert.Equal(0, exitCode);
        Assert.NotEmpty(stdout);

        // Stdout must be valid JSON
        using var jsonDoc = JsonDocument.Parse(stdout);
        Assert.NotNull(jsonDoc);

        // Must have $schema property (JSON Schema requirement)
        Assert.True(
            jsonDoc.RootElement.TryGetProperty("$schema", out var schemaProperty),
            "JSON Schema output must contain $schema property");
        Assert.NotNull(schemaProperty.GetString());

        // Stdout should not contain informational messages
        Assert.DoesNotContain("Wrote", stdout);
        Assert.DoesNotContain("---", stdout);
        Assert.DoesNotContain("info:", stdout, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Schema_AllCommands_ToDirectory_OnlyWritesToFiles()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"schema-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            var (stdout, stderr, exitCode) = await RunCliAsync($"schema --all --output \"{tempDir}\"");

            // Assert
            Assert.Equal(0, exitCode);

            // Stdout should be empty or minimal (no JSON schemas written to stdout)
            var trimmedStdout = stdout.Trim();
            if (!string.IsNullOrEmpty(trimmedStdout))
            {
                // If there's any stdout, it should not be valid JSON Schema
                // (informational messages are OK in stderr, not stdout)
                _output.WriteLine($"Stdout was: '{trimmedStdout}'");
                Assert.DoesNotContain("$schema", trimmedStdout);
            }

            // Files should be created in the output directory
            var schemaFiles = Directory.GetFiles(tempDir, "*.json");
            Assert.NotEmpty(schemaFiles);
            _output.WriteLine($"Created {schemaFiles.Length} schema files in {tempDir}");

            // Each file should be valid JSON Schema
            foreach (var file in schemaFiles)
            {
                var content = await File.ReadAllTextAsync(file);
                using var jsonDoc = JsonDocument.Parse(content);
                Assert.True(
                    jsonDoc.RootElement.TryGetProperty("$schema", out _),
                    $"File {Path.GetFileName(file)} should be valid JSON Schema");
            }
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Schema_SingleCommand_StdoutIsPipeableToJsonParser()
    {
        // Arrange & Act
        var (stdout, stderr, exitCode) = await RunCliAsync("schema --command find");

        // Assert
        Assert.Equal(0, exitCode);

        // Simulate what `| jq .` would do - parse the entire stdout as JSON
        using var jsonDoc = JsonDocument.Parse(stdout);
        Assert.NotNull(jsonDoc);

        // Verify we can extract schema properties (simulating jq queries)
        var schema = jsonDoc.RootElement.GetProperty("$schema").GetString();
        Assert.Contains("json-schema.org", schema!);

        // Stdout should start with JSON (no prefix messages)
        var trimmed = stdout.TrimStart();
        Assert.True(
            trimmed.StartsWith('{'),
            $"Stdout should start with JSON object. Got: {trimmed[..Math.Min(50, trimmed.Length)]}...");
    }

    [Theory]
    [InlineData("find")]
    [InlineData("list-types")]
    [InlineData("export-signatures")]
    [InlineData("diff")]
    public async Task Schema_EachCommand_OutputsValidJsonSchema(string commandName)
    {
        // Arrange & Act
        var (stdout, stderr, exitCode) = await RunCliAsync($"schema --command {commandName}");

        // Assert
        Assert.Equal(0, exitCode);
        Assert.NotEmpty(stdout);

        // Parse as JSON
        using var jsonDoc = JsonDocument.Parse(stdout);
        Assert.True(
            jsonDoc.RootElement.TryGetProperty("$schema", out _),
            $"Schema for '{commandName}' should have $schema property");
    }

    [Fact]
    public async Task Schema_StdoutContainsNoLogPrefixes()
    {
        // Arrange & Act
        var (stdout, stderr, exitCode) = await RunCliAsync("schema --command find");

        // Assert
        Assert.Equal(0, exitCode);

        // Stdout should not contain log prefixes
        Assert.DoesNotContain("info:", stdout, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("warn:", stdout, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("dbug:", stdout, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fail:", stdout, StringComparison.OrdinalIgnoreCase);
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

        _output.WriteLine($"Exit code: {process.ExitCode}");
        if (!string.IsNullOrEmpty(stderr))
        {
            _output.WriteLine($"Stderr: {stderr}");
        }

        return (stdout, stderr, process.ExitCode);
    }
}
