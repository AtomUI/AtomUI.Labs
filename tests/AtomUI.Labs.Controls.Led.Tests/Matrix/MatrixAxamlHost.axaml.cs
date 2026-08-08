using AtomUI.Labs.Controls.Led.Matrix;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AtomUI.Labs.Controls.Led.Tests.Matrix;

internal partial class MatrixAxamlHost : UserControl
{
    public MatrixAxamlHost()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public MatrixDisplay Display => this.FindControl<MatrixDisplay>("PART_Matrix")!;
}
