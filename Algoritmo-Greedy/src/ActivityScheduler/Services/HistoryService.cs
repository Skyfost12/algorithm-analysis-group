using System.Text.Json;
using TurnosSalas.Models;

namespace TurnosSalas.Services;

public class HistoryService
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public HistoryService(IWebHostEnvironment env)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "data");
        Directory.CreateDirectory(dataDir);
        _filePath = Path.Combine(dataDir, "historial.json");

        if (!File.Exists(_filePath))
        {
            File.WriteAllText(_filePath, "[]");
        }
    }

    public async Task<List<HistoryEntry>> ReadAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var raw = await File.ReadAllTextAsync(_filePath);
            return JsonSerializer.Deserialize<List<HistoryEntry>>(raw) ?? [];
        }
        catch
        {
            return [];
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task AppendAsync(HistoryEntry entry)
    {
        await _lock.WaitAsync();
        try
        {
            var raw = await File.ReadAllTextAsync(_filePath);
            var history = JsonSerializer.Deserialize<List<HistoryEntry>>(raw) ?? [];
            history.Add(entry);
            await File.WriteAllTextAsync(
                _filePath,
                JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true }));
        }
        finally
        {
            _lock.Release();
        }
    }
}
