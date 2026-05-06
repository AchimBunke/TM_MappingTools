using Microsoft.AspNetCore.Components.Forms;

namespace TM_MappingTools.Services;

/// <summary>
/// Abstract base class for GBX file services. Provides common functionality for loading different GBX file types.
/// </summary>
public abstract class GbxFileService : FileService
{
    /// <summary>
    /// Gets the loaded content object (Clip, Map, Item, etc).
    /// </summary>
    public abstract object? LoadedContent { get; }

    /// <summary>
    /// Gets the supported file extension for this service.
    /// </summary>
    public abstract string SupportedExtension { get; }

    /// <summary>
    /// Gets a user-friendly name for the content type.
    /// </summary>
    public abstract string ContentTypeName { get; }

    /// <summary>
    /// Event raised when content has been successfully loaded.
    /// </summary>
    public event Action? ContentLoaded;

    protected void InvokeContentLoaded() => ContentLoaded?.Invoke();
}
