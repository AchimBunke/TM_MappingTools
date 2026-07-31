namespace TM_MappingTools.Model;

public enum ValidationSeverity { Warning, Error }

public sealed record ValidationIssue(ValidationSeverity Severity, string Message);
