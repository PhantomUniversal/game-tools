using System.Collections.Generic;
using System.Collections.ObjectModel;
using Aion2Tools.Models;
using Aion2Tools.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aion2Tools.ViewModels;

/// <summary>The character list both composition tabs read from. Saved on every edit.</summary>
public partial class RosterViewModel : ViewModelBase
{
    private GameDataTable _data;

    public ObservableCollection<CharacterData> Characters => SettingsService.Settings.Characters;

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
    }

    public void SetData(GameDataTable data)
    {
        _data = data;
        Classes = data.Classes;
    }

    [RelayCommand]
    private void Add()
    {
        CharacterData character = new CharacterData();
        character.ClassId = _data.Classes[0].Id;
        Characters.Add(character);
    }

    [RelayCommand]
    private void Remove(CharacterData character)
    {
        Characters.Remove(character);
    }

    [RelayCommand]
    private void LoadSample()
    {
        Characters.Clear();
        foreach (CharacterData character in RosterService.CreateSample())
        {
            Characters.Add(character);
        }

        StatusText = $"샘플 {Characters.Count}명을 불러왔습니다.";
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
        StatusText = "명단을 비웠습니다.";
    }

    private void SetSelected(bool isSelected)
    {
        foreach (CharacterData character in Characters)
        {
            character.IsSelected = isSelected;
        }
    }
}
