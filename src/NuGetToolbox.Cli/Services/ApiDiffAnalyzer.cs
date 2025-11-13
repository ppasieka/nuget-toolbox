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
        methodsFrom ??= new List<MethodInfo>();
        methodsTo ??= new List<MethodInfo>();

        _logger.LogInformation("Comparing {From} vs {To} for {Package}", versionFrom, versionTo, packageId);

        var baseDict = BuildMethodDictionary(methodsFrom);
        var newDict = BuildMethodDictionary(methodsTo);

        var removed = new List<DiffItem>();
        var added = new List<TypeInfo>();
        var breaking = new List<DiffItem>();

        foreach (var (key, method) in baseDict)
        {
            if (!newDict.ContainsKey(key))
            {
                var diffItem = new DiffItem
                {
                    Type = method.Type,
                    Method = method.Method,
                    Signature = method.Signature,
                    Reason = "Method removed"
                };
                removed.Add(diffItem);
                breaking.Add(diffItem);
            }
        }

        var addedTypes = new HashSet<string>();
        foreach (var (key, method) in newDict)
        {
            if (!baseDict.ContainsKey(key))
            {
                if (addedTypes.Add(method.Type))
                {
                    var typeInfo = new TypeInfo
                    {
                        Namespace = ExtractNamespace(method.Type),
                        Name = ExtractTypeName(method.Type),
                        Kind = "class"
                    };
                    added.Add(typeInfo);
                }
            }
        }

        var modified = IdentifyModifiedSignatures(baseDict, newDict);
        breaking.AddRange(modified);

        _logger.LogInformation("Found {Breaking} breaking changes, {Added} additions", breaking.Count, added.Count);

        return new DiffResult
        {
            PackageId = packageId,
            VersionFrom = versionFrom,
            VersionTo = versionTo,
            Tfm = tfm,
            Breaking = breaking.Count > 0 ? breaking : null,
            Added = added.Count > 0 ? added : null,
            Removed = removed.Count > 0 ? removed.Select(d => new TypeInfo
            {
                Namespace = ExtractNamespace(d.Type),
                Name = ExtractTypeName(d.Type),
                Kind = "class"
            }).ToList() : null,
            Compatible = breaking.Count == 0
        };
    }

    /// <summary>
    /// Identifies methods with modified signatures (same type+method name, different signature).
    /// </summary>
    private List<DiffItem> IdentifyModifiedSignatures(
        Dictionary<string, MethodInfo> baseDict,
        Dictionary<string, MethodInfo> newDict)
    {
        var modified = new List<DiffItem>();
        var baseByTypeAndMethod = baseDict.Values
            .GroupBy(m => (m.Type, m.Method))
            .ToDictionary(g => g.Key, g => g.ToList());

        var newByTypeAndMethod = newDict.Values
            .GroupBy(m => (m.Type, m.Method))
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var (key, baseMethods) in baseByTypeAndMethod)
        {
            if (newByTypeAndMethod.TryGetValue(key, out var newMethods))
            {
                foreach (var baseMethod in baseMethods)
                {
                    var fullKey = $"{baseMethod.Type}::{baseMethod.Method}::{baseMethod.Signature}";
                    if (!newDict.ContainsKey(fullKey))
                    {
                        modified.Add(new DiffItem
                        {
                            Type = baseMethod.Type,
                            Method = baseMethod.Method,
                            Signature = baseMethod.Signature,
                            Reason = "Signature modified"
                        });
                    }
                }
            }
        }

        return modified;
    }

    /// <summary>
    /// Builds a dictionary keyed by (type, method, signature) for fast lookup.
    /// </summary>
    private Dictionary<string, MethodInfo> BuildMethodDictionary(List<MethodInfo> methods)
    {
        return methods.ToDictionary(
            m => $"{m.Type}::{m.Method}::{m.Signature}",
            m => m);
    }

    private string ExtractNamespace(string fullyQualifiedType)
    {
        var lastDot = fullyQualifiedType.LastIndexOf('.');
        return lastDot > 0 ? fullyQualifiedType.Substring(0, lastDot) : string.Empty;
    }

    private string ExtractTypeName(string fullyQualifiedType)
    {
        var lastDot = fullyQualifiedType.LastIndexOf('.');
        return lastDot > 0 ? fullyQualifiedType.Substring(lastDot + 1) : fullyQualifiedType;
    }
}
