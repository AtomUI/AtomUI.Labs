using AtomUI.Labs.Controls.Led.Segment.Character;
using AtomUI.Labs.Controls.Led.Segment.Rendering;
using Avalonia;
using Avalonia.Media;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Controls.Led.Tests.Segment;

public class SegmentGeometryFactoryTests
{
    static SegmentGeometryFactoryTests()
    {
        AvaloniaTestApp.EnsureInitialized();
    }

    [Fact]
    public void Create_ShouldReturnOneGeometryForEachSegmentPart()
    {
        var geometrySet = SegmentGeometryFactory.Create(
            new Rect(10, 20, 80, 140),
            new SegmentGeometryOptions(8, 2));

        geometrySet.Items.Count.ShouldBe(14);
        geometrySet.Items.Select(item => item.Part)
                   .ShouldBe(Enum.GetValues<SegmentParts>().Where(part => part != SegmentParts.None), ignoreOrder: true);
    }

    [Fact]
    public void Create_ShouldKeepGeometriesInsideCharacterBounds()
    {
        var bounds = new Rect(10, 20, 80, 140);
        var geometrySet = SegmentGeometryFactory.Create(bounds, new SegmentGeometryOptions(8, 2));

        foreach (var item in geometrySet.Items)
        {
            item.Geometry.Bounds.ShouldSatisfyAllConditions(
                geometryBounds => geometryBounds.X.ShouldBeGreaterThanOrEqualTo(bounds.X),
                geometryBounds => geometryBounds.Y.ShouldBeGreaterThanOrEqualTo(bounds.Y),
                geometryBounds => geometryBounds.Right.ShouldBeLessThanOrEqualTo(bounds.Right),
                geometryBounds => geometryBounds.Bottom.ShouldBeLessThanOrEqualTo(bounds.Bottom));
        }
    }

    [Fact]
    public void Create_ShouldKeepFractionalGeometriesFiniteAndInsideBounds()
    {
        var bounds = new Rect(0.25, 0.75, 37.5, 63.25);
        var geometrySet = SegmentGeometryFactory.Create(
            bounds,
            new SegmentGeometryOptions(3.75, 0.625, 0.35, 0.68));

        foreach (var item in geometrySet.Items)
        {
            AssertFinite(item.Geometry.Bounds);
            item.Geometry.Bounds.X.ShouldBeGreaterThanOrEqualTo(bounds.X);
            item.Geometry.Bounds.Y.ShouldBeGreaterThanOrEqualTo(bounds.Y);
            item.Geometry.Bounds.Right.ShouldBeLessThanOrEqualTo(bounds.Right);
            item.Geometry.Bounds.Bottom.ShouldBeLessThanOrEqualTo(bounds.Bottom);
        }
    }

    [Theory]
    [InlineData(2, 2, 20, 20)]
    [InlineData(1, 100, 50, 50)]
    [InlineData(100, 1, 50, 50)]
    [InlineData(40, 80, 1000, 1000)]
    public void Create_ShouldClampExtremeThicknessAndGapWithoutInvalidGeometry(
        double width,
        double height,
        double thickness,
        double gap)
    {
        var bounds = new Rect(0, 0, width, height);
        var geometrySet = SegmentGeometryFactory.Create(bounds, new SegmentGeometryOptions(thickness, gap));

        geometrySet.Items.Count.ShouldBe(14);
        foreach (var item in geometrySet.Items)
        {
            AssertFinite(item.Geometry.Bounds);
            item.Geometry.Bounds.X.ShouldBeGreaterThanOrEqualTo(bounds.X);
            item.Geometry.Bounds.Y.ShouldBeGreaterThanOrEqualTo(bounds.Y);
            item.Geometry.Bounds.Right.ShouldBeLessThanOrEqualTo(bounds.Right);
            item.Geometry.Bounds.Bottom.ShouldBeLessThanOrEqualTo(bounds.Bottom);
        }
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(10, 0)]
    [InlineData(-1, 10)]
    [InlineData(10, -1)]
    public void Create_ShouldReturnEmptyGeometriesForNonPositiveBounds(double width, double height)
    {
        var geometrySet = SegmentGeometryFactory.Create(
            new Rect(0, 0, width, height),
            new SegmentGeometryOptions(8, 2));

        geometrySet.Items.Count.ShouldBe(14);
        geometrySet.Items.ShouldAllBe(item => item.Geometry.Bounds == default);
    }

    [Theory]
    [InlineData(double.NaN, 2)]
    [InlineData(double.PositiveInfinity, 2)]
    [InlineData(8, double.NaN)]
    [InlineData(8, double.PositiveInfinity)]
    public void Create_ShouldHandleNonFiniteOptions(double thickness, double gap)
    {
        var geometrySet = SegmentGeometryFactory.Create(
            new Rect(0, 0, 80, 120),
            new SegmentGeometryOptions(thickness, gap));

        geometrySet.Items.Count.ShouldBe(14);
        foreach (var item in geometrySet.Items)
        {
            AssertFinite(item.Geometry.Bounds);
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(0.5)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Create_ShouldClampBevelRatioWithoutInvalidGeometry(double bevelRatio)
    {
        var bounds = new Rect(0, 0, 80, 120);
        var geometrySet = SegmentGeometryFactory.Create(bounds, new SegmentGeometryOptions(8, 2, bevelRatio));

        geometrySet.Items.Count.ShouldBe(14);
        foreach (var item in geometrySet.Items)
        {
            AssertFinite(item.Geometry.Bounds);
            item.Geometry.Bounds.X.ShouldBeGreaterThanOrEqualTo(bounds.X);
            item.Geometry.Bounds.Y.ShouldBeGreaterThanOrEqualTo(bounds.Y);
            item.Geometry.Bounds.Right.ShouldBeLessThanOrEqualTo(bounds.Right);
            item.Geometry.Bounds.Bottom.ShouldBeLessThanOrEqualTo(bounds.Bottom);
        }
    }

    [Fact]
    public void CreateColon_ShouldReturnTwoDotGeometriesInsideBounds()
    {
        var bounds = new Rect(10, 20, 20, 80);
        var geometries = SegmentGeometryFactory.CreateColon(bounds, new SegmentGeometryOptions(8, 2));

        geometries.Count.ShouldBe(2);
        foreach (var geometry in geometries)
        {
            geometry.Bounds.X.ShouldBeGreaterThanOrEqualTo(bounds.X);
            geometry.Bounds.Y.ShouldBeGreaterThanOrEqualTo(bounds.Y);
            geometry.Bounds.Right.ShouldBeLessThanOrEqualTo(bounds.Right);
            geometry.Bounds.Bottom.ShouldBeLessThanOrEqualTo(bounds.Bottom);
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(0.72)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void CreateDot_ShouldClampDotScaleWithoutInvalidGeometry(double dotScale)
    {
        var bounds = new Rect(10, 20, 20, 80);
        var geometry = SegmentGeometryFactory.CreateDot(bounds, new SegmentGeometryOptions(8, 2, DotScale: dotScale), false);

        AssertFinite(geometry.Bounds);
        geometry.Bounds.X.ShouldBeGreaterThanOrEqualTo(0);
        geometry.Bounds.Y.ShouldBeGreaterThanOrEqualTo(0);
        geometry.Bounds.Right.ShouldBeLessThanOrEqualTo(bounds.Right);
        geometry.Bounds.Bottom.ShouldBeLessThanOrEqualTo(bounds.Bottom);
    }

    private static void AssertFinite(Rect bounds)
    {
        bounds.X.ShouldNotBe(double.NaN);
        bounds.Y.ShouldNotBe(double.NaN);
        bounds.Width.ShouldNotBe(double.NaN);
        bounds.Height.ShouldNotBe(double.NaN);
        double.IsInfinity(bounds.X).ShouldBeFalse();
        double.IsInfinity(bounds.Y).ShouldBeFalse();
        double.IsInfinity(bounds.Width).ShouldBeFalse();
        double.IsInfinity(bounds.Height).ShouldBeFalse();
    }
}
