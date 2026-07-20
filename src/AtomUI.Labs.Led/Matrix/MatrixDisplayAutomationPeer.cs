using AtomUI.Labs.Led.Matrix.Character;
using Avalonia.Automation;
using Avalonia.Automation.Peers;

namespace AtomUI.Labs.Led.Matrix;

internal sealed class MatrixDisplayAutomationPeer : ControlAutomationPeer
{
    private readonly MatrixDisplay _owner;

    public MatrixDisplayAutomationPeer(MatrixDisplay owner) : base(owner)
    {
        _owner = owner;
    }

    protected override string GetClassNameCore()
    {
        return nameof(MatrixDisplay);
    }

    protected override AutomationControlType GetAutomationControlTypeCore()
    {
        return AutomationControlType.Text;
    }

    protected override string? GetNameCore()
    {
        var configuredName = AutomationProperties.GetName(_owner);
        if (configuredName is not null)
        {
            return configuredName;
        }

        var inheritedName = base.GetNameCore();
        return string.IsNullOrEmpty(inheritedName)
            ? MatrixCharacterMap.GetDisplayText(_owner.Text)
            : inheritedName;
    }

    internal void NotifyTextChanged(string? oldText, string? newText)
    {
        if (AutomationProperties.GetName(_owner) is not null
            || !string.IsNullOrEmpty(base.GetNameCore()))
        {
            return;
        }

        var oldName = MatrixCharacterMap.GetDisplayText(oldText);
        var newName = MatrixCharacterMap.GetDisplayText(newText);
        if (oldName != newName)
        {
            RaisePropertyChangedEvent(AutomationElementIdentifiers.NameProperty, oldName, newName);
        }
    }
}
