using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aion2Tools.Models;
using Aion2Tools.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aion2Tools.ViewModels;

/// <summary>The party tab: pick a profile and the member cards that join, then compose.
/// One run gives up to three alternative compositions; more runs give a main/alt rotation where each main plays a set number of runs.</summary>
public partial class PartyViewModel : ViewModelBase
{
    private const int ALTERNATIVE_COUNT = 3;
    private const int SEED = 0;

    /// <summary>Numbers left out per profile, so a choice survives switching tabs or profiles.</summary>
    private readonly Dictionary<ProfileData, HashSet<int>> _excluded = new Dictionary<ProfileData, HashSet<int>>();

    private GameDataTable _data;

    public PresetPickerViewModel Preset { get; } = new PresetPickerViewModel();

    [ObservableProperty]
    public partial IReadOnlyList<ProfileData> Profiles { get; set; } = new List<ProfileData>();

    [ObservableProperty]
    public partial ProfileData? SelectedProfileOrNull { get; set; }

    public ObservableCollection<PartyMemberViewModel> Members { get; } = new ObservableCollection<PartyMemberViewModel>();

    public string SelectionText => $"{Members.Count(member => member.IsSelected)} / {Members.Count}명 선택";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRotation))]
    public partial decimal? RunCount { get; set; } = 1;

    public bool IsRotation => GetRunCount() > 1;

    /// <summary>How many runs each main plays in a rotation; their alts fill the rest.</summary>
    [ObservableProperty]
    public partial decimal? MainRunCount { get; set; } = 2;

    /// <summary>The alternatives of one run, or the runs of a rotation, one row each.</summary>
    public ObservableCollection<ResultRowViewModel> ResultRows { get; } = new ObservableCollection<ResultRowViewModel>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedRow))]
    public partial ResultRowViewModel? SelectedRowOrNull { get; set; }

    public bool HasSelectedRow => SelectedRowOrNull is not null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResult))]
    public partial bool IsRotationResult { get; set; }

    public bool HasResult => ResultRows.Count > 0;

    [ObservableProperty]
    public partial string StatusText { get; set; } = "프로필과 참가할 카드를 고른 뒤 자동 조합을 누르세요.";

    public PartyViewModel(GameDataTable data)
    {
        _data = data;
        Preset.SetPresets(data.Presets);
        Refresh();
    }

    public void SetData(GameDataTable data)
    {
        _data = data;
        Preset.SetPresets(data.Presets);
        RebuildMembers();
    }

    /// <summary>Picks up profile and roster edits made on the profile tab.</summary>
    public void Refresh()
    {
        ProfileData? keepOrNull = SelectedProfileOrNull;
        Profiles = SettingsService.Settings.Profiles.ToList();
        if (keepOrNull is null || !Profiles.Contains(keepOrNull))
        {
            keepOrNull = Profiles.FirstOrDefault();
        }

        if (keepOrNull == SelectedProfileOrNull)
        {
            RebuildMembers();
            return;
        }

        SelectedProfileOrNull = keepOrNull;
    }

    public void ToggleMember(PartyMemberViewModel member)
    {
        SetSelected(member, !member.IsSelected);
        OnPropertyChanged(nameof(SelectionText));
    }

    /// <summary>Tapping the selected row again closes the side panel.</summary>
    public void SelectRow(ResultRowViewModel row)
    {
        SelectedRowOrNull = row == SelectedRowOrNull ? null : row;
    }

    /// <summary>The result as plain text, for pasting into a chat: every run of a rotation, or the alternative in view.</summary>
    public string ToText()
    {
        if (!IsRotationResult)
        {
            ResultRowViewModel? rowOrNull = SelectedRowOrNull is null ? ResultRows.FirstOrDefault() : SelectedRowOrNull;
            return rowOrNull is null ? string.Empty : rowOrNull.Result.ToText();
        }

        StringBuilder text = new StringBuilder();
        foreach (ResultRowViewModel row in ResultRows)
        {
            text.AppendLine($"== {row.Title} ==");
            text.Append(row.Result.ToText());
        }

        return text.ToString();
    }

    [RelayCommand]
    private void CloseDetail()
    {
        SelectedRowOrNull = null;
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (PartyMemberViewModel member in Members)
        {
            SetSelected(member, true);
        }

        OnPropertyChanged(nameof(SelectionText));
    }

    [RelayCommand]
    private void SelectNone()
    {
        foreach (PartyMemberViewModel member in Members)
        {
            SetSelected(member, false);
        }

        OnPropertyChanged(nameof(SelectionText));
    }

    [RelayCommand]
    private async Task Compose()
    {
        ClearResults();
        if (SelectedProfileOrNull is null)
        {
            StatusText = "프로필이 없습니다. 프로필 탭에서 먼저 만드세요.";
            return;
        }

        HashSet<int> numbers = Members.Where(member => member.IsSelected).Select(member => member.Number).ToHashSet();
        List<CharacterData> characters = SelectedProfileOrNull.Characters.Where(character => numbers.Contains(character.Number)).ToList();
        if (characters.Count == 0)
        {
            StatusText = "참가할 카드를 선택하세요.";
            return;
        }

        PresetRecord preset = Preset.CreateEffective();
        if (IsRotation)
        {
            await ComposeRotation(characters, preset);
            return;
        }

        IReadOnlyList<PartyResultModel> results = PartyService.Compose(characters, preset, _data, ALTERNATIVE_COUNT, SEED);
        if (results.Count == 0)
        {
            StatusText = $"{characters.Count}명 모두 컷 미달이거나 클래스가 없습니다.";
            return;
        }

        for (int index = 0; index < results.Count; index++)
        {
            ResultRows.Add(new ResultRowViewModel(new PartyResultViewModel($"조합 {index + 1}", results[index], _data)));
        }

        ShowResult(false);
        StatusText = $"{numbers.Count}명 · 캐릭터 {characters.Count}개 · {preset.Name} · 대안 {results.Count}개";
    }

    partial void OnSelectedProfileOrNullChanged(ProfileData? value)
    {
        RebuildMembers();
        ClearResults();
    }

    /// <summary>Off the UI thread: the rotation search can take a few seconds on a large roster.</summary>
    private async Task ComposeRotation(List<CharacterData> characters, PresetRecord preset)
    {
        int runCount = GetRunCount();
        int mainRunCount = MainRunCount.HasValue ? (int)MainRunCount.Value : 0;
        GameDataTable data = _data;
        StatusText = "로테이션 계산 중…";
        IReadOnlyList<PartyResultModel> runs = await Task.Run(() => RotationService.Compose(characters, preset, data, runCount, mainRunCount, SEED));
        if (runs.Count == 0)
        {
            StatusText = "편성할 수 있는 캐릭터가 없습니다.";
            return;
        }

        for (int index = 0; index < runs.Count; index++)
        {
            ResultRows.Add(new ResultRowViewModel(new PartyResultViewModel($"{index + 1}회차", runs[index], _data)));
        }

        ShowResult(true);
        int seated = runs.Sum(run => run.Parties.Sum(party => party.Members.Count));
        int mainSeats = runs.Sum(run => run.Parties.Sum(party => party.Members.Count(member => member.IsMain)));
        StatusText = $"{runs.Count}회차 편성 · 출전 {seated}회 (본캐 {mainSeats} · 부캐 {seated - mainSeats})";
    }

    private void ShowResult(bool isRotation)
    {
        IsRotationResult = isRotation;
        OnPropertyChanged(nameof(HasResult));
        SelectedRowOrNull = ResultRows[0];
    }

    private void ClearResults()
    {
        SelectedRowOrNull = null;
        ResultRows.Clear();
        IsRotationResult = false;
        OnPropertyChanged(nameof(HasResult));
    }

    partial void OnSelectedRowOrNullChanged(ResultRowViewModel? oldValue, ResultRowViewModel? newValue)
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

    private void RebuildMembers()
    {
        Members.Clear();
        if (SelectedProfileOrNull is not null)
        {
            HashSet<int> excluded = GetExcluded(SelectedProfileOrNull);
            foreach (IGrouping<int, CharacterData> group in SelectedProfileOrNull.Characters.GroupBy(character => character.Number).OrderBy(group => group.Key))
            {
                CharacterData main = group.First(character => character.IsMain);
                Members.Add(new PartyMemberViewModel(group.Key, main, GetClassName(main), group.Count() - 1, !excluded.Contains(group.Key)));
            }
        }

        OnPropertyChanged(nameof(SelectionText));
    }

    private void SetSelected(PartyMemberViewModel member, bool isSelected)
    {
        member.IsSelected = isSelected;
        if (SelectedProfileOrNull is null)
        {
            return;
        }

        HashSet<int> excluded = GetExcluded(SelectedProfileOrNull);
        if (isSelected)
        {
            excluded.Remove(member.Number);
        }
        else
        {
            excluded.Add(member.Number);
        }
    }

    private HashSet<int> GetExcluded(ProfileData profile)
    {
        HashSet<int>? excludedOrNull;
        if (!_excluded.TryGetValue(profile, out excludedOrNull))
        {
            excludedOrNull = new HashSet<int>();
            _excluded[profile] = excludedOrNull;
        }

        return excludedOrNull;
    }

    private int GetRunCount()
    {
        return RunCount.HasValue ? (int)RunCount.Value : 1;
    }

    private string GetClassName(CharacterData character)
    {
        ClassRecord? recordOrNull = _data.GetClassOrNull(character.ClassId);
        if (recordOrNull is null)
        {
            return "클래스 없음";
        }

        return recordOrNull.Name;
    }
}
