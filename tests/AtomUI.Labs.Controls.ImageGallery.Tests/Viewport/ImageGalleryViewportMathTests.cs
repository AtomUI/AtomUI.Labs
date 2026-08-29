using AtomUI.Labs.Controls.ImageGallery.Viewport;
using Avalonia;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Controls.ImageGallery.Tests.Viewport;

public sealed class ImageGalleryViewportMathTests
{
    [Theory]
    [InlineData(0, 0.125)]
    [InlineData(90, 0.25)]
    [InlineData(180, 0.125)]
    [InlineData(270, 0.25)]
    public void Fit_zoom_uses_rotated_bounds(int rotation, double expected)
    {
        ImageGalleryViewportMath.CalculateFitZoom(
                new Size(100, 50),
                new Size(200, 400),
                rotation,
                allowUpscaling: true)
            .ShouldBe(expected);
    }

    [Fact]
    public void Fit_zoom_does_not_upscale_by_default()
    {
        ImageGalleryViewportMath.CalculateFitZoom(
                new Size(1000, 1000),
                new Size(100, 100),
                0,
                allowUpscaling: false)
            .ShouldBe(1);
    }

    [Fact]
    public void Pan_is_centered_on_axes_where_content_is_smaller_than_viewport()
    {
        var result = ImageGalleryViewportMath.ClampPan(
            new Vector(400, 400),
            new Size(300, 200),
            new Size(100, 500),
            0,
            1);

        result.X.ShouldBe(0);
        result.Y.ShouldBe(150);
    }

    [Fact]
    public void Zoom_anchor_stays_stationary()
    {
        var result = ImageGalleryViewportMath.ZoomAroundPoint(
            default,
            new Point(75, 50),
            new Size(100, 100),
            1,
            2);

        result.ShouldBe(new Vector(-25, 0));
    }
}
