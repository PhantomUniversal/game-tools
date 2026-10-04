using Aion2Tools.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aion2Tools.ViewModels;

/// <summary>One member card on the party tab: their main, how many alts they bring, and whether they join this composition.</summary>
public partial class PartyMemberViewModel : ViewModelBase
{
    public int Number { get; }

    public string NumberText => $"#{Number}";

    public string Name { get; }

    public string ClassId { get; }

    public string DetailText { get; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public PartyMemberViewModel(int number, CharacterData main, string className, int altCount, bool isSelected)
    {
        Number = number;
        Name = main.Name.Trim().Length > 0 ? main.Name : "이름 없음";
        ClassId = main.ClassId;
        DetailText = altCount > 0 ? $"{className} · 부캐 {altCount}" : className;
        IsSelected = isSelected;
    }
}
