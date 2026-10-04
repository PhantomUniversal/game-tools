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
        IReadOnlyList<PartyResultModel> runs = ComposeSample(true);

        List<CharacterData> firstRun = runs[0].Parties.SelectMany(party => party.Members).ToList();
        Assert.All(firstRun, member => Assert.True(member.IsMain));
    }

    [Fact]
    public void Compose_ThreeRuns_UsesNoCharacterTwiceAndOnePerPlayerPerRun()
    {
        IReadOnlyList<PartyResultModel> runs = ComposeSample(false);

        List<CharacterData> all = runs.SelectMany(run => run.Parties.SelectMany(party => party.Members)).ToList();
        Assert.Equal(all.Count, all.Distinct().Count());
        foreach (PartyResultModel run in runs)
        {
            List<string> players = run.Parties.SelectMany(party => party.Members).Select(member => member.Player).ToList();
            Assert.Equal(players.Count, players.Distinct().Count());
        }
    }

    private static IReadOnlyList<PartyResultModel> ComposeSample(bool isMainFirst)
    {
        PresetRecord preset = DATA.Presets.Single(record => record.Id == "sanctuary-rudra");
        return RotationService.Compose(RosterService.CreateSample(), preset, DATA, 3, isMainFirst, 0);
    }
}
