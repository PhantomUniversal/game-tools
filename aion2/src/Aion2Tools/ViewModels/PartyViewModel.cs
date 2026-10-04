using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Aion2Tools.Models;
using Aion2Tools.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aion2Tools.ViewModels;

/// <summary>Tab 1: the checked roster characters split into parties, with up to three alternatives.</summary>
public partial class PartyViewModel : ViewModelBase
{
    private const int ALTERNATIVE_COUNT = 3;
    private const int SEED = 0;

    private GameDataTable _data;

    public PresetPickerViewModel Preset { get; } = new PresetPickerViewModel();

    public ObservableCollection<PartyResultViewModel> Results { get; } = new ObservableCollection<PartyResultViewModel>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResult))]
    public partial PartyResultViewModel? SelectedResultOrNull { get; set; }

    public bool HasResult => SelectedResultOrNull is not null;

    [ObservableProperty]
    public partial string StatusText { get; set; } = "명단 탭에서 참가할 캐릭터를 체크한 뒤 [자동 조합]을 누르세요.";

    public PartyViewModel(GameDataTable data)
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
        List<CharacterData> selected = SettingsService.Settings.Characters.Where(character => character.IsSelected).ToList();
        Results.Clear();
        SelectedResultOrNull = null;
        if (selected.Count == 0)
        {
            StatusText = "참가 체크된 캐릭터가 없습니다.";
            return;
        }

        PresetRecord preset = Preset.CreateEffective();
        IReadOnlyList<PartyResultModel> results = PartyService.Compose(selected, preset, _data, ALTERNATIVE_COUNT, SEED);
        if (results.Count == 0)
        {
            StatusText = $"참가 {selected.Count}명 모두 컷 미달이거나 클래스가 없습니다.";
            return;
        }

        for (int index = 0; index < results.Count; index++)
        {
            Results.Add(new PartyResultViewModel($"조합 {index + 1}", results[index], _data));
        }

        SelectedResultOrNull = Results[0];
        StatusText = $"참가 {selected.Count}명 · {preset.Name} · 대안 {results.Count}개";
    }
}
