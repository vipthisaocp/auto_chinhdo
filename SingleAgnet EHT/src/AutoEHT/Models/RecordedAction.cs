namespace AutoEHT.Models;

/// <summary>
/// Một thao tác đã được ghi lại
/// </summary>
public class RecordedAction
{
    public RecordActionType Type { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int EndX { get; set; }  // For swipe
    public int EndY { get; set; }  // For swipe
    public int Duration { get; set; }  // For swipe duration or wait time
    public long Timestamp { get; set; }  // Time since recording started (ms)
    
    public static RecordedAction Click(int x, int y, long timestamp)
        => new() { Type = RecordActionType.Click, X = x, Y = y, Timestamp = timestamp };
    
    public static RecordedAction Swipe(int x1, int y1, int x2, int y2, int duration, long timestamp)
        => new() { Type = RecordActionType.Swipe, X = x1, Y = y1, EndX = x2, EndY = y2, Duration = duration, Timestamp = timestamp };
    
    public static RecordedAction Wait(int ms, long timestamp)
        => new() { Type = RecordActionType.Wait, Duration = ms, Timestamp = timestamp };
}

public enum RecordActionType
{
    Click,
    Swipe,
    Wait
}
