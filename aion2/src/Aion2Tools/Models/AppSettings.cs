using System.Collections.ObjectModel;

namespace Aion2Tools.Models;

/// <summary>Everything the user changes, saved to settings/settings.json beside the executable.</summary>
public class AppSettings
{
    public const string DEFAULT_GAME_DATA_URL = "https://raw.githubusercontent.com/PhantomUniversal/game-tools/main/aion2/data/gamedata.json";

    public string GameDataUrl { get; set; } = DEFAULT_GAME_DATA_URL;

    public bool IsAutoCheckOn { get; set; } = true;

    public string PresetId { get; set; } = string.Empty;

    public ObservableCollection<CharacterData> Characters { get; set; } = new ObservableCollection<CharacterData>();

    public AppSettings()
    {
    }
}
