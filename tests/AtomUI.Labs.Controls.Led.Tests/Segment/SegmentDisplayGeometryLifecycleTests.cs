using System.Runtime.CompilerServices;
using AtomUI.Labs.Controls.Led.Segment;
using Avalonia;
using Avalonia.Media;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Controls.Led.Tests.Segment;

public class SegmentDisplayGeometryLifecycleTests
{
    static SegmentDisplayGeometryLifecycleTests()
    {
        AvaloniaTestApp.EnsureInitialized();
    }

    [Fact]
    public void PatternChange_ShouldReleasePreviousActiveGeometryAndReuseInactiveGeometry()
    {
        var display = CreateDisplay("1111");
        Render(display);
        var inactive = display.VisibleInactiveGeometry;
        var previousActive = CaptureActiveAndChangeText(display, "8888");

        ForceFullCollection();

        previousActive.IsAlive.ShouldBeFalse();
        display.VisibleInactiveGeometry.ShouldBeSameAs(inactive);
    }

    [Fact]
    public void GeometryOptionChange_ShouldReleasePreviousInactiveGeometry()
    {
        var display = CreateDisplay("8888");
        Render(display);
        var previousInactive = CaptureInactiveAndChangeThickness(display);

        ForceFullCollection();

        previousInactive.IsAlive.ShouldBeFalse();
        display.VisibleInactiveGeometry.ShouldNotBeNull();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CaptureActiveAndChangeText(SegmentDisplay display, string text)
    {
        var reference = new WeakReference(display.VisibleActiveGeometry!);
        display.Text = text;
        Render(display);
        return reference;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CaptureInactiveAndChangeThickness(SegmentDisplay display)
    {
        var reference = new WeakReference(display.VisibleInactiveGeometry!);
        display.SegmentThickness += 1;
        Render(display);
        return reference;
    }

    private static SegmentDisplay CreateDisplay(string text)
    {
        var display = new SegmentDisplay
        {
            Text = text,
            CharacterHeight = 72,
            ActiveBrush = Brushes.Red,
            InactiveBrush = Brushes.DarkGray
        };
        display.Measure(new Size(400, 120));
        display.Arrange(new Rect(0, 0, 400, 120));
        return display;
    }

    private static void Render(SegmentDisplay display)
    {
        using var context = new DrawingGroup().Open();
        display.Render(context);
    }

    private static void ForceFullCollection()
    {
        for (var i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}
