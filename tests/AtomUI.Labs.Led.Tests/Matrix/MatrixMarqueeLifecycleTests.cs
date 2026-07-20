using AtomUI.Labs.Led.Matrix;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Shouldly;
using System.Runtime.CompilerServices;
using Xunit;

namespace AtomUI.Labs.Led.Tests.Matrix;

public class MatrixMarqueeLifecycleTests
{
    static MatrixMarqueeLifecycleTests()
    {
        AvaloniaTestApp.EnsureInitialized();
    }

    [Fact]
    public void AttachEnableDisableAndDetach_ShouldOwnAnimationLifetime()
    {
        var display = CreateDisplay();
        var window = new Window { Width = 320, Height = 100, Content = display };

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            display.IsMarqueeAnimationRunning.ShouldBeTrue();

            display.IsMarqueeEnabled = false;
            Dispatcher.UIThread.RunJobs();
            display.IsMarqueeAnimationRunning.ShouldBeFalse();
            display.MarqueeProgress.ShouldBe(0);

            display.IsMarqueeEnabled = true;
            Dispatcher.UIThread.RunJobs();
            display.IsMarqueeAnimationRunning.ShouldBeTrue();
        }
        finally
        {
            window.Close();
        }

        display.IsMarqueeAnimationRunning.ShouldBeFalse();
        display.MarqueeProgress.ShouldBe(0);
    }

    [Fact]
    public void InvalidRuntimeConditions_ShouldNotKeepAnimation()
    {
        var display = CreateDisplay();
        var window = new Window { Width = 320, Height = 100, Content = display };

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            display.MarqueeSpeed = 0;
            display.IsMarqueeAnimationRunning.ShouldBeFalse();

            display.MarqueeSpeed = 48;
            display.Text = string.Empty;
            display.IsMarqueeAnimationRunning.ShouldBeFalse();

            display.Text = "MATRIX";
            display.IsVisible = false;
            display.IsMarqueeAnimationRunning.ShouldBeFalse();
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void RestartAndDisable_ShouldReleasePreviousController()
    {
        var display = CreateDisplay();
        var window = new Window { Width = 320, Height = 100, Content = display };
        WeakReference? oldController = null;

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            oldController = CaptureControllerAndDisable(display);
            Dispatcher.UIThread.RunJobs();
        }
        finally
        {
            window.Close();
        }

        ForceFullCollection();
        oldController.IsAlive.ShouldBeFalse();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CaptureControllerAndDisable(MatrixDisplay display)
    {
        var reference = new WeakReference(display.MarqueeController!);
        display.IsMarqueeEnabled = false;
        return reference;
    }

    private static void ForceFullCollection()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private static MatrixDisplay CreateDisplay()
    {
        return new MatrixDisplay
        {
            Text = "MARQUEE",
            IsMarqueeEnabled = true,
            Width = 280,
            Height = 72
        };
    }
}
