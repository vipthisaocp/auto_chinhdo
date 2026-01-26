using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace AutoEHT.Services;

/// <summary>
/// Service sử dụng Window Handle để chụp hình và điều khiển LDPlayer
/// Nhanh hơn ADB rất nhiều!
/// </summary>
public class WindowService
{
    #region Win32 APIs
    
    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    
    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);
    
    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);
    
    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);
    
    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    
    [DllImport("user32.dll")]
    private static extern bool SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    
    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);
    
    [DllImport("user32.dll")]
    private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string? lpszClass, string? lpszWindow);
    
    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
    
    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);
    
    [DllImport("user32.dll")]
    private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);
    
    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);
    
    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
    
    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);
    
    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);
    
    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);
    
    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);
    
    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);
    
    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
    
    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdc, int x, int y, int cx, int cy, IntPtr hdcSrc, int x1, int y1, uint rop);
    
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
        public int Width => Right - Left;
        public int Height => Bottom - Top;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X, Y;
    }
    
    // Windows Messages
    private const uint WM_LBUTTONDOWN = 0x0201;
    private const uint WM_LBUTTONUP = 0x0202;
    private const uint WM_MOUSEMOVE = 0x0200;
    private const uint SRCCOPY = 0x00CC0020;
    private const int PW_RENDERFULLCONTENT = 0x00000002;
    
    #endregion
    
    /// <summary>Tìm tất cả cửa sổ LDPlayer (class name chứa: dnplayer)</summary>
    public List<(IntPtr Handle, string Title, int Index)> FindLDPlayerWindows()
    {
        var windows = new List<(IntPtr, string, int)>();
        int index = 0;
        
        EnumWindows((hWnd, lParam) =>
        {
            if (!IsWindowVisible(hWnd)) return true;
            
            // Lấy class name
            var className = new System.Text.StringBuilder(256);
            GetClassName(hWnd, className, 256);
            var classNameStr = className.ToString();
            
            // Tìm window có class name chứa "dnplayer" (case insensitive)
            if (classNameStr.Contains("dnplayer", StringComparison.OrdinalIgnoreCase) ||
                classNameStr.Contains("LDPlayer", StringComparison.OrdinalIgnoreCase))
            {
                var title = new System.Text.StringBuilder(256);
                GetWindowText(hWnd, title, 256);
                var titleStr = title.ToString();
                
                Console.WriteLine($"[Window] Found: Class='{classNameStr}', Title='{titleStr}', Handle={hWnd}");
                windows.Add((hWnd, string.IsNullOrEmpty(titleStr) ? $"dnplayer-{index}" : titleStr, index));
                index++;
            }
            return true;
        }, IntPtr.Zero);
        
        return windows;
    }
    
    /// <summary>Tìm cửa sổ render của LDPlayer (class: TheRender, sub, hoặc dnplayer)</summary>
    public IntPtr FindRenderWindow(IntPtr ldPlayerHandle)
    {
        // Thử tìm các sub-window class phổ biến của LDPlayer
        var classNames = new[] { "TheRender", "sub", "subWin", "RenderWindow" };
        
        foreach (var cls in classNames)
        {
            var renderWnd = FindWindowEx(ldPlayerHandle, IntPtr.Zero, cls, null);
            if (renderWnd != IntPtr.Zero)
            {
                Console.WriteLine($"[Window] Found render: Class='{cls}', Handle={renderWnd}");
                return renderWnd;
            }
        }
        
        // Fallback: tìm bất kỳ child window nào
        var child = FindWindowEx(ldPlayerHandle, IntPtr.Zero, null, null);
        if (child != IntPtr.Zero)
        {
            var className = new System.Text.StringBuilder(256);
            GetClassName(child, className, 256);
            Console.WriteLine($"[Window] Using first child: Class='{className}', Handle={child}");
            return child;
        }
        
        return ldPlayerHandle;
    }
    
    /// <summary>Chụp hình cửa sổ</summary>
    public Bitmap? CaptureWindow(IntPtr hWnd)
    {
        try
        {
            if (!GetClientRect(hWnd, out var rect) || rect.Width <= 0 || rect.Height <= 0)
                return null;
            
            var width = rect.Width;
            var height = rect.Height;
            
            var hdcWindow = GetDC(hWnd);
            var hdcMemory = CreateCompatibleDC(hdcWindow);
            var hBitmap = CreateCompatibleBitmap(hdcWindow, width, height);
            var hOld = SelectObject(hdcMemory, hBitmap);
            
            // Thử PrintWindow trước (hoạt động tốt hơn với một số app)
            if (!PrintWindow(hWnd, hdcMemory, PW_RENDERFULLCONTENT))
            {
                // Fallback to BitBlt
                BitBlt(hdcMemory, 0, 0, width, height, hdcWindow, 0, 0, SRCCOPY);
            }
            
            SelectObject(hdcMemory, hOld);
            
            var bmp = Image.FromHbitmap(hBitmap);
            
            DeleteObject(hBitmap);
            DeleteDC(hdcMemory);
            ReleaseDC(hWnd, hdcWindow);
            
            return bmp;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WindowService] CaptureWindow error: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>Chuyển Bitmap sang byte array (PNG)</summary>
    public byte[]? BitmapToBytes(Bitmap bmp)
    {
        try
        {
            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>Click vào vị trí trong cửa sổ</summary>
    public void Click(IntPtr hWnd, int x, int y)
    {
        var lParam = MakeLParam(x, y);
        
        // Gửi mouse down rồi mouse up
        PostMessage(hWnd, WM_LBUTTONDOWN, IntPtr.Zero, lParam);
        Thread.Sleep(50);
        PostMessage(hWnd, WM_LBUTTONUP, IntPtr.Zero, lParam);
    }
    
    /// <summary>Click async</summary>
    public async Task ClickAsync(IntPtr hWnd, int x, int y)
    {
        var lParam = MakeLParam(x, y);
        
        PostMessage(hWnd, WM_LBUTTONDOWN, IntPtr.Zero, lParam);
        await Task.Delay(50);
        PostMessage(hWnd, WM_LBUTTONUP, IntPtr.Zero, lParam);
    }
    
    /// <summary>Swipe (kéo từ điểm này sang điểm khác)</summary>
    public async Task SwipeAsync(IntPtr hWnd, int x1, int y1, int x2, int y2, int durationMs = 300, int steps = 10)
    {
        var lParamStart = MakeLParam(x1, y1);
        PostMessage(hWnd, WM_LBUTTONDOWN, IntPtr.Zero, lParamStart);
        
        var stepDelay = durationMs / steps;
        for (int i = 1; i <= steps; i++)
        {
            var x = x1 + (x2 - x1) * i / steps;
            var y = y1 + (y2 - y1) * i / steps;
            var lParam = MakeLParam(x, y);
            PostMessage(hWnd, WM_MOUSEMOVE, IntPtr.Zero, lParam);
            await Task.Delay(stepDelay);
        }
        
        var lParamEnd = MakeLParam(x2, y2);
        PostMessage(hWnd, WM_LBUTTONUP, IntPtr.Zero, lParamEnd);
    }
    
    /// <summary>Lấy kích thước cửa sổ</summary>
    public (int Width, int Height) GetWindowSize(IntPtr hWnd)
    {
        if (GetClientRect(hWnd, out var rect))
        {
            return (rect.Width, rect.Height);
        }
        return (0, 0);
    }
    
    private static IntPtr MakeLParam(int x, int y)
    {
        return (IntPtr)((y << 16) | (x & 0xFFFF));
    }
}
