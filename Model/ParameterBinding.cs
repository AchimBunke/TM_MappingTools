using System.Drawing;
using System.Numerics;

namespace TM_MappingTools.Model;

/// <summary>
/// One editable parameter bound to a model object via typed lambdas.
/// <typeparamref name="TCtx"/> is the root object the bindings resolve against, <typeparamref name="TSection"/> groups bindings into UI sections.
/// </summary>
public sealed class ParameterBinding<TCtx, TSection>
{
    public required ToolParameterDefinition Definition { get; init; }
    public required TSection Section { get; init; }
    public required Func<TCtx, bool> AppliesTo { get; init; }
    /// <summary>Reads the current model value.</summary>
    public required Func<TCtx, object?> Read { get; init; }
    /// <summary>Writes the value from <see cref="ToolParameterValues"/> into the model if it differs. Returns true if changed.</summary>
    public required Func<TCtx, ToolParameterValues, bool> Apply { get; init; }

    public string Key => Definition.Key;
}

/// <summary>
/// Fluent builder that creates <see cref="ParameterBinding{TCtx,TSection}"/> entries (and their <see cref="ToolParameterDefinition"/>)
/// for properties of <typeparamref name="T"/>, which is resolved from the context via a selector.
/// </summary>
public class ParameterBinder<TCtx, TSection, T> where T : class
{
    private readonly List<ParameterBinding<TCtx, TSection>> _out;
    private readonly TSection _section;
    private readonly Func<TCtx, T?> _target;
    private readonly string _prefix;
    private string? _group;
    private string? _groupLabel;

    public ParameterBinder(List<ParameterBinding<TCtx, TSection>> output, TSection section, Func<TCtx, T?> target, string prefix)
    {
        _out = output;
        _section = section;
        _target = target;
        _prefix = prefix;
    }

    /// <summary>Subsequent parameters are shown inline in this group. Pass null to end grouping.</summary>
    public void Group(string? key, string? label = null)
    {
        _group = key;
        _groupLabel = label;
    }

    private void Add(string key, string label, string tip, ToolParameterKind kind, object? def,
        Func<T, object?> read, Func<T, ToolParameterValues, string, bool> apply)
    {
        var fullKey = _prefix + key;
        var definition = new ToolParameterDefinition
        {
            Key = fullKey,
            Label = label,
            Tooltip = tip,
            Kind = kind,
            DefaultValue = def,
            GroupKey = _group,
            GroupLabel = _groupLabel,
        };
        var target = _target;
        _out.Add(new ParameterBinding<TCtx, TSection>
        {
            Definition = definition,
            Section = _section,
            AppliesTo = c => target(c) != null,
            Read = c => read(target(c)!),
            Apply = (c, v) => apply(target(c)!, v, fullKey),
        });
    }

    public void Float(string key, string label, string tip, Func<T, float> get, Action<T, float> set)
        => Add(key, label, tip, ToolParameterKind.Float, 0f, n => get(n), (n, v, k) =>
        {
            var cur = get(n);
            var nv = v.GetFloat(k, cur);
            if (nv.Equals(cur)) return false;
            set(n, nv);
            return true;
        });

    public void Int(string key, string label, string tip, Func<T, int> get, Action<T, int> set)
        => Add(key, label, tip, ToolParameterKind.Int, 0, n => get(n), (n, v, k) =>
        {
            var cur = get(n);
            var nv = v.GetInt(k, cur);
            if (nv == cur) return false;
            set(n, nv);
            return true;
        });

    public void Bool(string key, string label, string tip, Func<T, bool> get, Action<T, bool> set)
        => Add(key, label, tip, ToolParameterKind.Bool, false, n => get(n), (n, v, k) =>
        {
            var cur = get(n);
            var nv = v.GetBool(k, cur);
            if (nv == cur) return false;
            set(n, nv);
            return true;
        });

    public void Text(string key, string label, string tip, Func<T, string> get, Action<T, string> set)
        => Add(key, label, tip, ToolParameterKind.Text, "", n => get(n), (n, v, k) =>
        {
            var cur = get(n);
            var nv = v.GetString(k, cur);
            if (nv == cur) return false;
            set(n, nv);
            return true;
        });

    public void Vector3(string key, string label, string tip, Func<T, Vector3> get, Action<T, Vector3> set)
        => Add(key, label, tip, ToolParameterKind.Vector3, System.Numerics.Vector3.Zero, n => get(n), (n, v, k) =>
        {
            var cur = get(n);
            var nv = v.GetVector3(k, cur);
            if (nv == cur) return false;
            set(n, nv);
            return true;
        });

    public void Color(string key, string label, string tip, Func<T, System.Drawing.Color> get, Action<T, System.Drawing.Color> set)
        => Color(key, label, tip, get, set, c => c, (_, c) => c);

    /// <summary>Color from a Vector3 with each component in 0-1 (clamped for display; unchanged colors keep their exact value).</summary>
    public void Color(string key, string label, string tip, Func<T, Vector3> get, Action<T, Vector3> set)
        => Color(key, label, tip, get, set,
            v => System.Drawing.Color.FromArgb(ToByte(v.X), ToByte(v.Y), ToByte(v.Z)),
            (_, c) => new Vector3(c.R / 255f, c.G / 255f, c.B / 255f));

    /// <summary>Color from a Vector4 (RGBA) with each component in 0-1. Alpha is preserved from the current value.</summary>
    public void Color(string key, string label, string tip, Func<T, Vector4> get, Action<T, Vector4> set)
        => Color(key, label, tip, get, set,
            v => System.Drawing.Color.FromArgb(ToByte(v.X), ToByte(v.Y), ToByte(v.Z)),
            (cur, c) => new Vector4(c.R / 255f, c.G / 255f, c.B / 255f, cur.W));

    /// <summary>Generic color binding. <paramref name="fromColor"/> receives the current value and the edited color.</summary>
    protected void Color<TV>(string key, string label, string tip, Func<T, TV> get, Action<T, TV> set,
        Func<TV, System.Drawing.Color> toColor, Func<TV, System.Drawing.Color, TV> fromColor)
        => Add(key, label, tip, ToolParameterKind.Color, "#000000", n => ToolParameterValues.ColorToHex(toColor(get(n))), (n, v, k) =>
        {
            var cur = get(n);
            var curColor = toColor(cur);
            var nv = v.GetColor(k, curColor);
            if (ToolParameterValues.ColorToHex(nv) == ToolParameterValues.ColorToHex(curColor)) return false;
            set(n, fromColor(cur, nv));
            return true;
        });

    private static int ToByte(float f) => (int)Math.Round(Math.Clamp(f, 0f, 1f) * 255f);

    public void Quaternion(string key, string label, string tip, Func<T, Quaternion> get, Action<T, Quaternion> set)
        => Add(key, label, tip, ToolParameterKind.Quaternion, System.Numerics.Quaternion.Identity, n => get(n), (n, v, k) =>
        {
            var cur = get(n);
            var nv = v.GetQuaternion(k, cur);
            if (nv == cur) return false;
            set(n, nv);
            return true;
        });


}
