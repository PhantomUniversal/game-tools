using System.Collections.Generic;

namespace Aion2Tools.ViewModels;

/// <summary>One number's line of the rotation table: which character it brings to each run.</summary>
public class RotationRowViewModel : ViewModelBase
{
    public string Number { get; }

    public string Characters { get; }

    public IReadOnlyList<string> Cells { get; }

    public RotationRowViewModel(string number, string characters, IReadOnlyList<string> cells)
    {
        Number = number;
        Characters = characters;
        Cells = cells;
    }
}
