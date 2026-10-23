using GBX.NET.Engines.GameData;
using GBX.NET.Engines.Meta;
using GBX.NET.Engines.Plug;

namespace TM_MappingTools.Model;

/// <summary>
/// Sets NPlugDynaObjectModel_SInstanceParams on the dynamic models that are the target (Ent2) of a
/// kinematic constraint. Uses the same entity indexing as the item compiler (index into the dynamic models of the prefab).
/// </summary>
public static class MovingItemInstanceParams
{
    public sealed record Values(
        bool IsKinematic,
        bool CastStaticShadow,
        float PeriodSc,
        float PeriodScMax,
        float Phase01,
        float Phase01Max,
        int TextureId);

    public static void Apply(CGameItemModel item, Values values)
    {
        if (item.EntityModel is CPlugPrefab prefab)
            Apply(prefab, values);
        else if (item.EntityModel is NPlugItem_SVariantList variantList)
        {
            foreach (var variant in variantList.Variants ?? [])
                if (variant.EntityModel is CPlugPrefab variantPrefab)
                    Apply(variantPrefab, values);
        }
    }

    private static void Apply(CPlugPrefab root, Values values)
    {
        var flat = Flatten(root).ToList();
        var dynas = flat.Where(e => e.Model is CPlugDynaObjectModel).ToList();

        foreach (var constraintEnt in flat.Where(e => e.Model is NPlugDyna_SKinematicConstraint))
        {
            if (constraintEnt.Params is not NPlugDyna_SPrefabConstraintParams constraintParams)
                continue;
            var targetIndex = constraintParams.Ent2;
            if (targetIndex < 0 || targetIndex >= dynas.Count)
                continue;

            var target = dynas[targetIndex];
            var instanceParams = target.Params as NPlugDynaObjectModel_SInstanceParams
                                 ?? new NPlugDynaObjectModel_SInstanceParams { Version = 2 };
            instanceParams.IsKinematic = values.IsKinematic;
            instanceParams.CastStaticShadow = values.CastStaticShadow;
            instanceParams.PeriodSc = values.PeriodSc;
            instanceParams.PeriodScMax = values.PeriodScMax;
            instanceParams.Phase01 = values.Phase01;
            instanceParams.Phase01Max = values.Phase01Max;
            instanceParams.TextureId = values.TextureId;
            target.Params = instanceParams;
        }
    }

    private static IEnumerable<CPlugPrefab.EntRef> Flatten(CPlugPrefab prefab)
    {
        foreach (var ent in prefab.Ents)
        {
            yield return ent;
            if (ent.Model is CPlugPrefab nested)
                foreach (var inner in Flatten(nested))
                    yield return inner;
        }
    }
}
