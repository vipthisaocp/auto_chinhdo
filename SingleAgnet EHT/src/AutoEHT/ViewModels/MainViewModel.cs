using System.Collections.ObjectModel;
using System.IO;
using AutoEHT.Models;
using AutoEHT.Scripts;
using AutoEHT.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoEHT.ViewModels;

/// <summary>
/// Main ViewModel for the application
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly ILDPlayerService _ldPlayerService;
    private readonly GameScriptRunner _scriptRunner;
    private readonly RecordService _recordService;
    private readonly Dictionary<int, CancellationTokenSource> _runningScripts = new();
    
    [ObservableProperty]
    private ObservableCollection<InstanceViewModel> _instances = [];
    
    [ObservableProperty]
    private InstanceViewModel? _selectedInstance;
    
    [ObservableProperty]
    private ObservableCollection<LogEntry> _logs = [];
    
    [ObservableProperty]
    private ObservableCollection<Template> _templates = [];
    
    [ObservableProperty]
    private ObservableCollection<GameScript> _scripts = [];
    
    [ObservableProperty]
    private GameScript? _selectedScript;
    
    [ObservableProperty]
    private bool _isRefreshing;
    
    [ObservableProperty]
    private bool _isRecording;
    
    [ObservableProperty]
    private string _statusMessage = "Ready";
    
    public MainViewModel(
        ILDPlayerService ldPlayerService,
        IImageMatchService imageMatchService,
        GameScriptRunner scriptRunner,
        RecordService recordService)
    {
        _ldPlayerService = ldPlayerService;
        _scriptRunner = scriptRunner;
        _recordService = recordService;
        
        _ldPlayerService.InstancesChanged += OnInstancesChanged;
        _scriptRunner.OnLog += msg => AddLog(LogLevel.Info, msg);
        _recordService.OnLog += msg => AddLog(LogLevel.Info, msg);
        
        _ = RefreshInstancesAsync();
        LoadTemplates(imageMatchService);
        LoadScripts();
        LoadRecordings();
    }
    
    private void OnInstancesChanged(object? sender, List<LDInstance> instances)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            Instances.Clear();
            foreach (var instance in instances)
                Instances.Add(new InstanceViewModel(instance));
            UpdateStatus();
        });
    }
    
    private void AddLog(LogLevel level, string message)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            Logs.Insert(0, new LogEntry(DateTime.Now, level, "", message, null));
            if (Logs.Count > 500) Logs.RemoveAt(Logs.Count - 1);
        });
    }
    
    private void UpdateStatus()
    {
        var connected = Instances.Count(i => i.Status == InstanceStatus.Connected);
        var running = _runningScripts.Count;
        var recording = IsRecording ? " | 🔴 Recording" : "";
        StatusMessage = $"Found: {connected} | Running: {running}{recording}";
    }
    
    [RelayCommand]
    private async Task RefreshInstancesAsync()
    {
        IsRefreshing = true;
        try { await _ldPlayerService.RefreshAsync(); }
        finally { IsRefreshing = false; }
    }
    
    [RelayCommand]
    private async Task StartScriptAsync(InstanceViewModel instance)
    {
        if (SelectedScript == null) return;
        if (_runningScripts.ContainsKey(instance.Index)) return;
        
        var cts = new CancellationTokenSource();
        _runningScripts[instance.Index] = cts;
        instance.IsScriptRunning = true;
        instance.CurrentScript = SelectedScript.Name;
        UpdateStatus();
        
        try
        {
            await _scriptRunner.RunAsync(instance.WindowHandle, SelectedScript.Id, cts.Token);
        }
        finally
        {
            _runningScripts.Remove(instance.Index);
            instance.IsScriptRunning = false;
            instance.CurrentScript = null;
            UpdateStatus();
        }
    }
    
    [RelayCommand]
    private void StopScript(InstanceViewModel instance)
    {
        if (_runningScripts.TryGetValue(instance.Index, out var cts))
            cts.Cancel();
    }
    
    [RelayCommand]
    private async Task StartAllAsync()
    {
        var selected = Instances.Where(i => i.IsSelected && i.Status == InstanceStatus.Connected && !i.IsScriptRunning).ToList();
        foreach (var instance in selected)
        {
            _ = StartScriptAsync(instance);
            await Task.Delay(100);
        }
    }
    
    [RelayCommand]
    private void StopAll()
    {
        foreach (var instance in Instances.Where(i => i.IsSelected && i.IsScriptRunning))
            StopScript(instance);
    }
    
    [RelayCommand]
    private void SelectAll() => Instances.ToList().ForEach(i => i.IsSelected = true);
    
    [RelayCommand]
    private void DeselectAll() => Instances.ToList().ForEach(i => i.IsSelected = false);
    
    [RelayCommand]
    private void ClearLogs() => Logs.Clear();
    
    // =====================================================
    // RECORDING COMMANDS
    // =====================================================
    
    [RelayCommand]
    private void StartRecording()
    {
        if (IsRecording) return;
        
        // Lấy instance đầu tiên đang connected
        var instance = Instances.FirstOrDefault(i => i.Status == InstanceStatus.Connected);
        if (instance == null)
        {
            AddLog(LogLevel.Warning, "No connected instance found!");
            return;
        }
        
        var name = $"Recording_{DateTime.Now:HHmmss}";
        _recordService.StartRecording(instance.WindowHandle, name);
        IsRecording = true;
        UpdateStatus();
    }
    
    [RelayCommand]
    private async Task StopRecordingAsync()
    {
        if (!IsRecording) return;
        
        var recording = _recordService.StopRecording();
        IsRecording = false;
        UpdateStatus();
        
        if (recording != null && recording.Actions.Count > 0)
        {
            // Lưu recording
            var recordingsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "recordings");
            Directory.CreateDirectory(recordingsDir);
            var path = Path.Combine(recordingsDir, $"{recording.Name}.json");
            await recording.SaveAsync(path);
            
            // Thêm vào danh sách scripts
            var script = new RecordingScript(recording);
            _scriptRunner.Register(script);
            Scripts.Add(script);
            SelectedScript = script;
            
            AddLog(LogLevel.Info, $"✅ Saved recording: {recording.Name} ({recording.Actions.Count} actions)");
        }
    }
    
    private void LoadTemplates(IImageMatchService imageMatchService)
    {
        Templates = new ObservableCollection<Template>(imageMatchService.GetAllTemplates());
    }
    
    private void LoadScripts()
    {
        foreach (var script in _scriptRunner.GetAllScripts())
            Scripts.Add(script);
        SelectedScript = Scripts.FirstOrDefault();
    }
    
    private void LoadRecordings()
    {
        var recordingsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "recordings");
        if (!Directory.Exists(recordingsDir)) return;
        
        foreach (var file in Directory.GetFiles(recordingsDir, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var recording = Recording.FromJson(json);
                if (recording != null)
                {
                    var script = new RecordingScript(recording);
                    _scriptRunner.Register(script);
                    Scripts.Add(script);
                }
            }
            catch { /* ignore invalid files */ }
        }
    }
}
