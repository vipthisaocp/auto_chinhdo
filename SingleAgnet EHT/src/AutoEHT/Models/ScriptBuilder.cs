using AutoEHT.Models.Actions;

namespace AutoEHT.Models;

/// <summary>
/// Fluent builder for creating scripts easily.
/// Usage: ScriptBuilder.Create("id", "name").Wait(1000).Click(100, 200).Build()
/// </summary>
public class ScriptBuilder
{
    private readonly Script _script;
    private readonly List<ScriptAction> _actions = [];
    
    private ScriptBuilder(string id, string name)
    {
        _script = new Script { Id = id, Name = name };
    }
    
    public static ScriptBuilder Create(string id, string name) => new(id, name);
    
    public ScriptBuilder Description(string desc) { _script.Description = desc; return this; }
    public ScriptBuilder Loop(int count = -1) { _script.LoopCount = count; return this; }
    
    // ===== WAIT =====
    public ScriptBuilder Wait(int ms, string? desc = null)
    {
        _actions.Add(new WaitAction { DurationMs = ms, Description = desc ?? $"Đợi {ms}ms" });
        return this;
    }
    
    // ===== CLICK =====
    public ScriptBuilder Click(int x, int y, int delayAfter = 300, string? desc = null)
    {
        _actions.Add(new ClickAction 
        { 
            TargetType = ClickTarget.Coordinate, 
            X = x, Y = y, 
            DelayAfter = delayAfter,
            Description = desc ?? $"Click ({x}, {y})"
        });
        return this;
    }
    
    public ScriptBuilder ClickTemplate(string templateName, int delayAfter = 300, string? desc = null)
    {
        _actions.Add(new ClickAction 
        { 
            TargetType = ClickTarget.Template, 
            TemplateName = templateName,
            DelayAfter = delayAfter,
            Description = desc ?? $"Click {templateName}"
        });
        return this;
    }
    
    // ===== WAIT FOR =====
    public ScriptBuilder WaitFor(string templateName, int timeoutMs = 10000, int pollMs = 500, string? desc = null)
    {
        _actions.Add(new WaitForAction 
        { 
            TemplateName = templateName, 
            TimeoutMs = timeoutMs, 
            PollIntervalMs = pollMs,
            Description = desc ?? $"Đợi {templateName}"
        });
        return this;
    }
    
    // ===== SWIPE =====
    public ScriptBuilder Swipe(int x1, int y1, int x2, int y2, int durationMs = 300, string? desc = null)
    {
        _actions.Add(new SwipeAction 
        { 
            StartX = x1, StartY = y1, 
            EndX = x2, EndY = y2, 
            DurationMs = durationMs,
            Description = desc ?? $"Swipe ({x1},{y1}) → ({x2},{y2})"
        });
        return this;
    }
    
    public ScriptBuilder ScrollUp(int x = 270, int y1 = 540, int y2 = 820, string? desc = null) 
        => Swipe(x, y1, x, y2, 400, desc ?? "Cuộn lên");
    
    public ScriptBuilder ScrollDown(int x = 270, int y1 = 820, int y2 = 540, string? desc = null) 
        => Swipe(x, y1, x, y2, 400, desc ?? "Cuộn xuống");
    
    // ===== CONDITION =====
    public ScriptBuilder If(string templateName, Action<ScriptBuilder> thenActions, Action<ScriptBuilder>? elseActions = null, string? desc = null)
    {
        var thenBuilder = new ScriptBuilder("", "");
        thenActions(thenBuilder);
        
        var elseBuilder = new ScriptBuilder("", "");
        elseActions?.Invoke(elseBuilder);
        
        _actions.Add(new ConditionAction 
        { 
            TemplateName = templateName,
            ThenActions = thenBuilder._actions,
            ElseActions = elseBuilder._actions,
            Description = desc ?? $"If {templateName}"
        });
        return this;
    }
    
    // ===== REPEAT =====
    public ScriptBuilder Repeat(int times, Action<ScriptBuilder, int> action)
    {
        for (int i = 1; i <= times; i++)
        {
            action(this, i);
        }
        return this;
    }
    
    // ===== COMPOSITE ACTIONS =====
    
    /// <summary>Find template with scroll: scroll up first, if not found scroll down</summary>
    public ScriptBuilder FindWithScroll(string templateName, int scrollX = 270, int scrollY1 = 540, int scrollY2 = 820)
    {
        // Scroll up first
        ScrollUp(scrollX, scrollY1, scrollY2, $"Cuộn tìm {templateName}");
        Wait(500);
        
        // Condition: found or scroll down
        If(templateName,
            then => then.ClickTemplate(templateName, 800),
            els => els
                .ScrollDown(scrollX, scrollY2, scrollY1, "Cuộn xuống tìm tiếp")
                .Wait(500)
                .WaitFor(templateName, 5000, 500)
                .ClickTemplate(templateName, 800)
        );
        
        return this;
    }
    
    /// <summary>Find and decompose an item</summary>
    public ScriptBuilder FindAndDecompose(string itemTemplate, string decomposeTemplate = "phan_ra", int times = 1)
    {
        Repeat(times, (b, i) =>
        {
            b.FindWithScroll(itemTemplate);
            b.WaitFor(decomposeTemplate, 5000, 300);
            b.ClickTemplate(decomposeTemplate, 1000, $"Phân rã {itemTemplate} (lần {i})");
        });
        return this;
    }
    
    // ===== BUILD =====
    public Script Build()
    {
        _script.Actions = _actions;
        return _script;
    }
}
