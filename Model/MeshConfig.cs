using TM_GenericMapping.Items.MeshCompilation;

namespace TM_MappingTools.Model;

/// <summary>
/// Role / display configuration of a single mesh, shape or light reference.
/// Keyed by the reference id, so a model that is placed multiple times shares one configuration.
/// </summary>
public class MeshConfig
{
    public bool Enabled { get; set; } = true;
    public bool Visible { get; set; } = false;
    public bool Collidable { get; set; } = false;

    /// <summary>Output role of the part: Static, Dynamic (movable), Trigger_Special (effect) or Trigger_Waypoint.</summary>
    public ModelType Role { get; set; } = ModelType.Static;

    /// <summary>
    /// Optional: key of another model whose group of the same role this part takes its settings from
    /// (movement, effect, waypoint type). null = group of its own model.
    /// </summary>
    public int? ShareWithModel { get; set; }

    public bool HasLOD { get; set; } = false;
    public int LODMask { get; set; } = 1;

    /// <summary>True for parts created with "Duplicate".</summary>
    public bool IsCopy { get; set; }

    public bool IsTrigger => Role is ModelType.Trigger_Special or ModelType.Trigger_Waypoint;
    public bool IsMovable => Role == ModelType.Dynamic;

    public MeshConfig Clone() => (MeshConfig)MemberwiseClone();
}

public class ItemConfig
{
    public List<float> LODDistances { get; set; } = [];
}
