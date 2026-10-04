using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aion2Tools.Models;

/// <summary>One named roster, such as a legion or a fixed dungeon party, with its own member cards.</summary>
public partial class ProfileData : ObservableObject
{
    public const int DEFAULT_MAX_GROUP_COUNT = 128;
    public const int MIN_MAX_GROUP_COUNT = 1;
    public const int MAX_MAX_GROUP_COUNT = 999;

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    /// <summary>How many member cards the profile holds; 128 is a full legion.</summary>
    [ObservableProperty]
    public partial int MaxGroupCount { get; set; } = DEFAULT_MAX_GROUP_COUNT;

    public ObservableCollection<CharacterData> Characters { get; set; } = new ObservableCollection<CharacterData>();
}
