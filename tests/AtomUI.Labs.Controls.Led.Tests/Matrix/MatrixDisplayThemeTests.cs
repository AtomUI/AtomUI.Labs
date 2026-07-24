using AtomUI.Labs.Controls.Led.Matrix;
using AtomUI.Theme;
using AtomUI.Theme.Styling;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Controls.Led.Tests.Matrix;

public class MatrixDisplayThemeTests
{
    static MatrixDisplayThemeTests()
    {
        AvaloniaTestApp.EnsureInitialized();
    }

    [Fact]
    public void AxamlHost_ShouldCreateMatrixThroughLabsXmlNamespace()
    {
        var host = new MatrixAxamlHost();

        host.Display.Text.ShouldBe("axaml 2026");
        host.Display.DotSize.ShouldBe(7);
        host.Display.DotShape.ShouldBe(MatrixDotShape.RoundedSquare);
        host.Display.DotCornerRadiusRatio.ShouldBe(0.3);
        GetBrushColor(host.Display.BorderBrush).ShouldBe(Colors.Blue);
        host.Display.BorderThickness.ShouldBe(new Thickness(1, 2, 3, 4));
        host.Display.IsMarqueeEnabled.ShouldBeTrue();
        host.Display.MarqueeSpeed.ShouldBe(64);
        host.Display.MarqueeRepeatDelay.ShouldBe(TimeSpan.FromSeconds(1));
        host.Display.HorizontalContentAlignment.ShouldBe(HorizontalAlignment.Center);
        host.Display.ShowInactiveDots.ShouldBeFalse();
    }

    [Fact]
    public void ControlTheme_ShouldResolveSharedTokenDefaults()
    {
        var host = new MatrixAxamlHost();
        ShowInWindow(host, () =>
        {
            BrushShouldHaveSameColor(host.Display.Background, GetThemeResource<IBrush>(SharedTokenKind.ColorBgContainer));
            BrushShouldHaveSameColor(host.Display.ActiveBrush, GetThemeResource<IBrush>(SharedTokenKind.ColorPrimary));
            BrushShouldHaveSameColor(host.Display.InactiveBrush, GetThemeResource<IBrush>(SharedTokenKind.ColorFillTertiary));
            host.Display.CornerRadius.ShouldBe(GetThemeResource<CornerRadius>(SharedTokenKind.BorderRadiusLG));
            host.Display.Padding.ShouldBe(GetThemeResource<Thickness>(SharedTokenKind.PaddingLG));
        });
    }

    [Fact]
    public void ThemeChange_ShouldRefreshTokenDefaultsAndPreserveLocalValue()
    {
        var application = Application.Current;
        application.ShouldNotBeNull();
        var previousVariant = application!.RequestedThemeVariant;
        var host = new MatrixAxamlHost();

        try
        {
            ShowInWindow(host, () =>
            {
                var initialBackground = GetBrushColor(host.Display.Background);
                host.Display.ActiveBrush = Brushes.Magenta;

                application.RequestedThemeVariant = new ThemeVariant($"{IThemeManager.DEFAULT_THEME_ID}-Dark", null);
                Dispatcher.UIThread.RunJobs();

                GetBrushColor(host.Display.Background).ShouldNotBe(initialBackground);
                BrushShouldHaveSameColor(host.Display.Background, GetThemeResource<IBrush>(SharedTokenKind.ColorBgContainer));
                host.Display.ActiveBrush.ShouldBeSameAs(Brushes.Magenta);
            });
        }
        finally
        {
            application.RequestedThemeVariant = previousVariant;
            Dispatcher.UIThread.RunJobs();
        }
    }

    [Fact]
    public void CompactThemeChange_ShouldRefreshSharedTokenPadding()
    {
        var application = Application.Current;
        application.ShouldNotBeNull();
        var previousVariant = application!.RequestedThemeVariant;
        var host = new MatrixAxamlHost();

        try
        {
            ShowInWindow(host, () =>
            {
                var initialPadding = host.Display.Padding;

                application.RequestedThemeVariant = new ThemeVariant($"{IThemeManager.DEFAULT_THEME_ID}-Compact", null);
                Dispatcher.UIThread.RunJobs();

                host.Display.Padding.ShouldBe(GetThemeResource<Thickness>(SharedTokenKind.PaddingLG));
                host.Display.Padding.ShouldNotBe(initialPadding);
            });
        }
        finally
        {
            application.RequestedThemeVariant = previousVariant;
            Dispatcher.UIThread.RunJobs();
        }
    }

    [Fact]
    public void ExplicitNullBrushes_ShouldOverrideThemeDefaultsAcrossThemeChange()
    {
        var application = Application.Current;
        application.ShouldNotBeNull();
        var previousVariant = application!.RequestedThemeVariant;
        var host = new MatrixAxamlHost();

        try
        {
            ShowInWindow(host, () =>
            {
                host.Display.ActiveBrush = null;
                host.Display.InactiveBrush = null;

                host.Display.ActiveBrush.ShouldBeNull();
                host.Display.InactiveBrush.ShouldBeNull();

                application.RequestedThemeVariant = new ThemeVariant($"{IThemeManager.DEFAULT_THEME_ID}-Dark", null);
                Dispatcher.UIThread.RunJobs();

                host.Display.ActiveBrush.ShouldBeNull();
                host.Display.InactiveBrush.ShouldBeNull();
            });
        }
        finally
        {
            application.RequestedThemeVariant = previousVariant;
            Dispatcher.UIThread.RunJobs();
        }
    }

    private static T GetThemeResource<T>(object key)
    {
        var application = Application.Current;
        application.ShouldNotBeNull();
        application!.TryGetResource(key, application.ActualThemeVariant, out var value).ShouldBeTrue();
        value.ShouldBeAssignableTo<T>();
        return (T)value!;
    }

    private static void BrushShouldHaveSameColor(IBrush? actual, IBrush expected)
    {
        GetBrushColor(actual).ShouldBe(GetBrushColor(expected));
    }

    private static Color GetBrushColor(IBrush? brush)
    {
        brush.ShouldBeAssignableTo<ISolidColorBrush>();
        return ((ISolidColorBrush)brush!).Color;
    }

    private static void ShowInWindow(Control content, Action assertion)
    {
        var window = new Window
        {
            Width   = 420,
            Height  = 160,
            Content = content
        };

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            assertion();
        }
        finally
        {
            window.Close();
        }
    }
}
