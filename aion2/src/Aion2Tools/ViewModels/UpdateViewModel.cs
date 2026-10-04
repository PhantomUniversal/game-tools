using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aion2Tools.Models;
using Aion2Tools.Services;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aion2Tools.ViewModels;

/// <summary>The app's own update, shared by the corner notice and the settings page.</summary>
public partial class UpdateViewModel : ViewModelBase
{
    private const int SHORT_COMMIT_LENGTH = 7;

    private static readonly TimeSpan CHECK_LIMIT = TimeSpan.FromMinutes(1);

    public bool IsAvailable => SelfUpdate.IsAvailable;

    public string VersionText
    {
        get
        {
            if (SelfUpdate.BuiltCommit.Length < SHORT_COMMIT_LENGTH)
            {
                return "개발 빌드 (자동 업데이트 없음)";
            }

            string? pathOrNull = Environment.ProcessPath;
            string commit = SelfUpdate.BuiltCommit.Substring(0, SHORT_COMMIT_LENGTH);
            if (pathOrNull is null)
            {
                return commit;
            }

            return commit + " · " + File.GetLastWriteTime(pathOrNull).ToString("yyyy.MM.dd HH:mm");
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBuilding))]
    [NotifyPropertyChangedFor(nameof(IsReady))]
    [NotifyPropertyChangedFor(nameof(CanStart))]
    public partial UpdateState State { get; set; }

    public bool IsBuilding => State == UpdateState.Building;

    public bool IsReady => State == UpdateState.Ready;

    public bool CanStart => IsAvailable && State is not (UpdateState.Building or UpdateState.Ready);

    [ObservableProperty]
    public partial bool IsNoticeOpen { get; set; }

    [ObservableProperty]
    public partial string StatusText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LastLine { get; set; } = string.Empty;

    /// <summary>Asks the remote once, quietly. Says nothing when there is nothing new or no network.</summary>
    public async Task Check()
    {
        if (!IsAvailable)
        {
            return;
        }

        int count;
        try
        {
            using CancellationTokenSource limit = new CancellationTokenSource(CHECK_LIMIT);
            count = await Task.Run(() => SelfUpdate.CountNewCommits(limit.Token));
        }
        catch (Exception)
        {
            return;
        }

        if (count <= 0 || State != UpdateState.None)
        {
            return;
        }

        StatusText = $"새 버전이 있습니다 (커밋 {count}개)";
        State = UpdateState.Available;
        IsNoticeOpen = true;
    }

    [RelayCommand]
    private async Task Start()
    {
        if (!CanStart)
        {
            return;
        }

        LastLine = string.Empty;
        StatusText = "업데이트 빌드 중… (1~2분)";
        State = UpdateState.Building;
        UpdateResult result;
        try
        {
            result = await Task.Run(() => SelfUpdate.Build(OnLine, CancellationToken.None));
        }
        catch (Exception exception)
        {
            LastLine = exception.Message;
            result = UpdateResult.Failed;
        }

        switch (result)
        {
            case UpdateResult.UpToDate:
                StatusText = "이미 최신 버전입니다.";
                State = UpdateState.UpToDate;
                break;
            case UpdateResult.Built:
                Install();
                break;
            case UpdateResult.Failed:
            case UpdateResult.None:
                Fail();
                break;
            default:
                throw new NotImplementedException($"unhandled switch case: {result}");
        }
    }

    [RelayCommand]
    private void Restart()
    {
        SettingsService.SaveNow();
        SelfUpdate.StartNewCopy();
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    [RelayCommand]
    private void CloseNotice()
    {
        IsNoticeOpen = false;
    }

    private void Install()
    {
        if (!SelfUpdate.Install(OnLine))
        {
            Fail();
            return;
        }

        StatusText = "업데이트 준비 완료 — 재시작하면 적용됩니다.";
        State = UpdateState.Ready;
        IsNoticeOpen = true;
    }

    private void Fail()
    {
        StatusText = "업데이트 실패";
        State = UpdateState.Failed;
    }

    private void OnLine(string line)
    {
        string text = line.Trim();
        if (text.Length == 0)
        {
            return;
        }

        Dispatcher.UIThread.Post(() => LastLine = text);
    }
}
