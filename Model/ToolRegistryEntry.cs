namespace TM_MappingTools.Model;

public record ToolRegistryEntry(
    string Category,
    string Href,
    string IconClass,
    string Title,
    string Description,
    IReadOnlyList<string> SourceAliases);
