using System.Diagnostics;
using System.Runtime.InteropServices;
using AutoEHT.Models;

namespace AutoEHT.Services;

/// <summary>
/// Service để ghi lại thao tác người dùng và phát lại
/// </summary>
public class RecordService
{
    private readonly WindowService _windowService;
    private Recording? _currentRecording;
    private Stopwatch? _stopwatch;
    private IntPtr _targetWindow;
    private bool _isRecording;
    
    // Mouse hook
    private IntPtr _hookId = IntPtr.Zero;
    private LowLevelMouseProc? _hookProc;
    private int _lastX, _lastY;
    private bool _isMouseDown;
    private long _mouseDownTime;
    
    public event Action<RecordedAction>? OnActionRecorded;
    public event Action<string>? OnLog;
    
    public bool IsRecording => _isRecording;
    public Recording? CurrentRecording => _currentRecording;
    
    public RecordService(WindowService windowService)
    {
        _windowService = windowService;
    }
    
    #region Win32 Mouse Hook
    
    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
    
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);
    
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);
    
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
    
    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(POINT point);
    
    [DllImport("user32.dll")]
    private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);
    
    [DllImport("user32.dll")]
    private static extern IntPtr GetParent(IntPtr hWnd);
    
    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X, Y; }
    
    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
    
    private const int WH_MOUSE_LL = 14;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_MOUSEMOVE = 0x0200;
    
    #endregion
    
    /// <summary>Bắt đầu ghi thao tác</summary>
    public void StartRecording(IntPtr targetWindow, string name = "New Recording")
    {
        if (_isRecording) return;
        
        _targetWindow = _windowService.FindRenderWindow(targetWindow);
        _currentRecording = new Recording { Name = name };
        _stopwatch = Stopwatch.StartNew();
        _isRecording = true;
        
        // Setup mouse hook
        _hookProc = HookCallback;
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        _hookId = SetWindowsHookEx(WH_MOUSE_LL, _hookProc, GetModuleHandle(curModule.ModuleName!), 0);
        
        OnLog?.Invoke($"🔴 Recording started: {name}");
    }
    
    /// <summary>Dừng ghi</summary>
    public Recording? StopRecording()
    {
        if (!_isRecording) return null;
        
        _isRecording = false;
        _stopwatch?.Stop();
        
        // Unhook
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
        
        OnLog?.Invoke($"⏹️ Recording stopped: {_currentRecording?.Actions.Count} actions");
        return _currentRecording;
    }
    
    /// <summary>Phát lại recording</summary>
    public async Task PlaybackAsync(Recording recording, IntPtr windowHandle, CancellationToken token = default)
    {
        var renderHandle = _windowService.FindRenderWindow(windowHandle);
        OnLog?.Invoke($"▶️ Playback started: {recording.Name} ({recording.Actions.Count} actions)");
        
        long lastTimestamp = 0;
        foreach (var action in recording.Actions)
        {
            if (token.IsCancellationRequested) break;
            
            // Đợi đúng thời gian
            var delay = (int)(action.Timestamp - lastTimestamp);
            if (delay > 0) await Task.Delay(delay, token);
            lastTimestamp = action.Timestamp;
            
            // Thực hiện action
            switch (action.Type)
            {
                case RecordActionType.Click:
                    await _windowService.ClickAsync(renderHandle, action.X, action.Y);
                    OnLog?.Invoke($"  👆 Click ({action.X}, {action.Y})");
                    break;
                    
                case RecordActionType.Swipe:
                    await _windowService.SwipeAsync(renderHandle, action.X, action.Y, action.EndX, action.EndY, action.Duration);
                    OnLog?.Invoke($"  👆 Swipe ({action.X},{action.Y}) → ({action.EndX},{action.EndY})");
                    break;
                    
                case RecordActionType.Wait:
                    await Task.Delay(action.Duration, token);
                    OnLog?.Invoke($"  ⏳ Wait {action.Duration}ms");
                    break;
            }
        }
        
        OnLog?.Invoke($"⏹️ Playback finished");
    }
    
    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && _isRecording)
        {
            var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            var screenPoint = new POINT { X = hookStruct.pt.X, Y = hookStruct.pt.Y };
            
            // Kiểm tra click có thuộc target window không
            var windowAtPoint = WindowFromPoint(screenPoint);
            if (IsChildOfTarget(windowAtPoint))
            {
                // Chuyển tọa độ sang client
                var clientPoint = screenPoint;
                ScreenToClient(_targetWindow, ref clientPoint);
                
                var msg = (int)wParam;
                var timestamp = _stopwatch?.ElapsedMilliseconds ?? 0;
                
                switch (msg)
                {
                    case WM_LBUTTONDOWN:
                        _isMouseDown = true;
                        _lastX = clientPoint.X;
                        _lastY = clientPoint.Y;
                        _mouseDownTime = timestamp;
                        break;
                        
                    case WM_LBUTTONUP when _isMouseDown:
                        _isMouseDown = false;
                        var distance = Math.Sqrt(Math.Pow(clientPoint.X - _lastX, 2) + Math.Pow(clientPoint.Y - _lastY, 2));
                        
                        RecordedAction action;
                        if (distance < 10)
                        {
                            // Click
                            action = RecordedAction.Click(clientPoint.X, clientPoint.Y, timestamp);
                            OnLog?.Invoke($"  📍 Click ({clientPoint.X}, {clientPoint.Y})");
                        }
                        else
                        {
                            // Swipe
                            var duration = (int)(timestamp - _mouseDownTime);
                            action = RecordedAction.Swipe(_lastX, _lastY, clientPoint.X, clientPoint.Y, duration, timestamp);
                            OnLog?.Invoke($"  📍 Swipe ({_lastX},{_lastY}) → ({clientPoint.X},{clientPoint.Y})");
                        }
                        
                        _currentRecording?.AddAction(action);
                        OnActionRecorded?.Invoke(action);
                        break;
                }
            }
        }
        
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }
    
    private bool IsChildOfTarget(IntPtr window)
    {
        if (window == _targetWindow) return true;
        
        var parent = GetParent(window);
        while (parent != IntPtr.Zero)
        {
            if (parent == _targetWindow) return true;
            parent = GetParent(parent);
        }
        return false;
    }
}
