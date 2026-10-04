using System.Collections.Generic;

namespace Aion2Tools.Models;

/// <summary>One main's turn in a rotation: the runs it plays, and whether they are the planned back-to-back block.</summary>
public class MainTurnModel
{
    public CharacterData Main { get; }

    /// <summary>1-based run numbers.</summary>
    public IReadOnlyList<int> Runs { get; }

    public bool IsBlock { get; }

    public MainTurnModel(CharacterData main, IReadOnlyList<int> runs, bool isBlock)
    {
        Main = main;
        Runs = runs;
        IsBlock = isBlock;
    }
}
