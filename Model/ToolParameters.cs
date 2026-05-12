using System.Globalization;
using System.Numerics;

namespace TM_MappingTools.Model;

public enum ToolParameterKind
{
    Bool,
    Int,
    Float,
    Text,
    Enum,
    Vector2,
    Vector3,
    Vector4,
    Quaternion,
    Color,
    Dictionary
}

public sealed class ToolParameterOption
{
    public required string Value { get; init; }
    public required string Label { get; init; }
}

public sealed class DictionaryParameterEntry
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
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
    public bool IsOptional { get; init; }
    public string? GroupKey { get; init; }
    public string? GroupLabel { get; init; }
    public ToolParameterKind DictionaryKeyKind { get; init; } = ToolParameterKind.Text;
    public ToolParameterKind DictionaryValueKind { get; init; } = ToolParameterKind.Text;
    public IReadOnlyList<ToolParameterOption> DictionaryKeyOptions { get; init; } = Array.Empty<ToolParameterOption>();
    public IReadOnlyList<ToolParameterOption> DictionaryValueOptions { get; init; } = Array.Empty<ToolParameterOption>();
    public string DictionaryKeyLabel { get; init; } = "Key";
    public string DictionaryValueLabel { get; init; } = "Value";

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
            ToolParameterKind.Vector2 => Vector2.Zero,
            ToolParameterKind.Vector3 => Vector3.Zero,
            ToolParameterKind.Vector4 => Vector4.Zero,
            ToolParameterKind.Quaternion => Quaternion.Identity,
            ToolParameterKind.Color => "#000000",
            ToolParameterKind.Dictionary => new List<DictionaryParameterEntry>(),
            _ => null,
        };
    }
}

public sealed class ToolParameterValues
{
    private static readonly char[] VectorSplitChars = [',', ';', ' ', '\t', '\r', '\n', '(', ')', '[', ']', '{', '}'];
    private readonly Dictionary<string, object?> values = new(StringComparer.Ordinal);
    private readonly HashSet<string> disabledOptionalKeys = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, object?> Items => values;

    public bool IsParameterEnabled(string key) => !disabledOptionalKeys.Contains(key);

    public void SetParameterEnabled(string key, bool enabled)
    {
        if (enabled) disabledOptionalKeys.Remove(key);
        else disabledOptionalKeys.Add(key);
    }

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

    public Vector2 GetVector2(string key, Vector2 fallback)
    {
        var components = GetVectorComponents(key, 2, [fallback.X, fallback.Y]);
        return new Vector2((float)components[0], (float)components[1]);
    }

    public Vector3 GetVector3(string key, Vector3 fallback)
    {
        var components = GetVectorComponents(key, 3, [fallback.X, fallback.Y, fallback.Z]);
        return new Vector3((float)components[0], (float)components[1], (float)components[2]);
    }

    public Vector4 GetVector4(string key, Vector4 fallback)
    {
        var components = GetVectorComponents(key, 4, [fallback.X, fallback.Y, fallback.Z, fallback.W]);
        return new Vector4((float)components[0], (float)components[1], (float)components[2], (float)components[3]);
    }

    public Quaternion GetQuaternion(string key, Quaternion fallback)
    {
        if (!values.TryGetValue(key, out var value) || value == null)
        {
            return fallback;
        }

        return value switch
        {
            Quaternion quaternionValue => quaternionValue,
            Vector4 vectorValue => new Quaternion(vectorValue.X, vectorValue.Y, vectorValue.Z, vectorValue.W),
            _ => CreateQuaternionFromComponents(GetVectorComponents(value, 4, [fallback.X, fallback.Y, fallback.Z, fallback.W])),
        };
    }

    private static Quaternion CreateQuaternionFromComponents(IReadOnlyList<double> components)
    {
        return new Quaternion((float)components[0], (float)components[1], (float)components[2], (float)components[3]);
    }

    public double GetVectorComponent(string key, int index, int dimensions, IReadOnlyList<double> fallback)
    {
        var components = GetVectorComponents(key, dimensions, fallback);
        return index >= 0 && index < components.Length ? components[index] : 0d;
    }

    public bool TrySetVectorFromString(string key, int dimensions, string? rawText)
    {
        if (!TryParseVectorString(rawText, dimensions, out var valuesOut))
        {
            return false;
        }

        Set(key, valuesOut);
        return true;
    }

    public static bool TryParseVectorString(string? rawText, int dimensions, out double[] valuesOut)
    {
        valuesOut = Array.Empty<double>();

        if (string.IsNullOrWhiteSpace(rawText) || dimensions <= 0)
        {
            return false;
        }

        var parts = rawText
            .Split(VectorSplitChars, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length != dimensions)
        {
            return false;
        }

        var parsed = new double[dimensions];
        for (var i = 0; i < dimensions; i++)
        {
            if (!double.TryParse(parts[i], NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out parsed[i]))
            {
                return false;
            }
        }

        valuesOut = parsed;
        return true;
    }

    private double[] GetVectorComponents(string key, int dimensions, IReadOnlyList<double> fallback)
    {
        if (!values.TryGetValue(key, out var value) || value == null)
        {
            return NormalizeFallback(fallback, dimensions);
        }

        return GetVectorComponents(value, dimensions, fallback);
    }

    private static double[] GetVectorComponents(object value, int dimensions, IReadOnlyList<double> fallback)
    {
        return value switch
        {
            double[] doubles => CopyWithFallback(doubles.Select(v => v).ToArray(), dimensions, fallback),
            float[] floats => CopyWithFallback(floats.Select(v => (double)v).ToArray(), dimensions, fallback),
            Vector2 vec2 => CopyWithFallback([vec2.X, vec2.Y], dimensions, fallback),
            Vector3 vec3 => CopyWithFallback([vec3.X, vec3.Y, vec3.Z], dimensions, fallback),
            Vector4 vec4 => CopyWithFallback([vec4.X, vec4.Y, vec4.Z, vec4.W], dimensions, fallback),
            Quaternion quaternion => CopyWithFallback([quaternion.X, quaternion.Y, quaternion.Z, quaternion.W], dimensions, fallback),
            string text when TryParseVectorString(text, dimensions, out var parsed) => parsed,
            _ => NormalizeFallback(fallback, dimensions),
        };
    }

    private static double[] CopyWithFallback(IReadOnlyList<double> source, int dimensions, IReadOnlyList<double> fallback)
    {
        var result = NormalizeFallback(fallback, dimensions);
        var count = Math.Min(source.Count, dimensions);

        for (var i = 0; i < count; i++)
        {
            result[i] = source[i];
        }

        return result;
    }

    private static double[] NormalizeFallback(IReadOnlyList<double> fallback, int dimensions)
    {
        var result = new double[dimensions];
        var count = Math.Min(fallback.Count, dimensions);
        for (var i = 0; i < count; i++)
        {
            result[i] = fallback[i];
        }

        return result;
    }

    public List<DictionaryParameterEntry> GetDictionaryEntries(string key)
    {
        if (values.TryGetValue(key, out var val) && val is List<DictionaryParameterEntry> list)
            return list;
        var newList = new List<DictionaryParameterEntry>();
        values[key] = newList;
        return newList;
    }
}