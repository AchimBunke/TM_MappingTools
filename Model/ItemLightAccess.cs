using System.Numerics;
using GBX.NET;
using GBX.NET.Engines.GameData;
using GBX.NET.Engines.Plug;
using TM_GenericMapping.Common;
using TM_GenericMapping.Items;

namespace TM_MappingTools.Model;

public sealed class LightEntry
{
    public LightEntry(CPlugSolid2Model model, int index, string modelName)
    {
        Model = model;
        Index = index;
        ModelName = modelName;
    }

    public CPlugSolid2Model Model { get; }
    public int Index { get; }
    public string ModelName { get; }
    public CPlugSolid2Model.Light Light => Model.Lights![Index];
    public bool IsEditable => Light.U03?.Light != null;
    public LightKind Kind => LightKinds.GetKind(Light.U03?.Light);
}

public static class ItemLightAccess
{
    public static IReadOnlyList<(string Name, CPlugSolid2Model Model)> GetModels(CGameItemModel item)
    {
        var result = new List<(string, CPlugSolid2Model)>();
        var seen = new HashSet<CPlugSolid2Model>(ReferenceEqualityComparer.Instance);

        void Add(string name, CPlugSolid2Model? model)
        {
            if (model != null && seen.Add(model))
                result.Add((name, model));
        }

        if (item.EntityModel is CGameCommonItemEntityModel common)
        {
            Add("Static", common.StaticObject?.Mesh);
        }
        else if (item.EntityModel is CPlugPrefab prefab)
        {
            int i = 0;
            foreach (var s in ItemExtensions.Traverse<CPlugStaticObjectModel>(prefab))
                Add($"Static {++i}", s.Mesh);
            i = 0;
            foreach (var d in ItemExtensions.Traverse<CPlugDynaObjectModel>(prefab))
                Add($"Dyna {++i}", d.Mesh);
        }
        return result;
    }

    public static List<LightEntry> GetEntries(CGameItemModel item)
    {
        var entries = new List<LightEntry>();
        var models = GetModels(item);
        foreach (var (name, model) in models)
        {
            var lights = model.Lights;
            if (lights == null) continue;
            for (int i = 0; i < lights.Length; i++)
                entries.Add(new LightEntry(model, i, models.Count > 1 ? name : ""));
        }
        return entries;
    }

    public static int CountUserLights(CGameItemModel item)
        => GetModels(item).Sum(m => m.Model.LightUserModels?.Length ?? 0);

    public static void ConvertUserLights(CGameItemModel item)
        => new ItemLightConverter().ConvertLightUserModels(item);

    /// <summary>Maps to the library light type. Throws for kinds that have no library equivalent.</summary>
    public static LightType ToLightType(LightKind kind) => kind switch
    {
        LightKind.Point => LightType.Point,
        LightKind.Spot => LightType.Spot,
        LightKind.Frustum => LightType.Frustum,
        LightKind.Directional => LightType.Directional,
        LightKind.Ambient => LightType.Ambient,
        _ => throw new NotSupportedException($"Light kind {kind} cannot be created."),
    };

    /// <summary>Creates a light via the library. Kinds not yet implemented by the library throw <see cref="NotImplementedException"/>.</summary>
    public static CPlugSolid2Model.Light CreateLight(LightKind kind)
    {
        var light = ItemLightHelper.CreateEmptyLight(ToLightType(kind));
        light.U05 = Iso4.IsoFromTransform(Vector3.Zero, Quaternion.Identity);
        return light;
    }

    public static CPlugSolid2Model.Light AddLight(CPlugSolid2Model model, LightKind kind)
    {
        var light = CreateLight(kind);
        model.Lights = [.. model.Lights ?? [], light];
        return light;
    }

    public static CPlugSolid2Model.Light Duplicate(CPlugSolid2Model model, CPlugSolid2Model.Light source)
    {
        var light = CreateLight(LightKinds.GetKind(source.U03?.Light));
        LightParameterCatalog.Apply(new LightContext(light), LightParameterCatalog.Load(new LightContext(source)));
        model.Lights = [.. model.Lights ?? [], light];
        return light;
    }

    /// <summary>Labels of currently applicable parameters that would be lost when switching to <paramref name="kind"/>.</summary>
    public static IReadOnlyList<string> GetDroppedParameters(CPlugSolid2Model.Light light, LightKind kind)
    {
        var current = new LightContext(light);
        var probe = new LightContext(CreateLight(kind));
        return LightParameterCatalog.Applicable(current)
            .Where(b => b.Section is LightSection.TypeSpecific or LightSection.Advanced && !b.AppliesTo(probe))
            .Select(b => b.Definition.Label)
            .ToList();
    }

    /// <summary>Replaces the GxLight with one of the given kind, carrying over all shared parameter values.</summary>
    public static void SwitchKind(CPlugSolid2Model.Light light, LightKind kind)
    {
        var plug = light.U03 ?? throw new InvalidOperationException("Light has no embedded CPlugLight.");
        var carried = LightParameterCatalog.Load(new LightContext(light));

        plug.Light = ItemLightHelper.CreateGxLight(ToLightType(kind));

        var newCtx = new LightContext(light);
        foreach (var b in LightParameterCatalog.Applicable(newCtx))
        {
            if (b.Section is LightSection.Common or LightSection.TypeSpecific && carried.Contains(b.Key))
                b.Apply(newCtx, carried);
        }
    }

    public static void Delete(CPlugSolid2Model model, int index)
    {
        var lights = model.Lights;
        if (lights == null || index < 0 || index >= lights.Length) return;
        model.Lights = [.. lights.Where((_, i) => i != index)];
    }
}
