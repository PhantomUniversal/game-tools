using System.Collections.Generic;

namespace Aion2Tools.Models;

/// <summary>One party of a composition.</summary>
public class PartyModel
{
    /// <summary>1-based, as the game counts parties.</summary>
    public int Number { get; }

    public IReadOnlyList<CharacterData> Members { get; }

    public int TotalCombatPower { get; }

    public PartyModel(int number, IReadOnlyList<CharacterData> members)
    {
        Number = number;
        Members = members;
        int total = 0;
        foreach (CharacterData member in members)
        {
            total += member.CombatPower;
        }

        TotalCombatPower = total;
    }
}
