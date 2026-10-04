using Aion2Tools.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;

namespace Aion2Tools.Views.Pages;

public partial class PartyView : UserControl
{
    public PartyView()
    {
        InitializeComponent();
    }

    private void OnMemberTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control control && control.DataContext is PartyMemberViewModel member && DataContext is PartyViewModel viewModel)
        {
            viewModel.ToggleMember(member);
        }
    }

    /// <summary>The result as plain text, for pasting into a chat.</summary>
    private async void OnCopyClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not PartyViewModel viewModel || !viewModel.HasResult)
        {
            return;
        }

        IClipboard? clipboardOrNull = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboardOrNull is null)
        {
            return;
        }

        await clipboardOrNull.SetTextAsync(viewModel.ToText());
        viewModel.StatusText = "클립보드에 복사했습니다.";
    }
}
