using TM_MappingTools.Model;

namespace TM_MappingTools.Services;

public class ToolRegistryService
{
    public IReadOnlyList<ToolRegistryEntry> ToolEntries { get; } = new List<ToolRegistryEntry>
    {
        new(
            Category: "Clip",
            Href: "player-attached-triangles",
            IconClass: "bi-paperclip",
            Title: "Player-Attached 3D Triangles",
            Description: "Attach 3D triangles to the player.",
            SourceAliases: new[] { "player-attached", "triangles", "player triangles" }),

        new(
            Category: "Clip",
            Href: "optimize-keyframe-animations",
            IconClass: "bi-bezier",
            Title: "Optimize Keyframe Animations",
            Description: "Reduce redundant animation keyframes to shrink filesize.",
            SourceAliases: new[] { "keyframe", "optimize keyframe" }),

        new(
            Category: "Clip",
            Href: "optimize-triangle-compression",
            IconClass: "bi-grid-3x3",
            Title: "Optimize Triangle Compression",
            Description: "Improve MediaTracker triangle compression.",
            SourceAliases: new[] { "triangle compression", "optimize triangle" }),

        new(
            Category: "Clip",
            Href: "import-triangle-animation",
            IconClass: "bi-collection-play",
            Title: "Import Triangle Animation",
            Description: "Import .obj animation from Blender and create triangle animations.",
            SourceAliases: new[] { "import triangle", "triangle animation import" }),

        new(
            Category: "Map",
            Href: "map-item-replacer",
            IconClass: "bi-shuffle",
            Title: "Map Item Replacer",
            Description: "Coming Soon...",
            SourceAliases: new[] { "map replacer", "item replacer" }),

        new(
            Category: "Item",
            Href: "moving-item-creator",
            IconClass: "bi-box-arrow-in-right",
            Title: "Moving Item Creator",
            Description: "Create moving items from source items.",
            SourceAliases: new[] { "moving item", "item creator" }),

        new(
            Category: "Map",
            Href: "embedded-item-extractor",
            IconClass: "bi-box-arrow-up",
            Title: "Embedded Item Extractor",
            Description: "Extract embedded items and blocks from a map.",
            SourceAliases: new[] { "embedded item", "item extractor" }),
    };

    public string GetIconForSource(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return "bi-tools";

        var s = source.Trim();

        var exact = ToolEntries.FirstOrDefault(t =>
            string.Equals(t.Title, s, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(t.Href, s, StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
            return exact.IconClass;

        var partial = ToolEntries.FirstOrDefault(t =>
            s.Contains(t.Title, StringComparison.OrdinalIgnoreCase) ||
            s.Contains(t.Href, StringComparison.OrdinalIgnoreCase) ||
            t.SourceAliases.Any(a => s.Contains(a, StringComparison.OrdinalIgnoreCase)));
        if (partial is not null)
            return partial.IconClass;

        return "bi-tools";
    }
}
