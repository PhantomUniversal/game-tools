using System.Collections.Generic;

namespace Aion2Tools.ViewModels;

/// <summary>One numbered box on the roster page: the person's main, which is always there, and their alts.</summary>
public class PlayerGroupViewModel : ViewModelBase
{
    public int Number { get; }

    public string Title => $"{Number}번";

    public CharacterRowViewModel Main { get; }

    public IReadOnlyList<CharacterRowViewModel> Alts { get; }

    public PlayerGroupViewModel(int number, CharacterRowViewModel main, IReadOnlyList<CharacterRowViewModel> alts)
    {
        Number = number;
        Main = main;
        Alts = alts;
    }
}
