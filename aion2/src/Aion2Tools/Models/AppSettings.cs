using System.Collections.ObjectModel;

namespace Aion2Tools.Models;

/// <summary>Everything the user changes, saved to settings/settings.json beside the executable.</summary>
public class AppSettings
{
    public const int DEFAULT_MAX_GROUP_COUNT = 128;
    public const int MIN_MAX_GROUP_COUNT = 1;
    public const int MAX_MAX_GROUP_COUNT = 999;

    public const string DEFAULT_GAME_DATA_URL = "https://raw.githubusercontent.com/PhantomUniversal/game-tools/main/aion2/data/gamedata.json";

    public string GameDataUrl { get; set; } = DEFAULT_GAME_DATA_URL;

    public bool IsAutoCheckOn { get; set; } = true;

    public string PresetId { get; set; } = string.Empty;

    /// <summary>How many groups the roster holds; one group per legion member.</summary>
    public int MaxGroupCount { get; set; } = DEFAULT_MAX_GROUP_COUNT;

    public ObservableCollection<CharacterData> Characters { get; set; } = new ObservableCollection<CharacterData>();

    public AppSettings()
    {
    }
}
