using System.Collections.Generic;
using Aion2Tools.Models;
using Aion2Tools.Services;
using Xunit;

namespace Aion2Tools.Tests;

public class RosterServiceTests
{
    private static readonly GameDataTable DATA = GameDataService.LoadBuiltIn();

    [Fact]
    public void Parse_TabAndCommaRows_ReadsBothAndCountsBadLines()
    {
        string text = "A, 철벽, 수호성, 36210, 3820, Y\nB\t그림자\tassassin\t38050\t4010\tN\n잘못된 줄";

        int skipped;
        List<CharacterData> parsed = RosterService.Parse(text, DATA, out skipped);

        Assert.Equal(2, parsed.Count);
        Assert.Equal("guardian", parsed[0].ClassId);
        Assert.True(parsed[0].IsMain);
        Assert.Equal("assassin", parsed[1].ClassId);
        Assert.Equal(1, skipped);
    }
}
