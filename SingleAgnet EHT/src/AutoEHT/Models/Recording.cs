using System.IO;
using System.Text.Json;

namespace AutoEHT.Models;

/// <summary>
/// Một recording chứa nhiều thao tác đã ghi
/// </summary>
public class Recording
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Name { get; set; } = "New Recording";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public List<RecordedAction> Actions { get; set; } = [];
    
    public void AddAction(RecordedAction action) => Actions.Add(action);
    
    public void Clear() => Actions.Clear();
    
    public int TotalDuration => Actions.Count > 0 ? (int)Actions.Max(a => a.Timestamp) : 0;
    
    public string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
    
    public static Recording? FromJson(string json)
    {
        try { return JsonSerializer.Deserialize<Recording>(json); }
        catch { return null; }
    }
    
    public async Task SaveAsync(string path)
    {
        await File.WriteAllTextAsync(path, ToJson());
    }
    
    public static async Task<Recording?> LoadAsync(string path)
    {
        if (!File.Exists(path)) return null;
        var json = await File.ReadAllTextAsync(path);
        return FromJson(json);
    }
}
