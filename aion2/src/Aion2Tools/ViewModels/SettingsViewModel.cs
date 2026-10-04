using System;
using System.Threading.Tasks;
using Aion2Tools.Models;
using Aion2Tools.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aion2Tools.ViewModels;

/// <summary>App version and update, and the game data source and its update.</summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly Action<GameDataTable> _applyData;

    public UpdateViewModel Update { get; }

    public string SettingsFolder => AppPaths.SettingsFolder;

    [ObservableProperty]
    public partial string DataVersionText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DataStatusText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsCheckingData { get; set; }

    public string GameDataUrl
    {
        get => SettingsService.Settings.GameDataUrl;
        set
        {
            SettingsService.Settings.GameDataUrl = value;
            SettingsService.ScheduleSave();
            OnPropertyChanged();
        }
    }

    public bool IsAutoCheckOn
    {
        get => SettingsService.Settings.IsAutoCheckOn;
        set
        {
            SettingsService.Settings.IsAutoCheckOn = value;
            SettingsService.ScheduleSave();
            OnPropertyChanged();
        }
    }

    public SettingsViewModel(UpdateViewModel update, GameDataTable data, Action<GameDataTable> applyData)
    {
        Update = update;
        _applyData = applyData;
        ShowDataVersion(data);
    }

    public void ShowDataVersion(GameDataTable data)
    {
        DataVersionText = $"v{data.Version} · 클래스 {data.Classes.Count}종 · 프리셋 {data.Presets.Count}개";
    }

    /// <summary>Downloads the game data and applies it when its version is higher than the one in use.</summary>
    [RelayCommand]
    public async Task CheckData()
    {
        IsCheckingData = true;
        DataStatusText = "확인 중…";
        string? jsonOrNull = await GameDataService.DownloadOrNull(GameDataUrl);
        IsCheckingData = false;
        if (jsonOrNull is null)
        {
            DataStatusText = $"확인 실패 (네트워크 또는 주소) · {DateTime.Now:HH:mm}";
            return;
        }

        GameDataTable? remoteOrNull = GameDataService.ParseOrNull(jsonOrNull);
        if (remoteOrNull is null)
        {
            DataStatusText = "받은 데이터 형식이 올바르지 않습니다.";
            return;
        }

        GameDataTable current = GameDataService.Load();
        if (remoteOrNull.Version <= current.Version)
        {
            DataStatusText = $"최신입니다 (원격 v{remoteOrNull.Version}) · {DateTime.Now:HH:mm}";
            return;
        }

        GameDataService.SaveDownloaded(jsonOrNull);
        _applyData(remoteOrNull);
        DataStatusText = $"v{current.Version} → v{remoteOrNull.Version} 적용됨";
    }
}
