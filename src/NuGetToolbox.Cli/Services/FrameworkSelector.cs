using Microsoft.Extensions.Logging;
using NuGet.Frameworks;

namespace NuGetToolbox.Cli.Services;

public class FrameworkSelector
{
    private readonly ILogger<FrameworkSelector> _logger;
    private readonly FrameworkReducer _reducer = new();

    public FrameworkSelector(ILogger<FrameworkSelector> logger)
    {
        _logger = logger;
    }

    public NuGetFramework? SelectNearest(NuGetFramework target, IEnumerable<NuGetFramework> available)
    {
        var frameworks = available.ToList();
        if (frameworks.Count == 0)
            return null;

        var nearest = _reducer.GetNearest(target, frameworks);

        if (nearest != null)
        {
            _logger.LogDebug("Selected {Selected} as nearest for {Target} from {Available}",
                nearest.GetShortFolderName(), target.GetShortFolderName(),
                string.Join(", ", frameworks.Select(f => f.GetShortFolderName())));
        }
        else
        {
            _logger.LogDebug("No compatible framework found for {Target} in {Available}",
                target.GetShortFolderName(),
                string.Join(", ", frameworks.Select(f => f.GetShortFolderName())));
        }

        return nearest;
    }

    public IReadOnlyList<string> GetAvailableTfms(IEnumerable<NuGetFramework> frameworks)
    {
        return frameworks
            .Select(f => f.GetShortFolderName())
            .OrderBy(s => s)
            .ToList();
    }

    public static NuGetFramework GetRuntimeFramework()
    {
        return NuGetFramework.Parse($"net{Environment.Version.Major}.{Environment.Version.Minor}");
    }
}
