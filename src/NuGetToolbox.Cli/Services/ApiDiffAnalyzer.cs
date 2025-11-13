using Microsoft.Extensions.Logging;
using NuGetToolbox.Cli.Models;

namespace NuGetToolbox.Cli.Services;

/// <summary>
/// Analyzes API differences between two package versions.
/// </summary>
public class ApiDiffAnalyzer
{
    private readonly ILogger<ApiDiffAnalyzer> _logger;

    public ApiDiffAnalyzer(ILogger<ApiDiffAnalyzer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Compares public APIs between two versions.
    /// </summary>
    public DiffResult CompareVersions(
        string packageId,
        List<MethodInfo> methodsFrom,
        List<MethodInfo> methodsTo,
        string versionFrom,
        string versionTo,
        string tfm)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Identifies breaking changes between two API sets.
    /// </summary>
    private List<DiffItem> IdentifyBreakingChanges(List<MethodInfo> from, List<MethodInfo> to)
    {
        throw new NotImplementedException();
    }
}
