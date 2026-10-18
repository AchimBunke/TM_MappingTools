using GBX.NET.Engines.Meta;
using TmEssentials;
using static GBX.NET.Engines.Meta.NPlugDyna_SKinematicConstraint;

namespace TM_MappingTools.Model;

/// <summary>One step of a translation/rotation animation.</summary>
public sealed class AnimationStep
{
    public AnimEase Ease { get; set; } = AnimEase.Linear;
    public bool Reverse { get; set; }
    public int DurationMs { get; set; } = 1000;

    public AnimationStep Clone() => (AnimationStep)MemberwiseClone();
}

/// <summary>A translation or rotation animation: a short sequence of steps (the game supports at most four).</summary>
public sealed class AnimationConfig
{
    public const int MaxSteps = 4;

    public List<AnimationStep> Steps { get; set; } = new();

    public bool CanAddStep => Steps.Count < MaxSteps;
    public int TotalMs => Steps.Sum(s => Math.Max(0, s.DurationMs));

    public AnimationConfig Clone() => new() { Steps = Steps.Select(s => s.Clone()).ToList() };

    internal static AnimationConfig From(AnimFunc? func) => new()
    {
        // Anything beyond the supported number of steps is dropped.
        Steps = (func?.SubFuncs ?? [])
            .Take(MaxSteps)
            .Select(s => new AnimationStep
            {
                Ease = s.Ease,
                Reverse = s.Reverse,
                DurationMs = (int)s.Duration.TotalMilliseconds,
            })
            .ToList(),
    };

    internal AnimFunc ToAnimFunc(AnimFunc? original) => new()
    {
        IsDuration = original?.IsDuration ?? true,
        SubFuncs = Steps
            .Take(MaxSteps)
            .Select(s => new SubAnimFunc
            {
                Ease = s.Ease,
                Reverse = s.Reverse,
                Duration = new TimeInt32(Math.Max(0, s.DurationMs)),
            })
            .ToArray(),
    };
}

/// <summary>
/// The editable part of a kinematic constraint (how a moving part moves). Everything that is not edited
/// (shader texture animation, ...) is kept from the constraint the values are applied to.
/// </summary>
public sealed class MovementConfig
{
    /// <summary>False as long as the values are only the ones found in the template/item (nothing is overwritten then).</summary>
    public bool Customized { get; set; }

    public EAxis TransAxis { get; set; }
    public float TransMin { get; set; }
    public float TransMax { get; set; }
    public AnimationConfig Translation { get; set; } = new();

    public EAxis RotAxis { get; set; }
    public float AngleMinDeg { get; set; }
    public float AngleMaxDeg { get; set; }
    public AnimationConfig Rotation { get; set; } = new();

    public static MovementConfig FromConstraint(NPlugDyna_SKinematicConstraint constraint) => new()
    {
        TransAxis = constraint.TransAxis,
        TransMin = constraint.TransMin,
        TransMax = constraint.TransMax,
        Translation = AnimationConfig.From(constraint.TransAnimFunc),
        RotAxis = constraint.RotAxis,
        AngleMinDeg = constraint.AngleMinDeg,
        AngleMaxDeg = constraint.AngleMaxDeg,
        Rotation = AnimationConfig.From(constraint.RotAnimFunc),
    };

    public void ApplyTo(NPlugDyna_SKinematicConstraint constraint)
    {
        constraint.TransAxis = TransAxis;
        constraint.TransMin = TransMin;
        constraint.TransMax = TransMax;
        constraint.TransAnimFunc = Translation.ToAnimFunc(constraint.TransAnimFunc);
        constraint.RotAxis = RotAxis;
        constraint.AngleMinDeg = AngleMinDeg;
        constraint.AngleMaxDeg = AngleMaxDeg;
        constraint.RotAnimFunc = Rotation.ToAnimFunc(constraint.RotAnimFunc);
    }
}
