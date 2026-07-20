using AtomUI.Labs.Led.Segment;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AtomUI.Labs.Led.Tests.Segment;

internal partial class SegmentAxamlHost : UserControl
{
    public SegmentAxamlHost()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public SegmentDisplay Display => this.FindControl<SegmentDisplay>("PART_Segment")!;
}
