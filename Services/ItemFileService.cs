using GBX.NET;
using GBX.NET.Engines.GameData;
using Microsoft.AspNetCore.Components.Forms;
using TM_MappingTools.Utils;

namespace TM_MappingTools.Services;

public class ItemFileService : GbxFileService
{
    public CGameItemModel? Item { get; private set; }

    public override object? LoadedContent => Item;
    public override string SupportedExtension => ".Item.Gbx";
    public override string ContentTypeName => "Item";

    public override void Clear()
    {
        Item = null;
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

            Item = await Gbx.ParseAsync<CGameItemModel>(memoryStream);
            if (Item is null)
            {
                throw new InvalidDataException("Parsed item content was null.");
            }

            await base.SetFileAsync(file);
            InvokeContentLoaded();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening item file: {ex.Message}");
            Clear();
        }
    }

    public Stream GetDownloadStream()
    {
        var stream = new MemoryStream();
        Item!.Save(stream);
        stream.Position = 0;
        return stream;
    }
}
