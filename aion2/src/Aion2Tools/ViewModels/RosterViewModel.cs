using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Aion2Tools.Models;
using Aion2Tools.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aion2Tools.ViewModels;

/// <summary>The roster as numbered groups. The saved list stays flat (number + main flag); the groups are rebuilt from it after each change.</summary>
public partial class RosterViewModel : ViewModelBase
{
    private GameDataTable _data;

    private ObservableCollection<CharacterData> Characters => SettingsService.Settings.Characters;

    public ObservableCollection<PlayerGroupViewModel> Groups { get; } = new ObservableCollection<PlayerGroupViewModel>();

    public bool CanAddGroup => Groups.Count < CharacterData.MAX_NUMBER;

    public string AddGroupText => $"+  그룹 추가 ({Groups.Count}/{CharacterData.MAX_NUMBER})";

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
        RebuildGroups();
    }

    public void SetData(GameDataTable data)
    {
        _data = data;
        Classes = data.Classes;
    }

    /// <summary>A new group takes the lowest free number and starts with its main.</summary>
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
        RebuildGroups();
    }

    [RelayCommand]
    private void RemoveGroup(PlayerGroupViewModel group)
    {
        foreach (CharacterData character in Characters.Where(character => character.Number == group.Number).ToList())
        {
            Characters.Remove(character);
        }

        RebuildGroups();
    }

    [RelayCommand]
    private void AddAlt(PlayerGroupViewModel group)
    {
        Characters.Add(CreateCharacter(group.Number, false));
        RebuildGroups();
    }

    [RelayCommand]
    private void RemoveAlt(CharacterRowViewModel row)
    {
        Characters.Remove(row.Character);
        RebuildGroups();
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
        RebuildGroups();
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
        RebuildGroups();
        StatusText = "명단을 비웠습니다.";
    }

    private CharacterData CreateCharacter(int number, bool isMain)
    {
        CharacterData character = new CharacterData();
        character.Number = number;
        character.IsMain = isMain;
        character.ClassId = _data.Classes[0].Id;
        return character;
    }

    private void RebuildGroups()
    {
        Groups.Clear();
        foreach (IGrouping<int, CharacterData> group in Characters.GroupBy(character => character.Number).OrderBy(group => group.Key))
        {
            CharacterRowViewModel main = new CharacterRowViewModel(group.First(character => character.IsMain));
            List<CharacterRowViewModel> alts = group.Where(character => !character.IsMain).Select(character => new CharacterRowViewModel(character)).ToList();
            Groups.Add(new PlayerGroupViewModel(group.Key, main, alts));
        }

        OnPropertyChanged(nameof(CanAddGroup));
        OnPropertyChanged(nameof(AddGroupText));
    }

    private void SetSelected(bool isSelected)
    {
        foreach (CharacterData character in Characters)
        {
            character.IsSelected = isSelected;
        }
    }
}
