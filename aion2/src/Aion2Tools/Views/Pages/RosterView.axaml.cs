using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Aion2Tools.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace Aion2Tools.Views.Pages;

public partial class RosterView : UserControl
{
    private const double CARD_MIN_WIDTH = 280;
    private const double CARD_GAP = 14;
    private const int KOREAN_CODE_PAGE = 949;

    private static readonly FilePickerFileType CSV_TYPE = new FilePickerFileType("CSV") { Patterns = new[] { "*.csv" } };

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

    /// <summary>Handled here so the tap does not also reach the fold header it sits in.</summary>
    private void OnRemoveAltTapped(object? sender, TappedEventArgs e)
    {
        e.Handled = true;
        if (sender is Control control && control.DataContext is CharacterRowViewModel row && DataContext is RosterViewModel viewModel)
        {
            viewModel.RemoveAlt(row);
        }
    }

    private void OnFoldTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control control && control.DataContext is CharacterRowViewModel row)
        {
            row.ToggleExpanded();
        }
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

    private async void OnImportClick(object? sender, RoutedEventArgs e)
    {
        TopLevel? topLevelOrNull = TopLevel.GetTopLevel(this);
        if (topLevelOrNull is null || DataContext is not RosterViewModel viewModel)
        {
            return;
        }

        FilePickerOpenOptions options = new FilePickerOpenOptions();
        options.Title = "명단 불러오기";
        options.FileTypeFilter = new[] { CSV_TYPE };
        IReadOnlyList<IStorageFile> files = await topLevelOrNull.StorageProvider.OpenFilePickerAsync(options);
        if (files.Count == 0)
        {
            return;
        }

        await using Stream stream = await files[0].OpenReadAsync();
        using MemoryStream buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        viewModel.Import(DecodeCsv(buffer.ToArray()));
    }

    /// <summary>Written with a BOM so Excel opens the Korean text correctly.</summary>
    private async void OnExportClick(object? sender, RoutedEventArgs e)
    {
        TopLevel? topLevelOrNull = TopLevel.GetTopLevel(this);
        if (topLevelOrNull is null || DataContext is not RosterViewModel viewModel)
        {
            return;
        }

        FilePickerSaveOptions options = new FilePickerSaveOptions();
        options.Title = "명단 내보내기";
        options.SuggestedFileName = $"{viewModel.Profile.Name}.csv";
        options.DefaultExtension = "csv";
        options.FileTypeChoices = new[] { CSV_TYPE };
        IStorageFile? fileOrNull = await topLevelOrNull.StorageProvider.SaveFilePickerAsync(options);
        if (fileOrNull is null)
        {
            return;
        }

        await using Stream stream = await fileOrNull.OpenWriteAsync();
        await using StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(true));
        await writer.WriteAsync(viewModel.Export());
        viewModel.StatusText = $"{fileOrNull.Name}(으)로 내보냈습니다.";
    }

    /// <summary>UTF-8 first; a file Excel saved as plain "CSV" is CP949 on a Korean Windows.</summary>
    private static string DecodeCsv(byte[] bytes)
    {
        try
        {
            return new UTF8Encoding(false, true).GetString(bytes).TrimStart('﻿');
        }
        catch (DecoderFallbackException)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(KOREAN_CODE_PAGE).GetString(bytes);
        }
    }

    private void OnClosePanelTapped(object? sender, TappedEventArgs e)
    {
        (DataContext as RosterViewModel)?.ClosePanelCommand.Execute(null);
    }
}
