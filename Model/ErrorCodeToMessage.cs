using System;
using TM_GenericMapping.Messaging;

namespace TM_MappingTools.Model;

public static class ErrorCodeToMessage
{
    public static Dictionary<string, string> ErrorCodeMap { get; } = new Dictionary<string, string>
    {
        [ErrorCodes.MeshExtractor.MissingMesh] = "Item contains no mesh to extract.",
        [ErrorCodes.MeshExtractor.UnsupportedMesh] = "Mesh to be extracted is not supported. Confirm that mesh is either: CPlugCrystal, CPlug2Solid, MovingItem.",
        [ErrorCodes.MovingItemCreator.MeshExtractionFailed] = "Failed to extract mesh from item.",
        [ErrorCodes.EmbeddedItemExtractor.MissingEmbeddedData] = "No embedded item data to extract.",
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
