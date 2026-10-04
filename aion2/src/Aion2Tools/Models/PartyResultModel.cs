using System.Collections.Generic;

namespace Aion2Tools.Models;

/// <summary>One composition: the parties, who sat out, its score and the rule checks behind it.</summary>
public class PartyResultModel
{
    public IReadOnlyList<PartyModel> Parties { get; }

    public IReadOnlyList<BenchModel> Bench { get; }

    public double Score { get; }

    /// <summary>One line per rule, starting with ✅ or ⚠.</summary>
    public IReadOnlyList<string> Checks { get; }

    public PartyResultModel(IReadOnlyList<PartyModel> parties, IReadOnlyList<BenchModel> bench, double score, IReadOnlyList<string> checks)
    {
        Parties = parties;
        Bench = bench;
        Score = score;
        Checks = checks;
    }
}
