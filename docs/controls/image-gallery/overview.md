# ImageGallery 控件设计

> 文档状态：首版设计基线，更新于 2026-08-19。本文记录已施工首版的产品定位、默认视觉结构、公共边界和设计理由；准确施工合同见 `implementation-contract.md`，实际验证结论见两份专项验证文档，不得仅凭本设计稿宣称最终发布能力。

> 施工入口：[ImageGallery 首版施工合同](implementation-contract.md)。本文保留设计理由、算法推导和协商结论；源码施工以施工合同中的名称、默认值、边界与验收入口为准。

本文记录实验控件 `ImageGallery` 的目标设计。未来独立包、项目和测试项目应遵守 Labs 的多包规范：

```text
Package / Project  AtomUI.Labs.Controls.ImageGallery
Test Project       AtomUI.Labs.Controls.ImageGallery.Tests
AXAML Namespace    https://atomui.net/labs
Preferred Prefix   atom.labs
```

文档可以先于源码进入仓库，但源码落地后必须重新核对包名、公共 API、主题、测试和 Gallery 验收。本文不是施工人员自行拼接公共合同的入口；发生矛盾时必须先同步修正文档。

## 定位

`ImageGallery` 用于浏览图片集合。它的默认形态是沉浸式主图查看器，由当前主图、浮动工具栏和缩略图走廊共同组成，不是只把图片排列成行列的普通缩略图网格。

目标使用场景包括：

- 在同一个视口中查看图片集合中的当前图片。
- 通过缩略图、Previous 和 Next 在相邻图片之间导航。
- 查看当前图片标题、显示模式和缩放状态。
- 在不离开 Gallery 的情况下执行受控的图片查看操作。

当前结构参考图只约束信息架构，不要求像素级复刻。默认主题的颜色、透明度、圆角、间距、阴影、字号、图标和交互状态应在实现阶段基于 AtomUI Token 和主题规范确定。

## 默认视觉结构

```text
ImageGallery
┌──────────────────────────────────────────────────────────────────┐
│                                                                  │
│                     Main Image Viewport                          │
│                  当前主图的显示、缩放与平移区域                    │
│                                                                  │
│             ┌──────────────────────────────────┐                 │
│             │ Title │ View/Scale │ Operations │                 │
│             │       Floating Toolbar          │                 │
│             └──────────────────────────────────┘                 │
│                                                                  │
│                                                                  │
│ ┌──────────────────────────────────────────────────────────────┐ │
│ │ Add Image │ Prev │ [Thumbnail][Thumbnail]... │ Next        │ │
│ │           │      Filmstrip Navigator                       │ │
│ └──────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────┘
```

默认结构分为三层：

1. 主图片视口占据主体空间，呈现当前选中图片。
2. 浮动工具栏覆盖在主图片视口之上，不参与图片内容布局。
3. 缩略图走廊在首版以 Overlay 方式浮动覆盖于主图片视口的指定边缘，内部包含 Add Image 按钮、相邻导航按钮和缩略图项。

工具栏和缩略图走廊均不参与主图内容 Measure，不挤压 Viewport。缩略图走廊的位置、边缘间距和方向规则已经确定；同边缘请求采用走廊优先、Toolbar 对边规整，相邻边缘采用 Toolbar 沿边避让及单调响应式降级。首版不实现 Pointer 离开后的自动隐藏，不能从参考图尺寸直接硬编码布局。

## 概念模块

以下名称首先表示职责角色，不等于已经决定将每个角色公开为独立 Class：

```text
ImageGallery
├── Viewport Role
│   └── 当前主图呈现、显示模式、缩放与平移
├── Toolbar Role
│   └── 标题、显示模式、缩放状态和操作入口
├── Navigator Role
│   ├── Previous
│   ├── Thumbnail Strip
│   │   └── Thumbnail Item
│   ├── Next
│   └── Add Image Action
└── Selection Coordination
    └── 集合、当前项、主图、工具栏和导航状态同步
```

### ImageGallery

根控件负责承载图片集合、维护唯一的当前选择，并协调主图、工具栏与缩略图导航。目标基类已经确定为 Avalonia `SelectingItemsControl`：

```csharp
public class ImageGallery : SelectingItemsControl
{
}
```

ImageGallery 固定为单选集合控件，不承诺或开放多选行为。缩略图走廊是这个选择型根控件的可视 ItemsPresenter；主图只是当前选中项的另一种异步呈现，不建立第二套集合或选择模型。

### Viewport 角色

该区域正式称为“主图查看区（Viewport）”，不称为“图片详情面板”。它只呈现当前项对应的主图片，并负责缩放、平移、适应窗口、原始尺寸、裁剪以及空、加载中、成功和失败状态。它不负责图片集合选择，也不应自行修改业务数据源。

标题、说明、EXIF 和其他业务元数据不属于主图查看区的内建职责。它们应由快捷操作区、业务模板或未来单独确认的信息区域承载。

主图查看区不保存独立的选中索引：

```text
ImageGallery Current Selection
              │
              v
       Viewport Current Image
```

切换图片时默认回到 `Fit`、`RotationAngle=0` 并清除平移，不能把上一张图片的缩放、旋转和平移状态无意应用到下一张图片。

#### 显示模式与缩放基准

主图查看区使用三个互斥的显示模式：

```csharp
public enum ImageGalleryZoomMode
{
    Fit,
    ActualSize,
    Custom
}

public ImageGalleryZoomMode ZoomMode { get; set; }
    = ImageGalleryZoomMode.Fit;

public double CustomZoomFactor { get; set; } = 1.0;
public double EffectiveZoomFactor { get; }
```

- `Fit`：按照当前 Viewport 可用尺寸动态计算比例，保证整张图片可见。
- `ActualSize`：一个源图片像素对应一个当前显示设备像素，即 100%。
- `Custom`：使用开发者或用户交互指定的自定义比例。

100% 不能解释成“一个图片像素对应一个 Avalonia DIP”。设源图片像素尺寸为 `SourcePixelWidth × SourcePixelHeight`，当前顶层窗口的缩放倍率为 `RenderScaling`，则 `ActualSize` 下的布局尺寸为：

```text
ActualWidthDip  = SourcePixelWidth  / RenderScaling
ActualHeightDip = SourcePixelHeight / RenderScaling
```

例如 1000 × 500 像素图片在 `RenderScaling = 2` 的显示器上，以 100% 显示时占用 500 × 250 DIP。公开给工具栏和业务绑定的有效缩放百分比必须表达“源图片像素到显示设备像素”的绝对比例，不能表达相对于 `Fit` 的二次倍率。

`Fit` 的原始比例按物理像素计算：

```text
ViewportPixelWidth  = ViewportWidthDip  × RenderScaling
ViewportPixelHeight = ViewportHeightDip × RenderScaling

RawFitZoom = min(
    ViewportPixelWidth  / SourcePixelWidth,
    ViewportPixelHeight / SourcePixelHeight)
```

目标公开以下开关：

```csharp
public bool IsFitUpscalingEnabled { get; set; } = false;
```

默认不放大尺寸小于 Viewport 的图片，避免 `Fit` 自动制造模糊；此时 `FitZoom = min(RawFitZoom, 1.0)`。开启后允许 `Fit` 放大，但仍受缩放范围的最大值约束。为保证超大图片能够完整进入 Viewport，`Fit` 允许低于缩放范围的最小值。

`ActualSize` 始终保持 `1.0`，不因开发者配置的自定义缩放范围而改变。用户从 `Fit` 或 `ActualSize` 执行首次缩放时，必须从当前有效比例连续进入 `Custom`，不能先跳到一个无关的固定比例。

#### 原子缩放范围

最小值和最大值不能作为两个可独立提交、相互制约的 StyledProperty 公开，否则 AXAML、Style、Binding 或运行时代码会产生依赖赋值顺序的瞬时非法状态。目标 API 使用单一原子值：

```csharp
public readonly record struct ImageGalleryZoomRange(
    double Minimum,
    double Maximum);

public ImageGalleryZoomRange ZoomRange { get; set; }
    = new(0.05, 32.0);

public double ZoomStep { get; set; } = 1.2;
```

实现必须为 `ImageGalleryZoomRange` 提供与编译 AXAML 兼容、使用固定文化格式的转换器，从而支持：

```xml
<atom.labs:ImageGallery
    ZoomRange="0.05,32"
    ZoomStep="1.2" />
```

范围作为整体一次提交、一次验证。`Minimum` 和 `Maximum` 必须是有限数且大于零，并满足 `Minimum <= Maximum`。写反、`NaN`、正负无穷或非正数属于开发者配置错误，控件必须抛出含属性名和实际值的明确异常，不能自动交换、静默替换或修改另一端。

`ZoomStep` 必须是有限数且严格大于 `1`；等于 `1` 不产生效果，小于 `1` 会颠倒放大和缩小方向，因此均视为配置错误。开发者配置错误与正常交互到达边界必须分开处理：配置错误抛出异常；用户请求的自定义比例超出有效范围时使用 Clamp 截断到最近边界，不抛异常。

```text
RequestedCustomZoom ── Clamp(ZoomRange) ──> EffectiveCustomZoom
```

当新的合法 `ZoomRange` 排除了当前 `Custom` 比例时，必须在同一次属性提交中把当前比例截断到新范围，不能短暂发布范围与有效比例互相矛盾的状态。

Toolbar 和开发者快捷操作使用公开命令进入同一缩放协调路径：

```csharp
public ICommand ZoomInCommand { get; }
public ICommand ZoomOutCommand { get; }
public ICommand FitCommand { get; }
public ICommand ActualSizeCommand { get; }
```

四个命令只在 `ImageState == Ready` 且存在有效当前 Lease 时执行。ZoomIn/ZoomOut 使用 `ZoomStep` 并在合法范围边界更新 `CanExecute`；从 Fit 或 ActualSize 首次增减时进入 Custom。Fit 和 ActualSize 重新应用对应模式并清零 PanOffset，作为明确的查看复位入口。ImageGallery 不为这些命令内置键盘手势，应用可以通过自身 KeyBinding 体系选择是否绑定。

#### 鼠标滚轮缩放

目标公开以下策略：

```csharp
public enum ImageGalleryWheelZoomMode
{
    Disabled,
    Always,
    ControlModifier
}

public ImageGalleryWheelZoomMode WheelZoomMode { get; set; }
    = ImageGalleryWheelZoomMode.Always;
```

| 模式 | 行为 |
|---|---|
| `Disabled` | 主图查看区不通过鼠标滚轮缩放，不消费对应滚轮事件 |
| `Always` | 指针位于主图查看区时，滚轮直接缩放并消费事件；这是沉浸式查看器的默认值 |
| `ControlModifier` | 普通滚轮允许交给外层滚动容器，只有 `Ctrl + Wheel` 缩放并消费事件 |

滚轮缩放以指针位置为锚点，必须在改变比例后保持指针下方对应的图片内容点稳定，不能始终围绕 Viewport 中心缩放。工具栏或程序化缩放入口默认以 Viewport 中心作为锚点。

高精度滚轮或触控板产生连续 Delta 时采用乘法比例：

```text
NewZoom = CurrentZoom × pow(ZoomStep, NormalizedWheelDelta)
```

实现需要限制单次异常 Delta，避免一次输入跨越整个缩放范围。只要当前手势符合 Gallery 的滚轮缩放策略，即使比例已经到达最小值或最大值，也必须继续消费该手势的滚轮事件；不能在边界处突然把事件泄漏给外层 `ScrollViewer`，导致页面意外滚动。平台提供的 Pinch/Scale 手势必须通过独立手势路径处理，不能仅凭 Wheel Delta 猜测。

#### Pinch/Scale连续缩放与双指平移

首版支持从主图绘制表面开始的平台Pinch/Scale手势，并公开：

```csharp
public bool IsPinchZoomEnabled { get; set; } = true;
public bool IsPinchZooming { get; }
```

`IsPinchZooming` 是只读交互状态，不允许开发者强制写入。Pinch、滚轮和Toolbar按钮不是相互调用的输入入口，而是共享同一个内部原子变换核心：

```text
ZoomIn / ZoomOut Command ──> 离散ZoomStep、Viewport中心锚点 ─┐
Wheel ─────────────────────> 连续倍率、Pointer锚点         ├─> ApplyViewportTransformRequest
Pinch ─────────────────────> 累计倍率、双指中心及其移动     ┘              │
                                                                           v
                                                               ReconcileViewportState
                                                                           │
                                                                           v
                                                      Zoom + Pan + Clamp + DestinationRect
```

Pinch不能通过循环执行 `ZoomInCommand` 或 `ZoomOutCommand` 实现，否则会把连续比例量化成 `ZoomStep`、错误使用Viewport中心锚点，并丢失双指中心移动带来的同步平移。三个入口只共享底层比例、锚点、边界和一次提交逻辑。

内部手势生命周期为：

```text
Idle
  │ 合法平台Scale手势从Ready主图表面开始
  v
Pinching + Gesture Ownership
  │ Scale / Centroid持续更新
  ├──> 原子更新目标Zoom与Pan
  │
  └── Completed / Cancelled / 状态失效
           v
          Idle
```

开始时保存不可变快照：起始有效Zoom、起始PanOffset、起始双指中心、该中心通过当前完整逆变换对应的图片内容坐标、Selection Identity、Source Identity和当前Lease资格。更新时使用平台提供的累计Scale计算，不能把每次事件Delta反复乘入已经取整的结果：

```text
TargetZoom = StartEffectiveZoom × CumulativeGestureScale
```

当前双指中心是目标锚点。计算新的PanOffset时，应让起始中心指向的同一图片内容点继续位于最新双指中心下方；因此双指张合产生连续缩放，两指整体移动同时产生平移。最后通过统一ZoomRange、旋转后显示边界和每轴Pan Clamp规整；边界与锚点冲突时仍优先保证不露出空白。

从Fit或ActualSize产生第一项有效比例变化时进入Custom。手势可能在相同Scale下只移动中心，此时仍通过Pan边界处理，不为了记录手势而制造无意义ZoomMode变化。Pinch不使用平滑动画，输入目标在同一渲染节奏内立即生效。

输入所有权与取消规则固定为：

- 只有 `ImageState == Ready`、当前Lease有效且手势起点属于主图绘制表面时才能进入Pinching。
- Toolbar、Navigation、Filmstrip、Thumbnail和自定义内容上开始的手势不缩放主图；Filmstrip既有“不处理Pinch”规则保持不变。
- 一旦合法手势开始，即使中心随后移动到某个Overlay上方，本次手势仍由Viewport完成，不能中途把所有权交给按钮。
- Pinch开始前取消PressCandidate、Panning及其Pointer Capture；Pinching期间拒绝新的鼠标拖拽和滚轮缩放，避免两个输入源竞争同一变换。
- 到达最小或最大比例后，本次Pinch仍由Viewport消费，不能突然泄漏给外层滚动或缩放容器。
- 手指或平台接触点减少到不足继续Scale时结束Pinch，不自动切换成单指Panning；继续平移必须开始新的合法拖拽。
- Selection或Source Identity改变、Lease替换、进入Empty/Loading/Error、`IsPinchZoomEnabled=false`、控件卸载、平台取消或手势所有权丢失时，统一终止手势。
- 运行中改变ZoomRange、Viewport Bounds、RenderScaling或RotationAngle，以及程序化执行新的Fit、ActualSize、Zoom或Rotate请求时，先终止当前Pinch，再让最新显式请求通过同一个原子核心生效；不能在旧手势快照上继续累计。
- 迟到的旧手势事件必须通过手势代次与身份快照失去提交资格，不能覆盖新图片的Viewport状态。

手势结束保留最后一次合法提交的ZoomMode、有效比例和PanOffset，不回滚到起始快照。取消只停止后续输入并清理 `IsPinchZooming` 和平台手势状态；已经呈现的合法变换保持不变，除非取消原因本身是图片切换或状态复位，此时服从对应的Fit、RotationAngle和Pan重置规则。

#### 主图绘制与平移架构

主图查看区不使用内部 `ScrollViewer`，也不通过“普通 `Image` 加 `RenderTransform`”实现缩放。首版采用 ImageGallery 专用 Viewport Presenter，直接绘制当前 Lease 中的 `IImage`，并自行维护缩放和平移状态：

```text
ImageGallery
└── PART_Viewport                      Clip 边界与背景
    └── Grid
        ├── ImageGalleryViewportPresenter
        │   └── 直接绘制 Current IImage
        ├── Empty / Loading / Error Presenter
        ├── PART_PreviousButton
        ├── PART_NextButton
        └── Action Bar Presenter
```

`ImageGalleryViewportPresenter` 是正式包的内部实现类型，概念基类为 Avalonia `Control`。它不是任意内容容器，不向开发者开放 `Content`，也不承担集合选择。显式导航按钮、快捷操作区及状态视觉与它处于覆盖布局的同级，不随图片缩放或平移。

这一选型是确定的架构边界：主图区域面对的是单个已解码 `IImage`，需要同步的指针锚点缩放与画布式拖拽，不需要滚动条、Extent、BringIntoView 或通用内容滚动。直接绘制避免缩放后等待 Measure/Arrange 更新 Extent，也避免目标 Offset 被旧滚动范围提前 Clamp。外层业务页面仍然可以使用自己的 `ScrollViewer`；`WheelZoomMode` 决定主图滚轮事件是否交还外层。

Presenter 的核心内部状态为：

```text
ViewportSize
SourcePixelSize
RenderScaling
EffectiveZoomFactor
RotationAngle
PanOffset
DestinationRect
```

图片的显示尺寸与最终绘制矩形按以下公式计算：

```text
RotatedSourceWidthPixel =
    RotationAngle 为 90/270 ? SourcePixelHeight : SourcePixelWidth

RotatedSourceHeightPixel =
    RotationAngle 为 90/270 ? SourcePixelWidth : SourcePixelHeight

DisplayWidthDip  = RotatedSourceWidthPixel  × EffectiveZoomFactor / RenderScaling
DisplayHeightDip = RotatedSourceHeightPixel × EffectiveZoomFactor / RenderScaling

CenteredX = (ViewportWidth  - DisplayWidthDip)  / 2
CenteredY = (ViewportHeight - DisplayHeightDip) / 2

DestinationX = CenteredX + PanOffset.X
DestinationY = CenteredY + PanOffset.Y

DestinationRect = (
    DestinationX,
    DestinationY,
    DisplayWidthDip,
    DisplayHeightDip)
```

Presenter 在 `Render` 中以图片中心为原点应用当前四分之一圈旋转，再把旋转后的完整图片边界绘制到 `DestinationRect`；`PART_Viewport` 负责裁掉超出其 Bounds 的部分。Stream 不得参与绘制；Presenter 只能使用尚未释放的 Lease 所提供的 `IImage`。

#### 平移边界与公开状态

目标公开：

```csharp
public bool IsPanEnabled { get; set; } = true;
public bool IsPanning { get; }
```

`IsPanning` 是只读状态，用于外观和业务观察，不允许外部强制写入。首版不公开 `PanOffset`、`TranslationX/Y` 或其他可写位移属性，避免开发者绕过边界校验或产生分轴的半更新状态。

平移以图片默认居中位置为零点。每个轴独立计算最大位移：

```text
MaximumPanX = max(0, (DisplayWidthDip  - ViewportWidth)  / 2)
MaximumPanY = max(0, (DisplayHeightDip - ViewportHeight) / 2)

PanOffset.X = Clamp(RequestedPanX, -MaximumPanX, +MaximumPanX)
PanOffset.Y = Clamp(RequestedPanY, -MaximumPanY, +MaximumPanY)
```

当图片在某个轴上不大于 Viewport 时，该轴的 `PanOffset` 强制为零并保持居中；只有一个轴超出时只允许沿该轴平移；两轴均未超出时不进入 Panning。首版禁止把图片拖出合法边界后露出空白，不提供弹性越界、回弹或惯性。

拖拽从按下时的位移快照计算，不逐事件累加误差：

```text
RequestedPan =
    PanOffsetAtPointerPressed
    + CurrentPointerPosition
    - PointerPressedPosition
```

因此鼠标把图片向右拖动时 `PanOffset.X` 直接增加，状态方向与用户看到的移动方向一致。

#### 拖拽输入状态机

只有主指针按钮在图片绘制表面按下时才可能开始平移。显式导航按钮、快捷操作区、缩略图和其他覆盖交互控件优先处理自己的输入，不得启动图片平移。

```text
Idle
  │ PointerPressed on image surface
  v
PressCandidate
  │ 累计移动超过拖拽阈值，且至少一个轴可平移
  v
Panning + Pointer Capture
  │ PointerReleased / CaptureLost / Cancel
  v
Idle
```

`PointerPressed` 不能立即移动图片。实现优先采用平台拖拽阈值；平台无法提供时使用约 `4 DIP` 的回退值。阈值按当前位置相对于最初按下位置的累计距离判断，不能按每个 Move 事件的单次 Delta 判断。

进入 `Panning` 后捕获当前 Pointer，使指针离开 Viewport 后仍能完成同一次拖拽。所有结束和异常路径必须调用同一个内部取消流程，完整清除 Candidate、按下位置、初始位移、Capture 和 `IsPanning`。至少在以下情况取消：

- 主指针按钮释放或 Pointer Capture 丢失。
- 当前图片或 Lease 被替换。
- 控件退出视觉树。
- 当前图片进入 Empty、Loading 或 Error 状态。
- `IsPanEnabled` 变为 `false`。
- 失去继续处理该次交互所需的平台 Pointer 状态。

#### 指针锚点与状态规整

滚轮缩放前，Presenter 根据旧 `DestinationRect` 计算光标在“当前已经旋转后的可视图片坐标系”中的归一化坐标：

```text
ImageU = (Anchor.X - OldDestination.Left) / OldDisplayWidth
ImageV = (Anchor.Y - OldDestination.Top)  / OldDisplayHeight
```

得到新显示尺寸后，保持锚点所需的目标左上角及平移量为：

```text
DesiredLeft = Anchor.X - ImageU × NewDisplayWidth
DesiredTop  = Anchor.Y - ImageV × NewDisplayHeight

RequestedPanX = DesiredLeft - NewCenteredX
RequestedPanY = DesiredTop  - NewCenteredY
```

四分之一圈旋转使显示宽高与坐标轴发生交换；实现若需要定位原始源像素，必须通过当前完整显示变换的逆矩阵把锚点映射回源坐标，不能把未旋转的 SourceRect 直接套入上述公式。最后使用新的平移边界 Clamp。图片内部区域具有足够平移空间时，缩放前后同一内容点应稳定在光标下方；锚点与合法边界发生冲突时，优先保证不露出空白；缩放后某个轴变成小图时，该轴优先居中。工具栏或程序化缩放没有指针锚点时使用 Viewport 中心。

缩放比例、新显示尺寸、目标 `PanOffset` 和 `DestinationRect` 必须通过同一个内部状态规整入口同步计算，然后一次请求重绘；不能把缩放和位移拆成依赖下一轮布局的两个可见阶段。快速连续滚轮输入从最新逻辑目标状态继续累计，不能反复从尚未呈现的旧比例起算。

以下变化统一进入同一个内部 `ReconcileViewportState` 语义流程，不能分别维护不同的边界算法：

- ZoomMode、CustomZoomFactor 或 ZoomRange 改变。
- RotationAngle 改变。
- Viewport Bounds 改变。
- 窗口跨屏导致 RenderScaling 改变。
- 当前图片及其源像素尺寸改变。
- 图片切换、加载完成或状态复位。

该流程重新计算有效比例、旋转后的显示尺寸、每轴平移能力、经过 Clamp 的位移和最终绘制矩形，并在必要时先取消当前拖拽。切换选择时立即取消拖拽，但 Loading 保留帧继续使用旧图片的既有几何状态；只有最新候选通过身份校验并提交时，才在同一 UI 操作中进入 `Fit`、恢复 `RotationAngle=0`、清零旧 `PanOffset`，并按新图片和当前 Viewport 重新居中。

#### 临时查看旋转

首版提供一个顺时针旋转90度的查看操作：

```csharp
public int RotationAngle { get; }
public ICommand RotateClockwiseCommand { get; }
public void RotateClockwise();
```

`RotationAngle` 是只读的 Viewport 状态，只能取 `0`、`90`、`180`、`270`，每次合法调用按照 `0 → 90 → 180 → 270 → 0` 前进。命令只在 `ImageState == Ready` 且当前 Lease 有效时可执行；Empty、Loading 和 Error 下禁用。首版不提供任意角度、逆时针按钮或旋转动画。

旋转围绕当前图片中心执行，只改变 ImageGallery 的显示变换：

```text
图片源解码及其原始 Orientation
            ↓
      Current IImage
            ↓
叠加临时 RotationAngle
            ↓
       Viewport 绘制
```

ImageGallery 不重新编码图片、不写回文件、不修改 EXIF Orientation、不上传修改结果，也不修改 ItemsSource 业务项。图片源或解码器负责先正确解释其能够支持的原始 Orientation，Gallery 的临时角度在该结果之上叠加。需要持久保存旋转的开发者必须在自定义 Toolbar 中实现业务命令，更新实际图片及新的 `IImageGallerySource.Identity`。

旋转时先取消正在进行的拖拽，并把 `PanOffset` 清零后重新居中，避免把旧坐标系位移机械应用到交换后的轴：

- `Fit`：按旋转后的宽高重新计算适应比例。
- `ActualSize`：继续保持一个源像素对应一个设备像素的含义。
- `Custom`：保留 `CustomZoomFactor`，但按旋转后边界重新计算平移能力。

切换到不同 Selection Identity 或当前 Source Identity 更新时，立即恢复 `RotationAngle=0`；返回此前项目也不恢复旧角度。Move 或仅业务元数据变化没有真正替换当前图片，因此保留当前角度。首版不建立逐图片旋转状态表，也不把临时角度纳入缓存 Key。

#### 主图相邻导航入口

主图查看区只支持显式的 Previous/Next 按钮，不提供点击图片左右边缘区域进行导航的隐式交互。根控件目标公开一个布尔属性：

```csharp
public bool IsViewportNavigationEnabled { get; set; } = true;
```

属性语义为：

| 值 | 行为 |
|---|---|
| `true` | 请求启用主图 Previous/Next；最终可见性还必须服从左右边栏占用规则 |
| `false` | 不创建或不呈现这两个按钮；缩略图导航和程序化选择不受影响 |

现在只有“启用”和“禁用”两种合法状态，因此不再引入导航模式枚举。公开只包含 `None` 与 `Buttons` 的枚举会给一个本质上的布尔决策制造无意义的类型复杂度。

主图导航使用“同侧存在即隐藏”的确定性规则，不执行逐像素相交判断：有效显示的 Filmstrip 或 Toolbar 位于 Left 时结构隐藏主图 Previous，位于 Right 时结构隐藏主图 Next；左右两侧分别被占用时两个按钮都隐藏。Top/Bottom 不影响主图左右按钮。只有实际参与当前布局的边栏才算占用：空集合或显式关闭导致 Filmstrip 结构隐藏、Toolbar 无有效内容或显式关闭、以及响应式降级已经隐藏对应边栏时，必须恢复该侧按钮。Toolbar 冲突规整后按照 `EffectiveToolbarPlacement` 判定，不读取开发者请求位置。

```text
┌─────────────────────────────────────────┐
│                                         │
│ [ ◀ ]            当前图片         [ ▶ ] │
│                                         │
└─────────────────────────────────────────┘
```

主图的非按钮区域绝不能承担 Previous/Next 命中职责，也不公开左右命中区宽度、比例或模板部件。图片缩放后的拖动平移只与图片内容区域交互；指针落在显式按钮上时，由按钮优先处理输入，不启动图片平移。

##### 相邻导航边界与循环

目标公开循环开关与统一导航命令：

```csharp
public bool IsLoopNavigationEnabled { get; set; } = false;

public ICommand PreviousCommand { get; }
public ICommand NextCommand { get; }
```

`PreviousCommand` 与 `NextCommand` 由根 `ImageGallery` 创建、持有并以只读属性公开，命令实例在控件生命周期内保持稳定；开发者可以执行或绑定它们，但不能替换。Viewport 和缩略图走廊中的两组默认按钮必须使用这两个公共命令，`ToolbarContent` 等受支持的业务扩展入口也可以绑定同一命令。禁止为不同视觉入口创建行为略有差异的私有导航命令。

两个命令不接收参数，只向唯一 `SelectionModel` 提交相邻选择请求，不直接修改主图 Presenter、缩略图容器、走廊 Offset 或加载状态。快速连续执行时立即接受最新合法选择；旧主图请求按既有取消与请求版本规则失去提交资格，不建立导航队列，也不让旧图片加载完成后覆盖最新选择。

该属性只控制相邻图片选择是否在首尾之间循环，不控制缩略图走廊自身的滚动位置。按钮可用性由当前已提交选择、集合数量和循环开关共同派生：

```text
CanNavigatePrevious =
    ItemCount > 1
    && (SelectedIndex > 0 || IsLoopNavigationEnabled)

CanNavigateNext =
    ItemCount > 1
    && (SelectedIndex < ItemCount - 1 || IsLoopNavigationEnabled)
```

默认非循环：首项禁用 Previous，末项禁用 Next，中间项两个按钮都可用。开启循环且集合至少包含两项时，从首项执行 Previous 选择末项，从末项执行 Next 选择首项。空集合和单项集合始终禁用两个按钮；单项循环没有导航意义，不能触发同一图片的无意义重载。

上述派生结果直接成为两个命令的 `CanExecute`，并在选择、集合数量或循环开关发生有效变化时发出 `CanExecuteChanged`。首版不重复公开 `CanNavigatePrevious`、`CanNavigateNext` 等只读属性；默认按钮与开发者自定义按钮均通过 `ICommand.CanExecute` 获得一致的启用状态。当前主图处于 `Loading` 或 `Error` 不会单独禁用相邻导航，只要目标相邻项在集合规则下仍然合法即可。

首尾边界只令仍被呈现的按钮进入 Disabled 状态，不因命令不可执行而从布局中隐藏。`IsViewportNavigationEnabled = false` 表示整组主图导航按钮不创建或不呈现；此外，有效左右边栏会按照上述同侧占用规则结构隐藏对应一侧按钮。这两种隐藏都必须同时移除布局、命中测试和不可见交互区域。

每次 Previous/Next 只请求选择集合稳定顺序中的一个相邻业务项，不滚动一整页缩略图、不跳过未缓存项或失败项，也不直接操作 Viewport。请求统一提交到 `SelectionModel`；目标图片加载失败时，选择仍然有效并进入该项的 `Error` 状态。

按钮可用性不依赖 `ImageState == Ready`。当前选择处于 `Loading` 时，用户仍可连续导航：

```text
A -> B (Request 20, Loading)
  -> C (Request 21, Loading)
  -> D (Request 22, Loading)
```

每次点击立即同步提交新选择并创建新的主图请求版本，旧请求取消且失去提交资格。导航操作不进入等待图片加载完成的异步队列；用户停止点击后，控件不能继续播放旧的排队动作。快速连续点击的最终当前选择是最后一次合法提交，过期结果按照主图加载状态机释放。

集合变化与导航发生在 Avalonia UI 线程的协调路径中。每次点击根据当时已经提交的 `SelectedIndex` 和当前集合快照计算相邻候选，并在提交前验证候选仍有效；不能缓存一个过期目标索引供稍后执行。

##### 主图与缩略图走廊共享相邻导航

ImageGallery 允许主图两侧和缩略图走廊各提供一组 Previous/Next 按钮。两组按钮是同一相邻选择能力在不同位置上的入口，不承担不同命令：

```text
Viewport Previous ──┐
Filmstrip Previous ─┼──> RequestPreviousSelection()
                    │            │
Viewport Next ──────┤            v
Filmstrip Next ─────┘       SelectionModel
```

这项视觉冗余是有意设计：部分业务布局不能在主图左右覆盖按钮，开发者仍可依靠缩略图走廊两端按钮切换图片。不得把走廊 Previous/Next 改成“缩略图向前/向后翻一页”，否则关闭主图按钮后会失去明确的相邻图片导航入口。

两组入口分别控制是否呈现：

```csharp
public bool IsViewportNavigationEnabled { get; set; } = true;
public bool IsFilmstripNavigationEnabled { get; set; } = true;
```

两个属性不是互斥模式，可以同时开启、分别关闭或同时关闭。它们只控制各自位置上的视觉入口，不改变缩略图点击和程序化选择能力。两组按钮共同使用 `PreviousCommand.CanExecute` / `NextCommand.CanExecute`，共同遵守 `IsLoopNavigationEnabled`、空集合、单项集合、首末项、Loading 连续点击和不排队规则。

主图按钮和走廊按钮必须使用不同的内部模板部件名称，以便包内模板明确定位，但无障碍名称和行为语义均为“上一张图片”和“下一张图片”；不能把走廊按钮播报成“滚动缩略图”。两处共享同一套按钮行为和内部状态机，但分别读取 `ViewportNavigationButtonAppearance` 与 `FilmstripNavigationButtonAppearance`，因此可以具有不同尺寸和视觉而不开放按钮模板。

缩略图走廊的内容滚动是独立交互，不占用两端按钮：

```text
Filmstrip Previous / Next click
    └── 改变 SelectionModel，一个相邻项目

Wheel / touchpad / direct scroll on thumbnail strip
    └── 只改变缩略图可视窗口，不改变 SelectionModel
```

选择发生变化时，缩略图走廊执行最小必要滚动，使新的选中缩略图完整可见；选中项已经完整可见时不移动，不强制把它居中。用户仅滚动缩略图走廊而没有改变选择时，控件不得立即把可视窗口强行拉回当前选中项。下一次真实选择变化时，再确保新的当前缩略图可见。

#### 显式导航按钮

显式导航按钮不能使用 `AtomUI.Desktop.Controls` 中的 `IconButton`、`ImagePreviewNavButton` 或 `CarouselNavButton`。ImageGallery 包在固定 AXAML 中使用自己的内部按钮角色：

```csharp
internal sealed class ImageGalleryNavigationButton : Avalonia.Controls.Button
{
}
```

该类型不属于公共 API，不定位为 Labs 通用按钮，也不能被开发者在外部 AXAML 中直接实例化或重新设置模板。它只负责包内命中、命令、伪类、自动化语义和固定视觉结构。

根控件以属性级 Appearance 分别开放两处按钮的视觉参数：

```csharp
public ImageGalleryButtonAppearance? ViewportNavigationButtonAppearance { get; set; }
public ImageGalleryButtonAppearance? FilmstripNavigationButtonAppearance { get; set; }
```

Viewport 与 Filmstrip 的 Previous/Next 在内部继续使用稳定且互斥的角色伪类：

```text
位置：    :viewport / :filmstrip
语义：    :previous / :next
主轴：    :horizontal / :vertical
```

主图按钮固定为 `:viewport:horizontal`；走廊按钮使用 `:filmstrip`，主轴伪类由当前 Filmstrip Placement 派生。伪类只供包内固定 `ControlTheme` 选择正确箭头方向与状态视觉，不能作为外部模板扩展点。Previous 与 Next 在同一位置共享一个 Appearance；箭头方向由按钮语义和主轴自动派生。主图与走廊需要差异时，分别设置上述两个 Appearance 属性，不增加成对的 `PreviousButtonBackground`、`NextButtonBackground` 等根属性。

`ImageGalleryButtonAppearance` 至少开放：

- `Background`。
- `BorderBrush` 与 `BorderThickness`。
- `Opacity`。
- 箭头画刷与箭头尺寸。
- `CornerRadius`、`Padding` 和命中尺寸。
- PointerOver、Pressed、Disabled 视觉状态。

边框颜色和边框厚度必须成对考虑；默认厚度为零时，单独改变边框颜色不能产生可见边框。箭头颜色也必须可定制，避免背景改变后失去对比度。

概念用法：

```xml
<atom.labs:ImageGallery IsViewportNavigationEnabled="True">
    <atom.labs:ImageGallery.ViewportNavigationButtonAppearance>
        <atom.labs:ImageGalleryButtonAppearance
            Background="#66000000"
            BorderBrush="White"
            BorderThickness="1"
            CornerRadius="20"
            Opacity="0.8"
            Width="40"
            Height="40"
            IconSize="18" />
    </atom.labs:ImageGallery.ViewportNavigationButtonAppearance>
</atom.labs:ImageGallery>
```

Appearance 只覆盖显式设置的成员；未设置成员继续使用包内默认 `ControlTheme` 和 AtomUI Token。它不包含 `Template`、`ControlTheme`、`Styles`、`DataTemplate` 或子内容，不能改变按钮 AXAML 结构。

Previous、Next、Up、Down、Add、Subtract 和 RotateClockwise 使用包内固定的几何矢量 Glyph，不使用受字体基线影响的文本字符。Glyph 的正方形布局尺寸由 `ImageGalleryButtonAppearance.IconSize` 控制，几何中心必须与按钮内容区中心严格重合；`Foreground` 及 PointerOver、Pressed、Disabled 前景状态直接驱动矢量画刷。Toolbar 的 Zoom In、Zoom Out 和 Rotate 默认按钮分别使用 Add、Subtract 和 RotateClockwise Glyph。

### Toolbar 角色

浮动工具栏用于表达当前图片标题、显示模式、缩放比例、临时旋转和开发者快捷操作。首版默认内容采用稳定的逻辑顺序：

```text
Toolbar
├── Title Region
├── Zoom Region
│   ├── ZoomMode / EffectiveZoomFactor
│   └── 默认缩放操作
├── Rotation Region
│   └── Rotate Clockwise 90°
└── Custom ToolbarContent                 固定末端
```

开发者可以分别隐藏三个默认区域，并在末端追加任意 Avalonia 内容：

```csharp
public bool IsToolbarTitleVisible { get; set; } = true;
public bool IsToolbarZoomControlsVisible { get; set; } = true;
public bool IsToolbarRotationVisible { get; set; } = true;

public object? ToolbarContent { get; set; }
public IDataTemplate? ToolbarContentTemplate { get; set; }
```

三个显隐属性只控制默认 Toolbar 中对应区域，不取消根控件自身的标题数据、缩放能力或旋转能力。隐藏区域必须同时移除其布局、命中、分隔线和间距，不能留下空白占位；`IsToolbarRotationVisible=false` 后，开发者仍可从自定义内容调用 `RotateClockwiseCommand`。

`ToolbarContent` 是自定义快捷操作的单一扩展槽，只能排列在全部可见默认区域之后。首版不开放 Start/End 选择，也不允许 Content 插入标题、缩放或旋转区域之间。`ToolbarContentTemplate` 仅负责呈现 `ToolbarContent`，不得替换整个 Toolbar 外壳；彻底改变默认区域顺序或结构不属于首版支持的定制能力，也不能通过替换 Toolbar 模板实现。

```xml
<atom.labs:ImageGallery>
    <atom.labs:ImageGallery.ToolbarContent>
        <StackPanel Orientation="Horizontal" Spacing="8">
            <Button Content="下载"/>
            <Button Content="删除"/>
            <Button Content="分享"/>
        </StackPanel>
    </atom.labs:ImageGallery.ToolbarContent>
</atom.labs:ImageGallery>
```

当三个默认区域全部隐藏且 `ToolbarContent` 为 null 时，Toolbar 没有有效内容并结构隐藏，不创建空壳背景或命中区域。`IsToolbarVisible=false` 始终拥有更高优先级；有内容但进入 Compact/Minimal 时仍遵守响应式结构隐藏。自定义内容的数据上下文遵守普通 Avalonia 内容继承，不由 ImageGallery 替换成内部 Coordinator；开发者需要当前项时应显式绑定祖先 ImageGallery 的 `SelectedItem` 或其他公共状态。

Toolbar Presenter 和缩略图走廊共用同一套根控件边缘概念：

```csharp
public ImageGalleryEdgePlacement ToolbarPlacement { get; set; }
    = ImageGalleryEdgePlacement.Top;

public ImageGalleryEdgePlacement EffectiveToolbarPlacement { get; }

public double ToolbarEdgeGap { get; set; } = 16.0;
public double ToolbarAlongEdgeInset { get; set; } = 16.0;
public double OverlayElementGap { get; set; } = 8.0;
public double ToolbarRegionSpacing { get; set; } = 16.0;
public double ToolbarItemSpacing { get; set; } = 8.0;

public ImageGalleryEdgeAlignment ToolbarAlongEdgeAlignment { get; set; }
    = ImageGalleryEdgeAlignment.Center;

public bool IsToolbarVisible { get; set; } = true;
```

`ToolbarPlacement` 表达开发者请求，`EffectiveToolbarPlacement` 是根协调器只读派生的实际位置。控件不得为了冲突规整而回写 `ToolbarPlacement`，否则会破坏 Binding、Style 以及开发者对原始配置的观察。

Toolbar 默认内容的方向由 `EffectiveToolbarPlacement` 确定性派生，不公开一套可能与位置冲突的独立 Orientation：

```text
Effective Placement    Default Toolbar Orientation
Top / Bottom           Horizontal
Left / Right           Vertical
```

方向变化只改变默认 Title、Zoom、Rotation 和 Custom Content 四个顶层区域的主轴排列，以及默认区域内部操作单元的主轴排列；它不旋转文字、图标或开发者自定义内容，也不改写 `ToolbarContent` 自己声明的内部 Panel 与 Orientation。默认区域的逻辑顺序始终是 Title、Zoom、Rotation、Custom Content。

Toolbar 的完整内容组在外壳可用内容区中水平居中。Top/Bottom 时，标题、缩放百分比、所有操作按钮和自定义内容槽共享同一条水平中心线；Left/Right 时，所有角色共享同一条垂直中心轴。内部使用专用 Centered Toolbar Panel 在 Arrange 阶段按每个角色真实 DesiredSize 求解交叉轴位置，不能仅给 StackPanel 子项设置 Alignment，因为不同高度或宽度的文案、按钮和自定义内容仍可能从交叉轴起点排列。`ToolbarContent` 的 Presenter 双轴居中，但自定义内容内部如何排列仍由开发者自己的 Panel 决定。

默认 Toolbar 必须同时满足外框几何居中和可见内容光学居中。Zoom In、Zoom Out 和 Rotate 使用以上几何 Glyph；标题、缩放百分比、Fit 和 `1:1` 仍是可本地化或具有文字语义的标签，内部文字 Presenter 必须依据 Avalonia `TextLayout.Extent` 与 `OverhangAfter` 计算可见墨迹中心，不能只居中包含 Ascender、Descender 和字体回退留白的行框，也不能为不同语言或字符逐项硬编码 Margin/TranslateTransform 偏移。

内部 `ImageGalleryToolbar` 根据有效位置设置互斥的 `:horizontal` / `:vertical` 和 `:top` / `:bottom` / `:left` / `:right` 角色伪类，使包内固定 Theme 能选择上下与左右位置所需的布局结构。开发者通过 `ToolbarAppearance` 设置 Width、Height、Padding 和圆角；伪类只反映 `EffectiveToolbarPlacement`，不会回写开发者请求属性，也不开放为外部模板选择器合同。

`ToolbarRegionSpacing` 表示相邻可见顶层区域之间的主轴距离；`ToolbarItemSpacing` 表示 Zoom、Rotation 等默认区域内部相邻基础操作单元之间的主轴距离。隐藏区域不保留间距，`ToolbarContent` 整体只被视为一个末端区域，ImageGallery 不把该自定义内容的子树拆开并强行应用 `ToolbarItemSpacing`。两个属性均使用 DIP，必须有限且不小于零；非法值属于开发者配置错误。

Toolbar 的 `Width`、`Height`、`MinWidth`、`MaxWidth`、`MinHeight` 和 `MaxHeight` 通过 `ToolbarAppearance` 配置，不在根控件上重复增加成组转发属性。固定尺寸仍服从 Overlay 安全区和 Normal/Compact/Minimal 降级：尺寸无法满足时结构隐藏，不能越过走廊、裁掉仍可命中的半个按钮或跑出 ImageGallery。

当缩略图走廊在结构上参与布局时，它拥有同边缘位置优先权：

```text
Requested Toolbar Edge     Filmstrip Edge     Effective Toolbar Edge
Top                        Top                Bottom
Bottom                     Bottom             Top
Left                       Left               Right
Right                      Right              Left
任意不同边缘                不同               Requested Toolbar Edge
```

即同边缘冲突通过 `Opposite(ThumbnailFilmstripPlacement)` 确定性规整，不抛异常、不排队，也不根据剩余像素临时猜测另一条边。移动的是 Toolbar Presenter，控件不得旋转、重排或改写开发者放入操作栏的自定义内容；Toolbar 内部保持默认区域在前、自定义 `ToolbarContent` 固定末端的既定顺序。

未来如果增加自动隐藏，走廊仅因透明度动画或 Pointer 离开而暂时不可见时仍应参与该位置计算，避免 Toolbar 在相对边缘间来回跳动。首版只有显式关闭、空集合和 Minimal 响应式降级会使走廊退出结构；此时 `EffectiveToolbarPlacement` 恢复为请求的 `ToolbarPlacement`，但 Minimal 下 Toolbar 自身仍保持响应式隐藏。

### Overlay 相邻边缘避让与响应式降级

同边缘规整完成后，走廊与 Toolbar 位于相邻边缘时仍可能在 ImageGallery 拐角发生实际 Bounds 碰撞，例如 Left + Top 占用左上角。首版采用“走廊优先、Toolbar 沿原边缩短、最后结构隐藏”的确定性规则，不再把 Toolbar 跳到第三条边：

```text
Filmstrip = Left，Toolbar = Top

发生碰撞：                         避让后：
┌─────────────────────┐           ┌─────────────────────┐
│XXXX Toolbar         │           │走廊│ Gap │ Toolbar   │
│XXXX                 │           │走廊│                 │
│走廊│    Viewport    │           │走廊│    Viewport     │
└─────────────────────┘           └─────────────────────┘

XXXX = 相交区域
```

默认模板使用内部专用 `ImageGalleryOverlayPanel` 统一 Measure 与 Arrange Viewport、Viewport Navigation、Toolbar Presenter 和 Filmstrip Presenter。各子组件不得分别通过 Margin 猜测其他组件位置。空间分配顺序固定为：

```text
1. Viewport 获得完整根内容区域
2. Filmstrip 按 Placement、EdgeGap、AlongEdgeInset 布局
3. Filmstrip Bounds 外扩 OverlayElementGap，形成占用区
4. Toolbar 从自己所在边缘的可用线段中扣除该占用区
5. Toolbar 在剩余安全线段中按照 Alignment 布局
6. Viewport Previous / Next 按左右边栏的有效最终位置应用“同侧存在即隐藏”
7. 空间不足时进入固定响应式降级
```

`ToolbarEdgeGap` 表示 Toolbar 与其有效边缘的距离，`ToolbarAlongEdgeInset` 表示 Toolbar 沿该边缘两端的保留距离，`OverlayElementGap` 是 Toolbar、走廊和其他可交互 Overlay 之间的最小安全间距。三者均使用 DIP，必须有限且不小于零；非法值属于配置错误。

Toolbar 沿边对齐使用共用枚举：

```csharp
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

public ImageGalleryResponsiveState ResponsiveState { get; }
```

“优先缩短 Toolbar”只表示先限制 Toolbar Presenter 可占用的沿边布局区域。实现不得通过 ScaleTransform 缩小按钮、字体和图标，不得裁掉一部分仍保留命中，也不得擅自重排开发者自定义内容。能够响应约束的内容可以在安全区内自行收缩或换行；其最小布局要求已经无法满足时，整个 Toolbar 进入响应式结构隐藏。

响应式降级必须单调执行：

```text
Normal
├── Filmstrip 显示
├── Toolbar 显示，但优先使用被缩短的安全区
└── Viewport Navigation 按配置显示
          │ Toolbar 已无法完整、正常使用
          v
Compact
├── Filmstrip 保留
├── Toolbar 响应式结构隐藏
└── Filmstrip Items 可视区随剩余空间缩短
          │ 走廊连“配置为可见的 Add、走廊导航按钮、
          │ 至少一个完整 Thumbnail Item 和内部间距”都放不下
          v
Minimal
├── Toolbar 保持隐藏，不得重新出现
├── Filmstrip 响应式结构隐藏
├── Viewport Navigation 能满足最小命中尺寸时保留，否则隐藏
└── Viewport 始终保留
```

缩短 Filmstrip 时只减少同时可见的 Thumbnail Item 数量和 Items Viewport 长度，不缩小 `ThumbnailItemExtent`、Add Image 或 Previous/Next 按钮。响应式隐藏只改变视觉结构，不改变开发者请求属性、Selection、当前主图 Lease、缓存或 ItemsSource。空间恢复后，走廊重新建立虚拟窗口并以最小必要滚动显示当前选中项，不机械恢复旧方向或旧尺寸下已经失效的 Offset。

为避免窗口尺寸在临界值附近变化时反复显示/隐藏，Normal、Compact、Minimal 的恢复阈值相对进入阈值增加内部 `8 DIP` 滞回区，首版不公开配置，也不做淡入淡出动画。结构隐藏前若目标元素仍持有 Pointer Capture，先取消该交互再移除命中区域。

空间分配优先级与默认 Z-Index 分开定义但保持一致：

```text
空间优先级：Filmstrip > Toolbar > Viewport Navigation > 装饰
Z-Index：   Viewport 0，Viewport Navigation 10，Toolbar 20，Filmstrip 30
```

正常布局不应依靠 Z-Index 遮住冲突；Z-Index 只是在自定义内容或极端尺寸意外相交时保证走廊命中优先。Overlay Panel 自己不得使用吞掉整个 Viewport 输入的透明命中背景，根内容必须裁剪到 ImageGallery Bounds。

### Navigator 角色

缩略图走廊负责表达图片集合中的位置和当前选择。点击缩略图项或走廊 Previous/Next 后，根控件应更新唯一选择，主图、工具栏和缩略图选中视觉从同一选择状态同步，不能分别维护彼此可能失配的索引。走廊按钮与主图按钮共享相邻选择语义；缩略图走廊本身的滚动只改变可视窗口，不改变选择。

### 缩略图走廊位置、方向与边距

“边缘”统一指当前 `ImageGallery` 根控件的布局边界，不指应用 `Window`、屏幕工作区或任意外层容器。可复用控件不得读取应用窗口边界来定位自己的缩略图走廊。

首版公开以下布局属性：

```csharp
public enum ImageGalleryEdgePlacement
{
    Top,
    Bottom,
    Left,
    Right
}

public ImageGalleryEdgePlacement ThumbnailFilmstripPlacement { get; set; }
    = ImageGalleryEdgePlacement.Bottom;

public double ThumbnailFilmstripExtent { get; set; } = 120.0;
public double ThumbnailFilmstripEdgeGap { get; set; } = 16.0;
public double ThumbnailFilmstripAlongEdgeInset { get; set; } = 16.0;
```

`ThumbnailFilmstripExtent` 是整个走廊外壳在交叉轴上的目标尺寸，包含走廊边框和 `FilmstripAppearance.Padding`：Top/Bottom 时表示走廊高度，Left/Right 时表示走廊宽度。它不表示单张缩略图主轴长度，也不包含走廊到 ImageGallery 边缘的外部 Gap。使用方向无关的 Extent 可以避免 Placement 改变后 `Height` 与 `Width` 两套根属性互相冲突。

`ThumbnailFilmstripEdgeGap` 是走廊靠近 Placement 一侧的外边缘与 ImageGallery 同侧边界之间的距离。`ThumbnailFilmstripAlongEdgeInset` 同时控制走廊沿所在边缘方向的两个端点与根控件另外两条边之间的最小距离。“AlongEdge”在 Top/Bottom 时表示水平，在 Left/Right 时表示垂直，不能误称为交叉轴：

```text
Placement = Bottom

┌──────────────── ImageGallery ────────────────┐
│                    Viewport                   │
│                                               │
│  ← AlongEdgeInset →                 ← 同值 → │
│       ┌──── Previous / Items / Next ────┐     │
│       └──────── Thumbnail Filmstrip ────┘     │
│                    ↑ EdgeGap                  │
└───────────────────────────────────────────────┘

Placement = Left

┌──────────────── ImageGallery ────────────────┐
│       ↑ AlongEdgeInset                        │
│  Gap  ┌──────────┐                            │
│  ←→   │ Previous │                            │
│       │  Items   │          Viewport          │
│       │   Next   │                            │
│       └──────────┘                            │
│       ↓ AlongEdgeInset                        │
└───────────────────────────────────────────────┘
```

`ThumbnailFilmstripExtent` 必须是有限且大于零的 DIP；两个距离属性必须有限且不小于零。`NaN`、无穷、非法非正 Extent 和负距离属于开发者配置错误。`ThumbnailFilmstripPlacement` 与 `ToolbarPlacement` 的未知枚举值同样属于配置错误。Gallery 实际尺寸暂时不足以容纳配置的走廊或间距不属于异常；布局按照已经确定的 Normal、Compact、Minimal 响应式规则规整，不能因为尺寸不足抛出配置异常。

走廊方向是 Placement 的确定性派生状态，不再公开可独立设置的 `Orientation`：

```text
Placement     Orientation    Main Scroll Axis    Buttons
Top           Horizontal     Horizontal          Left / Right
Bottom        Horizontal     Horizontal          Left / Right
Left          Vertical       Vertical            Up / Down
Right         Vertical       Vertical            Up / Down
```

因此不存在 `Placement=Left`、`Orientation=Horizontal` 之类的矛盾配置。走廊两端按钮只改变图标方向和布局位置，命令语义仍然是稳定集合顺序中的 Previous Image / Next Image，不随视觉方向改写业务顺序。

首版 Placement 始终表示主图上方的 Overlay 位置，不表示 Dock。缩略图走廊不占用或挤压 Viewport 的 Measure 空间。若未来需要走廊挤压主图的 Docked 模式，必须增加独立布局模式并重新设计空间分配，不能让同一个 Placement 值暗含两套行为。

运行中改变合法 Placement、Extent 或间距时，保持当前 Selection、当前主图 Lease、缩放与平移状态不变；走廊重新 Measure，重建对应方向的虚拟窗口和目标缩略图解码尺寸，并以最小必要滚动使当前选中缩略图完整可见。横向 Offset 与纵向 Offset 没有可交换语义，切换方向后不得机械复用旧 Offset。

### 缩略图走廊输入协调

缩略图项的点击在 Pointer Released 阶段提交。Pointer Pressed 后先进入候选状态；移动量未超过当前平台拖动阈值便释放时，提交该业务项选择。移动量超过阈值后，本次手势转换为沿走廊主轴的直接滚动，并永久取消本次点击候选，直到 Pointer Released 或 Cancelled：

```text
Pointer Pressed
      │
      ├── 未超过阈值并 Released ──> Select Thumbnail Item
      │
      └── 超过阈值 ──> Capture Pointer ──> Scroll Filmstrip
                                            └── 不再提交选择
```

触摸与按住鼠标左键拖动采用同一状态机。实现优先使用平台拖动阈值；平台未提供时使用 `4 DIP` 的内部回退值。滚动期间选择不随经过的缩略图改变，释放后也不能补发最后落点的 Click。

滚轮和触控板只改变走廊可视窗口，不改变 SelectionModel：

```text
Top / Bottom
├── 明确的水平 Delta ──> 沿水平方向滚动
└── 只有垂直 Delta ────> 映射为水平滚动

Left / Right
├── 明确的垂直 Delta ──> 沿垂直方向滚动
└── 只有水平 Delta ────> 映射为垂直滚动
```

同一事件同时包含主轴与交叉轴 Delta 时优先使用主轴分量，不能把两个分量相加造成速度突增。`PART_Filmstrip` 的完整视觉矩形是统一输入区域，追加按钮、Previous/Next 按钮、缩略图列表、按钮间距、Padding 和边框上的 Wheel/Touchpad 输入都进入同一走廊滚动路径。

只要走廊主轴存在溢出，走廊就消费该区域中的 Wheel/Touchpad 事件；已经位于首尾且输入继续越界时也不得把事件交给外层页面，避免指针未离开走廊却突然滚动整个页面。只有主轴不存在溢出时，走廊才不消费事件并允许其沿 Avalonia 路由传给外层滚动容器。该策略属于固定行为，不增加“禁用走廊滚轮”或边界滚动链公开属性。

缩略图走廊不处理 Pinch/Scale，不改变缩略图槽位大小，也不得隔空缩放主图。只有从主图 Viewport 开始的合法 Pinch/Scale 才进入主图缩放路径。ImageGallery 专用键盘导航与焦点策略明确作为首版之后的 TODO，不属于首版实现与验收范围。

无论缩略图处于 Loading、Ready 或 Error，只要对应业务项仍有效，点击均可提交选择。新选择继续遵守“最小必要完整可见”规则，不强制居中、不整页翻动；快速连续选择由新选择和新主图请求立即取代旧请求，不建立动作队列。

### 缩略图走廊结构可见性

开发者通过根控件属性显式控制走廊是否参与结构布局：

```csharp
public bool IsThumbnailFilmstripVisible { get; set; } = true;
```

走廊先由配置和当前集合确定是否具备结构显示资格，再由响应式状态决定当前是否真正呈现：

```text
FilmstripStructuralEligibility =
    IsThumbnailFilmstripVisible
    && ItemsCount > 0

EffectiveFilmstripVisible =
    FilmstripStructuralEligibility
    && ResponsiveState != Minimal

EffectiveToolbarVisible =
    IsToolbarVisible
    && ((ItemsCount > 0
         && (IsToolbarTitleVisible
             || IsToolbarZoomControlsVisible
             || IsToolbarRotationVisible))
        || ToolbarContent != null)
    && ResponsiveState == Normal
```

规则如下：

```text
ItemsCount = 0
└── 走廊结构隐藏
    ├── 不 Measure / Arrange 走廊内容
    ├── 不创建可命中的空壳区域
    ├── 不参与 Toolbar 边缘冲突计算
    └── EffectiveToolbarPlacement 恢复为 ToolbarPlacement

ItemsCount = 1
└── 走廊正常显示
    ├── 显示唯一缩略图
    ├── 显示默认 Add Image 入口
    └── Previous / Next 仍可见但按既有规则禁用

IsThumbnailFilmstripVisible = false
└── 无论集合数量多少，整个走廊结构隐藏
```

空集合可能来自开发者未提供数据，也可能是首次异步加载、筛选为空或用户尚未添加图片。控件不抛异常、不猜测业务原因，只执行相同的结构隐藏策略。首版不实现 Pointer 离开后的自动淡出，因此不会出现“视觉不可见但仍参与冲突”的运行状态；这项区分只为未来自动隐藏能力保留契约边界。

### Thumbnail Item 角色

缩略图项负责呈现缩略图走廊中的单张轻量预览、选中状态和交互状态。主图资源与缩略图资源不强制分离：开发者提供专用缩略图时优先使用；未提供时，以主图源发起带目标像素尺寸的缩略图加载请求。缩略图虚拟化、并发加载、预取、内存缓存和状态视觉遵守本文对应的专门合同。

### 缩略图来源与内存缓存

缩略图走廊使用以下固定来源优先级：

```text
1. IImageGalleryItem.ThumbnailImageSource 提供有效专用缩略图源
       └── 按 Thumbnail 用途和目标像素尺寸加载

2. 未提供专用源或其绑定值为 null
       └── 使用 MainImageSource，以 Thumbnail 用途和目标像素尺寸加载
```

开发者提供专用缩略图源是网络、OSS 和超大图片场景的首选方式。缩略图文件、CDN 变体或业务缩略图服务保存在哪里，完全由开发者及其 `IImageGallerySource` 控制；ImageGallery 不参与路径规划或持久化。

回退到主图源时，图片源应尽可能按请求的 `TargetPixelSize` 解码成小尺寸 `IImage`，而不是先长期持有完整解码原图再缩小绘制。目标像素尺寸由缩略图槽位 DIP 尺寸、当前 `RenderScaling` 和已确定的内部解码尺寸分档共同计算。小尺寸解码可以降低解码后内存、GPU 上传和绘制成本，但如果远端只提供原始文件，它不保证减少网络下载字节；真正需要节省流量时应提供服务端或 CDN 缩略图源。

首版默认提供有容量限制的运行期内存缓存，不提供磁盘缓存：

- 不自动创建缩略图目录，不把远程、私有或本地图片静默写入磁盘。
- 不公开 `ThumbnailCacheDirectory`、磁盘缓存开关或持久缓存注入接口。
- 不承诺应用重启后复用上一次运行的解码结果。
- 开发者需要跨启动复用时，应通过 `IImageGalleryItem.ThumbnailImageSource` 提供自己持久化管理的缩略图源，而不是由 UI 控件管理文件生命周期。

内存缓存的概念 Key 至少包含：

```text
Thumbnail Source Identity
+ ImageGalleryImagePurpose.Thumbnail
+ Target Pixel Size Bucket
```

相同 Key 的并发加载应合并为一个底层读取/解码工作，多个消费者各自取得受 Lease 协议管理的引用。缓存采用有上限的 LRU 或等价淘汰策略；缓存淘汰只释放缓存持有的引用，当前可见项或 Overscan 项仍持有 Lease 时，图片不能被提前销毁。

缓存只保证同一应用运行期间的机会性复用，不保证命中。容器和缓存都只释放自己持有的 Lease 或内部引用；共享 `IImage` 的真实销毁由统一 Release Callback 与内部引用计数决定，不能被某个缩略图容器直接销毁。

### 缩略图内存预算与解码尺寸分档

目标公开缩略图解码缓存预算：

```csharp
public long ThumbnailCacheMemoryBudgetBytes { get; set; }
    = 32L * 1024 * 1024;
```

该预算只约束缓存额外持有的唯一解码缩略图资源，不限制当前 Visible 和 Overscan 容器为了正确呈现而持有的 Lease，也不是进程总内存硬上限。同一个共享图片资源即使存在多个消费者，也只按唯一解码资源的 `EstimatedMemorySizeBytes` 统计一次；同一 Source 的不同解码尺寸是不同资源，分别统计。

预算必须大于等于零，负数属于配置错误。`0` 表示不保留机会性缩略图缓存：Visible/Overscan 项仍然正常加载，离开虚拟范围且没有其他消费者后释放；再次进入时可能重新加载。运行中缩小预算时，缓存按 LRU 或等价的最近使用顺序立即释放缓存引用，直到缓存自身的估算总量不超过新预算；当前容器引用不被强制销毁。增大预算只供后续请求逐步使用，不立即批量加载。

精确目标像素尺寸来自缩略图槽位 DIP 尺寸与当前 `RenderScaling`。不同 DPI、跨屏、运行时宽度调整和浮点布局取整可能让同一图片产生仅相差一两个像素的请求，例如 `191×143`、`192×144` 和 `193×145`。如果它们都成为独立 Cache Key，会降低命中率并重复解码。

首版将请求的两个像素维度分别向上规整到 `16 pixel` 的整数倍，得到内部 `TargetPixelSizeBucket`：

```text
BucketWidth  = RoundUp(RequestedPixelWidth,  16)
BucketHeight = RoundUp(RequestedPixelHeight, 16)

96 × 72    -> 96 × 80
120 × 90   -> 128 × 96
191 × 143  -> 192 × 144
192 × 144  -> 192 × 144
193 × 145  -> 208 × 160
```

向上而不是向下规整，避免用分辨率不足的缓存作为最终图后再放大。`16 pixel` 是内部缓存实现参数，不公开为 StyledProperty；它需要通过清晰度、内存和命中率 Benchmark 验证，未来可以在不改变公共 API 与可观察语义的前提下调整。

缓存查找按以下规则进行：

```text
1. 优先相同 Source Identity、处理策略和精确尺寸分档。
2. 没有精确分档时，使用能够覆盖目标的最小较大分档。
3. 只有较小分档时，可暂时作为低清占位，同时请求当前所需分档。
4. 完全没有时，呈现 Loading 并创建共享加载需求。
```

较小版本不能作为更高 DPI 或更大槽位下的最终清晰结果。较大版本可以缩小复用，但缓存压力下优先保留更贴近当前需求的版本，避免用明显过大的解码资源长期服务很小的槽位。DPI 降低时已有高分辨率版本可以直接复用；DPI 提高时旧低分辨率版本只作过渡，并在新版本 Ready 后替换。

缩略图 Cache Key 至少包含：

```text
Source Identity
+ ImageGalleryImagePurpose.Thumbnail
+ TargetPixelSizeBucket
+ Processing Policy Version
```

首版处理策略固定为当前确认的槽位填充与居中裁剪语义，因此处理策略可以使用内部版本标识，不增加公共复杂对象。专用缩略图源可以返回自身最接近的尺寸，最终缓存接纳与预算统计仍以 Lease 的实际 `EstimatedMemorySizeBytes` 为准。

尺寸分档是缓存内部的防御性优化，不是业务层必须频繁配置的功能。在固定单显示器、整数 DPI、槽位尺寸永不变化的应用中，它可能很少被观察到；在 Windows 125%/150% DPI、多显示器跨屏和运行时布局取整中则会自然发生。首版保留并测试该机制，但不向开发者暴露调节入口。

### 缩略图槽位、裁剪与虚拟化

缩略图走廊采用单轴线性列表，不换行。所有缩略图项沿走廊主轴使用统一槽位长度，目标公开：

```csharp
public double ThumbnailItemExtent { get; set; } = 96.0;
public double ThumbnailItemSpacing { get; set; } = 8.0;
```

`ThumbnailItemExtent` 使用 Avalonia DIP，不表示图片源像素尺寸。Top/Bottom 时它控制容器宽度，项目高度使用走廊分配的可用内容高度；Left/Right 时它控制容器高度，项目宽度使用走廊分配的可用内容宽度。默认图片以 `UniformToFill` 填充槽位，容器启用裁剪，因宽高比不同而超出的图片部分采用居中裁剪。首版不增加裁剪焦点、逐项目主轴长度 Binding 或变长槽位。

`ThumbnailItemSpacing` 表示相邻缩略图容器之间的主轴空白：Top/Bottom 时是水平间距，Left/Right 时是垂直间距。它不在首项之前或末项之后额外产生空白；走廊外壳与内部内容的距离由 `FilmstripAppearance.Padding` 管理，图片与单个 Thumbnail Item 边框的距离由 `ThumbnailItemAppearance.Padding` 管理，三者不能混用。

```text
SlotStride = ThumbnailItemExtent + ThumbnailItemSpacing

N == 0：MainAxisItemsExtent = 0
N > 0：MainAxisItemsExtent = N * ThumbnailItemExtent
                             + (N - 1) * ThumbnailItemSpacing
```

`ThumbnailItemExtent` 必须有限且大于零，`ThumbnailItemSpacing` 必须有限且不小于零；`NaN`、无穷、非正 Extent 或负 Spacing 属于配置错误。首版不允许用 `NaN` 表示 Auto，因为变长项目会破坏远距离索引定位和稳定虚拟化。运行中修改合法长度或间距不会改变当前选择或主图，但会重新 Measure 走廊、计算槽位步长、滚动总长、虚拟窗口和目标解码尺寸，并以最小必要滚动保持当前选中项完整可见。

缩略图项可以在走廊 Viewport 边缘部分可见；自由滚动停止后不强制吸附到完整槽位。新选择提交后，新的选中项必须通过最小必要滚动完整进入可视范围。

默认容器结构采用内部单主轴 `ScrollViewer`、`ItemsPresenter` 和 `VirtualizingStackPanel` 或经当前 Avalonia 版本验证的等价虚拟化容器机制。ScrollViewer 与面板 Orientation 必须由 Placement 同步派生，交叉轴不得形成第二套可滚动范围：

```text
Thumbnail Filmstrip
├── Previous Button
├── Internal Main-Axis ScrollViewer
│   └── ItemsPresenter
│       └── Placement-Derived VirtualizingStackPanel
│           └── ImageGalleryThumbnailItem × realized range
└── Next Button
```

主图 Viewport 禁止内部 `ScrollViewer` 的结论不适用于这里：缩略图走廊面对的是线性同级 Items 和列表滚动，正是 `ScrollViewer` 与虚拟化面板的适用场景。普通 `StackPanel`、`WrapPanel` 或一次创建全部缩略图项的模板不能作为默认实现。

虚拟化保存全部轻量 Descriptor，但只实现当前可视区域和 Overscan 范围内的 UI 容器。首版 Overscan 采用主轴前后各约 `0.5` 个缩略图走廊 Viewport 的内部默认策略，不公开调节属性；可实现容器数量随 `ThumbnailItemExtent + ThumbnailItemSpacing` 的槽位步长和走廊主轴长度变化，而预先准备的滚动距离保持相对稳定。到达集合边缘时无需把无法使用的一侧 Overscan 额度转移到另一侧。

远距离选择的目标容器可能尚未实现。协调器先根据统一 `SlotStride` 估算最小必要滚动位置，使虚拟面板实现目标附近容器；布局完成后再用实际 Bounds 进行一次精确修正。不能忽略 Spacing、不能先要求获取尚不存在的容器，也不能每次都强制把选中项居中。

缩略图虚拟化的容器数量红线、海量数据矩阵、异步故障注入、Lease 审计、Nightly 长稳、真实桌面指标和反作弊证据要求，统一见 [缩略图虚拟化测试与性能验收设计](thumbnail-virtualization-verification.md)。实现不能只继承或声明使用虚拟化面板就视为达标，必须以该专项文档规定的可重复证据证明容器、加载和解码资源确实受 Viewport 与预算约束。

### 图片加载调度与缩略图容器回收

首版不公开 `MaximumConcurrentThumbnailLoads`、总并发数或职责等价的配置属性。每个已附加到视觉树的 `ImageGallery` 使用自己的内部有界调度器；首版不跨多个 Gallery 建立应用级全局队列。基础并发固定为6个槽位：

```text
6个基础槽位
├── 1个 Current Main 专用槽位
├── 4个 Thumbnail 前台槽位
└── 1个 Speculative 后台槽位
```

槽位是“一项底层 `LoadAsync` 从开始到终止的完整加载管线许可证”，不是CPU核心、固定线程、线程池线程或协程对象。任务等待网络、异步Stream或其他I/O时通常不消耗CPU，但仍占有槽位，因为它仍持有连接、文件、缓冲区、取消状态和潜在解码资源：

```text
取得槽位
   -> 等待网络 / 文件I/O
   -> 读取与格式验证
   -> 后台解码
   -> 创建并返回Lease，或失败/取消
   -> 底层操作进入终态并释放槽位
```

因此“6个槽位”只表示最多6项普通底层加载可以同时处于执行生命周期中，不表示同时有6项CPU解码，也不创建6个永久工作线程。排队但尚未调用底层 `LoadAsync` 的需求不占槽位。同一个Cache Key的多个消费者必须合并为一个共享底层操作，只占一个槽位。

Current Main专用槽位只执行当前主图首次加载，以及当前已经Ready后的清晰度升档；它不能借给普通缩略图、Overscan或相邻预取。Thumbnail槽位优先服务选中和可见缩略图，没有前台缩略图等待时可以借给机会性需求。Speculative槽位服务Overscan与相邻主图预取；前台缩略图积压时可以借用该槽位。借用只影响尚未开始工作的调度选择，不赋予实现强制终止任意正在运行代码的能力。

当前主图请求具有独立的最高优先级启动通道，不受缩略图并发上限阻塞。缩略图需求按以下优先级调度：

```text
P0  Current Main首次加载
P1  Selected Thumbnail
P2  Visible Thumbnails
P3  Current Main清晰度升档
P4  Overscan Thumbnails
P5  Adjacent Main Prefetch
```

该顺序描述需求价值；专用槽位允许不同类别并行，并不要求P0未完成时其余五个槽位全部空闲。当前主图已经Ready时，P3只提高已有画质，而P1/P2仍负责消除用户可见的空白缩略图，因此可见缩略图价值高于质量升档。同一级缩略图优先处理距离当前选择和可视中心更近的项目，再按首次进入该优先级的序号保持稳定FIFO；重新评估不能让旧合法需求反复回到队尾。

必须防止P0、P1和稳定Viewport中的P2无限饥饿。用户持续快速滚动时，已经离开可视范围的旧需求应解除消费者并退出，新的稳定可视范围重新排序。P4和P5是机会性工作，在用户持续操作、前台需求或预算压力下允许长期不执行，这不属于饥饿缺陷。首版不根据滚动速度和方向实现预测性调度。请求从Overscan进入Visible或Selected时提升同一共享需求的优先级，不创建重复任务；已开始的任务不因随后降级而重复启动另一份工作。

快速滚动使旧 Visible/Overscan 范围失效时，调度器优先移除未开始的无消费者旧请求，并对已开始且无消费者的旧请求发出取消，让并发槽位尽快用于新可视范围。`CancellationToken` 是节省资源的合作机制，不是正确性屏障；不响应取消的图片源仍可能晚到，结果必须经过身份校验。

Current Main额外允许一个不公开的、有界替换逃生槽位。正常并发硬上限为6；旧当前主图已经请求取消但尚未真正结束，而用户产生新的当前选择时，最新主图允许使用一次逃生槽立即启动，使短时硬上限最多为7：

```text
A正在Current Main槽位
选择B
├── A请求取消但尚未结束
└── B使用唯一逃生槽立即开始
```

逃生槽不能递归扩张。若A和B都忽略取消，用户又选择C，则C取代任何尚未开始的旧候选，作为唯一P0等待项；在A或B真正终止并释放主图槽位前，不允许创建第8个及更多“僵尸”加载。旧结果始终通过请求版本与身份检查拒绝。该策略优先覆盖常见的一次快速替换，同时在不合作Source下仍保持绝对有界。

缩略图项只向根控件内部 Image Load Coordinator 注册或解除需求，不自行创建孤立后台任务。调度器维护的是按Cache Key合并的共享需求，而不是盲目的普通Task列表。每个共享操作至少保存Key、当前最高优先级、消费者集合、首次进入当前优先级的稳定序号、Queued/Running/Completed状态、取消源以及请求代际。某个消费者离开但其他消费者仍存在时不得取消共享底层操作；最后一个消费者离开后，尚未开始的需求直接移出队列，已经开始的需求请求取消并继续占用槽位，直到其底层操作真正终止。取消请求本身不能提前释放槽位，也不能据此假设后台代码已经停止。

每个 `ImageGalleryThumbnailItem` 在绑定新 Descriptor 时递增内部 `BindingGeneration`。回收和复用顺序至少为：

```text
1. 增加 BindingGeneration，使旧异步结果失去提交资格
2. 解除旧 Descriptor 与共享需求消费者注册
3. 释放旧 Lease 引用
4. 清理 Loading / Ready / Error、标题和无障碍状态
5. 绑定新 Descriptor
6. 从 SelectionModel 重新派生 :selected
7. 查询缓存并注册新的共享加载需求
```

缩略图结果提交到容器之前必须同时满足：

```text
Result.BindingGeneration == Container.BindingGeneration
Result.ItemIdentity       == Container.ItemIdentity
Result.SourceIdentity     == Container.SourceIdentity
Container still owns the consumer registration
```

任一条件不满足时，不得修改容器并释放本次消费者引用。缓存是否保留共享结果由 Coordinator 与内存预算决定，容器不能自行销毁共享 `IImage`。

当前选中缩略图项的容器也允许在用户主动把走廊滚远后回收；真正选择保存在 `SelectionModel`，不保存在容器。该项目重新进入虚拟化范围时，新容器从选择模型恢复 `:selected`。禁止为了当前选择永久固定缩略图容器或 Lease。

### 缩略图失败呈现与再次加载准入

缩略图项内部区分以下加载状态，并向包内固定 Theme 提供对应伪类；状态枚举和容器伪类首版均不作为根控件公共样式 API：

```text
Unloaded
Loading   -> :loading
Ready     -> :ready
Error     -> :error
```

`:error` 可以与 `:selected` 组合。缩略图失败不使业务项失效：错误项保持统一槽位、选中视觉、点击和 Previous/Next 导航能力，不从集合移除、不设为 Disabled，也不直接改变主图的 `ImageState`。专用缩略图源失败后，用户仍可选择该项目并由主图加载器独立尝试主图源。

请求因容器回收、消费者归零、Source Identity 变化、集合变化、Gallery 卸载或应用关闭而取消时，属于正常生命周期，容器回到 `Unloaded`，不得进入 `Error` 或写入失败记录。只有当前有效的非取消异常才形成真实失败。

ImageGallery 不在一次缩略图加载内部执行固定次数的即时自动重试，也不公开 Retry 方法、Retry 命令或定时重试开关。具体 HTTP、OSS、文件或业务图片源最了解瞬时错误语义；如其自身需要在单次 `LoadAsync` 内实施受控退避，那属于图片源实现，不属于 Gallery 的重试机制。Gallery 不能猜测 `404`、认证失败、损坏格式或临时网络错误的语义。

为防止容器离开并重新进入虚拟范围时反复请求同一失败资源，首版在运行期内存中为失败 Cache Key 保存短期抑制记录：

```text
Failure Record
├── Cache Key
├── FailedAt
└── NewDemandAllowedAt = FailedAt + 5 seconds
```

默认抑制时间为 `5 seconds`，首版不公开配置，也不写入磁盘。抑制期内相同 Key 的新消费者直接呈现稳定 Error，不创建底层请求。抑制期结束本身不启动计时器加载；只有随后出现新的有效需求时才允许创建一项全新的普通加载请求：

- 项目重新进入 Visible。
- 项目从 Overscan 进入 Visible。
- 项目成为 Selected Thumbnail。
- Source Identity 改变并形成新的 Cache Key。

单纯持续停留在 Error、仅处于 Overscan 或无人消费时，不周期加载。Source Identity 改变立即绕开旧失败记录。Gallery 不实现全局循环重试或定时器驱动的无限退避链。用户切走后再次回到该项目，只在已经形成新的 Selected 或 Visible 需求并通过上述准入规则时才重新加载；这属于新的普通需求，不是公开 Retry 操作。

共享缩略图请求失败时只建立一个底层 Failure Record。每个仍有效的消费者通过 BindingGeneration、Item Identity、Source Identity 和注册资格检查后独立进入 Error；已经回收的容器忽略晚到失败。失败记录不能永久保留完整异常和响应对象而造成内存滞留。首版不公开加载失败事件或错误对象；异常只进入内部诊断通道，不直接显示给最终用户。

错误视觉至少保留与正常缩略图相同的槽位尺寸，不挤压相邻项、不显示完整异常堆栈、不使用持续闪烁动画，并且不能覆盖 `:selected` 指示器。包内固定 Theme 在槽位中呈现内置矢量失败占位图；它不是另一个需要异步加载的图片资源，因此不存在“兜底图再次加载失败”的递归故障。首版不公开 `ErrorImageSource` 或状态模板，开发者只能通过 `ThumbnailItemAppearance.ErrorForeground` 等已确定属性修饰固定错误视觉。

### Add Image Action

缩略图走廊在稳定顺序的起始端默认提供 Add Image 按钮。Top/Bottom 的首版中英 LTR 布局位于最左侧，Left/Right 布局位于最上方；随后依次为 Previous、缩略图 Items 和 Next。这里的“起始端”是走廊主轴概念，不能在垂直走廊中继续称为最左侧。

走廊 LayoutPanel 必须沿交叉轴居中 Add、Previous 和 Next 的实际 DesiredSize；Items Viewport 继续占满交叉轴，所有已实现 Thumbnail Item 同样以该交叉轴中心为视觉中心。Top/Bottom 时表现为全部内容的水平中线一致，Left/Right 时表现为垂直中线一致，不能把固定尺寸按钮顶对齐到走廊边缘。

开发者可以独立隐藏追加按钮，而不隐藏整个缩略图走廊：

```csharp
public bool IsAddImageButtonVisible { get; set; } = true;
public ICommand? AddImageCommand { get; set; }
public object? AddImageCommandParameter { get; set; }
```

命令所有权必须与相邻导航明确区分：`PreviousCommand` / `NextCommand` 是 ImageGallery 自己创建并公开的只读能力；`AddImageCommand` 则由开发者注入并拥有，因为添加来源、文件选择、上传和集合修改都属于业务职责。二者不能统一成一组可替换的根命令，也不能让默认导航按钮绕过控件状态机执行开发者命令。

`IsAddImageButtonVisible=false` 只移除 Add Image 按钮及其命中区域，不改变 Selection、走廊 Placement、缩略图虚拟窗口或 Previous/Next。空集合下整个走廊已经结构隐藏，因此该按钮也不会单独浮在 Viewport 上；需要支持“从零添加”的应用应在自定义 Toolbar 或 Gallery 外部提供入口。

ImageGallery 只把按钮作为命令源，不拥有业务集合修改权：

- `AddImageCommand` 为 null，或其 `CanExecute(AddImageCommandParameter)` 为 false 时，按钮保持显示但禁用。
- 点击只执行开发者提供的命令和显式参数，不默认传递 SelectedItem。
- 控件不自行打开系统文件选择器、不创建业务 ViewModel、不决定插入索引，也不直接修改 `ItemsSource`。
- 命令执行后如果开发者更新集合，新增项是否被选中继续遵守普通 Add/Insert 选择保持规则；按钮不能绕过 SelectionModel 强制选中新项。

追加按钮使用包内固定 Theme，开发者通过 `AddImageButtonAppearance` 修饰其背景、边框、尺寸、图标画刷和交互状态；默认图标与自动化名称必须表达“添加图片”，不能把它作为无语义的任意扩展槽，也不能替换按钮模板。高对比度和焦点播报继续服从后续无障碍合同。

## 图片集合合同与加载抽象

### ItemsSource 与内部描述符

`ItemsSource` 保留 Avalonia 集合控件的 `IEnumerable?` 公共形态，但其中每个非空项目必须实现唯一的数据合同 `IImageGalleryItem`。首版删除逐字段运行时映射，不公开 `MainImageSourceBinding`、`ThumbnailSourceBinding`、`TitleBinding` 或 `KeyBinding`：

```csharp
public IEnumerable? ItemsSource { get; set; }

public interface IImageGalleryItem
{
    object Key { get; }
    string? Title { get; }
    IImageGallerySource MainImageSource { get; }
    IImageGallerySource? ThumbnailImageSource { get; }
}
```

ImageGallery枚举集合后执行普通类型检查和接口调用，不使用反射、字符串属性路径、Converter或字段名猜测：

```text
ItemsSource Item
      │
      ├── is IImageGalleryItem ? ──否──> 明确配置异常
      │
      └── 是：静态接口调用
          ├── Key ─────────────────> Selection Identity
          ├── Title ───────────────> Optional Title
          ├── MainImageSource ─────> Main Source
          └── ThumbnailImageSource > Optional Thumbnail Source
                                      │
                                      v
                           Lightweight Descriptor
```

`Key` 和 `MainImageSource` 必须非null；Key在当前集合内必须唯一并在同一逻辑项目生命周期中稳定。Key比较使用 `EqualityComparer<object>.Default`，推荐采用不可变的 `string`、`Guid`、整数或具有稳定值相等语义的业务Key；运行中改变参与Equals/GetHashCode的字段属于违反身份合同。`ThumbnailImageSource` 可以为null，此时复用主图源发起 `Thumbnail` 用途的目标尺寸请求。项目为null、未实现接口、Key为空或重复、MainImageSource为空均是开发者数据合同错误，必须抛出包含索引和实际类型/Key的明确异常，不能跳过项目或映射成图片Error。

内部Descriptor只保存 `IImageGalleryItem` 引用、Key、Title和两个Source引用，不持有Bitmap、模板控件或长期打开的Stream。首版把接口值看作集合事件发生时读取的项目快照；业务内容变化应通过ObservableCollection的Replace或整体Reset提交，从而进入已经确定的身份与Source变化规则。不能静默使用反射观察任意业务属性。

#### 有限集合、变化通知与内存边界

首版 `ItemsSource` 必须是一次枚举能够结束的有限集合。它可以在应用运行期间持续增量增加项目，控件不设置业务数量硬上限，但这不构成真正的无限流式数据源、分页 Provider 或内存有界的数据虚拟化承诺。网络读取、阻塞等待或“永不结束”的 `yield return` 不能隐藏在 `ItemsSource` 枚举过程中；需要分页加载的应用应先在后台取得一页业务数据，再在 UI 线程把该页项目追加到通知集合。

```text
后台取得下一页数据
        │
        v
UI线程向 ObservableCollection 追加有限批次
        │
        v
ImageGallery 增量建立 Descriptor
```

集合观察规则固定为：

- 普通数组、`List<T>` 或其他不实现 `INotifyCollectionChanged` 的有限 `IEnumerable`，在赋给 `ItemsSource` 时完整枚举并建立一次快照；开发者随后直接修改原集合，ImageGallery 不轮询、不重新枚举，也不承诺自动感知。
- 实现 `INotifyCollectionChanged` 的有限集合支持运行期 Add、Remove、Move、Replace 和 Reset；需要动态增删的官方示例优先使用 `ObservableCollection<IImageGalleryItem>`。
- Replace 整个 `ItemsSource` 或收到 Reset 时重新建立完整快照；精确通知按照本文集合变化章节进行增量协调。
- ImageGallery 不直接修改开发者集合。Add Image 命令仍只通知业务层，由业务层决定何时以及如何更新集合。

所有 `ItemsSource` 赋值、初次枚举和 `INotifyCollectionChanged` 通知必须发生在 Avalonia UI 线程，因为它们会同步更新 Descriptor、SelectionModel、虚拟范围和布局状态。图片读取与解码可以在后台执行，但后台线程不得直接修改绑定集合。收到非 UI 线程集合通知时，控件必须抛出包含集合操作和 UI 线程要求的明确 `InvalidOperationException`，并且不得部分提交内部 Descriptor 或 Selection 状态；控件不替开发者静默调度通知。正确用法是先在后台准备业务项，再通过 `Dispatcher.UIThread` 提交集合修改。

```csharp
var item = await LoadImageItemAsync();

await Dispatcher.UIThread.InvokeAsync(() =>
{
    GalleryItems.Add(item);
});
```

首版保存全部业务项引用和全部轻量 Descriptor，因此数据侧空间复杂度明确为 `O(N)`：

```text
ItemsSource.Count = N
Descriptor.Count  = N

始终保留                    受虚拟化与预算约束
├── N个业务项目引用          ├── 可视/Overscan缩略图容器
└── N个轻量Descriptor        ├── 缩略图Lease
                             └── 主图与解码缓存
```

用户浏览到第 N 项后，只要第 1～20 项仍留在集合中，就仍可返回并重新查看；其可视容器可能已经回收、解码图片可能已经被缓存淘汰，但 Descriptor 仍能重新定位图片源并按需加载。开发者删除项目时对应 Descriptor 同步删除。若业务要求永久追加、永不删除且进程内存保持稳定，必须在未来引入分页数据 Provider、Descriptor 窗口化和历史项目重新获取协议，不能把当前 `ObservableCollection` 方案称为“无限集合”。

为避免业务模型必须直接依赖控件接口，正式包提供便利包装类型：

```csharp
public sealed class ImageGalleryItem : IImageGalleryItem
{
    public required object Key { get; init; }
    public string? Title { get; init; }
    public required IImageGallerySource MainImageSource { get; init; }
    public IImageGallerySource? ThumbnailImageSource { get; init; }
    public object? Data { get; init; }
}
```

开发者可以让ViewModel直接实现接口，也可以通过普通C#投影包装原始业务对象：

```csharp
GalleryItems = photos.Select(photo => new ImageGalleryItem
{
    Key = photo.Id,
    Title = photo.Name,
    MainImageSource = ImageGallerySources.FromFile(photo.FilePath),
    Data = photo
}).ToList();
```

`Data`只帮助开发者保留原业务对象，ImageGallery从不读取或反射它。使用包装类型时，`SelectedItem`返回包装项目；开发者可以从其Data取回业务对象。

集合进入控件的严格AOT用法只有代码直接赋值或Avalonia `CompiledBinding`：

```xml
<UserControl
    x:DataType="local:GalleryViewModel">
    <atom.labs:ImageGallery
        ItemsSource="{CompiledBinding GalleryItems}" />
</UserControl>
```

```csharp
gallery.ItemsSource = viewModel.GalleryItems;
```

`CompiledBinding`负责让编译器明确看见ViewModel的 `GalleryItems` 访问；`IImageGalleryItem`负责让控件明确读取每个项目。两段链路都不需要运行时反向推测。普通 `{Binding GalleryItems}` 在某些配置下可能仍可运行，但不属于ImageGallery严格NativeAOT与trimming兼容承诺，正式示例和发布测试不得使用它。控件在Binding解析后只能看到最终 `IEnumerable`，不能可靠判断它最初来自普通Binding还是CompiledBinding，因此不能伪造一个并不可靠的运行时拒绝机制。

### 统一图片源

文件、Avalonia 资源 URI、HTTP、OSS、数据库 Blob 和 Stream 的获取方式差异，由显式图片源适配器屏蔽。控件内部只依赖统一协议：

```csharp
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

public ImageGalleryLoadLimits LoadLimits { get; set; }
    = ImageGalleryLoadLimits.Default;
```

`IImageGallerySource` 是图片的可重复加载方案，不是图片数据容器。`Identity` 用于同一运行期内的请求合并、缓存寻址和过期结果识别；它必须非 null，并且在 Source 被使用期间保持稳定的 `Equals` 与 `GetHashCode` 结果。字符串、不可变值对象或不可变 record 都可以作为身份；可变集合、会随时间改变哈希值的对象或临时随机值不能作为稳定身份。底层图片内容发生变化时，开发者必须同步更换 Identity；内容已变而 Identity 不变时，ImageGallery 合法复用旧缓存不属于控件错误。Identity 只服务当前运行期，不隐式承担跨应用启动的持久化协议。

`IImageGallerySource` 不公开 `DisplayName`。面向用户的标题由 `IImageGalleryItem.Title` 唯一负责，避免 Source 文件名与业务标题形成两个互相竞争的显示来源；内部诊断也不能依赖可变显示名称。

`Purpose` 只区分主图和缩略图加载意图。`TargetPixelSize` 是可空的解码建议边界，不是图片在布局中的强制显示尺寸，也不要求 Source 裁剪、拉伸或精确返回该尺寸。Source 能力允许时应返回适合目标的解码结果；受编码格式、服务端变体或平台解码器限制时可以返回相邻档位或自然尺寸。`null` 表示当前没有可靠目标尺寸，Source 可以选择自然解码。Lease 必须通过 `SourcePixelSize` 与 `DecodedPixelSize` 如实报告原始尺寸和实际解码尺寸，缓存不能假设请求尺寸等于返回尺寸。

`LoadAsync` 可以由 ImageGallery 在后台线程调用，同一 Source 实例也可能因主图、缩略图和不同目标尺寸被并发调用；实现必须线程安全，不能要求 ImageGallery 串行化全部业务 Source，也不能同步阻塞 UI 线程。`CancellationToken` 是节省资源的合作取消机制，不是结果正确性的唯一屏障：Source 忽略取消并迟到返回时，协调器仍按请求版本、Selection Identity 和 Source Identity 拒绝过期结果，并立即释放返回的 Lease。

文件、HTTP、OSS、数据库或业务 Stream 只允许存在于单次 `LoadAsync` 的读取与解码阶段。正常完成、失败和取消路径都必须在方法返回或抛出前关闭本次 Stream；Lease 只持有已经可绘制的图片资源，不得把开放 Stream、HTTP Response 或厂商 SDK 请求对象带入 UI 生命周期。

`IImageGalleryItem`直接返回 `IImageGallerySource`。控件不接受 `string`、`Uri`、`Stream`、`byte[]`、`IImage` 或任意 `object` 后在运行时猜测来源类型，也不通过Converter补救错误接口实现；业务模型或包装投影应在进入ItemsSource前调用下面的工厂完成适配。

#### 单张图片加载安全限制

缓存预算只约束成功解码后可以保留多少非当前图片，不能防止单个文件在读取或解码阶段造成巨大资源峰值。ImageGallery 因此通过单一原子 `LoadLimits` 值定义每次图片请求的硬安全边界，并把同一快照随 `ImageGalleryImageRequest` 传给内置或自定义 Source：

```text
ImageGalleryLoadLimits.Default
├── MaximumEncodedBytes       = 256 MiB
├── MaximumSourcePixelCount   = 100,000,000 pixels
├── MaximumDimension          = 32,768 pixels
└── MaximumDecodedBytes       = 512 MiB
```

- `MaximumEncodedBytes` 限制单次文件、资源、HTTP 响应或 Stream 实际读取的压缩/编码数据总量。
- `MaximumSourcePixelCount` 限制原始图片逻辑宽高乘积，防御压缩后很小但声明巨大画布的输入。
- `MaximumDimension` 限制原始图片任意单边尺寸。
- `MaximumDecodedBytes` 限制单次目标解码表示的保守估算内存，不等同于整个进程、GPU 或临时缓冲区硬上限。

四个值都必须为有限可表示的正整数；零和负数不表示无限制，而是开发者配置错误。限制作为整体一次提交和验证，不能拆成四个可独立更新、在属性赋值顺序之间形成半新半旧安全策略的根 StyledProperty。默认值是桌面图片查看场景的防御性起点，最终发布前必须用真实大图、各格式极端压缩样本和多平台 NativeAOT Benchmark 校准；专业超大影像应用可以显式提交另一组合法限制。

内置 Source 的检查顺序至少为：

```text
打开来源
   │
   ├── 已知 Content-Length / 文件长度超限 -> 读取前拒绝
   ├── 实际累计读取字节超限              -> 立即中止
   ├── 图片头尺寸、单边或像素总数超限     -> 完整解码前拒绝
   ├── 目标解码内存估算超限               -> 分配前拒绝
   └── 合法                              -> 解码并返回 Lease
```

`Content-Length`、文件长度和扩展名都不是最终信任依据；长度未知或声明错误的来源仍必须通过计数读取边界，图片尺寸必须从受验证的格式头/解码元数据取得。取消、失败和超限路径都必须关闭 Stream、响应和临时资源。控件不吞掉 `OutOfMemoryException` 等进程级不可恢复故障并伪装成普通图片错误；安全限制的目标是在危险分配前拒绝，而不是承诺能够从任意资源耗尽中恢复。

图片内容超过合法 `LoadLimits` 属于当前来源无法呈现，按现有主图或缩略图加载失败路径进入固定 Error 视觉，不作为根配置异常抛给布局系统。`LoadLimits` 本身非法才立即抛出包含字段和值的配置异常。

自定义 `IImageGallerySource` 必须在昂贵读取和分配前遵守请求携带的 Limits。Gallery 在 Lease 返回后仍验证 `SourcePixelSize`、`DecodedPixelSize` 与可得的 `EstimatedMemorySizeBytes`；超限结果不得提交，必须释放并进入当前请求的 Error 路径。但事后验证无法撤销自定义 Source 已产生的内存峰值，因此自定义 Source 是受信任扩展边界。ImageGallery 不是恶意媒体解码沙箱；需要处理真正敌对输入的应用应在受限独立进程中解码，再向 Gallery 交付经过验证的结果。

安全限制与缓存预算的职责固定为：

```text
LoadLimits                         单张图片能否被读取和解码
ThumbnailCacheMemoryBudgetBytes    可保留多少缩略图缓存
MainImageCacheMemoryBudgetBytes    可保留多少非当前主图缓存
```

三者不能互相替代。当前主图可以超过非当前缓存预算并继续显示，但它仍必须先满足单张图片 `LoadLimits`。

#### 内置解码格式、方向与色彩边界

`ImageGallerySources` 提供的内置文件、Avalonia 资源、HTTP 和 Stream 来源只承诺静态光栅图片。首版发布保证矩阵固定为：

```text
保证支持
├── PNG
├── JPEG / JPG
├── BMP
└── 静态 WebP

首版不保证
├── GIF
├── APNG
├── 动态 WebP
├── SVG
├── TIFF
├── ICO
├── HEIF / HEIC
└── AVIF
```

上述保证必须由 Windows、Linux、macOS 和 NativeAOT 真实宿主中的固定样本矩阵验证；底层平台解码器偶然能够打开列表之外的格式，不构成 ImageGallery 的公共兼容性承诺。格式判断以内容签名和真实解码结果为准，文件扩展名、URI 后缀和 HTTP `Content-Type` 只能作为提示，不能让伪装成图片的 HTML、JSON 或错误响应绕过解码验证。

首版明确不支持动画图片。GIF、APNG 和动态 WebP 不能静默退化为第一帧，否则开发者无法区分“完整支持动画”和“偶然显示静态帧”。多帧输入由内置 Source 拒绝并按照当前有效加载失败进入既有 Error 路径。未来动画支持必须单独设计帧序列 Lease、帧时序、循环、暂停、缩略图策略、Composition 渲染和帧内存预算，不能扩张当前静态 `IImage` Lease 的隐含语义。

内置 Source 必须在返回 Lease 前正确应用其支持格式中的 EXIF Orientation。`SourcePixelSize` 表示方向纠正后的逻辑原图尺寸，`DecodedPixelSize` 同样按最终可绘制方向报告；例如底层像素为横向但 EXIF 要求顺时针旋转 90° 的手机照片，交付 Gallery 时应已经成为正确的竖向图片。ImageGallery 的 `RotationAngle=0` 表示按图片正确自然方向显示，用户执行临时旋转时再在该结果之上叠加四分之一圈。控件不修改原文件、不重编码，也不写回 EXIF。

首版不建立专业色彩管理公共合同，不公开 ICC Profile、色彩空间或渲染意图属性。内置 Source 使用当前受支持 Avalonia 平台解码路径产生的颜色结果，但不承诺摄影、印刷级跨平台色彩一致性；CMYK JPEG、特殊 ICC 或平台无法可靠处理的颜色输入按普通不支持/解码失败处理。需要明确 Display-P3、CMYK 转换或专业色彩工作流的业务应在自定义 `IImageGallerySource` 中完成转换，再返回已可绘制的 `IImage`。

#### 便捷图片源工厂

正式包提供静态工厂，普通开发者无需为常见来源自行实现接口：

```csharp
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
```

前四个 API 是面向常规来源的强语义工厂：文件、Avalonia 资源、HTTP 与可重复打开的 Stream。它们必须成为基础文档、Gallery 示例和常规业务代码中的默认入口；调用点应当直接表达来源协议，不得依赖一个通用 URI 工厂在运行时猜测协议。

`Create` 是高级逃生口，仅面向需要自定义加载策略的来源。只有当加载逻辑必须读取 `Purpose` 或 `TargetPixelSize`、调用专用 SDK、生成动态签名地址、执行自定义解码，或者接管资源释放时，才应使用 `Create`。它不是普通来源的推荐写法，基础示例不得使用它，首版也不围绕它继续增加便捷重载。需要更完整状态或行为的来源仍可直接实现 `IImageGallerySource`。

工厂的返回实现可以保持 internal 或使用不承诺继承扩展的密封类型；公共稳定面是 `IImageGallerySource` 和工厂方法。`identity` 传入 `null` 时，仅允许能够安全产生规范化默认身份的来源自行生成；开发者显式身份优先。

#### 文件与 Avalonia 资源

`FromFile` 明确把字符串解释为文件系统路径。实现规范化为当前平台语义下的绝对路径，并在每次 `LoadAsync` 时重新以只读方式打开、解码并在方法返回前关闭 Stream；Source 构造时不得永久占用文件句柄。默认 Identity 基于规范化路径，但同一路径内容被原地覆盖不会自动改变身份；开发者需要提供包含业务版本的显式 Identity，才能使旧缓存自然失效。

`FromAvaloniaResource` 首版只接受绝对 `avares://` URI。文件路径统一使用 `FromFile`，HTTP/HTTPS 统一使用 `FromHttp`；相对 URI、其他 Scheme 和含糊字符串必须在开始加载前明确失败，不允许静默猜测或降级为其他协议。支持范围必须由自动化测试固定。

#### HTTP 所有权

`FromHttp` 要求开发者提供可复用的 `HttpClient`。开发者拥有客户端，Source 与 Gallery 均不得 Dispose 它；单次请求产生的响应和 Stream 由 Source 在加载/解码完成后释放。正式包不为每张图片创建 `HttpClient`，也不隐藏认证、Cookie、代理或厂商 SDK 策略。首版不提供暗中创建默认客户端的 HTTP 工厂。

#### 可重复 Stream 工厂

首版不提供接受单个现成 `Stream` 的重载。任意 Stream 可能已读到末尾、不可 Seek、已被释放或只能使用一次，而同一 Source 会因主图、缩略图、缓存淘汰、重新进入可见范围和目标尺寸变化而多次加载。`FromStream` 因此接收一个能够为每次请求产生新 Stream 的异步工厂：

```text
LoadAsync
    -> openStream(cancellationToken)
    -> 本次专用 Stream
    -> 读取 / 解码
    -> 在 LoadAsync 完成前 Dispose Stream
    -> 返回不持有 Stream 的 Lease
```

工厂返回的 Stream 所有权在成功返回时立即转交 Source；Source 对正常完成、失败和取消路径都负责释放。Stream Source 和委托 Source 无法从临时对象推导稳定身份，因此 `identity` 必填。

#### 委托与自定义来源

`Create` 为 OSS、数据库 Blob、带认证下载、CDN 变体和其他需要自定义加载策略的来源提供低样板扩展入口。委托按 `Purpose` 与 `TargetPixelSize` 返回带有明确同步 Release Callback 的 Lease，并负责自身 SDK、网络、解码与临时资源清理。公共 API 不再提供 Owned、Borrowed、Shared 或 Cached 所有权枚举；释放差异完全封装在每个 Lease 的回调中。需要复用较多状态、额外元数据或专门测试的开发者可以直接实现 `IImageGallerySource`。

首版删除 `FromImage` 便捷工厂，不为现成 `IImage` 建立公开 Borrowed 模式。确实需要把业务已有图片接入 Gallery 时，开发者必须通过 `Create` 或自定义 `IImageGallerySource` 明确提供完整 Lease 元数据和 Release Callback，并自行保证外部资源寿命；ImageGallery 不猜测它是否可以 Dispose，也不提供 `ownsImage` 布尔开关。

首版同样不提供 `FromBytes`、接收一次性 `Stream` 的重载、未显式注入 `HttpClient` 的 HTTP 重载，以及会自动猜测协议的通用 `FromUri`。`byte[]` 等内存数据可通过 `FromStream` 为每次请求返回新的 `MemoryStream`，或者在确有高级加载需求时使用 `Create`。

正式包不能因此增加云厂商 SDK、数据库驱动或认证框架依赖。所有便捷 Source 都必须遵守取消、Stream 不跨 Lease 存活、过期结果释放和 `EstimatedMemorySizeBytes` 合同。

#### 默认 Identity 规则

```text
File               规范化绝对路径；允许显式版本身份覆盖
Avalonia Resource  规范化绝对 avares:// URI；允许显式覆盖
HTTP               规范化绝对 URI；动态内容建议显式版本身份
Stream Factory     必须显式提供
Delegate Loader    必须显式提供
```

相同 URL 或路径并不保证远端/文件内容永远不变。Identity 是开发者与缓存之间的内容版本合同：底层字节变化但 Identity 不变时，Gallery 可以合法复用旧缓存；开发者应提供包含版本、ETag 或业务修订号的身份。首版不提供显式 Reload API，因此内容已经变化却继续沿用旧 Identity 属于开发者违反缓存身份合同。

### ImageGalleryImageLease

`ImageGalleryImageLease` 是 ImageGallery 自己定义的生命周期包装类型，不是 C# 或 Avalonia 内建类型。首版固定为密封引用类型并只实现同步释放：

```csharp
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
```

`Image` 是已经可以交给 Avalonia 绘制的非 null 图片资源。`SourcePixelSize` 表示原始图片像素尺寸，用于宽高比、ActualSize 与缩放语义；`DecodedPixelSize` 表示当前 `IImage` 实际解码表示的像素尺寸，用于清晰度升级判断和内存估算。二者必须分别如实报告，不能把较小解码版本伪装成原图尺寸，也不能假设请求的 `TargetPixelSize` 等于实际解码尺寸。两个尺寸的宽高都必须大于零。

例如原图为 `6000×4000`，当前为适配 Viewport 只解码到 `1500×1000` 时，SourcePixelSize 仍是前者，DecodedPixelSize 是后者。否则 ImageGallery 会错误解释 100% 显示，并失去后续请求更高分辨率版本的依据。

`EstimatedMemorySizeBytes` 表示当前解码资源的保守近似内存。图片源可以提供更准确估算；普通已解码位图可以使用经过整数溢出检查的 `DecodedWidth × DecodedHeight × BytesPerPixel` 估算，常见 32 位 RGBA/BGRA 约为每像素 `4 bytes`。该值用于缓存接纳和淘汰，不承诺等于整个进程的实际内存增量；GPU 资源、平台原生副本及解码临时缓冲可能令峰值更高。无法可靠估算时返回 `null`，提供值时必须大于零，不能用 `0` 伪装未知成本。

`Create` 的 `image` 与 `release` 均为必填。Release Callback 封装“释放私有 Bitmap”“减少共享资源引用计数”或开发者其他明确的资源结束动作；Lease 和 Gallery 都不再识别 Owned、Borrowed、Shared 或 Cached 标签。回调必须同步、快速、非阻塞且不得抛出异常，不得在其中重新进入 Gallery 或启动异步清理。

`Dispose` 必须通过线程安全的一次性门闩保证幂等；顺序或并发调用多次都只能执行一次 Release Callback。第一次释放后访问 `Image` 抛出 `ObjectDisposedException`，不可变像素尺寸和估算值仍可用于诊断。ImageGallery 对已接受、被替换、取消后迟到、失败回收和 Detach 清理的 Lease 都在 Avalonia UI 线程调用 Dispose；协调器仍需防御性捕获违反合同的回调异常、记录内部诊断并继续释放其他资源，不能让一个错误 Source 中断整批清理。

Lease 不实现 `IAsyncDisposable`。文件、HTTP、OSS 或数据库 Stream 必须在 `LoadAsync` 返回前关闭；如果释放 Lease 仍需要异步关闭网络或 Stream，说明 Source 错误地把读取阶段泄漏到了 UI 生命周期。Lease 也不实现终结器：终结器时机与线程不可预测，不能执行开发者回调或释放具有线程归属的 Avalonia 资源。ImageGallery 必须通过明确的请求替换、`try/finally` 和 Detach 生命周期保证每个取得的 Lease 最终释放。

ImageGallery 在当前请求被替换、取消、失败、控件卸载或图片不再需要时始终 Dispose Lease，不能根据具体图片源类型自行猜测所有权。过期异步请求即使晚到，也只能在 UI 线程释放其 Lease，不能覆盖新的当前选择。

## 选择模型与组件协调

### 唯一权威选择

Avalonia `SelectingItemsControl` 所维护的 `SelectionModel` 是当前选择的唯一权威来源。`SelectedIndex` 与 `SelectedItem` 是同一选择结果的两种公开表达和设置入口，不是两份可以独立变化的状态：

```text
                    SelectionModel
                         │
             ┌───────────┴───────────┐
             v                       v
       SelectedIndex            SelectedItem
```

以下入口都只能提交选择变更请求，不能绕过 `SelectionModel` 直接修改主图或子组件：

```text
点击 Thumbnail ───────┐
点击 Previous / Next ─┼──> Request Selection ──> SelectionModel
设置 SelectedIndex ───┤
设置 SelectedItem ────┤
ItemsSource 变化 ─────┘
```

ImageGallery 固定单选：只有空集合允许零项；只要集合非空，稳定状态下必须恰好选择一项。即使基类包含多选能力，也不能把多选配置作为 ImageGallery 的有效公共行为；实现应屏蔽或拒绝会产生多项选择的配置。

### SelectionChanged 与主图状态时序

ImageGallery 采用“逻辑状态原子切换、图片资源异步完成”的选择提交合同。用户、命令、程序代码或集合变化把选择从 A 切换到 B 时，必须在同一个 Avalonia UI 提交批次中完成以下同步规整：

```text
Selection Identity        A -> B
SelectedItem / Index      更新为 B
当前 Descriptor 与标题    更新为 B
Thumbnail 逻辑选中状态     更新为 B
旧主图请求                 失去提交资格并请求取消
旧主图                     立即停止呈现
Zoom / Rotation / Pan     恢复新选择初始状态
ImageState                成为 B 的 Loading 或 Ready
```

这里的“立即”只约束逻辑身份、公开状态和视觉归属，不要求文件读取、网络访问和图片解码同步完成。新主图缓存未命中时进入 `Loading`，随后由当前有效请求进入 `Ready` 或 `Error`；有效缓存命中时允许在同一选择提交路径直接进入 `Ready`，不人为插入一帧 `Loading`。

`SelectionChanged` 只表示业务选择已经提交，不表示主图加载成功。事件交付给开发者时，`SelectedItem`、`SelectedIndex`、标题和当前 Descriptor 必须已经属于新选择；此时 `ImageState` 必须已经属于新选择，但可能是 `Loading`，也可能因缓存命中而是 `Ready`。Loading 时允许上一张已提交帧作为不可交互底图继续绘制，但它不得被当作新选择的主图语义。开发者需要观察加载状态时，应绑定或观察只读 `ImageState`，不能把 `SelectionChanged` 当作 `ImageLoaded`。

首版不重复增加 `ImageLoaded`、`ImageFailed` 或携带异常的加载事件。快速发生 `A -> B -> C` 时，可以依次产生两次正常选择事件；B 的迟到成功或失败只能按请求版本与身份规则丢弃并释放结果，绝不能使 C 的 `ImageState` 进入 B 的 `Ready` 或 `Error`。

```text
SelectedItem = B
Title        = B
ImageState   = A 的 Ready
主图画面      = A
```

上述混合状态在任何公开事件回调和可见渲染阶段都属于严重实现错误。缩略图走廊为了显示 B 而进行的最小必要滚动和虚拟容器实现可以等待后续布局批次，但唯一逻辑选中项必须在选择提交时立即成为 B。

`SelectionChanged` 继续遵守 Avalonia `SelectingItemsControl` 的数据选择语义，不被重新定义为图片身份事件。Replace 当前项时，即使新旧 `IImageGalleryItem.Key` 相同，业务对象实例仍可能发生替换，不能为了避免图片重载而人为吞掉基类选择通知；内部是否重载主图继续独立按照 `Key + MainImageSource.Identity` 判断。换言之，选择事件报告数据对象变化，身份组合决定图片资源变化，两者不能混用。

### 选择事件同步重入（首版之后 TODO）

以下场景正式降级为首版之后的 TODO：开发者正在处理一次 `SelectionChanged` 时，又在该回调尚未返回前同步设置 `SelectedIndex` / `SelectedItem`，或者同步修改、删除、替换、筛选、重置 `ItemsSource`，从而在一次选择提交过程中嵌套发起新的选择提交。

```text
选择 A -> B
    └── SelectionChanged(A -> B)
            └── 回调内部又选择 C 或删除 B
                    └── 形成同步嵌套重入
```

首版不为这种同步重入建立待处理选择队列、最后请求获胜规则、嵌套事件顺序、集合通知排序或循环收敛上限，也不把这些行为纳入首版兼容性与发布合同。开发者首版不得依赖在 `SelectionChanged` 回调中同步改选或同步重构集合能够得到特定结果；确有需要时，应在回调返回后通过 Dispatcher 的后续调度批次提交新的业务操作。

这个 TODO 只排除“事件回调内部的同步嵌套修改”，不能扩大解释为首版不处理普通选择和集合变化。缩略图点击、Previous / Next、外部顺序设置选择、回调之外的 Add / Remove / Move / Replace / Reset，以及快速连续导航，仍必须遵守本文已经确定的选择与集合合同。请求版本、Selection Identity、Source Identity 和 Lease 资格校验也仍是首版硬要求；任何旧异步图片结果均不得覆盖最终当前选择。

### 非空集合自动选择第一项

首版采用固定的自动选择策略，不公开 `IsSelectionRequired`、`AutoSelectFirstItem` 或其他关闭开关：

```text
ItemsSource 为空
    └── SelectedIndex = -1，ImageState = Empty

ItemsSource 非空
    ├── 已存在有效显式选择 ──> 保留该选择
    └── 没有有效选择 ───────> 自动选择索引 0
```

ImageGallery 是主图查看器，不采用普通列表“集合非空但默认长期无选择”的行为。开发者提供图片集合后，无需额外设置 `SelectedIndex="0"` 才能看到第一张图片。

初始化或一次属性协调批次中，合法的 `SelectedItem` / `SelectedIndex` 优先于第一项回退，不能先发布第一项选择再在同一批次无意义地切换到显式选择。开发者在非空集合上请求清除选择时，控件不抛异常，但不能稳定保持 `SelectedIndex = -1`；完成选择规整后回到有效项。当前项被删除时采用哪一个幸存邻项，应按后续集合变化规则决定，而不是一律跳回第一项。

集合从空变为非空且没有显式选择时，第一项选择通过与缩略图点击相同的 Selection Coordinator 路径提交，并正常触发主图加载；实现不能绕过 `SelectionModel` 直接加载第一张图片。

### Remove 与 RemoveRange 的选择保持

开发者从可观察集合删除当前选中项时，ImageGallery 采用“优先后继，没有后继则选择前驱”的确定性规则：

```text
删除前：A  B [C] D  E
删除后：A  B [D] E       原位置仍有项目，选择后继 D

删除前：A  B [C]
删除后：A [B]             删除末项，选择前驱 B

删除前：[A]
删除后：空                SelectedIndex = -1，ImageState = Empty
```

选择邻项的索引算法为：

```text
if NewCount == 0:
    NewSelectedIndex = -1
else:
    NewSelectedIndex = min(RemovedStartIndex, NewCount - 1)
```

因此批量删除包含当前项时，也优先选择删除范围之后第一个占据删除起始索引的幸存项；删除范围延伸到集合末尾时选择最后一个前驱。控件不依据上一次 Previous/Next 导航方向改变这个规则，避免相同集合操作产生依赖交互历史的结果。

删除当前项会通过正常 Selection Coordinator 路径取消旧请求和拖拽、释放旧主图 Lease、重置为 `Fit` 并加载新选择。集合删除后为空时进入 `Empty`。

删除非当前项时，必须按业务项身份保持当前选择：

```text
删除前：A  B [C] D
删除 A：   B [C] D

SelectedIndex  2 -> 1
SelectedItem   C -> C
```

此时只是当前项的新索引发生变化，不能取消 C 的请求、释放 C 的 Lease、重置缩放、旋转和平移或重新进入 `Loading`。实现判断是否真正切换图片时必须比较选择身份，不能只比较 `SelectedIndex`。删除当前项后，即使未来把同一业务对象重新插回集合，也不自动抢回选择；它只作为普通集合项参与后续导航。

### Replace 的两级身份判断

可观察集合在原索引上以新业务对象替换旧对象时，首先判断替换位置是否为当前选择。替换非当前项只更新该项的 Descriptor、缩略图和预取状态，不取消或重置当前主图。

替换当前项时保持原 `SelectedIndex`，并把 `SelectedItem` 更新为新业务对象；非空集合不能因为 Replace 暂时跳到邻项或进入无选择。随后依次比较两个不同层次的身份：

```text
Replace(CurrentIndex, NewItem)
              │
              ├── Selection Identity 改变
              │       └── 新逻辑项目：重置并重新加载
              │
              └── Selection Identity 相同
                      │
                      ├── Source.Identity 改变
                      │       └── 同一项目的新图片内容：重置并重新加载
                      │
                      └── Source.Identity 相同
                              └── 仅元数据变化：保留当前主图状态
```

Selection Identity始终由 `IImageGalleryItem.Key` 表达，不再存在未提供Key时的引用身份回退。`IImageGallerySource.Identity` 表达可加载图片内容的身份。二者职责不能混淆：

```text
IImageGalleryItem.Key          是不是同一个业务项目
IImageGallerySource.Identity   是不是同一份图片内容
```

当 Selection Identity 改变时，即使新旧项目恰好使用相同 Source Identity，也视为一次真正选择变化：取消旧请求和拖拽并进入 Loading，通过缓存或图片源取得新选择的候选主图；已有 Ready Lease 仅作为不可交互保留帧。候选提交时原子重置 `Fit`、`RotationAngle=0` 与 `PanOffset`，安装新 Lease 后再释放旧 Lease。

当 Selection Identity 相同但 Source Identity 改变时，视为同一业务项目的图片内容更新，采用相同两阶段交接。旧内容只能作为 Loading 保留帧，不能因为 Key 相同而继续处于 Ready；新内容提交后才重置 Viewport 并释放旧 Lease。

只有 Selection Identity 和 Source Identity 均保持不变时，才把 Replace 视为元数据或 ViewModel 实例更新：

- 重建当前轻量 Descriptor，并更新 `SelectedItem`、标题及其他派生元数据。
- 更新对应缩略图所需的显示数据。
- 保留当前主图 Lease、`ZoomMode`、有效缩放比例、`RotationAngle` 和 `PanOffset`。
- 不进入 `Loading`，也不创建新的主图请求版本。

Source Identity 是图片内容缓存与新旧内容判定合同。如果底层 URI 未变但实际字节已经更新，开发者必须提供新的 Source Identity；控件不能通过相同 Identity 猜测远端内容是否已经变化，首版也不提供 Reload 作为旁路。

### Move 按身份保持选择

集合 `Move(OldIndex, NewIndex)` 只改变项目位置，不改变业务项目身份。无论移动的是当前项，还是其他项目跨过当前项，ImageGallery 都必须保持 `SelectedItem` 与 Selection Identity，只修正 `SelectedIndex`、缩略图排列和 Previous/Next 边界：

```text
移动前：A  B [C] D  E
Move C：A  B  D  E [C]

SelectedIndex      2 -> 4
SelectedItem       C -> C
SelectionIdentity  C -> C
```

设旧选中索引为 `S`，移动起点为 `O`，移动终点为 `N`，索引换算为：

```text
if S == O:
    NewSelectedIndex = N
else if O < S <= N:
    NewSelectedIndex = S - 1
else if N <= S < O:
    NewSelectedIndex = S + 1
else:
    NewSelectedIndex = S
```

集合事件参数用于计算候选索引，最终仍应核对选择身份。Move 不得取消当前主图请求、增加请求版本、释放 Lease、进入 `Loading`、重置 `ZoomMode`、`RotationAngle` 或改变 `PanOffset`。只需更新 `SelectedIndex`、导航边界和缩略图选中项的位置；被选缩略图移出缩略图走廊可视窗口时，Navigator 应跟随到足以显示它的位置。

正在加载的当前项发生 Move 时，请求仍然有效。异步提交条件只比较请求版本与 Selection Identity，不比较请求发起时的索引：

```text
有效：Request.Version == CurrentVersion
  && Request.SelectionIdentity == CurrentSelectionIdentity

无效：Request.SelectedIndex == CurrentSelectedIndex
```

选择快照中的索引可以更新为新位置或保留为诊断信息，但不能参与图片结果的有效性判定。虚拟化缩略图容器被移动、回收或重新生成后，`:selected` 必须跟随当前业务项身份，不能残留在旧视觉容器上。

### Reset 的选择恢复与身份回退

`NotifyCollectionChangedAction.Reset` 只表示集合整体发生变化，不提供可靠的 Add、Remove、Replace 或 Move 差异。整体替换 `ItemsSource` 也采用同一选择恢复语义。Reset 前，根协调器已有的当前选择快照至少保留旧索引、旧业务项引用、Selection Identity、Source Identity 及当前主图状态。

Reset 后首先处理空集合：取消当前请求与拖拽、释放 Lease，设置 `SelectedIndex = -1`、`SelectedItem = null` 并进入 `Empty`。新集合非空时，按以下身份策略尝试恢复原逻辑项目。

#### 严格Key身份恢复

`IImageGalleryItem.Key`是所有项目必须提供的严格身份合同。当前集合中的每个有效项必须产生非null、唯一且在该逻辑项目生命周期内稳定的Key。Reset后控件检查新集合并按旧Selection Identity查找：

```text
exactly one match   恢复该逻辑项目
zero matches        原项目不存在，进入位置回退
multiple matches    身份歧义，抛出配置异常
null key            身份合同无效，抛出配置异常
```

重复或空Key不能静默选第一个，也不能映射为 `ImageState.Error`；它们是开发者违反 `IImageGalleryItem` 数据合同，应抛出包含实际Key和冲突索引的明确配置异常。

恢复到相同 Selection Identity 后，重新构建当前 Descriptor，并继续应用 Replace 的第二级判断：Source Identity 相同则保留 Lease、缩放和平移；Source Identity 改变则重置并重新加载。

#### 位置回退

找不到原逻辑项目时，按照旧视觉位置选择新候选：

```text
if NewCount == 0:
    NewSelectedIndex = -1
else if OldSelectedIndex >= 0:
    NewSelectedIndex = min(OldSelectedIndex, NewCount - 1)
else:
    NewSelectedIndex = 0
```

位置回退得到的是新逻辑选择，必须取消旧请求与拖拽并进入 Loading，再加载候选项；已有 Ready Lease 只作为保留帧。候选成功提交时重置 `Fit`、`RotationAngle=0` 和 `PanOffset`，安装新 Lease 后释放旧 Lease；候选失败则清除保留帧并进入 Error。Reset 前无选择而新集合非空时，最后一条分支与已经确定的自动首选规则一致。

完整恢复顺序为：

```text
Reset / ItemsSource replacement
    │
    ├── NewCount == 0 ──> Empty
    │
    └── NewCount > 0
          │
          └── 验证全部IImageGalleryItem Key非空且唯一，按旧Key查找
                    │
                    ├── 找到同一逻辑项
                    │      ├── Source Identity相同：保留主图状态
                    │      └── Source Identity改变：重载
                    │
                    └── 未找到：旧索引Clamp，作为新选择加载
```

Reset 后验证 Key 并恢复选择最坏需要扫描整个新集合，时间复杂度为 `O(n)`；实现可以在同一次扫描中建立身份索引，但缩略图容器虚拟化无法消除 Reset 本身丢失差异信息的代价。频繁更新超大集合的开发者应发出精确的 Add、Remove、Replace 和 Move 事件，首版不承诺频繁超大 Reset 的常数时间性能。

### Add 与 Insert 按身份保持选择

集合从空变为非空时，按照已经确定的自动首选规则，通过 `SelectionModel` 与 Selection Coordinator 选择并加载第一项。Add 处理器不能绕过选择模型直接把新增图片交给 Viewport。

非空集合增加项目时，新项目不得抢走现有选择。当前选择按业务项身份保持；插入点位于当前项之前或恰好位于当前索引时，只修正当前项的新索引：

```text
插入前：A [B] C
Insert X at index 1
插入后：A  X [B] C

SelectedIndex      1 -> 2
SelectedItem       B -> B
SelectionIdentity  B -> B
```

设旧选中索引为 `S`，插入起始索引为 `I`，插入数量为 `K`，索引换算为：

```text
if OldCount == 0:
    NewSelectedIndex = 0
else if I <= S:
    NewSelectedIndex = S + K
else:
    NewSelectedIndex = S
```

候选索引计算后仍应核对 Selection Identity。只要当前身份不变，就不得取消主图请求、增加请求版本、释放 Lease、进入 `Loading`、重置缩放、旋转或改变 `PanOffset`；只更新 `SelectedIndex`、缩略图排列、导航边界和必要的预取计划。

所有新增项都必须通过 `IImageGalleryItem.Key` 产生非null且在集合中唯一的Key。新增项与现有项发生Key冲突属于开发者数据合同错误，应抛出包含冲突Key和索引的明确配置异常，不能猜测为Move、Replace或允许两个逻辑项共享同一身份。

### 根控件协调器

`SelectionModel` 只负责回答“当前选中了谁”，不直接绘制图片或逐一操作模板部件。`ImageGallery` 根控件承担内部 Selection Coordinator 职责；这是一个逻辑模块，不要求公开独立的 Coordinator 类型。

```text
SelectionModel 正式提交选择
              │
              v
      ImageGallery Coordinator
       ├──> Thumbnail selection visual
       ├──> Toolbar derived state
       ├──> Previous / Next availability
       ├──> Cancel old request and panning
       ├──> Reset Viewport to Fit
       └──> Start current main-image request
```

协调器使用三种明确的驱动渠道：

1. 缩略图选中视觉由 `SelectingItemsControl` 的容器选择机制驱动。选中容器进入稳定的 `:selected` 状态，容器不自行比较索引或启动主图请求。
2. Toolbar 和导航按钮使用根控件从当前描述符派生的状态，例如标题、`ImageState`、有效缩放比例以及 Previous/Next 是否可用；子组件不自行扫描 `ItemsSource`。
3. Viewport 不直接绑定业务项。协调器先按当前描述符异步取得 Lease，通过版本和身份校验后，才把有效 `IImage` 与像素尺寸交给内部 `ImageGalleryViewportPresenter`。

一次合法选择提交的同步阶段必须形成一致快照：

```text
Selection Snapshot
├── SelectedIndex
├── SelectedItem
├── Current Descriptor
└── Selection Identity
```

选择快照是内部不可变值，不属于公共 API。它使异步加载能够判断完成结果是否仍属于当前选择。协调器不能在请求结束时只重新读取一个可能已变化的 `SelectedIndex`，也不能让 Thumbnail、Toolbar、Navigator 和 Viewport 各自保存一份当前索引。

同步协调顺序为：

```text
1. 验证目标仍属于当前集合
2. 由 SelectionModel 原子提交选择
3. 同步 SelectedIndex 与 SelectedItem
4. 建立当前 Descriptor 和 Selection Snapshot
5. 更新缩略图选择状态及根控件派生状态
6. 取消旧主图请求、拖拽和 Pointer Capture；保留已有 Ready 帧及其几何状态
7. 启动当前选择的主图 Loading 状态机
8. 最新候选通过身份校验后原子安装新 Lease、重置 Viewport 并释放旧 Lease
```

“选择已改变”不表示主图已经加载成功。外部观察到新选择后，`ImageState` 可以是 `Loading`；主图是否可绘制由后续加载状态决定。不得为了等待网络或解码完成而延迟提交 `SelectedItem`。

以下绕行明确禁止：

```text
Thumbnail ──x──> 直接给 Viewport 设置图片
Previous  ──x──> 直接修改 Current Descriptor
Toolbar   ──x──> 维护自己的 CurrentIndex
Viewport  ──x──> 反向修改 SelectionModel
```

所有输入先进入选择模型，所有视觉和加载状态再由根协调器从已提交选择向外派生。

## 主图加载生命周期

### 主图异步加载状态机

主图区域明确区分空集合、当前项加载中、加载成功和加载失败：

```csharp
public enum ImageGalleryImageState
{
    Empty,
    Loading,
    Ready,
    Error
}

public ImageGalleryImageState ImageState { get; }
```

`ImageState` 是由当前选择和当前主图请求驱动的只读状态，不允许开发者直接写入。每次选择新图片，或同一项目在切走后再次形成一项合法的新加载需求时，内部创建一个单调递增的请求版本，并取消上一请求：

```text
Select A ──> RequestVersion 10
Select B ──> RequestVersion 11
Select C ──> RequestVersion 12
```

`CancellationToken` 用于尽快停止网络读取、解码和其他昂贵工作，但它不是正确性屏障。图片源可能延迟响应或无法响应取消，因此异步结果提交主图之前必须同时满足：

```text
CompletedRequest.Version      == CurrentRequest.Version
CompletedRequest.ItemIdentity == CurrentSelection.Identity
```

任一条件不满足即为过期结果。过期结果不得改变 `ImageState`、当前 Lease、标题、缩放或任何 Viewport 状态；如果它返回了 Lease，必须立即释放。

首版采用“新选择立即提交、旧帧保留到最新候选完成”的无空帧策略。旧帧只是 Loading 期间不可交互的视觉底图，不取得新选择身份：

```text
提交新选择
    ↓
取消旧请求、当前拖拽与 Pointer Capture
    ↓
ImageState = Loading
继续绘制上一张已提交帧并保持其几何状态；Zoom/Pan/Rotation 禁用
    ↓
加载当前选择
    ├── 当前请求成功 ──> 原子安装新 Lease、重置 Viewport、Ready ──> 释放旧 Lease
    ├── 当前请求失败 ──> 清除并释放旧 Lease ──> Error ──> 内置矢量失败占位图
    └── 过期请求完成 ──> 释放结果，不改变 UI
```

`SelectedItem`、标题和选中缩略图在第一步已经指向新项目，`ImageState=Loading` 明确说明主图尚未提交；此时旧帧不能被公开为新项目图片，也不能接受查看交互。当前请求失败后不回退显示上一张图片，而是释放保留 Lease 并进入 Error。首版不做淡入淡出；旧帧与候选 Lease 只允许在原子交接所需的短窗口共存，不能在同一帧混合绘制。

成功提交必须在一个不含 `await` 的 UI 线程回调中完成，并固定执行：再次验证 LifecycleGeneration、RequestVersion、Selection Identity、Source Identity 和目标档位；把候选 Lease 安装为当前 Lease；清除失败状态；重置新图的 Fit、RotationAngle 与 PanOffset；设置 Ready 并请求绘制；最后 Dispose 旧当前 Lease。这样旧 Lease 的 Release Callback 被执行时，新 Lease 已经成为唯一当前绘制资源。加载协调器把候选所有权转交根控件后必须清空自己的局部引用；取消、过期和未转交异常路径统一在 `finally` 释放候选。

高速 A -> B -> C 切换中，A 保留显示，B 的迟到结果因代次或身份不匹配被释放，只有 C 可以替换 A。连续点击不排队播放视觉结果，不允许出现 `A -> null -> B -> null -> C`，也不允许 B 在 C 之后闪回。

如果新选择的主图已经存在于有效解码缓存中，可以在同一选择提交路径内直接获得 Lease 并进入 `Ready`，不要求人为显示一帧 `Loading`。这属于缓存命中，不是继续显示上一选择。

状态规则还包括：

- `Loading`、`Ready` 和 `Error` 只描述当前选中项的主图请求，不描述邻项预取或缩略图请求。
- 旧请求晚到的成功或失败均不能改变当前状态。
- 当前请求失败进入 `Error`，释放请求可能产生的临时资源，并在主图查看区居中呈现内置矢量失败占位图；占位图不参与缩放、旋转和平移。
- 首版不公开失败异常对象、失败事件、Retry/Reload 方法或命令，也不自动重试。错误详情只进入内部诊断通道，不能把文件路径、服务地址或异常堆栈直接暴露给最终用户。
- 主图处于 `Error` 时仍保留当前选择，并继续允许 Previous/Next、缩略图点击和程序化选择。用户切走后再次回到该项目会形成一项新的普通加载需求；Source Identity 改变同样形成新的缓存身份。
- 集合为空时进入 `Empty`；非空集合会自动建立有效选择，因此不能把稳定的“非空但无选择”呈现成 `Empty`。`Empty` 与 `Error` 不得混用。
- Gallery根控件退出视觉树时遵守后文“视觉树附加、脱离与资源休眠”合同，取消全部请求并释放所有图片Lease与缓存引用；卸载后的结果不得重新提交视觉状态。

### 主图解码尺寸与清晰度升级

主图不得无条件以原始像素尺寸完整解码，也不得永久停留在首次适配 Viewport 的低分辨率表示。首版采用“首次按当前显示需求解码，清晰度不足时后台升档”的固定策略：

```text
首次选择 / 重新附加
        │
        ├── 根据 Viewport、RenderScaling、ZoomMode 和 Rotation
        │   计算当前显示所需物理像素尺寸
        │
        └── 通过内部尺寸分档形成 TargetPixelSize
                    │
                    ├── 有效缓存命中 -> Ready
                    └── 加载成功     -> Ready

Ready 状态下继续放大 / 跨屏 / Viewport 变大
        │
        └── 显示需求超过当前 DecodedPixelSize 的清晰度覆盖范围
                    │
                    └── 后台请求更高档位
                            ├── 成功 -> 原子替换当前 Lease
                            └── 失败 -> 保留当前 Lease 与 Ready
```

显示需求使用图片在当前变换后实际绘制区域所需的设备像素计算，不能把 Avalonia DIP 直接当成图片像素。实现至少考虑 Viewport Bounds、顶层 `RenderScaling`、当前有效缩放比例与四分之一圈旋转；传给 Source 的 `TargetPixelSize` 仍以未旋转源图片坐标表达，旋转只影响宽高需求换算。最终请求不得超过已知 `SourcePixelSize`，达到原图自然尺寸后停止继续升档。

初次选择默认 `Fit`，因此通常只请求足以覆盖当前 Viewport 设备像素的主图表示；`ActualSize` 或较高 `Custom` 缩放可以形成更大的请求需求。相邻主图预取只按当前 Viewport 的 `Fit` 需求准备，不复制当前图片的高倍缩放档位，避免一次放大导致 Previous 和 Next 同时解码为巨大图片。

主图尺寸分档必须单调增长并带有内部余量与滞回，避免滚轮每变化一个像素就产生新的 Cache Key 或重复解码。具体档位倍率、触发余量和稳定等待时间是内部性能参数，不公开 StyledProperty；初版实现可采用约 `150ms` 的连续输入稳定窗口，并必须由清晰度、请求次数、内存峰值和快速缩放 Benchmark 校准。Pinch 结束、显式执行 `ActualSize` 或其他明确终态命令时，可以立即重新评估，不必再机械等待完整窗口。

清晰度升级是当前 `Ready` 图片的机会性质量增强，不是一次新的业务选择，也不是主图首次加载：

- 升级期间继续呈现当前合法 Lease，`ImageState` 保持 `Ready`，不闪回 Loading 占位图。
- `SelectedItem`、标题、有效缩放、旋转和 `PanOffset` 保持不变；新 Lease 到达后在一次 UI 提交中替换绘制资源，不能改变用户正在查看的位置。
- 新结果只有在 LifecycleGeneration、RequestVersion、Selection Identity、Source Identity 和目标档位仍有效时才允许提交；迟到结果立即释放。
- Source 返回的实际 `DecodedPixelSize` 不足以优于当前 Lease 时不得倒退替换；协调器记录该档位能力并避免对同一需求形成无休止升级循环。
- 升级失败只结束本次质量请求并进入内部短期抑制，不把主图改成 `Error`，因为用户仍拥有可绘制图片。只有不存在任何合法当前 Lease 的首次/正式当前加载失败，才进入 `Error`。

缩小、恢复 Fit 或移动到较低 DPI 显示器时，不立即把当前主图降档或重新解码较小版本；当前 Lease 保留到新选择或新 Source 的候选成功提交、当前候选失败、集合变空、Detach 或正常清晰度替换。这样避免往返缩放和选择加载造成空帧。该规则不改变缓存预算：当前 Lease 退出 Current 后，非当前缓存仍按预算决定是否接纳或释放，高分辨率旧档位不能绕过预算长期常驻。

测试必须证明连续滚轮、Pinch、窗口缩放与跨 DPI 屏幕不会形成无界请求风暴；升级成功不改变视图几何状态，升级失败不破坏已有 Ready，旧档位与过期档位的 Lease 均能精确释放。

### 主图与缩略图状态视觉合同

根 ImageGallery 按当前 `ImageState` 公开互斥伪类 `:empty`、`:loading`、`:ready`、`:error`，默认根 Theme 在同一 Viewport Bounds 内切换互斥 Presenter。状态视觉不能建立第二套加载状态，也不能通过自身可见性反向修改 `ImageState`：

```text
State       Main Viewport                                  Thumbnail Item
Empty       中性“暂无图片”矢量视觉与短文案                   Filmstrip 结构隐藏
Loading     可选上一张已提交帧 + 内置加载视觉                :loading 占位
Ready       当前选择的已提交 Lease 对应图片                  :ready 图片
Error       内置矢量失败视觉与通用短文案                     :error 小型矢量失败视觉
```

#### Empty

Empty 采用方案A：默认主题在 Viewport 中央呈现克制的内置矢量图片符号和本地化“暂无图片”短文案，而不是只留下无法区分状态的空背景。图形使用 Theme Token，不依赖网络、文件或另一张位图。文案由包内本地化资源提供，不能把中文字符串硬编码到控件逻辑。

空集合继续遵守已经确定的结构规则：Filmstrip 完全退出布局；默认 Title、Zoom 和 Rotation 区域没有当前图片，不构成有效 Toolbar 内容。开发者提供了 `ToolbarContent` 时，Toolbar 仍可显示“打开图片”等业务入口。Viewport Previous/Next 若按配置存在，则沿用既有稳定布局并处于 Disabled，不得触发请求。

#### Loading

Loading 视觉居中呈现，并允许主题使用受控的不确定进度动画；首版不显示伪造的百分比，因为 `IImageGallerySource` 没有公开总字节进度合同。已有 Ready Lease 时，它在 Loading Presenter 下继续作为防空帧的保留底图；首次加载没有旧 Lease 时只显示加载视觉。保留底图不得取得当前选择语义或接受 Zoom、Pan、Rotation，但 Previous/Next、Filmstrip 和 Toolbar 自定义操作仍然可用。标题来自当前 Descriptor。

Loading Presenter 不得取得覆盖整个 Viewport 的命中优先权。系统请求减少动态效果时，默认 Theme 停止循环旋转或其他装饰性 Loading 动画，改用仍能明确表达 Loading 的静态视觉；动画被关闭不能导致状态区域消失或与 Empty 混淆。

#### Ready

Ready 隐藏 Empty、Loading 和 Error Presenter，只呈现当前有效 Lease，并启用符合条件的缩放、旋转和平移。状态切换不使用淡入淡出；候选安装、Viewport 重置、Ready 与重绘失效在同一个 UI 提交中完成，随后释放旧 Lease，不能出现空帧、Error 图与 Ready 图片同时命中或旧候选晚到闪回。

#### Error、损坏格式与不支持格式

当前主图的有效请求出现普通读取、网络、解码、损坏图片或不支持编码格式失败时统一进入 `Error`，显示内置矢量失败图和本地化通用短文案。Thumbnail 在自己的固定槽位显示小型失败符号，不显示长文本，不改变槽位尺寸。两处失败图均由已加载 Theme 矢量资源绘制，不产生新的异步图片请求。

Error 视觉不得展示异常消息、文件路径、服务器地址、HTTP 响应正文或堆栈。首版既不公开异常对象，也不提供 Retry/Reload；当前选择仍然有效，Previous/Next、Filmstrip 选择和程序化选择继续工作。Zoom、Rotation、Pan 对 Error 禁用。

取消、消费者归零和过期请求不是 Error，不允许短暂闪现失败图。配置错误也不能伪装成 Error：ItemsSource项目未实现 `IImageGalleryItem`、空或重复Key、空MainImageSource、非法数值和不受支持的Source配置仍按开发者合同抛出明确异常。

#### 超大图片与状态边界

“超大图片”不是独立 UI 状态。图片满足当前 `LoadLimits`，且图片源能够按请求成功解码并返回合法 Lease 时进入 Ready；普通加载、解码失败或超过单张图片安全限制时进入 Error。缓存预算不是当前图片可见性的硬门槛，当前图片不会仅因超过非当前缓存预算就显示错误，但它绝不能绕过 `LoadLimits`。进程级资源耗尽等不可恢复故障不承诺都能安全转换成 Error，控件不能以“工业级”为名宣称可以吞掉任意致命内存异常。

所有主图状态 Presenter 默认 `IsHitTestVisible=false`，只通过自动化语义报告状态；Navigation、Toolbar 和 Filmstrip 保持各自真实 Bounds 的输入优先级。状态视觉裁剪到 Viewport，不影响 OverlayPanel 的空间计算。Empty、Loading 与 Error 的图形结构和文案位置由包内固定 AXAML 管理，开发者只能通过 `ViewportAppearance` 修改背景、边框和各状态语义画刷；首版不公开 `EmptyContent`、`LoadingContent`、`ErrorContent`、`ErrorImageSource` 或状态模板。Thumbnail 状态视觉同样只能通过 `ThumbnailItemAppearance` 的属性级入口定制。

### 主图邻项预取

首版公开两种主图预取模式：

```csharp
public enum ImageGalleryMainImagePrefetchMode
{
    Disabled,
    Adjacent
}

public ImageGalleryMainImagePrefetchMode MainImagePrefetchMode { get; set; }
    = ImageGalleryMainImagePrefetchMode.Adjacent;
```

`Disabled` 只加载当前选择；`Adjacent` 在资源与预算允许时尽力准备稳定顺序中的前一张和后一张完整主图。首版不公开任意预取距离，避免开发者无意中请求大量完整图片。

预取只由 `SelectionModel` 的当前选择产生，缩略图走廊的可视范围绝不能直接驱动完整主图加载：

```text
Thumbnail Filmstrip Visible / Overscan
    └── 只驱动缩略图需求

Current Selection
    ├── 驱动当前主图
    └── 驱动合法相邻主图预取
```

新的当前主图取得 `Ready` 后，选择需保持稳定且缩略图走廊停止活跃滚动约 `200ms`，才启动新的相邻预取。该稳定延迟是首版内部策略，不公开属性。走廊快速滚动期间暂缓尚未开始的预取，但不取消当前主图；已经执行的预取保持低优先级，除非失去所有消费者并按通用取消规则处理。

当前主图拥有独立最高优先级启动通道；相邻主图预取首版最多同时执行一个，使用Speculative槽位或在没有前台缩略图等待时借用空闲Thumbnail槽位，绝不能占用Current Main专用槽位。前台负载持续繁忙时预取允许延后。默认优先最近一次相邻导航方向；尚无方向时优先Next。目标集合仍为前后各一个合法邻项，不进行速度预测。

预取与正式选择使用相同 Cache Key 和共享底层加载。预取项成为当前选择时必须提升同一个请求，而不是取消并重新开始：已完成则直接取得 Lease，正在运行则增加当前消费者并提升优先级，尚在队列则立即提升。当前消费者仍使用自己的 RequestVersion 和 Selection Identity 决定是否允许提交 Viewport。

选择改变后，不再属于新邻项且无其他消费者的旧预取被移出队列或请求取消；原当前图片若成为新选择的邻项，可以在预算允许时从当前 Lease 降级为非当前缓存引用，避免返回时重新解码。循环导航启用时按循环后的逻辑邻项计算并按 Cache Key 去重；单项集合不预取，两项集合的 Previous 和 Next 指向同一项目时只产生一个共享需求。

预取失败是推测性失败，不改变当前 `ImageState`、缩略图状态或业务项有效性，也不启动后续加载。项目随后成为当前选择时允许发起一次正式当前主图加载；只有当前请求失败才进入主图 `Error`。

### 主图解码缓存预算

目标公开非当前完整主图的内存预算：

```csharp
public long MainImageCacheMemoryBudgetBytes { get; set; }
    = 128L * 1024 * 1024;
```

预算只约束可淘汰的非当前完整主图，包括相邻预取结果和最近浏览过的图片。当前正在呈现的主图 Lease 必须保留到选择变化、图片替换或控件卸载，不计入这项可淘汰预算：

```text
Approximate Main Image Memory
    = Current Main Image
    + Non-current Cache (bounded by budget)
    + Temporary decode / platform rendering overhead
```

因此该属性不是进程总内存硬上限。如果当前图片本身满足 `LoadLimits`、但估算内存已经超过非当前缓存预算，仍然允许正确显示；控件应清空或停止接纳非当前主图并暂停没有容量意义的预取，不能为了满足缓存预算突然移除当前图片。

预算必须大于等于零，负数属于配置错误。`0` 表示不保留任何非当前完整主图，因此相邻预取没有可接纳空间而不执行；它不妨碍当前主图加载。`MainImagePrefetchMode = Disabled` 与正预算可以保留已经浏览过的近期主图；二者职责不同，不构成非法组合。

邻项不是无条件同时常驻。缓存根据 Lease 的 `EstimatedMemorySizeBytes` 在预算内尽力保留：

```text
预算足够      保留 Previous + Next
只能容纳一项  优先最近导航方向，无方向时优先 Next
单项超过预算  不接纳为非当前缓存
大小未知      当前图片允许显示，非当前图片不长期缓存
```

如果图片源在加载前能够提供可靠尺寸估算，调度器应在预取前跳过确定无法接纳的项目。真实 Lease 返回后再次验证估算；结果仍是非当前项且超过单项预算或大小未知时立即释放消费者引用，不为了已经完成的推测性工作突破缓存合同。如果它在加载期间成为当前选择，则提升为 Current 并允许显示。

新非当前结果的接纳顺序为：先估算大小，按“当前导航方向邻项、另一邻项、最近浏览项、较旧项”的优先级淘汰低价值缓存引用，只有空间足够后才接纳。不能先让缓存长期超预算再异步清理；解码完成瞬间仍可能产生不可完全避免的短时峰值。

从 B 切换到已预取的 C 时，C 从缓存提升为 Current，不重新加载；旧 B 若是 C 的合法邻项且预算允许，则转为非当前缓存。缓存淘汰只释放缓存自己的 Lease 引用，其他消费者仍使用同一共享图片时不得强制销毁底层 `IImage`。

运行中增大预算只允许后续请求逐步利用空间，不立即批量加载；缩小预算时立即按上述优先级释放非当前缓存引用，直到缓存自身的估算总量不超过新预算。当前主图和仍被其他非缓存消费者持有的图片不会被强制销毁，因此实际进程内存可以暂时高于该值。

主图缓存预算与缩略图内存预算必须分开，避免大量缩略图淘汰相邻完整主图，或巨大主图挤掉缩略图走廊所需的全部轻量预览。两类缓存都仅存在于本次应用运行期间，不写入磁盘。

## 主图呈现模式与外部资源消费

ImageGallery 是资源生命周期和选择状态的权威，但它不应强迫所有消费者都通过默认 Viewport 查看主图。为裁剪、标注、取色等需要复用当前已解码图片的通用宿主场景，控件公开两种主图模式：

```csharp
public enum ImageGalleryMainImageMode
{
    Presented,
    ResourceOnly
}

public ImageGalleryMainImageMode MainImageMode { get; set; }
    = ImageGalleryMainImageMode.Presented;

public PixelSize? MainImageDecodeSizeHint { get; set; }
    = null;
```

Presented 保持默认图片查看器。ResourceOnly 不是“不加载主图”，而是“Gallery 继续维护逻辑主图资源，默认 Viewport 不绘制它”：

```text
共同保留                         ResourceOnly 关闭
├── ItemsSource / Selection      ├── 默认主图绘制与状态占位视觉
├── 当前主图加载与缓存           ├── Viewport Wheel/Pan/Pinch
├── ImageState                   ├── Viewport Previous/Next
├── Title / ToolbarContent       └── Zoom/Fit/1:1/Rotation 默认工具
├── Thumbnail Filmstrip
├── Adjacent 基线预取
└── 当前资源安全借用
```

ResourceOnly 的 Viewport 仍参与与 Presented 相同的 Measure/Arrange，避免模式切换导致外围布局跳变；空白主图区不绘制背景、图片和 Empty/Loading/Error 视觉，也不命中输入，使下方业务画布可接收指针。根边框装饰是非命中绘制；ToolbarContent、缩略图走廊等 Overlay 在自己的真实 Bounds 内继续交互。模式切换只取消正在进行的主图手势和 Pointer Capture，不取消加载、不释放当前槽、不重置 Zoom/Pan/Rotation、不改变选择，也不产生资源变化事件。切回 Presented 后直接使用已有当前资源。

`MainImageDecodeSizeHint` 以 EXIF 规整后的整张逻辑图片物理像素表达。它是质量下界提示，不是最终图片容器大小，也不允许 0 或负数。Presented 综合 Viewport、RenderScaling、Zoom、Rotation 与 Hint；ResourceOnly 综合 Viewport、RenderScaling 与 Hint，但不让仅被保留的 Gallery Zoom 推高外部资源模式的解码需求。两者都经过 128 pixel 分档和 LoadLimits 约束；Adjacent 预取永远只使用不含高 Hint 和当前高倍 Zoom 的 Viewport 基线。

零尺寸 Viewport 且 Hint=null 时，Gallery 保持 Loading 并等待真实需求，绝不能把 `TargetPixelSize=null` 解释成“完整解码原图”。加载中提高 Hint 不取消已经运行的首帧：先提交首个合法结果，再对最新需求进行一次清晰度追赶。Ready 后采用约 150ms 稳定窗口、约 25% 提升阈值和只升不降策略。自定义 Source 低于请求交付时，只要首次 Lease 合法即可 Ready；升级候选必须以实际 DecodedPixelSize 改善当前质量，否则释放且不触发事件、不重复死循环。

外部消费只复用现有 `ImageGalleryImageLease`，不引入仅服务某个应用的新图片包装类型：

```csharp
public bool TryAcquireCurrentImage(
    IImageGalleryItem expectedItem,
    out ImageGalleryImageLease? lease);

public event EventHandler? CurrentImageResourceChanged;
```

获取 API 只允许在 UI 线程调用，并同时核验 Ready、稳定 Key、Source Identity、当前 Descriptor 与请求代次。Loading 期间即使旧帧仍在绘制，也必须返回 false，防止外部把旧帧误认为新选择。成功返回的是调用方独立拥有的新 Lease；Gallery 的当前槽只持有自己的 Lease，缓存只持有基础引用。缓存淘汰、切图和 Detach 只释放各自引用，外部 Lease 持有期间底层资源继续有效；调用方必须 Dispose，且这部分内存不属于 Gallery 缓存预算。

事件不携带 Lease，避免在通知边界制造模糊所有权。它只在 UI 线程、完整提交后触发：选择开始导致资源不可取得、首次 Ready、成功清晰度升级、当前失败、清空、Detach。模式切换、过期候选、升级失败和没有实际质量改善的候选均不触发。事件处理器如果需要图片，必须以当前 SelectedItem 再次调用 TryAcquire，不能保存事件发生前的“推测资源”。

## 视觉树附加、脱离与资源休眠

首版采用“根ImageGallery脱离视觉树即释放全部图片重资源，重新附加后按当前选择重新加载”的固定策略，不提供保留缓存开关。实现只监听根控件自身的视觉树生命周期：

```csharp
protected override void OnAttachedToVisualTree(
    VisualTreeAttachmentEventArgs e);

protected override void OnDetachedFromVisualTree(
    VisualTreeAttachmentEventArgs e);
```

内部 `PART_Viewport`、Toolbar、Filmstrip或Theme模板部件被替换、重建或单独Detached，不等同于根ImageGallery离开页面，不能触发整库资源清理。根控件Detach可以来自Tab切换后移除页面、导航替换内容、关闭窗口或从父容器移除；它只表示暂时不再参与当前视觉树，不表示对象必然被销毁。

根控件每次Detached时按确定顺序执行幂等清理：

```text
1. 增加LifecycleGeneration，使全部旧异步结果失去提交资格
2. 取消当前主图、相邻预取和全部缩略图请求
3. 终止PressCandidate、Panning、Pinching与所有Capture/手势所有权
4. 解除虚拟缩略图容器的消费者注册并释放其Lease
5. 释放当前主图Lease
6. 释放非当前主图缓存和缩略图缓存持有的全部引用
7. 清空失败抑制记录、排队需求和仅服务于旧视觉窗口的调度状态
```

释放 Lease 始终只调用其统一 `Dispose`，由 Lease 的一次性 Release Callback 完成真实资源结束动作；Gallery 不判断底层图片是私有还是共享。请求无法及时响应 Cancellation 时，其迟到结果仍必须经过 LifecycleGeneration、RequestVersion 和 Identity 检查；只能在 UI 线程立即 Dispose 返回 Lease，不得重新填充已休眠 Gallery 的缓存或视觉状态。

Detach不修改业务与配置状态：

```text
保留
├── ItemsSource及集合订阅
├── SelectedIndex / SelectedItem / Selection Identity
├── 轻量Descriptor与Title等业务元数据
├── Appearance和全部开发者配置属性
├── MainImageMode与MainImageDecodeSizeHint
└── Zoom/Pan/Rotation等轻量查看配置

释放或结束
├── 所有解码图片、Lease和图片缓存引用
├── 所有图片请求及调度队列
├── 已实现的缩略图视觉窗口与消费者资格
└── Pointer/Pinch交互状态
```

Detach 终止正在执行的手势，但保留 PanOffset、RotationAngle、ZoomMode 和 CustomZoomFactor；同一逻辑选择重新加载提交后按新 Viewport 重新 Clamp，而不是提前清零。若 Detached 期间选择身份已经改变，新选择成功提交时仍执行普通的新图 Viewport 重置。

Detach 释放 Gallery 当前槽并把 ImageState 原子切换为 Empty，不启动新的图片请求；已经由 TryAcquireCurrentImage 返回的外部 Lease 继续有效，但这不表示 Gallery 自己仍 Ready。重新 Attached 时执行：

```text
1. 增加新的LifecycleGeneration
2. 验证当前集合及选择，必要时按既有集合规则修正
3. 空集合保持Empty，不创建Filmstrip
4. 非空选择进入Loading，重新加载当前主图
5. 布局完成后重建当前Visible + Overscan缩略图需求
6. 主图Ready且稳定后，再按既有规则恢复相邻预取
```

控件Detached期间集合仍可能变化。协调器继续维护轻量Descriptor和Selection一致性，但不得启动图片加载；重新Attached只加载最终有效选择与最终可视范围，不回放Detached期间经历过的每个中间选择。

单纯 `IsVisible=false` 不触发上述释放。它可能来自短暂动画、折叠区域或很快恢复的Tab实现，控件无法从一个布尔值判断隐藏时长；首版继续保留资源并允许正常取消/替换既有请求。需要长期隐藏并强制释放内存的应用应从视觉树移除该控件，而不是期待ImageGallery猜测业务生命周期。

重复Attach/Detach必须安全，不得重复Dispose同一Lease、重复解除同一消费者或恢复已失效请求。对应Headless测试至少覆盖：Detach后全部内部图片引用归零、外部 Lease 继续有效、迟到结果无法回填、Selection与视图状态保持、再次Attach只加载当前项，以及内部模板重建不会误清全局缓存。

## 基本状态流

当前只确认单一选择状态驱动全部呈现：

```text
Image Collection
       │
       v
Current Selection
  ├──> Main Image
  ├──> Toolbar Metadata / Scale State
  ├──> Selected Thumbnail Visual
  └──> Navigator Follow Position
```

缩略图点击和 Previous/Next 都应进入同一选择提交路径。异步加载采用上述请求版本与身份双重校验。普通选择和集合变化遵守前文已经确定的规则；`SelectionChanged` 回调内部同步修改选择或集合的嵌套重入语义，明确属于首版之后的 TODO。

## 本地化与运行时语言切换

ImageGallery 不建立包内静态语言单例，也不公开每个控件独立的 `Culture` 属性。它直接复用允许依赖的 `AtomUI.Core` 应用级语言基础设施，以 `ThemeManager.LanguageVariant` 作为整个 AtomUI/Labs 应用的唯一语言状态：

```text
ThemeManager.LanguageVariant
        │
        ├── 当前语言ResourceDictionary
        ├── Avalonia DynamicResource更新
        └── LanguageVariantChanged
                    │
                    └── 必须由C#计算的组合文案更新
```

这项复用只需要 `AtomUI.Core` 和编译期 `AtomUI.Generator`，不得为本地化引入 `AtomUI.Controls`、`AtomUI.Desktop.Controls` 或 `AtomUI.Toolkits.GalleryBase`。ImageGallery 通过自己的 `UseImageGallery()` 注册入口把包内 Theme 与语言 Provider 显式加入 `IThemeManagerBuilder`；不得扫描程序集发现语言文件，也不得在注册时擅自覆盖宿主已经选定的全局 `LanguageVariant`。

### 强类型Key与分语言文件

固定文案使用Generator产生的强类型资源Key，不在控件逻辑或AXAML中散落字符串Key：

```text
Localization/
└── ImageGalleryLang/
    ├── zh_CN.cs
    ├── en_US.cs
    └── zh_TW.cs    仅作为当前AtomUI内建变体的zh-CN显式回退桥
```

`zh_CN.cs` 与 `en_US.cs` 分别保存简体中文和英文真实翻译；两者必须具有完全相同的Key集合，例如 NoImages、Loading、ImageLoadFailed、PreviousImage、NextImage、ZoomIn、ZoomOut、Fit、ActualSize、RotateClockwise 和 AddImage。生成结果至少包含 `ImageGalleryLangResourceKind`、对应AXAML语言资源扩展和包内 `LanguageProviderPool`，使缺Key、重复Key或类型错误在生成/测试阶段暴露。

ImageGallery首版只正式支持 `zh-CN` 与 `en-US`。AtomUI Core当前还存在 `zh-TW` 内建变体；由于本控件不提供繁体翻译，必须为该变体注册一个显式复用zh-CN值的桥接Provider，避免应用其他AtomUI模块启用zh-TW后，ImageGallery动态资源退化成Key名称。该桥只实现“未支持语言回退中文”，不构成繁体中文支持声明。

控件负责本地化自己拥有的 Empty、Loading、Error通用短文案，默认按钮ToolTip、基础Automation Name和缩放百分比格式。`IImageGalleryItem.Title`、`ToolbarContent`、文件名、业务标签、自定义命令界面和异常详情由开发者负责；ImageGallery既不翻译，也不得把文件路径、服务地址或异常原文作为兜底文案显示。

### 启动语言与中文兜底

宿主启动时读取 `CultureInfo.CurrentUICulture`，再显式规整到ImageGallery首版支持集合：

```text
系统语言为zh-*       -> LanguageVariant.zh_CN
系统语言为en-*       -> LanguageVariant.en_US
无法取得或其他语言   -> LanguageVariant.zh_CN
```

不能直接依赖AtomUI当前 `WithDefaultCultureInfo` 对未知语言的默认映射，因为该现有实现会回退 `en_US`，不符合本项目已经确认的中文兜底。宿主应先用无反射、确定性的启动解析器得到上述变体，再调用 `WithDefaultLanguageVariant`。`UseImageGallery()` 只注册资源，不第二次决定或改写应用语言。

Gallery、README和正式集成示例必须展示这一启动顺序。ImageGallery包本身不能承诺翻译宿主业务页面；“软件整体切换”要求所有业务模块与AtomUI/Labs控件都使用同一 `ThemeManager`，并消除自然语言硬编码。

### 运行时切换与动态通知

用户在应用启动后选择新语言时，由应用在Avalonia UI线程一次设置：

```csharp
ThemeManager.Current!.LanguageVariant = LanguageVariant.en_US;
```

ThemeManager负责切换当前语言ResourceDictionary并触发 `LanguageVariantChanged`。普通AXAML文本、ToolTip与能够使用Avalonia属性表达的Automation Name优先通过语言资源扩展/DynamicResource绑定，让资源替换自动刷新；不得让每个TextBlock和Button都手写订阅全局事件。

只有必须由C#组合或格式化的文本才监听 `LanguageVariantChanged`。这类订阅必须跟随根ImageGallery的视觉树生命周期建立和解除，或使用可释放的资源Observable绑定；禁止全局ThemeManager事件永久持有已经Detached且不再使用的控件。模板重新应用同样不能产生重复订阅。

语言切换只更新文案、ToolTip、基础Automation Name和数值格式，不改变 `SelectedItem`、`ImageState`、当前Lease、缩放、旋转、平移、布局Placement、请求版本、加载队列或缓存。状态更新在当前UI提交中生效，最终文字在下一次正常渲染呈现，不重新加载图片或重建业务集合。

首版不为每条固定文案增加 `EmptyText`、`ErrorText`、`PreviousToolTip` 等零散公共属性，也不开放状态模板。语言Key表完整性、zh-TW桥接、未知系统语言到zh-CN、运行时双向切换、重复Attach/Detach订阅数、NativeAOT裁剪后资源存在性，以及切换语言不产生任何图片请求，都属于发布前硬性测试。

## 首版键盘输入边界

ImageGallery 首版不注册控件范围或应用范围的 `R`、`+`、`-` 字符快捷键。根控件不能在隧道或冒泡阶段抢夺来自 `ToolbarContent`、文本输入控件或外层应用的普通字符输入：

```text
R       不由 ImageGallery 自动绑定旋转
+ / -   不由 ImageGallery 自动绑定缩放
```

旋转和缩放通过 `RotateClockwiseCommand`、`ZoomInCommand`、`ZoomOutCommand`、`FitCommand`、`ActualSizeCommand` 公开。默认 Toolbar 按钮使用这些命令；开发者需要键盘手势时，在应用层明确绑定并承担与文本输入、菜单及平台快捷键的冲突处理。隐藏默认 Toolbar 区域不会移除命令能力。

首版不实现 ImageGallery 专用键盘导航和焦点协调，包括：

- Filmstrip 主轴方向键选择。
- `Tab` / `Shift+Tab` 的组件级焦点顺序。
- 虚拟缩略图的 Roving Focus。
- Thumbnail Item 的 `Enter` / `Space` 专用激活合同。
- `Home` / `End` 首尾定位。
- 焦点缩略图被虚拟化回收后的焦点迁移。

ImageGallery 不主动截获上述按键，也不为了排除专项设计而破坏 `Button` 等模板部件从 Avalonia 基类继承的默认输入语义；但这些继承行为不构成 ImageGallery 首版专用键盘合同，也不进入首版专项测试矩阵。应用若在首版需要键盘操作，应在应用层基于公开命令和选择属性显式实现。

后续版本启动本 TODO 时，必须把虚拟化感知的焦点模型作为整体设计，禁止零散加入单个按键。尤其不能把大量缩略图全部变成独立 Tab 站点，也不能让已回收容器继续拥有逻辑焦点。

### 屏幕阅读器专项支持边界

首版不考虑屏幕阅读器专项场景，不实现 ImageGallery 专用 AutomationPeer、状态 Live Region、缩放/旋转语音播报、虚拟缩略图自动化导航或图片内容描述生成，也不为这些能力建立首版测试矩阵。`IImageGalleryItem.Title` 继续服务业务标题与默认Toolbar，不额外承诺能够形成图片替代文本。

这项边界不意味着主动移除内部 Avalonia `Button`、`ListBoxItem` 等基础类型天然提供的 Automation 语义；固定模板不得把标准交互控件降级为无法聚焦的纯装饰节点。开发者仍可在自己的 `ToolbarContent` 和业务模板中设置标准 AutomationProperties。但是，ImageGallery 首版不得宣称已经通过屏幕阅读器、完整无障碍或相关企业合规验收。未来引入专项支持时必须单独设计虚拟化集合、状态播报节流、本地化名称和自动化测试，不能把当前基础行为包装成正式承诺。

## 高对比度与减少动态效果

高对比度与减少动态效果属于视觉可用性合同，不等同于屏幕阅读器或键盘快捷操作。首版自动遵循平台能够提供的用户偏好，不公开 `IsHighContrastEnabled`、`IsReducedMotionEnabled` 或允许应用强制覆盖用户偏好的根控件属性。平台没有提供对应偏好时使用普通主题行为。

根控件将平台状态投影为供包内默认 Theme、公开 Appearance 生效逻辑和开发者 `ToolbarContent` 使用的稳定伪类：

```text
:high-contrast
:reduced-motion
```

平台偏好在控件仍处于视觉树时改变，应在 UI 线程更新伪类并重新求值相关资源，不重建 ItemsSource、SelectionModel、Descriptor、当前 Lease 或缩略图缓存。根控件 Detach 后解除平台状态监听，重新 Attach 时读取最新状态，不能通过静态事件订阅使 Gallery 无法回收。

### 高对比度

高对比度模式不处理图片自身像素，也不把整个 Gallery 简单转换为黑白。它要求覆盖在任意复杂图片上的控制表面具有稳定可辨识性：

- Toolbar、Filmstrip、Previous/Next 和 Add Image 的默认背景使用不透明的系统或 AtomUI 高对比度资源，不能继续依赖图片背景与半透明叠加产生可读性。
- 文字、图标、边框、分隔线和状态视觉使用具有明确前景/背景反差的语义资源，不能硬编码只适合普通 Light 或 Dark 主题的灰度值。
- 当前缩略图不能只通过细微颜色变化表达选择，必须至少再使用清晰边框、边框粗细或等价非颜色线索。
- Loading、Error、Selected、Disabled 和 PointerOver 的关键差异不能只依靠相近颜色；Error 兜底图和 Loading 静态图必须保持不同轮廓或语义图形。
- Navigation Button 和 Add Image Button 的命中区域不变，图标必须在其实际背景上保持可见，不能因透明背景或图片局部颜色而消失。
- 高对比度切换只改变资源和视觉状态，不改变布局尺寸、选择、导航边界、图片加载或缓存策略；需要更粗边框时，默认 Theme 应在既有按钮和槽位范围内处理，避免切换后整库明显跳动。

包内默认 Theme 必须提供高对比度资源兜底。开发者通过 `ViewportAppearance`、`ToolbarAppearance`、按钮 Appearance、`FilmstripAppearance`、`ThumbnailItemAppearance` 修改视觉，或注入 `ToolbarContent` 后，应继续使用具有合格对比度的语义资源；ImageGallery 必须保证未被显式覆盖的 Appearance 成员仍随默认资源适配，但不能保证开发者提供的任意画刷或第三方自定义内容自动获得合格对比度。

### 减少动态效果

减少动态效果只移除非必要、由控件自行播放的运动，不关闭用户直接控制的反馈，也不改变功能结果：

```text
应停止或改为立即提交
├── 循环旋转、脉冲或闪烁的 Loading 装饰动画
├── 主图切换淡入淡出
├── 自动定位缩略图的平滑滚动
├── Toolbar / Filmstrip 的自动滑入滑出
└── Theme 添加的非必要位移、缩放和旋转 Transition

继续保留
├── Pointer 拖拽直接驱动的 Pan
├── Pinch 手势直接驱动的 Scale 与中心点平移
├── 鼠标滚轮直接驱动的 Zoom 或 Filmstrip 滚动
├── 点击后的立即选择、缩放和四分之一圈旋转结果
└── Loading / Ready / Error 的清晰静态状态变化
```

当前首版已经确定主图切换不使用双 Lease 淡入淡出、顺时针旋转不播放旋转动画、选择变化立即提交，因此这些路径天然满足减少动态效果。实现仍必须防止默认 Theme 或后续样式在 `:reduced-motion` 下重新引入同类 Transition。

系统进入减少动态效果后，正在执行的非必要自动动画应停止并立即提交最终合法状态，不能排队等待动画结束；正在进行的 Pointer 或 Pinch 直接操作不被突然取消。系统退出该模式只影响后续视觉行为，不补播先前跳过的动画。

### 验收边界

Headless 和真实默认 Theme 验收至少覆盖：

- 运行中切换两个平台偏好时，根伪类与主题资源同步更新。
- 高对比度下 Toolbar、Filmstrip、导航按钮、追加按钮、选中缩略图及 Loading/Error 在明亮和深色主图上均保持清晰。
- 选中状态存在颜色之外的视觉线索。
- `:reduced-motion` 下没有持续 Loading 旋转、自动平滑定位或非必要 Transition，状态与选择结果仍然正确。
- Pointer Pan、Pinch 和鼠标滚轮等直接输入在减少动态效果下仍正常响应。
- 偏好切换不重新加载当前图片、不改变选择、不清空缓存，Detach 后不存在平台事件订阅泄漏。

像素对比度的最终数值阈值应结合 AtomUI Token、高对比度主题和真实渲染后再校准；首版实现前已经确定的硬性红线是默认表面不得继续依赖透明叠加、关键状态不得只靠颜色、减少动态效果不得遗留无限装饰动画。不能仅凭某一张示例图片上“肉眼还能看见”宣布高对比度通过。

## 当前边界

本草案不把 `ImageGallery` 定义为：

- 普通图片网格或瀑布流布局控件。
- 图片编辑器。
- 文件管理器或持久化系统。
- 默认打开系统文件选择器并直接修改业务集合的控件。
- 已经具备无限图片集合、异步加载、缓存或虚拟化性能保证的控件。

未来如果需要网格浏览、Lightbox、图片编辑或文件导入，应先判断它们是 ImageGallery 的既有组合能力、受约束属性级扩展、独立控件还是业务层职责，不能通过替换 ImageGallery 根模板把所有图片相关能力堆入一个万能控件。

## 依赖边界

ImageGallery 采用比 Labs 全局规范更严格的专项依赖约束，并向 LED 正式控件包的轻量依赖方式看齐：

```text
AtomUI.Labs.Controls.ImageGallery
├── Avalonia
├── AtomUI.Core
└── AtomUI.Generator（语言Provider与强类型Key生成，编译期、PrivateAssets=all）
```

正式控件包不得直接或传递依赖：

- `AtomUI.Toolkits.GalleryBase`。
- `AtomUI.Controls`、`AtomUI.Controls.Shared`。
- `AtomUI.Desktop.Controls` 及其成型控件包。
- AtomUI 的 `ImagePreviewer`、`Carousel`、`IconButton` 等成型控件实现。

可以阅读 AtomUI 成型控件源码作为设计参考，但不能复制其源码，也不能把内部类型变成 ImageGallery 的实现依赖。

Gallery 展示项目与正式控件包必须分层：

```text
AtomUILabsGallery
├── AtomUI.Labs.Controls.ImageGallery
├── AtomUI.Toolkits.GalleryBase
└── AtomUI.Desktop.Controls
```

GalleryBase 与 Desktop Controls 只允许用于 ShowCase、属性工作台、Gallery 路由和人眼验收，不能进入 ImageGallery 默认主题、公共 API 类型或正式 NuGet 依赖。打包验收必须扫描 `.nupkg` 依赖；出现 GalleryBase 或 AtomUI 成型控件包时直接判定失败。

## NativeAOT、trimming与源码生成边界

ImageGallery正式实现不得依赖运行时反射、动态成员访问、字符串字段推测、`Activator.CreateInstance`创建未知业务类型、运行时代码生成或反射扫描特性。数据链路的静态边界固定为：

```text
ViewModel.GalleryItems
    │ 代码赋值或Avalonia CompiledBinding
    v
ItemsSource : IEnumerable
    │ 普通类型检查与接口调用
    v
IImageGalleryItem
    ├── Key
    ├── Title
    ├── MainImageSource
    └── ThumbnailImageSource
```

`ItemsSource`本身因继承Avalonia选择集合控件而保持 `IEnumerable?`，这不表示控件需要反射项目；运行时每项必须能够直接转换为 `IImageGalleryItem`。官方AXAML示例必须设置正确的 `x:DataType` 并使用 `CompiledBinding`，ShowCase和NativeAOT测试不得用普通 `{Binding ...}` 掩盖裁剪风险。

普通运行时Binding不被控件主动拦截，因为Binding引擎解析后ImageGallery只能看到最终集合值，无法可靠识别其来源；但它明确不属于严格NativeAOT/trimming保证。若开发者自行使用普通Binding、反射Converter或动态插件类型，必须由应用自己的裁剪描述、`DynamicDependency`或其他保留策略负责，ImageGallery不能替开发者业务程序集保留未知成员。

`AtomUI.Generator` 已经具有ImageGallery语言Provider、强类型资源Key和显式Provider池生成这一真实用途，因此作为编译期依赖并设置 `PrivateAssets=all`。它还可以减少StyledProperty、DirectProperty、注册或其他固定模式的样板代码，使最终生成结果成为编译器可见的普通C#，从而降低手写错误并改善AOT可分析性；它不是运行时组件，也不是普通Binding的补救机制：

```text
CompiledBinding       负责静态访问ViewModel集合
IImageGalleryItem     负责静态读取集合项目
AtomUI.Generator      只负责控件自身可生成的样板代码
```

生成器只以Analyzer形式参与编译，不能进入运行时依赖闭包。实现必须把语言生成结果及其他实际采用的生成代码纳入编译和测试，禁止手改GeneratedFiles，也禁止在运行时重新扫描同一声明。

默认ControlTheme、DataTemplate、矢量状态资源和配套控件类型必须通过编译AXAML及 `ImageGalleryThemesProvider` 的明确资源引用进入应用，不能依靠按字符串扫描程序集寻找Theme。自定义 `IImageGallerySource` 通过普通接口实例或静态工厂创建；首版不提供按类型名称动态装载Source的插件机制。

严格发布验证必须包含一个真实启用trimming与NativeAOT的桌面宿主，覆盖：CompiledBinding取得ItemsSource、接口项目读取、默认Theme加载、文件/资源图片Source、主图与缩略图呈现、选择切换、虚拟化、Toolbar命令和Detach/Attach。普通Debug/JIT构建成功不能替代该发布验证。

真实宿主固定使用 `controlgallery/AtomUILabsGallery.Desktop`，并通过专用命令行 Smoke、结构化结果、退出码、干净发布、警告归因和交互式 Windows Agent 验证发布产物。整体 Benchmark、NuGet 审计、Gallery Case、人眼矩阵及最终发布闸门统一见 [整体性能、NativeAOT 与 Gallery 发布验收设计](release-and-gallery-verification.md)。

## 固定模板与属性级外观开放

ImageGallery 在包内继续使用 Avalonia 原生 `ControlTheme`、编译 AXAML、StyledProperty、伪类和 `AtomUI.Core` Shared Token 实现默认视觉，但 `ControlTheme` 只是内部实现机制，不是首版公共定制入口。对外开放边界固定为：

```text
行为属性             决定控件做什么
布局属性             决定受支持区域的位置、尺寸和间距
Appearance 属性对象  修改稳定角色的视觉参数
固定 ControlTheme    包内决定 AXAML 结构、部件和状态映射
```

开发者可以修改“装修参数”，不能修改“结构骨架”。根 `ImageGallery`、Viewport、Toolbar、Navigation Button、Filmstrip、Thumbnail Item 和 Add Image Button 的 `ControlTemplate` 均不属于公共合同。首版不得公开任何 `ToolbarTheme`、`NavigationButtonTheme`、`FilmstripTheme`、`ThumbnailItemTheme`、`AddImageButtonTheme` 或职责等价的 `ControlTheme` 属性。

### Appearance 公共类型

Appearance 是强类型视觉参数对象，不是 `Control`、`Style`、`ControlTheme`、`DataTemplate` 或任意内容容器。目标公共类型为：

```csharp
public sealed class ImageGalleryViewportAppearance : AvaloniaObject { }
public sealed class ImageGalleryToolbarAppearance : AvaloniaObject { }
public sealed class ImageGalleryButtonAppearance : AvaloniaObject { }
public sealed class ImageGalleryFilmstripAppearance : AvaloniaObject { }
public sealed class ImageGalleryThumbnailItemAppearance : AvaloniaObject { }
```

根控件公开以下可空 StyledProperty；`null` 表示该角色全部使用包内默认 Theme 与 Token：

```csharp
public ImageGalleryViewportAppearance? ViewportAppearance { get; set; }
public ImageGalleryToolbarAppearance? ToolbarAppearance { get; set; }
public ImageGalleryButtonAppearance? ToolbarButtonAppearance { get; set; }
public ImageGalleryButtonAppearance? ViewportNavigationButtonAppearance { get; set; }
public ImageGalleryButtonAppearance? FilmstripNavigationButtonAppearance { get; set; }
public ImageGalleryFilmstripAppearance? FilmstripAppearance { get; set; }
public ImageGalleryThumbnailItemAppearance? ThumbnailItemAppearance { get; set; }
public ImageGalleryButtonAppearance? AddImageButtonAppearance { get; set; }
```

同一个 `ImageGalleryButtonAppearance` 值类型复用于三类真实按钮角色，是因为它们共享背景、前景、边框、图标、命中尺寸和交互状态这一组稳定外观语义；三个根属性仍然彼此独立，设置 Toolbar 按钮外观不会影响 Navigation 或 Add Image。Viewport 与 Filmstrip 的导航按钮也分别开放 Appearance，避免以前依赖外部伪类选择器区分两处位置。

每个 Appearance 成员必须注册为 StyledProperty，以支持直接值、资源引用和合法 Binding。Appearance 是“稀疏覆盖”：实现必须通过 Avalonia 属性值来源判断某成员是否被显式设置，不能用 CLR 默认值猜测。成员未设置时保留内部 Theme 值；画刷成员被显式设置为 `null` 时表示开发者有意移除该画刷，这与“未设置”不同。根控件不得为所有实例共享一个可变默认 Appearance 对象。

各类型的首版属性控制面为：

| Appearance | 必须开放的属性级控制面 |
|---|---|
| `ImageGalleryViewportAppearance` | `Background`、`BorderBrush`、`BorderThickness`、`CornerRadius`、`Opacity`、`EmptyForeground`、`LoadingForeground`、`ErrorForeground` |
| `ImageGalleryToolbarAppearance` | `Background`、`Foreground`、`BorderBrush`、`BorderThickness`、`CornerRadius`、`Opacity`、`Padding`、`Width`、`Height`、`MinWidth`、`MaxWidth`、`MinHeight`、`MaxHeight`、`ItemSpacing` |
| `ImageGalleryButtonAppearance` | `Background`、`Foreground`、`BorderBrush`、`BorderThickness`、`CornerRadius`、`Opacity`、`Padding`、`Width`、`Height`、`IconSize`；`PointerOverBackground`、`PointerOverForeground`、`PointerOverBorderBrush`；`PressedBackground`、`PressedForeground`、`PressedBorderBrush`；`DisabledBackground`、`DisabledForeground`、`DisabledBorderBrush`、`DisabledOpacity` |
| `ImageGalleryFilmstripAppearance` | `Background`、`BorderBrush`、`BorderThickness`、`CornerRadius`、`Opacity`、`Padding`；交叉轴尺寸仍由根 `ThumbnailFilmstripExtent` 决定 |
| `ImageGalleryThumbnailItemAppearance` | `Background`、`PointerOverBackground`、`SelectedBackground`、`BorderBrush`、`BorderThickness`、`CornerRadius`、`Opacity`、`Padding`、`SelectionIndicatorBrush`、`SelectionIndicatorThickness`、`LoadingForeground`、`ErrorForeground` |

按钮的 PointerOver、Pressed 与 Disabled 属性由内部 `ImageGalleryButton` 状态解析器读取，优先级固定为 Disabled、Pressed、PointerOver、Normal。包内固定 `ImageGalleryButtonTheme` 的 ContentPresenter 只绑定解析后的根 Background、Foreground 与 BorderBrush，不能再建立第二套状态颜色，也不得继承宿主 Avalonia Button Theme 的 ContentPresenter 状态规则。状态解析必须观察 `IsEffectivelyEnabled`，以覆盖 Command/CanExecute 驱动的启用变化，而不只是开发者直接修改的 IsEnabled。

Appearance 不允许开发者增加新状态、改变命令或替换箭头和图标的视觉树。Previous/Next 箭头方向、Zoom/Fit/Rotate 图标语义和 Add Image 图标结构由固定模板决定，`Foreground` 与 `IconSize` 只修饰包内图标。需要完全不同图标或按钮结构属于新设计议题，首版不以模板替换解决。

`FilmstripAppearance.Padding` 表示走廊外边框到 Add/Navigation/Items 内容的内距；`ThumbnailItemAppearance.Padding` 表示缩略图片像素到单个 Thumbnail Item 边框的内距。相邻 Thumbnail Item 之间的外部主轴距离只由根 `ThumbnailItemSpacing` 决定，Appearance 不提供 Margin，也不能偷偷改变虚拟化步长。

选中缩略图的稳定属性继续命名为 `SelectionIndicatorBrush` 与 `SelectionIndicatorThickness`，不使用含义模糊的 `SelectedForeground`。指示器在现有 Thumbnail Item Bounds 内绘制并裁剪，不参与 Measure，也不改变 `ThumbnailItemExtent`、`SlotStride` 或相邻项位置。默认 Theme 提供非 null 语义画刷和 `2 DIP` 厚度。由于模板结构不开放，首版没有另一套可由开发者插入的非颜色选中标记，因此 `SelectionIndicatorBrush=null` 或 Thickness 全零属于非法配置；`SelectedBackground` 只能作为附加状态色，不能代替固定轮廓。开发者仍需为画刷选择满足主题与高对比度要求的语义资源。

`Opacity` 会使角色自身、文字、图标和全部子内容整体透明。只希望背景半透明时，应使用 `Background` Brush 的 Alpha，避免箭头、标题和状态指示器同时变淡。Thickness 各分量、CornerRadius、Padding、Spacing、IconSize 和显式尺寸必须遵守对应 Avalonia 有限值与非负约束；`Opacity` 必须是 `[0, 1]` 内有限值。非法 Appearance 值属于开发者配置错误，必须抛出包含类型、属性和值的明确异常。

### Appearance 用法与更新

属性级定制示例：

```xml
<atom.labs:ImageGallery
    ItemsSource="{CompiledBinding GalleryItems}"
    ThumbnailFilmstripExtent="120"
    ThumbnailItemSpacing="8">

    <atom.labs:ImageGallery.ToolbarAppearance>
        <atom.labs:ImageGalleryToolbarAppearance
            Width="640"
            Height="64"
            ItemSpacing="8"
            Padding="12,8"
            Background="#CC20242C"
            BorderThickness="0"
            CornerRadius="12" />
    </atom.labs:ImageGallery.ToolbarAppearance>

    <atom.labs:ImageGallery.FilmstripAppearance>
        <atom.labs:ImageGalleryFilmstripAppearance
            Background="#D9FFFFFF"
            BorderThickness="0"
            CornerRadius="16"
            Padding="8" />
    </atom.labs:ImageGallery.FilmstripAppearance>

    <atom.labs:ImageGallery.ThumbnailItemAppearance>
        <atom.labs:ImageGalleryThumbnailItemAppearance
            Padding="4"
            SelectionIndicatorBrush="#1677FF"
            SelectionIndicatorThickness="2" />
    </atom.labs:ImageGallery.ThumbnailItemAppearance>
</atom.labs:ImageGallery>
```

Appearance 可以定义为资源并被多个 ImageGallery 复用；实现不得修改开发者提供的对象。根控件附加到视觉树后订阅当前 Appearance 的属性变化，替换 Appearance 或脱离视觉树时解除旧订阅，重新附加时恢复。共享资源对象不能因为事件订阅而把已经移除的 Gallery 永久保活。

运行中修改合法 Appearance 值必须实时生效，但不能重建根模板、替换内部控件、重建 SelectionModel、重新加载图片或清空缓存。纯画刷变化只使对应角色重绘；Padding、尺寸与 Spacing 等布局值只使必要角色重新 Measure/Arrange；Thumbnail 布局变化还必须重新计算虚拟窗口并保持当前选中项最小必要可见。

Appearance 只负责视觉参数，不能承载 Placement、选择、导航循环、图片加载、缓存、缩放、旋转、响应式状态或命令。`ToolbarContent` 与 `ToolbarContentTemplate` 仍是唯一明确开放的自定义快捷内容槽，但模板只呈现该槽内业务内容，不能替换 Toolbar 外壳或插入默认区域之间。

### 包内 ControlTheme 与模板封闭合同

目标资源组织仍遵循 AtomUI 的静态 AXAML 与 Theme Provider 模式：

```text
ImageGalleryThemesProvider.axaml
  -> Themes/ImageGalleryThemes.axaml
       ├── ImageGalleryTheme.axaml
       └── Internal/
           ├── ToolbarTheme.axaml
           ├── ImageGalleryButtonTheme.axaml
           ├── FilmstripTheme.axaml
           └── ThumbnailItemTheme.axaml
```

这些 `ControlTheme` 和视觉辅助控件必须通过编译 AXAML 静态进入包，内部功能视觉节点不得为了隐藏类型而改成 C# 动态注入。内部 Theme 可以拆文件维护，但资源键、TargetType、模板部件、内部伪类和辅助控件类型均不构成公共兼容合同；包不得发布 `DefaultImageGalleryToolbarTheme` 等供外部 `BasedOn` 的稳定资源键。

包内视觉辅助类型保持 `internal sealed`，包括 Toolbar、默认 Toolbar Button、Navigation Button、Filmstrip、Thumbnail Item、Add Image Button、Viewport Presenter 和 Overlay Panel。它们可以分别拥有内部 `ControlTheme`，但不提供公共 `Theme` 属性转发，也不允许外部直接实例化。

根 `ImageGallery` 继承自 Avalonia `SelectingItemsControl`，因此框架已有的 `Theme` 能力无法从类型系统删除；这不等于 ImageGallery 承诺根模板可替换。首版明确禁止外部 `ControlTheme` 设置新的 `Template`，也不承诺删除、插入、重排或替换 Viewport、Toolbar、Filmstrip 及内部 Presenter 后仍能工作。开发者需要统一复用外观时，应使用作用于公开 `ImageGallery` 属性和 Appearance 属性的 Style，而不是为内部类型或根结构编写模板。

`OnApplyTemplate` 必须严格取得包内固定 Theme 的必需部件并验证准确类型。根模板缺少部件、部件类型错误或核心 Presenter 被替换时，应抛出包含部件名和预期类型的明确 `InvalidOperationException`，不能显示一个选择、缩放、虚拟化或资源释放已经失效的半残控件。该校验是违反模板合同后的快速失败，不是把内部部件变成公共扩展点。

允许与禁止边界汇总如下：

```text
允许
├── 根控件标准 Background / Border / Padding 等外观属性
├── 已确定的 Placement、Extent、Gap、Spacing 等布局属性
├── 各稳定角色的 Appearance 属性
├── 明确的显隐、行为和命令属性
└── ToolbarContent / ToolbarContentTemplate 业务内容槽

禁止
├── 替换根 ControlTemplate
├── 替换任意内部子组件 ControlTemplate
├── 直接实例化或样式定位内部视觉控件
├── 删除、插入或重排固定视觉节点
├── 替换 ImageGalleryViewportPresenter
└── 通过视觉配置反向改变选择、加载、缓存或变换行为
```

### 属性级开放验证

自动测试和 Gallery 验收至少必须证明：

- 公共 API 中不存在 `ControlTheme` 类型的 ImageGallery 定制属性，也不存在公开视觉辅助控件类型。
- 每个 Appearance 的显式成员都只影响目标角色；未设置成员继续使用默认 Token，显式 null 画刷与未设置能够正确区分。
- 两处 Navigation、Toolbar 默认按钮和 Add Image 可以使用不同的 `ImageGalleryButtonAppearance`，互不串值。
- Appearance 资源复用、运行中替换、单成员更新、Detach/Attach 和 GC 后不存在订阅泄漏。
- 颜色变化不触发布局；合法尺寸变化只使必要角色布局；任何 Appearance 更新都不改变选择、请求计数、Lease 平衡和缓存身份。
- 编译 AXAML、主题切换、高对比度和 NativeAOT 宿主均能使用 Appearance；非法数值快速失败。
- 用缺少必需部件或错误类型部件的测试根模板能够得到清晰异常，不能静默降级。
- 包内默认 Theme 的真实 Visual Tree 仍覆盖 Viewport、Toolbar、Filmstrip、虚拟化 ItemsPresenter 和状态 Presenter 完整链路。

## 已确定验证议题与后续 TODO

ImageGallery 整体 Benchmark、真实 NativeAOT 宿主、NuGet 边界、Gallery Case、人眼矩阵和候选发布闸门已经在 [整体性能、NativeAOT 与 Gallery 发布验收设计](release-and-gallery-verification.md) 中确定。缩略图走廊的专项海量数据与虚拟化证据继续见 [缩略图虚拟化测试与性能验收设计](thumbnail-virtualization-verification.md)。

ImageGallery 专用方向键、Tab 焦点顺序、Roving Focus、Enter/Space 与 Home/End，以及 `SelectionChanged` 回调内部同步修改选择或集合的重入协调，均已明确降级为首版之后的 TODO；屏幕阅读器专项支持与状态语音播报明确排除在首版之外。这些后续事项不阻塞首版施工，也不能在实现中零散加入并形成未经设计的隐式合同。

## 实现与发布验证现状

首版源码施工以以下设计条件为入口，当前均已形成对应实现或自动验证：

- 公共类型、属性、事件、命令、默认值和非法输入策略明确。
- 默认主题结构、模板部件和可替换边界明确。
- 图片加载、取消、缓存、释放和错误状态具有完整生命周期。
- 主图 Presenter 的高 DPI 尺寸、锚点缩放、平移边界、连续输入和 Pointer Capture 具有可重复的 Headless 测试。
- 大量缩略图场景的虚拟化方案和可验证指标明确。
- 输入冲突、选择状态和主图变换状态有唯一协调路径。
- Direct AXAML、绑定数据源和 Gallery 用例均有可执行验收设计。
- 默认 Theme 的高对比度和减少动态效果具有状态切换、静态兜底与资源泄漏测试。
- NativeAOT、trimming、Headless 交互测试和性能测试入口明确。

当前已知的首版实现阻塞性设计议题已经形成结论并完成源码施工。自动测试、性能基线、单固定种子 30 分钟真实默认模板 Headless 长稳、NativeAOT Runtime Smoke 和 NuGet 审计结果见专项验证文档；完整 Gallery 人眼、真实图片/DPI/触控矩阵和提交后干净检出尚未签署，因此最终 RC 发布结论仍必须等待这些证据。
