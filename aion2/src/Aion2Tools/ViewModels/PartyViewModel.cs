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

    public ObservableCollection<PartyResultViewModel> Results { get; } = new ObservableCollection<PartyResultViewModel>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAlternatives))]
    [NotifyPropertyChangedFor(nameof(HasResult))]
    public partial PartyResultViewModel? SelectedResultOrNull { get; set; }

    public bool HasAlternatives => SelectedResultOrNull is not null;

    public ObservableCollection<string> RunHeaders { get; } = new ObservableCollection<string>();

    public ObservableCollection<RotationRowViewModel> Rows { get; } = new ObservableCollection<RotationRowViewModel>();

    public ObservableCollection<PartyResultViewModel> Runs { get; } = new ObservableCollection<PartyResultViewModel>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResult))]
    public partial bool HasRuns { get; set; }

    public bool HasResult => HasAlternatives || HasRuns;

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

    /// <summary>The result as plain text, for pasting into a chat.</summary>
    public string ToText()
    {
        if (!HasRuns)
        {
            return SelectedResultOrNull is null ? string.Empty : SelectedResultOrNull.ToText();
        }

        StringBuilder text = new StringBuilder();
        foreach (PartyResultViewModel run in Runs)
        {
            text.AppendLine($"== {run.Title} ==");
            text.Append(run.ToText());
        }

        return text.ToString();
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
            Results.Add(new PartyResultViewModel($"조합 {index + 1}", results[index], _data));
        }

        SelectedResultOrNull = Results[0];
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
            RunHeaders.Add($"{index + 1}회차");
            Runs.Add(new PartyResultViewModel($"{index + 1}회차", runs[index], _data));
        }

        foreach (IGrouping<int, CharacterData> player in characters.GroupBy(character => character.Number).OrderBy(group => group.Key))
        {
            Rows.Add(CreateRow(player.Key, player.ToList(), runs));
        }

        HasRuns = true;
        int seated = runs.Sum(run => run.Parties.Sum(party => party.Members.Count));
        int mainSeats = runs.Sum(run => run.Parties.Sum(party => party.Members.Count(member => member.IsMain)));
        StatusText = $"{runs.Count}회차 편성 · 출전 {seated}회 (본캐 {mainSeats} · 부캐 {seated - mainSeats})";
    }

    private void ClearResults()
    {
        Results.Clear();
        SelectedResultOrNull = null;
        RunHeaders.Clear();
        Rows.Clear();
        Runs.Clear();
        HasRuns = false;
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
        return new RotationRowViewModel($"#{number}", string.Join(", ", names), cells);
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
            return "클래스 없음";
        }

        return recordOrNull.Name;
    }
}
