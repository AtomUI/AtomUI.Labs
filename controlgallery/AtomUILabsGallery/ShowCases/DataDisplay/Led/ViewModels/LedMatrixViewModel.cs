using AtomUI.Controls;
using ReactiveUI;

namespace AtomUILabsGallery.ShowCases.Led;

public sealed class LedMatrixViewModel : ReactiveObject, IRoutableViewModel
{
    public static EntityKey ID => "LedMatrixShowCase";

    public LedMatrixViewModel(IScreen hostScreen)
    {
        HostScreen = hostScreen;
    }

    public IScreen HostScreen { get; }

    public string UrlPathSegment => ID.ToString();
}
