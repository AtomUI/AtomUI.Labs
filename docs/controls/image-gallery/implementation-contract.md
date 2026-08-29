# ImageGallery 首版施工合同

> 文档状态：首版施工标准与实现基线，更新于 2026-08-24。S0～S10 工程施工、主图资源消费扩展及单固定种子 30 分钟 Headless 长稳已落地；自动验证证据与仍待执行的人眼/真实图片闸门见两份专项验证文档。本文继续作为公共 API、内部职责、测试和发布边界的唯一施工合同。

## 文档权威关系

ImageGallery 的文档按以下职责使用：

1. 本文是施工阶段的唯一规范入口，回答“必须实现什么、名称是什么、默认值是什么、怎样才算完成”。
2. [控件设计稿](overview.md)保存设计理由、算法推导、交互图和被否决方案，回答“为什么这样设计”。
3. [缩略图虚拟化测试与性能验收设计](thumbnail-virtualization-verification.md)规定海量缩略图、容器数量、加载、缓存和长稳证据。
4. [整体性能、NativeAOT 与 Gallery 发布验收设计](release-and-gallery-verification.md)规定整体性能、NativeAOT、NuGet、Gallery 和候选发布闸门。

施工人员不得从设计稿中重新拼接另一套公共 API。本文与设计稿发生矛盾时必须暂停相关施工，先同步修正文档；不得选择对实现最方便的一项，也不得只改源码或只改其中一份文档。

本文中的“必须”“不得”“首版固定”均为强制合同。“建议”才允许在不改变可观察语义的前提下调整。

## 交付目标

首版交付独立实验控件包 AtomUI.Labs.Controls.ImageGallery。根控件 ImageGallery 是面向静态光栅图片集合的沉浸式查看器，由以下三部分组成：

~~~text
ImageGallery
├── Viewport
│   ├── 当前主图绘制
│   ├── Empty / Loading / Error 状态视觉
│   └── Previous / Next 显式导航按钮
├── Toolbar
│   ├── Title
│   ├── Zoom
│   ├── Rotation
│   └── ToolbarContent，自定义内容固定在末端
└── Thumbnail Filmstrip
    ├── Add Image
    ├── Previous
    ├── 虚拟化 Thumbnail Items
    └── Next
~~~

根控件继承 Avalonia SelectingItemsControl，SelectionModel 是当前选择的唯一权威。主图、标题、缩略图选中态、相邻导航和加载请求都只能从同一选择提交派生。

首版明确不交付：

- 点击主图左右边缘区域导航；只保留显式按钮。
- 动画图片、SVG、专业色彩管理或图片编辑、重编码、EXIF 写回。
- 自动重试、公开异常对象、Retry/Reload API。
- 任意角度或逆时针旋转、导航平滑动画、弹性平移和惯性。
- 分页数据 Provider、Descriptor 窗口化或真正的无限数据源。
- 控件级独立 Culture、公开并发槽位、磁盘缩略图缓存。
- 根模板和内部子组件模板替换。
- 首版键盘快捷导航和屏幕阅读器专项增强；对应工作保留为后续 TODO。

## 工程与依赖边界

### 项目身份

| 项目 | 固定值 |
|---|---|
| Package / Project | AtomUI.Labs.Controls.ImageGallery |
| 主命名空间 | AtomUI.Labs.Controls.ImageGallery |
| 测试项目 | AtomUI.Labs.Controls.ImageGallery.Tests |
| AXAML Namespace | https://atomui.net/labs |
| 推荐前缀 | atom.labs |
| Debug TFM | net10.0 |
| Release TFM | net10.0;net8.0 |

项目、版本、集中输出和打包必须服从仓库根构建规则，不得在子项目中重复定义 AtomUILabsVersion 或私自改变输出目录。

### 允许依赖

- Avalonia 基础包。
- AtomUI.Core。
- AtomUI.Generator，仅作为编译期生成器，并设置 PrivateAssets=all。

### 禁止依赖

- AtomUI.Controls。
- AtomUI.Controls.Shared。
- AtomUI.Desktop.Controls 及其中任何成型控件。
- AtomUI.Toolkits.GalleryBase。
- LED 或其他 Labs 控件包。

ImageGallery 的按钮、工具栏、缩略图项、Overlay Panel 和 Viewport Presenter 必须在本包内实现为 internal sealed 辅助控件。不得为了节省代码把 AtomUI 已成型控件重新引入依赖图。

### 目标目录

目录按领域职责组织，不创建 Api、Contracts、Managers、Helpers、Common 或 Shared 等泛化桶：

~~~text
src/AtomUI.Labs.Controls.ImageGallery/
├── AtomUI.Labs.Controls.ImageGallery.csproj
├── ImageGallery.cs
├── ImageGalleryThemesProvider.axaml
├── ImageGalleryThemesProvider.axaml.cs
├── ImageGalleryThemeManagerBuilderExtensions.cs
├── Properties/
│   └── AssemblyInfo.cs
├── Data/
│   ├── IImageGalleryItem.cs
│   ├── ImageGalleryItem.cs
│   └── ImageGalleryDescriptor.cs
├── Sources/
│   ├── IImageGallerySource.cs
│   ├── ImageGallerySources.cs
│   ├── ImageGalleryImageRequest.cs
│   ├── ImageGalleryLoadLimits.cs
│   └── ImageGalleryImageLease.cs
├── Selection/
│   └── ImageGalleryCoordinator.cs
├── Loading/
│   ├── ImageGalleryLoadScheduler.cs
│   ├── ImageGalleryLoadCoordinator.cs
│   ├── ImageGalleryCacheKey.cs
│   ├── SharedImageResource.cs
│   └── CurrentImageResourceSlot.cs
├── Viewport/
│   ├── ImageGalleryViewportPresenter.cs
│   └── ImageGalleryViewportState.cs
├── Overlay/
│   ├── ImageGalleryOverlayPanel.cs
│   ├── ImageGalleryToolbar.cs
│   └── ImageGalleryNavigationButton.cs
├── Filmstrip/
│   ├── ImageGalleryFilmstrip.cs
│   ├── ImageGalleryThumbnailItem.cs
│   └── ImageGalleryVirtualizingPanel.cs
├── Appearance/
│   └── ImageGallery*Appearance.cs
├── Localization/
│   └── ImageGalleryLang/
│       ├── zh_CN.cs
│       ├── zh_TW.cs
│       └── en_US.cs
└── Themes/
    ├── ImageGalleryThemes.axaml
    ├── ImageGalleryTheme.axaml
    └── Internal/
        ├── ToolbarTheme.axaml
        ├── ImageGalleryButtonTheme.axaml
        ├── FilmstripTheme.axaml
        └── ThumbnailItemTheme.axaml
~~~

文件可以在职责被证明后进一步拆分，但公共类型不得散入内部视觉目录，内部实现不得反向泄漏成公共 API。

配套工程固定为：

~~~text
tests/AtomUI.Labs.Controls.ImageGallery.Tests/
tests/AtomUI.Labs.Controls.ImageGallery.Benchmarks/
controlgallery/AtomUILabsGallery/Controls/ImageGallery/
~~~

Benchmark 项目必须设置 IsPackable=false。Gallery 可以使用自己的 Desktop Controls 和 GalleryBase 组织 ShowCase，但这些依赖绝不能进入正式 ImageGallery 包。

## 公共类型清单

下列类型和成员名称是首版公共面。未经同步修改本文，不得另造同义 API。

### 注册入口

注册方式与现有 Labs 控件一致：

~~~csharp
public static class ImageGalleryThemeManagerBuilderExtensions
{
    public static IThemeManagerBuilder UseImageGallery(
        this IThemeManagerBuilder themeManagerBuilder);
}
~~~

UseImageGallery() 必须返回传入 Builder 以支持链式调用，并以静态引用注册 ImageGalleryThemesProvider 和语言 Provider。重复调用不得产生重复资源、重复语言事件订阅或另一套控件级语言状态。

### 集合项目

~~~csharp
public interface IImageGalleryItem
{
    object Key { get; }
    string? Title { get; }
    IImageGallerySource MainImageSource { get; }
    IImageGallerySource? ThumbnailImageSource { get; }
}

public sealed class ImageGalleryItem : IImageGalleryItem
{
    public required object Key { get; init; }
    public string? Title { get; init; }
    public required IImageGallerySource MainImageSource { get; init; }
    public IImageGallerySource? ThumbnailImageSource { get; init; }
    public object? Data { get; init; }
}
~~~

ItemsSource 保留 SelectingItemsControl 继承的 IEnumerable? 形态。集合中的每个项目必须非 null 并实现 IImageGalleryItem。Key 和 MainImageSource 必须非 null，Key 在当前集合内唯一且相等性稳定。

控件只读取接口，不提供 MainImageSourceBinding、ThumbnailSourceBinding、TitleBinding、KeyBinding，不使用反射、字符串属性路径或 Converter 猜测数据字段。

### 图片源与 Lease

~~~csharp
public interface IImageGallerySource
{
    object Identity { get; }

    ValueTask<ImageGalleryImageLease> LoadAsync(
        ImageGalleryImageRequest request,
        CancellationToken cancellationToken);
}

public enum ImageGalleryImagePurpose
{
    MainImage,
    Thumbnail
}

public readonly record struct ImageGalleryLoadLimits(
    long MaximumEncodedBytes,
    long MaximumSourcePixelCount,
    int MaximumDimension,
    long MaximumDecodedBytes)
{
    public static ImageGalleryLoadLimits Default { get; }
}

public readonly record struct ImageGalleryImageRequest(
    ImageGalleryImagePurpose Purpose,
    PixelSize? TargetPixelSize,
    ImageGalleryLoadLimits Limits);
~~~

ImageGalleryLoadLimits.Default 固定为：

| 字段 | 默认值 |
|---|---:|
| MaximumEncodedBytes | 256 MiB |
| MaximumSourcePixelCount | 100,000,000 |
| MaximumDimension | 32,768 |
| MaximumDecodedBytes | 512 MiB |

四个值必须为正整数并作为一个原子值提交。内置 Source 必须在危险分配前执行累计读取字节、图片头尺寸、像素总数、单边尺寸和目标解码内存检查。

~~~csharp
public static class ImageGallerySources
{
    public static IImageGallerySource FromFile(
        string path,
        object? identity = null);

    public static IImageGallerySource FromAvaloniaResource(
        Uri uri,
        object? identity = null);

    public static IImageGallerySource FromHttp(
        Uri uri,
        HttpClient httpClient,
        object? identity = null);

    public static IImageGallerySource FromStream(
        object identity,
        Func<CancellationToken, ValueTask<Stream>> openStream);

    public static IImageGallerySource Create(
        object identity,
        Func<ImageGalleryImageRequest,
             CancellationToken,
             ValueTask<ImageGalleryImageLease>> loader);
}
~~~

FromFile、FromAvaloniaResource、FromHttp 和 FromStream 是常规入口；Create 是需要读取请求用途、目标尺寸、专用 SDK 或自定义释放策略时的高级逃生口。不得提供接受单个既有 Stream 的重载。

内置 Source 首版保证 PNG、JPEG/JPG、BMP 和静态 WebP。GIF、APNG、动态 WebP、SVG、TIFF、ICO、HEIF/HEIC、AVIF 不在保证范围。多帧图片不得静默退化为第一帧。支持格式的 EXIF Orientation 必须在返回 Lease 前修正。

Source 合同固定为：

- Identity 必须非 null，并在使用期间保持稳定的 Equals 和 GetHashCode。
- LoadAsync 可以在后台调用，同一个 Source 可能因不同 Purpose 和目标尺寸并发调用；实现必须线程安全，不得同步阻塞 UI 线程。
- LoadAsync 返回的 IImage 必须能够安全地交给 Avalonia UI 线程绘制；自定义 IImage 不得持有在后台加载线程创建的 SolidColorBrush 等线程关联 AvaloniaObject，应使用线程安全的解码图片资源、不可变绘制资源或在 UI 线程接管后创建视觉资源。
- FromFile 每次加载重新打开只读文件，不长期占用句柄；默认身份是规范化绝对路径。
- FromAvaloniaResource 只接受绝对 avares:// URI；默认身份是规范化绝对 URI。
- FromHttp 只接受绝对 HTTP/HTTPS URI；开发者拥有并复用传入 HttpClient，Source 不得 Dispose 客户端。
- FromStream 每次调用 openStream 都必须取得本次专用的新 Stream；Stream Factory 和 Create 必须显式提供身份。
- File、Resource 和 HTTP 的显式 identity 始终优先于默认身份。底层内容变化时开发者必须同步改变身份。
- 自定义 Source 必须主动遵守 request.Limits；Gallery 在 Lease 返回后再次校验尺寸和可得内存估算，但事后校验不构成恶意媒体沙箱。

~~~csharp
public sealed class ImageGalleryImageLease : IDisposable
{
    public IImage Image { get; }
    public PixelSize SourcePixelSize { get; }
    public PixelSize DecodedPixelSize { get; }
    public long? EstimatedMemorySizeBytes { get; }

    public static ImageGalleryImageLease Create(
        IImage image,
        PixelSize sourcePixelSize,
        PixelSize decodedPixelSize,
        long? estimatedMemorySizeBytes,
        Action release);

    public void Dispose();
}
~~~

Lease 是 sealed class，只同步实现 IDisposable，不实现 IAsyncDisposable 和终结器。Dispose 必须线程安全、幂等，并且只执行一次 release。Stream、HTTP Response 和厂商请求对象必须在 LoadAsync 返回前释放，Lease 只持有可绘制 IImage 资源。

ImageGallery 自己和外部消费者必须通过同一个 `SharedImageResource` 引用计数所有权域持有图片。缓存拥有基础引用，当前资源槽拥有 Gallery 专用 Lease；缓存淘汰不得破坏当前槽，Gallery 离开视觉树也不得破坏已经交给调用方的独立 Lease。`SharedImageResource.Dispose()`、当前资源槽和所有 Lease 的释放必须线程安全、幂等，基础 Source Lease 的 release 最终只能执行一次。

### 枚举和值对象

~~~csharp
public enum ImageGalleryZoomMode
{
    Fit,
    ActualSize,
    Custom
}

public readonly record struct ImageGalleryZoomRange(
    double Minimum,
    double Maximum);

public enum ImageGalleryWheelZoomMode
{
    Disabled,
    Always,
    ControlModifier
}

public enum ImageGalleryEdgePlacement
{
    Top,
    Bottom,
    Left,
    Right
}

public enum ImageGalleryEdgeAlignment
{
    Start,
    Center,
    End,
    Stretch
}

public enum ImageGalleryResponsiveState
{
    Normal,
    Compact,
    Minimal
}

public enum ImageGalleryImageState
{
    Empty,
    Loading,
    Ready,
    Error
}

public enum ImageGalleryMainImageMode
{
    Presented,
    ResourceOnly
}

public enum ImageGalleryMainImagePrefetchMode
{
    Disabled,
    Adjacent
}
~~~

ImageGalleryZoomRange 必须提供与编译 AXAML 兼容、使用固定文化格式的转换器，使 ZoomRange="0.05,32" 可用。

### 根控件属性

根控件声明为：

~~~csharp
public class ImageGallery : SelectingItemsControl
{
}
~~~

行为与布局 StyledProperty 的首版名称和默认值如下：

| 分组 | 属性 | 默认值 |
|---|---|---|
| Zoom | ZoomMode | Fit |
| Zoom | CustomZoomFactor | 1.0 |
| Zoom | IsFitUpscalingEnabled | false |
| Zoom | ZoomRange | new(0.05, 32.0) |
| Zoom | ZoomStep | 1.2 |
| Input | WheelZoomMode | Always |
| Input | IsPinchZoomEnabled | true |
| Input | IsPanEnabled | true |
| Navigation | IsViewportNavigationEnabled | true |
| Navigation | IsFilmstripNavigationEnabled | true |
| Navigation | IsLoopNavigationEnabled | false |
| Toolbar | IsToolbarTitleVisible | true |
| Toolbar | IsToolbarZoomControlsVisible | true |
| Toolbar | IsToolbarRotationVisible | true |
| Toolbar | IsToolbarVisible | true |
| Toolbar | ToolbarContent | null |
| Toolbar | ToolbarContentTemplate | null |
| Toolbar | ToolbarPlacement | Top |
| Toolbar | ToolbarEdgeGap | 16.0 |
| Toolbar | ToolbarAlongEdgeInset | 16.0 |
| Toolbar | OverlayElementGap | 8.0 |
| Toolbar | ToolbarRegionSpacing | 16.0 |
| Toolbar | ToolbarItemSpacing | 8.0 |
| Toolbar | ToolbarAlongEdgeAlignment | Center |
| Filmstrip | ThumbnailFilmstripPlacement | Bottom |
| Filmstrip | ThumbnailFilmstripExtent | 120.0 |
| Filmstrip | ThumbnailFilmstripEdgeGap | 16.0 |
| Filmstrip | ThumbnailFilmstripAlongEdgeInset | 16.0 |
| Filmstrip | IsThumbnailFilmstripVisible | true |
| Filmstrip | ThumbnailItemExtent | 96.0 |
| Filmstrip | ThumbnailItemSpacing | 8.0 |
| Filmstrip | ThumbnailCacheMemoryBudgetBytes | 32 MiB |
| Add | IsAddImageButtonVisible | true |
| Add | AddImageCommand | null |
| Add | AddImageCommandParameter | null |
| Loading | LoadLimits | ImageGalleryLoadLimits.Default |
| Loading | MainImagePrefetchMode | Adjacent |
| Loading | MainImageCacheMemoryBudgetBytes | 128 MiB |
| Main image | MainImageMode | Presented |
| Main image | MainImageDecodeSizeHint | null |

以下状态必须是只读 DirectProperty，不允许外部写入：

| 属性 | 类型 |
|---|---|
| EffectiveZoomFactor | double |
| IsPinchZooming | bool |
| IsPanning | bool |
| RotationAngle | int，只允许 0、90、180、270 |
| EffectiveToolbarPlacement | ImageGalleryEdgePlacement |
| ResponsiveState | ImageGalleryResponsiveState |
| ImageState | ImageGalleryImageState |

以下命令由根控件创建并持有，实例在控件生命周期内稳定，外部只读：

- PreviousCommand。
- NextCommand。
- ZoomInCommand。
- ZoomOutCommand。
- FitCommand。
- ActualSizeCommand。
- RotateClockwiseCommand。

同时公开 RotateClockwise()，它与 RotateClockwiseCommand 进入同一旋转协调路径。AddImageCommand 是唯一由开发者注入的命令。

SelectedIndex、SelectedItem、SelectionChanged 和 ItemsSource 使用 SelectingItemsControl 已有公共合同，不重复创建同义属性或事件。

根控件额外公开当前逻辑主图的安全消费面：

~~~csharp
public bool TryAcquireCurrentImage(
    IImageGalleryItem expectedItem,
    out ImageGalleryImageLease? lease);

public event EventHandler? CurrentImageResourceChanged;
~~~

`TryAcquireCurrentImage` 只能在 Avalonia UI 线程调用；`expectedItem=null` 抛出 `ArgumentNullException`。只有 `ImageState=Ready`，且 expected Item 的稳定 Key、Source Identity、当前 Descriptor 和请求代次全部仍与当前资源槽一致时才返回 true。成功时返回一份新的调用方所有 Lease；失败返回 false 和 null。不得把 Gallery 自己的 Lease 或缓存基础引用直接交给外部。外部 Lease 不计入 `MainImageCacheMemoryBudgetBytes`，调用方必须 Dispose；Gallery 切图、缓存淘汰或 Detach 不得使仍在外部持有的 Lease 失效。

`CurrentImageResourceChanged` 是无 Lease 载荷的普通 CLR 事件，只在 UI 线程、状态和资源槽完成原子提交之后触发。触发矩阵固定为：选择开始使资源不可取得、首次成功提交、成功清晰度升级、当前失败、清空和 Detach。仅切换 `MainImageMode`、失败/无提升/过期的清晰度候选不得触发。事件处理器需要资源时必须在回调中再次调用 `TryAcquireCurrentImage`。

### Appearance 公共面

首版公开五种 sealed AvaloniaObject：

- ImageGalleryViewportAppearance。
- ImageGalleryToolbarAppearance。
- ImageGalleryButtonAppearance。
- ImageGalleryFilmstripAppearance。
- ImageGalleryThumbnailItemAppearance。

根控件公开八个可空 StyledProperty：

- ViewportAppearance。
- ToolbarAppearance。
- ToolbarButtonAppearance。
- ViewportNavigationButtonAppearance。
- FilmstripNavigationButtonAppearance。
- FilmstripAppearance。
- ThumbnailItemAppearance。
- AddImageButtonAppearance。

null 表示全部使用包内 Theme。Appearance 是稀疏覆盖对象；每个成员必须是 StyledProperty，显式设置和未设置必须通过 Avalonia 属性值来源区分。共享 Appearance 资源不得被控件修改。

| 类型 | 必须开放的成员 |
|---|---|
| Viewport | Background、BorderBrush、BorderThickness、CornerRadius、Opacity、EmptyForeground、LoadingForeground、ErrorForeground |
| Toolbar | Background、Foreground、BorderBrush、BorderThickness、CornerRadius、Opacity、Padding、Width、Height、MinWidth、MaxWidth、MinHeight、MaxHeight、ItemSpacing |
| Button | Background、Foreground、BorderBrush、BorderThickness、CornerRadius、Opacity、Padding、Width、Height、IconSize；PointerOver、Pressed、Disabled 三组 Background、Foreground、BorderBrush；DisabledOpacity |
| Filmstrip | Background、BorderBrush、BorderThickness、CornerRadius、Opacity、Padding |
| Thumbnail Item | Background、PointerOverBackground、SelectedBackground、BorderBrush、BorderThickness、CornerRadius、Opacity、Padding、SelectionIndicatorBrush、SelectionIndicatorThickness、LoadingForeground、ErrorForeground |

`ImageGalleryButton` 只继承 Avalonia `Button` 的点击、Command、CanExecute、Pointer Capture 和输入语义，必须使用包内编译的固定 `ImageGalleryButtonTheme` 渲染；不得继承或 BasedOn 宿主应用的原生 Button Theme，也不得让宿主 Theme 的 ContentPresenter 状态选择器覆盖 Appearance。Toolbar、Viewport Navigation、Filmstrip Navigation 和 Add Image 共享该内部 Theme，通过各自根 Appearance 属性提供不同参数。

Navigation、Add、Toolbar Zoom In、Toolbar Zoom Out 与 Toolbar Rotate 的固定图标必须由包内几何矢量 Glyph 绘制，不得使用存在字体基线偏移的文本字符。Glyph 的布局正方形边长由 Button Appearance 的 `IconSize` 决定，中心必须与 Button 内容区中心重合。Filmstrip LayoutPanel 必须使用 Add、Previous、Next 的 DesiredSize 在交叉轴居中；Items Viewport 与 Thumbnail Item 的交叉轴视觉中心必须与按钮一致。

按钮状态解析顺序固定为 Disabled、Pressed、PointerOver、Normal。内部按钮负责根据 Appearance 和 `IsEffectivelyEnabled` 计算根 Background、Foreground、BorderBrush 与 Opacity，内部 Theme 的 ContentPresenter 只做 TemplateBinding，不建立第二套状态颜色。Appearance 为 null 时，内部 Theme 使用 AtomUI.Core Token 提供默认 Normal、PointerOver、Pressed 与 Disabled 反馈。Theme、模板、内部类型和资源 Key 均不得公开。

画刷成员使用 IBrush?，边距和边框使用 Thickness，圆角使用 CornerRadius，透明度、尺寸和间距使用 double。Opacity 必须是有限的 0 到 1；尺寸、间距、Padding、Thickness、CornerRadius 和 IconSize 必须满足对应的有限、非负约束。

SelectionIndicatorBrush 和 SelectionIndicatorThickness 构成固定选中轮廓。显式 null Brush 或全零 Thickness 属于非法配置。选中轮廓只在已有 Item Bounds 内绘制，不参与 Measure，不改变虚拟槽位步长。

任何 Appearance 都不得包含 ControlTheme、Template、Styles、DataTemplate、Content 或行为属性。

## 数据集合与选择合同

### 集合模型

ItemsSource 必须是一次枚举能够结束的有限 IEnumerable：

- 普通数组或 List 在赋值时完整枚举为快照。
- INotifyCollectionChanged 支持 Add、Remove、Move、Replace 和 Reset。
- 动态场景推荐 ObservableCollection<IImageGalleryItem>。
- 所有赋值、初次枚举和集合通知必须发生在 Avalonia UI 线程。
- 非 UI 线程通知立即抛出 InvalidOperationException，不进行部分提交或静默调度。
- 控件保存全部项目引用和轻量 Descriptor，数据侧空间复杂度为 O(N)。
- 缩略图视觉、Lease、请求和缓存才由虚拟化与预算约束。

初次快照、Reset 或集合变更发现 null 项、错误类型、null Key、重复 Key、null MainImageSource 或不稳定身份时，必须快速失败，不能跳过无效项，也不能把数据合同错误呈现成图片 Error。

### 选择不变量

- 空集合：SelectedIndex=-1、SelectedItem=null、ImageState=Empty，Filmstrip 结构隐藏。
- 非空集合：始终保持一项合法选择；没有有效选择时自动选择第一项。
- 单项集合：Filmstrip 仍显示，Previous/Next 可见但禁用；Add Image 按配置显示。
- 相邻导航每次只改变一个稳定顺序中的项目，不翻页、不跳过失败项。
- 非循环首尾禁用对应方向；循环仅在项目数大于 1 时生效。
- 导航按钮在 Loading 和 Error 状态仍可用，只取决于集合和选择边界。
- 快速连续导航立即提交最新合法选择，不排队；旧主图请求取消并失去提交资格。
- 已经存在 Ready 主图时，新选择进入 Loading 但继续绘制上一张已提交帧，直到最新合法候选成功提交、当前候选失败、集合变空或控件 Detach；该保留帧只是防闪烁的视觉底图，不取得新选择身份，也不能启用 Zoom、Pan 或 Rotation。

集合变化规则固定为：

- Add/Insert：空集合变非空时选择索引 0；已有选择时按 Key 保持当前业务项，仅修正 SelectedIndex，新项不得抢走选择。
- Remove/RemoveRange：未删除当前 Key 时按身份保持；删除当前项时优先选择占据删除起始索引的后继，没有后继则选择前驱，即 newIndex=min(removedStartIndex, newCount-1)。
- Replace：非当前项只更新其 Descriptor。替换当前项时先保持原索引，再比较新旧 Key；Key 改变视为新选择。Key 相同再比较 MainImageSource.Identity；Source Identity 改变则重置并重载，两级身份都相同才只更新元数据并保留当前 Lease 和 Viewport 状态。
- Move：始终按 Key 保持当前业务项，只修正 SelectedIndex、缩略图位置和导航边界，不增加请求版本或重置 Viewport。
- Reset 或替换整个 ItemsSource：先完整验证 Key；按旧 Key 恢复恰好一个同一逻辑项，再按 Source Identity 决定保留或重载。旧 Key 不存在时选择 min(oldSelectedIndex, newCount-1)；旧时无选择则选索引 0。

上述变化必须由每个根 ImageGallery 独有的 ImageGalleryCoordinator 在 UI 线程一次性提交。Coordinator 不是应用级单例；Viewport、Toolbar 和 Filmstrip 不得直接互相写状态，只能向 Coordinator 提交选择、导航或变换意图，再观察统一结果。任何集合变化都不能让 SelectedItem、SelectedIndex、Descriptor、当前 Source 和主图身份出现可见的半提交。

SelectionChanged 的同步重入保护仍是首版之后 TODO；首版测试必须固定当前 Avalonia 事件时序，避免实现内部自行制造重入。

## Viewport 施工标准

### 绘制架构

主图 Viewport 不使用内部 ScrollViewer，也不使用普通 Image 加 RenderTransform。ImageGalleryViewportPresenter 继承 Avalonia Control，直接绘制当前 Lease 的 IImage，并维护：

~~~text
ViewportSize
SourcePixelSize
RenderScaling
EffectiveZoomFactor
RotationAngle
PanOffset
DestinationRect
~~~

Viewport Presenter 是 internal sealed，不开放 Content。Stream 不得进入 Render；只允许绘制尚未 Dispose 的 Lease。

ActualSize 表示一个源图片像素对应一个设备像素：

~~~text
ActualWidthDip  = SourcePixelWidth  / RenderScaling
ActualHeightDip = SourcePixelHeight / RenderScaling
~~~

Fit 使用 Viewport 物理像素与源像素计算。默认不放大小图；IsFitUpscalingEnabled=true 时才允许放大，并受 ZoomRange.Maximum 约束。Fit 可以低于 ZoomRange.Minimum，ActualSize 固定为 1.0。

CustomZoomFactor 的交互请求超出 ZoomRange 时 Clamp；非法 ZoomRange 或 ZoomStep 是配置异常。ZoomStep 必须有限且大于 1。

所有 Zoom、Pan、Rotation、Viewport Bounds、RenderScaling 和图片变化必须进入同一个 ReconcileViewportState 语义入口，一次计算比例、旋转后尺寸、平移边界和 DestinationRect，再请求绘制。不得把缩放和位移拆成两个可见阶段。

### 输入

- WheelZoomMode=Always 时，指针位于 Viewport 的滚轮缩放并消费事件。
- ControlModifier 仅在 Ctrl+Wheel 时缩放，否则交给外层。
- Disabled 不处理滚轮缩放。
- 滚轮以指针为锚点；命令以 Viewport 中心为锚点。
- Pinch 使用累计倍率和双指中心，不能循环调用离散 Zoom 命令。
- Pinch、鼠标 Panning 和滚轮缩放互斥，统一经过原子变换核心。
- 图片没有超出 Viewport 的轴强制 PanOffset=0。
- 首版不公开 PanOffset，不允许拖出空白，不做惯性、回弹或平滑动画。

鼠标平移状态固定为：

~~~text
Idle -> PressCandidate -> Panning + Pointer Capture -> Idle
~~~

按下不能立即平移；移动超过平台拖拽阈值且至少一个轴可移动时才进入 Panning。图片切换、Lease 替换、Detach、进入非 Ready、禁用平移或 Capture 丢失必须走同一个取消入口。

Pinch 生命周期固定为：

~~~text
Idle -> Pinching + Gesture Ownership -> Idle
~~~

Selection Identity、Source Identity、Lease、生命周期代次或配置发生使旧快照失效的变化时，迟到手势不得提交。

### 旋转与图片切换

RotateClockwise 每次顺时针旋转 90 度。旋转只是查看状态，不修改图片文件、业务项或 Source Identity。切换到不同 Selection Identity 或 Source Identity 时必须：

1. 取消当前手势、拖拽和 Pointer Capture。
2. 取消旧请求并增加请求代次，使全部旧结果失去提交资格。
3. 进入新选择 Loading；已有 Ready Lease 暂时作为不可交互的保留帧继续绘制，且保持旧图的几何状态，避免加载开始时发生空帧或跳变。
4. 最新合法候选 Lease 到达后，在一次 UI 线程提交中安装新 Lease、恢复 Fit、将 RotationAngle 归零、清除 PanOffset、进入 Ready 并请求重绘。
5. 新状态安装成功后才释放旧当前 Lease。当前候选失败、集合变空或 Detach 时则清除并释放保留 Lease，不回退为旧选择的 Ready。

## Overlay、Toolbar 与导航

### Placement

ThumbnailFilmstripPlacement 和 ToolbarPlacement 都相对于 ImageGallery 根 Bounds，不读取 Window 或屏幕边界。Top/Bottom 自动使用水平主轴，Left/Right 自动使用垂直主轴，不公开独立 Orientation。

ToolbarPlacement 是开发者请求值，EffectiveToolbarPlacement 是实际只读值。Filmstrip 与 Toolbar 请求同一边时，Toolbar 确定性移动到对边，绝不回写 ToolbarPlacement。

相邻边发生拐角冲突时执行：

1. Filmstrip 位置和完整交互优先。
2. Toolbar 在原有效边的安全线段中缩短。
3. Toolbar 无法完整使用时进入 Compact 并结构隐藏。
4. Filmstrip 连 Add、导航、至少一个完整 Item 和间距都放不下时进入 Minimal 并结构隐藏。
5. Viewport 始终保留。

响应式状态顺序固定为 Normal -> Compact -> Minimal，恢复阈值有内部 8 DIP 滞回，不公开配置，不做动画。空间优先级为 Filmstrip、Toolbar、Viewport Navigation、装饰；默认 Z-Index 为 Viewport 0、Viewport Navigation 10、Toolbar 20、Filmstrip 30。

Viewport Navigation 的公开总开关仍是 `IsViewportNavigationEnabled`，但模板必须绑定内部派生的 Previous/Next 有效可见状态。有效显示的 Filmstrip 或 Toolbar 位于 Left 时结构隐藏 Previous，位于 Right 时结构隐藏 Next；左右两侧均被占用时两个按钮都隐藏，Top/Bottom 不影响。该规则按“同侧存在”判定，不做矩形相交测试。空集合、显式关闭、无有效 Toolbar 内容或响应式结构隐藏的边栏不算占用；Toolbar 必须读取 `EffectiveToolbarPlacement`。结构隐藏必须令 `IsVisible=false`，不能只安排零 Bounds 或依靠 Z-Index 覆盖。

ImageGalleryOverlayPanel 必须统一 Measure/Arrange Viewport、导航、Toolbar 和 Filmstrip。内部子组件不得通过 Margin 各自猜测别人的位置。

### Toolbar

默认逻辑顺序固定为 Title、Zoom、Rotation、ToolbarContent。ToolbarContent 只能在末端，不允许插入默认区域之间，也不能替换 Toolbar 外壳。

Toolbar 内容组必须在外壳可用内容区内水平居中。Top/Bottom 的横向布局中，Title、缩放百分比、全部按钮和 ToolbarContent 必须共享同一条水平中心线；Left/Right 的竖向布局中，全部角色必须共享同一条垂直中心轴。实现必须使用能在 Arrange 阶段逐项求解交叉轴居中的专用内部 Panel，不能依赖 StackPanel 对不同高度或宽度子项的 Alignment 推测。ToolbarContent Presenter 作为一个整体双轴居中，但不得改写开发者自定义内容内部的 Panel、Orientation 或子项布局。

共同中心线必须覆盖内部可见内容，不能只验证 Button/Text 控件的外层 Bounds。图标角色使用以正方形 ViewBox 中心构造的几何 Glyph；文字角色依据 `TextLayout.Height`、`Extent` 和 `OverhangAfter` 求出可见墨迹中心并把它安排到控件中心。禁止以某个字体、语言或 DPI 为前提给单个按钮写死像素偏移。内部 Button Theme 的根 ContentPresenter 必须双轴 Stretch，内容再按 Button 的 ContentAlignment 居中，保证按钮背景、命中区域和内容中心是三个可分别验证的层次。

三个默认区域可以分别隐藏；隐藏后必须移除布局、命中、分隔和间距。当三个默认区域全部隐藏且 ToolbarContent=null 时，Toolbar 结构隐藏。IsToolbarVisible=false 的优先级最高。

ToolbarContent 沿用正常 Avalonia DataContext 继承。需要当前项时，开发者显式绑定祖先 ImageGallery.SelectedItem；不得把内部 Coordinator 注入为 DataContext。

### 相邻导航

Viewport 与 Filmstrip 两组按钮都绑定同一 PreviousCommand 和 NextCommand。Filmstrip 按钮改变相邻选择，不承担走廊翻页。走廊自身 Wheel、Touchpad 或直接滚动只改变缩略图可视窗口，不改变 Selection。

`PART_Filmstrip` 的完整视觉矩形是统一的 Wheel/Touchpad 命中区域，包含追加按钮、Previous/Next 按钮、缩略图列表、按钮间距、Padding 和边框。只要缩略图列表在走廊主轴上存在溢出，命中上述任一区域的输入都必须转发给内部 ScrollViewer；即使已经到达首尾边界，也必须消费事件，禁止外层页面突然接管同一输入。只有走廊主轴不存在溢出时才不消费事件，并允许其沿 Avalonia 路由传给外层滚动容器。该行为是固定交互合同，不增加公开开关。

只允许显式按钮命中；不得恢复 Edge Region 隐式导航。按钮 Disabled 时保留布局，只有对应 Is*NavigationEnabled=false 才结构隐藏。

## Thumbnail Filmstrip 施工标准

### 结构和尺寸

走廊固定为单轴、不换行、统一槽位：

~~~text
SlotStride = ThumbnailItemExtent + ThumbnailItemSpacing

N == 0:
    MainAxisItemsExtent = 0

N > 0:
    MainAxisItemsExtent =
        N * ThumbnailItemExtent
        + (N - 1) * ThumbnailItemSpacing
~~~

ThumbnailItemExtent 必须有限且大于零；Spacing 必须有限且不小于零。Top/Bottom 时 Extent 控制 Item 宽，Left/Right 时控制 Item 高。图片使用 UniformToFill 并居中裁剪，允许 Viewport 边缘只显示一个 Item 的一部分。

ThumbnailFilmstripExtent 是外壳交叉轴尺寸，包含边框和 FilmstripAppearance.Padding；EdgeGap 是到同侧根边界的距离；AlongEdgeInset 是沿所在边缘两端的最小距离。

### 虚拟化

Filmstrip 内部使用单主轴 ScrollViewer、ItemsPresenter 和基于 VirtualizingStackPanel 的专用 ImageGalleryVirtualizingPanel。不得使用 StackPanel、WrapPanel 或一次创建全部容器。

所有轻量 Descriptor 常驻，只实现 Visible 与主轴前后各约 0.5 Viewport 的 Overscan 容器。Overscan 和 16 pixel 目标尺寸分档都是内部策略，不公开属性。

稳定布局容器上限必须满足专项验证合同：

~~~text
L = Filmstrip 主轴 Viewport 长度
E = ThumbnailItemExtent
S = ThumbnailItemSpacing
P = E + S

Rmax = ceil(2L / P) + 4
~~~

远距离选择允许新旧窗口短暂共存，瞬时不得超过 2 * Rmax，并在最多两个 Dispatcher/Layout 周期内回落到 Rmax。不得逐项实现中间容器。

新选择只做最小必要滚动使目标 Item 完整可见；已完整可见时不移动，不强制居中。用户只滚动走廊时，不得立即把走廊拉回当前选择。

### 容器回收

Thumbnail Item 容器只保存当前 Descriptor、状态和消费者令牌。回收前必须：

- 取消 PressCandidate、直接滚动和 Pointer Capture。
- 递增绑定代次，使迟到结果失去提交资格。
- 解除 Source、Lease、Selection 和 Appearance 订阅。
- 释放容器消费者持有的 Lease 引用。
- 清除伪类、标题、Key、索引、错误状态和图片引用。

回收容器不得 Dispose 仍由共享操作、缓存或其他消费者持有的底层图片。

缩略图来源优先使用 IImageGalleryItem.ThumbnailImageSource；该值为 null 时，使用 MainImageSource 发起 Purpose=Thumbnail 且带目标尺寸的请求。不得为了缩略图永久完整解码原图，也不得在控件内建立磁盘缓存。

Add Image 固定为业务命令入口：AddImageCommand 为 null 或 CanExecute(AddImageCommandParameter)=false 时按钮显示但禁用；点击只执行该命令和显式参数，不默认传递 SelectedItem，不打开系统文件选择器，也不直接修改 ItemsSource。

## 加载、调度与缓存

### 每实例调度器

每个已附加到视觉树的 ImageGallery 拥有自己的有界调度器。首版不跨多个 Gallery 共用全局队列，也不公开 MaximumConcurrentThumbnailLoads 或任何并发数量属性。

六个基础槽位固定为：

~~~text
1 Current Main
4 Thumbnail
1 Speculative
~~~

槽位是一项 LoadAsync 从开始到终态的完整管线许可证，不是 CPU 核、线程或协程。相同 Cache Key 的多个消费者合并为一个共享底层操作，只占一个槽位。

调度优先级固定为：

~~~text
P0 Current Main 首次加载
P1 Selected Thumbnail
P2 Visible Thumbnails
P3 Current Main 清晰度升档
P4 Overscan Thumbnails
P5 Adjacent Main Prefetch
~~~

为替换一个已经忽略取消、且无消费者的旧操作，允许一个内部有界逃生槽位，因此每实例底层执行中的短时绝对上限为 7。逃生槽位不得被普通吞吐长期占用。

每个共享操作至少保存 Cache Key、当前最高优先级、进入该优先级的稳定序号、消费者集合、Queued/Running/Completed 状态、CancellationTokenSource 和请求代次。同优先级按稳定序号先进先出；消费者升级可以提升优先级，但不得重复创建底层操作。最后一个消费者离开时，Queued 操作直接移除，Running 操作请求取消并继续占用槽位，直到 LoadAsync 真正进入终态。

CancellationToken 用于节省资源，不是正确性屏障。每个异步结果提交前至少验证 LifecycleGeneration、RequestVersion、Selection Identity、Source Identity、目标档位和消费者代次。失败验证的 Lease 必须在 UI 线程立即 Dispose。

### 主图状态机

~~~text
Empty
  -> 合法非空选择
Loading
  -> 当前请求成功: Ready
  -> 当前请求失败: Error

Ready / Error
  -> 新选择或 Source Identity 改变
Loading
~~~

选择切换后立即提交新的 Selection 和 Loading 状态，但已有 Ready 主图继续作为不可交互的保留帧绘制，直到最新合法候选完成。保留帧不代表当前选择，不允许把其标题、命令或交互状态伪装成新图片；Loading Presenter 必须明确当前图片仍在加载。缓存命中可以在同一选择提交中直接进入 Ready，无须强制闪过 Loading。

主图首次加载采用两阶段提交。异步阶段只产生局部候选 Lease；提交前必须再次验证 Attached 状态、RequestVersion、Selection Identity、Source Identity 和目标档位。验证成功后，在一个不含 await 的 UI 线程回调中按固定顺序执行：安装候选 Lease、清除失败状态、重置新图 Viewport 状态、进入 Ready、请求重绘，最后释放旧当前 Lease。候选所有权一旦转交根控件，加载协调器不得再次 Dispose；验证失败、取消或异常路径中的未转交候选必须在 finally 中释放。

快速执行 A -> B -> C 时，A 可以在 B、C 的 Loading 期间继续显示；B 即使迟到成功也只能被释放，只有 C 有资格替换 A。任意时刻最多只有一个已提交保留帧和一个等待 UI 提交的最新候选参与主图视觉交接，连续选择不能建立待播放队列。

取消、过期和消费者归零不是 Error。只有当前有效正式加载的普通读取、网络、解码、不支持格式或 LoadLimits 超限进入 Error。升级或预取失败不得破坏现有 Ready。

根控件必须设置互斥的 :empty、:loading、:ready、:error 伪类。Empty 显示内置矢量空状态和本地化短文案；Loading 在可选保留帧之上显示受减少动态效果策略控制的加载视觉；Ready 只显示当前合法 Lease；Error 显示内置矢量失败图和通用本地化文案。状态 Presenter 不得命中输入，也不得建立第二套状态。只有 Ready 才允许主图 Zoom、Pan 和 Rotation；Loading 即使具有保留帧也必须禁用这些交互。

空集合下默认 Title、Zoom 和 Rotation 不构成有效 Toolbar 内容；只有 ToolbarContent 非 null 时 Toolbar 才可以继续提供“打开图片”等业务入口。Error、Loading 和 Empty 占位均不得展示路径、响应正文或异常详情。

首图按当前 Viewport、RenderScaling、ZoomMode 和 Rotation 的设备像素需求请求目标尺寸，不得无条件完整解码原图。Ready 后显示需求超过 DecodedPixelSize 覆盖范围时，后台升档并原子替换 Lease；连续输入可以使用约 150ms 内部稳定窗口，具体值必须由 Benchmark 校准且不公开。

### 预取与缓存

MainImagePrefetchMode=Adjacent 时，只在当前主图 Ready、选择稳定且 Filmstrip 停止活跃滚动约 200ms 后，尽力预取前后邻项。最多同时执行一项相邻主图预取；当前选择方向优先，未知方向时 Next 优先。

MainImageCacheMemoryBudgetBytes 只约束非当前完整主图。当前主图不计入该预算，但仍必须满足 LoadLimits。预算为 0 时不保留非当前主图。

ThumbnailCacheMemoryBudgetBytes 只约束机会性缩略图缓存，不强制回收当前 Visible/Overscan 容器正在使用的图片。预算为 0 时离开消费者范围即释放。

两类缓存分开维护。推荐实现均使用 Dictionary 做 O(1) 查找，并用 LRU 链表或职责等价结构维护淘汰顺序；Map、链表、引用计数和预算总额只能在 UI 线程的单一协调入口内同步更新，不允许 await 穿过半提交状态。

缓存 Key 至少包含 Source Identity、Purpose、目标尺寸分档和处理策略版本。Source Identity 必须非 null、稳定；内容变化而身份不变时允许合法命中旧缓存，开发者必须为新内容提供新身份。

缩略图真实失败后为该 Cache Key 建立运行期 5 秒失败抑制记录。抑制期内的新消费者直接显示稳定 Error，不创建底层请求；时间到期本身不启动计时器或重试，只有新的 Visible、Selected 或有效需求到来时才允许重新加载。取消、过期和消费者归零不得写入失败记录，记录不得长期持有完整异常、响应或 Stream。

## 主图呈现模式与逻辑资源消费

`MainImageMode=Presented` 保持普通图片查看器行为。`ResourceOnly` 仅关闭主图绘制、Viewport Wheel/Pan/Pinch、主图 Previous/Next、Zoom/Fit/1:1/Rotation 默认工具；它不得关闭选择、标题、`ToolbarContent`、缩略图走廊、当前主图加载、相邻基线预取、缓存或 `TryAcquireCurrentImage`。Empty/Loading/Error 主图状态视觉在 ResourceOnly 中隐藏，但 `ImageState` 仍反映逻辑资源状态。

ResourceOnly 的空白 Viewport 必须参与原 Measure/Arrange，不得因隐藏而把根布局折叠为零；其 Viewport 背景和主图内容不得命中，指针可落到同层下方业务控件。根 Border 装饰可以保留但不得命中；ToolbarContent 和缩略图走廊等实际 Overlay Bounds 继续正常命中。进入 ResourceOnly 时取消正在进行的 Pointer Capture、Pan 和 Pinch；模式往返不得取消或重载当前请求、释放当前槽、重置 Zoom/Pan/Rotation、改变选择或触发 `CurrentImageResourceChanged`。切回 Presented 后已有当前图片必须在下一帧直接可绘制。

`MainImageDecodeSizeHint` 是整张逻辑图片所需的物理像素尺寸提示，null 表示无显式提示；非 null 时宽高都必须为正。Presented 的目标需求综合 Viewport、DPI、有效 Zoom、Rotation 和 Hint；ResourceOnly 综合 Viewport、DPI 和 Hint，不把保留的 Gallery Zoom 放入资源模式需求。最终目标使用 128 pixel 内部分档，并受 `LoadLimits` 尺寸、像素和解码字节边界约束。

零 Viewport 且 Hint=null 时不得发出 `TargetPixelSize=null` 的整图请求；保持 Loading，等待有效 Viewport 或 Hint。Ready 后视口暂时归零不得清除或降档当前槽。加载中改变 Hint 不取消已运行首帧：记录最新需求，首帧成功原子提交后再追赶；Ready 状态采用约 150ms 稳定窗口和约 25% 提升门槛，只升不降。高 Hint 只影响当前图，Adjacent 仍按 Viewport 基线预取。

自定义 Source 可以返回低于请求尺寸的合法 Lease。首次合法结果仍可进入 Ready；升级候选只有实际 `DecodedPixelSize` 优于当前槽时才能原子替换并触发事件。同档或更差候选立即释放、记录本档能力且不得循环重试；升级失败保留旧槽和 Ready，首次加载失败才进入 Error。`SourcePixelSize`、`DecodedPixelSize` 和 `IImage` 的宽高均使用 EXIF 规整后的逻辑轴。

## 生命周期与资源释放

根 ImageGallery OnAttachedToVisualTree / OnDetachedFromVisualTree 是重资源生命周期边界。内部模板部件单独重建或 Detach 不得触发整库清理。

根 Detach 必须按顺序幂等执行：

1. 增加 LifecycleGeneration。
2. 取消主图、预取和缩略图请求。
3. 终止 PressCandidate、Panning、Pinching 和 Capture。
4. 解除虚拟容器消费者并释放其 Lease。
5. 释放当前主图 Lease。
6. 释放主图和缩略图缓存引用。
7. 清空失败抑制、队列和旧视觉窗口调度状态。

保留 ItemsSource、集合订阅、Descriptor、Key、Title、SelectedIndex、SelectedItem、Appearance、MainImageMode、MainImageDecodeSizeHint、Zoom/Pan/Rotation 和其他开发者配置。重新 Attach 后只加载最终有效选择和最终可视缩略图窗口，不回放离开期间的中间状态；同一逻辑选择重新提交后恢复保留的视图状态。

Detached 期间集合变化仍只维护轻量 Descriptor 和选择一致性，不启动图片请求。Detach 原子释放 Gallery 自己的当前槽和缓存并进入 Empty；重新 Attach 后，空集合保持 Empty，非空选择先进入 Loading，再按最终状态提交 Ready 或 Error。已经通过 `TryAcquireCurrentImage` 交给外部的独立 Lease 不属于 Gallery Detach 清理范围，继续有效直到调用方 Dispose。

IsVisible=false 不等于 Detach，首版不因此主动释放全部资源。

所有取得的 Lease 都必须有明确的 try/finally 或所有权转移路径。release 回调违反合同并抛出时，Gallery 捕获并记录内部诊断，继续释放其余资源。

## 固定模板与主题

资源聚合固定为：

~~~text
ImageGalleryThemesProvider.axaml
  -> Themes/ImageGalleryThemes.axaml
      ├── ImageGalleryTheme.axaml
      └── Internal/*
~~~

所有模板、状态矢量图和内部辅助控件必须使用编译 AXAML 静态进入包。不得运行时扫描程序集发现 Theme，不得用 C# 动态注入功能视觉树。

PART_Viewport、PART_PreviousButton 和 PART_NextButton 是已经确定的内部部件名。其余内部部件名在实现时必须集中定义常量并由 OnApplyTemplate 严格验证准确类型。缺失、类型错误或核心 Presenter 被替换时抛出含部件名与预期类型的 InvalidOperationException，不能显示半残控件。

内部 Theme、资源 Key、伪类和模板部件不构成外部兼容合同。框架继承的根 Theme 属性不等于支持模板替换。首版禁止：

- 替换根 ControlTemplate。
- 替换任一内部子组件 ControlTemplate。
- 外部实例化或样式定位 internal 辅助控件。
- 删除、插入、重排固定视觉节点。
- 替换 Viewport Presenter。

外部定制只能使用根公开属性、Appearance 对象和 ToolbarContent / ToolbarContentTemplate。

运行中修改合法 Appearance 必须立即作用于对应角色，但不得重建根模板、重建 SelectionModel、重新加载图片或清空缓存。纯画刷变化只重绘；尺寸和间距只使必要角色重新布局。替换 Appearance 或根控件 Detach 时必须解除旧订阅，不能因共享资源订阅使 Gallery 无法被回收。

默认视觉必须使用 AtomUI.Core 语义 Token，并在高对比度下保留可辨认边界、选中轮廓和按钮图标，不能只依赖颜色或透明度表达选择与状态。系统请求减少动态效果时，停止装饰性 Loading 循环和未来可能增加的过渡；首版导航、缩放、旋转和响应式切换本来就不使用平滑动画。

## 本地化

ImageGallery 复用 AtomUI.Core ThemeManager 和 LanguageVariant，不建立包级静态语言单例，不公开控件级 Culture。

首版资源文件：

- zh_CN：中文默认资源。
- en_US：英文资源。
- zh_TW：显式桥接到 zh_CN，直到真正提供繁体翻译。

系统语言启动映射：

~~~text
zh-* -> zh_CN
en-* -> en_US
unsupported / detect failure -> zh_CN
~~~

宿主先解析启动语言并调用 WithDefaultLanguageVariant。UseImageGallery() 只注册 ImageGallery Theme、语言 Provider 和强类型资源 Key，不得覆盖宿主已选 LanguageVariant。

运行时切换统一修改 ThemeManager.LanguageVariant，并通过 AtomUI.Core 现有 LanguageVariantChanged 和动态资源链即时更新可见文案。控件不得轮询语言，也不得缓存已解析字符串。

AtomUI.Generator 在本包中的实际职责是生成语言 Provider、强类型 Key 和显式 Provider 池；它是 PrivateAssets=all 的编译期依赖，不是普通 Binding 的补救机制。

## 配置验证与失败分类

| 情况 | 必须行为 |
|---|---|
| 非法数值、未知枚举、错误 ZoomRange、错误 Appearance | 立即抛出含属性和值的配置异常，不规整为另一值 |
| ItemsSource 项错误、重复 Key、非 UI 线程通知 | 原子拒绝，抛出明确异常，不部分提交 |
| 固定模板缺部件或类型错误 | InvalidOperationException，快速失败 |
| 当前图片读取、网络、格式、解码或安全限制失败 | 当前项进入 Error，显示内置本地化失败视觉 |
| 缩略图失败 | 固定槽位显示小型失败视觉，不改变尺寸或主图状态 |
| 取消、过期结果、消费者归零 | 释放结果，不进入 Error |
| 当前 Ready 图片清晰度升级失败 | 保留当前 Lease 和 Ready |
| 相邻预取失败 | 静默结束推测请求，不污染当前状态 |
| OutOfMemoryException 等进程级不可恢复故障 | 不承诺吞掉并伪装为普通 Error |

首版不公开失败异常对象、失败事件、Retry/Reload 命令。错误详情只能进入内部诊断，不得把路径、服务地址、响应正文或堆栈显示给最终用户。

## NativeAOT 与数据绑定

正式示例只允许：

~~~xml
<UserControl
    x:DataType="local:GalleryViewModel">
    <atom.labs:ImageGallery
        ItemsSource="{CompiledBinding GalleryItems}" />
</UserControl>
~~~

或在 C# 中直接赋值 ItemsSource。普通 Binding 可能运行，但不属于严格 NativeAOT 与 trimming 兼容承诺。控件不得通过反射读取业务项，不按类型名动态加载 Source，不扫描程序集发现 Theme 或语言资源。

## 测试与发布闸门

### 必须建立的测试层

1. 纯逻辑：缩放范围、平移边界、缓存 Key、优先级、集合变化和选择回退。
2. Headless：模板、布局、伪类、输入状态机、响应式降级、Detach/Attach 和迟到结果。
3. 资源审计：每个 Lease 创建、共享、替换、取消、淘汰和释放次数平衡。
4. 虚拟化：10,000 项 PR 闸门、100,000 项 Nightly、算法极限索引与溢出。
5. 故障注入：即时、延迟、失败、响应取消、忽略取消、乱序完成和错误 release。
6. 编译 AXAML 与 API 基线：公共面、internal 辅助类型、无 ControlTheme 定制属性。
7. NativeAOT：真实 win-x64 宿主 publish 后启动并走通核心链路。
8. Gallery：四 Placement、三响应式状态、缩放/平移/Pinch/旋转、格式、错误和高 DPI 人眼验收。

缩略图容器数量、2 * Rmax 瞬时峰值、请求并发上限、缓存预算和 Lease 平衡属于正确性断言，不以机器慢为由放宽。

### 候选发布定义

只有以下证据全部适用且通过，才能声明具备候选发布能力：

- Debug 和 Release 编译。
- net10.0 与 net8.0 测试和 pack。
- Headless 与虚拟化正确性。
- Benchmark 基线。
- trimmed build。
- win-x64 NativeAOT Runtime Smoke。
- NuGet 依赖白名单与内容审计。
- Gallery 自动 Smoke 和完整人眼验收。

在首版实现和 Benchmark 产生之前，文档中的性能公式、预算和复杂度是目标合同，不是已经取得的性能结论。

## 强制施工顺序清单

施工必须保持每一阶段可编译、可测试、可审计。功能源码与本阶段测试同步提交，不允许先写完全部视觉再补状态机、资源释放、虚拟化或 NativeAOT。前一阶段退出闸门没有通过时，不得以“后续一起修”为由进入依赖它的下一阶段。

当前进度基线：

- [x] S0：设计收敛与施工合同冻结。
- [x] S1：工程骨架与依赖边界。
- [x] S2：公共 API 与属性系统。
- [x] S3：集合、Descriptor 与选择协调。
- [x] S4：图片源、安全限制与 Lease。
- [x] S5：加载调度、状态机、缓存与生命周期。
- [x] S6：主图 Viewport 与直接交互。
- [x] S7：Overlay、Toolbar 与显式导航。
- [x] S8：缩略图走廊与真实虚拟化。
- [x] S9：Appearance、主题、本地化与兼容性收口。
- [x] S10：Gallery、Benchmark、NativeAOT 与候选发布工程入口。

以上勾选表示 2026-08-19 工作树已经完成 S0～S10 的源码、自动测试、Gallery、Benchmark、Package 与 NativeAOT Smoke 施工入口，并通过本轮适用的自动闸门及单固定种子 30 分钟真实默认模板 Headless 长稳。它不替代完整 Gallery 人眼、真实图片/DPI/触控矩阵和提交后干净检出签署；这些证据必须继续按 [发布验收文档](release-and-gallery-verification.md) 管理，不能因本清单完成而伪称最终 RC 已签署。

### S0：设计收敛与施工合同冻结

交付物：

- 本施工合同成为名称、默认值、行为边界和施工顺序的唯一入口。
- 设计稿只保留理由与推导，两份验证文档分别承接虚拟化和发布证据。
- 所有首版外部能力、明确非目标和延期 TODO 已分类。

退出闸门：

- 文档相对链接有效。
- 公共 API、依赖、状态机、异常、生命周期和发布边界不存在相互矛盾的表述。
- 后续改变公共面或强制行为时，必须先修改本文再修改源码。

### S1：工程骨架与依赖边界

交付物：

- 创建 AtomUI.Labs.Controls.ImageGallery 正式项目。
- 创建 Tests、Benchmarks 和 Gallery ImageGallery 目录入口。
- 配置 Debug net10.0、Release net10.0/net8.0、集中输出、版本和打包元数据。
- 只引入 Avalonia、AtomUI.Core 与 PrivateAssets=all 的 AtomUI.Generator。
- 创建 ImageGalleryThemesProvider、UseImageGallery() 和最小编译 AXAML 资源链。
- 创建可以显式打开 ImageGallery 页面并退出的 NativeAOT Smoke 骨架。

必须同步完成：

- 工程引用与 NuGet 依赖白名单测试。
- Theme Provider 重复注册幂等测试。
- Debug/Release 编译、最小 pack 内容检查。
- win-x64 trimmed/NativeAOT 最小宿主 publish 与启动检查。

退出闸门：

- 正式包依赖图中不存在 Desktop Controls、GalleryBase、其他 Labs 控件或反射注册机制。
- 两个目标框架均能编译正式包；NativeAOT 骨架没有 ImageGallery 自身引入的未归因裁剪警告。

### S2：公共 API 与属性系统

交付物：

- 建立本文规定的全部 public enum、record struct、interface、sealed data/Lease/Appearance 类型。
- 建立 ImageGallery 根控件、全部 StyledProperty、只读 DirectProperty、命令和 RotateClockwise()。
- 实现默认值、原子 ZoomRange/LoadLimits 提交和全部数值、枚举、Appearance 校验。
- 建立公开 API 基线文件，internal 辅助类型不得泄漏。

必须同步完成：

- 默认值、属性元数据、只读性、非法值和 AXAML 转换器测试。
- API 基线、命名空间、XMLNS 映射和编译 AXAML 测试。
- 反射仅允许在测试中审计公共面；正式实现不得依赖反射。

退出闸门：

- 本文公共类型和成员逐项对齐，不存在临时同义 API。
- 配置错误能够原子拒绝且包含属性名和实际值。

### S3：集合、Descriptor 与选择协调

交付物：

- 实现有限 ItemsSource 快照、INotifyCollectionChanged 订阅和 O(N) 轻量 Descriptor。
- 实现 Key 唯一性、UI 线程检查和每实例 ImageGalleryCoordinator。
- 实现空/非空选择、相邻导航和 Add/Insert/Remove/Replace/Move/Reset 的身份规则。
- Viewport、Toolbar 和 Filmstrip 只提交意图，不直接互相写状态。

必须同步完成：

- 0、1、2、6、30、10,000 项集合测试。
- 重复/null Key、错误项目类型、非 UI 线程通知和集合操作原子性测试。
- 所有集合变化的 SelectedIndex、SelectedItem、Key、Source Identity 和请求版本断言。

退出闸门：

- 非空集合始终拥有一项合法选择。
- 仅索引变化不会错误重载当前图片；真正身份变化不会把 Loading 保留帧错误标记为新选择的 Ready 图片。
- 已知索引的相邻选择和身份查找达到既定复杂度，不出现 O(N²) 协调路径。

### S4：图片源、安全限制与 Lease

交付物：

- 实现 IImageGallerySource、ImageGallerySources 五个工厂和身份规则。
- 实现 PNG、JPEG/JPG、BMP、静态 WebP 解码与 EXIF Orientation 修正。
- 实现 LoadLimits 的读取前、累计读取、图片头、像素和解码内存防线。
- 实现 ImageGalleryImageLease 一次性同步释放和 UI 线程接管规则。
- 建立 ControllableImageGallerySource 等确定性测试替身。

必须同步完成：

- 文件、avares、HTTP、重复 Stream Factory 和 Create 的成功/失败/取消测试。
- HttpClient、Stream、Response、文件句柄和 Lease 的所有权审计。
- 损坏格式、多帧格式、伪装 Content-Type、超大尺寸和整数溢出样本测试。
- NativeAOT 宿主中的文件与资源图片加载 Smoke。

退出闸门：

- LoadAsync 不阻塞 UI 线程，所有 Stream 在返回前关闭。
- 每个已取得 Lease 都存在唯一、可证明的释放路径。
- 不支持格式和安全限制进入图片 Error；开发者配置错误仍快速抛出。

### S5：加载调度、状态机、缓存与生命周期

交付物：

- 实现每实例六基础槽位、一个有界逃生槽位和 P0～P5 稳定优先队列。
- 实现按 Cache Key 合并的共享加载操作、消费者集合、取消和请求代次。
- 实现 Empty/Loading/Ready/Error 主图状态机、清晰度升档和 5 秒缩略图失败抑制。
- 实现主图/缩略图独立缓存、LRU 淘汰、预算和 Adjacent 预取。
- 实现 Attach/Detach 代次、请求取消、缓存清理和迟到结果拒绝。

必须同步完成：

- 即时、延迟、乱序、失败、响应取消和忽略取消的调度测试。
- `A -> B -> C` 高速选择测试必须证明 Loading 期间 A 持续可绘制、B 迟到不得提交、C 成为唯一 Ready；当前候选失败、空集合与 Detach 必须清除保留帧。
- 所有权测试必须证明旧 Lease 的 Release Callback 观察到新 Lease 已安装，未转交候选在取消、过期和异常路径中得到释放。
- 六基础槽位、七瞬时绝对上限、同 Key 单底层操作和稳定优先级断言。
- 缓存命中、淘汰、预算缩放、未知大小、预取提升和失败抑制测试。
- Detach/Attach、迟到成功/失败、Dispose 次数与订阅泄漏测试。

退出闸门：

- 过期结果永远不能覆盖最终选择。
- 请求、消费者、缓存和 Lease 计数在所有终态下平衡。
- 取消不会提前释放运行槽位，也不会被错误显示为 Error。

### S6：主图 Viewport 与直接交互

交付物：

- 实现不依赖 ScrollViewer 的 ImageGalleryViewportPresenter 直接绘制。
- 实现 Fit、ActualSize、Custom、RenderScaling、四分之一圈旋转和 DestinationRect。
- 实现指针锚点 Wheel Zoom、中心命令缩放、Pan、Pinch 和统一 ReconcileViewportState。
- 实现 PressCandidate/Panning 与 Pinching 的互斥输入状态机。
- 图片切换时原子重置 Fit、RotationAngle 和 PanOffset。

必须同步完成：

- 公式、Clamp、跨 DPI、极小/极大 Viewport 和 0/90/180/270 旋转测试。
- Wheel、Ctrl+Wheel、Pinch、Pointer Capture、边界消费和取消路径测试。
- 连续输入、图片切换、Loading 保留帧绘制、原子 Lease 替换、提交后释放和过期手势的竞态测试。
- 真实桌面高 DPI 与 NativeAOT Viewport Render Smoke。

退出闸门：

- 不出现加载空帧、空白越界、锚点跳变、过期图片闪回或两阶段可见半提交。
- Viewport 协调开销不随 ItemsSource 数量增长。

### S7：Overlay、Toolbar 与显式导航

交付物：

- 实现固定根模板、ImageGalleryOverlayPanel、Toolbar 和两组 Navigation Button。
- 实现四边 Placement、同边对置、相邻边避让和安全区域。
- 实现 Normal/Compact/Minimal、8 DIP 滞回、空间优先级和 Z-Index。
- 实现 Title/Zoom/Rotation 显隐、ToolbarContent 固定末端和命令状态。
- Viewport 与 Filmstrip Previous/Next 共用根稳定命令；不得实现 Edge Region。

必须同步完成：

- 四 Placement 的同边、对边、相邻边和极小 Bounds 布局矩阵。
- Toolbar 区域隐藏、Content DataContext、命令 CanExecute 和快速连续导航测试。
- Toolbar 四个 Placement 必须验证整组水平居中、横向共同中心线、竖向共同中心轴，以及不同按钮/文案尺寸和 ToolbarContent 的交叉轴对齐。
- Toolbar 必须验证 Zoom In、Zoom Out、Rotate 使用固定几何 Glyph、Glyph 中心与 Button 内容区中心重合，并使用真实 `TextLayout` 度量验证 Title、缩放百分比、Fit 与 `1:1` 的可见墨迹中心；仅断言外层 Bounds 或 Alignment 属性不构成通过。
- 固定模板缺部件、错误类型、输入命中和响应式 Capture 清理测试。

退出闸门：

- 所有 Overlay 保持在根 Bounds 内，不依靠遮盖掩饰布局冲突。
- Filmstrip、Toolbar 和导航按钮不直接修改主图或创建私有选择状态。

### S8：缩略图走廊与真实虚拟化

交付物：

- 实现四边 Filmstrip、Add Image、相邻导航、直接滚动和单轴 ScrollViewer。
- 实现固定 SlotStride、UniformToFill 居中裁剪和最小必要选中项滚动。
- 实现基于 VirtualizingStackPanel 的专用面板、Visible + Overscan 和容器彻底回收。
- 实现专用缩略图优先、主图源回退、16 pixel 尺寸分档和机会性缓存。

必须同步完成：

- 0、1、2、6、30、100、1,000、10,000 项正确性矩阵。
- 四 Placement、不同 DPI、Extent、Spacing、Padding 和边缘部分可见测试。
- Rmax、2 * Rmax 瞬时峰值、两周期收敛和远距离选择不逐项实现断言。
- 快速滚动、选择切换、回收复用、迟到结果和 Add Image 命令测试。

退出闸门：

- 相同 Viewport 参数下，容器、活跃请求和解码资源不随总项目数线性增长。
- 继承 VirtualizingStackPanel 但仍创建全部 Item 的实现直接判定失败。

### S9：Appearance、主题、本地化与兼容性收口

交付物：

- 完成固定编译 AXAML、内部 Theme、状态矢量图和 AtomUI.Core Token。
- 实现五类 Appearance 的稀疏覆盖、运行时更新和订阅解除。
- 实现高对比度、减少动态效果和固定选中轮廓。
- 通过 AtomUI.Generator 生成 zh_CN、zh_TW 桥接、en_US Provider 和强类型 Key。
- 实现 ThemeManager.LanguageVariant 运行时切换，不增加控件级 Culture。

必须同步完成：

- Appearance 未设置/显式 null、共享、替换、GC、布局失效范围和非法值测试。
- 中英文启动映射、中文兜底、运行时切换和 Detach/Attach 测试。
- 编译 AXAML、trimmed build、完整 NativeAOT Runtime Smoke 和无反射扫描审计。

退出闸门：

- 公共 API 中不存在 ControlTheme 或内部视觉辅助类型。
- 语言、主题和 Appearance 变化不改变 Selection、请求、Lease 或缓存身份。
- win-x64 NativeAOT 发布产物真实启动并走通核心 ImageGallery 链路。

### S10：Gallery、Benchmark、NativeAOT 与候选发布

交付物：

- 建立 Basic、Placement、Interaction、Appearance、Error、Large Collection、AOT 等 Gallery Cases。
- 默认首次进入 Basic，不默认打开压力或故障页面。
- 建立独立 Benchmarks、100,000 项 Nightly、30 分钟长稳和固定随机故障序列。
- 完成 net10.0/net8.0 pack、NuGet 内容审计、trimmed build 和 win-x64 NativeAOT RC Smoke。
- 保存三轮 Benchmark 中位结果、机器环境、原始数据和人眼验收记录。

必须同步完成：

- 执行两份专项验证文档的全部 PR、Nightly 和 Release Candidate 适用项。
- 扫描 nupkg，确认不存在 Gallery、Tests、Benchmarks、本机路径或禁止依赖。
- P0、P1、P2 未关闭缺陷全部归零；P3 必须具有书面评估、明确影响和后续跟踪。

退出闸门：

- Build、Headless、虚拟化、Benchmark、NativeAOT Runtime、Package 和 Gallery 人眼证据全部通过。
- 只有此时才能把“目标 NativeAOT 兼容”和“目标性能合同”升级为当前版本已经取得的发布证据。
