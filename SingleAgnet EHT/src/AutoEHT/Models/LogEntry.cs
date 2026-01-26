namespace AutoEHT.Models;

/// <summary>
/// Log entry for the application log
/// </summary>
public record LogEntry(
    DateTime Timestamp,
    LogLevel Level,
    string InstanceName,
    string Message,
    ActionType? ActionType = null
);

/// <summary>
/// Log level for entries
/// </summary>
public enum LogLevel
{
    Info,
    Success,
    Warning,
    Error
}

/// <summary>
/// Type of script action
/// </summary>
public enum ActionType
{
    Click,
    Wait,
    WaitFor,
    Swipe,
    Condition,
    Loop
}
