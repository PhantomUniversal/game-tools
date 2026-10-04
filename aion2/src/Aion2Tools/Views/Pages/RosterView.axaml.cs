using Aion2Tools.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;

namespace Aion2Tools.Views.Pages;

public partial class RosterView : UserControl
{
    public RosterView()
    {
        InitializeComponent();
    }

    private void OnGroupTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control control && control.DataContext is PlayerGroupViewModel group && DataContext is RosterViewModel viewModel)
        {
            viewModel.SelectGroup(group);
        }
    }

    private void OnAddGroupTapped(object? sender, TappedEventArgs e)
    {
        (DataContext as RosterViewModel)?.AddGroupCommand.Execute(null);
    }

    private void OnAddAltTapped(object? sender, TappedEventArgs e)
    {
        (DataContext as RosterViewModel)?.AddAltCommand.Execute(null);
    }

    private void OnRemoveAltTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control control && control.DataContext is CharacterRowViewModel row && DataContext is RosterViewModel viewModel)
        {
            viewModel.RemoveAlt(row);
        }
    }

    private void OnClosePanelTapped(object? sender, TappedEventArgs e)
    {
        (DataContext as RosterViewModel)?.ClosePanelCommand.Execute(null);
    }
}
