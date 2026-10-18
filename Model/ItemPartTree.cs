using System.Numerics;
using GBX.NET;
using TM_GenericMapping.Items.MeshCompilation;

namespace TM_MappingTools.Model;

public enum PartKind
{
    Mesh,
    Shape,
    Light,
}

/// <summary>
/// One mesh / shape / light reference of a model as placed in the item (a shared model yields one placement per use).
/// </summary>
public sealed class ItemPartPlacement
{
    /// <summary>Index within <see cref="ItemPartTree.PreviewParts"/> (and the 3D preview). -1 for lights.</summary>
    public int PreviewIndex { get; init; } = -1;
    public required Guid RefId { get; init; }
    public required PartKind Kind { get; init; }
    public required string Name { get; set; }
    public string Tooltip { get; init; } = "";
    public int VertexCount { get; init; }
    public ShapeRole? ShapeRole { get; init; }

    /// <summary>Key of the mesh / shape data in the item pool (null for lights).</summary>
    public object? GeometryKey { get; init; }

    /// <summary>1-based id of shared mesh / shape data (same id = same data), 0 if only one part uses it.</summary>
    public int GeometryLinkIndex { get; set; }
    public int GeometryUseCount { get; set; } = 1;
    public string GeometryLinkTooltip { get; set; } = "";

    public Vec3[] Positions { get; init; } = [];
    public Vec3[] Normals { get; init; } = [];
    public int[] Indices { get; init; } = [];

    public Vector3 WorldPosition { get; init; }
    public Quaternion WorldRotation { get; init; } = Quaternion.Identity;
}

public sealed class ItemTreeNode
{
    public required string Key { get; init; }

    /// <summary>Friendly model name (e.g. "Static 2") or variant description.</summary>
    public required string Label { get; init; }

    /// <summary>Secondary info: placement position, rotation hint, ...</summary>
    public string Detail { get; init; } = "";
    public required int ModelKey { get; init; }
    public required ModelType ModelType { get; init; }

    /// <summary>The entity that places this model (null for the root).</summary>
    public EntityRefBase? Entity { get; init; }
    public Vector3 WorldPosition { get; init; }
    public Quaternion WorldRotation { get; init; } = Quaternion.Identity;

    /// <summary>How many times the underlying model is placed in the whole item.</summary>
    public int UseCount { get; set; } = 1;

    /// <summary>1-based id of the shared model (same id = same linked model), 0 if the model is used once.</summary>
    public int LinkIndex { get; set; }
    public bool IsLeaf => ModelType is not (ModelType.Container or ModelType.Variant_List);
    public List<ItemTreeNode> Children { get; } = [];
    public List<ItemPartPlacement> Parts { get; } = [];
}

/// <summary>
/// Read-only view of a <see cref="NormalizedItem"/> as a tree of placed models, plus a flat list
/// of previewable (mesh / shape) placements with baked world transforms.
/// </summary>
public sealed class ItemPartTree
{
    public ItemTreeNode Root { get; private set; } = null!;
    public IReadOnlyList<ItemPartPlacement> PreviewParts => _previewParts;

    private readonly List<ItemPartPlacement> _previewParts = [];
    private readonly Dictionary<int, List<ItemTreeNode>> _uses = [];
    private readonly Dictionary<int, string> _modelNames = [];
    private NormalizedItem _item = null!;

    public static ItemPartTree Build(NormalizedItem item)
    {
        var tree = new ItemPartTree { _item = item };
        var rootKey = item.ModelPool.FirstOrDefault(kv => ReferenceEquals(kv.Value, item.Model)).Key;
        tree.AssignModelNames(rootKey);
        tree.Root = tree.BuildNode("root", tree.NameOf(rootKey), "", null, rootKey, item.Model, Vector3.Zero, Quaternion.Identity, 0);

        int link = 0;
        foreach (var node in tree.Nodes())
        {
            var uses = tree._uses[node.ModelKey];
            node.UseCount = uses.Count;
        }
        var linkIds = new Dictionary<int, int>();
        foreach (var node in tree.Nodes())
        {
            if (node.UseCount <= 1) continue;
            if (!linkIds.TryGetValue(node.ModelKey, out var id))
                linkIds[node.ModelKey] = id = ++link;
            node.LinkIndex = id;
        }
        tree.AssignGeometryLinks();
        return tree;
    }

    /// <summary>Marks parts (distinct mesh/shape references) that use the same mesh/shape data.</summary>
    private void AssignGeometryLinks()
    {
        var users = new Dictionary<(PartKind, object), List<(Guid RefId, string Model)>>();
        foreach (var node in Nodes())
        {
            foreach (var part in node.Parts)
            {
                if (part.GeometryKey == null) continue;
                var id = (part.Kind, part.GeometryKey);
                if (!users.TryGetValue(id, out var list))
                    users[id] = list = [];
                if (!list.Any(u => u.RefId == part.RefId))
                    list.Add((part.RefId, NameOf(node.ModelKey)));
            }
        }

        var linkIds = new Dictionary<(PartKind, object), int>();
        int link = 0;
        foreach (var node in Nodes())
        {
            foreach (var part in node.Parts)
            {
                if (part.GeometryKey == null) continue;
                var id = (part.Kind, part.GeometryKey);
                var list = users[id];
                if (list.Count <= 1) continue;
                if (!linkIds.TryGetValue(id, out var linkId))
                    linkIds[id] = linkId = ++link;
                var kind = part.Kind == PartKind.Mesh ? "mesh" : "shape";
                part.GeometryLinkIndex = linkId;
                part.GeometryUseCount = list.Count;
                part.GeometryLinkTooltip =
                    $"Linked {kind}: the same {kind} data is used by {list.Count} parts ({string.Join(", ", list.Select(u => u.Model).Distinct())}). Each part keeps its own settings.";
            }
        }
    }

    /// <summary>All nodes depth-first, starting at the root.</summary>
    public IEnumerable<ItemTreeNode> Nodes() => Flatten(Root);

    /// <summary>Friendly name of a model, e.g. "Static 2", "Moving", "Effect trigger 1".</summary>
    public string NameOf(int modelKey) => _modelNames.GetValueOrDefault(modelKey, $"Model {modelKey}");

    /// <summary>Every placement of the given model.</summary>
    public IReadOnlyList<ItemTreeNode> UsesOf(int modelKey) => _uses.TryGetValue(modelKey, out var l) ? l : [];

    public static string FormatVector(Vector3 v)
        => FormattableString.Invariant($"({v.X:0.##}, {v.Y:0.##}, {v.Z:0.##})");

    private IEnumerable<ItemTreeNode> Flatten(ItemTreeNode node)
    {
        yield return node;
        foreach (var child in node.Children)
            foreach (var n in Flatten(child))
                yield return n;
    }

    private static string TypeName(ModelType type) => type switch
    {
        ModelType.Container => "Group",
        ModelType.Static => "Static",
        ModelType.Dynamic => "Moving",
        ModelType.Trigger_Special => "Effect trigger",
        ModelType.Trigger_Waypoint => "Waypoint trigger",
        ModelType.Variant_List => "Variants",
        _ => type.ToString(),
    };

    private void AssignModelNames(int rootKey)
    {
        foreach (var group in _item.ModelPool.Where(kv => kv.Key != rootKey).OrderBy(kv => kv.Key).GroupBy(kv => kv.Value.Type))
        {
            int n = 0;
            int total = group.Count();
            foreach (var kv in group)
            {
                n++;
                _modelNames[kv.Key] = total > 1 ? $"{TypeName(group.Key)} {n}" : TypeName(group.Key);
            }
        }
        _modelNames[rootKey] = "Item";
    }

    private static string DescribePlacement(Vector3 pos, Quaternion rot)
    {
        var parts = new List<string>();
        if (pos != Vector3.Zero)
            parts.Add($"at {FormatVector(pos)}");
        if (MathF.Abs(MathF.Abs(rot.W) - 1f) > 1e-4f)
            parts.Add("rotated");
        return string.Join(", ", parts);
    }

    private ItemTreeNode BuildNode(
        string key, string label, string detail, EntityRefBase? entity, int modelKey, NormalizedModel model,
        Vector3 worldPos, Quaternion worldRot, int depth)
    {
        var node = new ItemTreeNode
        {
            Key = key,
            Label = label,
            Detail = detail,
            ModelKey = modelKey,
            ModelType = model.Type,
            Entity = entity,
            WorldPosition = worldPos,
            WorldRotation = worldRot,
        };
        if (!_uses.TryGetValue(modelKey, out var uses))
            _uses[modelKey] = uses = [];
        uses.Add(node);

        foreach (var meshRef in model.Meshes)
        {
            if (!_item.MeshPool.TryGetValue(meshRef.MeshKey, out var mesh))
                continue;
            var part = new ItemPartPlacement
            {
                PreviewIndex = _previewParts.Count,
                RefId = meshRef.Id,
                Kind = PartKind.Mesh,
                GeometryKey = meshRef.MeshKey,
                Name = string.IsNullOrWhiteSpace(mesh.Name) ? "Mesh" : mesh.Name,
                Tooltip = $"Mesh, {mesh.Positions.Length} vertices, {mesh.Indices.Length / 3} triangles",
                VertexCount = mesh.Positions.Length,
                Positions = mesh.Positions,
                Normals = mesh.Normals,
                Indices = mesh.Indices,
                WorldPosition = worldPos,
                WorldRotation = worldRot,
            };
            _previewParts.Add(part);
            node.Parts.Add(part);
        }

        foreach (var shapeRef in model.Shapes)
        {
            if (!_item.ShapePool.TryGetValue(shapeRef.ShapeKey, out var shape))
                continue;
            var part = new ItemPartPlacement
            {
                PreviewIndex = _previewParts.Count,
                RefId = shapeRef.Id,
                Kind = PartKind.Shape,
                GeometryKey = shapeRef.ShapeKey,
                Name = shapeRef.Role switch
                {
                    ShapeRole.Dynamic => "Moving collision shape",
                    ShapeRole.Trigger_Special => "Effect trigger shape",
                    ShapeRole.Trigger_Waypoint => "Waypoint trigger shape",
                    _ => "Collision shape",
                },
                Tooltip = $"Invisible shape (originally {shapeRef.Role}), {shape.Positions.Length} vertices, {shape.Indices.Length / 3} triangles",
                VertexCount = shape.Positions.Length,
                ShapeRole = shapeRef.Role,
                Positions = shape.Positions,
                Indices = shape.Indices,
                WorldPosition = worldPos,
                WorldRotation = worldRot,
            };
            _previewParts.Add(part);
            node.Parts.Add(part);
        }

        foreach (var lightRef in model.Lights)
        {
            node.Parts.Add(new ItemPartPlacement
            {
                RefId = lightRef.Id,
                Kind = PartKind.Light,
                Name = "Light",
                Tooltip = "Light",
                WorldPosition = worldPos + Vector3.Transform(lightRef.Position, worldRot),
                WorldRotation = worldRot * lightRef.Rotation,
            });
        }

        // number parts that would otherwise look identical
        foreach (var group in node.Parts.GroupBy(p => (p.Kind, p.Name)).Where(g => g.Count() > 1).ToList())
        {
            int n = 1;
            foreach (var part in group)
                part.Name = $"{part.Name} {n++}";
        }

        if (depth > 32)
            return node; // guard against cyclic data

        foreach (var child in model.Children)
        {
            if (!_item.ModelPool.TryGetValue(child.ModelKey, out var childModel))
                continue;
            var pos = worldPos + Vector3.Transform(child.Position, worldRot);
            var rot = worldRot * child.Rotation;
            node.Children.Add(BuildNode(
                child.Id.ToString(), NameOf(child.ModelKey), DescribePlacement(child.Position, child.Rotation),
                child, child.ModelKey, childModel, pos, rot, depth + 1));
        }

        foreach (var variant in model.Variants)
        {
            if (!_item.ModelPool.TryGetValue(variant.ModelKey, out var variantModel))
                continue;
            var tags = variant.Tags.Count == 0
                ? "untagged"
                : string.Join(", ", variant.Tags.Select(t => $"{t.Key}={t.Value}"));
            node.Children.Add(BuildNode(
                variant.Id.ToString(), $"Variant: {tags}", $"uses {NameOf(variant.ModelKey)}",
                variant, variant.ModelKey, variantModel, worldPos, worldRot, depth + 1));
        }

        return node;
    }
}
