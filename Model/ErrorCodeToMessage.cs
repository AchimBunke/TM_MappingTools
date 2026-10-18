using System;
using TM_GenericMapping.Messaging;

namespace TM_MappingTools.Model;

public static class ErrorCodeToMessage
{
    public static Dictionary<string, string> ErrorCodeMap { get; } = new Dictionary<string, string>
    {
        [ErrorCodes.MissingErrorCode] = "Unknown Error occured.",

        [ErrorCodes.MeshExtractor.MissingMesh] = "Item contains no mesh to extract.",
        [ErrorCodes.MeshExtractor.UnsupportedMesh] = "Mesh to be extracted is not supported. Confirm that mesh is either: CPlugCrystal, CPlug2Solid, MovingItem.",

        [ErrorCodes.MovingItemCreator.MeshExtractionFailed] = "Failed to extract mesh from item.",
        [ErrorCodes.MovingItemCreator.MeshBuildingFailed] = "Building item failed.",

        [ErrorCodes.EmbeddedItemExtractor.MissingEmbeddedData] = "No embedded item data to extract.",

        [ErrorCodes.ItemEffectVariantCreator.MissingTriggerSpecial] = "Item doesn't have a valid trigger shape.",

        [ErrorCodes.MeshBuilder.MissingDynaShape] = "Item required a collidable dynamic surface but has none.",
        [ErrorCodes.MeshBuilder.MissingStaticShape] = "Item required a collidable static surface but has none.",
        [ErrorCodes.MeshBuilder.MissingTrigger] = "Item required a trigger shape but has none.",
        [ErrorCodes.MeshBuilder.UnsupportedType] = "Requested item type is not supported.",

        [ErrorCodes.TriangleProjector.InvalidCameraType] = "Camera type is not supported for triangle projection.",
        [ErrorCodes.TriangleProjector.MissingTriangleBlock] = "There is no triangle track to project.",

        [ErrorCodes.VariantItemBuilder.MissingVariantInputs] = "No variants are provided to create the item.",

        [ErrorCodes.NormalizedItemValidator.ValidationError] = "Item configuration is invalid.",

        [ErrorCodes.ItemParser.UnsupportedMesh] = "Item contains a mesh type that is not supported.",
        [ErrorCodes.ItemParser.MissingMesh] = "Item contains no mesh to extract.",
        [ErrorCodes.ItemParser.MissingDynamicConstraintTarget] = "A dynamic part references a constraint target that does not exist.",
        [ErrorCodes.ItemParser.MissingDynamicConstraintParent] = "A dynamic part references a constraint parent that does not exist.",
        [ErrorCodes.ItemParser.UnsupportedSurfaceType] = "Item contains a surface type that is not supported.",
        [ErrorCodes.ItemParser.MissingTriggerShape] = "Item has a trigger without a valid trigger shape.",
        [ErrorCodes.ItemParser.UnsupportedVariantType] = "Item contains a variant type that is not supported.",

        [ErrorCodes.ItemCompiler.MissingDynaCollisionShape] = "A movable part requires a collidable surface but has none.",
        [ErrorCodes.ItemCompiler.UnsupportedSurfaceType] = "A surface type of the item is not supported by the selected target.",

        [ErrorCodes.MeshExtractor.MissingTriggerShape] = "Item has a trigger without a valid trigger shape.",
        [ErrorCodes.MeshExtractor.MissingDynaModel] = "Item has a movable part without a model.",
        [ErrorCodes.MeshExtractor.UnsupportedPrefabEntity] = "Item contains a prefab entity that is not supported.",

    };
    public static string GetMessage(string errorCode)
    {
        if (ErrorCodeMap.TryGetValue(errorCode, out var message))
        {
            return message;
        }
        return "Unknown tool error.";
    }
}
