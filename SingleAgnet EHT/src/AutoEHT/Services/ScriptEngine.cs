using AutoEHT.Models;
using AutoEHT.Models.Actions;

namespace AutoEHT.Services;

/// <summary>
/// Engine for executing automation scripts
/// </summary>
public class ScriptEngine : IScriptEngine
{
    private readonly IAdbService _adbService;
    private readonly IImageMatchService _imageMatchService;
    private readonly AppSettings _settings;
    
    public event EventHandler<LogEntry>? ActionExecuted;
    
    public ScriptEngine(IAdbService adbService, IImageMatchService imageMatchService, AppSettings settings)
    {
        _adbService = adbService;
        _imageMatchService = imageMatchService;
        _settings = settings;
    }
    
    public async Task RunAsync(string serial, Script script, CancellationToken cancellationToken = default)
    {
        var loopCount = script.LoopCount;
        var currentLoop = 0;
        
        Log(serial, LogLevel.Info, $"Starting script: {script.Name}");
        
        while (!cancellationToken.IsCancellationRequested)
        {
            currentLoop++;
            
            if (loopCount > 0 && currentLoop > loopCount)
            {
                break;
            }
            
            Log(serial, LogLevel.Info, $"Loop {currentLoop}" + (loopCount > 0 ? $"/{loopCount}" : "/∞"));
            
            foreach (var action in script.Actions)
            {
                if (cancellationToken.IsCancellationRequested) break;
                
                var success = await ExecuteActionAsync(serial, action, cancellationToken);
                if (!success)
                {
                    Log(serial, LogLevel.Warning, $"Action failed: {action.Description ?? action.Type.ToString()}");
                }
            }
        }
        
        Log(serial, LogLevel.Info, $"Script stopped: {script.Name}");
    }
    
    public async Task<bool> ExecuteActionAsync(string serial, ScriptAction action, CancellationToken cancellationToken = default)
    {
        return action switch
        {
            ClickAction click => await ExecuteClickAsync(serial, click),
            WaitAction wait => await ExecuteWaitAsync(wait, cancellationToken),
            WaitForAction waitFor => await ExecuteWaitForAsync(serial, waitFor, cancellationToken),
            SwipeAction swipe => await ExecuteSwipeAsync(serial, swipe),
            ConditionAction condition => await ExecuteConditionAsync(serial, condition, cancellationToken),
            _ => false
        };
    }
    
    private async Task<bool> ExecuteClickAsync(string serial, ClickAction click)
    {
        int x, y;
        
        if (click.TargetType == ClickTarget.Template && !string.IsNullOrEmpty(click.TemplateName))
        {
            var match = await _imageMatchService.FindTemplateOnDeviceAsync(serial, click.TemplateName);
            if (!match.Found)
            {
                Log(serial, LogLevel.Warning, $"Template not found: {click.TemplateName}");
                return false;
            }
            x = match.X + click.OffsetX;
            y = match.Y + click.OffsetY;
        }
        else
        {
            x = click.X ?? 0;
            y = click.Y ?? 0;
        }
        
        if (click.DelayBefore > 0)
        {
            await Task.Delay(click.DelayBefore);
        }
        
        var success = await _adbService.TapAsync(serial, x, y);
        Log(serial, success ? LogLevel.Success : LogLevel.Error, $"Click at ({x}, {y})", ActionType.Click);
        
        if (click.DelayAfter > 0)
        {
            await Task.Delay(click.DelayAfter);
        }
        
        return success;
    }
    
    private async Task<bool> ExecuteWaitAsync(WaitAction wait, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(wait.DurationMs, cancellationToken);
            return true;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }
    
    private async Task<bool> ExecuteWaitForAsync(string serial, WaitForAction waitFor, CancellationToken cancellationToken)
    {
        var timeout = waitFor.TimeoutMs > 0 ? waitFor.TimeoutMs : _settings.WaitForDefaultTimeout;
        var pollInterval = waitFor.PollIntervalMs > 0 ? waitFor.PollIntervalMs : _settings.WaitForPollInterval;
        var elapsed = 0;
        
        Log(serial, LogLevel.Info, $"Waiting for: {waitFor.TemplateName}", ActionType.WaitFor);
        
        while (elapsed < timeout && !cancellationToken.IsCancellationRequested)
        {
            var match = await _imageMatchService.FindTemplateOnDeviceAsync(serial, waitFor.TemplateName);
            if (match.Found)
            {
                Log(serial, LogLevel.Success, $"Found: {waitFor.TemplateName} at ({match.X}, {match.Y})", ActionType.WaitFor);
                return true;
            }
            
            await Task.Delay(pollInterval, cancellationToken);
            elapsed += pollInterval;
        }
        
        Log(serial, LogLevel.Warning, $"Timeout waiting for: {waitFor.TemplateName}", ActionType.WaitFor);
        return false;
    }
    
    private async Task<bool> ExecuteSwipeAsync(string serial, SwipeAction swipe)
    {
        var success = await _adbService.SwipeAsync(serial, swipe.StartX, swipe.StartY, swipe.EndX, swipe.EndY, swipe.DurationMs);
        Log(serial, success ? LogLevel.Success : LogLevel.Error, 
            $"Swipe ({swipe.StartX},{swipe.StartY}) → ({swipe.EndX},{swipe.EndY})", ActionType.Swipe);
        return success;
    }
    
    private async Task<bool> ExecuteConditionAsync(string serial, ConditionAction condition, CancellationToken cancellationToken)
    {
        var match = await _imageMatchService.FindTemplateOnDeviceAsync(serial, condition.TemplateName);
        var actions = match.Found ? condition.ThenActions : condition.ElseActions;
        var branch = match.Found ? "THEN" : "ELSE";
        
        Log(serial, LogLevel.Info, $"Condition [{condition.TemplateName}]: {branch}", ActionType.Condition);
        
        foreach (var action in actions)
        {
            if (cancellationToken.IsCancellationRequested) break;
            await ExecuteActionAsync(serial, action, cancellationToken);
        }
        
        return true;
    }
    
    public void StopAll()
    {
        // Cancellation is handled by the caller through CancellationToken
    }
    
    private void Log(string instanceName, LogLevel level, string message, ActionType? actionType = null)
    {
        var entry = new LogEntry(DateTime.Now, level, instanceName, message, actionType);
        ActionExecuted?.Invoke(this, entry);
    }
}
