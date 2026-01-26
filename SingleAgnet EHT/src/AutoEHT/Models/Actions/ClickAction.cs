namespace AutoEHT.Models.Actions;

/// <summary>
/// Action to click on a target
/// </summary>
public class ClickAction : ScriptAction
{
    public override ActionType Type => ActionType.Click;
    
    /// <summary>Type of click target</summary>
    public ClickTarget TargetType { get; set; } = ClickTarget.Coordinate;
    
    /// <summary>X coordinate (if TargetType is Coordinate)</summary>
    public int? X { get; set; }
    
    /// <summary>Y coordinate (if TargetType is Coordinate)</summary>
    public int? Y { get; set; }
    
    /// <summary>Template name to find and click (if TargetType is Template)</summary>
    public string? TemplateName { get; set; }
    
    /// <summary>Offset from template center X</summary>
    public int OffsetX { get; set; }
    
    /// <summary>Offset from template center Y</summary>
    public int OffsetY { get; set; }
    
    /// <summary>Delay before click in ms</summary>
    public int DelayBefore { get; set; }
    
    /// <summary>Delay after click in ms</summary>
    public int DelayAfter { get; set; } = 100;
}

/// <summary>
/// Type of click target
/// </summary>
public enum ClickTarget
{
    /// <summary>Click at fixed coordinates</summary>
    Coordinate,
    
    /// <summary>Click at center of matched template</summary>
    Template
}
