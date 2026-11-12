using System.Globalization;
using System.Numerics;
using GBX.NET.Engines.GameData;
using GBX.NET.Engines.Meta;
using GBX.NET.Engines.MetaNotPersistent;
using GBX.NET.Engines.Plug;
using TM_GenericMapping.Items;
using TM_GenericMapping.Messaging;
using static GBX.NET.Engines.Plug.CPlugPrefab;

namespace TM_MappingTools.Model;

public sealed class AttachmentTagModel
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}

/// <summary>A set of required tags. An item can be attached when it matches one of the options of a definition.</summary>
public sealed class AttachmentOptionModel
{
    public List<AttachmentTagModel> Tags { get; set; } = [];
}

public sealed class AttachmentDefinitionModel
{
    public int ILayout { get; set; }
    public List<AttachmentOptionModel> Options { get; set; } = [];
}

public sealed class AttachmentLocationModel
{
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; } = Quaternion.Identity;
    public AttachmentDefinitionModel Definition { get; set; } = new();
}

public sealed class AttachmentEntryModel
{
    /// <summary>Group entries hold any number of locations that share definitions. Single entries hold exactly one location.</summary>
    public bool IsGroup { get; set; }
    public List<AttachmentDefinitionModel> Definitions { get; set; } = [];
    public List<AttachmentLocationModel> Locations { get; set; } = [];
}

public sealed class PodiumSlotModel
{
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; } = Quaternion.Identity;
    public int PlayerPosition { get; set; } = 1;
    public bool Car { get; set; }
}

public static class ItemAttachmentAccess
{
    public const string PodiumType = "Podium";
    public const string CarSlotKey = "Slot";
    public const string PilotSlotKey = "PilotSlot";

    public static CPlugPrefab? FindPrefab(CGameItemModel item)
    {
        if (item.EntityModel is CPlugPrefab prefab)
            return prefab;
        if (item.EntityModel is NPlugItem_SVariantList variantList)
        {
            foreach (var variant in variantList.Variants ?? [])
            {
                if (variant.EntityModel is CPlugPrefab vp)
                    return vp;
            }
        }
        return null;
    }

    public static AttachmentTagModel NewTag(string key = "", string value = "") => new() { Key = key, Value = value };

    public static AttachmentDefinitionModel NewDefinition()
        => new() { Options = [new AttachmentOptionModel { Tags = [NewTag()] }] };

    public static AttachmentEntryModel NewEntry(bool isGroup)
    {
        var definition = NewDefinition();
        return new AttachmentEntryModel
        {
            IsGroup = isGroup,
            Definitions = [definition],
            Locations = [new AttachmentLocationModel { Definition = definition }],
        };
    }

    // ---------- Reading ----------

    public static List<AttachmentEntryModel> Read(CGameItemModel item)
    {
        var result = new List<AttachmentEntryModel>();
        var prefab = FindPrefab(item);
        if (prefab is null)
            return result;

        foreach (var ent in prefab.Ents ?? [])
        {
            var entry = ParseEnt(ent);
            if (entry is not null)
                result.Add(entry);
        }
        return result;
    }

    static bool IsPlacementEnt(EntRef ent)
        => ent.Params is NPlugItemPlacement_SPlacement or NPlugItemPlacement_SPlacementGroup;

    static AttachmentDefinitionModel ReadDefinition(NPlugItemPlacement_SPlacement placement)
    {
        var def = ItemPlacementUtils.ReadSPlacement(placement, Vector3.Zero, Quaternion.Identity).Definition;
        return new AttachmentDefinitionModel
        {
            ILayout = def.ILayout,
            Options = def.Options
                .Select(o => new AttachmentOptionModel { Tags = o.Select(kvp => NewTag(kvp.Key, kvp.Value)).ToList() })
                .ToList(),
        };
    }

    static AttachmentEntryModel? ParseEnt(EntRef ent)
    {
        switch (ent.Params)
        {
            case NPlugItemPlacement_SPlacement placement:
            {
                var def = ReadDefinition(placement);
                return new AttachmentEntryModel
                {
                    IsGroup = false,
                    Definitions = [def],
                    Locations =
                    [
                        new AttachmentLocationModel
                        {
                            Position = (Vector3)ent.Position,
                            Rotation = ent.Rotation.AsQuaternion(),
                            Definition = def,
                        }
                    ],
                };
            }
            case NPlugItemPlacement_SPlacementGroup group:
            {
                var placements = group.Placements ?? [];
                var transforms = group.U02 ?? [];
                var indices = group.U01 ?? [];
                var defs = placements.Select(ReadDefinition).ToList();
                var entry = new AttachmentEntryModel { IsGroup = true, Definitions = defs };
                for (int i = 0; i < transforms.Length; i++)
                {
                    var index = i < indices.Length ? indices[i] : -1;
                    if (index < 0 || index >= defs.Count)
                        throw new InvalidOperationException($"Invalid placement index {index} in attachment group with {defs.Count} definitions.");
                    transforms[i].Deconstruct(out var position, out var rotation);
                    entry.Locations.Add(new AttachmentLocationModel { Position = position, Rotation = rotation, Definition = defs[index] });
                }
                return entry;
            }
            default:
                return null;
        }
    }

    // ---------- Writing ----------

    static ItemAttachmentDefinition ToLibraryDefinition(AttachmentDefinitionModel def)
        => new(
            def.Options.Select(o =>
            {
                var dict = new Dictionary<string, string>();
                foreach (var tag in o.Tags)
                {
                    if (!string.IsNullOrWhiteSpace(tag.Key))
                        dict[tag.Key.Trim()] = tag.Value ?? "";
                }
                return dict;
            }).ToList(),
            def.ILayout);

    static EntRef CreateEnt(AttachmentEntryModel entry)
    {
        var ent = GbxItemUtils.CreateEntRef();
        var libDefs = new Dictionary<AttachmentDefinitionModel, ItemAttachmentDefinition>(ReferenceEqualityComparer.Instance);
        ItemAttachmentDefinition GetDef(AttachmentDefinitionModel d)
        {
            if (!libDefs.TryGetValue(d, out var lib))
                libDefs[d] = lib = ToLibraryDefinition(d);
            return lib;
        }

        var locations = entry.Locations
            .Select(l => new ItemAttachmentLocation(l.Position, l.Rotation, GetDef(l.Definition)))
            .ToArray();

        if (!entry.IsGroup && locations.Length == 1)
        {
            ent.Params = ItemPlacementUtils.CreateSPlacement(locations[0], out var pos, out var rot);
            ent.Position = pos;
            ent.Rotation = rot;
        }
        else
        {
            ent.Params = ItemPlacementUtils.CreateSPlacementGroup(locations);
        }
        return ent;
    }

    /// <summary>Validates the entries. Returns a list of problems that prevent writing.</summary>
    public static List<string> ValidateEntries(IReadOnlyList<AttachmentEntryModel> entries)
    {
        var errors = new List<string>();
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry.Locations.Count == 0)
                errors.Add($"Entry #{i + 1} has no locations.");
            foreach (var loc in entry.Locations)
            {
                if (!entry.Definitions.Contains(loc.Definition))
                {
                    errors.Add($"Entry #{i + 1} has a location with an unknown definition.");
                    break;
                }
            }
        }
        return errors;
    }

    /// <summary>Replaces all attachment entries of the item with the given entries.</summary>
    public static ToolResult<CGameItemModel> Write(CGameItemModel item, IEnumerable<AttachmentEntryModel> entries)
    {
        var prefab = FindPrefab(item);
        if (prefab is null)
            return ToolResult.Fail(nameof(ItemAttachmentAccess), ErrorCodes.ItemAttachmentCreator.UnsupportedEntityModel);

        var kept = (prefab.Ents ?? []).Where(e => !IsPlacementEnt(e));
        var created = entries.Select(CreateEnt);
        prefab.Ents = [.. kept, .. created];
        return ToolResult.Success(item, nameof(ItemAttachmentAccess));
    }

    // ---------- Podium ----------

    public static bool TryGetPodiumSlot(AttachmentLocationModel location, out PodiumSlotModel slot)
    {
        slot = new PodiumSlotModel { Position = location.Position, Rotation = location.Rotation };
        foreach (var option in location.Definition.Options)
        {
            if (!option.Tags.Any(t => t.Key == "Type" && t.Value == PodiumType))
                continue;
            foreach (var tag in option.Tags)
            {
                if ((tag.Key == CarSlotKey || tag.Key == PilotSlotKey)
                    && int.TryParse(tag.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
                {
                    slot.Car = tag.Key == CarSlotKey;
                    slot.PlayerPosition = number;
                    return true;
                }
            }
        }
        return false;
    }

    static bool IsPodiumEntry(AttachmentEntryModel entry)
        => entry.Locations.Count > 0 && entry.Locations.All(l => TryGetPodiumSlot(l, out _));

    public static List<PodiumSlotModel> ReadPodium(CGameItemModel item)
    {
        var slots = new List<PodiumSlotModel>();
        foreach (var entry in Read(item))
        {
            if (!IsPodiumEntry(entry))
                continue;
            foreach (var location in entry.Locations)
            {
                if (TryGetPodiumSlot(location, out var slot))
                    slots.Add(slot);
            }
        }
        return slots;
    }

    /// <summary>Warnings for missing player positions 1-3 (for pilots and cars) and duplicated slots.</summary>
    public static List<string> ValidatePodium(IEnumerable<PodiumSlotModel> slots)
    {
        var list = slots.ToList();
        var warnings = new List<string>();
        foreach (var (car, label) in new[] { (false, "player"), (true, "car") })
        {
            var missing = Enumerable.Range(1, 3)
                .Where(p => !list.Any(s => s.Car == car && s.PlayerPosition == p))
                .ToList();
            if (missing.Count > 0)
                warnings.Add($"Missing {label} position{(missing.Count > 1 ? "s" : "")} {string.Join(", ", missing)}. A podium should define {label} positions 1, 2 and 3.");
        }
        foreach (var group in list.GroupBy(s => (s.Car, s.PlayerPosition)).Where(g => g.Count() > 1))
            warnings.Add($"{(group.Key.Car ? "Car" : "Player")} position {group.Key.PlayerPosition} is defined {group.Count()} times.");
        if (list.Any(s => s.PlayerPosition < 1))
            warnings.Add("Positions below 1 are not valid.");
        return warnings;
    }

    /// <summary>Replaces all podium definitions of the item.</summary>
    public static ToolResult<CGameItemModel> WritePodium(CGameItemModel item, IReadOnlyList<PodiumSlotModel> slots)
    {
        var prefab = FindPrefab(item);
        if (prefab is null)
            return ToolResult.Fail(nameof(ItemAttachmentAccess), ErrorCodes.ItemAttachmentCreator.UnsupportedEntityModel);

        var kept = new List<EntRef>();
        foreach (var ent in prefab.Ents ?? [])
        {
            var entry = IsPlacementEnt(ent) ? ParseEnt(ent) : null;
            if (entry is not null && IsPodiumEntry(entry))
                continue;
            kept.Add(ent);
        }
        prefab.Ents = [.. kept];

        if (slots.Count == 0)
        {
            item.PodiumClipList = null;
            return ToolResult.Success(item, nameof(ItemAttachmentAccess));
        }

        var podiumSlots = slots.Select(s => new ItemPodiumCreator.PodiumSlot
        {
            Position = s.Position,
            Rotation = s.Rotation,
            PlayerPosition = s.PlayerPosition,
            Car = s.Car,
        });
        return new ItemPodiumCreator().ConvertToPodium(item, podiumSlots);
    }
}
