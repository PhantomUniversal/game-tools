using System.Collections.ObjectModel;
using System.Linq;
using Aion2Tools.Models;
using Aion2Tools.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aion2Tools.ViewModels;

/// <summary>The profile tab: the list of profiles, and the open profile's member cards.</summary>
public partial class ProfilesViewModel : ViewModelBase
{
    private GameDataTable _data;

    private static ObservableCollection<ProfileData> Profiles => SettingsService.Settings.Profiles;

    /// <summary>The profile cards, then the add tile.</summary>
    public ObservableCollection<ViewModelBase> Tiles { get; } = new ObservableCollection<ViewModelBase>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsListShown))]
    public partial RosterViewModel? OpenRosterOrNull { get; set; }

    public bool IsListShown => OpenRosterOrNull is null;

    public ProfilesViewModel(GameDataTable data)
    {
        _data = data;
        RebuildTiles();
    }

    public void SetData(GameDataTable data)
    {
        _data = data;
        OpenRosterOrNull?.SetData(data);
    }

    public void Open(ProfileData profile)
    {
        OpenRosterOrNull = new RosterViewModel(profile, _data, Close, () => Delete(profile));
    }

    /// <summary>A new profile gets the next free "프로필 n" name and opens straight away.</summary>
    [RelayCommand]
    private void AddProfile()
    {
        int index = 1;
        while (Profiles.Any(profile => profile.Name == $"프로필 {index}"))
        {
            index++;
        }

        ProfileData created = new ProfileData();
        created.Name = $"프로필 {index}";
        Profiles.Add(created);
        RebuildTiles();
        Open(created);
    }

    private void Delete(ProfileData profile)
    {
        Profiles.Remove(profile);
        Close();
    }

    private void Close()
    {
        OpenRosterOrNull = null;
        RebuildTiles();
    }

    private void RebuildTiles()
    {
        foreach (ProfileCardViewModel card in Tiles.OfType<ProfileCardViewModel>())
        {
            card.Detach();
        }

        Tiles.Clear();
        foreach (ProfileData profile in Profiles)
        {
            Tiles.Add(new ProfileCardViewModel(profile));
        }

        Tiles.Add(new AddTileViewModel());
    }
}
