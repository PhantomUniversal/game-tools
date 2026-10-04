using System.Collections.Generic;

namespace Aion2Tools.Models;

/// <summary>A rotation: each run's composition, and the mains in the order they take their turns.</summary>
public class RotationResult
{
    public IReadOnlyList<PartyResultModel> Runs { get; }

    /// <summary>One entry per run a turn starts in; more than one main when mains play side by side.</summary>
    public IReadOnlyList<IReadOnlyList<MainTurnModel>> MainTurns { get; }

    public RotationResult(IReadOnlyList<PartyResultModel> runs, IReadOnlyList<IReadOnlyList<MainTurnModel>> mainTurns)
    {
        Runs = runs;
        MainTurns = mainTurns;
    }
}
