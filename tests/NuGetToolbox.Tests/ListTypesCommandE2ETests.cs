using System.Diagnostics;
using System.Text.Json;
using NuGetToolbox.Cli.Models;

namespace NuGetToolbox.Tests;

public class ListTypesCommandE2ETests
{
    private const string CliPath = "c:\\dev\\app\\nuget-toolbox\\src\\NuGetToolbox.Cli\\bin\\Debug\\net8.0\\NuGetToolbox.Cli.dll";

    [Fact]
    public async Task ListTypes_NewtonsoftJson_13_0_1_ReturnsAtLeast50Types()
    {
        // Arrange
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"{CliPath} list-types --package Newtonsoft.Json --version 13.0.1",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Act
        using var process = Process.Start(startInfo)!;
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        // Assert
        Assert.Equal(0, process.ExitCode);
        Assert.NotEmpty(output);

        var types = JsonSerializer.Deserialize<List<TypeInfo>>(output);
        Assert.NotNull(types);
        Assert.True(types.Count >= 50, $"Expected at least 50 types, got {types.Count}");
    }

    [Fact]
    public async Task ListTypes_NewtonsoftJson_ValidatesTypeStructure()
    {
        // Arrange
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"{CliPath} list-types --package Newtonsoft.Json --version 13.0.1",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Act
        using var process = Process.Start(startInfo)!;
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        // Assert
        var types = JsonSerializer.Deserialize<List<TypeInfo>>(output);
        Assert.NotNull(types);
        Assert.All(types, type =>
        {
            Assert.NotNull(type.Namespace);
            Assert.NotEmpty(type.Namespace);
            Assert.NotNull(type.Name);
            Assert.NotEmpty(type.Name);
            Assert.NotNull(type.Kind);
            Assert.Contains(type.Kind, new[] { "class", "interface", "struct", "enum" });
        });
    }

    [Fact]
    public async Task ListTypes_NewtonsoftJson_ContainsJsonConvert()
    {
        // Arrange
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"{CliPath} list-types --package Newtonsoft.Json --version 13.0.1",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Act
        using var process = Process.Start(startInfo)!;
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        // Assert
        var types = JsonSerializer.Deserialize<List<TypeInfo>>(output);
        Assert.NotNull(types);
        var jsonConvert = types.FirstOrDefault(t => t.Name == "JsonConvert" && t.Namespace == "Newtonsoft.Json");
        Assert.NotNull(jsonConvert);
        Assert.Equal("class", jsonConvert.Kind);
    }
}
