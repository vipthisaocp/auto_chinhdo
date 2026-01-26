using OpenCvSharp;

namespace AutoEHT.Models;

/// <summary>
/// Represents an image template for matching.
/// Filename format: key.x_y_w_h.png
/// Example: kho_thi_tran.345_882_71_68.png
/// </summary>
public class Template : IDisposable
{
    /// <summary>Template key/identifier (e.g., "kho_thi_tran")</summary>
    public string Key { get; set; } = string.Empty;
    
    /// <summary>Original filename</summary>
    public string FileName { get; set; } = string.Empty;
    
    /// <summary>Full file path</summary>
    public string FilePath { get; set; } = string.Empty;
    
    /// <summary>X position to crop on screenshot</summary>
    public int CropX { get; set; }
    
    /// <summary>Y position to crop on screenshot</summary>
    public int CropY { get; set; }
    
    /// <summary>Width to crop on screenshot</summary>
    public int CropWidth { get; set; }
    
    /// <summary>Height to crop on screenshot</summary>
    public int CropHeight { get; set; }
    
    /// <summary>Pre-loaded template image (thread-safe: clone before use)</summary>
    public Mat? Image { get; private set; }
    
    /// <summary>Match threshold (0.0 - 1.0)</summary>
    public double Threshold { get; set; } = 0.6;
    
    /// <summary>Whether the template is loaded</summary>
    public bool IsLoaded => Image != null && !Image.Empty();
    
    /// <summary>Load template image from file</summary>
    public bool Load()
    {
        try
        {
            if (!System.IO.File.Exists(FilePath)) return false;
            
            Image?.Dispose();
            Image = Cv2.ImRead(FilePath, ImreadModes.Color);
            return !Image.Empty();
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>Clone the template image for thread-safe usage</summary>
    public Mat? CloneImage()
    {
        return Image?.Clone();
    }
    
    /// <summary>Get crop rectangle</summary>
    public Rect GetCropRect() => new(CropX, CropY, CropWidth, CropHeight);
    
    /// <summary>Parse template from filename</summary>
    /// <param name="filePath">Full path to template file</param>
    /// <returns>Template instance or null if invalid format</returns>
    public static Template? FromFile(string filePath)
    {
        try
        {
            var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            var parts = fileName.Split('.');
            
            if (parts.Length < 2) return null;
            
            var key = parts[0];
            var regionParts = parts[1].Split('_');
            
            if (regionParts.Length != 4) return null;
            
            if (!int.TryParse(regionParts[0], out var x) ||
                !int.TryParse(regionParts[1], out var y) ||
                !int.TryParse(regionParts[2], out var w) ||
                !int.TryParse(regionParts[3], out var h))
            {
                return null;
            }
            
            return new Template
            {
                Key = key,
                FileName = System.IO.Path.GetFileName(filePath),
                FilePath = filePath,
                CropX = x,
                CropY = y,
                CropWidth = w,
                CropHeight = h
            };
        }
        catch
        {
            return null;
        }
    }
    
    public void Dispose()
    {
        Image?.Dispose();
        Image = null;
        GC.SuppressFinalize(this);
    }
}
