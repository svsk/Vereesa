using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Vereesa.Neon.Data.Interfaces;

namespace Vereesa.Neon.Data.Repositories;

public class SimpleStore : ISimpleStore
{
    private readonly ConcurrentDictionary<string, object> _store = new();
    private readonly string _fileDirectory;
    private readonly string _fileName;
    private string _filePath => Path.Combine(_fileDirectory, _fileName);
    private readonly object _fileLock = new();

    // Singleton instance managed via DI
    public SimpleStore(string fileDirectory, string fileName = "simplestore.json")
    {
        _fileDirectory = fileDirectory;
        _fileName = fileName;
        LoadFromDisk();
    }

    // Retrieve a value by key
    public T? Get<T>(string key)
    {
        if (_store.TryGetValue(key, out var value))
        {
            // Handle primitive types stored as JsonElements.
            if (value is JsonElement jsonElement)
            {
                return JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
            }

            return (T)value;
        }

        return default;
    }

    // Set a value by key
    public void Set<T>(string key, T? value)
    {
        if (value == null)
        {
            Remove(key);
            return;
        }

        _store[key] = value;
        WriteToDisk();
    }

    // Remove a key-value pair
    public void Remove(string key)
    {
        _store.TryRemove(key, out _);
        WriteToDisk();
    }

    // Load data from disk on startup
    private void LoadFromDisk()
    {
        if (File.Exists(_filePath))
        {
            lock (_fileLock)
            {
                var json = File.ReadAllText(_filePath);
                var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                if (data != null)
                {
                    foreach (var kvp in data)
                    {
                        _store[kvp.Key] = kvp.Value!;
                    }
                }
            }
        }
    }

    // Write all data to disk after a change
    private void WriteToDisk()
    {
        lock (_fileLock)
        {
            if (Directory.Exists(_fileDirectory) == false)
            {
                Directory.CreateDirectory(_fileDirectory);
            }

            var json = JsonSerializer.Serialize(_store);
            File.WriteAllText(_filePath, json);
        }
    }

    // Allow manual saving if required
    public void SaveToDisk()
    {
        WriteToDisk();
    }
}
