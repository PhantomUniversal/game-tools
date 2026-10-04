namespace Aion2Tools.Models;

/// <summary>One class row of the game data.</summary>
public class ClassRecord
{
    public string Id { get; set; } = string.Empty;

    /// <summary>Shown before the name wherever a class is picked. An emoji, so the data can change it.</summary>
    public string Icon { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public RoleKind Role { get; set; }

    /// <summary>Puts a debuff on the boss that the whole force shares; two of the same class do not stack.</summary>
    public bool IsRaidDebuff { get; set; }

    /// <summary>How much a dealer gains from sitting in a party with a Chanter. 0 = no preference.</summary>
    public int BuffPriority { get; set; }

    /// <summary>The party (1-based) this class should go to. 0 = any.</summary>
    public int PreferredParty { get; set; }

    public ClassRecord()
    {
    }
}
