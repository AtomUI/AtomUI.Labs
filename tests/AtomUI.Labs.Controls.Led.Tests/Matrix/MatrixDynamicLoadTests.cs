using System.Globalization;
using AtomUI.Labs.Controls.Led.Matrix;
using Avalonia;
using Avalonia.Media;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Controls.Led.Tests.Matrix;

public class MatrixDynamicLoadTests
{
    static MatrixDynamicLoadTests()
    {
        AvaloniaTestApp.EnsureInitialized();
    }

    [Theory]
    [InlineData(6, false)]
    [InlineData(6, true)]
    [InlineData(8, false)]
    [InlineData(8, true)]
    [InlineData(16, false)]
    [InlineData(16, true)]
    public void FixedLengthDynamicText_ShouldReuseGeometryForSixHundredFrames(
        int characterCount,
        bool showInactiveDots)
    {
        var display = CreateDisplay(showInactiveDots);
        for (var frame = 0; frame < 20; frame++)
        {
            UpdateAndRender(display, CreateText(characterCount, frame));
        }

        var geometryBuildCount = display.GeometryBuildCount;
        var geometryCacheCount = display.GeometryCacheCount;
        var layoutVersion = display.LayoutCacheVersion;

        DrawingGroup? finalDrawing = null;
        for (var frame = 20; frame < 620; frame++)
        {
            finalDrawing = UpdateAndRender(display, CreateText(characterCount, frame));
        }

        display.GeometryBuildCount.ShouldBe(geometryBuildCount);
        display.GeometryCacheCount.ShouldBe(geometryCacheCount);
        display.LayoutCacheVersion.ShouldBe(layoutVersion + 600);
        CountGeometryDrawings(finalDrawing!).ShouldBe(characterCount * (showInactiveDots ? 2 : 1));
    }

    private static MatrixDisplay CreateDisplay(bool showInactiveDots)
    {
        return new MatrixDisplay
        {
            DotSize          = 6,
            DotSpacing       = 2,
            CharacterSpacing = 8,
            Padding          = default,
            ActiveBrush      = Brushes.White,
            InactiveBrush    = showInactiveDots ? Brushes.Gray : null,
            ShowInactiveDots = showInactiveDots
        };
    }

    private static DrawingGroup UpdateAndRender(MatrixDisplay display, string text)
    {
        var viewport = new Size(800, 80);
        display.Text = text;
        display.Measure(viewport);
        display.Arrange(new Rect(viewport));

        var drawing = new DrawingGroup();
        using var context = drawing.Open();
        display.Render(context);
        return drawing;
    }

    private static string CreateText(int characterCount, int value)
    {
        return characterCount switch
        {
            6 => (value % 1_000_000).ToString("D6", CultureInfo.InvariantCulture),
            8 => (value % 100_000_000).ToString("D8", CultureInfo.InvariantCulture),
            16 => "TEMP"
                  + (value % 10_000).ToString("D4", CultureInfo.InvariantCulture)
                  + "RPM"
                  + (value % 100_000).ToString("D5", CultureInfo.InvariantCulture),
            _ => throw new ArgumentOutOfRangeException(nameof(characterCount))
        };
    }

    private static int CountGeometryDrawings(Drawing drawing)
    {
        if (drawing is GeometryDrawing)
        {
            return 1;
        }

        return drawing is DrawingGroup group
            ? group.Children.Sum(CountGeometryDrawings)
            : 0;
    }
}
