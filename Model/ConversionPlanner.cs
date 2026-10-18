using TM_GenericMapping.Common;
using TM_GenericMapping.Items;
using TM_GenericMapping.Items.MeshCompilation;
using TM_GenericMapping.Templating;
using GBX.NET.Engines.Meta;
using static GBX.NET.Engines.GameData.CGameItemModel;

namespace TM_MappingTools.Model;

/// <summary>Build settings created from the part configuration plus info needed to explain them.</summary>
public sealed record ConversionPlan(
    BuildSettings Settings,
    IReadOnlyDictionary<int, Guid> OriginClusterByModel,
    IReadOnlySet<Guid> NewClusters,
    IReadOnlyDictionary<int, Guid> MovingClusterByModel,
    IReadOnlySet<Guid> CustomizedClusters);

public sealed record OutputGroupSummary(ModelType Type, string Origin, string Detail, IReadOnlyList<string> Parts);

public sealed record OutputModelSummary(
    string ModelName, int Uses, string Detail, IReadOnlyList<OutputGroupSummary> Groups, int RemovedParts);

public static class ConversionPlanner
{
    public static string RoleLabel(ModelType role) => role switch
    {
        ModelType.Dynamic => "Movable",
        ModelType.Trigger_Special => "Effect",
        ModelType.Trigger_Waypoint => "Waypoint",
        _ => "Static",
    };

    /// <summary>
    /// Starts from the settings found in the item and applies the part configuration: instance flags, roles
    /// (by moving instances into groups ("clusters") of the matching type) and LOD settings.
    /// Groups never span models: parts of one model that get the same role end up in one group of that model,
    /// parts of different models always end up in different groups (optionally sharing the settings of another model's group).
    /// </summary>
    public static ConversionPlan Create(
        NormalizedItem item,
        IReadOnlyDictionary<Guid, MeshConfig> configs,
        IReadOnlyList<float> lodDistanceList,
        LegacyGameplayId effect,
        EWaypointType? waypointType,
        IReadOnlyDictionary<int, MovementConfig>? movements = null)
    {
        var settings = BuildSettings.DefaultFromItem(item);
        float[] lodDistances = lodDistanceList.ToArray();
        int lodLevels = lodDistances.Length + 1;
        var createdClusters = new Dictionary<(Guid Origin, ModelType Role), Guid>();

        var originByModel = new Dictionary<int, Guid>();
        foreach (var (modelKey, model) in item.ModelPool)
        {
            var firstId = model.Meshes.Select(m => m.Id)
                .Concat(model.Shapes.Select(s => s.Id))
                .Concat(model.Lights.Select(l => l.Id))
                .Cast<Guid?>()
                .FirstOrDefault();
            if (firstId is Guid id && settings.Instances.TryGetValue(id, out var first))
                originByModel[modelKey] = first.ClusterKey;
        }

        Guid ResolveCluster(Guid origin, MeshConfig cfg)
        {
            if (cfg.ShareWithModel is int shareKey
                && originByModel.TryGetValue(shareKey, out var shared)
                && shared != origin
                && settings.Clusters[shared].Type == cfg.Role)
                return shared;

            var source = settings.Clusters[origin];
            if (source.Type == cfg.Role)
                return origin;
            if (!createdClusters.TryGetValue((origin, cfg.Role), out var key))
            {
                key = Guid.NewGuid();
                settings.Clusters[key] = new ClusterSettings
                {
                    Type = cfg.Role,
                    LODDistances = source.LODDistances,
                    GameplayMainDir = source.GameplayMainDir,
                    KinematicConstraint = source.KinematicConstraint,
                    DynaObjectModelParams = source.DynaObjectModelParams,
                    RelativeMovingParentCluster = source.RelativeMovingParentCluster,
                    WaypointSpawnPosition = source.WaypointSpawnPosition,
                    WaypointSpawnRotation = source.WaypointSpawnRotation,
                    Detached = true,
                };
                createdClusters[(origin, cfg.Role)] = key;
            }
            return key;
        }

        foreach (var model in item.ModelPool.Values)
        {
            foreach (var mesh in model.Meshes)
            {
                if (!settings.Instances.TryGetValue(mesh.Id, out var inst) || !configs.TryGetValue(mesh.Id, out var cfg))
                    continue;
                inst.Enabled = cfg.Enabled;
                inst.Visible = cfg.Visible;
                inst.Collidable = cfg.Collidable;
                inst.ClusterKey = ResolveCluster(inst.ClusterKey, cfg);
                // Parts without LOD must stay visible in every level.
                if (cfg.HasLOD)
                    inst.LODMaskOverride = cfg.LODMask;
                else
                    inst.LODMaskOverride = lodLevels > 1 ? LODUtils.GetAllLodsMask(lodLevels) : null;
            }
            foreach (var shape in model.Shapes)
            {
                if (!settings.Instances.TryGetValue(shape.Id, out var inst) || !configs.TryGetValue(shape.Id, out var cfg))
                    continue;
                inst.Enabled = cfg.Enabled;
                inst.Visible = cfg.Visible;
                inst.Collidable = cfg.Collidable;
                var origin = settings.Clusters[inst.ClusterKey];
                if (origin.Type != cfg.Role)
                    inst.ShapeRoleOverride = cfg.Role switch
                    {
                        ModelType.Dynamic => ShapeRole.Dynamic,
                        ModelType.Trigger_Special => ShapeRole.Trigger_Special,
                        ModelType.Trigger_Waypoint => ShapeRole.Trigger_Waypoint,
                        _ => ShapeRole.Static,
                    };
                inst.ClusterKey = ResolveCluster(inst.ClusterKey, cfg);
            }
            foreach (var light in model.Lights)
            {
                if (settings.Instances.TryGetValue(light.Id, out var inst) && configs.TryGetValue(light.Id, out var cfg))
                    inst.Enabled = cfg.Enabled;
            }
        }

        // Prune leaf clusters without enabled instances (an unused cluster could still leak its waypoint type
        // into the item) and apply the shared settings.
        var used = settings.Instances.Values.Where(i => i.Enabled).Select(i => i.ClusterKey).ToHashSet();
        foreach (var key in settings.Clusters.Keys.ToList())
        {
            var cluster = settings.Clusters[key];
            if (cluster.Type is ModelType.Container or ModelType.Variant_List)
                continue;
            if (!used.Contains(key))
            {
                settings.Clusters.Remove(key);
                continue;
            }

            cluster.LODDistances = lodDistances;
            if (cluster.Type == ModelType.Trigger_Special && cluster.TriggerGameplayId == null)
                cluster.TriggerGameplayId = effect;
            if (cluster.Type == ModelType.Trigger_Waypoint && waypointType.HasValue)
                cluster.WaypointType = waypointType;
        }

        // Each model has at most one moving group: its own group if it is moving in the original, else the new one.
        var movingByModel = new Dictionary<int, Guid>();
        var customized = new HashSet<Guid>();
        foreach (var (modelKey, origin) in originByModel)
        {
            Guid? moving = settings.Clusters.TryGetValue(origin, out var originCluster) && originCluster.Type == ModelType.Dynamic
                ? origin
                : createdClusters.TryGetValue((origin, ModelType.Dynamic), out var created) ? created : null;
            if (moving is not Guid movingKey || !settings.Clusters.TryGetValue(movingKey, out var movingCluster))
                continue;

            movingByModel[modelKey] = movingKey;
            if (movements != null && movements.TryGetValue(modelKey, out var movement) && movement.Customized)
            {
                var constraint = ObjectCloner.DeepCloneObject(
                    movingCluster.KinematicConstraint ?? GbxTemplateLibrary.CreateKinematicConstraintTemplate().Value)!;
                movement.ApplyTo(constraint);
                movingCluster.KinematicConstraint = constraint;
                customized.Add(movingKey);
            }
        }

        return new ConversionPlan(settings, originByModel, createdClusters.Values.ToHashSet(), movingByModel, customized);
    }

    /// <summary>
    /// The movement a model's moving group starts with: the one found in the item, otherwise the default movement.
    /// </summary>
    public static MovementConfig BaseMovement(ItemPartTree tree, int modelKey)
    {
        var constraint = tree.Nodes()
            .Where(n => n.ModelKey == modelKey)
            .Select(n => (n.Entity as EntityRef)?.KinematicConstraint)
            .FirstOrDefault(c => c != null)
            ?? GbxTemplateLibrary.CreateKinematicConstraintTemplate().Value;
        return MovementConfig.FromConstraint(constraint);
    }

    /// <summary>Describes, per distinct leaf model, which groups the converted item will contain.</summary>
    public static IReadOnlyList<OutputModelSummary> Summarize(ConversionPlan plan, ItemPartTree tree)
    {
        var modelByOrigin = plan.OriginClusterByModel.GroupBy(kv => kv.Value).ToDictionary(g => g.Key, g => g.First().Key);
        var result = new List<OutputModelSummary>();
        var seen = new HashSet<int>();

        foreach (var node in tree.Nodes())
        {
            if (!node.IsLeaf || node.Parts.Count == 0 || !seen.Add(node.ModelKey))
                continue;

            var enabled = node.Parts
                .Where(p => plan.Settings.Instances.TryGetValue(p.RefId, out var i) && i.Enabled)
                .GroupBy(p => plan.Settings.Instances[p.RefId].ClusterKey)
                .ToList();

            var groups = new List<OutputGroupSummary>();
            foreach (var g in enabled)
            {
                var cluster = plan.Settings.Clusters[g.Key];
                string origin;
                if (plan.OriginClusterByModel.TryGetValue(node.ModelKey, out var own) && own == g.Key)
                    origin = "as in the original";
                else if (modelByOrigin.TryGetValue(g.Key, out var otherModel))
                    origin = $"uses the settings of {tree.NameOf(otherModel)}";
                else
                    origin = "new group";

                string detail = cluster.Type switch
                {
                    ModelType.Dynamic when plan.CustomizedClusters.Contains(g.Key) => "custom movement",
                    ModelType.Dynamic when cluster.KinematicConstraint == null => "default movement",
                    ModelType.Dynamic => "movement from the item",
                    ModelType.Trigger_Special => $"effect: {cluster.TriggerGameplayId}",
                    ModelType.Trigger_Waypoint => $"type: {cluster.WaypointType?.ToString() ?? "Checkpoint"}",
                    _ => "",
                };

                groups.Add(new OutputGroupSummary(
                    cluster.Type, origin, detail,
                    g.Select(p => p.Name).ToList()));
            }

            int removed = node.Parts.Count - enabled.Sum(g => g.Count());
            var uses = tree.UsesOf(node.ModelKey);
            string placement = uses.Count == 1 ? node.Detail : "";
            result.Add(new OutputModelSummary(tree.NameOf(node.ModelKey), uses.Count, placement, groups, removed));
        }

        return result;
    }
}
