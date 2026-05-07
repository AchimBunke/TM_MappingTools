using TM_GenericMapping.Messaging;
using TM_MappingTools.Model;

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
    DateTimeOffset Timestamp = default,
    IReadOnlyList<ToolMessage>? InnerMessages = null,
    string? TechnicalDetails = null)
{
    public bool HasInnerMessages => InnerMessages is { Count: > 0 };
    public bool HasTechnicalDetails => !string.IsNullOrWhiteSpace(TechnicalDetails);
}

public class ToolMessageService
{
    public IReadOnlyList<ToolMessage> Messages => _messages;
    public event Action? MessagesChanged;
    public event Action? RevealRequested;

    private readonly List<ToolMessage> _messages = new();

    public void Add(ToolMessageLevel level, string text, string? source = null,
        IReadOnlyList<ToolMessage>? innerMessages = null,
        string? technicalDetails = null)
    {
        _messages.Add(new ToolMessage(level, text, source, DateTimeOffset.Now, innerMessages, technicalDetails));
        MessagesChanged?.Invoke();
    }

    public void AddInfo(string text, string? source = null, IReadOnlyList<ToolMessage>? innerMessages = null, string? technicalDetails = null)    => Add(ToolMessageLevel.Info,    text, source, innerMessages, technicalDetails);
    public void AddSuccess(string text, string? source = null, IReadOnlyList<ToolMessage>? innerMessages = null, string? technicalDetails = null) => Add(ToolMessageLevel.Success, text, source, innerMessages, technicalDetails);
    public void AddWarning(string text, string? source = null, IReadOnlyList<ToolMessage>? innerMessages = null, string? technicalDetails = null) => Add(ToolMessageLevel.Warning, text, source, innerMessages, technicalDetails);
    public void AddError(string text, string? source = null, IReadOnlyList<ToolMessage>? innerMessages = null, string? technicalDetails = null)   => Add(ToolMessageLevel.Error,   text, source, innerMessages, technicalDetails);

    public void AddException(Exception ex, string? userMessage = null, string? source = null)
    {
        var message = string.IsNullOrWhiteSpace(userMessage)
            ? ex.Message
            : userMessage;
        AddError(message, source, technicalDetails: ex.ToString());
    }

    public void AddError(string text, IToolResult errorResult, string? source = null)
    {
        var innerMessages = AccumulateInnerMessages(errorResult);
        AddError(text, source, innerMessages);
    }
    List<ToolMessage> AccumulateInnerMessages(IToolResult errorResult)
    {
        List<ToolMessage> messages = new();
        object? currentData = errorResult;
        while(currentData != null && currentData is IToolResult toolResult && toolResult.IsFailure)
        {
            messages.Add(new ToolMessage(
                ToolMessageLevel.Error, 
                ErrorCodeToMessage.GetMessage(toolResult.ErrorCode!),
                toolResult.ToolId,
                DateTimeOffset.Now,
                TechnicalDetails: toolResult.ToString()));
            currentData = toolResult.Data;
        }
        return messages;
    }

    public string BuildLogText()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"TM_MappingTools Output Log - {DateTimeOffset.Now:O}");
        sb.AppendLine(new string('=', 72));

        foreach (var message in _messages)
        {
            AppendMessage(sb, message, indentLevel: 0);
        }

        return sb.ToString();
    }

    public Stream BuildLogStream()
    {
        var logText = BuildLogText();
        return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(logText));
    }

    private static void AppendMessage(System.Text.StringBuilder sb, ToolMessage message, int indentLevel)
    {
        var indent = new string(' ', indentLevel * 2);
        sb.Append(indent)
          .Append('[').Append(message.Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")).Append("] ")
          .Append('[').Append(message.Level).Append("] ");

        if (!string.IsNullOrWhiteSpace(message.Source))
        {
            sb.Append('[').Append(message.Source).Append("] ");
        }

        sb.AppendLine(message.Text);

        if (message.HasTechnicalDetails)
        {
            sb.Append(indent).AppendLine("  Technical Details:");
            foreach (var line in message.TechnicalDetails!.Split('\n'))
            {
                sb.Append(indent).Append("    ").AppendLine(line.TrimEnd('\r'));
            }
        }

        if (!message.HasInnerMessages)
        {
            return;
        }

        foreach (var inner in message.InnerMessages!)
        {
            AppendMessage(sb, inner, indentLevel + 1);
        }
    }

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
