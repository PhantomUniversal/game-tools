using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Aion2Tools.Models;
using Aion2Tools.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aion2Tools.ViewModels;

/// <summary>Tab 2 (본부 조합): mains and alts spread over several runs, one character per number per run.</summary>
public partial class RotationViewModel : ViewModelBase
{
    private const int SEED = 0;

    private GameDataTable _data;

    public PresetPickerViewModel Preset { get; } = new PresetPickerViewModel();

    [ObservableProperty]
    public partial decimal? RunCount { get; set; } = 3;

    [ObservableProperty]
    public partial bool IsMainFirst { get; set; } = true;

    public ObservableCollection<string> RunHeaders { get; } = new ObservableCollection<string>();

    public ObservableCollection<RotationRowViewModel> Rows { get; } = new ObservableCollection<RotationRowViewModel>();

    public ObservableCollection<PartyResultViewModel> Runs { get; } = new ObservableCollection<PartyResultViewModel>();

    [ObservableProperty]
    public partial string StatusText { get; set; } = "명단의 본캐/부캐를 기준으로 회차별 편성을 만듭니다.";

    public RotationViewModel(GameDataTable data)
    {
        _data = data;
        Preset.SetPresets(data.Presets);
    }

    public void SetData(GameDataTable data)
    {
        _data = data;
        Preset.SetPresets(data.Presets);
    }

    [RelayCommand]
    private void Compose()
    {
        List<CharacterData> characters = SettingsService.Settings.Characters.ToList();
        RunHeaders.Clear();
        Rows.Clear();
        Runs.Clear();
        int runCount = RunCount.HasValue ? (int)RunCount.Value : 1;
        IReadOnlyList<PartyResultModel> runs = RotationService.Compose(characters, Preset.CreateEffective(), _data, runCount, IsMainFirst, SEED);
        if (runs.Count == 0)
        {
            StatusText = "편성할 수 있는 캐릭터가 없습니다.";
            return;
        }

        for (int index = 0; index < runs.Count; index++)
        {
            RunHeaders.Add($"{index + 1}회차");
            Runs.Add(new PartyResultViewModel($"{index + 1}회차", runs[index], _data));
        }

        foreach (IGrouping<int, CharacterData> player in characters.GroupBy(character => character.Number).OrderBy(group => group.Key))
        {
            Rows.Add(CreateRow(player.Key, player.ToList(), runs));
        }

        int seated = runs.Sum(run => run.Parties.Sum(party => party.Members.Count));
        StatusText = $"{runs.Count}회차 편성 · 출전 {seated}회 / 캐릭터 {characters.Count}개";
    }

    private RotationRowViewModel CreateRow(int number, List<CharacterData> characters, IReadOnlyList<PartyResultModel> runs)
    {
        List<string> cells = new List<string>();
        foreach (PartyResultModel run in runs)
        {
            cells.Add(GetCell(characters, run));
        }

        IEnumerable<string> names = characters
            .OrderByDescending(character => character.IsMain)
            .Select(character => GetMainMark(character) + character.Name + "(" + GetClassName(character) + ")");
        return new RotationRowViewModel($"{number}번", string.Join(", ", names), cells);
    }

    /// <summary>The character this player brings to the run, or a dash.</summary>
    private static string GetCell(List<CharacterData> characters, PartyResultModel run)
    {
        foreach (PartyModel party in run.Parties)
        {
            foreach (CharacterData member in party.Members)
            {
                if (characters.Contains(member))
                {
                    return $"{GetMainMark(member)}{member.Name} ({party.Number}파티)";
                }
            }
        }

        return "—";
    }

    private static string GetMainMark(CharacterData character)
    {
        return character.IsMain ? "★" : string.Empty;
    }

    private string GetClassName(CharacterData character)
    {
        ClassRecord? recordOrNull = _data.GetClassOrNull(character.ClassId);
        if (recordOrNull is null)
        {
            return "?";
        }

        return recordOrNull.Name;
    }
}
