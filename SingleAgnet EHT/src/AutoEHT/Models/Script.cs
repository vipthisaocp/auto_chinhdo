namespace AutoEHT.Models;

/// <summary>
/// Represents a script with a sequence of actions
/// </summary>
public class Script
{
    /// <summary>Unique identifier</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>Display name</summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>Optional description</summary>
    public string? Description { get; set; }
    
    /// <summary>List of actions to execute</summary>
    public List<ScriptAction> Actions { get; set; } = [];
    
    /// <summary>Number of times to loop (-1 = infinite)</summary>
    public int LoopCount { get; set; } = 1;
    
    /// <summary>When this script was created</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    /// <summary>When this script was last modified</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Base class for all script actions
/// </summary>
public abstract class ScriptAction
{
    /// <summary>Type of the action</summary>
    public abstract ActionType Type { get; }
    
    /// <summary>Optional description for logging</summary>
    public string? Description { get; set; }
}
