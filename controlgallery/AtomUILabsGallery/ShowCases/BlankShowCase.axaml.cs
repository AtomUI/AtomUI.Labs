using AtomUI.Controls;
using AtomUI.Toolkits.GalleryBase.Controls;
using ReactiveUI;

namespace AtomUILabsGallery.ShowCases;

public partial class BlankShowCase : GalleryReactiveUserControl<BlankViewModel>
{
    public BlankShowCase()
    {
        InitializeComponent();
    }
}

public sealed class BlankViewModel : ReactiveObject, IRoutableViewModel
{
    public static EntityKey ID => "BlankShowCase";

    public BlankViewModel(IScreen hostScreen)
    {
        HostScreen = hostScreen;
    }

    public IScreen HostScreen { get; }

    public string UrlPathSegment => ID.ToString();
}
