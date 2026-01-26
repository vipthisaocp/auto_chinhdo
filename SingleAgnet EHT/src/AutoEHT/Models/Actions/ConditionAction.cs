namespace AutoEHT.Models.Actions;

/// <summary>
/// Conditional action based on template presence
/// </summary>
public class ConditionAction : ScriptAction
{
    public override ActionType Type => ActionType.Condition;
    
    /// <summary>Template to check for</summary>
    public string TemplateName { get; set; } = string.Empty;
    
    /// <summary>Actions to execute if template is found</summary>
    public List<ScriptAction> ThenActions { get; set; } = [];
    
    /// <summary>Actions to execute if template is not found</summary>
    public List<ScriptAction> ElseActions { get; set; } = [];
}
