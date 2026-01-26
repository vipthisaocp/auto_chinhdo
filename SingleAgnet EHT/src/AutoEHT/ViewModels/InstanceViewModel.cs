using AutoEHT.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoEHT.ViewModels;

/// <summary>
/// ViewModel for an LDPlayer instance
/// </summary>
public partial class InstanceViewModel : ObservableObject
{
    private readonly LDInstance _model;
    
    public InstanceViewModel(LDInstance model)
    {
        _model = model;
    }
    
    public int Index => _model.Index;
    public string Name => _model.Name;
    public int AdbPort => _model.AdbPort;
    public string AdbSerial => _model.AdbSerial;
    public IntPtr WindowHandle => _model.WindowHandle;
    
    public InstanceStatus Status
    {
        get => _model.Status;
        set
        {
            _model.Status = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(IsConnected));
        }
    }
    
    public bool IsSelected
    {
        get => _model.IsSelected;
        set
        {
            _model.IsSelected = value;
            OnPropertyChanged();
        }
    }
    
    public bool IsScriptRunning
    {
        get => _model.IsScriptRunning;
        set
        {
            _model.IsScriptRunning = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusText));
        }
    }
    
    public string? CurrentScript
    {
        get => _model.CurrentScript;
        set
        {
            _model.CurrentScript = value;
            OnPropertyChanged();
        }
    }
    
    public bool IsConnected => Status == InstanceStatus.Connected;
    
    public string StatusText => Status switch
    {
        InstanceStatus.Connected when IsScriptRunning => $"Running: {CurrentScript}",
        InstanceStatus.Connected => "Connected",
        InstanceStatus.Disconnected => "Disconnected",
        InstanceStatus.Error => "Error",
        _ => "Detected"
    };
}
