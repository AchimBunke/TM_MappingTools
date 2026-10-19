using System.Numerics;
using GBX.NET;
using GBX.NET.Engines.Graphic;
using GBX.NET.Engines.Plug;
using TM_GenericMapping.Common;
using LightBinding = TM_MappingTools.Model.ParameterBinding<TM_MappingTools.Model.LightContext, TM_MappingTools.Model.LightSection>;

namespace TM_MappingTools.Model;

public enum LightSection
{
    Light,
    Transform,
    Plug,
    Common,
    TypeSpecific,
    Advanced,
}

public enum LightKind
{
    Point,
    Spot,
    Frustum,
    Directional,
    Ambient,
    /// <summary>Plain GxLightPoint (not a ball). Cannot be created, only displayed.</summary>
    PointBase,
    /// <summary>Unknown / unsupported GxLight subclass.</summary>
    Other,
}

public static class LightKinds
{
    public static LightKind GetKind(GxLight? light) => light switch
    {
        GxLightSpot => LightKind.Spot,
        GxLightFrustum => LightKind.Frustum,
        GxLightBall => LightKind.Point,
        GxLightPoint => LightKind.PointBase,
        GxLightDirectional => LightKind.Directional,
        GxLightAmbient => LightKind.Ambient,
        _ => LightKind.Other,
    };

    public static string GetLabel(LightKind kind) => kind switch
    {
        LightKind.Point => "Point",
        LightKind.Spot => "Spot",
        LightKind.Frustum => "Frustum",
        LightKind.Directional => "Directional",
        LightKind.Ambient => "Ambient",
        LightKind.PointBase => "Point (legacy)",
        _ => "Unknown",
    };

    /// <summary>Kinds ItemLightHelper can currently create. Add Frustum/Directional/Ambient once the library supports them.</summary>
    public static readonly LightKind[] Creatable = [LightKind.Point, LightKind.Spot, LightKind.Ambient,LightKind.Frustum, LightKind.Directional];
}

public sealed class LightContext
{
    public LightContext(CPlugSolid2Model.Light light) { Light = light; }
    public CPlugSolid2Model.Light Light { get; }
    public CPlugLight? Plug => Light.U03;
    public GxLight? Gx => Light.U03?.Light;
}

public static class LightParameterCatalog
{
    public static IReadOnlyList<LightBinding> All { get; }

    static LightParameterCatalog()
    {
        var list = new List<LightBinding>();
        BuildLight(list);
        BuildPlug(list);
        BuildCommon(list);
        BuildTypeSpecific(list);
        BuildAdvanced(list);
        All = list;
    }

    public static IEnumerable<LightBinding> Applicable(LightContext ctx, LightSection? section = null)
        => All.Where(b => (section == null || b.Section == section) && b.AppliesTo(ctx));

    public static IReadOnlyList<ToolParameterDefinition> Definitions(LightContext ctx, LightSection section)
        => Applicable(ctx, section).Select(b => b.Definition).ToList();

    /// <summary>Creates a fresh values object containing the current model values of every applicable parameter.</summary>
    public static ToolParameterValues Load(LightContext ctx)
    {
        var values = new ToolParameterValues();
        foreach (var b in Applicable(ctx))
            values.Set(b.Key, b.Read(ctx));
        return values;
    }

    /// <summary>Writes all changed values into the model. Returns the number of changed parameters.</summary>
    public static int Apply(LightContext ctx, ToolParameterValues values)
    {
        int changed = 0;
        foreach (var b in Applicable(ctx))
            if (b.Apply(ctx, values)) changed++;
        return changed;
    }

    // ------------------------------------------------------------------ Light wrapper

    private static void BuildLight(List<LightBinding> list)
    {
        var w = new Binder<CPlugSolid2Model.Light>(list, LightSection.Light, c => c.Light, "light.");
        w.Text("name", "Name", "Light identifier.", n => n.U01 ?? "", (n, v) => n.U01 = v);
        w.Int("u06", "Unknown U06", "Unknown. Converter default: 1.", n => n.U06, (n, v) => n.U06 = v);
        w.Int("u07", "Unknown U07", "Unknown. Converter default: 1.", n => n.U07, (n, v) => n.U07 = v);
        w.Int("u08", "Unknown U08", "Unknown.", n => n.U08, (n, v) => n.U08 = v);
        w.Int("u09", "Unknown U09", "Part of some string.", n => n.U09, (n, v) => n.U09 = v);
        w.Int("u10", "Unknown U10", "Part of some string.", n => n.U10, (n, v) => n.U10 = v);
        w.Int("u11", "Unknown U11", "Part of some string.", n => n.U11, (n, v) => n.U11 = v);
        w.Int("u12", "Unknown U12", "Unknown (file version 26+).", n => n.U12, (n, v) => n.U12 = v);
        w.Int("u13", "Unknown U13", "Unknown.", n => n.U13, (n, v) => n.U13 = v);
        w.Int("u14", "Unknown U14", "Unknown.", n => n.U14, (n, v) => n.U14 = v);
        w.Bool("u15", "Unknown U15", "Unknown flag that enables U16-U18.", n => n.U15, (n, v) => n.U15 = v);
        w.Float("u16", "Unknown U16", "Unknown.", n => n.U16, (n, v) => n.U16 = v);
        w.Float("u17", "Unknown U17", "Unknown.", n => n.U17, (n, v) => n.U17 = v);
        w.Float("u18", "Unknown U18", "Unknown.", n => n.U18, (n, v) => n.U18 = v);
        // Light-section bindings are all "advanced" by nature except the name; the page decides how to lay them out by key.

        var t = new Binder<CPlugSolid2Model.Light>(list, LightSection.Transform, c => c.Light, "transform.");
        t.Vector3("position", "Position", "Position of the light relative to the mesh.",
            n => n.U05.GetPosition(),
            (n, v) => n.U05 = Iso4.IsoFromTransform(v, n.U05.GetRotationQuaternion()));
        t.Quaternion("rotation", "Rotation", "Rotation of the light (quaternion). Spot and directional lights point along their local axis.",
            n => n.U05.GetRotationQuaternion(),
            (n, v) => n.U05 = Iso4.IsoFromTransform(n.U05.GetPosition(), v));
    }

    // ------------------------------------------------------------------ CPlugLight

    private static void BuildPlug(List<LightBinding> list)
    {
        var p = new Binder<CPlugLight>(list, LightSection.Plug, c => c.Plug, "plug.");
        p.Bool("nightOnly", "Night Only", "Light is only active at night.", n => n.NightOnly, (n, v) => n.NightOnly = v);
        p.Bool("reflectByGround", "Reflect By Ground", "Light is reflected by the ground.", n => n.ReflectByGround, (n, v) => n.ReflectByGround = v);
        p.Bool("duplicateGxLight", "Duplicate GxLight", "Duplicate the underlying GxLight.", n => n.DuplicateGxLight, (n, v) => n.DuplicateGxLight = v);
        p.Bool("sceneLightOnlyWhenTreeVisible", "Scene Light Only When Tree Visible", "Scene light is only active while the item tree is visible.", n => n.SceneLightOnlyWhenTreeVisible, (n, v) => n.SceneLightOnlyWhenTreeVisible = v);
        p.Float("animPeriodMin", "Anim Period Min", "Minimum animation period.", n => n.AnimPeriodMin, (n, v) => n.AnimPeriodMin = v);
        p.Float("animPeriodMax", "Anim Period Max", "Maximum animation period.", n => n.AnimPeriodMax, (n, v) => n.AnimPeriodMax = v);
        p.Text("animTimerName", "Anim Timer Name", "Name of the animation timer.", n => n.AnimTimerName ?? "", (n, v) => n.AnimTimerName = v);
    }

    // ------------------------------------------------------------------ GxLight (common)

    private static void BuildCommon(List<LightBinding> list)
    {
        var g = new Binder<GxLight>(list, LightSection.Common, c => c.Gx, "gx.");
        g.Color("color", "Color", "Light color (can exceed 1 for HDR).", n => n.Color, (n, v) => n.Color = v);
        g.Float("intensity", "Intensity", "Light intensity. Negative values invert the light.", n => n.Intensity, (n, v) => n.Intensity = v);
        g.Float("diffuseIntensity", "Diffuse Intensity", "Diffuse intensity multiplier.", n => n.DiffuseIntensity, (n, v) => n.DiffuseIntensity = v);
        g.Float("specularIntens", "Specular Intensity", "Specular intensity multiplier.", n => n.SpecularIntens, (n, v) => n.SpecularIntens = v);
        g.Float("specularPower", "Specular Power", "Specular power.", n => n.SpecularPower, (n, v) => n.SpecularPower = v);
        g.Color("shadowRGB", "Shadow Color", "Shadow color.", n => n.ShadowRGB, (n, v) => n.ShadowRGB = v);
        g.Float("shadowIntensity", "Shadow Intensity", "Shadow intensity.", n => n.ShadowIntensity, (n, v) => n.ShadowIntensity = v);
        g.Float("flareIntensity", "Flare Intensity", "Lens flare intensity.", n => n.FlareIntensity, (n, v) => n.FlareIntensity = v);
        g.Bool("doLighting", "Do Lighting", "Light affects surfaces.", n => n.DoLighting, (n, v) => n.DoLighting = v);
        g.Bool("lightMapOnly", "Light Map Only", "Light is only used for light map baking.", n => n.LightMapOnly, (n, v) => n.LightMapOnly = v);
        g.Bool("isShadowGen", "Shadow Gen", "Light generates shadows.", n => n.IsShadowGen, (n, v) => n.IsShadowGen = v);
        g.Bool("doSpecular", "Specular", "Light produces specular highlights.", n => n.DoSpecular, (n, v) => n.DoSpecular = v);
        g.Bool("hasLensFlare", "Lens Flare", "Light has a lens flare.", n => n.HasLensFlare, (n, v) => n.HasLensFlare = v);
        g.Bool("hasSprite", "Sprite", "Light has a sprite.", n => n.HasSprite, (n, v) => n.HasSprite = v);
        g.Group("EnableGroups", "Enable Groups");
        g.Bool("enableGroup0", "Group 0", "Enable light group 0.", n => n.EnableGroup0, (n, v) => n.EnableGroup0 = v);
        g.Bool("enableGroup1", "Group 1", "Enable light group 1.", n => n.EnableGroup1, (n, v) => n.EnableGroup1 = v);
        g.Bool("enableGroup2", "Group 2", "Enable light group 2.", n => n.EnableGroup2, (n, v) => n.EnableGroup2 = v);
        g.Bool("enableGroup3", "Group 3", "Enable light group 3.", n => n.EnableGroup3, (n, v) => n.EnableGroup3 = v);
    }

    // ------------------------------------------------------------------ Type specific

    private static void BuildTypeSpecific(List<LightBinding> list)
    {
        var point = new Binder<GxLightPoint>(list, LightSection.TypeSpecific, c => c.Gx as GxLightPoint, "point.");
        point.Float("flareSize", "Flare Size", "Size of the lens flare.", n => n.FlareSize, (n, v) => n.FlareSize = v);
        point.Float("flareBiasZ", "Flare Bias Z", "Z bias of the lens flare.", n => n.FlareBiasZ, (n, v) => n.FlareBiasZ = v);

        var ball = new Binder<GxLightBall>(list, LightSection.TypeSpecific, c => c.Gx as GxLightBall, "ball.");
        ball.Float("radius", "Radius", "Light radius.", n => n.Radius, (n, v) => n.Radius = v);
        ball.Float("radiusSpecular", "Radius Specular", "Radius of specular highlights.", n => n.RadiusSpecular, (n, v) => n.RadiusSpecular = v);
        ball.Float("radiusShadow", "Radius Shadow", "Radius of shadows.", n => n.RadiusShadow, (n, v) => n.RadiusShadow = v);
        ball.Float("radiusFlare", "Radius Flare", "Radius of the lens flare.", n => n.RadiusFlare, (n, v) => n.RadiusFlare = v);
        ball.Float("emittingRadius", "Emitting Radius", "Radius of the emitting surface.", n => n.EmittingRadius, (n, v) => n.EmittingRadius = v);
        ball.Float("emittingCylinderLenZ", "Emitting Cylinder Length Z", "Length of the emitting cylinder.", n => n.EmittingCylinderLenZ, (n, v) => n.EmittingCylinderLenZ = v);
        ball.Float("attenuation1", "Attenuation 1", "Linear attenuation.", n => n.Attenuation1, (n, v) => n.Attenuation1 = v);
        ball.Float("attenuation2", "Attenuation 2", "Quadratic attenuation.", n => n.Attenuation2, (n, v) => n.Attenuation2 = v);
        ball.Float("attHTnLR", "Att HTnLR", "Attenuation parameter.", n => n.AttHTnLR, (n, v) => n.AttHTnLR = v);
        ball.Float("attHTnLR2", "Att HTnLR2", "Attenuation parameter.", n => n.AttHTnLR2, (n, v) => n.AttHTnLR2 = v);
        ball.Float("attHyper2DerivAt0", "Att Hyper2 Deriv At 0", "Attenuation hyperbola derivative at 0.", n => n.AttHyper2DerivAt0, (n, v) => n.AttHyper2DerivAt0 = v);
        ball.Float("attHyper2Tension", "Att Hyper2 Tension", "Attenuation hyperbola tension.", n => n.AttHyper2Tension, (n, v) => n.AttHyper2Tension = v);
        ball.Color("ambientRGB", "Ambient Color", "Ambient color.", n => n.AmbientRGB, (n, v) => n.AmbientRGB = v);
        ball.Int("flags", "Ball Flags", "Raw ball flags.", n => unchecked((int)n.Flags), (n, v) => n.Flags = unchecked((uint)v));

        var spot = new Binder<GxLightSpot>(list, LightSection.TypeSpecific, c => c.Gx as GxLightSpot, "spot.");
        spot.Float("angleInner", "Angle Inner", "Inner cone angle (degrees).", n => n.AngleInner, (n, v) => n.AngleInner = v);
        spot.Float("angleOuter", "Angle Outer", "Outer cone angle (degrees).", n => n.AngleOuter, (n, v) => n.AngleOuter = v);
        spot.Float("angleFlare", "Angle Flare", "Flare cone angle (degrees).", n => n.AngleFlare, (n, v) => n.AngleFlare = v);
        spot.Float("angleInnerShadow", "Angle Inner Shadow", "Inner shadow cone angle.", n => n.AngleInnerShadow, (n, v) => n.AngleInnerShadow = v);
        spot.Float("angleOuterShadow", "Angle Outer Shadow", "Outer shadow cone angle.", n => n.AngleOuterShadow, (n, v) => n.AngleOuterShadow = v);
        spot.Float("falloffExponent", "Falloff Exponent", "Falloff exponent between inner and outer angle.", n => n.FalloffExponent, (n, v) => n.FalloffExponent = v);
        spot.Int("flags", "Spot Flags", "Raw spot flags.", n => unchecked((int)n.Flags), (n, v) => n.Flags = unchecked((uint)v));

        var dir = new Binder<GxLightDirectional>(list, LightSection.TypeSpecific, c => c.Gx as GxLightDirectional, "dir.");
        dir.Bool("useBoundaryHint", "Use Boundary Hint", "Use the boundary hint position.", n => n.UseBoundaryHint, (n, v) => n.UseBoundaryHint = v);
        dir.Vector3("boundaryHintPos", "Boundary Hint Position", "Boundary hint position.", n => n.BoundaryHintPos, (n, v) => n.BoundaryHintPos = v);
        dir.Float("dazzleAngleMax", "Dazzle Angle Max", "Maximum dazzle angle.", n => n.DazzleAngleMax, (n, v) => n.DazzleAngleMax = v);
        dir.Float("dazzleIntensity", "Dazzle Intensity", "Dazzle intensity.", n => n.DazzleIntensity, (n, v) => n.DazzleIntensity = v);
        dir.Color("dblSidedRGB", "Double Sided Color", "Color for the back side.", n => n.DblSidedRGB, (n, v) => n.DblSidedRGB = v);
        dir.Color("reverseRGB", "Reverse Color", "Reverse light color.", n => n.ReverseRGB, (n, v) => n.ReverseRGB = v);
        dir.Float("reverseIntens", "Reverse Intensity", "Reverse light intensity.", n => n.ReverseIntens, (n, v) => n.ReverseIntens = v);
        dir.Float("emittAngularSize", "Emitting Angular Size", "Angular size of the emitter.", n => n.EmittAngularSize, (n, v) => n.EmittAngularSize = v);
        dir.Float("flareAngularSize", "Flare Angular Size", "Angular size of the flare.", n => n.FlareAngularSize, (n, v) => n.FlareAngularSize = v);

        var amb = new Binder<GxLightAmbient>(list, LightSection.TypeSpecific, c => c.Gx as GxLightAmbient, "amb.");
        amb.Float("shadeMinY", "Shade Min Y", "Minimum Y shade.", n => n.ShadeMinY, (n, v) => n.ShadeMinY = v);
        amb.Float("shadeMaxY", "Shade Max Y", "Maximum Y shade.", n => n.ShadeMaxY, (n, v) => n.ShadeMaxY = v);
    }

    // ------------------------------------------------------------------ Chunk-only (unnamed) fields

    private static void BuildAdvanced(List<LightBinding> list)
    {
        var plug4 = new Binder<CPlugLight.Chunk0901D004>(list, LightSection.Advanced, c => c.Plug?.GetChunk<CPlugLight.Chunk0901D004>(), "plugC4.");
        plug4.Int("u01", "CPlugLight U01", "Unknown chunk 0x0901D004 value.", n => n.U01, (n, v) => n.U01 = v);
        plug4.Int("u02", "CPlugLight U02", "Unknown chunk 0x0901D004 value.", n => n.U02, (n, v) => n.U02 = v);
        plug4.Int("u03", "CPlugLight U03", "Unknown chunk 0x0901D004 value.", n => n.U03, (n, v) => n.U03 = v);
        plug4.Int("u04", "CPlugLight U04", "Unknown chunk 0x0901D004 value.", n => n.U04, (n, v) => n.U04 = v);
        plug4.Int("u05", "CPlugLight U05", "Unknown chunk 0x0901D004 value.", n => n.U05, (n, v) => n.U05 = v);

        var ball9 = new Binder<GxLightBall.Chunk04002009>(list, LightSection.Advanced, c => (c.Gx as GxLightBall)?.GetChunk<GxLightBall.Chunk04002009>(), "ballC9.");
        ball9.Float("u01", "Ball Chunk 0x09 U01", "Unknown. Seems to be equal to the light distance.", n => n.U01, (n, v) => n.U01 = v);

        var ballA = new Binder<GxLightBall.Chunk0400200A>(list, LightSection.Advanced, c => (c.Gx as GxLightBall)?.GetChunk<GxLightBall.Chunk0400200A>(), "ballCA.");
        ballA.Float("u01", "Ball Chunk 0x0A U01", "Unknown. Default is 0.015625 (1/64).", n => n.U01, (n, v) => n.U01 = v);

        var spotB3 = new Binder<GxLightSpot.Chunk0400B003>(list, LightSection.Advanced, c => (c.Gx as GxLightSpot)?.GetChunk<GxLightSpot.Chunk0400B003>(), "spotCB3.");
        spotB3.Int("u01", "Spot Chunk U01", "Unknown byte (sub-light count X?).", n => n.U01, (n, v) => n.U01 = (byte)Math.Clamp(v, 0, 255));
        spotB3.Int("u02", "Spot Chunk U02", "Unknown byte (sub-light count Y?).", n => n.U02, (n, v) => n.U02 = (byte)Math.Clamp(v, 0, 255));
        spotB3.Int("u03", "Spot Chunk U03", "Unknown int.", n => n.U03, (n, v) => n.U03 = v);

        var fr4 = new Binder<GxLightFrustum.Chunk0400A004>(list, LightSection.Advanced, c => (c.Gx as GxLightFrustum)?.GetChunk<GxLightFrustum.Chunk0400A004>(), "frC4.");
        fr4.Int("u01", "Frustum U01", "Unknown int.", n => n.U01, (n, v) => n.U01 = v);
        fr4.Int("u02", "Frustum U02", "Unknown int.", n => n.U02, (n, v) => n.U02 = v);
        fr4.Int("u03", "Frustum U03", "Unknown int.", n => n.U03, (n, v) => n.U03 = v);
        fr4.Float("u04", "Frustum U04", "Unknown float.", n => n.U04, (n, v) => n.U04 = v);
        fr4.Float("u05", "Frustum U05", "Unknown float.", n => n.U05, (n, v) => n.U05 = v);
        fr4.Float("u06", "Frustum U06", "Unknown float.", n => n.U06, (n, v) => n.U06 = v);
        fr4.Float("u07", "Frustum U07", "Unknown float.", n => n.U07, (n, v) => n.U07 = v);
        fr4.Int("u08", "Frustum U08", "Unknown int.", n => n.U08, (n, v) => n.U08 = v);

        var fr6 = new Binder<GxLightFrustum.Chunk0400A006>(list, LightSection.Advanced, c => (c.Gx as GxLightFrustum)?.GetChunk<GxLightFrustum.Chunk0400A006>(), "frC6.");
        fr6.Bool("u01", "Frustum Box Enabled", "Unknown flag (box aligned data follows). The box itself is preserved but not editable.", n => n.U01, (n, v) => n.U01 = v);
        fr6.Int("u03", "Frustum U03 (flags)", "Unknown uint.", n => unchecked((int)n.U03), (n, v) => n.U03 = unchecked((uint)v));
    }

    // ------------------------------------------------------------------ Helper

    /// <summary>Generic binder plus GBX.NET <see cref="Vec3"/> overloads.</summary>
    private sealed class Binder<T> : ParameterBinder<LightContext, LightSection, T> where T : class
    {
        public Binder(List<LightBinding> output, LightSection section, Func<LightContext, T?> target, string prefix)
            : base(output, section, target, prefix) { }

        public void Vector3(string key, string label, string tip, Func<T, Vec3> get, Action<T, Vec3> set)
            => Vector3(key, label, tip, n => get(n).ToVector3(), (n, v) => set(n, v.ToVec3()));

        /// <summary>Color from a Vec3 with each component in 0-1. Unchanged colors keep their exact (possibly HDR) value.</summary>
        public void Color(string key, string label, string tip, Func<T, Vec3> get, Action<T, Vec3> set)
            => Color(key, label, tip, get, set,
                v => System.Drawing.Color.FromArgb(ToByte(v.X), ToByte(v.Y), ToByte(v.Z)),
                (_, c) => new Vec3(c.R / 255f, c.G / 255f, c.B / 255f));

        private static int ToByte(float f) => (int)Math.Round(Math.Clamp(f, 0f, 1f) * 255f);
    }
}
