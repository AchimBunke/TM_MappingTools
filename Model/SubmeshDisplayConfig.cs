namespace TM_MappingTools.Model;

/// <summary>
/// Describes the display / role configuration of a single submesh for the 3-D preview panel.
/// </summary>
public sealed class SubmeshDisplayConfig
{
    public bool Enabled  { get; set; }
    public bool Visible  { get; set; }
    public bool Collidable { get; set; }
    public bool Trigger  { get; set; }
    public bool Movable  { get; set; }
}
