using System.Runtime.CompilerServices;
using AtomUI.Labs.Controls.Led.Matrix;
using Avalonia;
using Avalonia.Media;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Controls.Led.Tests.Matrix;

public class MatrixGlyphGeometryLifecycleTests
{
    static MatrixGlyphGeometryLifecycleTests()
    {
        AvaloniaTestApp.EnsureInitialized();
    }

    [Fact]
    public void DotMetricChange_ShouldReleaseOldGeometrySet()
    {
        var display = CreateDisplay();
        var geometryReferences = CaptureAndClearGeometryCache(display);

        ForceGarbageCollection();

        geometryReferences.ShouldAllBe(reference => !reference.IsAlive);
        display.GeometryCacheCount.ShouldBe(0);
        GC.KeepAlive(display);
    }

    [Fact]
    public void DotShapeChange_ShouldReleaseOldGeometrySet()
    {
        var display = CreateDisplay();
        var geometryReferences = CaptureAndChangeDotShape(display);

        ForceGarbageCollection();

        geometryReferences.ShouldAllBe(reference => !reference.IsAlive);
        display.GeometryCacheCount.ShouldBe(0);
        GC.KeepAlive(display);
    }

    [Fact]
    public void RoundedCornerRatioChange_ShouldReleaseOldGeometrySet()
    {
        var display = CreateDisplay();
        var geometryReferences = CaptureAndChangeRoundedCornerRatio(display);

        ForceGarbageCollection();

        geometryReferences.ShouldAllBe(reference => !reference.IsAlive);
        display.GeometryCacheCount.ShouldBe(0);
        GC.KeepAlive(display);
    }

    [Fact]
    public void UnreferencedDisplayAndGeometryCache_ShouldBeCollectible()
    {
        var displayReference = CreatePopulatedDisplayReference();

        ForceGarbageCollection();

        displayReference.IsAlive.ShouldBeFalse();
    }

    [Fact]
    public void BorderMetricChange_ShouldReleaseOldComplexGeometry()
    {
        var display = CreateDisplay();
        var geometryReference = CaptureAndChangeBorderThickness(display);

        ForceGarbageCollection();

        geometryReference.IsAlive.ShouldBeFalse();
        display.BorderGeometryCache.ShouldBeNull();
        GC.KeepAlive(display);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference[] CaptureAndClearGeometryCache(MatrixDisplay display)
    {
        Render(display);
        var references = display.GeometryCacheValues
                                .Select(geometry => new WeakReference(geometry))
                                .ToArray();

        display.DotSize = 9;
        return references;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreatePopulatedDisplayReference()
    {
        var display = CreateDisplay();
        Render(display);
        display.GeometryCacheCount.ShouldBeGreaterThan(0);
        return new WeakReference(display);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CaptureAndChangeBorderThickness(MatrixDisplay display)
    {
        display.BorderBrush = Brushes.Blue;
        display.BorderThickness = new Thickness(1, 2, 3, 4);
        display.CornerRadius = new CornerRadius(8);
        Render(display);
        var reference = new WeakReference(display.BorderGeometryCache!);

        display.BorderThickness = new Thickness(2, 3, 4, 5);
        return reference;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference[] CaptureAndChangeDotShape(MatrixDisplay display)
    {
        Render(display);
        var references = display.GeometryCacheValues
                                .Select(geometry => new WeakReference(geometry))
                                .ToArray();

        display.DotShape = MatrixDotShape.Square;
        return references;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference[] CaptureAndChangeRoundedCornerRatio(MatrixDisplay display)
    {
        display.DotShape = MatrixDotShape.RoundedSquare;
        Render(display);
        var references = display.GeometryCacheValues
                                .Select(geometry => new WeakReference(geometry))
                                .ToArray();

        display.DotCornerRadiusRatio = 0.4;
        return references;
    }

    private static MatrixDisplay CreateDisplay()
    {
        var display = new MatrixDisplay
        {
            Text             = "MATRIX 2026",
            DotSize          = 6,
            DotSpacing       = 2,
            CharacterSpacing = 8,
            ActiveBrush      = Brushes.Red,
            InactiveBrush    = Brushes.DarkGray
        };
        display.Measure(new Size(800, 120));
        display.Arrange(new Rect(0, 0, 800, 120));
        return display;
    }

    private static void Render(MatrixDisplay display)
    {
        var drawing = new DrawingGroup();
        using var context = drawing.Open();
        display.Render(context);
    }

    private static void ForceGarbageCollection()
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}
