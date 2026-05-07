using Microsoft.AspNetCore.Components.Forms;

namespace TM_MappingTools.Services;

public class FileTypeManagedFileService : IFileService
{
    private readonly FileTypeManager _fileTypeManager;

    public FileTypeManagedFileService(FileTypeManager fileTypeManager)
    {
        _fileTypeManager = fileTypeManager;
    }

    public string FileName => _fileTypeManager.CurrentService?.FileName ?? string.Empty;
    public long FileSize => _fileTypeManager.CurrentService?.FileSize ?? 0;
    public bool HasFile => _fileTypeManager.CurrentService?.HasFile == true;
    public IReadOnlyList<IBrowserFile> Files => _fileTypeManager.CurrentService?.Files ?? Array.Empty<IBrowserFile>();
    public bool HasFiles => _fileTypeManager.CurrentService?.HasFiles == true;

    public event Action? FileChanged;

    public void Clear()
    {
        _fileTypeManager.Clear();
        FileChanged?.Invoke();
    }

    public async Task SetFileAsync(IBrowserFile file)
    {
        await _fileTypeManager.LoadFileAsync(file);
        FileChanged?.Invoke();
    }

    public async Task SetFilesAsync(IEnumerable<IBrowserFile> files)
    {
        var fileList = files.ToList();
        if (fileList.Count != 1)
        {
            throw new InvalidOperationException("This file service only supports selecting a single file.");
        }

        await SetFileAsync(fileList[0]);
    }
}