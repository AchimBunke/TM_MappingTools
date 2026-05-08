using System;
using Microsoft.AspNetCore.Components.Forms;
using TM_MappingTools.Utils;

namespace TM_MappingTools.Services;

public class FileService : IFileService
{
    public string FileName { get; private set; } = string.Empty;
    public long FileSize { get; private set; } = 0;
    public bool HasFile => _browserFile != null;
    public IReadOnlyList<IBrowserFile> Files => _browserFiles.AsReadOnly();
    public bool HasFiles => _browserFiles.Count > 0;

    protected IBrowserFile? _browserFile;
    protected List<IBrowserFile> _browserFiles = new();

    public event Action? FileChanged;

    public virtual void Clear()
    {
        FileName = string.Empty;
        FileSize = 0;
        _browserFile = null;
        _browserFiles.Clear();
        InvokeFileChanged();
    }

    public virtual async Task SetFileAsync(IBrowserFile file)
    {
        FileName = file.Name;
        FileSize = file.Size;
        _browserFile = file;
        _browserFiles.Clear();
        _browserFiles.Add(file);
        InvokeFileChanged();
    }

    public virtual async Task SetFilesAsync(IEnumerable<IBrowserFile> files)
    {
        _browserFiles = files.ToList();
        if (_browserFiles.Count > 0)
        {
            _browserFile = _browserFiles[0];
            FileName = _browserFile.Name;
            FileSize = _browserFile.Size;
        }
        else
        {
            _browserFile = null;
            FileName = string.Empty;
            FileSize = 0;
        }
        InvokeFileChanged();
    }

    protected void InvokeFileChanged() => FileChanged?.Invoke();

    public virtual Stream GetDownloadStream()
    {
        if (_browserFile == null)
            throw new InvalidOperationException("No file is currently loaded.");

        return _browserFile.OpenReadStream(maxAllowedSize: FileHelper.MaxAllowedFileSize);  
    }
}
