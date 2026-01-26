using System.Diagnostics;
using System.IO;
using AutoEHT.Models;

namespace AutoEHT.Services;

/// <summary>
/// Service for detecting and managing LDPlayer instances using Window Handles
/// </summary>
public class LDPlayerService : ILDPlayerService
{
    private readonly string _ldConsolePath;
    private readonly WindowService _windowService;
    private List<LDInstance> _instances = [];
    
    public event EventHandler<List<LDInstance>>? InstancesChanged;
    
    public LDPlayerService(WindowService windowService)
    {
        _windowService = windowService;
        _ldConsolePath = FindLDConsolePath();
    }
    
    private static string FindLDConsolePath()
    {
        var paths = new[]
        {
            @"D:\LDPlayer\LDPlayer9\ldconsole.exe",
            @"C:\LDPlayer\LDPlayer9\ldconsole.exe",
            @"C:\LDPlayer\LDPlayer4.0\ldconsole.exe",
            @"D:\LDPlayer\LDPlayer4.0\ldconsole.exe",
            @"C:\Program Files\LDPlayer\LDPlayer9\ldconsole.exe",
            @"C:\Program Files (x86)\LDPlayer\LDPlayer9\ldconsole.exe"
        };
        
        foreach (var path in paths)
        {
            if (File.Exists(path)) return path;
        }
        return "ldconsole";
    }
    
    public async Task<List<LDInstance>> GetInstancesAsync()
    {
        await RefreshAsync();
        return _instances;
    }
    
    public Task RefreshAsync()
    {
        try
        {
            // Tìm LDPlayer windows trực tiếp bằng Window Handle
            var windows = _windowService.FindLDPlayerWindows();
            
            var instances = new List<LDInstance>();
            foreach (var (handle, title, index) in windows)
            {
                instances.Add(new LDInstance
                {
                    Index = index,
                    Name = title,
                    WindowHandle = handle,
                    Status = InstanceStatus.Connected,
                    AdbPort = 5555 + (index * 2),
                    AdbSerial = $"localhost:{5555 + (index * 2)}"
                });
                Console.WriteLine($"[LDPlayer] Found: {title} (Handle: {handle})");
            }
            
            _instances = instances;
            InstancesChanged?.Invoke(this, _instances);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LDPlayer] Error: {ex.Message}");
        }
        
        return Task.CompletedTask;
    }
    
    public LDInstance? GetInstance(int index)
    {
        return _instances.FirstOrDefault(i => i.Index == index);
    }
}
