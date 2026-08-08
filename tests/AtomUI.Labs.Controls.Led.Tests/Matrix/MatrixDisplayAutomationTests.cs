using AtomUI.Labs.Controls.Led.Matrix;
using AtomUI.Labs.Controls.Led.Matrix.Character;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Controls.Led.Tests.Matrix;

public class MatrixDisplayAutomationTests
{
    static MatrixDisplayAutomationTests()
    {
        AvaloniaTestApp.EnsureInitialized();
    }

    [Fact]
    public void AutomationPeer_ShouldExposeNormalizedDisplayAsTextContent()
    {
        var display = new MatrixDisplay { Text = "a中z" };

        var peer = ControlAutomationPeer.CreatePeerForElement(display);

        peer.ShouldBeOfType<MatrixDisplayAutomationPeer>();
        peer.GetClassName().ShouldBe(nameof(MatrixDisplay));
        peer.GetAutomationControlType().ShouldBe(AutomationControlType.Text);
        peer.GetName().ShouldBe("A?Z");
        peer.IsContentElement().ShouldBeTrue();
    }

    [Fact]
    public void AutomationPeer_ShouldExposeOneFallbackPerUnsupportedUnicodeScalar()
    {
        var display = new MatrixDisplay { Text = "A😀中Z" };

        var peer = ControlAutomationPeer.CreatePeerForElement(display);

        peer.GetName().ShouldBe("A??Z");
    }

    [Fact]
    public void AutomationPeer_ShouldPreferExplicitAutomationNameIncludingEmptyString()
    {
        var display = new MatrixDisplay { Text = "1234" };
        AutomationProperties.SetName(display, "Counter");
        var peer = ControlAutomationPeer.CreatePeerForElement(display);

        peer.GetName().ShouldBe("Counter");

        AutomationProperties.SetName(display, string.Empty);
        peer.GetName().ShouldBeEmpty();
    }

    [Fact]
    public void AutomationPeer_ShouldRaiseNameChangeForDifferentDisplayText()
    {
        var display = new MatrixDisplay { Text = "abc" };
        var peer = ControlAutomationPeer.CreatePeerForElement(display);
        var propertyChangeCount = 0;
        peer.PropertyChanged += (_, _) => propertyChangeCount++;

        display.Text = "98中";

        peer.GetName().ShouldBe("98?");
        propertyChangeCount.ShouldBe(1);
    }

    [Fact]
    public void AutomationPeer_ShouldNotRaiseNameChangeForEquivalentDisplayText()
    {
        var display = new MatrixDisplay { Text = "abc" };
        var peer = ControlAutomationPeer.CreatePeerForElement(display);
        var propertyChangeCount = 0;
        peer.PropertyChanged += (_, _) => propertyChangeCount++;

        display.Text = "ABC";

        peer.GetName().ShouldBe(MatrixCharacterMap.GetDisplayText(display.Text));
        propertyChangeCount.ShouldBe(0);
    }
}
