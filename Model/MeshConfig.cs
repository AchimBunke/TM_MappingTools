namespace TM_MappingTools.Model;

/// <summary>
/// Describes the display / role configuration of a single submesh for the 3-D preview panel.
/// </summary>
public class SubMeshConfig
{
    public bool Enabled { get; set; } = false;
    public bool Visible { get; set; } = false;
    public bool Collidable { get; set; } = false;
    public bool Trigger { get; set; } = false;
    public bool Movable { get; set; } = false;
    public int LODMask { get; set; } = 1;
    public bool HasLOD { get; set; } = false;
}

public class MeshConfig
{
    public List<float> LODDistances { get; set; } = [];
}