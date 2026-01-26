namespace AutoEHT.Models;

/// <summary>
/// Result of a template matching operation
/// </summary>
/// <param name="Found">Whether the template was found</param>
/// <param name="X">X coordinate of match center</param>
/// <param name="Y">Y coordinate of match center</param>
/// <param name="Confidence">Match confidence (0.0 - 1.0)</param>
/// <param name="TemplateName">Name/key of the template</param>
/// <param name="Timestamp">When the match was performed</param>
public record MatchResult(
    bool Found,
    int X,
    int Y,
    double Confidence,
    string TemplateName,
    DateTime Timestamp)
{
    /// <summary>Create a not-found result</summary>
    public static MatchResult NotFound(string templateName) => 
        new(false, 0, 0, 0, templateName, DateTime.Now);
}
