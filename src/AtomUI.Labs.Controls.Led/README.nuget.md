## AtomUI Labs LED

`AtomUI.Labs.Controls.Led` provides experimental LED-style display controls for AtomUI applications:

- `SegmentDisplay`: fourteen-segment text display;
- `MatrixDisplay`: fixed 5x7 dot-matrix text display with optional marquee and glow.

### Install

```bash
dotnet add package AtomUI.Labs.Controls.Led
```

Use a package version that matches the AtomUI packages in the application.

### Application setup

```csharp
this.UseAtomUI(builder => builder.UseLed());
```

### AXAML

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:atom.labs="https://atomui.net/labs">
    <StackPanel Spacing="12">
        <atom.labs:SegmentDisplay Text="12:45" />
        <atom.labs:MatrixDisplay Text="ATOMUI LABS" />
    </StackPanel>
</Window>
```

The package depends directly on AtomUI.Core and Avalonia. It does not require AtomUI.Desktop.Controls.

### Status and license

The controls are experimental and their public APIs may evolve. The package follows the AtomUI.Labs repository license.
