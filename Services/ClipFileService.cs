
using GBX.NET;
using Microsoft.AspNetCore.Components.Forms;
using TM_GenericMapping.IO;
using TM_MappingTools.Utils;

namespace TM_MappingTools.Services;

public class ClipFileService
{
    public string FileName { get; private set; } = string.Empty;
    public long FileSize { get; private set; } = 0;
    public bool HasFile => Clip != null;
    public Clip? Clip { get; private set; }
    
    public event Action? FileChanged;

    public void Clear()
    {
        FileName = string.Empty;
        FileSize = 0;
        Clip = null;
        FileChanged?.Invoke();

    }
    public async Task SetFileAsync(IBrowserFile file)
    {
        FileName = file.Name;
        FileSize = file.Size;
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
            Clip.Open(memoryStream);
            FileChanged?.Invoke();
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
