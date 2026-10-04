using System.Collections.Generic;
using Aion2Tools.Models;
using Aion2Tools.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aion2Tools.ViewModels;

/// <summary>The preset box and its two cuts on the party page.</summary>
public partial class PresetPickerViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial IReadOnlyList<PresetRecord> Presets { get; set; } = new List<PresetRecord>();

    [ObservableProperty]
    public partial PresetRecord? SelectedPresetOrNull { get; set; }

    [ObservableProperty]
    public partial decimal? MinItemLevel { get; set; }

    [ObservableProperty]
    public partial decimal? MinCombatPower { get; set; }

    /// <summary>Keeps the chosen preset by id across a game data update.</summary>
    public void SetPresets(IReadOnlyList<PresetRecord> presets)
    {
        string id = SettingsService.Settings.PresetId;
        Presets = presets;
        PresetRecord? matchOrNull = null;
        foreach (PresetRecord preset in presets)
        {
            if (preset.Id == id)
            {
                matchOrNull = preset;
            }
        }

        SelectedPresetOrNull = matchOrNull is null ? presets[0] : matchOrNull;
    }

    /// <summary>The selected preset with the cuts as typed.</summary>
    public PresetRecord CreateEffective()
    {
        PresetRecord source = SelectedPresetOrNull!;
        PresetRecord effective = new PresetRecord();
        effective.Id = source.Id;
        effective.Name = source.Name;
        effective.PartyCount = source.PartyCount;
        effective.PartySize = source.PartySize;
        effective.MinItemLevel = MinItemLevel.HasValue ? (int)MinItemLevel.Value : 0;
        effective.MinCombatPower = MinCombatPower.HasValue ? (int)MinCombatPower.Value : 0;
        effective.EntryLimit = source.EntryLimit;
        return effective;
    }

    partial void OnSelectedPresetOrNullChanged(PresetRecord? value)
    {
        if (value is null)
        {
            return;
        }

        MinItemLevel = value.MinItemLevel;
        MinCombatPower = value.MinCombatPower;
        SettingsService.Settings.PresetId = value.Id;
        SettingsService.ScheduleSave();
    }
}
