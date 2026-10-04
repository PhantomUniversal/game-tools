using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Aion2Tools.Views;

/// <summary>A class id to its official icon (Assets/Classes/{id}.png). Null for a class with no icon file.</summary>
public sealed class ClassIconConverter : IValueConverter
{
    /// <summary>The source art is 256px; decoded once at this width, sharp up to about 48px on a 2x screen.</summary>
    private const int DECODE_WIDTH = 96;

    public static readonly ClassIconConverter Instance = new ClassIconConverter();

    private readonly Dictionary<string, Bitmap?> _cache = new Dictionary<string, Bitmap?>();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string classId || classId.Length == 0)
        {
            return null;
        }

        Bitmap? bitmapOrNull;
        if (!_cache.TryGetValue(classId, out bitmapOrNull))
        {
            bitmapOrNull = LoadOrNull(classId);
            _cache[classId] = bitmapOrNull;
        }

        return bitmapOrNull;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static Bitmap? LoadOrNull(string classId)
    {
        Uri uri = new Uri($"avares://Aion2Tools/Assets/Classes/{classId}.png");
        if (!AssetLoader.Exists(uri))
        {
            return null;
        }

        using Stream stream = AssetLoader.Open(uri);
        return Bitmap.DecodeToWidth(stream, DECODE_WIDTH, BitmapInterpolationMode.HighQuality);
    }
}
