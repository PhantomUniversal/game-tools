using System.Collections.Generic;
using System.Linq;
using Aion2Tools.Models;

namespace Aion2Tools.Services;

/// <summary>Runs one after another from mains and alts: one character per player per run, and each
/// character in one run only (weekly lockout). Each run is a party composition of what is left.</summary>
public static class RotationService
{
    public static IReadOnlyList<PartyResultModel> Compose(
        IReadOnlyList<CharacterData> characters, PresetRecord preset, GameDataTable data, int runCount, bool isMainFirst, int seed)
    {
        List<PartyResultModel> runs = new List<PartyResultModel>();
        HashSet<CharacterData> used = new HashSet<CharacterData>();
        for (int run = 0; run < runCount; run++)
        {
            List<CharacterData> pool = characters.Where(character => !used.Contains(character)).ToList();
            if (isMainFirst && run == 0)
            {
                pool = pool.Where(character => character.IsMain).ToList();
            }

            IReadOnlyList<PartyResultModel> results = PartyService.Compose(pool, preset, data, 1, seed + run);
            if (results.Count == 0)
            {
                break;
            }

            PartyResultModel result = results[0];
            List<CharacterData> seated = result.Parties.SelectMany(party => party.Members).ToList();
            if (seated.Count == 0)
            {
                break;
            }

            used.UnionWith(seated);
            runs.Add(result);
        }

        return runs;
    }
}
