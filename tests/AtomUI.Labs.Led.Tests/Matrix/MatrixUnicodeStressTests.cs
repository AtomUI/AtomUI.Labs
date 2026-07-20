using System.Text;
using AtomUI.Labs.Led.Matrix;
using AtomUI.Labs.Led.Matrix.Character;
using AtomUI.Labs.Led.Matrix.Layout;
using Avalonia;
using Avalonia.Media;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Led.Tests.Matrix;

public class MatrixUnicodeStressTests
{
    static MatrixUnicodeStressTests()
    {
        AvaloniaTestApp.EnsureInitialized();
    }

    [Fact]
    public void RandomUtf16Input_ShouldKeepMappingAndLayoutConsistent()
    {
        var random = new Random(0x5A17);
        for (var iteration = 0; iteration < 500; iteration++)
        {
            var text = CreateRandomUtf16(random, random.Next(0, 257));
            var displayText = MatrixCharacterMap.GetDisplayText(text);
            var layout = MatrixLayoutEngine.Calculate(text, new MatrixLayoutOptions(6, 2, 8, default));

            displayText.Length.ShouldBe(MatrixCharacterMap.GetPatternCount(text));
            layout.Slots.Count.ShouldBe(displayText.Length);
            layout.Slots.Select(slot => slot.Pattern.Character).ShouldBe(displayText);
            MatrixValueSanitizer.IsFinite(layout.DesiredSize.Width).ShouldBeTrue();
            MatrixValueSanitizer.IsFinite(layout.DesiredSize.Height).ShouldBeTrue();

            if (iteration % 25 == 0)
            {
                RenderShouldNotThrow(text);
            }
        }
    }

    private static string CreateRandomUtf16(Random random, int length)
    {
        var characters = new char[length];
        for (var i = 0; i < characters.Length; i++)
        {
            characters[i] = (char)random.Next(char.MinValue, char.MaxValue + 1);
        }

        return new string(characters);
    }

    private static void RenderShouldNotThrow(string text)
    {
        var display = new MatrixDisplay
        {
            Text             = text,
            DotSize          = 4,
            DotSpacing       = 1,
            CharacterSpacing = 2,
            ActiveBrush      = Brushes.White,
            InactiveBrush    = null,
            ShowInactiveDots = false
        };
        display.Measure(new Size(320, 80));
        display.Arrange(new Rect(0, 0, 320, 80));

        var drawing = new DrawingGroup();
        using var context = drawing.Open();
        display.Render(context);
    }
}
