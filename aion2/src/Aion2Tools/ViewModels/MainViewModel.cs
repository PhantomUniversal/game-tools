using System.Collections.Generic;
using System.Threading.Tasks;
using Aion2Tools.Models;
using Aion2Tools.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aion2Tools.ViewModels;

/// <summary>The window: sidebar pages, the update notice, and the game data every page reads.</summary>
public partial class MainViewModel : ViewModelBase
{
    public PartyViewModel Party { get; }

    public RotationViewModel Rotation { get; }

    public RosterViewModel Roster { get; }

    public SettingsViewModel Settings { get; }

    public UpdateViewModel Update { get; } = new UpdateViewModel();

    public IReadOnlyList<NavItemViewModel> NavItems { get; }

    [ObservableProperty]
    public partial NavItemViewModel SelectedNav { get; set; }

    public MainViewModel()
    {
        GameDataTable data = GameDataService.Load();
        Party = new PartyViewModel(data);
        Rotation = new RotationViewModel(data);
        Roster = new RosterViewModel(data);
        Settings = new SettingsViewModel(Update, data, ApplyData);
        NavItems = new List<NavItemViewModel>
        {
            new NavItemViewModel("🛡  파티 조합", Party),
            new NavItemViewModel("👥  본부 조합", Rotation),
            new NavItemViewModel("📋  캐릭터 명단", Roster),
            new NavItemViewModel("⚙  설정", Settings),
        };
        SelectedNav = SettingsService.Settings.Characters.Count == 0 ? NavItems[2] : NavItems[0];
    }

    /// <summary>Background checks at start: the app's own update, then the game data when auto check is on.</summary>
    public async Task Startup()
    {
        await Update.Check();
        if (SettingsService.Settings.IsAutoCheckOn)
        {
            await Settings.CheckData();
        }
    }

    private void ApplyData(GameDataTable data)
    {
        Party.SetData(data);
        Rotation.SetData(data);
        Roster.SetData(data);
        Settings.ShowDataVersion(data);
    }
}
