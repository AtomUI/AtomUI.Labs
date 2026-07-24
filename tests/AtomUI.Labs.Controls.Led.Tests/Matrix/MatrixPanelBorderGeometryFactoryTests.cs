using AtomUI.Labs.Controls.Led.Matrix;
using AtomUI.Labs.Controls.Led.Matrix.Rendering;
using Avalonia;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Controls.Led.Tests.Matrix;

public class MatrixPanelBorderGeometryFactoryTests
{
    static MatrixPanelBorderGeometryFactoryTests()
    {
        AvaloniaTestApp.EnsureInitialized();
    }

    [Fact]
    public void Create_ShouldKeepGeometryInsideBoundsForAsymmetricBorder()
    {
        var geometry = MatrixPanelBorderGeometryFactory.Create(
            new Size(100, 60),
            new Thickness(1, 2, 3, 4),
            new CornerRadius(12, 8, 16, 4));

        geometry.Bounds.Left.ShouldBeGreaterThanOrEqualTo(0);
        geometry.Bounds.Top.ShouldBeGreaterThanOrEqualTo(0);
        geometry.Bounds.Right.ShouldBeLessThanOrEqualTo(100);
        geometry.Bounds.Bottom.ShouldBeLessThanOrEqualTo(60);
    }

    [Fact]
    public void Create_ShouldFillOuterBoundsWhenBorderConsumesInnerArea()
    {
        var geometry = MatrixPanelBorderGeometryFactory.Create(
            new Size(12, 8),
            new Thickness(7, 5, 7, 5),
            new CornerRadius(20));

        geometry.Bounds.ShouldBe(new Rect(0, 0, 12, 8));
    }

    [Fact]
    public void Create_ShouldNormalizeOverlappingCornerRadii()
    {
        var geometry = MatrixPanelBorderGeometryFactory.Create(
            new Size(20, 10),
            new Thickness(1, 2, 3, 1),
            new CornerRadius(1_000_000));

        MatrixValueSanitizer.IsFinite(geometry.Bounds.X).ShouldBeTrue();
        MatrixValueSanitizer.IsFinite(geometry.Bounds.Y).ShouldBeTrue();
        MatrixValueSanitizer.IsFinite(geometry.Bounds.Width).ShouldBeTrue();
        MatrixValueSanitizer.IsFinite(geometry.Bounds.Height).ShouldBeTrue();
    }
}
