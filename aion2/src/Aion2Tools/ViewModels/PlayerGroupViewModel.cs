using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aion2Tools.ViewModels;

/// <summary>One numbered group: the person's main, which is always there and names the card, and their alts.</summary>
public partial class PlayerGroupViewModel : ViewModelBase
{
    public int Number { get; }

    public string NumberText => $"#{Number}";

    public CharacterRowViewModel Main { get; }

    public IReadOnlyList<CharacterRowViewModel> Alts { get; }

    public bool HasAlts => Alts.Count > 0;

    /// <summary>The main then the alts, as the edit panel lists them.</summary>
    public IReadOnlyList<CharacterRowViewModel> Rows { get; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public PlayerGroupViewModel(int number, CharacterRowViewModel main, IReadOnlyList<CharacterRowViewModel> alts)
    {
        Number = number;
        Main = main;
        Alts = alts;
        Rows = alts.Prepend(main).ToList();
    }
}
