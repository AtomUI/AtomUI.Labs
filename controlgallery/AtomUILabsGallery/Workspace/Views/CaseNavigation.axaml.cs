using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using AtomUI.Controls;
using AtomUI.Controls.Primitives;
using AtomUI.Desktop.Controls;
using AtomUI.Theme;
using AtomUI.Theme.Language;
using AtomUI.Toolkits.GalleryBase.Controls;
using AtomUI.Toolkits.GalleryBase.Navigation;
using AtomUILabsGallery.Workspace.ViewModels;
using Avalonia;
using ReactiveUI;

namespace AtomUILabsGallery.Workspace.Views;

public partial class CaseNavigation : GalleryReactiveUserControl<CaseNavigationViewModel>
{
    private EventHandler<LanguageVariantChangedEventArgs>? _languageVariantChangedHandler;

    public CaseNavigation()
    {
        InitializeComponent();
        ConfigureNavigationMenu();

        this.WhenActivated(disposables =>
        {
            void NavMenuItemClickHandler(object? sender, NavMenuItemClickEventArgs args)
            {
                var key = args.NavMenuItem.ItemKey;
                if (key.HasValue &&
                    ViewModel is not null &&
                    ViewModel.CanNavigateTo(key.Value))
                {
                    ViewModel.NavigateToCommand.Execute(key.Value)
                             .Subscribe()
                             .DisposeWith(disposables);
                }
            }

            ShowCaseNavMenu.NavMenuItemClick += NavMenuItemClickHandler;
            Disposable.Create(() => ShowCaseNavMenu.NavMenuItemClick -= NavMenuItemClickHandler)
                      .DisposeWith(disposables);
        });
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        SubscribeLanguageChanged();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        UnsubscribeLanguageChanged();
        base.OnDetachedFromVisualTree(e);
    }

    private void ConfigureNavigationMenu()
    {
        var configuration = AtomUILabsGalleryModule.GetConfiguration();
        var adapter       = new GalleryNavigationMenuAdapter();

        ShowCaseNavMenu.Items.Clear();
        foreach (var node in adapter.BuildNodes(configuration.NavigationNodes))
        {
            ShowCaseNavMenu.Items.Add(node);
        }

        ShowCaseNavMenu.DefaultOpenPaths = adapter.BuildDefaultOpenPaths(configuration.DefaultOpenKeys);
    }

    private void SubscribeLanguageChanged()
    {
        if (_languageVariantChangedHandler is not null)
        {
            return;
        }

        var themeManager = Application.Current?.GetThemeManager();
        if (themeManager is null)
        {
            return;
        }

        _languageVariantChangedHandler = (_, _) => ConfigureNavigationMenu();
        themeManager.LanguageVariantChanged += _languageVariantChangedHandler;
    }

    private void UnsubscribeLanguageChanged()
    {
        if (_languageVariantChangedHandler is null)
        {
            return;
        }

        var themeManager = Application.Current?.GetThemeManager();
        if (themeManager is not null)
        {
            themeManager.LanguageVariantChanged -= _languageVariantChangedHandler;
        }

        _languageVariantChangedHandler = null;
    }
}
