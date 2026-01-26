namespace AutoEHT.Models.Actions;

/// <summary>
/// Action to swipe from one point to another
/// </summary>
public class SwipeAction : ScriptAction
{
    public override ActionType Type => ActionType.Swipe;
    
    /// <summary>Starting X coordinate</summary>
    public int StartX { get; set; }
    
    /// <summary>Starting Y coordinate</summary>
    public int StartY { get; set; }
    
    /// <summary>Ending X coordinate</summary>
    public int EndX { get; set; }
    
    /// <summary>Ending Y coordinate</summary>
    public int EndY { get; set; }
    
    /// <summary>Duration of swipe in milliseconds</summary>
    public int DurationMs { get; set; } = 300;
}
