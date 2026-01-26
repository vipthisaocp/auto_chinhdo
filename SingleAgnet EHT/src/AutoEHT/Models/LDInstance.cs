namespace AutoEHT.Models;

/// <summary>
/// Represents an LDPlayer emulator instance
/// </summary>
public class LDInstance
{
    /// <summary>LDPlayer index (0, 1, 2...)</summary>
    public int Index { get; set; }
    
    /// <summary>Display name from LDPlayer</summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>ADB port (5555, 5557, 5559...)</summary>
    public int AdbPort { get; set; }
    
    /// <summary>ADB serial string (e.g., "emulator-5554" or "localhost:5555")</summary>
    public string AdbSerial { get; set; } = string.Empty;
    
    /// <summary>Connection status</summary>
    public InstanceStatus Status { get; set; } = InstanceStatus.Detected;
    
    /// <summary>Whether this instance is selected in UI</summary>
    public bool IsSelected { get; set; }
    
    /// <summary>Whether a script is currently running</summary>
    public bool IsScriptRunning { get; set; }
    
    /// <summary>Name of the currently running script</summary>
    public string? CurrentScript { get; set; }
    
    /// <summary>Window handle for the emulator</summary>
    public nint WindowHandle { get; set; }
}

/// <summary>
/// Status of an LDPlayer instance
/// </summary>
public enum InstanceStatus
{
    /// <summary>Detected via ldconsole, not yet verified</summary>
    Detected,
    
    /// <summary>ADB connected successfully</summary>
    Connected,
    
    /// <summary>Cannot connect via ADB</summary>
    Disconnected,
    
    /// <summary>Error occurred</summary>
    Error
}
