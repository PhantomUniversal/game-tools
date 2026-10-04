using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aion2Tools.ViewModels;

/// <summary>One numbered group: the person's main, which is always there, and their alts.</summary>
public partial class PlayerGroupViewModel : ViewModelBase
{
    public int Number { get; }

    public string Title => $"{Number}번";

    public string CountText => $"캐릭터 {1 + Alts.Count}";

    public CharacterRowViewModel Main { get; }

    public IReadOnlyList<CharacterRowViewModel> Alts { get; }

    public bool HasAlts => Alts.Count > 0;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public PlayerGroupViewModel(int number, CharacterRowViewModel main, IReadOnlyList<CharacterRowViewModel> alts)
    {
        Number = number;
        Main = main;
        Alts = alts;
    }
}
