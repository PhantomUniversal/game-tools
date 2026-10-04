using CommunityToolkit.Mvvm.ComponentModel;

namespace Aion2Tools.ViewModels;

/// <summary>One sidebar row and the page it opens.</summary>
public partial class NavItemViewModel : ViewModelBase
{
    public string Title { get; }

    /// <summary>Stroke path on a 24-unit grid, drawn like Kiln's sidebar icons.</summary>
    public string IconData { get; }

    public ViewModelBase Page { get; }

    [ObservableProperty]
    public partial bool IsActive { get; set; }

    public NavItemViewModel(string title, string iconData, ViewModelBase page)
    {
        Title = title;
        IconData = iconData;
        Page = page;
    }
}
