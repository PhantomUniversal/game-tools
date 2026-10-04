namespace Aion2Tools.ViewModels;

/// <summary>One sidebar row and the page it opens.</summary>
public class NavItemViewModel : ViewModelBase
{
    public string Title { get; }

    public ViewModelBase Page { get; }

    public NavItemViewModel(string title, ViewModelBase page)
    {
        Title = title;
        Page = page;
    }
}
