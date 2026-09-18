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
