namespace AutoEHT.Scripts;

/// <summary>
/// Base class cho các Script - sử dụng Window Handle để chụp hình và điều khiển (nhanh hơn ADB)
/// Kế thừa class này và override phương thức Run() để viết script
/// </summary>
public abstract class GameScript
{
    protected Services.WindowService Window = null!;
    protected Services.IImageMatchService Matcher = null!;
    protected IntPtr Handle;  // Window handle của LDPlayer
    protected CancellationToken Token;
    
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    
    public void Init(Services.WindowService window, Services.IImageMatchService matcher, IntPtr handle, CancellationToken token)
    {
        Window = window;
        Matcher = matcher;
        Handle = handle;
        Token = token;
    }
    
    /// <summary>Override this to write your script</summary>
    public abstract Task Run();
    
    // ==========================================
    // CÁC HÀM CƠ BẢN ĐỂ VIẾT SCRIPT
    // ==========================================
    
    /// <summary>Đợi (ms)</summary>
    protected async Task Wait(int ms)
    {
        await Task.Delay(ms, Token);
    }
    
    /// <summary>Click vào tọa độ</summary>
    protected async Task Click(int x, int y)
    {
        Log($"👆 Click ({x}, {y})");
        await Window.ClickAsync(Handle, x, y);
    }
    
    /// <summary>Vuốt</summary>
    protected async Task Swipe(int x1, int y1, int x2, int y2, int durationMs = 300)
    {
        Log($"👆 Swipe ({x1},{y1}) → ({x2},{y2})");
        await Window.SwipeAsync(Handle, x1, y1, x2, y2, durationMs);
    }
    
    /// <summary>Cuộn lên</summary>
    protected Task ScrollUp(int x = 270, int y1 = 540, int y2 = 820) => Swipe(x, y1, x, y2, 400);
    
    /// <summary>Cuộn xuống</summary>
    protected Task ScrollDown(int x = 270, int y1 = 820, int y2 = 540) => Swipe(x, y1, x, y2, 400);
    
    /// <summary>Chụp màn hình</summary>
    protected byte[]? CaptureScreen()
    {
        using var bmp = Window.CaptureWindow(Handle);
        //bmp.Save("aaaaa.png");
        return bmp != null ? Window.BitmapToBytes(bmp) : null;
    }
    
    /// <summary>Tìm hình - trả về true nếu thấy</summary>
    protected bool Find(string templateKey)
    {
        var screenshot = CaptureScreen();
        if (screenshot == null) return false;
        var result = Matcher.FindTemplate(screenshot, templateKey);
        return result.Found;
    }
    
    /// <summary>Tìm hình và click vào vị trí của nó</summary>
    protected async Task<bool> FindAndClick(string templateKey, int delayAfter = 300)
    {
        var screenshot = CaptureScreen();
        if (screenshot == null) return false;
        var result = Matcher.FindTemplate(screenshot, templateKey);
        if (result.Found)
        {
            Log($"✅ {templateKey} ({result.X}, {result.Y})");
            await Window.ClickAsync(Handle, result.X, result.Y);
            await Task.Delay(delayAfter, Token);
            return true;
        }
        return false;
    }
    
    /// <summary>Đợi cho đến khi thấy hình (timeout)</summary>
    protected async Task<bool> WaitFor(string templateKey, int timeoutMs = 10000, int pollMs = 200)
    {
        var elapsed = 0;
        while (elapsed < timeoutMs && !Token.IsCancellationRequested)
        {
            if (Find(templateKey)) return true;
            await Task.Delay(pollMs, Token);
            elapsed += pollMs;
        }
        Log($"⏰ Timeout: {templateKey}");
        return false;
    }
    
    /// <summary>Đợi hình xuất hiện rồi click</summary>
    protected async Task<bool> WaitAndClick(string templateKey, int timeoutMs = 10000, int delayAfter = 300)
    {
        if (await WaitFor(templateKey, timeoutMs))
        {
            return await FindAndClick(templateKey, delayAfter);
        }
        return false;
    }
    
    /// <summary>Cuộn lên hết đầu danh sách</summary>
    protected async Task ScrollToTop(int times = 5)
    {
        for (int i = 0; i < times && !Token.IsCancellationRequested; i++)
        {
            await ScrollUp();
            await Task.Delay(100, Token);
        }
    }
    
    /// <summary>Cuộn tìm hình - cuộn từng bước và kiểm tra ngay</summary>
    protected async Task<bool> ScrollAndFind(string templateKey, int maxScrolls = 10)
    {
        // Kiểm tra ngay trước khi cuộn
        if (Find(templateKey)) return true;
        
        // Cuộn xuống từng bước và kiểm tra
        for (int i = 0; i < maxScrolls && !Token.IsCancellationRequested; i++)
        {
            await ScrollDown();
            await Wait(150);
            if (Find(templateKey)) return true;
        }
        return false;
    }
    
    /// <summary>Cuộn tìm và click - cuộn từng bước và thao tác ngay khi thấy</summary>
    protected async Task<bool> ScrollFindAndClick(string templateKey, int delayAfter = 300, int maxScrolls = 10)
    {
        // Kiểm tra và click ngay trước khi cuộn
        if (await FindAndClick(templateKey, delayAfter)) return true;
        
        // Cuộn xuống từng bước, kiểm tra và click ngay khi thấy
        for (int i = 0; i < maxScrolls && !Token.IsCancellationRequested; i++)
        {
            await ScrollDown();
            await Wait(150);
            if (await FindAndClick(templateKey, delayAfter)) return true;
        }
        Log($"❌ Không tìm thấy {templateKey}");
        return false;
    }
    
    /// <summary>Lặp lại action n lần</summary>
    protected async Task Repeat(int times, Func<int, Task> action)
    {
        for (int i = 1; i <= times && !Token.IsCancellationRequested; i++)
        {
            await action(i);
        }
    }
    
    protected bool IsCancelled => Token.IsCancellationRequested;
    protected void Log(string message) => Console.WriteLine($"[{Name}] {message}");
}
