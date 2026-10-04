using System;
using System.IO;

namespace Aion2Tools.Services;

/// <summary>Everything sits beside the executable, so the folder is the whole installation.</summary>
public static class AppPaths
{
    private const string SETTINGS_FOLDER_NAME = "settings";
    private const string SETTINGS_FILE_NAME = "settings.json";
    private const string GAME_DATA_FILE_NAME = "gamedata.json";

    /// <summary>Read from the process: a single-file build's AppContext.BaseDirectory can be the unpack folder.</summary>
    public static string ExecutableFolder { get; } = ResolveExecutableFolder();

    public static string SettingsFolder { get; } = Path.Combine(ExecutableFolder, SETTINGS_FOLDER_NAME);

    public static string SettingsFile { get; } = Path.Combine(SettingsFolder, SETTINGS_FILE_NAME);

    /// <summary>The last game data downloaded, used when it is newer than the built-in copy.</summary>
    public static string GameDataFile { get; } = Path.Combine(SettingsFolder, GAME_DATA_FILE_NAME);

    private static string ResolveExecutableFolder()
    {
        string? processPathOrNull = Environment.ProcessPath;
        if (processPathOrNull is null)
        {
            return AppContext.BaseDirectory;
        }

        string? folderOrNull = Path.GetDirectoryName(processPathOrNull);
        if (folderOrNull is null || folderOrNull.Length == 0)
        {
            return AppContext.BaseDirectory;
        }

        return folderOrNull;
    }
}
