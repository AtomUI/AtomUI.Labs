using AtomUI.Toolkits.GalleryBase.Shell;

namespace AtomUILabsGallery.Workspace.ViewModels;

public sealed class WorkspaceWindowViewModel : GalleryWorkspaceViewModel
{
    public WorkspaceWindowViewModel()
        : base(AtomUILabsGalleryModule.GetConfiguration(), screen => new CaseNavigationViewModel(screen))
    {
    }

    public CaseNavigationViewModel CaseNavigation => (CaseNavigationViewModel)Navigation;
}
