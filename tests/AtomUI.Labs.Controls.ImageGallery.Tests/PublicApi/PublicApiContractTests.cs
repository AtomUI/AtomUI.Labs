using System.ComponentModel;
using System.Globalization;
using AtomUI.Labs.Controls.ImageGallery.Appearance;
using AtomUI.Theme;
using AtomUI.Theme.Language;
using AtomUI.Theme.Styling;
using Avalonia;
using Avalonia.Media;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Controls.ImageGallery.Tests.PublicApi;

public sealed class PublicApiContractTests
{
    public PublicApiContractTests()
    {
        AvaloniaTestApp.EnsureInitialized();
    }

    [Fact]
    public void DefaultsMatchConstructionContract()
    {
        var control = new ImageGallery();

        control.ZoomMode.ShouldBe(ImageGalleryZoomMode.Fit);
        control.CustomZoomFactor.ShouldBe(1);
        control.ZoomRange.ShouldBe(new ImageGalleryZoomRange(0.05, 32));
        control.ZoomStep.ShouldBe(1.2);
        control.WheelZoomMode.ShouldBe(ImageGalleryWheelZoomMode.Always);
        control.IsPinchZoomEnabled.ShouldBeTrue();
        control.IsPanEnabled.ShouldBeTrue();
        control.IsViewportNavigationEnabled.ShouldBeTrue();
        control.IsFilmstripNavigationEnabled.ShouldBeTrue();
        control.IsLoopNavigationEnabled.ShouldBeFalse();
        control.ToolbarPlacement.ShouldBe(ImageGalleryEdgePlacement.Top);
        control.EffectiveToolbarPlacement.ShouldBe(ImageGalleryEdgePlacement.Top);
        control.ThumbnailFilmstripPlacement.ShouldBe(ImageGalleryEdgePlacement.Bottom);
        control.ThumbnailFilmstripExtent.ShouldBe(120);
        control.ThumbnailItemExtent.ShouldBe(96);
        control.ThumbnailItemSpacing.ShouldBe(8);
        control.ThumbnailCacheMemoryBudgetBytes.ShouldBe(32L * 1024 * 1024);
        control.MainImageCacheMemoryBudgetBytes.ShouldBe(128L * 1024 * 1024);
        control.MainImagePrefetchMode.ShouldBe(ImageGalleryMainImagePrefetchMode.Adjacent);
        control.MainImageMode.ShouldBe(ImageGalleryMainImageMode.Presented);
        control.MainImageDecodeSizeHint.ShouldBeNull();
        control.LoadLimits.ShouldBe(ImageGalleryLoadLimits.Default);
        control.ImageState.ShouldBe(ImageGalleryImageState.Empty);
    }

    [Fact]
    public void RootOwnedCommandsAreStable()
    {
        var control = new ImageGallery();

        control.PreviousCommand.ShouldBeSameAs(control.PreviousCommand);
        control.NextCommand.ShouldBeSameAs(control.NextCommand);
        control.ZoomInCommand.ShouldBeSameAs(control.ZoomInCommand);
        control.RotateClockwiseCommand.ShouldBeSameAs(control.RotateClockwiseCommand);
        control.ZoomInCommand.CanExecute(null).ShouldBeFalse();
    }

    [Fact]
    public void AtomicRangesRejectInvalidValues()
    {
        var control = new ImageGallery();

        Should.Throw<ArgumentException>(() => control.ZoomRange = new ImageGalleryZoomRange(2, 1));
        Should.Throw<ArgumentException>(() => control.ZoomStep = 1);
        Should.Throw<ArgumentException>(() => control.LoadLimits = default);
        Should.Throw<ArgumentException>(() => control.ThumbnailItemExtent = 0);
        Should.Throw<ArgumentException>(() => control.ThumbnailItemSpacing = -1);
        Should.Throw<ArgumentException>(() => control.MainImageCacheMemoryBudgetBytes = -1);
        Should.Throw<ArgumentException>(() => control.MainImageDecodeSizeHint = new PixelSize(0, 100));
        Should.Throw<ArgumentException>(() => control.MainImageDecodeSizeHint = new PixelSize(100, -1));
    }

    [Fact]
    public void ZoomRangeConverterUsesInvariantPair()
    {
        var converter = TypeDescriptor.GetConverter(typeof(ImageGalleryZoomRange));

        var value = converter.ConvertFromInvariantString("0.1,16");

        value.ShouldBe(new ImageGalleryZoomRange(0.1, 16));
        Should.Throw<FormatException>(() => converter.ConvertFromInvariantString("16,0.1"));
    }

    [Fact]
    public void AppearanceTypesAreSealedAvaloniaObjects()
    {
        typeof(ImageGalleryViewportAppearance).IsSealed.ShouldBeTrue();
        typeof(ImageGalleryToolbarAppearance).IsSealed.ShouldBeTrue();
        typeof(ImageGalleryButtonAppearance).IsSealed.ShouldBeTrue();
        typeof(ImageGalleryFilmstripAppearance).IsSealed.ShouldBeTrue();
        typeof(ImageGalleryThumbnailItemAppearance).IsSealed.ShouldBeTrue();
    }

    [Fact]
    public void ThemeAndGeneratedLanguageRegistrationIsIdempotent()
    {
        var builder = new TestThemeManagerBuilder();

        builder.UseImageGallery();
        builder.UseImageGallery();

        builder.ControlThemesProviders.Count(provider => provider is ImageGalleryThemesProvider).ShouldBe(1);
        builder.LanguageProviders.Count.ShouldBe(3);
        builder.LanguageProviders.Select(provider => (provider.LangCode, provider.LangId)).Distinct().Count().ShouldBe(3);
    }

    private sealed class TestThemeManagerBuilder : IThemeManagerBuilder
    {
        public IList<Type> ControlDesignTokens { get; } = [];
        public IList<IThemeAssetPathProvider> ThemeAssetPathProviders { get; } = [];
        public IList<IControlThemesProvider> ControlThemesProviders { get; } = [];
        public IList<LanguageProvider> LanguageProviders { get; } = [];
        public IList<EventHandler> InitializedHandlers { get; } = [];
        public LanguageVariant LanguageVariant { get; private set; } = LanguageVariant.zh_CN;
        public string ThemeId { get; private set; } = IThemeManager.DEFAULT_THEME_ID;

        public void AddControlToken(Type tokenType) => ControlDesignTokens.Add(tokenType);
        public void AddControlThemesProvider(IThemeAssetPathProvider themeAssetPathProvider) => ThemeAssetPathProviders.Add(themeAssetPathProvider);
        public void AddControlThemesProvider(IControlThemesProvider controlThemesProvider) => ControlThemesProviders.Add(controlThemesProvider);
        public void AddLanguageProviders(LanguageProvider languageProvider) => LanguageProviders.Add(languageProvider);
        public void WithDefaultTheme(string themeId) => ThemeId = themeId;
        public void WithDefaultFontFamily(FontFamily fontFamily)
        {
        }

        public void WithDefaultFontFamily(string fontFamily)
        {
        }

        public void WithDefaultCultureInfo(CultureInfo cultureInfo) =>
            LanguageVariant = LanguageVariant.FromCultureInfo(cultureInfo);

        public void WithDefaultLanguageVariant(LanguageVariant languageVariant) => LanguageVariant = languageVariant;
        public void WithThemeVariantCalculatorFactory(IThemeVariantCalculatorFactory factory)
        {
        }
    }
}
