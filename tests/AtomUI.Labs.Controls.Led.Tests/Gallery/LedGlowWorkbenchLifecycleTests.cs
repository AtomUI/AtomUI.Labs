using AtomUILabsGallery.ShowCases.Led;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Controls.Led.Tests.Gallery;

public class LedGlowWorkbenchLifecycleTests
{
    static LedGlowWorkbenchLifecycleTests()
    {
        AvaloniaTestApp.EnsureInitialized();
    }

    [Fact]
    public void DetachAndReattach_ShouldCancelAndRestartConfiguredAnimation()
    {
        var workbench = new LedGlowWorkbench { AnimationModeIndex = 1 };

        ShowAndClose(workbench);
        workbench.HasActiveAnimation.ShouldBeFalse();

        ShowAndClose(workbench);
        workbench.HasActiveAnimation.ShouldBeFalse();
    }

    [Fact]
    public void BrushEditors_ShouldUpdateBothPreviews_AndPreserveLastValidColor()
    {
        var workbench = new LedGlowWorkbench();

        workbench.BrushPresetIndex = 1;
        workbench.BrushHexText.ShouldBe("#FF3030");
        GetColor(workbench.MatrixGlowBrush).ShouldBe(Color.FromRgb(255, 48, 48));
        GetColor(workbench.SegmentGlowBrush).ShouldBe(Color.FromRgb(255, 48, 48));

        workbench.BrushHexText = "#8044CC88";
        workbench.BrushPresetIndex.ShouldBe(-1);
        workbench.HasBrushValidationError.ShouldBeFalse();
        GetColor(workbench.MatrixGlowBrush).ShouldBe(Color.FromArgb(128, 68, 204, 136));
        GetColor(workbench.SegmentGlowBrush).ShouldBe(Color.FromArgb(128, 68, 204, 136));

        workbench.BrushHexText = "#abcdef";
        workbench.HasBrushValidationError.ShouldBeFalse();
        GetColor(workbench.MatrixGlowBrush).ShouldBe(Color.FromRgb(171, 205, 239));
        GetColor(workbench.SegmentGlowBrush).ShouldBe(Color.FromRgb(171, 205, 239));

        workbench.BrushHexText = "#12";
        workbench.HasBrushValidationError.ShouldBeTrue();
        GetColor(workbench.MatrixGlowBrush).ShouldBe(Color.FromRgb(171, 205, 239));
        GetColor(workbench.SegmentGlowBrush).ShouldBe(Color.FromRgb(171, 205, 239));
    }

    [Fact]
    public void DisabledState_ShouldRetainEditedConfigurationUntilReenabled()
    {
        var workbench = new LedGlowWorkbench
        {
            GlowEnabled = false,
            BrushHexText = "#336699",
            GlowOpacity = 0.72,
            GlowRadius = 13
        };

        workbench.MatrixGlowBrush.ShouldBeNull();
        workbench.SegmentGlowBrush.ShouldBeNull();
        workbench.MatrixGlowOpacity.ShouldBe(0.72);
        workbench.SegmentGlowOpacity.ShouldBe(0.72);
        workbench.MatrixGlowRadius.ShouldBe(13);
        workbench.SegmentGlowRadius.ShouldBe(13);

        workbench.GlowEnabled = true;

        GetColor(workbench.MatrixGlowBrush).ShouldBe(Color.FromRgb(51, 102, 153));
        GetColor(workbench.SegmentGlowBrush).ShouldBe(Color.FromRgb(51, 102, 153));
        workbench.MatrixGlowOpacity.ShouldBe(0.72);
        workbench.SegmentGlowOpacity.ShouldBe(0.72);
        workbench.MatrixGlowRadius.ShouldBe(13);
        workbench.SegmentGlowRadius.ShouldBe(13);
    }

    [Fact]
    public void ModeAndStateChanges_ShouldRestartOrCancelAnimationImmediately()
    {
        var workbench = new LedGlowWorkbench();
        var window = new Window { Width = 800, Height = 600, Content = workbench };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            workbench.HasActiveAnimation.ShouldBeFalse();

            workbench.AnimationModeIndex = 1;
            Dispatcher.UIThread.RunJobs();
            workbench.HasActiveAnimation.ShouldBeTrue();

            workbench.AnimationModeIndex = 2;
            Dispatcher.UIThread.RunJobs();
            workbench.HasActiveAnimation.ShouldBeTrue();

            workbench.GlowEnabled = false;
            Dispatcher.UIThread.RunJobs();
            workbench.HasActiveAnimation.ShouldBeFalse();

            workbench.GlowEnabled = true;
            Dispatcher.UIThread.RunJobs();
            workbench.HasActiveAnimation.ShouldBeTrue();

            workbench.AnimationModeIndex = 0;
            Dispatcher.UIThread.RunJobs();
            workbench.HasActiveAnimation.ShouldBeFalse();
        }
        finally
        {
            window.Content = null;
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }

    private static void ShowAndClose(LedGlowWorkbench workbench)
    {
        var window = new Window { Width = 800, Height = 600, Content = workbench };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            workbench.HasActiveAnimation.ShouldBeTrue();
        }
        finally
        {
            window.Content = null;
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }

    private static Color GetColor(IBrush? brush)
    {
        return brush.ShouldBeOfType<SolidColorBrush>().Color;
    }
}
