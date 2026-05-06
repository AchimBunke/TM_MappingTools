using GBX.NET.Engines.Game;
using GBX.NET.Engines.GameData;
using Microsoft.AspNetCore.Components.Forms;

namespace TM_MappingTools.Services;

/// <summary>
/// Manages loading and switching between different GBX file types (Clips, Maps, Items, etc).
/// </summary>
public class FileTypeManager
{
    private readonly ClipFileService _clipService;
    private readonly MapFileService _mapService;
    private readonly ItemFileService _itemService;

    private GbxFileService? _currentService;

    public event Action? FileTypeChanged;

    public FileTypeManager(
        ClipFileService clipService,
        MapFileService mapService,
        ItemFileService itemService)
    {
        _clipService = clipService;
        _mapService = mapService;
        _itemService = itemService;
    }

    /// <summary>
    /// Gets the currently loaded file service, or null if no file is loaded.
    /// </summary>
    public GbxFileService? CurrentService
    {
        get => _currentService;
        private set
        {
            if (_currentService != value)
            {
                _currentService = value;
                FileTypeChanged?.Invoke();
            }
        }
    }

    /// <summary>
    /// Gets the type name of the currently loaded content.
    /// </summary>
    public string? CurrentContentType => CurrentService?.ContentTypeName;

    /// <summary>
    /// Determines the appropriate service for a file and loads it.
    /// </summary>
    public async Task LoadFileAsync(IBrowserFile file)
    {
        var service = GetServiceForFile(file.Name);

        if (service == null)
        {
            throw new InvalidOperationException($"No service found for file type: {file.Name}");
        }

        CurrentService = service;
        await service.SetFileAsync(file);

        if (service.HasFile != true || service.LoadedContent is null)
        {
            throw new InvalidOperationException($"Failed to load file content: {file.Name}");
        }
    }

    /// <summary>
    /// Gets the service appropriate for a specific file extension.
    /// </summary>
    private GbxFileService? GetServiceForFile(string fileName)
    {
        return fileName switch
        {
            var n when n.EndsWith(".Clip.Gbx", StringComparison.OrdinalIgnoreCase) => _clipService,
            var n when n.EndsWith(".Map.Gbx", StringComparison.OrdinalIgnoreCase) => _mapService,
            var n when n.EndsWith(".Item.Gbx", StringComparison.OrdinalIgnoreCase) => _itemService,
            _ => null
        };
    }

    /// <summary>
    /// Clears the current loaded file and service.
    /// </summary>
    public void Clear()
    {
        _clipService.Clear();
        _mapService.Clear();
        _itemService.Clear();
        CurrentService = null;
    }

    /// <summary>
    /// Gets the currently loaded Clip, or null if a Clip is not loaded.
    /// </summary>
    public TM_GenericMapping.IO.Clip? GetClip() => _clipService.Clip;

    /// <summary>
    /// Gets the currently loaded Map, or null if a Map is not loaded.
    /// </summary>
    public CGameCtnChallenge? GetChallenge() => _mapService.Challenge;

    /// <summary>
    /// Gets the currently loaded Item, or null if an Item is not loaded.
    /// </summary>
    public CGameItemModel? GetItem() => _itemService.Item;

    /// <summary>
    /// Gets the download stream for the currently loaded file.
    /// </summary>
    public Stream? GetDownloadStream()
    {
        return CurrentService switch
        {
            ClipFileService clip => clip.GetDownloadStream(),
            MapFileService map => map.GetDownloadStream(),
            ItemFileService item => item.GetDownloadStream(),
            _ => null
        };
    }

    /// <summary>
    /// Gets the appropriate file extension filter for file loading.
    /// </summary>
    public static string GetFileFilter()
    {
        return ".Clip.Gbx,.Map.Gbx,.Item.Gbx";
    }
}
