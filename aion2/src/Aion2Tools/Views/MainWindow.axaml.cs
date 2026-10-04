using Aion2Tools.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;

namespace Aion2Tools.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnNavTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control control && control.DataContext is NavItemViewModel item && DataContext is MainViewModel viewModel)
        {
            viewModel.SelectNav(item);
        }
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }
}
