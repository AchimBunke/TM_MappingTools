using System.Globalization;

namespace TM_MappingTools.Model;

public enum ToolParameterKind
{
    Bool,
    Int,
    Float,
    Text,
    Enum
}

public sealed class ToolParameterOption
{
    public required string Value { get; init; }
    public required string Label { get; init; }
}

public sealed class ToolParameterDefinition
{
    public required string Key { get; init; }
    public required string Label { get; init; }
    public string? Tooltip { get; init; }
    public ToolParameterKind Kind { get; init; }
    public bool LongValue { get; init; }
    public object? DefaultValue { get; init; }
    public double? Min { get; init; }
    public double? Max { get; init; }
    public double? Step { get; init; }
    public IReadOnlyList<ToolParameterOption> Options { get; init; } = Array.Empty<ToolParameterOption>();

    public object? GetDefaultValue()
    {
        if (DefaultValue != null)
        {
            return DefaultValue;
        }

        return Kind switch
        {
            ToolParameterKind.Bool => false,
            ToolParameterKind.Int => 0,
            ToolParameterKind.Float => 0d,
            ToolParameterKind.Text => string.Empty,
            ToolParameterKind.Enum => Options.FirstOrDefault()?.Value ?? string.Empty,
            _ => null,
        };
    }
}

public sealed class ToolParameterValues
{
    private readonly Dictionary<string, object?> values = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, object?> Items => values;

    public bool Contains(string key)
    {
        return values.ContainsKey(key);
    }

    public void Set(string key, object? value)
    {
        values[key] = value;
    }

    public object? GetRaw(string key)
    {
        return values.TryGetValue(key, out var value) ? value : null;
    }

    public string GetString(string key, string fallback = "")
    {
        if (!values.TryGetValue(key, out var value) || value == null)
        {
            return fallback;
        }

        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? fallback;
    }

    public bool GetBool(string key, bool fallback = false)
    {
        if (!values.TryGetValue(key, out var value) || value == null)
        {
            return fallback;
        }

        if (value is bool boolValue)
        {
            return boolValue;
        }

        if (bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsedBool))
        {
            return parsedBool;
        }

        return fallback;
    }

    public int GetInt(string key, int fallback = 0)
    {
        if (!values.TryGetValue(key, out var value) || value == null)
        {
            return fallback;
        }

        if (value is int intValue)
        {
            return intValue;
        }

        if (int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedInt))
        {
            return parsedInt;
        }

        return fallback;
    }

    public double GetDouble(string key, double fallback = 0)
    {
        if (!values.TryGetValue(key, out var value) || value == null)
        {
            return fallback;
        }

        if (value is double doubleValue)
        {
            return doubleValue;
        }

        if (value is float floatValue)
        {
            return floatValue;
        }

        if (double.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsedDouble))
        {
            return parsedDouble;
        }

        return fallback;
    }

    public float GetFloat(string key, float fallback = 0){
        return GetDouble(key, fallback) is double doubleValue ? (float)doubleValue : fallback;
    }
    
}