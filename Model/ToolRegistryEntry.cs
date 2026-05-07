namespace TM_MappingTools.Model;

public enum ToolSelectionActive
{
    All,
    Clip,
    Map,
    Item,
}

public record ToolRegistryEntry(
    string Category,
    string Href,
    string IconClass,
    string Title,
    string Description,
    ToolSelectionActive SelectionActive,
    IReadOnlyList<string> SourceAliases);
