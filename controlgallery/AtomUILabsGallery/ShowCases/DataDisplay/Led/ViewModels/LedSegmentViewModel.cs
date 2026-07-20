using AtomUI.Controls;
using ReactiveUI;

namespace AtomUILabsGallery.ShowCases.Led;

public sealed class LedSegmentViewModel : ReactiveObject, IRoutableViewModel
{
    public static EntityKey ID => "LedSegmentShowCase";

    public LedSegmentViewModel(IScreen hostScreen)
    {
        HostScreen = hostScreen;
    }

    public IScreen HostScreen { get; }

    public string UrlPathSegment => ID.ToString();
}
