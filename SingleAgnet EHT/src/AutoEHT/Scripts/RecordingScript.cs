using AutoEHT.Models;

namespace AutoEHT.Scripts;

/// <summary>
/// Script để phát lại recording đã ghi
/// </summary>
public class RecordingScript : GameScript
{
    private readonly Recording _recording;
    
    public RecordingScript(Recording recording)
    {
        _recording = recording;
        Id = $"rec-{recording.Id}";
        Name = $"📹 {recording.Name}";
        Description = $"{recording.Actions.Count} actions, {recording.TotalDuration}ms";
    }
    
    public override async Task Run()
    {
        Log($"▶️ Playback: {_recording.Name}");
        
        long lastTimestamp = 0;
        foreach (var action in _recording.Actions)
        {
            if (IsCancelled) break;
            
            // Đợi đúng thời gian
            var delay = (int)(action.Timestamp - lastTimestamp);
            if (delay > 0) await Wait(delay);
            lastTimestamp = action.Timestamp;
            
            // Thực hiện action
            switch (action.Type)
            {
                case RecordActionType.Click:
                    await Click(action.X, action.Y);
                    break;
                    
                case RecordActionType.Swipe:
                    await Swipe(action.X, action.Y, action.EndX, action.EndY, action.Duration);
                    break;
                    
                case RecordActionType.Wait:
                    await Wait(action.Duration);
                    break;
            }
        }
        
        Log($"✅ Playback finished");
    }
}
