using Microsoft.Extensions.DependencyInjection;
using NuGetToolbox.Cli.Commands;

namespace NuGetToolbox.Tests;

public class ExportSignaturesCommandTests
{
    private static IServiceProvider CreateTestServiceProvider()
    {
        var services = new ServiceCollection();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void Create_CommandHasFilterOptionWithNamespaceAlias()
    {
        // Arrange & Act
        var command = ExportSignaturesCommand.Create(CreateTestServiceProvider());

        // Assert
        Assert.NotNull(command);
        Assert.Equal("export-signatures", command.Name);

        var filterOption = command.Options.FirstOrDefault(o => o.Name == "filter" || o.Name == "--filter");
        Assert.NotNull(filterOption);
        // Assert.Equal("--filter", filterOption.Name); // Name might be "filter" now
        Assert.Contains("--namespace", filterOption.Aliases);
        Assert.Equal("Namespace filter (e.g., Newtonsoft.Json.Linq)", filterOption.Description);
    }

    [Fact]
    public void Create_CommandHasAllExpectedOptions()
    {
        // Arrange & Act
        var command = ExportSignaturesCommand.Create(CreateTestServiceProvider());

        // Assert
        Assert.NotNull(command);

        // Check that all expected options exist by their names and aliases
        var allNames = command.Options.Select(o => o.Name).ToHashSet();
        var allAliases = command.Options.SelectMany(o => o.Aliases).ToHashSet();

        // Check names (System.CommandLine 2.0+ usually normalizes names by removing prefixes)
        Assert.True(allNames.Contains("package") || allNames.Contains("--package"));
        Assert.True(allNames.Contains("version") || allNames.Contains("--version"));
        Assert.True(allNames.Contains("tfm") || allNames.Contains("--tfm"));
        Assert.True(allNames.Contains("format") || allNames.Contains("--format"));
        Assert.True(allNames.Contains("filter") || allNames.Contains("--filter"));
        Assert.True(allNames.Contains("output") || allNames.Contains("--output"));

        // Check specific aliases
        Assert.Contains("-p", allAliases);
        Assert.Contains("-v", allAliases);
        Assert.Contains("--namespace", allAliases);
        Assert.Contains("-o", allAliases);
    }

    [Fact]
    public void Create_CommandHasCorrectDescription()
    {
        // Arrange & Act
        var command = ExportSignaturesCommand.Create(CreateTestServiceProvider());

        // Assert
        Assert.Equal("Export public method signatures with XML documentation", command.Description);
    }

    [Fact]
    public void Create_FormatOptionHasFromAmongConstraint()
    {
        // Arrange & Act
        var command = ExportSignaturesCommand.Create(CreateTestServiceProvider());

        // Assert
        var formatOption = command.Options.FirstOrDefault(o => o.Name == "format" || o.Name == "--format");
        Assert.NotNull(formatOption);

        // Check completions contain exactly json and jsonl
        var completions = formatOption.GetCompletions().Select(c => c.Label).ToList();
        Assert.Contains("json", completions);
        Assert.Contains("jsonl", completions);
        Assert.Equal(2, completions.Count);
    }

    [Fact]
    public void Create_NoCacheOptionRemoved()
    {
        // Arrange & Act
        var command = ExportSignaturesCommand.Create(CreateTestServiceProvider());

        // Assert
        var allNames = command.Options.Select(o => o.Name).ToHashSet();
        var allAliases = command.Options.SelectMany(o => o.Aliases).ToHashSet();

        Assert.DoesNotContain("no-cache", allNames);
        Assert.DoesNotContain("--no-cache", allNames);
        Assert.DoesNotContain("--no-cache", allAliases);
    }
}