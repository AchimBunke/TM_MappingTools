using System;

namespace TM_MappingTools.Services;

public interface IFileService
{
    // Single file properties (for backward compatibility)
    public string FileName { get; }
    public long FileSize { get; }
    public bool HasFile { get; }

    // Multiple file support
    public IReadOnlyList<Microsoft.AspNetCore.Components.Forms.IBrowserFile> Files { get; }
    public bool HasFiles { get; }

    public void Clear();
    public event Action? FileChanged;
    public Task SetFileAsync(Microsoft.AspNetCore.Components.Forms.IBrowserFile file);
    public Task SetFilesAsync(IEnumerable<Microsoft.AspNetCore.Components.Forms.IBrowserFile> files);
}
