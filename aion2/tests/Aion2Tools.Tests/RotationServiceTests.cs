using System.Collections.Generic;
using System.Linq;
using Aion2Tools.Models;
using Aion2Tools.Services;
using Xunit;

namespace Aion2Tools.Tests;

public class RotationServiceTests
{
    private static readonly GameDataTable DATA = GameDataService.LoadBuiltIn();

    [Fact]
    public void Compose_MainFirst_PutsOnlyMainsInFirstRun()
    {
        IReadOnlyList<PartyResultModel> runs = ComposeRoster(true);

        List<CharacterData> firstRun = runs[0].Parties.SelectMany(party => party.Members).ToList();
        Assert.All(firstRun, member => Assert.True(member.IsMain));
    }

    [Fact]
    public void Compose_ThreeRuns_UsesNoCharacterTwiceAndOnePerNumberPerRun()
    {
        IReadOnlyList<PartyResultModel> runs = ComposeRoster(false);

        List<CharacterData> all = runs.SelectMany(run => run.Parties.SelectMany(party => party.Members)).ToList();
        Assert.Equal(all.Count, all.Distinct().Count());
        foreach (PartyResultModel run in runs)
        {
            List<int> numbers = run.Parties.SelectMany(party => party.Members).Select(member => member.Number).ToList();
            Assert.Equal(numbers.Count, numbers.Distinct().Count());
        }
    }

    private static IReadOnlyList<PartyResultModel> ComposeRoster(bool isMainFirst)
    {
        PresetRecord preset = DATA.Presets.Single(record => record.Id == "sanctuary-rudra");
        return RotationService.Compose(CreateRoster(), preset, DATA, 3, isMainFirst, 0);
    }

    /// <summary>Twelve numbers with their mains and alts, 22 characters in all.</summary>
    private static List<CharacterData> CreateRoster()
    {
        List<CharacterData> characters = new List<CharacterData>();
        Add(characters, 1, "철벽", "guardian", 36210, 3820, true);
        Add(characters, 1, "보조", "cleric", 28400, 3100, false);
        Add(characters, 1, "셋째", "ranger", 25300, 2900, false);
        Add(characters, 2, "그림자", "assassin", 38050, 4010, true);
        Add(characters, 2, "둘째", "chanter", 27100, 3050, false);
        Add(characters, 3, "새벽", "cleric", 32400, 3500, true);
        Add(characters, 3, "칼날", "gladiator", 29800, 3200, false);
        Add(characters, 3, "마나", "sorcerer", 26600, 2950, false);
        Add(characters, 4, "불꽃", "sorcerer", 35500, 3700, true);
        Add(characters, 5, "칼바람", "gladiator", 34800, 3650, true);
        Add(characters, 5, "방패", "guardian", 27900, 3000, false);
        Add(characters, 6, "주먹왕", "fighter", 33600, 3550, true);
        Add(characters, 6, "바람", "spiritmaster", 26000, 2800, false);
        Add(characters, 7, "바람결", "spiritmaster", 31900, 3400, true);
        Add(characters, 7, "은빛", "cleric", 27500, 2950, false);
        Add(characters, 8, "은하", "cleric", 30700, 3300, true);
        Add(characters, 8, "번개", "assassin", 28200, 3000, false);
        Add(characters, 9, "수호천사", "chanter", 31200, 3350, true);
        Add(characters, 9, "주문", "chanter", 26900, 2900, false);
        Add(characters, 10, "축복", "chanter", 30500, 3300, true);
        Add(characters, 11, "별빛", "ranger", 33000, 3500, true);
        Add(characters, 12, "초보", "ranger", 24000, 2500, true);
        return characters;
    }

    private static void Add(List<CharacterData> characters, int number, string name, string classId, int combatPower, int itemLevel, bool isMain)
    {
        CharacterData character = new CharacterData();
        character.Number = number;
        character.Name = name;
        character.ClassId = classId;
        character.CombatPower = combatPower;
        character.ItemLevel = itemLevel;
        character.IsMain = isMain;
        characters.Add(character);
    }
}
