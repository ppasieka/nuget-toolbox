using Microsoft.Extensions.Logging.Abstractions;
using NuGet.Frameworks;
using NuGetToolbox.Cli.Services;

namespace NuGetToolbox.Tests;

public class FrameworkSelectorTests
{
    private readonly FrameworkSelector _selector;

    public FrameworkSelectorTests()
    {
        _selector = new FrameworkSelector(NullLogger<FrameworkSelector>.Instance);
    }

    [Fact]
    public void SelectNearest_WhenNet8Runtime_SelectsNetstandard20OverNet48()
    {
        // Arrange - This is the key P1 bug fix
        var target = NuGetFramework.Parse("net8.0");
        var available = new[]
        {
            NuGetFramework.Parse("net48"),
            NuGetFramework.Parse("netstandard2.0")
        };

        // Act
        var result = _selector.SelectNearest(target, available);

        // Assert - Should select netstandard2.0, NOT net48
        Assert.NotNull(result);
        Assert.Equal("netstandard2.0", result.GetShortFolderName());
    }

    [Fact]
    public void SelectNearest_WhenExactMatchExists_SelectsExactMatch()
    {
        // Arrange
        var target = NuGetFramework.Parse("net8.0");
        var available = new[]
        {
            NuGetFramework.Parse("net8.0"),
            NuGetFramework.Parse("net6.0"),
            NuGetFramework.Parse("netstandard2.0")
        };

        // Act
        var result = _selector.SelectNearest(target, available);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("net8.0", result.GetShortFolderName());
    }

    [Fact]
    public void SelectNearest_WhenNoExactMatch_SelectsNearestCompatible()
    {
        // Arrange
        var target = NuGetFramework.Parse("net8.0");
        var available = new[]
        {
            NuGetFramework.Parse("net6.0"),
            NuGetFramework.Parse("netstandard2.1")
        };

        // Act
        var result = _selector.SelectNearest(target, available);

        // Assert - net6.0 is more compatible than netstandard2.1 for net8.0
        Assert.NotNull(result);
        Assert.Equal("net6.0", result.GetShortFolderName());
    }

    [Fact]
    public void SelectNearest_WhenNoCompatibleFramework_ReturnsNull()
    {
        // Arrange
        var target = NuGetFramework.Parse("net48");
        var available = new[]
        {
            NuGetFramework.Parse("net6.0"),  // Not compatible with .NET Framework
            NuGetFramework.Parse("net8.0")
        };

        // Act
        var result = _selector.SelectNearest(target, available);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void SelectNearest_WhenEmptyList_ReturnsNull()
    {
        // Arrange
        var target = NuGetFramework.Parse("net8.0");
        var available = Array.Empty<NuGetFramework>();

        // Act
        var result = _selector.SelectNearest(target, available);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetAvailableTfms_ReturnsSortedList()
    {
        // Arrange
        var frameworks = new[]
        {
            NuGetFramework.Parse("net6.0"),
            NuGetFramework.Parse("netstandard2.0"),
            NuGetFramework.Parse("net48")
        };

        // Act
        var result = _selector.GetAvailableTfms(frameworks);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(["net48", "net6.0", "netstandard2.0"], result);
    }
}
