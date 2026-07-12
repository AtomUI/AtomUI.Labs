using AtomUI.Controls;
using AtomUI.Toolkits.GalleryBase.Controls;
using ReactiveUI;

namespace AtomUILabsGallery.ShowCases;

public partial class OverviewShowCase : GalleryReactiveUserControl<OverviewViewModel>
{
    public OverviewShowCase()
    {
        InitializeComponent();
    }
}

public sealed class OverviewViewModel : ReactiveObject, IRoutableViewModel
{
    public static EntityKey ID => "Overview";

    public OverviewViewModel(IScreen hostScreen)
    {
        HostScreen = hostScreen;
    }

    public IScreen HostScreen { get; }

    public string UrlPathSegment => ID.ToString();
}
