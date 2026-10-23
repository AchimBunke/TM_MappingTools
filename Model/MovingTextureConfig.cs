using GBX.NET.Engines.Meta;
using TmEssentials;
using static GBX.NET.Engines.Meta.NPlugDyna_SKinematicConstraint;

namespace TM_MappingTools.Model;

/// <summary>One frame of the moving texture: which sub texture is shown for how long.</summary>
public sealed class TextureAnimStep
{
    public int DurationMs { get; set; } = 1000;
    public int TextureId { get; set; }

    public TextureAnimStep Clone() => (TextureAnimStep)MemberwiseClone();
}

/// <summary>
/// The moving texture (sub texture animation) of a kinematic constraint. Mirrors the shader texture coordinate
/// settings (ShaderTcType = TransSubTexture) the item compiler writes.
/// </summary>
public sealed class MovingTextureConfig
{
    /// <summary>False as long as the values are only the ones found in the template/item (nothing is overwritten then).</summary>
    public bool Customized { get; set; }

    /// <summary>When false the constraint gets no moving texture.</summary>
    public bool Enabled { get; set; }

    /// <summary>Number of sub textures. Texture ids outside 0..SubTextures-1 use the last texture.</summary>
    public int SubTextures { get; set; } = 8;
    public int SubTexturesPerLine { get; set; } = 1;
    public int SubTexturesPerColumn { get; set; } = 8;

    /// <summary>Count the texture indices from top to bottom instead of bottom to top.</summary>
    public bool TopToBottom { get; set; }

    public const int MaxSteps = 12;

    public List<TextureAnimStep> Steps { get; set; } = new()
    {
        new TextureAnimStep { TextureId = 0, DurationMs = 1000 },
        new TextureAnimStep { TextureId = 1, DurationMs = 1000 },
    };

    public bool CanAddStep => Steps.Count < MaxSteps;

    public static MovingTextureConfig FromConstraint(NPlugDyna_SKinematicConstraint constraint)
    {
        var enabled = constraint.ShaderTcType == EShaderTcType.TransSubTexture;
        var config = new MovingTextureConfig { Enabled = enabled };
        if (!enabled)
            return config;

        var data = constraint.ShaderTcDataTransSub;
        config.SubTextures = data.NbSubTexture;
        config.SubTexturesPerLine = data.NbSubTexturePerLine;
        config.SubTexturesPerColumn = data.NbSubTexturePerColumn;
        config.TopToBottom = data.TopToBottom;
        config.Steps = (constraint.ShaderTcAnimFunc ?? [])
            .Take(MaxSteps)
            .Select(a => new TextureAnimStep { DurationMs = (int)a.Duration.TotalMilliseconds, TextureId = a.TextureId })
            .ToList();
        return config;
    }

    public void ApplyTo(NPlugDyna_SKinematicConstraint constraint)
    {
        if (!Enabled)
        {
            constraint.ShaderTcType = EShaderTcType.None;
            constraint.ShaderTcAnimFunc = [];
            return;
        }

        constraint.ShaderTcType = EShaderTcType.TransSubTexture;
        constraint.ShaderTcDataTransSub = new TransSubTextureIn
        {
            NbSubTexture = SubTextures,
            NbSubTexturePerLine = SubTexturesPerLine,
            NbSubTexturePerColumn = SubTexturesPerColumn,
            TopToBottom = TopToBottom,
        };
        constraint.ShaderTcAnimFunc = Steps
            .Take(MaxSteps)
            .Select(s => new AnimFuncNat
            {
                Duration = new TimeInt32(Math.Max(0, s.DurationMs)),
                TextureId = s.TextureId,
            })
            .ToArray();
    }
}
