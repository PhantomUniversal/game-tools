using CommunityToolkit.Mvvm.ComponentModel;

namespace Aion2Tools.Models;

/// <summary>One roster character. Edited in place on the roster page, so it raises its own changes.</summary>
public partial class CharacterData : ObservableObject
{
    [ObservableProperty]
    public partial string Player { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ClassId { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int CombatPower { get; set; }

    [ObservableProperty]
    public partial int ItemLevel { get; set; }

    /// <summary>The player's main character; the rest of the player's characters are alts.</summary>
    [ObservableProperty]
    public partial bool IsMain { get; set; }

    /// <summary>Available this time. Only checked characters go into a composition.</summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; } = true;

    public CharacterData()
    {
    }
}
