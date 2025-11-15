using NuGetToolbox.Cli.Commands;

namespace NuGetToolbox.Tests;

public class ExportSignaturesCommandTests
{
    [Fact]
    public void Create_CommandHasFilterOptionWithNamespaceAlias()
    {
        // Arrange & Act
        var command = ExportSignaturesCommand.Create();
        
        // Assert
        Assert.NotNull(command);
        Assert.Equal("export-signatures", command.Name);
        
        var filterOption = command.Options.FirstOrDefault(o => o.Name == "--filter");
        Assert.NotNull(filterOption);
        Assert.Equal("--filter", filterOption.Name);
        Assert.Contains("--namespace", filterOption.Aliases);
        Assert.Equal("Namespace filter (e.g., Newtonsoft.Json.Linq)", filterOption.Description);
    }

    [Fact]
    public void Create_CommandHasAllExpectedOptions()
    {
        // Arrange & Act
        var command = ExportSignaturesCommand.Create();
        
        // Assert
        Assert.NotNull(command);
        
        // Check that all expected options exist by their names and aliases
        var allNames = command.Options.Select(o => o.Name).ToHashSet();
        var allAliases = command.Options.SelectMany(o => o.Aliases).ToHashSet();
        
        Assert.Contains("--package", allNames);
        Assert.Contains("--version", allNames);
        Assert.Contains("--tfm", allNames);
        Assert.Contains("--format", allNames);
        Assert.Contains("--filter", allNames);
        Assert.Contains("--output", allNames);
        Assert.Contains("--no-cache", allNames);
        
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
        var command = ExportSignaturesCommand.Create();
        
        // Assert
        Assert.Equal("Export public method signatures with XML documentation", command.Description);
    }
}