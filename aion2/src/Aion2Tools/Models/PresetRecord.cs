namespace Aion2Tools.Models;

/// <summary>One content preset row of the game data: how many parties of what size, and the entry cuts.</summary>
public class PresetRecord
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int PartyCount { get; set; }

    public int PartySize { get; set; }

    public int MinItemLevel { get; set; }

    public int MinCombatPower { get; set; }

    /// <summary>How many runs one character may enter, such as a weekly lockout; 0 is no limit.</summary>
    public int EntryLimit { get; set; }

    public PresetRecord()
    {
    }
}
