namespace TM_MappingTools.Model;

/// <summary>
/// Describes the display / role configuration of a single submesh for the 3-D preview panel.
/// </summary>
public class MeshConfig
{
    public bool Enabled { get; set; } = false;
    public bool Visible { get; set; } = false;
    public bool Collidable { get; set; } = false;
    public bool Trigger_Waypoint { get; set; } = false;
    public bool Trigger_Effect { get; set; } = false;
    public bool Movable { get; set; } = false;
    public int LODMask { get; set; } = 1;
    public bool HasLOD { get; set; } = false;
}

public class ItemConfig
{
    public List<float> LODDistances { get; set; } = [];
}