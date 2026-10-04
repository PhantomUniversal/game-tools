using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Aion2Tools.Models;

namespace Aion2Tools.Services;

/// <summary>Classes, presets and weights: the built-in copy, or a newer one downloaded from the repository.</summary>
public static class GameDataService
{
    private const string RESOURCE_NAME = "gamedata.json";

    private static readonly TimeSpan DOWNLOAD_LIMIT = TimeSpan.FromSeconds(10);

    /// <summary>The newer of the built-in and the downloaded copy.</summary>
    public static GameDataTable Load()
    {
        GameDataTable builtIn = LoadBuiltIn();
        if (!File.Exists(AppPaths.GameDataFile))
        {
            return builtIn;
        }

        GameDataTable? downloadedOrNull = ParseOrNull(File.ReadAllText(AppPaths.GameDataFile));
        if (downloadedOrNull is null || downloadedOrNull.Version <= builtIn.Version)
        {
            return builtIn;
        }

        return downloadedOrNull;
    }

    public static GameDataTable LoadBuiltIn()
    {
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(RESOURCE_NAME)!;
        using StreamReader reader = new StreamReader(stream);
        return ParseOrNull(reader.ReadToEnd())!;
    }

    /// <summary>Null when the JSON is malformed or missing what a composition needs.</summary>
    public static GameDataTable? ParseOrNull(string json)
    {
        GameDataTable? tableOrNull = JsonUtil.DeserializeOrNull<GameDataTable>(json);
        if (tableOrNull is null || tableOrNull.Classes.Count == 0 || tableOrNull.Presets.Count == 0)
        {
            return null;
        }

        foreach (ClassRecord record in tableOrNull.Classes)
        {
            if (record.Id.Length == 0 || record.Role == RoleKind.None)
            {
                return null;
            }
        }

        foreach (PresetRecord preset in tableOrNull.Presets)
        {
            if (preset.PartyCount < 1 || preset.PartySize < 1)
            {
                return null;
            }
        }

        return tableOrNull;
    }

    /// <summary>The text at the URL, or null when it cannot be fetched.</summary>
    public static async Task<string?> DownloadOrNull(string url)
    {
        try
        {
            using HttpClient client = new HttpClient();
            client.Timeout = DOWNLOAD_LIMIT;
            return await client.GetStringAsync(url);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static void SaveDownloaded(string json)
    {
        Directory.CreateDirectory(AppPaths.SettingsFolder);
        File.WriteAllText(AppPaths.GameDataFile, json);
    }
}
