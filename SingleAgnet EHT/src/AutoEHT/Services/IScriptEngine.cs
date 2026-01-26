using AutoEHT.Models;

namespace AutoEHT.Services;

/// <summary>
/// Interface for script execution
/// </summary>
public interface IScriptEngine
{
    /// <summary>Execute a script on a device</summary>
    Task RunAsync(string serial, Script script, CancellationToken cancellationToken = default);
    
    /// <summary>Execute a single action</summary>
    Task<bool> ExecuteActionAsync(string serial, ScriptAction action, CancellationToken cancellationToken = default);
    
    /// <summary>Stop all running scripts</summary>
    void StopAll();
    
    /// <summary>Event raised when action is executed</summary>
    event EventHandler<LogEntry>? ActionExecuted;
}
