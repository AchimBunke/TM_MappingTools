namespace TM_MappingTools.Model;

/// <summary>
/// Represents a single file that can be exported/downloaded by the user.
/// GetBytes is evaluated lazily at download time for in-place-modified files,
/// or returns pre-captured bytes for generated content.
/// </summary>
public class ExportableItem
{
    public required string FileName { get; init; }
    public required Func<byte[]> GetBytes { get; init; }
}
