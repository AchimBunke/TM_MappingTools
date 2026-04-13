
using GBX.NET;
using Microsoft.AspNetCore.Components.Forms;
using TM_GenericMapping.IO;
using TM_MappingTools.Utils;

namespace TM_MappingTools.Services;

public class ClipFileService : FileService
{
    public Clip? Clip { get; private set; }
    
    public override void Clear()
    {
        Clip = null;
        base.Clear();
    }
    public override async Task SetFileAsync(IBrowserFile file)
    {
        await base.SetFileAsync(file);
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
            InvokeFileChanged();
        }
        catch (IOException io)
        {
            Console.WriteLine($"Error opening file: {io.Message}");
            Clear();
       
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
