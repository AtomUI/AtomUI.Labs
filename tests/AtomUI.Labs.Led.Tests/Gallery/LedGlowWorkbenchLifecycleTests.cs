using AtomUILabsGallery.ShowCases.Led;
using Avalonia.Controls;
using Avalonia.Threading;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Led.Tests.Gallery;

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
}
