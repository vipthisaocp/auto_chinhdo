using AutoEHT.Models;

namespace AutoEHT.Services;

/// <summary>
/// Interface for image template matching operations
/// </summary>
public interface IImageMatchService
{
    /// <summary>Find template in screenshot bytes</summary>
    MatchResult FindTemplate(byte[] screenshot, string templateKey);
    
    /// <summary>Capture screenshot and find template</summary>
    Task<MatchResult> FindTemplateOnDeviceAsync(string serial, string templateKey);
    
    /// <summary>Get all loaded templates</summary>
    IEnumerable<Template> GetAllTemplates();
    
    /// <summary>Get template by key</summary>
    Template? GetTemplate(string key);
    
    /// <summary>Add a template</summary>
    void AddTemplate(Template template);
    
    /// <summary>Remove a template</summary>
    void RemoveTemplate(string key);
    
    /// <summary>Reload all templates from disk</summary>
    void ReloadTemplates();
}
