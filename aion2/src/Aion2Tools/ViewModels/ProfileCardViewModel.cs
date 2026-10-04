using System.ComponentModel;
using System.Linq;
using Aion2Tools.Models;

namespace Aion2Tools.ViewModels;

/// <summary>A profile as a card on the profile list.</summary>
public class ProfileCardViewModel : ViewModelBase
{
    public ProfileData Profile { get; }

    public string DisplayName => Profile.Name.Trim().Length > 0 ? Profile.Name : "이름 없음";

    public string SummaryText
    {
        get
        {
            int groupCount = Profile.Characters.Select(character => character.Number).Distinct().Count();
            return $"그룹 {groupCount}/{Profile.MaxGroupCount} · 캐릭터 {Profile.Characters.Count}";
        }
    }

    public ProfileCardViewModel(ProfileData profile)
    {
        Profile = profile;
        Profile.PropertyChanged += OnProfileChanged;
    }

    /// <summary>Stops listening to the profile; called when the list rebuilds its cards.</summary>
    public void Detach()
    {
        Profile.PropertyChanged -= OnProfileChanged;
    }

    private void OnProfileChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(SummaryText));
    }
}
