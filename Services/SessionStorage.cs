using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace TM_MappingTools.Services;

public class SessionStorage
{
    private readonly Dictionary<string, object?> _store = new();
    public void Set<T>(string key, T value) => _store[key] = value;
    public void Set<T>([CallerFilePath] string? file = null, [CallerMemberName] string? member = null, T? value = default)
    {
        if (file == null || member == null)
            throw new ArgumentException("Caller information is not available.");
        Set(CallerToKey(file, member), value);
    }
    public T? Get<T>(string key)
    {
        if (_store.TryGetValue(key, out var value))
            return (T?)value;
        return default;
    }
    public T? Get<T>([CallerFilePath] string? file = null, [CallerMemberName] string? member = null)
    {
        if (file == null || member == null)
            throw new ArgumentException("Caller information is not available.");
        return Get<T>(CallerToKey(file, member));
    }
    public bool Has(string key) => _store.ContainsKey(key);
    public bool Has([CallerFilePath] string? file = null, [CallerMemberName] string? member = null)
    {
        if (file == null || member == null)
            throw new ArgumentException("Caller information is not available.");
        return Has(CallerToKey(file, member));
    }
   

    public void Remove(string key) => _store.Remove(key);
    public void Remove([CallerFilePath] string? file = null, [CallerMemberName] string? member = null)
    {
        if (file == null || member == null)
            throw new ArgumentException("Caller information is not available.");
        Remove(CallerToKey(file, member));
    }

    public void Clear() => _store.Clear();

    string CallerToKey(string file, string member) => $"{file}:{member}";
}
