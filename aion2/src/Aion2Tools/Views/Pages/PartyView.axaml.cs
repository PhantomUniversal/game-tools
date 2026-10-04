using Aion2Tools.ViewModels;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;

namespace Aion2Tools.Views.Pages;

public partial class PartyView : UserControl
{
    public PartyView()
    {
        InitializeComponent();
    }

    /// <summary>The selected composition as plain text, for pasting into a chat.</summary>
    private async void OnCopyClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not PartyViewModel viewModel || viewModel.SelectedResultOrNull is null)
        {
            return;
        }

        IClipboard? clipboardOrNull = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboardOrNull is null)
        {
            return;
        }

        await clipboardOrNull.SetTextAsync(viewModel.SelectedResultOrNull.ToText());
        viewModel.StatusText = "클립보드에 복사했습니다.";
    }
}
