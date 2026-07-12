using AtomUI.Desktop.Controls;
using AtomUI.Toolkits.GalleryBase.Controls;
using AtomUI.Toolkits.GalleryBase.Shell;
using AtomUILabsGallery.Workspace.ViewModels;
using Avalonia;
using Avalonia.Controls;

namespace AtomUILabsGallery.Workspace.Views;

public partial class WorkspaceWindow : ReactiveWindow<WorkspaceWindowViewModel>
{
    private GalleryShellView? _shellView;

    public WorkspaceWindow()
    {
        ViewModel = new WorkspaceWindowViewModel();
        InitializeComponent();

        if (ViewModel is not null)
        {
            var showCaseNavigation = new CaseNavigation
            {
                Name      = "ShowCaseNavigation",
                ViewModel = ViewModel.CaseNavigation
            };
            _shellView = new GalleryShellView(
                AtomUILabsGalleryModule.GetConfiguration(),
                showCaseNavigation,
                ViewModel.Router);
            ShellHost.Children.Add(_shellView);
        }
    }

    protected override WindowTitleBar? NotifyCreateTitleBar(WindowTitleBar? oldTitleBar)
    {
        return new GalleryWindowTitleBar
        {
            Name = "PART_TitleBar"
        };
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _shellView?.Dispose();
        _shellView = null;
        ViewModel?.Dispose();
        base.OnDetachedFromVisualTree(e);
    }
}
