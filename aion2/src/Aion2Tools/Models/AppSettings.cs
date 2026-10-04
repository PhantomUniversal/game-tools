using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Aion2Tools.Models;

/// <summary>Everything the user changes, saved to settings/settings.json beside the executable.</summary>
public class AppSettings
{
    public const string DEFAULT_GAME_DATA_URL = "https://raw.githubusercontent.com/PhantomUniversal/game-tools/main/aion2/data/gamedata.json";

    public string GameDataUrl { get; set; } = DEFAULT_GAME_DATA_URL;

    public bool IsAutoCheckOn { get; set; } = true;

    public string PresetId { get; set; } = string.Empty;

    public ObservableCollection<ProfileData> Profiles { get; set; } = new ObservableCollection<ProfileData>();

    /// <summary>The single roster saved before profiles existed; moved into a profile on load and not written again.</summary>
    [JsonPropertyName("Characters")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ObservableCollection<CharacterData>? LegacyCharactersOrNull { get; set; }

    public AppSettings()
    {
    }
}
