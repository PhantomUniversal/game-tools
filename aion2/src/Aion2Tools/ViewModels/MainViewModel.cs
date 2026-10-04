using System.Collections.Generic;
using System.Threading.Tasks;
using Aion2Tools.Models;
using Aion2Tools.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aion2Tools.ViewModels;

/// <summary>The window: sidebar pages, the update notice, and the game data every page reads.</summary>
public partial class MainViewModel : ViewModelBase
{
    private const string PARTY_ICON = "M12,3 L19,6 V11 C19,15.5 16,19 12,21 C8,19 5,15.5 5,11 V6 Z";
    private const string ROSTER_ICON = "M9,4 a3.5,3.5 0 1,0 0,7 a3.5,3.5 0 1,0 0,-7 Z M3,20 a6,6 0 0,1 12,0 M16,5 a3,3 0 0,1 0,6 M17,14 a5,5 0 0,1 4,6";
    private const string SETTINGS_ICON = "M12.22,2 H11.78 a2,2 0 0 0 -2,2 v0.18 a2,2 0 0 1 -1,1.73 l-0.43,0.25 a2,2 0 0 1 -2,0 l-0.15,-0.08 a2,2 0 0 0 -2.73,0.73 l-0.22,0.38 a2,2 0 0 0 0.73,2.73 l0.15,0.1 a2,2 0 0 1 1,1.72 v0.51 a2,2 0 0 1 -1,1.74 l-0.15,0.09 a2,2 0 0 0 -0.73,2.73 l0.22,0.38 a2,2 0 0 0 2.73,0.73 l0.15,-0.08 a2,2 0 0 1 2,0 l0.43,0.25 a2,2 0 0 1 1,1.73 V20 a2,2 0 0 0 2,2 h0.44 a2,2 0 0 0 2,-2 v-0.18 a2,2 0 0 1 1,-1.73 l0.43,-0.25 a2,2 0 0 1 2,0 l0.15,0.08 a2,2 0 0 0 2.73,-0.73 l0.22,-0.39 a2,2 0 0 0 -0.73,-2.73 l-0.15,-0.08 a2,2 0 0 1 -1,-1.74 v-0.5 a2,2 0 0 1 1,-1.74 l0.15,-0.09 a2,2 0 0 0 0.73,-2.73 l-0.22,-0.38 a2,2 0 0 0 -2.73,-0.73 l-0.15,0.08 a2,2 0 0 1 -2,0 l-0.43,-0.25 a2,2 0 0 1 -1,-1.73 V4 a2,2 0 0 0 -2,-2 Z M15,12 A3,3 0 1,1 9,12 A3,3 0 1,1 15,12 Z";

    public PartyViewModel Party { get; }

    public ProfilesViewModel Profiles { get; }

    public SettingsViewModel Settings { get; }

    public UpdateViewModel Update { get; } = new UpdateViewModel();

    /// <summary>The pages in the upper part of the sidebar; settings sits apart at the bottom.</summary>
    public IReadOnlyList<NavItemViewModel> NavItems { get; }

    public NavItemViewModel SettingsNav { get; }

    [ObservableProperty]
    public partial NavItemViewModel SelectedNav { get; set; }

    public MainViewModel()
    {
        GameDataTable data = GameDataService.Load();
        Party = new PartyViewModel(data);
        Profiles = new ProfilesViewModel(data);
        Settings = new SettingsViewModel(Update, data, ApplyData);
        NavItems = new List<NavItemViewModel>
        {
            new NavItemViewModel("프로필", ROSTER_ICON, Profiles),
            new NavItemViewModel("파티", PARTY_ICON, Party),
        };
        SettingsNav = new NavItemViewModel("설정", SETTINGS_ICON, Settings);
        SelectedNav = NavItems[0];
    }

    public void SelectNav(NavItemViewModel item)
    {
        SelectedNav = item;
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

    partial void OnSelectedNavChanged(NavItemViewModel oldValue, NavItemViewModel newValue)
    {
        if (oldValue is not null)
        {
            oldValue.IsActive = false;
        }

        newValue.IsActive = true;
        if (newValue.Page == Party)
        {
            Party.Refresh();
        }
    }

    private void ApplyData(GameDataTable data)
    {
        Party.SetData(data);
        Profiles.SetData(data);
        Settings.ShowDataVersion(data);
    }
}
