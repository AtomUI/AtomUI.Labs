using AtomUI.Toolkits.GalleryBase.Navigation;
using ReactiveUI;

namespace AtomUILabsGallery.Workspace.ViewModels;

public sealed class CaseNavigationViewModel : GalleryNavigationViewModel
{
    public CaseNavigationViewModel(IScreen hostScreen)
        : base(hostScreen, AtomUILabsGalleryModule.GetConfiguration())
    {
    }
}
