using System.Collections.Generic;
using System.Linq;
using Aion2Tools.Models;
using Aion2Tools.Services;
using Xunit;

namespace Aion2Tools.Tests;

public class RotationServiceTests
{
    private static readonly GameDataTable DATA = GameDataService.LoadBuiltIn();

    private static readonly string[] CLASSES = { "guardian", "cleric", "chanter", "assassin", "sorcerer", "gladiator", "cleric", "chanter", "fighter", "ranger" };

    // ─────────────────────────────────────────────────────────────────────────
    // << 원정 >>
    // * Characters may repeat: each main plays its runs back to back, the mains spread evenly.
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public void Compose_FiveMainsAndAltsOverTenExpeditions_EachMainPlaysTwoRunsInARowAndEveryRunHasOneMain()
    {
        RotationResult rotation = RotationService.Compose(CreateFivePlayers(), GetPreset("expedition"), DATA, 10, 2, 0);

        Assert.Equal(10, rotation.Runs.Count);
        Assert.Equal(5, rotation.MainTurns.Count);
        foreach (IReadOnlyList<MainTurnModel> turn in rotation.MainTurns)
        {
            MainTurnModel main = Assert.Single(turn);
            Assert.True(main.IsBlock);
            Assert.Equal(main.Runs[0] + 1, main.Runs[1]);
        }

        foreach (PartyResultModel run in rotation.Runs)
        {
            List<CharacterData> members = GetMembers(run);
            Assert.Equal(5, members.Count);
            Assert.Equal(5, members.Select(member => member.Number).Distinct().Count());
            Assert.Equal(1, members.Count(member => member.IsMain));
        }
    }

    [Fact]
    public void Compose_ThreeRunsEachOverTenExpeditions_EachMainPlaysThreeAndRunsHoldOneOrTwoMains()
    {
        RotationResult rotation = RotationService.Compose(CreateFivePlayers(), GetPreset("expedition"), DATA, 10, 3, 0);

        List<CharacterData> seated = rotation.Runs.SelectMany(GetMembers).ToList();
        foreach (CharacterData main in seated.Where(member => member.IsMain).Distinct())
        {
            Assert.Equal(3, seated.Count(member => member == main));
        }

        Assert.All(rotation.Runs, run => Assert.InRange(GetMembers(run).Count(member => member.IsMain), 1, 2));
    }

    [Fact]
    public void Compose_TwoRuns_MixesMainsAndAltsInEachRun()
    {
        IReadOnlyList<PartyResultModel> runs = RotationService.Compose(CreateFivePlayers(), GetPreset("expedition"), DATA, 2, 1, 0).Runs;

        Assert.Equal(2, runs.Count);
        foreach (PartyResultModel run in runs)
        {
            List<CharacterData> members = GetMembers(run);
            Assert.Contains(members, member => member.IsMain);
            Assert.Contains(members, member => !member.IsMain);
        }
    }

    [Fact]
    public void Compose_PlayerWithOnlyAMain_PlaysJustTheMainRuns()
    {
        List<CharacterData> roster = CreateFivePlayers().Where(character => character.Number != 5 || character.IsMain).ToList();

        RotationResult rotation = RotationService.Compose(roster, GetPreset("expedition"), DATA, 10, 2, 0);

        List<CharacterData> seated = rotation.Runs.SelectMany(GetMembers).ToList();
        Assert.Equal(2, seated.Count(member => member.Number == 5));
        Assert.All(rotation.Runs, run => Assert.True(GetMembers(run).Count >= 4));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // << 성역 >>
    // * Each character enters once: mains and alts are mixed so the early runs fill first.
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public void Compose_SanctuaryTenMainsAndAlts_FillsBothRunsWithEveryCharacterOnce()
    {
        RotationResult rotation = RotationService.Compose(CreateTenPlayers(new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }), GetPreset("sanctuary-rudra"), DATA, 2, 2, 0);

        List<CharacterData> seated = rotation.Runs.SelectMany(GetMembers).ToList();
        Assert.Equal(seated.Count, seated.Distinct().Count());
        Assert.Equal(10, GetMembers(rotation.Runs[0]).Count);
        Assert.Equal(10, GetMembers(rotation.Runs[1]).Count);
    }

    [Fact]
    public void Compose_SanctuaryUnevenAlts_FillsTheEarlyRunsFirst()
    {
        RotationResult rotation = RotationService.Compose(CreateTenPlayers(new[] { 3, 0, 1, 2, 0, 1, 3, 1, 0, 2 }), GetPreset("sanctuary-rudra"), DATA, 4, 1, 0);

        List<int> counts = rotation.Runs.Select(run => GetMembers(run).Count).ToList();
        Assert.Equal(23, counts.Sum());
        Assert.Equal(10, counts[0]);
        Assert.True(counts.SequenceEqual(counts.OrderByDescending(count => count)), string.Join(",", counts));
    }

    [Fact]
    public void Compose_MoreNumbersThanSeats_SeatsOnePerNumberAndEachCharacterOnce()
    {
        IReadOnlyList<PartyResultModel> runs = RotationService.Compose(CreateRoster(), GetPreset("sanctuary-rudra"), DATA, 3, 1, 0).Runs;

        Assert.Equal(3, runs.Count);
        foreach (PartyResultModel run in runs)
        {
            List<int> numbers = GetMembers(run).Select(member => member.Number).ToList();
            Assert.Equal(numbers.Count, numbers.Distinct().Count());
            Assert.True(numbers.Count <= 10);
        }

        List<CharacterData> seated = runs.SelectMany(GetMembers).ToList();
        Assert.Equal(seated.Count, seated.Distinct().Count());
    }

    private static List<CharacterData> GetMembers(PartyResultModel run)
    {
        return run.Parties.SelectMany(party => party.Members).ToList();
    }

    private static PresetRecord GetPreset(string id)
    {
        return DATA.Presets.Single(record => record.Id == id);
    }

    /// <summary>Five numbers, each a main and one alt, enough tanks and healers to go around.</summary>
    private static List<CharacterData> CreateFivePlayers()
    {
        List<CharacterData> characters = new List<CharacterData>();
        Add(characters, 1, "철벽", "guardian", 36210, 3820, true);
        Add(characters, 1, "보조", "cleric", 28400, 3100, false);
        Add(characters, 2, "그림자", "assassin", 38050, 4010, true);
        Add(characters, 2, "방패", "gladiator", 27100, 3050, false);
        Add(characters, 3, "새벽", "cleric", 32400, 3500, true);
        Add(characters, 3, "마나", "sorcerer", 29800, 3200, false);
        Add(characters, 4, "수호천사", "chanter", 35500, 3700, true);
        Add(characters, 4, "번개", "assassin", 26600, 2950, false);
        Add(characters, 5, "주먹왕", "fighter", 34800, 3650, true);
        Add(characters, 5, "주문", "chanter", 27900, 3000, false);
        return characters;
    }

    /// <summary>Ten numbers, each a main and the given number of alts.</summary>
    private static List<CharacterData> CreateTenPlayers(int[] altCounts)
    {
        List<CharacterData> characters = new List<CharacterData>();
        for (int number = 1; number <= 10; number++)
        {
            Add(characters, number, $"본{number}", CLASSES[number - 1], 35000, 3500, true);
            for (int alt = 0; alt < altCounts[number - 1]; alt++)
            {
                Add(characters, number, $"부{number}-{alt}", CLASSES[(number + 4 + alt) % 10], 28000, 3000, false);
            }
        }

        return characters;
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
