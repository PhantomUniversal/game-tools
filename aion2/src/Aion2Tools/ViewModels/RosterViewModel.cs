using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Aion2Tools.Models;
using Aion2Tools.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aion2Tools.ViewModels;

/// <summary>The roster as numbered group cards, with the selected group edited in the side panel.
/// The saved list stays flat (number + main flag); the groups are rebuilt from it after each change.</summary>
public partial class RosterViewModel : ViewModelBase
{
    private GameDataTable _data;

    private ObservableCollection<CharacterData> Characters => SettingsService.Settings.Characters;

    public ObservableCollection<PlayerGroupViewModel> Groups { get; } = new ObservableCollection<PlayerGroupViewModel>();

    /// <summary>What the card wall shows: the groups, then the add tile while there is room for another.</summary>
    public ObservableCollection<ViewModelBase> Tiles { get; } = new ObservableCollection<ViewModelBase>();

    public bool CanAddGroup => Groups.Count < CharacterData.MAX_NUMBER;

    public string CountText => $"그룹 {Groups.Count}/{CharacterData.MAX_NUMBER} · 캐릭터 {Characters.Count}";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    public partial PlayerGroupViewModel? SelectedGroupOrNull { get; set; }

    public bool HasSelection => SelectedGroupOrNull is not null;

    [ObservableProperty]
    public partial IReadOnlyList<ClassRecord> Classes { get; set; }

    [ObservableProperty]
    public partial string ImportText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusText { get; set; } = string.Empty;

    public RosterViewModel(GameDataTable data)
    {
        _data = data;
        Classes = data.Classes;
        RosterService.NormalizeMains(Characters);
        RebuildGroups(0);
    }

    public void SetData(GameDataTable data)
    {
        _data = data;
        Classes = data.Classes;
        RebuildGroups(SelectedNumber());
    }

    /// <summary>Tapping the selected card again closes the panel.</summary>
    public void SelectGroup(PlayerGroupViewModel group)
    {
        SelectedGroupOrNull = group == SelectedGroupOrNull ? null : group;
    }

    public void RemoveAlt(CharacterRowViewModel row)
    {
        Characters.Remove(row.Character);
        RebuildGroups(SelectedNumber());
    }

    /// <summary>A new group takes the lowest free number, starts with its main, and opens in the panel.</summary>
    [RelayCommand]
    private void AddGroup()
    {
        if (!CanAddGroup)
        {
            return;
        }

        HashSet<int> used = Groups.Select(group => group.Number).ToHashSet();
        int number = Enumerable.Range(CharacterData.MIN_NUMBER, CharacterData.MAX_NUMBER).First(candidate => !used.Contains(candidate));
        Characters.Add(CreateCharacter(number, true));
        RebuildGroups(number);
    }

    [RelayCommand]
    private void RemoveGroup()
    {
        if (SelectedGroupOrNull is null)
        {
            return;
        }

        int number = SelectedGroupOrNull.Number;
        foreach (CharacterData character in Characters.Where(character => character.Number == number).ToList())
        {
            Characters.Remove(character);
        }

        RebuildGroups(0);
    }

    [RelayCommand]
    private void AddAlt()
    {
        if (SelectedGroupOrNull is null)
        {
            return;
        }

        int number = SelectedGroupOrNull.Number;
        Characters.Add(CreateCharacter(number, false));
        RebuildGroups(number);
    }

    [RelayCommand]
    private void ClosePanel()
    {
        SelectedGroupOrNull = null;
    }

    [RelayCommand]
    private void Import()
    {
        int skipped;
        List<CharacterData> parsed = RosterService.Parse(ImportText, _data, out skipped);
        foreach (CharacterData character in parsed)
        {
            Characters.Add(character);
        }

        RosterService.NormalizeMains(Characters);
        RebuildGroups(SelectedNumber());
        StatusText = skipped > 0 ? $"{parsed.Count}명 추가 · {skipped}줄은 형식이 맞지 않아 건너뜀" : $"{parsed.Count}명 추가";
        if (skipped == 0)
        {
            ImportText = string.Empty;
        }
    }

    [RelayCommand]
    private void SelectAll()
    {
        SetSelected(true);
    }

    [RelayCommand]
    private void SelectNone()
    {
        SetSelected(false);
    }

    [RelayCommand]
    private void Clear()
    {
        Characters.Clear();
        RebuildGroups(0);
        StatusText = "명단을 비웠습니다.";
    }

    partial void OnSelectedGroupOrNullChanged(PlayerGroupViewModel? oldValue, PlayerGroupViewModel? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.IsSelected = false;
        }

        if (newValue is not null)
        {
            newValue.IsSelected = true;
        }
    }

    private int SelectedNumber()
    {
        if (SelectedGroupOrNull is null)
        {
            return 0;
        }

        return SelectedGroupOrNull.Number;
    }

    private CharacterData CreateCharacter(int number, bool isMain)
    {
        CharacterData character = new CharacterData();
        character.Number = number;
        character.IsMain = isMain;
        character.ClassId = _data.Classes[0].Id;
        return character;
    }

    /// <summary>Recreates the cards from the flat list and reselects the group with this number (0 = none).</summary>
    private void RebuildGroups(int selectNumber)
    {
        foreach (PlayerGroupViewModel group in Groups)
        {
            group.Main.Detach();
            foreach (CharacterRowViewModel alt in group.Alts)
            {
                alt.Detach();
            }
        }

        SelectedGroupOrNull = null;
        Groups.Clear();
        foreach (IGrouping<int, CharacterData> group in Characters.GroupBy(character => character.Number).OrderBy(group => group.Key))
        {
            CharacterRowViewModel main = new CharacterRowViewModel(group.First(character => character.IsMain), _data);
            List<CharacterRowViewModel> alts = group.Where(character => !character.IsMain).Select(character => new CharacterRowViewModel(character, _data)).ToList();
            Groups.Add(new PlayerGroupViewModel(group.Key, main, alts));
        }

        Tiles.Clear();
        foreach (PlayerGroupViewModel group in Groups)
        {
            Tiles.Add(group);
        }

        if (CanAddGroup)
        {
            Tiles.Add(new AddGroupTileViewModel());
        }

        SelectedGroupOrNull = Groups.FirstOrDefault(group => group.Number == selectNumber);
        OnPropertyChanged(nameof(CanAddGroup));
        OnPropertyChanged(nameof(CountText));
    }

    private void SetSelected(bool isSelected)
    {
        foreach (CharacterData character in Characters)
        {
            character.IsSelected = isSelected;
        }
    }
}
