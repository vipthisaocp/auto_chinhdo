using AutoEHT.Models;

namespace AutoEHT.Services;

/// <summary>
/// Interface for LDPlayer operations
/// </summary>
public interface ILDPlayerService
{
    /// <summary>Detect all running LDPlayer instances</summary>
    Task<List<LDInstance>> GetInstancesAsync();
    
    /// <summary>Refresh instance list</summary>
    Task RefreshAsync();
    
    /// <summary>Get instance by index</summary>
    LDInstance? GetInstance(int index);
    
    /// <summary>Event raised when instances change</summary>
    event EventHandler<List<LDInstance>>? InstancesChanged;
}
