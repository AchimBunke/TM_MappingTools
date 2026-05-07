using GBX.NET;
using GBX.NET.Engines.Game;
using Microsoft.AspNetCore.Components.Forms;
using TM_MappingTools.Utils;

namespace TM_MappingTools.Services;

public class MapFileService : GbxFileService
{
    public CGameCtnChallenge? Challenge { get; private set; }

    public override object? LoadedContent => Challenge;
    public override string SupportedExtension => ".Map.Gbx";
    public override string ContentTypeName => "Map";

    public override void Clear()
    {
        Challenge = null;
        base.Clear();
    }

    public override async Task SetFileAsync(IBrowserFile file)
    { 
        try
        {
            using var browserStream = file.OpenReadStream(FileHelper.MaxAllowedFileSize);
            var memoryStream = new MemoryStream();
            await browserStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            Challenge = await Gbx.ParseAsync<CGameCtnChallenge>(memoryStream);
            if (Challenge is null)
            {
                throw new InvalidDataException("Parsed map content was null.");
            }

            await base.SetFileAsync(file);
            InvokeContentLoaded();
        }
        catch (Exception ex)
        {
            Clear();
            throw new InvalidDataException($"Failed to parse map file '{file.Name}': {ex.Message}", ex);
        }
    }

    public Stream GetDownloadStream()
    {
        var stream = new MemoryStream();
        Challenge!.Save(stream);
        stream.Position = 0;
        return stream;
    }
}
