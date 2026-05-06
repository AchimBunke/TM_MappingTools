using System;

namespace TM_MappingTools.Model;

public sealed class InMemoryObjSource : TM_GenericMapping.MediaTracker.IO.ObjSerializer.IObjSource
{
    private readonly IReadOnlyDictionary<string, byte[]> files;

    public InMemoryObjSource(IReadOnlyDictionary<string, byte[]> files)
    {
        this.files = files;
    }

    public Stream Open(string name)
    {
        if (!files.TryGetValue(name, out var data))
        {
            throw new FileNotFoundException($"Stream source has no entry for '{name}'");
        }

        // ObjSerializer disposes each opened stream; return a fresh one every time.
        return new MemoryStream(data, writable: false);
    }

    public bool Exists(string name)
    {
        return files.ContainsKey(name);
    }
}
