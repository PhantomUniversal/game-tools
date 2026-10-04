using System.Collections.Generic;

namespace Aion2Tools.ViewModels;

/// <summary>One player's line of the rotation table: who they bring to each run.</summary>
public class RotationRowViewModel : ViewModelBase
{
    public string Player { get; }

    public string Characters { get; }

    public IReadOnlyList<string> Cells { get; }

    public RotationRowViewModel(string player, string characters, IReadOnlyList<string> cells)
    {
        Player = player;
        Characters = characters;
        Cells = cells;
    }
}
