using System.Collections.Generic;
using Aion2Tools.Models;
using Aion2Tools.Services;
using Xunit;

namespace Aion2Tools.Tests;

public class RosterServiceTests
{
    private static readonly GameDataTable DATA = GameDataService.LoadBuiltIn();

    [Fact]
    public void Parse_TabAndCommaRows_ReadsBothAndSkipsBadOrOutOfRangeLines()
    {
        string text = "1, 철벽, 수호성, 36210, 3820, Y\n2\t그림자\tassassin\t38050\t4010\tN\n잘못된 줄\n0, 범위밖, 궁성, 30000, 3000, N";

        int skipped;
        List<CharacterData> parsed = RosterService.Parse(text, DATA, out skipped);

        Assert.Equal(2, parsed.Count);
        Assert.Equal("guardian", parsed[0].ClassId);
        Assert.True(parsed[0].IsMain);
        Assert.Equal("assassin", parsed[1].ClassId);
        Assert.Equal(2, parsed[1].Number);
        Assert.Equal(2, skipped);
    }

    [Fact]
    public void Format_ThenParse_RoundTripsWithTheHeaderSkipped()
    {
        int skipped;
        List<CharacterData> original = RosterService.Parse("2, 부캐, 치유성, -, 3700, N\n2, 본캐, 수호성, 36210, -, Y", DATA, out skipped);

        string text = RosterService.Format(original, DATA);
        List<CharacterData> parsed = RosterService.Parse(text, DATA, out skipped);

        Assert.StartsWith(RosterService.HEADER, text);
        Assert.Equal(0, skipped);
        Assert.Equal(2, parsed.Count);
        Assert.Equal("본캐", parsed[0].Name);
        Assert.True(parsed[0].IsMain);
        Assert.Null(parsed[0].ItemLevel);
        Assert.Equal("cleric", parsed[1].ClassId);
        Assert.Null(parsed[1].CombatPower);
    }

    [Fact]
    public void Parse_DashOrBlankValues_AreNotEntered()
    {
        int skipped;
        List<CharacterData> parsed = RosterService.Parse("3, 신규, 궁성, -, , N", DATA, out skipped);

        CharacterData character = Assert.Single(parsed);
        Assert.Null(character.CombatPower);
        Assert.Null(character.ItemLevel);
        Assert.Equal(0, skipped);
    }

    [Fact]
    public void NormalizeMains_NoneOrTwoMarked_LeavesExactlyOneMainPerNumber()
    {
        List<CharacterData> characters = new List<CharacterData>
        {
            Create(1, false),
            Create(1, false),
            Create(2, true),
            Create(2, true),
        };

        RosterService.NormalizeMains(characters);

        Assert.True(characters[0].IsMain);
        Assert.False(characters[1].IsMain);
        Assert.True(characters[2].IsMain);
        Assert.False(characters[3].IsMain);
    }

    private static CharacterData Create(int number, bool isMain)
    {
        CharacterData character = new CharacterData();
        character.Number = number;
        character.IsMain = isMain;
        return character;
    }
}
