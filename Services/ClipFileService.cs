
using GBX.NET;
using Microsoft.AspNetCore.Components.Forms;
using TM_GenericMapping.IO;
using TM_MappingTools.Utils;

namespace TM_MappingTools.Services;

public class ClipFileService : GbxFileService
{
    public Clip? Clip { get; private set; }

    public override object? LoadedContent => Clip;
    public override string SupportedExtension => ".Clip.Gbx";
    public override string ContentTypeName => "Clip";
    
    public override void Clear()
    {
        Clip = null;
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

            Clip = new Clip() { 
                SavePath = file.Name,
                WriteSettings = new GbxWriteSettings()
                {
                    CloseStream = false,
                }
            };
            await Clip.OpenAsync(memoryStream);
            if(Clip.MediaClip == null)
            {
                throw new InvalidDataException("Parsed clip content was null.");
            }
            await base.SetFileAsync(file);
            InvokeContentLoaded();
        }
        catch (Exception ex)
        {
            Clear();
            throw new InvalidDataException($"Failed to parse clip file '{file.Name}': {ex.Message}", ex);
        }
    }

    public Stream GetDownloadStream()
    {
        var stream = new MemoryStream();
        Clip!.Save(stream);
        stream.Position = 0;
        return stream;
    }
}
