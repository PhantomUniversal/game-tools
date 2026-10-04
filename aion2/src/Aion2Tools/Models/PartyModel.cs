using System.Collections.Generic;

namespace Aion2Tools.Models;

/// <summary>One party of a composition.</summary>
public class PartyModel
{
    /// <summary>1-based, as the game counts parties.</summary>
    public int Number { get; }

    public IReadOnlyList<CharacterData> Members { get; }

    /// <summary>The sum of the combat powers entered; members without one add nothing.</summary>
    public int TotalCombatPower { get; }

    /// <summary>Members with no combat power entered.</summary>
    public int UnknownCount { get; }

    public PartyModel(int number, IReadOnlyList<CharacterData> members)
    {
        Number = number;
        Members = members;
        int total = 0;
        int unknown = 0;
        foreach (CharacterData member in members)
        {
            if (member.CombatPower.HasValue)
            {
                total += member.CombatPower.Value;
            }
            else
            {
                unknown++;
            }
        }

        TotalCombatPower = total;
        UnknownCount = unknown;
    }
}
