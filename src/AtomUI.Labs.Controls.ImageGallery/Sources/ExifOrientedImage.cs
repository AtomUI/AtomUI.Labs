using Avalonia;
using Avalonia.Media;

namespace AtomUI.Labs.Controls.ImageGallery;

/// <summary>
/// Presents a decoded JPEG in its EXIF-defined logical orientation without
/// copying the decoded pixel buffer.
/// </summary>
internal sealed class ExifOrientedImage(IImage image, int orientation) : IImage
{
    private readonly IImage _image = image ?? throw new ArgumentNullException(nameof(image));
    private readonly int _orientation = orientation is >= 1 and <= 8
        ? orientation
        : throw new ArgumentOutOfRangeException(nameof(orientation));

    public Size Size => _orientation is >= 5 and <= 8
        ? new Size(_image.Size.Height, _image.Size.Width)
        : _image.Size;

    public void Draw(DrawingContext context, Rect sourceRect, Rect destRect)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (sourceRect.Width <= 0 || sourceRect.Height <= 0 ||
            destRect.Width <= 0 || destRect.Height <= 0)
        {
            return;
        }

        var rawToLogical = CreateRawToLogicalTransform(
            _image.Size.Width,
            _image.Size.Height,
            _orientation);
        var logicalToDestination =
            Matrix.CreateTranslation(-sourceRect.X, -sourceRect.Y) *
            Matrix.CreateScale(
                destRect.Width / sourceRect.Width,
                destRect.Height / sourceRect.Height) *
            Matrix.CreateTranslation(destRect.X, destRect.Y);
        var transform = rawToLogical * logicalToDestination;
        var logicalToRaw = rawToLogical.Invert();
        var rawSourceRect = TransformBounds(sourceRect, logicalToRaw);

        using (context.PushTransform(transform))
        {
            _image.Draw(context, rawSourceRect, rawSourceRect);
        }
    }

    internal static Matrix CreateRawToLogicalTransform(
        double width,
        double height,
        int orientation) => orientation switch
        {
            1 => Matrix.Identity,
            2 => new Matrix(-1, 0, 0, 1, width, 0),
            3 => new Matrix(-1, 0, 0, -1, width, height),
            4 => new Matrix(1, 0, 0, -1, 0, height),
            5 => new Matrix(0, 1, 1, 0, 0, 0),
            6 => new Matrix(0, 1, -1, 0, height, 0),
            7 => new Matrix(0, -1, -1, 0, height, width),
            8 => new Matrix(0, -1, 1, 0, 0, width),
            _ => throw new ArgumentOutOfRangeException(nameof(orientation)),
        };

    private static Rect TransformBounds(Rect rect, Matrix matrix)
    {
        var topLeft = matrix.Transform(rect.TopLeft);
        var topRight = matrix.Transform(rect.TopRight);
        var bottomLeft = matrix.Transform(rect.BottomLeft);
        var bottomRight = matrix.Transform(rect.BottomRight);
        var minX = Math.Min(Math.Min(topLeft.X, topRight.X), Math.Min(bottomLeft.X, bottomRight.X));
        var minY = Math.Min(Math.Min(topLeft.Y, topRight.Y), Math.Min(bottomLeft.Y, bottomRight.Y));
        var maxX = Math.Max(Math.Max(topLeft.X, topRight.X), Math.Max(bottomLeft.X, bottomRight.X));
        var maxY = Math.Max(Math.Max(topLeft.Y, topRight.Y), Math.Max(bottomLeft.Y, bottomRight.Y));
        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }
}
