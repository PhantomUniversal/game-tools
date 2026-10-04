using System.Collections.Generic;
using System.Linq;
using Aion2Tools.Models;
using Aion2Tools.Services;
using Xunit;

namespace Aion2Tools.Tests;

public class PartyServiceTests
{
    private static readonly GameDataTable DATA = GameDataService.LoadBuiltIn();

    [Fact]
    public void Compose_TenFromTwelve_SeatsTenAndBenchesTwo()
    {
        PartyResultModel result = ComposeBest(CreateRaidRoster(), GetPreset("sanctuary-rudra"));

        Assert.Equal(new[] { 5, 5 }, result.Parties.Select(party => party.Members.Count));
        Assert.Equal(2, result.Bench.Count);
    }

    [Fact]
    public void Compose_TwoOfEachRole_GivesEveryPartyOneTankHealerAndChanter()
    {
        PartyResultModel result = ComposeBest(CreateRaidRoster(), GetPreset("sanctuary-rudra"));

        foreach (PartyModel party in result.Parties)
        {
            Assert.Equal(1, CountRole(party, RoleKind.Tank));
            Assert.Equal(1, CountRole(party, RoleKind.Healer));
            Assert.Equal(1, CountRole(party, RoleKind.Support));
        }
    }

    [Fact]
    public void Compose_Guardian_GoesToFirstParty()
    {
        PartyResultModel result = ComposeBest(CreateRaidRoster(), GetPreset("sanctuary-rudra"));

        Assert.Contains(result.Parties[0].Members, member => member.ClassId == "guardian");
    }

    [Fact]
    public void Compose_OneChanter_PutsAssassinWithChanter()
    {
        List<CharacterData> roster = new List<CharacterData>
        {
            Create(1, "탱1", "guardian", 30000),
            Create(2, "탱2", "gladiator", 30000),
            Create(3, "힐1", "cleric", 30000),
            Create(4, "힐2", "cleric", 30000),
            Create(5, "호법", "chanter", 30000),
            Create(6, "살성", "assassin", 30000),
            Create(7, "궁성1", "ranger", 30000),
            Create(8, "궁성2", "ranger", 30000),
            Create(9, "정령1", "spiritmaster", 30000),
            Create(10, "정령2", "spiritmaster", 30000),
        };

        PartyResultModel result = ComposeBest(roster, GetPreset("sanctuary-rudra"));

        PartyModel chanterParty = result.Parties.Single(party => party.Members.Any(member => member.ClassId == "chanter"));
        Assert.Contains(chanterParty.Members, member => member.ClassId == "assassin");
    }

    [Fact]
    public void Compose_SameNumberTwice_SeatsOnlyOneOfItsCharacters()
    {
        List<CharacterData> roster = CreateRaidRoster();
        roster.Add(Create(1, "철벽 부캐", "assassin", 50000));

        PartyResultModel result = ComposeBest(roster, GetPreset("sanctuary-rudra"));

        int seatedFromOne = result.Parties.Sum(party => party.Members.Count(member => member.Number == 1));
        Assert.Equal(1, seatedFromOne);
    }

    [Fact]
    public void Compose_FewerThanTheSeats_SeatsEveryone()
    {
        List<CharacterData> roster = new List<CharacterData>
        {
            Create(1, "철벽", "guardian", 30000),
            Create(2, "불꽃", "sorcerer", 30000),
            Create(3, "칼바람", "gladiator", 30000),
            Create(4, "바람결", "spiritmaster", 30000),
            Create(5, "새벽", "cleric", 30000),
        };

        PartyResultModel result = ComposeBest(roster, GetPreset("sanctuary-rudra"));

        Assert.Empty(result.Bench);
        Assert.Equal(5, result.Parties.Sum(party => party.Members.Count));
    }

    [Fact]
    public void Compose_BelowItemLevel_IsBenchedWithReason()
    {
        List<CharacterData> roster = CreateRaidRoster();
        CharacterData low = Create(13, "초보", "ranger", 40000);
        low.ItemLevel = 1000;
        roster.Add(low);

        PartyResultModel result = ComposeBest(roster, GetPreset("sanctuary-rudra"));

        BenchModel bench = Assert.Single(result.Bench, entry => entry.Character == low);
        Assert.StartsWith("아이템 레벨 미달", bench.Reason);
    }

    [Fact]
    public void Compose_ValuesNotEntered_PassTheCuts()
    {
        List<CharacterData> roster = CreateRaidRoster();
        foreach (CharacterData character in roster)
        {
            character.CombatPower = null;
            character.ItemLevel = null;
        }

        PresetRecord preset = new PresetRecord();
        preset.PartyCount = 2;
        preset.PartySize = 5;
        preset.MinItemLevel = 4500;
        preset.MinCombatPower = 50000;
        PartyResultModel result = ComposeBest(roster, preset);

        Assert.Equal(10, result.Parties.Sum(party => party.Members.Count));
        Assert.All(result.Bench, bench => Assert.Equal("정원 초과", bench.Reason));
    }

    [Fact]
    public void Compose_NobodyEligible_ReturnsEmpty()
    {
        CharacterData unknown = Create(1, "무명", "unknown-class", 30000);

        IReadOnlyList<PartyResultModel> results = PartyService.Compose(new[] { unknown }, GetPreset("expedition"), DATA, 3, 0);

        Assert.Empty(results);
    }

    [Fact]
    public void Compose_SamePowerRoster_KeepsPartiesWithinTwoPercent()
    {
        PartyResultModel result = ComposeBest(CreateRaidRoster(), GetPreset("sanctuary-rudra"));

        double spread = PartyService.GetSpreadPercent(result.Parties.Select(party => (double)party.TotalCombatPower).ToList());
        Assert.True(spread < 2.0, $"spread {spread:0.00}%");
    }

    private static PartyResultModel ComposeBest(IReadOnlyList<CharacterData> roster, PresetRecord preset)
    {
        return PartyService.Compose(roster, preset, DATA, 1, 0)[0];
    }

    private static PresetRecord GetPreset(string id)
    {
        return DATA.Presets.Single(preset => preset.Id == id);
    }

    private static int CountRole(PartyModel party, RoleKind role)
    {
        return party.Members.Count(member => DATA.GetClassOrNull(member.ClassId)!.Role == role);
    }

    /// <summary>Twelve numbers: two of each tank, healer and chanter, six dealers.</summary>
    private static List<CharacterData> CreateRaidRoster()
    {
        return new List<CharacterData>
        {
            Create(1, "철벽", "guardian", 36000),
            Create(2, "칼바람", "gladiator", 35000),
            Create(3, "새벽", "cleric", 32000),
            Create(4, "은하", "cleric", 31000),
            Create(5, "수호천사", "chanter", 31000),
            Create(6, "축복", "chanter", 30000),
            Create(7, "그림자", "assassin", 38000),
            Create(8, "불꽃", "sorcerer", 35000),
            Create(9, "주먹왕", "fighter", 34000),
            Create(10, "바람결", "spiritmaster", 32000),
            Create(11, "별빛", "ranger", 33000),
            Create(12, "초보", "ranger", 24000),
        };
    }

    private static CharacterData Create(int number, string name, string classId, int combatPower)
    {
        CharacterData character = new CharacterData();
        character.Number = number;
        character.Name = name;
        character.ClassId = classId;
        character.CombatPower = combatPower;
        character.ItemLevel = 3000;
        return character;
    }
}
