using System.Reflection;
using System.Runtime.InteropServices;
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
        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException($"Assembly not found: {assemblyPath}");
        }

        _logger.LogInformation("Extracting public types from {AssemblyPath}", assemblyPath);

        var runtimeAssemblies = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");
        var paths = new List<string>(runtimeAssemblies) { assemblyPath };
        var resolver = new PathAssemblyResolver(paths);

        using var metadataContext = new MetadataLoadContext(resolver);
        
        try
        {
            var assembly = metadataContext.LoadFromAssemblyPath(assemblyPath);
            var types = new List<Models.TypeInfo>();

            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsPublic)
                    continue;

                var kind = GetTypeKind(type);
                if (kind == null)
                    continue;

                types.Add(new Models.TypeInfo
                {
                    Namespace = type.Namespace ?? string.Empty,
                    Name = type.Name,
                    Kind = kind
                });
            }

            _logger.LogInformation("Extracted {Count} public types from {AssemblyPath}", types.Count, assemblyPath);
            return types;
        }
        catch (BadImageFormatException ex)
        {
            throw new InvalidOperationException($"Invalid assembly format: {assemblyPath}", ex);
        }
    }

    /// <summary>
    /// Extracts public types from multiple assembly files.
    /// </summary>
    public List<Models.TypeInfo> ExtractPublicTypesFromMultiple(params string[] assemblyPaths)
    {
        var allTypes = new List<Models.TypeInfo>();

        foreach (var path in assemblyPaths)
        {
            var types = ExtractPublicTypes(path);
            allTypes.AddRange(types);
        }

        return allTypes;
    }

    /// <summary>
    /// Gets all public members (methods, properties, etc.) from a type.
    /// </summary>
    public IEnumerable<MemberInfo> GetPublicMembers(Type type)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;

        var methods = type.GetMethods(flags);
        var properties = type.GetProperties(flags);
        var fields = type.GetFields(flags);

        return methods.Cast<MemberInfo>()
            .Concat(properties)
            .Concat(fields)
            .Where(m => !m.Name.StartsWith("get_") && !m.Name.StartsWith("set_"));
    }

    private static string? GetTypeKind(Type type)
    {
        if (type.IsClass)
            return "class";
        if (type.IsInterface)
            return "interface";
        if (type.IsValueType && !type.IsEnum)
            return "struct";
        if (type.IsEnum)
            return "enum";

        return null;
    }
}
