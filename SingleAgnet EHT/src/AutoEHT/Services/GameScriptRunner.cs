using AutoEHT.Scripts;
using AutoEHT.Services;

namespace AutoEHT.Services;

/// <summary>
/// Service để chạy các GameScript sử dụng Window Handle
/// </summary>
public class GameScriptRunner
{
    private readonly WindowService _windowService;
    private readonly IImageMatchService _imageMatchService;
    private readonly Dictionary<string, GameScript> _scripts = new();
    
    public event Action<string>? OnLog;
    
    public GameScriptRunner(WindowService windowService, IImageMatchService imageMatchService)
    {
        _windowService = windowService;
        _imageMatchService = imageMatchService;
        LoadScripts();
    }
    
    private void LoadScripts()
    {
        Register(new StoneFarmScript());
    }
    
    public void Register(GameScript script) => _scripts[script.Id] = script;
    
    public IEnumerable<GameScript> GetAllScripts() => _scripts.Values;
    
    public GameScript? GetScript(string id) => _scripts.TryGetValue(id, out var s) ? s : null;
    
    public async Task RunAsync(IntPtr windowHandle, string scriptId, CancellationToken token = default)
    {
        if (!_scripts.TryGetValue(scriptId, out var script))
        {
            OnLog?.Invoke($"Script not found: {scriptId}");
            return;
        }
        
        // Tìm render window (sub-window để chụp hình và điều khiển)
        var renderHandle = _windowService.FindRenderWindow(windowHandle);
        
        script.Init(_windowService, _imageMatchService, renderHandle, token);
        OnLog?.Invoke($"Starting: {script.Name} (Render: {renderHandle})");
        
        try
        {
            await script.Run();
        }
        catch (OperationCanceledException)
        {
            OnLog?.Invoke($"Cancelled: {script.Name}");
        }
        catch (Exception ex)
        {
            OnLog?.Invoke($"Error: {ex.Message}");
        }
        
        OnLog?.Invoke($"Stopped: {script.Name}");
    }
}
