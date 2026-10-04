namespace Aion2Tools.Models;

/// <summary>Score weights the party composer adds up. Penalties are positive and subtracted.</summary>
public class WeightsRecord
{
    public int MissingTank { get; set; }

    public int MissingHealer { get; set; }

    public int SupportInParty { get; set; }

    public int ExtraSameRole { get; set; }

    public int BuffPriority { get; set; }

    public int RaidDebuffDuplicate { get; set; }

    public int PreferredParty { get; set; }

    public int DuplicatePlayer { get; set; }

    public int BalancePercent { get; set; }

    public int CombatPowerPer1000 { get; set; }

    public WeightsRecord()
    {
    }
}
