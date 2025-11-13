using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace NuGetToolbox.Cli.Services;

/// <summary>
/// Parses and provides XML documentation from compiler-generated .xml files.
/// </summary>
public class XmlDocumentationProvider
{
    private readonly ILogger<XmlDocumentationProvider> _logger;
    private readonly Dictionary<string, XElement> _memberElements = new();

    public XmlDocumentationProvider(ILogger<XmlDocumentationProvider> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Loads XML documentation from a .xml file.
    /// </summary>
    public void LoadDocumentation(string xmlPath)
    {
        if (!File.Exists(xmlPath))
        {
            _logger.LogWarning("XML documentation file not found: {XmlPath}", xmlPath);
            return;
        }

        try
        {
            var doc = XDocument.Load(xmlPath);
            var members = doc.Root?.Element("members")?.Elements("member");

            if (members == null)
            {
                _logger.LogWarning("No members found in XML documentation: {XmlPath}", xmlPath);
                return;
            }

            foreach (var member in members)
            {
                var nameAttr = member.Attribute("name")?.Value;
                if (!string.IsNullOrEmpty(nameAttr))
                {
                    _memberElements[nameAttr] = member;
                }
            }

            _logger.LogDebug("Loaded {Count} documentation members from {XmlPath}", _memberElements.Count, xmlPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse XML documentation file: {XmlPath}", xmlPath);
        }
    }

    /// <summary>
    /// Gets the summary documentation for a member by its canonical documentation ID.
    /// </summary>
    public string? GetSummary(string documentationCommentId)
    {
        if (!_memberElements.TryGetValue(documentationCommentId, out var memberElement))
        {
            return null;
        }

        var summaryElement = memberElement.Element("summary");
        return summaryElement != null ? NormalizeWhitespace(summaryElement.Value) : null;
    }

    /// <summary>
    /// Gets parameter documentation for a member.
    /// </summary>
    public Dictionary<string, string> GetParameters(string documentationCommentId)
    {
        var result = new Dictionary<string, string>();

        if (!_memberElements.TryGetValue(documentationCommentId, out var memberElement))
        {
            return result;
        }

        var paramElements = memberElement.Elements("param");
        foreach (var param in paramElements)
        {
            var nameAttr = param.Attribute("name")?.Value;
            if (!string.IsNullOrEmpty(nameAttr))
            {
                result[nameAttr] = NormalizeWhitespace(param.Value);
            }
        }

        return result;
    }

    /// <summary>
    /// Gets the returns documentation for a member.
    /// </summary>
    public string? GetReturns(string documentationCommentId)
    {
        if (!_memberElements.TryGetValue(documentationCommentId, out var memberElement))
        {
            return null;
        }

        var returnsElement = memberElement.Element("returns");
        return returnsElement != null ? NormalizeWhitespace(returnsElement.Value) : null;
    }

    private static string NormalizeWhitespace(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var lines = text.Split('\n')
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrEmpty(line));

        return string.Join(" ", lines);
    }
}
