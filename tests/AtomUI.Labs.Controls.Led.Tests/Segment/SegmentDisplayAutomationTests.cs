using AtomUI.Labs.Controls.Led.Segment;
using AtomUI.Labs.Controls.Led.Segment.Character;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Controls.Led.Tests.Segment;

public class SegmentDisplayAutomationTests
{
    static SegmentDisplayAutomationTests()
    {
        AvaloniaTestApp.EnsureInitialized();
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("12:ab", "12:AB")]
    [InlineData("A?中Z", "A  Z")]
    [InlineData("A\nZ", "A Z")]
    public void GetDisplayText_ShouldMatchRenderedCharacterSemantics(string? text, string expected)
    {
        SegmentCharacterMap.GetDisplayText(text).ShouldBe(expected);
    }

    [Fact]
    public void AutomationPeer_ShouldExposeDisplayAsTextContent()
    {
        var display = new SegmentDisplay { Text = "12:ab" };

        var peer = ControlAutomationPeer.CreatePeerForElement(display);

        peer.ShouldBeOfType<SegmentDisplayAutomationPeer>();
        peer.GetClassName().ShouldBe(nameof(SegmentDisplay));
        peer.GetAutomationControlType().ShouldBe(AutomationControlType.Text);
        peer.GetName().ShouldBe("12:AB");
        peer.IsContentElement().ShouldBeTrue();
    }

    [Fact]
    public void AutomationPeer_ShouldPreferExplicitAutomationName()
    {
        var display = new SegmentDisplay { Text = "12:34" };
        AutomationProperties.SetName(display, "Elapsed time");

        var peer = ControlAutomationPeer.CreatePeerForElement(display);

        peer.GetName().ShouldBe("Elapsed time");
    }

    [Fact]
    public void AutomationPeer_ShouldRespectExplicitEmptyAutomationName()
    {
        var display = new SegmentDisplay { Text = "12:34" };
        AutomationProperties.SetName(display, string.Empty);

        var peer = ControlAutomationPeer.CreatePeerForElement(display);

        peer.GetName().ShouldBeEmpty();
    }

    [Fact]
    public void AutomationPeer_ShouldReflectTextChanges()
    {
        var display = new SegmentDisplay { Text = "abc" };
        var peer = ControlAutomationPeer.CreatePeerForElement(display);
        var propertyChangeCount = 0;
        peer.PropertyChanged += (_, _) => propertyChangeCount++;

        display.Text = "98:xy";

        peer.GetName().ShouldBe("98:XY");
        propertyChangeCount.ShouldBe(1);
    }

    [Fact]
    public void AutomationPeer_ShouldNotRaiseNameChangeWhenRenderedTextIsEquivalent()
    {
        var display = new SegmentDisplay { Text = "abc" };
        var peer = ControlAutomationPeer.CreatePeerForElement(display);
        var propertyChangeCount = 0;
        peer.PropertyChanged += (_, _) => propertyChangeCount++;

        display.Text = "ABC";

        peer.GetName().ShouldBe("ABC");
        propertyChangeCount.ShouldBe(0);
    }
}
