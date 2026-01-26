using System.IO;
using AutoEHT.Models;
using OpenCvSharp;

namespace AutoEHT.Services;

/// <summary>
/// Service for image template matching using OpenCV.
/// Templates are pre-loaded and cloned for thread-safety.
/// </summary>
public class ImageMatchService : IImageMatchService
{
    private readonly IAdbService _adbService;
    private readonly AppSettings _settings;
    private readonly Dictionary<string, Template> _templates = new();
    private readonly object _lock = new();
    
    public ImageMatchService(IAdbService adbService, AppSettings settings)
    {
        _adbService = adbService;
        _settings = settings;
        LoadAllTemplates();
    }
    
    /// <summary>Load all templates from the templates folder</summary>
    private void LoadAllTemplates()
    {
        var templatesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _settings.TemplatesFolder);
        Directory.CreateDirectory(templatesFolder);
        
        var files = Directory.GetFiles(templatesFolder, "*.png");
        foreach (var file in files)
        {
            var template = Template.FromFile(file);
            if (template != null && template.Load())
            {
                lock (_lock)
                {
                    _templates[template.Key] = template;
                }
                Console.WriteLine($"[Template] Loaded: {template.Key} (crop: {template.CropX},{template.CropY} {template.CropWidth}x{template.CropHeight})");
            }
        }
        
        Console.WriteLine($"[Template] Total loaded: {_templates.Count}");
    }
    
    public async void ReloadTemplates()
    {
        lock (_lock)
        {
            foreach (var template in _templates.Values)
            {
                template.Dispose();
            }
            _templates.Clear();
        }
        
        await Task.Run(LoadAllTemplates);
    }
    
    public MatchResult FindTemplate(byte[] screenshot, string templateKey)
    {
        Template? template;
        lock (_lock)
        {
            if (!_templates.TryGetValue(templateKey, out template) || !template.IsLoaded)
            {
                return MatchResult.NotFound(templateKey);
            }
        }
        
        try
        {
            using var screenshotMat = Cv2.ImDecode(screenshot, ImreadModes.Color);
            if (screenshotMat.Empty())
            {
                return MatchResult.NotFound(templateKey);
            }
            
            using var templateMat = template.CloneImage();
            if (templateMat == null || templateMat.Empty())
            {
                return MatchResult.NotFound(templateKey);
            }
            
            var cropRect = template.GetCropRect();
            
            // Check if crop region is valid
            bool useCrop = cropRect.Width > 0 && cropRect.Height > 0 &&
                           cropRect.X >= 0 && cropRect.Y >= 0 &&
                           cropRect.X + cropRect.Width <= screenshotMat.Width &&
                           cropRect.Y + cropRect.Height <= screenshotMat.Height;
            
            using var searchRegion = useCrop 
                ? new Mat(screenshotMat, cropRect) 
                : screenshotMat.Clone();
            
            // Template matching on search region
            using var result = new Mat();
            Cv2.MatchTemplate(searchRegion, templateMat, result, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(result, out _, out var maxVal, out _, out var maxLoc);
            
            if (maxVal >= template.Threshold)
            {
                // Calculate absolute position
                int x, y;
                if (useCrop)
                {
                    x = cropRect.X + maxLoc.X + templateMat.Width / 2;
                    y = cropRect.Y + maxLoc.Y + templateMat.Height / 2;
                }
                else
                {
                    x = maxLoc.X + templateMat.Width / 2;
                    y = maxLoc.Y + templateMat.Height / 2;
                }
                return new MatchResult(true, x, y, maxVal, templateKey, DateTime.Now);
            }
            
            return MatchResult.NotFound(templateKey);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Match] Error: {ex.Message}");
            return MatchResult.NotFound(templateKey);
        }
    }
    
    public async Task<MatchResult> FindTemplateOnDeviceAsync(string serial, string templateKey)
    {
        var screenshot = await _adbService.CaptureScreenAsync(serial);
        if (screenshot == null || screenshot.Length == 0)
        {
            Console.WriteLine($"[Template] Failed to capture screenshot from {serial}");
            return MatchResult.NotFound(templateKey);
        }
        
        return FindTemplate(screenshot, templateKey);
    }
    
    public IEnumerable<Template> GetAllTemplates()
    {
        lock (_lock)
        {
            return _templates.Values.ToList();
        }
    }
    
    public Template? GetTemplate(string key)
    {
        lock (_lock)
        {
            return _templates.TryGetValue(key, out var template) ? template : null;
        }
    }
    
    public void AddTemplate(Template template)
    {
        if (template.Load())
        {
            lock (_lock)
            {
                _templates[template.Key] = template;
            }
        }
    }
    
    public void RemoveTemplate(string key)
    {
        lock (_lock)
        {
            if (_templates.TryGetValue(key, out var template))
            {
                template.Dispose();
                _templates.Remove(key);
            }
        }
    }
}
