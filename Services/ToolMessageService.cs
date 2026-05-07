namespace TM_MappingTools.Services;

public enum ToolMessageLevel
{
    Info,
    Success,
    Warning,
    Error
}

public record ToolMessage(
    ToolMessageLevel Level,
    string Text,
    string? Source = null,
    DateTimeOffset Timestamp = default);

public class ToolMessageService
{
    public IReadOnlyList<ToolMessage> Messages => _messages;
    public event Action? MessagesChanged;
    public event Action? RevealRequested;

    private readonly List<ToolMessage> _messages = new();

    public void Add(ToolMessageLevel level, string text, string? source = null)
    {
        _messages.Add(new ToolMessage(level, text, source, DateTimeOffset.Now));
        MessagesChanged?.Invoke();
    }

    public void AddInfo(string text, string? source = null) => Add(ToolMessageLevel.Info, text, source);
    public void AddSuccess(string text, string? source = null) => Add(ToolMessageLevel.Success, text, source);
    public void AddWarning(string text, string? source = null) => Add(ToolMessageLevel.Warning, text, source);
    public void AddError(string text, string? source = null) => Add(ToolMessageLevel.Error, text, source);

    public void Clear()
    {
        _messages.Clear();
        MessagesChanged?.Invoke();
    }

    public void RequestReveal()
    {
        RevealRequested?.Invoke();
    }
}
