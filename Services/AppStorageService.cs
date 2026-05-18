using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using MétéoWither.Models;

namespace MétéoWither.Services;

public static class AppStorageService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private static string BasePath => AppContext.BaseDirectory;

    private static string ConfigPath => Path.Combine(BasePath, "config.json");

    private static string OptionsPath => Path.Combine(BasePath, "options.json");

    public static async Task<AppConfig?> LoadConfigAsync()
    {
        if (!File.Exists(ConfigPath))
        {
            return null;
        }

        try
        {
            var content = await File.ReadAllTextAsync(ConfigPath);
            return JsonSerializer.Deserialize<AppConfig>(content, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public static async Task<AppOptions> LoadOptionsAsync()
    {
        if (!File.Exists(OptionsPath))
        {
            var defaultOptions = new AppOptions();
            await SaveOptionsAsync(defaultOptions);
            return defaultOptions;
        }

        try
        {
            var content = await File.ReadAllTextAsync(OptionsPath);
            var options = JsonSerializer.Deserialize<AppOptions>(content, JsonOptions);

            if (options is null)
            {
                throw new InvalidOperationException();
            }

            return options;
        }
        catch
        {
            var defaultOptions = new AppOptions();
            await SaveOptionsAsync(defaultOptions);
            return defaultOptions;
        }
    }

    public static Task SaveOptionsAsync(AppOptions options)
    {
        var json = JsonSerializer.Serialize(options, JsonOptions);
        return File.WriteAllTextAsync(OptionsPath, json);
    }
}
