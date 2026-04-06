using System;

namespace TM_MappingTools.Services;

public class OperationHistoryService
{
    public List<string> OperationsHistory { get; private set; } = new();
    public event Action? HistoryChanged;

    public void AddOperation(string operationKey)
    {
        OperationsHistory.Add(operationKey);
        HistoryChanged?.Invoke();
    }

    public void Clear()
    {
        OperationsHistory.Clear();
        HistoryChanged?.Invoke();
    }
}
