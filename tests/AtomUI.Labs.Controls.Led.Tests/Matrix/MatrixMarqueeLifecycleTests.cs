using AtomUI.Labs.Controls.Led.Matrix;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Shouldly;
using System.Runtime.CompilerServices;
using Xunit;

namespace AtomUI.Labs.Controls.Led.Tests.Matrix;

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
            display.MarqueeVisibilitySubscriptionCount.ShouldBeGreaterThan(0);

            display.IsMarqueeEnabled = false;
            Dispatcher.UIThread.RunJobs();
            display.IsMarqueeAnimationRunning.ShouldBeFalse();
            display.MarqueeProgress.ShouldBe(0);
            display.MarqueeVisibilitySubscriptionCount.ShouldBe(0);

            display.IsMarqueeEnabled = true;
            Dispatcher.UIThread.RunJobs();
            display.IsMarqueeAnimationRunning.ShouldBeTrue();
            display.MarqueeVisibilitySubscriptionCount.ShouldBeGreaterThan(0);
        }
        finally
        {
            window.Close();
        }

        display.IsMarqueeAnimationRunning.ShouldBeFalse();
        display.MarqueeProgress.ShouldBe(0);
        display.MarqueeVisibilitySubscriptionCount.ShouldBe(0);
    }

    [Fact]
    public void AttachedStaticDisplay_ShouldNotSubscribeToAncestorVisibility()
    {
        var display = CreateDisplay();
        display.IsMarqueeEnabled = false;
        var window = new Window { Width = 320, Height = 100, Content = new Border { Child = display } };

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            display.MarqueeVisibilitySubscriptionCount.ShouldBe(0);
            display.IsMarqueeAnimationRunning.ShouldBeFalse();
        }
        finally
        {
            window.Close();
        }
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
    public void ParentVisibility_ShouldSuspendAndResumeAnimation()
    {
        var display = CreateDisplay();
        var parent = new Border { Child = display };
        var window = new Window { Width = 320, Height = 100, Content = parent };

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            display.IsMarqueeAnimationRunning.ShouldBeTrue();

            parent.IsVisible = false;
            Dispatcher.UIThread.RunJobs();
            display.IsEffectivelyVisible.ShouldBeFalse();
            display.IsMarqueeAnimationRunning.ShouldBeFalse();
            display.MarqueeProgress.ShouldBe(0);

            parent.IsVisible = true;
            Dispatcher.UIThread.RunJobs();
            display.IsEffectivelyVisible.ShouldBeTrue();
            display.IsMarqueeAnimationRunning.ShouldBeTrue();
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
