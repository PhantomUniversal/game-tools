using System.ComponentModel;
using Aion2Tools.Models;
using Aion2Tools.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aion2Tools.ViewModels;

/// <summary>One character as the roster shows it: a line on its group card, and a foldout form in the edit panel.
/// Combat power and item level are edited as text so "-" can mean not entered.</summary>
public partial class CharacterRowViewModel : ViewModelBase
{
    private const string NOT_ENTERED = "-";

    private readonly GameDataTable _data;

    public CharacterData Character { get; }

    public string KindText => Character.IsMain ? "본캐" : "부캐";

    public string DisplayName => Character.Name.Trim().Length > 0 ? Character.Name : "이름 없음";

    public string ClassName
    {
        get
        {
            ClassRecord? recordOrNull = _data.GetClassOrNull(Character.ClassId);
            if (recordOrNull is null)
            {
                return "클래스 없음";
            }

            return recordOrNull.Name;
        }
    }

    /// <summary>Whether the foldout in the edit panel shows its form.</summary>
    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    /// <summary>The setter does not echo back: a box cleared mid-edit must stay empty for the next digit, not turn into "-".</summary>
    public string CombatPowerText
    {
        get => FormatOptional(Character.CombatPower);
        set
        {
            int? parsed;
            if (RosterService.TryParseOptional(value, out parsed))
            {
                Character.CombatPower = parsed;
            }
        }
    }

    public string ItemLevelText
    {
        get => FormatOptional(Character.ItemLevel);
        set
        {
            int? parsed;
            if (RosterService.TryParseOptional(value, out parsed))
            {
                Character.ItemLevel = parsed;
            }
        }
    }

    public CharacterRowViewModel(CharacterData character, GameDataTable data, bool isExpanded)
    {
        Character = character;
        _data = data;
        IsExpanded = isExpanded;
        Character.PropertyChanged += OnCharacterChanged;
    }

    public void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
    }

    /// <summary>Stops listening to the character; called when the roster rebuilds its rows.</summary>
    public void Detach()
    {
        Character.PropertyChanged -= OnCharacterChanged;
    }

    private void OnCharacterChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(ClassName));
    }

    private static string FormatOptional(int? value)
    {
        if (!value.HasValue)
        {
            return NOT_ENTERED;
        }

        return value.Value.ToString();
    }
}
