using System;

namespace TM_MappingTools.Model;

public enum ExecuteActionResultStatus
{
    Success,
    Warning,
    Failure
}
public record struct ExecuteActionResult(ExecuteActionResultStatus Status, string? Message = null);
