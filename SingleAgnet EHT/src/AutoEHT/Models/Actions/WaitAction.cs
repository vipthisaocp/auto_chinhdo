namespace AutoEHT.Models.Actions;

/// <summary>
/// Action to wait for a fixed duration
/// </summary>
public class WaitAction : ScriptAction
{
    public override ActionType Type => ActionType.Wait;
    
    /// <summary>Duration to wait in milliseconds</summary>
    public int DurationMs { get; set; }
}
