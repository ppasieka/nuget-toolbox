using System.Reflection;
using Microsoft.Extensions.Logging;
using NuGetToolbox.Cli.Models;

namespace NuGetToolbox.Cli.Services;

/// <summary>
/// Inspects assemblies using MetadataLoadContext for safe metadata-only inspection.
/// Never loads code into the default context.
/// </summary>
public class AssemblyInspector
{
    private readonly ILogger<AssemblyInspector> _logger;

    public AssemblyInspector(ILogger<AssemblyInspector> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Extracts public types from assembly files.
    /// </summary>
    public List<Models.TypeInfo> ExtractPublicTypes(string assemblyPath)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Extracts public types from multiple assembly files.
    /// </summary>
    public List<Models.TypeInfo> ExtractPublicTypesFromMultiple(params string[] assemblyPaths)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets all public members (methods, properties, etc.) from a type.
    /// </summary>
    public IEnumerable<MemberInfo> GetPublicMembers(Type type)
    {
        throw new NotImplementedException();
    }
}
