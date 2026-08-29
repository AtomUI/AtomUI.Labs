using System.ComponentModel;
using System.Globalization;

namespace AtomUI.Labs.Controls.ImageGallery;

[TypeConverter(typeof(ImageGalleryZoomRangeConverter))]
public readonly record struct ImageGalleryZoomRange(double Minimum, double Maximum)
{
    public static ImageGalleryZoomRange Default { get; } = new(0.05, 32.0);

    internal static bool IsValid(ImageGalleryZoomRange value)
    {
        return double.IsFinite(value.Minimum) &&
               double.IsFinite(value.Maximum) &&
               value.Minimum > 0 &&
               value.Minimum <= value.Maximum;
    }
}

internal sealed class ImageGalleryZoomRangeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object ConvertFrom(
        ITypeDescriptorContext? context,
        CultureInfo? culture,
        object value)
    {
        if (value is not string text)
        {
            return base.ConvertFrom(context, culture, value)!;
        }

        var parts = text.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length != 2 ||
            !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var minimum) ||
            !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var maximum))
        {
            throw new FormatException($"Invalid ImageGalleryZoomRange '{text}'. Expected 'minimum,maximum'.");
        }

        var result = new ImageGalleryZoomRange(minimum, maximum);
        if (!ImageGalleryZoomRange.IsValid(result))
        {
            throw new FormatException($"Invalid ImageGalleryZoomRange '{text}'. Values must satisfy 0 < minimum <= maximum.");
        }

        return result;
    }
}
