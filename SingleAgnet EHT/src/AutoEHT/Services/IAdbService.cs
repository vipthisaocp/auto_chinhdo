namespace AutoEHT.Services;

/// <summary>
/// Interface for ADB operations
/// </summary>
public interface IAdbService
{
    /// <summary>Connect to a device by serial</summary>
    Task<bool> ConnectAsync(string serial);
    
    /// <summary>Tap at coordinates</summary>
    Task<bool> TapAsync(string serial, int x, int y);
    
    /// <summary>Swipe from point A to point B</summary>
    Task<bool> SwipeAsync(string serial, int x1, int y1, int x2, int y2, int durationMs);
    
    /// <summary>Capture screenshot and return as byte array</summary>
    Task<byte[]?> CaptureScreenAsync(string serial);
    
    /// <summary>Execute a shell command</summary>
    Task<string> ShellAsync(string serial, string command);
    
    /// <summary>Check if device is connected</summary>
    Task<bool> IsConnectedAsync(string serial);
}
