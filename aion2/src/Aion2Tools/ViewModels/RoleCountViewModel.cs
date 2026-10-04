using Avalonia.Media;

namespace Aion2Tools.ViewModels;

/// <summary>One role and how many of it a party has, on the party card's status bar.</summary>
public class RoleCountViewModel : ViewModelBase
{
    public string Text { get; }

    public IBrush Brush { get; }

    public RoleCountViewModel(string text, IBrush brush)
    {
        Text = text;
        Brush = brush;
    }
}
