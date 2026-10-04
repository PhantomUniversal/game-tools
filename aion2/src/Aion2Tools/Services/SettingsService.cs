using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using Aion2Tools.Models;
using Avalonia.Threading;

namespace Aion2Tools.Services;

/// <summary>Loads and saves <see cref="AppSettings"/>. Saves are delayed and merged: a text box changes on every key.</summary>
public static class SettingsService
{
    private const int SAVE_DELAY_MILLISECONDS = 500;
    private const string LEGACY_PROFILE_NAME = "기본";

    private static DispatcherTimer? _saveTimerOrNull;

    public static AppSettings Settings { get; private set; } = new AppSettings();

    /// <summary>Called once at start, on the UI thread.</summary>
    public static void Load()
    {
        Settings = LoadFromDisk();
        _saveTimerOrNull = new DispatcherTimer();
        _saveTimerOrNull.Interval = TimeSpan.FromMilliseconds(SAVE_DELAY_MILLISECONDS);
        _saveTimerOrNull.Tick += OnSaveTimerTick;

        foreach (ProfileData profile in Settings.Profiles)
        {
            WatchProfile(profile);
        }

        Settings.Profiles.CollectionChanged += OnProfilesChanged;
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

            if (loadedOrNull.LegacyCharactersOrNull is not null)
            {
                if (loadedOrNull.LegacyCharactersOrNull.Count > 0)
                {
                    ProfileData legacy = new ProfileData();
                    legacy.Name = LEGACY_PROFILE_NAME;
                    legacy.Characters = loadedOrNull.LegacyCharactersOrNull;
                    loadedOrNull.Profiles.Add(legacy);
                }

                loadedOrNull.LegacyCharactersOrNull = null;
            }

            foreach (ProfileData profile in loadedOrNull.Profiles)
            {
                foreach (CharacterData character in profile.Characters)
                {
                    if (character.ClassId is null)
                    {
                        character.ClassId = string.Empty;
                    }
                }
            }

            return loadedOrNull;
        }
        catch (Exception)
        {
            return new AppSettings();
        }
    }

    /// <summary>Saves on any change to the profile, its list, or a character in it.</summary>
    private static void WatchProfile(ProfileData profile)
    {
        profile.PropertyChanged += OnItemChanged;
        foreach (CharacterData character in profile.Characters)
        {
            character.PropertyChanged += OnItemChanged;
        }

        profile.Characters.CollectionChanged += OnCharactersChanged;
    }

    private static void OnProfilesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (ProfileData profile in e.NewItems)
            {
                WatchProfile(profile);
            }
        }

        ScheduleSave();
    }

    private static void OnCharactersChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (CharacterData character in e.NewItems)
            {
                character.PropertyChanged += OnItemChanged;
            }
        }

        ScheduleSave();
    }

    private static void OnItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        ScheduleSave();
    }

    private static void OnSaveTimerTick(object? sender, EventArgs e)
    {
        SaveNow();
    }
}
