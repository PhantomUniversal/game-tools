using System;
using Aion2Tools.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;

namespace Aion2Tools.Views.Pages;

public partial class ProfilesView : UserControl
{
    private const double CARD_MIN_WIDTH = 280;
    private const double CARD_GAP = 14;

    public ProfilesView()
    {
        InitializeComponent();
    }

    private void OnProfileTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control control && control.DataContext is ProfileCardViewModel card && DataContext is ProfilesViewModel viewModel)
        {
            viewModel.Open(card.Profile);
        }
    }

    private void OnAddProfileTapped(object? sender, TappedEventArgs e)
    {
        (DataContext as ProfilesViewModel)?.AddProfileCommand.Execute(null);
    }

    /// <summary>As many columns as fit at the minimum card width, then the cards widen to fill the row.</summary>
    private void OnCardWallSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (CardWall.ItemsPanelRoot is not WrapPanel panel)
        {
            return;
        }

        double width = e.NewSize.Width + CARD_GAP;
        int columns = Math.Max(1, (int)(width / (CARD_MIN_WIDTH + CARD_GAP)));
        panel.ItemWidth = Math.Floor(width / columns);
    }
}
