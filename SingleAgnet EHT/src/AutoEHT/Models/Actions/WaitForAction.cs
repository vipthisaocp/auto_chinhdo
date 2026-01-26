namespace AutoEHT.Models.Actions;

/// <summary>
/// Action to wait until a template appears
/// </summary>
public class WaitForAction : ScriptAction
{
    public override ActionType Type => ActionType.WaitFor;
    
    /// <summary>Template name to wait for</summary>
    public string TemplateName { get; set; } = string.Empty;
    
    /// <summary>Maximum time to wait in milliseconds</summary>
    public int TimeoutMs { get; set; } = 10000;
    
    /// <summary>How often to check for the template in milliseconds</summary>
    public int PollIntervalMs { get; set; } = 500;
}
