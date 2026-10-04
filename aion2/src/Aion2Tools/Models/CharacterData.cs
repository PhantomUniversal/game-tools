using CommunityToolkit.Mvvm.ComponentModel;

namespace Aion2Tools.Models;

/// <summary>One roster character. Edited in place on the roster page, so it raises its own changes.</summary>
public partial class CharacterData : ObservableObject
{
    public const int MIN_NUMBER = 1;
    public const int MAX_NUMBER = 10;

    /// <summary>Who plays this character. Characters sharing a number are one person's main and alts.</summary>
    [ObservableProperty]
    public partial int Number { get; set; } = MIN_NUMBER;

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ClassId { get; set; } = string.Empty;

    /// <summary>Null = not entered: passes the cut, and counts as the roster average when balancing.</summary>
    [ObservableProperty]
    public partial int? CombatPower { get; set; }

    /// <summary>Null = not entered: passes the cut.</summary>
    [ObservableProperty]
    public partial int? ItemLevel { get; set; }

    /// <summary>The player's main character; the rest of the player's characters are alts.</summary>
    [ObservableProperty]
    public partial bool IsMain { get; set; }

    public CharacterData()
    {
    }
}
