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

namespace AtomUI.Labs.Led.Tests.Segment;

public class SegmentDisplayThemeTests
{
    static SegmentDisplayThemeTests()
    {
        AvaloniaTestApp.EnsureInitialized();
    }

    [Fact]
    public void AxamlHost_ShouldCreateSegmentThroughLabsXmlNamespace()
    {
        var display = new SegmentAxamlHost().Display;

        display.Text.ShouldBe("12:45");
        display.CharacterHeight.ShouldBe(64);
        display.CharacterAspectRatio.ShouldBe(0.6);
        display.SegmentThickness.ShouldBe(7);
        display.HorizontalContentAlignment.ShouldBe(HorizontalAlignment.Center);
        display.ShowInactiveSegments.ShouldBeFalse();
    }

    [Fact]
    public void ControlTheme_ShouldResolveSharedTokenDefaults()
    {
        var host = new SegmentAxamlHost();
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
    public void ThemeChanges_ShouldRefreshDefaultsAndPreserveLocalValues()
    {
        var application = Application.Current;
        application.ShouldNotBeNull();
        var previousVariant = application!.RequestedThemeVariant;
        var host = new SegmentAxamlHost();

        try
        {
            ShowInWindow(host, () =>
            {
                var initialBackground = GetBrushColor(host.Display.Background);
                var initialPadding = host.Display.Padding;
                host.Display.ActiveBrush = Brushes.Magenta;

                application.RequestedThemeVariant = new ThemeVariant($"{IThemeManager.DEFAULT_THEME_ID}-Dark", null);
                Dispatcher.UIThread.RunJobs();
                GetBrushColor(host.Display.Background).ShouldNotBe(initialBackground);
                host.Display.ActiveBrush.ShouldBeSameAs(Brushes.Magenta);

                application.RequestedThemeVariant = new ThemeVariant($"{IThemeManager.DEFAULT_THEME_ID}-Compact", null);
                Dispatcher.UIThread.RunJobs();
                host.Display.Padding.ShouldBe(GetThemeResource<Thickness>(SharedTokenKind.PaddingLG));
                host.Display.Padding.ShouldNotBe(initialPadding);
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
    public void ExplicitNullBrushes_ShouldOverrideThemeDefaultsAcrossThemeChange()
    {
        var application = Application.Current;
        application.ShouldNotBeNull();
        var previousVariant = application!.RequestedThemeVariant;
        var host = new SegmentAxamlHost();

        try
        {
            ShowInWindow(host, () =>
            {
                host.Display.ActiveBrush = null;
                host.Display.InactiveBrush = null;

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
        var window = new Window { Width = 420, Height = 160, Content = content };
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
