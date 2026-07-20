# LED Segment 工业级实现原理

> 文档状态：迁移参考。本文于 2026-07-20 从 AtomUI 仓库的 `dev-and-mark/modules/desktop-controls-labs` 复制到 AtomUI.Labs 并适配文档结构。`AtomUI.Labs.Controls.LED` 表示本仓库的目标设计；文中的“已实现”、验证数据及旧项目命令来自迁移前的 `AtomUI.Desktop.Controls.Labs` 参考实现，不表示当前仓库已经包含相应源码、测试或性能工具。

> 当前实现已将早期同形叠色Glow升级为共享Scoped Blur Glow，并补充`GlowRadius`。正式Glow契约与性能结论见[LED Glow技术路线选型](glow-technical-options.md)和[LED Glow原型评估](glow-prototype-evaluation.md)。

> 当前Render不再逐段提交Geometry命令。可见Inactive段聚合为一个缓存Geometry，可见Active段聚合为另一个缓存Geometry；Active聚合同时用于一次Glow Effect和清晰本体绘制。Text变化只替换当前Active聚合，同槽位同Geometry配置继续复用Inactive聚合，不保留历史文本缓存。

本文记录 `AtomUI.Labs.Controls.LED.Segment` 的目标实现原理。目标读者可以是第一次接触 LED 控件的新手，但实现标准必须按工业级自绘控件来约束。

`Segment` 是十四段数码管路线。它不是字体控件，不是点阵控件，也不是硬件 LED 控制器。

## 核心链路

Segment 的本质是基于字符映射表和参数化几何生成器的 Avalonia 自绘控件。

完整链路如下：

```text
原始 Text
  -> 字符规范化
  -> 字符到十四段映射
  -> 字符归类
  -> 布局测量
  -> 几何生成
  -> 绘制命令
  -> Avalonia 底层渲染
```

辅助技术链路与视觉链路共享同一份字符映射：

```text
原始 Text
  -> 字符规范化和不支持字符降级
  -> 实际可显示文本
  -> SegmentDisplayAutomationPeer
  -> 平台辅助功能系统
```

`SegmentDisplay` 在自动化系统中按只读 `Text` 内容暴露，而不是可编辑输入框。默认自动化名称等于实际可显示文本：ASCII 小写转换为大写，不支持字符转换为空格。开发者显式设置的 `AutomationProperties.Name` 优先级更高，可为时钟、计数器或单位值提供更自然的语义名称。`Text` 动态变化时，已创建的 AutomationPeer 必须同步发出 Name 属性变化通知。

每一层只能做自己的事：

- 字符规范化只处理输入字符，例如小写转大写。
- 字符映射只回答某个字符应该点亮哪些逻辑段。
- 布局测量只计算字符格尺寸和位置。
- 几何生成只把逻辑段转换为可绘制形状。
- 绘制命令只把几何、画刷和顺序提交给 `DrawingContext`。
- Avalonia 负责把绘制命令栅格化成屏幕像素。

严禁把字符判断、坐标计算和 `DrawingContext` 调用混在一串 `if/else` 中。

## 工程目录

`Segment` 必须保持和正式控件库一致的工程入口习惯：公共控件类型留在控件根目录，内部实现按稳定职责进入子目录，主题通过 `*Themes.axaml` 聚合。

```text
LED/Segment/
  SegmentDisplay.cs
  SegmentOverflowMode.cs
  SegmentValueSanitizer.cs
  Character/
    SegmentCharacterKind.cs
    SegmentCharacterMap.cs
    SegmentCharacterPattern.cs
    SegmentParts.cs
  Layout/
    SegmentCharacterSlot.cs
    SegmentDisplayLayout.cs
    SegmentLayoutEngine.cs
    SegmentLayoutOptions.cs
  Rendering/
    SegmentGeometryFactory.cs
    SegmentGeometryItem.cs
    SegmentGeometryOptions.cs
    SegmentGeometrySet.cs
  Themes/
    SegmentThemes.axaml
    SegmentDisplayTheme.axaml
```

主题入口链路：

```text
AtomUILabsThemesProvider.axaml
  -> LED/Themes/LEDThemes.axaml
      -> LED/Segment/Themes/SegmentThemes.axaml
          -> LED/Segment/Themes/SegmentDisplayTheme.axaml
```

`SegmentToken.cs` 当前不创建。Token 会固定主题契约，必须等视觉语义稳定后单独设计，不能作为目录对齐的附带动作。

## 职责边界

AtomUI Labs 需要负责：

- 字符应该如何规范化。
- 字符应该点亮哪些十四段逻辑段。
- 每个字符格在控件中的位置和尺寸。
- 每个逻辑段在字符格内的具体几何形状。
- 亮段、暗段、发光层的绘制顺序。
- 何时触发布局失效和视觉失效。

Avalonia 负责：

- 执行 `DrawingContext.DrawGeometry(...)`。
- 将 `Geometry` 栅格化为像素。
- 处理抗锯齿。
- 处理 DPI、render scale 和底层 Skia/平台渲染管线。

换句话说，Avalonia 提供画布和渲染管线，但不会知道十四段 LED 长什么样。十四段语义和几何必须由 Labs 自己定义。

## 字符映射

字符映射层是纯数据层，不依赖 Avalonia、主题、画刷或几何。

推荐用 `[Flags]` enum 表达逻辑段：

```csharp
[Flags]
internal enum SegmentParts
{
    None = 0,
    Top = 1 << 0,
    UpperLeft = 1 << 1,
    UpperRight = 1 << 2,
    MiddleLeft = 1 << 3,
    MiddleRight = 1 << 4,
    LowerLeft = 1 << 5,
    LowerRight = 1 << 6,
    Bottom = 1 << 7,
    UpperCenter = 1 << 8,
    LowerCenter = 1 << 9,
    UpperLeftDiagonal = 1 << 10,
    UpperRightDiagonal = 1 << 11,
    LowerLeftDiagonal = 1 << 12,
    LowerRightDiagonal = 1 << 13
}
```

`<<` 用于给每个段分配一个独立二进制位。一个字符可以通过按位或组合多个段：

```csharp
var parts = SegmentParts.Top
          | SegmentParts.UpperLeft
          | SegmentParts.UpperRight;
```

字符映射应集中维护：

```csharp
internal static class SegmentCharacterMap
{
    public static SegmentCharacterPattern GetPattern(char value)
    {
        // char -> SegmentCharacterPattern
    }
}
```

不要在渲染代码里写：

```csharp
if (ch == 'A')
{
    DrawTop();
    DrawUpperLeft();
}
```

这种写法会把字符语义和绘制逻辑绑定死，后续无法维护。

## 字符归类

映射结果不应该只有 `SegmentParts`。冒号、小数点和空格不是标准十四段字符，应该明确分类。

推荐模型：

```csharp
internal enum SegmentCharacterKind
{
    Empty,
    Segments,
    Colon,
    Dot
}

internal readonly record struct SegmentCharacterPattern(
    char Character,
    SegmentCharacterKind Kind,
    SegmentParts Parts);
```

示例：

```text
'8' -> Kind = Segments, Parts = Top | UpperLeft | ...
':' -> Kind = Colon, Parts = None
'.' -> Kind = Dot, Parts = None
' ' -> Kind = Empty, Parts = None
```

不支持字符第一版建议按空格处理，不抛异常。显示控件不应该因为输入中出现一个不可显示字符导致 UI 崩溃。

## 布局测量

映射决定“哪些段亮”，布局测量决定“每个字符放哪里、多大”。

布局输入：

- 规范化后的字符 pattern 列表。
- 字符期望高度或最终可用尺寸。
- 字符宽高比。
- 字符间距。
- 符号宽度规则。
- Padding。

`CharacterHeight` 表示期望字符高度，用于 `MeasureOverride` 计算理想尺寸。实际 `Render` 阶段会根据最终 `Bounds.Height` 重新计算 layout；如果父容器给了更高或更低的最终高度，最终绘制高度以 arranged bounds 为准。因此 `CharacterHeight` 不是强制绘制高度，而是参与测量的期望值。

段厚度和段间隙不参与理想尺寸计算。它们只影响字符格内部的几何形状，所以只应触发重绘，不应触发布局测量。

父容器给的最终空间可能大于或小于理想尺寸。工业级控件不能假设父容器一定尊重 `DesiredSize`：

- 空间更大时，由 `HorizontalContentAlignment` 和 `VerticalContentAlignment` 决定内容在最终 bounds 内的位置。
- 空间更小时，默认 `OverflowMode = Clip`，内容按真实尺寸绘制并裁剪到控件 bounds 内。
- 如果开发者显式设置 `OverflowMode = ScaleDown`，内容整体等比缩小到可用 bounds 内，但不会放大超过 1 倍。

这几个属性属于运行期布局/绘制策略，不是 Token。Token 决定默认视觉基因，StyledProperty 决定单个控件实例的最终行为。

布局输出：

```csharp
internal readonly record struct SegmentCharacterSlot(
    SegmentCharacterPattern Pattern,
    Rect Bounds);

internal sealed class SegmentDisplayLayout
{
    public Size DesiredSize { get; }
    public IReadOnlyList<SegmentCharacterSlot> Slots { get; }
}
```

布局算法应该独立，例如：

```csharp
internal static class SegmentLayoutEngine
{
    public static SegmentDisplayLayout Calculate(...)
    {
        // patterns + size options -> slots + desired size
    }
}
```

`MeasureOverride` 和 `Render` 必须复用同一套布局算法。不能在 `MeasureOverride` 粗算一套，在 `Render` 又重新写另一套位置计算。

## MeasureOverride、ArrangeOverride 和 Render

`MeasureOverride` 的职责是告诉父容器控件理想尺寸：

```text
Text 改变
  -> 重新规范化和测量
  -> InvalidateMeasure()
  -> InvalidateVisual()
```

`Render` 的职责是基于最终尺寸发出绘制命令：

```text
Render
  -> 根据 Bounds.Size 计算实际 layout
  -> 根据 OverflowMode 计算绘制缩放
  -> 根据 HorizontalContentAlignment / VerticalContentAlignment 计算偏移
  -> PushClip 到 Bounds
  -> PushTransform 应用缩放和偏移
  -> 为每个 slot 生成或读取 geometry
  -> 根据 pattern 绘制暗段和亮段
```

`ArrangeOverride` 不是主要几何生成入口。Segment 是自绘控件，通常没有子控件需要 arrange。`ArrangeOverride` 最多用于记录最终尺寸或标记缓存失效：

最终空间中的ScaleDown比例和内容对齐偏移由LED家族根目录的`LEDDisplayLayoutMath`计算。Segment仍自行决定何时启用ScaleDown，并保留最终Bounds参与字符高度布局的路线专属语义。

```csharp
protected override Size ArrangeOverride(Size finalSize)
{
    if (_lastArrangeSize != finalSize)
    {
        _lastArrangeSize = finalSize;
        InvalidateGeometryCache();
    }

    return finalSize;
}
```

不要把几何生成主要塞进 `ArrangeOverride`。几何不仅依赖最终尺寸，也依赖段厚度、间隙、几何风格和字符 slot。Avalonia 可能因为视觉失效重新 `Render`，但不一定重新 `Arrange`。

## 几何生成

逻辑段不是图形。几何生成负责把逻辑段转换为 Avalonia 可绘制的 `Geometry`。

推荐模型：

```csharp
internal readonly record struct SegmentGeometryItem(
    SegmentParts Part,
    Geometry Geometry);

internal sealed class SegmentGeometrySet
{
    public IReadOnlyList<SegmentGeometryItem> Items { get; }
}

internal static class SegmentGeometryFactory
{
    public static SegmentGeometrySet Create(
        Rect bounds,
        SegmentGeometryOptions options)
    {
        // bounds + options -> 14 segment geometries
    }
}
```

`SegmentGeometryFactory` 不关心当前字符是 `A`、`8` 还是 `K`。它只在给定字符格中生成完整十四段骨架。字符映射层决定哪些段亮。

横段和竖段建议表达为有厚度的填充形状，而不是简单 `DrawLine`。例如顶部段可以是一个斜切六边形：

```text
   /--------\
  /          \
  \          /
   \--------/
```

斜段可以通过起点、终点、方向向量和法线向量生成一个有宽度的多边形。

当前几何骨架参数：

- `SegmentThickness`：段厚度，只影响字符格内部几何，不影响测量尺寸。
- `SegmentGap`：段间隙，只影响字符格内部几何，不影响测量尺寸。
- `SegmentBevelRatio`：段端斜切比例，默认 `0.5`，会被 clamp 到 `0..1`。
- `DotScale`：冒号和小数点的点位尺寸比例，默认 `0.72`，会被 clamp 到 `0..1`。

这些属性都是视觉几何参数，变化时只应清理 geometry 缓存并触发重绘，不应触发 `InvalidateMeasure()`。

冒号和小数点已经进入统一几何管线：

```text
Colon/Dot slot
  -> SegmentGeometryFactory.CreateColon/CreateDot
  -> cached Geometry
  -> DrawGeometry
```

不要在 `Render` 中直接 `DrawEllipse` 绘制符号点位。否则符号和普通 segment 字符会走两套渲染路径，后续 Glow、高光、缓存和测试都会分裂。

## C# 中的最终绘图对象

Segment 的最终几何对象是 Avalonia 的 `Geometry`：

```csharp
using Avalonia.Media;

Geometry geometry = ...;
drawingContext.DrawGeometry(activeBrush, null, geometry);
```

常用具体类型包括：

- `StreamGeometry`
- `PathGeometry`
- `GeometryGroup`
- `EllipseGeometry`

Segment 主路径建议用 `StreamGeometry` 或其它可缓存的 `Geometry` 表达段形状。冒号和小数点可以用 `EllipseGeometry`，也可以直接调用 `DrawingContext.DrawEllipse(...)`，但如果要统一绘制管线，仍可包装成几何对象。

最终提交给 Avalonia 的核心组合是：

```text
Brush + Geometry
```

需要描边时才使用：

```text
Brush + Pen + Geometry
```

## 绘制顺序

推荐绘制顺序：

```text
1. 可选背景
2. 暗段
3. 可选 Glow 层
4. 亮段主体
5. 可选高光层
```

基础伪代码：

```csharp
foreach (var slot in layout.Slots)
{
    var geometrySet = geometryFactory.Create(slot.Bounds, options);

    if (showInactiveSegments)
    {
        foreach (var item in geometrySet.Items)
        {
            drawingContext.DrawGeometry(inactiveBrush, null, item.Geometry);
        }
    }

    foreach (var item in geometrySet.Items)
    {
        if ((slot.Pattern.Parts & item.Part) != 0)
        {
            drawingContext.DrawGeometry(activeBrush, null, item.Geometry);
        }
    }
}
```

暗段用于表达未点亮但仍可见的 LED 轮廓。没有暗段时，控件更像普通矢量图形，不像真实设备面板。

第一版不做真实 blur 发光。当前 Glow 是一个可选半透明预绘制层：

- `GlowBrush = null` 时完全关闭，这是默认状态。
- `GlowOpacity` 默认 `0.35`，但只有 `GlowBrush` 存在时才生效。
- Glow 使用同一份 cached `Geometry`，不会因为开启 Glow 生成另一套几何。
- `GlowBrush` 和 `GlowOpacity` 只影响绘制，不进入 geometry cache key。

这不是最终真实 LED 光晕模型，只是最低风险的视觉层次能力。后续如果做 blur、外扩光晕或材质效果，必须重新审查性能、缓存和边界测试。

当前第一版已经落地的绘制语义：

- 背景如果存在，先绘制背景。
- `ActiveBrush` 为 `null` 时，只绘制背景，不绘制亮段、暗段、冒号或小数点。
- 普通十四段字符在 `ShowInactiveSegments = true` 且 `InactiveBrush` 不为空时，先绘制完整 14 个暗段，再绘制当前字符的亮段。
- `ShowInactiveSegments = false` 时，普通十四段字符只绘制当前字符的亮段。
- 冒号和小数点不走 14 段暗段逻辑，只绘制对应点位。
- 冒号和小数点使用 cached `Geometry` 绘制，不再直接 `DrawEllipse`。
- `GlowBrush` 存在且 `GlowOpacity > 0` 时，亮段和符号点位会先绘制一层 Glow，再绘制亮段主体。

这些语义已经通过 `SegmentDisplayRenderTests` 固定。

## 数值规整策略

Segment 第一版采用“显示控件不因非法输入崩溃”的策略。所有用户输入的数值型属性在进入布局、几何或绘制前都会规整。

当前内部由 `SegmentValueSanitizer` 统一处理：

- `NaN` 视为最小值。
- `Infinity` 视为最小值或被 clamp 到范围内。
- 负数按 0 处理，带最小值的属性按最小值处理。
- `Padding` 的四个方向分别规整为非负有限数。
- `CharacterAspectRatio` 最小值为 `0.1`。
- 渲染阶段 `SegmentThickness` 最小值为 `1`，随后在 `SegmentGeometryFactory` 中按字符格尺寸继续 clamp。
- `SegmentGap` 最小值为 `0`，随后在 `SegmentGeometryFactory` 中按字符格尺寸继续 clamp。
- `SegmentBevelRatio` clamp 到 `0..1`。
- `DotScale` clamp 到 `0..1`。
- `GlowOpacity` clamp 到 `0..1`。

几何工厂的边界策略：

- 非正尺寸或非有限 bounds 返回包含 14 个空 `Geometry` 的 `SegmentGeometrySet`，保持结构稳定。
- 极小字符格、过大 thickness、过大 gap 都会被 clamp，生成的几何不会超出字符格 bounds。

## 属性失效

属性变化必须触发正确的失效路径：

| 属性类型 | 示例 | 处理 |
|---|---|---|
| 文本和字符布局 | `Text`、字符间距、Padding | `InvalidateMeasure()` + `InvalidateVisual()` |
| 几何参数 | 段厚度、段间隙、斜切比例 | 清几何缓存 + `InvalidateVisual()` |
| 颜色参数 | 亮段画刷、暗段画刷、背景画刷 | `InvalidateVisual()` |
| 绘制开关 | 是否显示暗段、是否显示发光层 | `InvalidateVisual()` |
| 绘制变换 | 内容对齐、溢出策略 | `InvalidateVisual()` |

不要把所有属性变化都粗暴当成重新布局，也不要让几何参数变化只触发重绘。

## 缓存策略

第一版不做全局缓存，但 `SegmentDisplay` 内部已经维护当前实例级缓存，避免同一控件在相同 layout 和几何参数下重复生成 `StreamGeometry`。

可缓存内容：

- 字符规范化结果。
- 字符到 `SegmentCharacterPattern` 的映射结果。
- 给定字符格尺寸和几何参数下的十四段 `Geometry`。

当前控件缓存分两层：

- layout 缓存：`Text`、`CharacterHeight`、`CharacterAspectRatio`、`CharacterSpacing`、`Padding`、最终 `Bounds.Size`。
- geometry 缓存：slot 数量、每个 slot 的字符类型和 bounds，以及 `SegmentThickness`、`SegmentGap`、`SegmentBevelRatio`、`DotScale`。

geometry 缓存不能直接包含 `Text`。数字和字母的字符内容决定哪些段点亮，但不改变同一个字符格中的十四段骨架。例如 `"12" -> "34"` 必须重新映射字符和计算 layout，却可以复用原来的 Geometry。Render 必须使用当前 layout 中的 `SegmentCharacterPattern` 选择亮段，不能把旧 pattern 和 cached Geometry 捆绑保存。

以下变化属于 geometry topology 变化，必须重新生成：

- slot 数量变化。
- `Segments`、`Colon`、`Dot`、`Empty` 之间发生类型变化。
- 任意 slot 的 bounds 变化。
- 任意几何参数变化。

颜色不应该进入几何缓存 key。颜色变化只需要重新绘制，不需要重建几何。

当前失效规则：

- `Text` 变化：清 layout 缓存；geometry 缓存保留为复用候选，Render 阶段按 topology 严格匹配。
- 字符尺寸、字符间距、`Padding` 变化：清 layout 缓存和 geometry 缓存。
- `SegmentThickness`、`SegmentGap`、`SegmentBevelRatio`、`DotScale` 变化：只清 geometry 缓存。
- `ActiveBrush`、`InactiveBrush`、`GlowBrush`、`GlowOpacity`、`Background`、`ShowInactiveSegments` 变化：只触发重绘，不清 geometry 缓存。
- `HorizontalContentAlignment`、`VerticalContentAlignment`、`OverflowMode` 变化：只触发重绘，不清 geometry 缓存。

这意味着固定字符结构的时钟、计数器等高频文本更新只重建必要的字符映射和 layout，不重复生成十四段 Geometry；单纯颜色变化也不会重建几何。闪烁分隔符如果在 `Colon` 和空格之间切换，会改变 slot 类型和宽度，因此仍然必须重建。

对齐和溢出策略不进入 geometry 缓存 key。它们只改变绘制阶段的 `Transform`，不改变字符 slot、段位映射和单个字符格内的几何形状。

时钟、计数器、冒号闪烁不是 `SegmentDisplay` 的职责。它们应该通过外部 `DispatcherTimer`、ViewModel 或组合控件更新 `Text`，基础显示控件只负责显示当前字符串。

## 主题边界

Segment 可以使用 AtomUI 基础设施：

- `AtomUI.Core` 的 ThemeManager。
- Shared Token。
- 资源绑定能力。
- `AtomUI.Generator`。

Segment 不得使用 AtomUI 已经成型的控件包：

- `AtomUI.Controls`
- `AtomUI.Desktop.Controls`
- `AtomUI.Desktop.Controls.Extras`
- `AtomUI.Desktop.Controls.DataGrid`
- `AtomUI.Desktop.Controls.ColorPicker`

渲染层不要直接查询 token。正确关系是：

```text
Shared Token
  -> 主题 Setter 或 StyledProperty 默认值
  -> 控件属性
  -> Render 读取最终属性值
```

## 第一版边界

第一版应该做：

- 静态十四段字符显示。
- 数字、`A-Z`、冒号、小数点、负号、空格。
- 小写输入转大写。
- 自绘。
- 可测量。
- 可缩放。
- 可对齐。
- 小空间下可裁剪或显式等比缩小。
- 可主题化。
- 基础段形态配置。
- 冒号和小数点统一几何缓存。
- 最小 Glow 绘制层。

第一版不做：

- 普通字体模拟 LED。
- 点阵显示。
- 滚动字幕。
- 内置闪烁和复杂动画。
- 真实 blur 光晕和复杂材质。
- 多行文本。
- 中文和复杂脚本。
- 富文本。
- 硬件 LED 控制。
- 依赖 AtomUI 成型控件包。

## 可测试性

Segment 不能只靠手动看 sample。

应优先测试：

- 字符规范化，例如 `"abc"` 变成 `"ABC"`。
- 字符映射，例如 `8` 包含预期段，`-` 只包含中段。
- 不支持字符按空格处理。
- 布局测量，例如 `"12:45"` 的 slot 数量和窄字符宽度符合预期。
- 属性失效路径，例如 `Text` 改变触发布局和重绘，画刷改变只触发重绘。

几何像素级测试可以后置，但映射和布局必须可测试。

当前测试覆盖：

- `LEDCharacterNormalizerTests`：ASCII 小写转大写。
- `SegmentCharacterMapTests`：数字、`A-Z`、符号、冒号、小数点、未知字符 fallback。
- `SegmentLayoutEngineTests`：slot 数量、窄符号宽度、padding、spacing、最终高度、非法数值规整。
- `SegmentGeometryFactoryTests`：14 段完整性、bounds 内几何、极端 thickness/gap、非正 bounds、非有限选项。
- `SegmentDisplayContractTests`：StyledProperty 名称、默认值、CLR wrapper、内容对齐和溢出策略。
- `SegmentDisplayMeasureTests`：真实控件测量、padding、非法数值、厚度和间隙不影响 DesiredSize。
- `SegmentDisplayRenderTests`：基础 render 不抛异常、暗段/亮段绘制数量、冒号/小数点绘制语义、`ActiveBrush = null` 语义、Glow 绘制语义、同 topology 文本更新复用几何、topology 或几何参数变化刷新几何、Glow 参数变化不刷新几何、内容对齐或溢出策略变化不刷新几何、`ScaleDown` 产生绘制变换。
- `SegmentDisplayAutomationTests`：只读 Text 自动化类型、规范化后的自动化名称、显式自动化名称优先级和动态文本同步。
- 小数逻辑尺寸测试：非整数 bounds、段厚度和间隙下，几何保持有限并位于字符格范围内；DPI 栅格化仍由 Avalonia 负责。
- 高频更新测试：连续 2000 次固定四位数字更新必须重建 layout、复用 geometry，并在随后发生 topology 或几何参数变化时正确失效。

性能回归场景和验收矩阵见 [segment-performance-regression.md](segment-performance-regression.md)。

真实空间Glow的三条候选路线、统一属性约束和选型标准见 [glow-technical-options.md](glow-technical-options.md)。Segment当前同形叠色Glow属于历史现状，不代表选型已经完成。
