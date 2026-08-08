using AtomUI.Labs.Controls.Led.Performance;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;

namespace AtomUI.Labs.Controls.Led.GlowPrototype.Desktop;

internal sealed class GlowPrototypeWindow : Window
{
    private static readonly Color WindowBackground = Color.FromRgb(10, 13, 17);
    private static readonly Color PanelBackground = Color.FromRgb(16, 23, 29);

    public GlowPrototypeWindow()
    {
        Title = "AtomUI Labs LED Glow Prototype";
        Width = 1120;
        Height = 820;
        MinWidth = 760;
        MinHeight = 560;
        Background = new SolidColorBrush(WindowBackground);
        Content = new ScrollViewer
        {
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            Content = BuildContent()
        };
    }

    private static Control BuildContent()
    {
        var root = new StackPanel
        {
            Margin = new Thickness(24),
            Spacing = 18
        };
        root.Children.Add(new TextBlock
        {
            Text = "LED Glow route comparison",
            FontSize = 24,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.White
        });
        root.Children.Add(new TextBlock
        {
            Text = "Same geometry, brush, opacity and radius. Active source remains white; cyan is Glow; gray is inactive; yellow is panel border.",
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.FromRgb(170, 181, 190))
        });

        foreach (var radius in new[] { 6d, 12d, 24d })
        {
            root.Children.Add(BuildRadiusSection(radius));
        }

        root.Children.Add(BuildClipSection());
        return root;
    }

    private static Control BuildRadiusSection(double radius)
    {
        var section = new StackPanel { Spacing = 10 };
        section.Children.Add(new TextBlock
        {
            Text = $"GlowRadius {radius:0} DIP",
            FontSize = 17,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.White
        });

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            ColumnSpacing = 14
        };
        grid.Children.Add(BuildRouteColumn("Route A - Vector expansion", radius, false));
        var scoped = BuildRouteColumn("Route C - Scoped BlurEffect", radius, true);
        Grid.SetColumn(scoped, 1);
        grid.Children.Add(scoped);
        section.Children.Add(grid);
        return section;
    }

    private static Control BuildRouteColumn(string title, double radius, bool scopedBlur)
    {
        var panel = new StackPanel
        {
            Spacing = 8,
            Background = new SolidColorBrush(PanelBackground),
            Margin = new Thickness(0)
        };
        panel.Children.Add(new TextBlock
        {
            Text = title,
            Margin = new Thickness(12, 10, 12, 0),
            FontSize = 14,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.White
        });

        var previews = new UniformGrid
        {
            Columns = 2,
            Rows = 2,
            Margin = new Thickness(8)
        };
        foreach (var source in CreateSources())
        {
            previews.Children.Add(new GlowPrototypePreview(
                source.Name,
                source.Geometry,
                CreateRenderer(scopedBlur, radius)));
        }

        panel.Children.Add(previews);
        return panel;
    }

    private static Control BuildClipSection()
    {
        var section = new StackPanel { Spacing = 8 };
        section.Children.Add(new TextBlock
        {
            Text = "Strict clip and layer isolation",
            FontSize = 17,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.White
        });
        section.Children.Add(new GlowPrototypePreview(
            "Scoped Blur, Radius 24, clipped inside yellow border",
            new EllipseGeometry(new Rect(54, 34, 52, 52)),
            CreateRenderer(true, 24),
            showInactive: true,
            clipBounds: new Rect(16, 16, 128, 88))
        {
            Width = 520,
            HorizontalAlignment = HorizontalAlignment.Left
        });
        return section;
    }

    private static IGlowPrototypeRenderer CreateRenderer(bool scopedBlur, double radius)
    {
        var brush = new SolidColorBrush(Color.FromRgb(64, 222, 255));
        return scopedBlur
            ? new ScopedBlurEffectGlowPrototype(brush, 0.65, radius)
            : new VectorExpansionGlowPrototype(brush, 0.65, radius);
    }

    private static IReadOnlyList<GlowSource> CreateSources()
    {
        return new[]
        {
            new GlowSource("Circle", new EllipseGeometry(new Rect(56, 36, 48, 48))),
            new GlowSource("Rounded square", new RectangleGeometry(new Rect(56, 36, 48, 48), 10, 10)),
            new GlowSource(
                "Segment diagonal",
                CreatePolygon(
                    new Point(46, 28),
                    new Point(59, 28),
                    new Point(116, 92),
                    new Point(103, 92))),
            new GlowSource(
                "Colon dots",
                new GeometryGroup
                {
                    Children =
                    {
                        new EllipseGeometry(new Rect(72, 35, 16, 16)),
                        new EllipseGeometry(new Rect(72, 69, 16, 16))
                    }
                })
        };
    }

    private static StreamGeometry CreatePolygon(params Point[] points)
    {
        var geometry = new StreamGeometry();
        using var context = geometry.Open();
        context.BeginFigure(points[0], true);
        for (var i = 1; i < points.Length; i++)
        {
            context.LineTo(points[i], true);
        }

        context.EndFigure(true);
        return geometry;
    }

    private sealed record GlowSource(string Name, Geometry Geometry);
}

internal sealed class GlowPrototypePreview : Control
{
    private readonly string _label;
    private readonly Geometry _activeGeometry;
    private readonly IGlowPrototypeRenderer _renderer;
    private readonly bool _showInactive;
    private readonly Rect? _clipBounds;
    private readonly Pen _borderPen = new(Brushes.Yellow, 2);

    public GlowPrototypePreview(
        string label,
        Geometry activeGeometry,
        IGlowPrototypeRenderer renderer,
        bool showInactive = true,
        Rect? clipBounds = null)
    {
        _label = label;
        _activeGeometry = activeGeometry;
        _renderer = renderer;
        _showInactive = showInactive;
        _clipBounds = clipBounds;
        Width = 180;
        Height = 132;
        Margin = new Thickness(4);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.DrawRectangle(
            new SolidColorBrush(Color.FromRgb(7, 11, 14)),
            null,
            new RoundedRect(new Rect(Bounds.Size), 6));

        if (_showInactive)
        {
            context.DrawGeometry(
                new SolidColorBrush(Color.FromArgb(110, 88, 101, 108)),
                null,
                new EllipseGeometry(new Rect(24, 50, 18, 18)));
        }

        if (_clipBounds is { } clipBounds)
        {
            using (context.PushClip(clipBounds))
            {
                RenderGlowAndSource(context);
            }
            context.DrawRectangle(null, _borderPen, new RoundedRect(clipBounds, 4));
        }
        else
        {
            RenderGlowAndSource(context);
        }

        var label = new FormattedText(
            _label,
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            11,
            Brushes.White);
        context.DrawText(label, new Point(8, Bounds.Height - 20));
    }

    private void RenderGlowAndSource(DrawingContext context)
    {
        _renderer.Render(context, _activeGeometry);
        context.DrawGeometry(Brushes.White, null, _activeGeometry);
    }
}
