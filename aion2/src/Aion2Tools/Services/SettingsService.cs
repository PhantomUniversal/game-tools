using System;
using System.Collections.Specialized;
using System.IO;
using Aion2Tools.Models;
using Avalonia.Threading;

namespace Aion2Tools.Services;

/// <summary>Loads and saves <see cref="AppSettings"/>. Saves are delayed and merged: a text box changes on every key.</summary>
public static class SettingsService
{
    private const int SAVE_DELAY_MILLISECONDS = 500;

    private static DispatcherTimer? _saveTimerOrNull;

    public static AppSettings Settings { get; private set; } = new AppSettings();

    /// <summary>Called once at start, on the UI thread.</summary>
    public static void Load()
    {
        Settings = LoadFromDisk();
        _saveTimerOrNull = new DispatcherTimer();
        _saveTimerOrNull.Interval = TimeSpan.FromMilliseconds(SAVE_DELAY_MILLISECONDS);
        _saveTimerOrNull.Tick += OnSaveTimerTick;

        foreach (CharacterData character in Settings.Characters)
        {
            character.PropertyChanged += OnCharacterChanged;
        }

        Settings.Characters.CollectionChanged += OnCharactersChanged;
    }

    public static void ScheduleSave()
    {
        if (_saveTimerOrNull is null)
        {
            return;
        }

        _saveTimerOrNull.Stop();
        _saveTimerOrNull.Start();
    }

    public static void SaveNow()
    {
        _saveTimerOrNull?.Stop();
        try
        {
            Directory.CreateDirectory(AppPaths.SettingsFolder);
            File.WriteAllText(AppPaths.SettingsFile, JsonUtil.Serialize(Settings));
        }
        catch (Exception)
        {
            // A failed save must not take the app down; the settings in memory stay usable.
        }
    }

    private static AppSettings LoadFromDisk()
    {
        try
        {
            if (!File.Exists(AppPaths.SettingsFile))
            {
                return new AppSettings();
            }

            AppSettings? loadedOrNull = JsonUtil.DeserializeOrNull<AppSettings>(File.ReadAllText(AppPaths.SettingsFile));
            if (loadedOrNull is null)
            {
                return new AppSettings();
            }

            foreach (CharacterData character in loadedOrNull.Characters)
            {
                if (character.ClassId is null)
                {
                    character.ClassId = string.Empty;
                }
            }

            return loadedOrNull;
        }
        catch (Exception)
        {
            return new AppSettings();
        }
    }

    private static void OnCharactersChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (CharacterData character in e.NewItems)
            {
                character.PropertyChanged += OnCharacterChanged;
            }
        }

        ScheduleSave();
    }

    private static void OnCharacterChanged(object? sender, EventArgs e)
    {
        ScheduleSave();
    }

    private static void OnSaveTimerTick(object? sender, EventArgs e)
    {
        SaveNow();
    }
}
