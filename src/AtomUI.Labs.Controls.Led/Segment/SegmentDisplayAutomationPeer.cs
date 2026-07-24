using Avalonia.Automation;
using Avalonia.Automation.Peers;
using AtomUI.Labs.Controls.Led.Segment.Character;

namespace AtomUI.Labs.Controls.Led.Segment;

internal sealed class SegmentDisplayAutomationPeer : ControlAutomationPeer
{
    private readonly SegmentDisplay _owner;

    public SegmentDisplayAutomationPeer(SegmentDisplay owner) : base(owner)
    {
        _owner = owner;
    }

    protected override string GetClassNameCore()
    {
        return nameof(SegmentDisplay);
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
            ? SegmentCharacterMap.GetDisplayText(_owner.Text)
            : inheritedName;
    }

    internal void NotifyTextChanged(string? oldText, string? newText)
    {
        if (AutomationProperties.GetName(_owner) is not null
            || !string.IsNullOrEmpty(base.GetNameCore()))
        {
            return;
        }

        var oldName = SegmentCharacterMap.GetDisplayText(oldText);
        var newName = SegmentCharacterMap.GetDisplayText(newText);
        if (oldName != newName)
        {
            RaisePropertyChangedEvent(AutomationElementIdentifiers.NameProperty, oldName, newName);
        }
    }
}
