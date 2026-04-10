using System.Text.Json;
using Microsoft.AspNetCore.Hosting;

namespace FlowCore.Services;

/// <summary>Loads UI strings from <c>locales/en.default.json</c> (copy with the app).</summary>
public sealed class UiText
{
    private readonly IWebHostEnvironment _env;
    private Dictionary<string, string>? _map;

    public UiText(IWebHostEnvironment env) => _env = env;

    public string this[string key]
    {
        get
        {
            _map ??= Load();
            return _map.TryGetValue(key, out var v) ? v : key;
        }
    }

    private Dictionary<string, string> Load()
    {
        var path = Path.Combine(_env.ContentRootPath, "locales", "en.default.json");
        if (!File.Exists(path))
            return new Dictionary<string, string>();

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                   ?? new Dictionary<string, string>();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }
}
