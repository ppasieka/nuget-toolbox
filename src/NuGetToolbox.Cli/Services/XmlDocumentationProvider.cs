using Microsoft.Extensions.Logging;

namespace NuGetToolbox.Cli.Services;

/// <summary>
/// Parses and provides XML documentation from compiler-generated .xml files.
/// </summary>
public class XmlDocumentationProvider
{
    private readonly ILogger<XmlDocumentationProvider> _logger;

    public XmlDocumentationProvider(ILogger<XmlDocumentationProvider> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Loads XML documentation from a .xml file.
    /// </summary>
    public void LoadDocumentation(string xmlPath)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets the summary documentation for a member by its canonical documentation ID.
    /// </summary>
    public string? GetSummary(string documentationCommentId)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets parameter documentation for a member.
    /// </summary>
    public Dictionary<string, string> GetParameters(string documentationCommentId)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets the returns documentation for a member.
    /// </summary>
    public string? GetReturns(string documentationCommentId)
    {
        throw new NotImplementedException();
    }
}
