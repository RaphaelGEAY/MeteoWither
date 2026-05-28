using System;
using System.IO;
using System.Threading.Tasks;
using MétéoWither.Models;
using Newtonsoft.Json;

namespace MétéoWither.Services;

public static class AppStorageService
{
    private static string BasePath => AppContext.BaseDirectory;

    public static string ConfigPath => Path.Combine(BasePath, "config.json");

    public static string OptionsPath => Path.Combine(BasePath, "options.json");

    public static async Task<AppConfig?> LoadConfigAsync()
    {
        if (!File.Exists(ConfigPath))
        {
            return null;
        }

        try
        {
            var content = await File.ReadAllTextAsync(ConfigPath);
            return JsonConvert.DeserializeObject<AppConfig>(content);
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
            var options = JsonConvert.DeserializeObject<AppOptions>(content);

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
        var json = JsonConvert.SerializeObject(options, Formatting.Indented);
        return File.WriteAllTextAsync(OptionsPath, json);
    }
}
