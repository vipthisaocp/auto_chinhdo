namespace AutoEHT.Models;

/// <summary>
/// Application settings
/// </summary>
public class AppSettings
{
    /// <summary>Default threshold for template matching (0.0 - 1.0)</summary>
    public double DefaultThreshold { get; set; } = 0.8;
    
    /// <summary>Default delay after click in milliseconds</summary>
    public int DefaultClickDelay { get; set; } = 100;
    
    /// <summary>Path to adb.exe (null = auto-detect)</summary>
    public string? AdbPath { get; set; }
    
    /// <summary>Path to ldconsole.exe (null = auto-detect)</summary>
    public string? LDConsolePath { get; set; }
    
    /// <summary>Folder containing template images</summary>
    public string TemplatesFolder { get; set; } = "templates";
    
    /// <summary>Folder containing script files</summary>
    public string ScriptsFolder { get; set; } = "scripts";
    
    /// <summary>Polling interval for WaitFor action in ms</summary>
    public int WaitForPollInterval { get; set; } = 500;
    
    /// <summary>Default timeout for WaitFor action in ms</summary>
    public int WaitForDefaultTimeout { get; set; } = 10000;
}
