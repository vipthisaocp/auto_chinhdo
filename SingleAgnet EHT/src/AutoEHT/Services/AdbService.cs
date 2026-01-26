using System.Diagnostics;
using System.IO;

namespace AutoEHT.Services;

/// <summary>
/// ADB service for communicating with Android devices/emulators
/// </summary>
public class AdbService : IAdbService
{
    private readonly string _adbPath;
    
    public AdbService(string? adbPath = null)
    {
        _adbPath = adbPath ?? FindAdbPath();
    }
    
    /// <summary>Find ADB executable path</summary>
    private static string FindAdbPath()
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var paths = new[]
        {
            Path.Combine(appDir, "adb.exe"),
            Path.Combine(appDir, "platform-tools", "adb.exe"),
            Path.Combine(appDir, "adb", "adb.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                "Android", "Sdk", "platform-tools", "adb.exe"),
            @"C:\adb\adb.exe",
            @"C:\platform-tools\adb.exe"
        };
        
        foreach (var path in paths)
        {
            if (File.Exists(path)) return path;
        }
        return "adb"; // Fallback to PATH
    }
    
    public async Task<bool> ConnectAsync(string serial)
    {
        var result = await RunCommandAsync($"connect {serial}");
        return result.Contains("connected") || result.Contains("already");
    }
    
    public async Task<bool> TapAsync(string serial, int x, int y)
    {
        var args = $"-s {serial} shell input tap {x} {y}";
        Console.WriteLine($"[ADB] Executing: adb {args}");
        var result = await RunCommandAsync(args);
        Console.WriteLine($"[ADB] Result: '{result}'");
        var success = !result.Contains("error", StringComparison.OrdinalIgnoreCase);
        Console.WriteLine($"[ADB] Success: {success}");
        return success;
    }
    
    public async Task<bool> SwipeAsync(string serial, int x1, int y1, int x2, int y2, int durationMs)
    {
        var result = await RunCommandAsync($"-s {serial} shell input swipe {x1} {y1} {x2} {y2} {durationMs}");
        return !result.Contains("error", StringComparison.OrdinalIgnoreCase);
    }
    
    public async Task<byte[]?> CaptureScreenAsync(string serial)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _adbPath,
                    Arguments = $"-s {serial} exec-out screencap -p",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            
            using var memoryStream = new MemoryStream();
            await process.StandardOutput.BaseStream.CopyToAsync(memoryStream);
            await process.WaitForExitAsync();
            
            if (process.ExitCode != 0) return null;
            return memoryStream.ToArray();
        }
        catch
        {
            return null;
        }
    }
    
    public async Task<string> ShellAsync(string serial, string command)
    {
        return await RunCommandAsync($"-s {serial} shell {command}");
    }
    
    public async Task<bool> IsConnectedAsync(string serial)
    {
        var result = await RunCommandAsync("devices");
        return result.Contains(serial) && result.Contains("device");
    }
    
    private async Task<string> RunCommandAsync(string arguments)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _adbPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            
            return string.IsNullOrEmpty(error) ? output : $"{output}\n{error}";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}
