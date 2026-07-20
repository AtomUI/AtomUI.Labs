# LED Matrix 工业级实现原理

> 文档状态：迁移参考。本文于 2026-07-20 从 AtomUI 仓库的 `dev-and-mark/modules/desktop-controls-labs` 复制到 AtomUI.Labs 并适配文档结构。`AtomUI.Labs.Controls.LED` 表示本仓库的目标设计；文中的“已实现”、验证数据及旧项目命令来自迁移前的 `AtomUI.Desktop.Controls.Labs` 参考实现，不表示当前仓库已经包含相应源码、测试或性能工具。

本文记录 `AtomUI.Labs.Controls.LED.Matrix` 的目标实现设计。Matrix 是 LED 家族中的点阵屏路线，不是字体控件，不是 Segment 的升级版，也不是硬件 LED 控制器。

第一版公共控件类型固定为 `MatrixDisplay`。第一版只实现单行静态 `5x7` 等宽点阵文本。

## 核心链路

```text
原始 Text
  -> Unicode Rune 遍历和 ASCII 字符规范化
  -> 5x7 字模映射和 fallback
  -> 字符格布局测量
  -> 最终空间下的对齐或缩小变换
  -> 查询或生成字模亮暗 Geometry
  -> DrawingContext 按字模批量绘制
  -> Avalonia 底层渲染
```

每一层只承担自己的职责：

- 字符规范化只处理 ASCII 小写转大写。
- 字模映射只回答一个字符对应哪些点位。
- 布局只计算字符格原点和整体理想尺寸。
- 几何工厂把35个点位互斥分流到亮点和暗点 Geometry。
- 绘制按可见字符提交每字模最多两个 Geometry 命令。
- Avalonia 负责抗锯齿、DPI、render scale 和底层平台渲染。

字符判断、bit 解析、坐标计算和 `DrawingContext` 调用不得混成一组字符专用分支。

## 工程目录

Matrix 按当前真实职责组织：

```text
LED/Matrix/
  MatrixDisplay.cs
  MatrixDisplayAutomationPeer.cs
  MatrixDotShape.cs
  MatrixOverflowMode.cs
  MatrixValueSanitizer.cs
  Character/
    MatrixCharacterMap.cs
    MatrixCharacterPattern.cs
    MatrixFiveBySevenGlyphMap.cs
    MatrixGlyph.cs
  Layout/
    MatrixDisplayLayout.cs
    MatrixGlyphSlot.cs
    MatrixLayoutEngine.cs
    MatrixLayoutOptions.cs
  Rendering/
    MatrixGlyphGeometry.cs
    MatrixGlyphGeometryCacheKey.cs
    MatrixGlyphGeometryFactory.cs
    MatrixDotShapeResolver.cs
    MatrixPanelBorderGeometryFactory.cs
  Themes/
    MatrixThemes.axaml
    MatrixDisplayTheme.axaml
```

`MatrixDisplay.cs` 保留完整公共契约、生命周期、`MeasureOverride`、`Render` 和属性失效入口。内部字模和布局进入对应稳定职责目录。

主题聚合链固定为：

```text
AtomUILabsThemesProvider.axaml
  -> LED/Themes/LEDThemes.axaml
      -> LED/Matrix/Themes/MatrixThemes.axaml
          -> LED/Matrix/Themes/MatrixDisplayTheme.axaml
```

第一版不创建 Provider、FontSet、Primitives 或 Shared 目录。`Rendering/` 是性能基线证明逐点命令提交成本后形成的真实职责边界，不是为了机械复制 Segment。

## 5x7 的含义

一个字符使用横向 5 列、纵向 7 行的点阵字符格，共 35 个逻辑点位。例如 `A`：

```text
01110
10001
10001
11111
10001
10001
10001
```

`1` 表示亮点，`0` 表示暗点。多个字符由多个 `5x7` 字符格横向排列，并在字符格之间加入 `CharacterSpacing`。

必须区分两个概念：

```text
Glyph Resolution
  固定为 5 列 x 7 行，属于字模数据合同。

DotSize
  每个点轮廓外接框的边长，单位是DIP；Circle下同时是圆直径。
```

用户可以配置 `DotSize` 和间距，但不能通过 `GlyphWidth`、`GlyphHeight` 把 `5x7` 字模改成另一种规格。第一版不公开这两个属性。

## 字符合同

第一版内置 45 个字模：

```text
ABCDEFGHIJKLMNOPQRSTUVWXYZ
0123456789
空格
? - . : _ + = /
```

规则固定为：

```text
a-z              -> A-Z
受支持字符        -> 对应字模
每个不受支持的 Unicode 标量 -> 一个 ?
每个非法 UTF-16 代理项      -> 一个 ?
null 或空字符串   -> 没有字符格
```

`?` 是字模集的强制成员，因此未知字符 fallback 不需要运行时寻找第二候选。空格使用 35 个全灭点位，但仍占据一个完整等宽字符格。

输入遍历以 `System.Text.Rune` 为边界，不以 UTF-16 `char` 数量作为显示字符数量。例如 `A😀Z` 必须形成 `A?Z` 三个字符格，不能把一个补充平面字符拆成两个 fallback。布局槽位、自动化名称和实际绘制必须复用同一 Rune 映射合同。

第一版不支持：

- 中文、CJK 和复杂脚本。
- 小写字母独立字形。
- 多行文本和自动换行。
- 滚动字幕、闪烁和复杂动画。

## 字模数据结构

固定 `5x7` 只有 35 bit，使用单个 `ulong` 表达完整字模：

```csharp
internal readonly record struct MatrixGlyph(ulong Bits)
{
    public bool IsActive(int row, int column)
    {
        var bitIndex = row * 5 + column;
        return ((Bits >> bitIndex) & 1UL) != 0;
    }
}
```

bit 顺序必须固定：

```text
左上角       = bit 0
第一行最右侧 = bit 4
第二行最左侧 = bit 5
右下角       = bit 34
```

也就是严格使用 row-major 索引：

```text
bitIndex = row * 5 + column
```

不要使用 `ReadOnlyMemory<ushort>` 或每次查询时创建行数组。`ulong` 是不可变值，具备稳定的值相等性，也不会为每次字模查询分配数组。

字模常量应由可读的内部构造辅助在静态初始化阶段打包，绘制热路径只读取 `Bits`。

## 字模映射边界

第一版只有一套固定字模，不创建 `IMatrixGlyphProvider`。字模知识由内部静态映射拥有：

```csharp
internal static class MatrixFiveBySevenGlyphMap
{
    public const int Width = 5;
    public const int Height = 7;

    public static bool TryGetGlyph(char character, out MatrixGlyph glyph)
    {
        // Supported normalized character -> fixed 5x7 glyph
    }
}
```

`MatrixFiveBySevenGlyphMap` 只拥有 45 个受支持字符的原始 bit 数据，不处理大小写或 fallback。`MatrixCharacterMap` 负责统一语义：

```csharp
internal static class MatrixCharacterMap
{
    public static MatrixCharacterPattern GetPattern(Rune rune)
    {
        // Normalize supported BMP ASCII lowercase
        // Query MatrixFiveBySevenGlyphMap
        // Fallback once per Unicode scalar
    }
}
```

`MatrixFiveBySevenGlyphMap` 仍以 `char` 查询固定 ASCII 字模；Rune 到固定字模的规范化和 fallback 属于 `MatrixCharacterMap`。不得让布局和自动化各自处理代理项。

映射结果固定为：

```csharp
internal readonly record struct MatrixCharacterPattern(
    char Character,
    MatrixGlyph Glyph);
```

Provider 不属于 Matrix MVP 的内部或公开合同。若新增其它真实字模来源，必须单独设计以下边界后才能引入：

- Provider 是否公开给开发者。
- 字模数据是否固定分辨率。
- 数据所有权、不可变性和值相等性。
- 缺字 fallback、Provider identity 和缓存失效。
- 大型资源的加载、生命周期、AOT 和发布体积。

不得仅为了模式完整而创建“一个接口加唯一实现”。

## 公共 API

`MatrixDisplay` 第一版公开以下 StyledProperty：

| 属性 | 类型 | 代码默认值 | 作用 |
|---|---|---:|---|
| `Text` | `string?` | `null` | 待显示的单行文本 |
| `DotSize` | `double` | `6` | 圆点直径，单位 DIP |
| `DotSpacing` | `double` | `2` | 字符格内部点间距 |
| `DotShape` | `MatrixDotShape` | `Circle` | Circle、Square或RoundedSquare点轮廓 |
| `DotCornerRadiusRatio` | `double` | `0.25` | RoundedSquare圆角半径相对DotSize的比例 |
| `CharacterSpacing` | `double` | `8` | 相邻字符格间距 |
| `Padding` | `Thickness` | `0` | 内容内边距 |
| `HorizontalContentAlignment` | `HorizontalAlignment` | `Left` | 多余水平空间中的内容位置 |
| `VerticalContentAlignment` | `VerticalAlignment` | `Top` | 多余垂直空间中的内容位置 |
| `OverflowMode` | `MatrixOverflowMode` | `Clip` | 小空间处理策略 |
| `Background` | `IBrush?` | `null` | 控件背景 |
| `BorderBrush` | `IBrush?` | `null` | 可选面板边框画刷 |
| `BorderThickness` | `Thickness` | `0` | 面板四边边框厚度 |
| `CornerRadius` | `CornerRadius` | `0` | 背景圆角 |
| `ActiveBrush` | `IBrush?` | `null` | 亮点画刷 |
| `InactiveBrush` | `IBrush?` | `null` | 暗点画刷 |
| `ShowInactiveDots` | `bool` | `true` | 是否绘制熄灭点位 |

MVP第一版固定圆点。V2以增量合同加入`MatrixDotShape`和`DotCornerRadiusRatio`；仍不公开Glow、Provider、FontSet、`GlyphWidth`或`GlyphHeight`。

后续Glow增量已经落地，新增`GlowBrush`、`GlowOpacity`和`GlowRadius`，默认`GlowBrush=null`，因此不改变基础显示。正式算法、范围和测试结论以[LED Glow技术路线选型](glow-technical-options.md)及[LED Glow原型评估](glow-prototype-evaluation.md)为准；本段中“仍不公开Glow”只描述MVP历史边界，不再代表当前API。

`MatrixOverflowMode` 只包含：

```csharp
public enum MatrixOverflowMode
{
    Clip,
    ScaleDown
}
```

Matrix 和 Segment 是独立路线。Matrix 不引用 `SegmentOverflowMode`，也不为了消除两个枚举而修改 Segment 的既有公共类型。

## 数值规整

用户输入在进入测量和绘制前统一规整：

- `DotSize`：非有限值或小于 `1` 时使用 `1`。
- `DotSpacing`：非有限值或负数时使用 `0`。
- `CharacterSpacing`：非有限值或负数时使用 `0`。
- `Padding`：四个方向分别规整为非负有限值。
- `BorderThickness`：四个方向分别规整为非负有限值；原始StyledProperty值不回写。
- `CornerRadius`：四角分别规整为非负有限值，重叠半径按最终Bounds等比收敛。
- 单项布局参数的内部有效值上限为 `1,000,000 DIP`，防止 `double.MaxValue` 等有限超大值在尺寸公式和绘制坐标中溢出。
- 缩放比例：限制到 `0..1`，绝不放大。

规整只影响内部有效值，不回写 StyledProperty，避免破坏 binding 和属性优先级。

`MatrixValueSanitizer` 第一版保留在 Matrix 根目录。虽然它和 `SegmentValueSanitizer` 存在相似数学规则，但在两条路线真正形成稳定重复前，不提前提升到 LED 家族公共抽象。

## 布局测量

布局输入：

- 原始 `Text`。
- 规整后的 `DotSize`、`DotSpacing`、`CharacterSpacing` 和 `Padding`。
- 固定 `5x7` 字模映射。

单个字符格理想尺寸：

```text
glyphWidth  = 5 * DotSize + 4 * DotSpacing
glyphHeight = 7 * DotSize + 6 * DotSpacing
```

全部尺寸均为 Avalonia DIP，不是物理屏幕像素。

布局输出：

```csharp
internal readonly record struct MatrixGlyphSlot(
    MatrixCharacterPattern Pattern,
    Point Origin);

internal sealed class MatrixDisplayLayout
{
    public Size DesiredSize { get; }
    public Size GlyphSize { get; }
    public IReadOnlyList<MatrixGlyphSlot> Slots { get; }
}
```

`GlyphSize` 是布局引擎根据有效 `DotSize` 和 `DotSpacing` 计算出的单字符几何尺寸。Render 必须复用该结果，不得再次维护一套字符宽高公式。

布局算法独立：

```csharp
internal static class MatrixLayoutEngine
{
    public static MatrixDisplayLayout Calculate(
        string? text,
        MatrixLayoutOptions options)
    {
        // text + fixed glyph map + options -> slots + desired size
    }
}
```

`MeasureOverride` 返回同一布局算法产生的 `DesiredSize`。Matrix 没有子控件，不需要在 `ArrangeOverride` 生成点位或字模数据。

## 最终空间、对齐与溢出

Matrix 的理想点尺寸由 `DotSize` 决定，不因最终 Bounds 自动改变。

`Render` 必须先得到理想布局，再根据最终空间处理：

```text
Render
  -> 绘制 Bounds 内背景
  -> PushClip 到控件 Bounds
  -> 根据 OverflowMode 计算 scale
  -> 根据内容对齐计算 offset
  -> PushTransform
  -> 绘制当前字符点阵
```

规则固定为：

- `Clip`：保持理想尺寸，超出控件 Bounds 的内容被裁剪。
- `ScaleDown`：当空间不足时整体等比缩小到 Bounds 内；空间充足时 scale 保持 `1`。
- `Left` / `Top`：额外空间偏移为 `0`。
- `Center`：额外空间偏移一半。
- `Right` / `Bottom`：使用全部额外空间作为偏移。
- `Stretch`：点阵内容不拉伸，按 Center 处理。

不能默认偷偷缩小，也不能让绘制越过控件 Bounds 污染相邻视觉。

ScaleDown比例和内容对齐偏移由LED家族根目录的`LEDDisplayLayoutMath`计算。Matrix仍自行判断`MatrixOverflowMode`，并保留可见视口逆变换和字符剔除逻辑；共享工具不参与layout、Geometry或Render命令提交。

面板边框采用`Border + Padding + Glyph` Box Model。Measure在原内容DesiredSize外增加四边有效BorderThickness。Render按Background、内侧内容视口、Border顺序提交；内容对齐、裁剪和ScaleDown以Border内侧视口为边界，边框自身保持DIP厚度。Background铺满外框，因此半透明BorderBrush会与背景混色。

## 点位几何与绘制

Matrix 不创建 `MatrixDotSlot[]`。`MatrixGlyphGeometryFactory` 在几何缓存未命中时使用固定的7行、5列循环，把35个点位互斥分流到两个`StreamGeometry`，并在同一个循环中根据有效DotShape追加圆、方或圆角方子路径：

```text
dotX = column * (DotSize + DotSpacing)
dotY = row    * (DotSize + DotSpacing)
dotBounds = Rect(dotX, dotY, DotSize, DotSize)
```

每个点是`StreamGeometry`中独立闭合的子路径，不连接相邻点。Circle保留原有双ArcTo路径，Square使用直边闭合路径，RoundedSquare使用直边和四段圆角弧。亮暗点在同一个`if/else`中分类，满足：

```text
ActiveDotCount + InactiveDotCount = 35
ActiveDotCount = PopCount(Glyph.Bits)
```

Render 复用几何，并通过槽位平移定位字符：

```csharp
foreach (var slot in layout.Slots)
{
    var geometry = GetGlyphGeometry(slot.Pattern.Glyph, options);
    using (drawingContext.PushTransform(CreateTranslation(slot.Origin)))
    {
        if (geometry.ActiveDotCount > 0)
        {
            drawingContext.DrawGeometry(activeBrush, null, geometry.ActiveGeometry);
        }
        if (ShowInactiveDots && inactiveBrush is not null && geometry.InactiveDotCount > 0)
        {
            drawingContext.DrawGeometry(inactiveBrush, null, geometry.InactiveGeometry);
        }
    }
}
```

完整绘制顺序：

```text
1. 可选背景
2. 当前字模的亮点或暗点
```

明确语义：

- `ActiveBrush = null` 时只绘制背景，不绘制亮点或暗点。
- `ShowInactiveDots = false` 时只绘制亮点。
- `InactiveBrush = null` 时不绘制暗点。
- 亮点位置不先绘制暗点底层，避免半透明画刷发生隐式混色。
- 背景为空时仍然正常绘制点阵。

## 属性失效

| 属性 | 布局缓存 | 字模几何缓存 | Avalonia 失效 |
|---|---|---|---|
| `Text` | 清理 | 保留 | Measure + Render |
| `DotSize`、`DotSpacing` | 清理 | 清理 | Measure + Render |
| `DotShape` | 保留 | 清理 | Render |
| RoundedSquare下`DotCornerRadiusRatio` | 保留 | 清理 | Render |
| Circle/Square下`DotCornerRadiusRatio` | 保留 | 保留 | Render |
| `CharacterSpacing`、`Padding` | 清理 | 保留 | Measure + Render |
| 内容对齐、`OverflowMode` | 保留 | 保留 | Render |
| `Background`、`CornerRadius` | 保留 | 保留 | Render |
| `BorderBrush` | 保留 | 保留 | Render |
| `BorderThickness` | 保留 | 保留 | Measure + Render |
| `ActiveBrush`、`InactiveBrush` | 保留 | 保留 | Render |
| `ShowInactiveDots` | 保留 | 保留 | Render |

颜色、圆角、暗点开关、内容对齐和溢出策略不得进入 layout cache key。

## 缓存策略

Matrix 使用单实例 layout 缓存和有界字模几何缓存，不做全局缓存：

```text
Layout cache key
  = Text
  + 有效 DotSize
  + 有效 DotSpacing
  + 有效 CharacterSpacing
  + 有效 Padding
```

缓存内容是当前 `MatrixDisplayLayout`。相同输入的重复 Render 复用布局；文本或布局参数变化后重建。

字模几何缓存键固定为：

```text
Geometry cache key
  = Glyph.Bits
  + 有效 DotSize
  + 有效 DotSpacing
```

缓存值包含一个亮点`StreamGeometry`、一个暗点`StreamGeometry`和两类点数。缓存键包含字模Bits、有效DotSize、DotSpacing、DotShape和DotCornerRadiusRatio。Circle/Square的有效圆角比例固定为0。`Text`只选择和摆放字模，不改变字模内部形状，因此动态文本不得清理几何缓存；画刷、Padding、对齐和溢出模式同样不得进入几何键。

几何缓存属于 `MatrixDisplay` 实例，随控件一起释放。当前固定字模集使缓存最多覆盖45种字模形状；`DotSize` 或 `DotSpacing` 变化时立即清空旧几何，避免不同尺寸历史无限增长。

生命周期验收必须证明：

- 输入全部支持字符后，缓存数量不超过固定字模形状数量；后续历史 Text 不增加上界。
- 连续修改 `DotSize`、`DotSpacing` 时，缓存只保留当前参数对应的几何。
- 清理缓存后，旧 Geometry 在没有其它引用时可被 GC。
- `MatrixDisplay` 失去外部引用后，控件和实例缓存整体可被 GC；不得引入静态 Geometry 字典或事件订阅。

layout 缓存只能保留当前一份布局，不维护随文本增长的历史集合。几何缓存只按固定字模形状复用，不按字符槽位或历史 Text 缓存。

面板边框另有单实例复杂Geometry缓存，键为最终Bounds、有效BorderThickness和CornerRadius。均匀且未吞没内框的边框走缓存Pen；非均匀或过厚边框走外内Geometry排除。画刷变化复用Geometry，Thickness、CornerRadius或Bounds变化替换旧Geometry，历史外壳不得累积。

## 可见字符剔除

`PushClip` 只保证最终像素不越过控件边界，不能代替 CPU 侧的绘制剔除。若 `Clip` 模式仍向 Avalonia 提交屏外字符的全部圆点，长文本会产生无效绘制调用。

当前布局槽位按 X 坐标递增且字符等宽。Render 将控件视口逆变换到布局坐标后：

1. 先判断单行字符在垂直方向是否与视口相交；
2. 使用二分查找定位第一个右边界超过视口左边界的槽位；
3. 从该槽位向右绘制，到槽位左边界到达视口右边界时停止。

稳定布局下，字符选择复杂度为 `O(log n + visible)`，其中 `visible` 是实际与视口相交的字符数量。`ScaleDown` 将完整内容缩入视口时，全部字符仍然必须参与绘制，不能错误剔除。

零宽或零高 Bounds 不提交圆点绘制。逻辑剔除之后仍保留 `PushClip`，前者负责避免无效工作，后者负责最终像素边界正确性，二者职责不同。

## 自动化与可访问性

点阵只是视觉表达，辅助功能系统必须能读取实际显示文本。

`MatrixDisplayAutomationPeer` 规则：

- AutomationControlType 为只读 `Text`。
- ClassName 为 `MatrixDisplay`。
- 默认 Name 为规范化和 fallback 后的实际显示文本。
- 开发者显式设置的 `AutomationProperties.Name` 优先，包括显式空字符串。
- `Text` 动态变化时，已创建的 peer 发出 Name 属性变化通知。
- 如果新旧输入映射为相同显示文本，不发送无效通知。

自动化文本必须复用 `MatrixCharacterMap` 的字符合同，不得维护第二套 fallback 逻辑。

## 主题与 Token

Matrix 可以使用 AtomUI 基础设施：

- `AtomUI.Core` 的 ThemeManager、Shared Token 和资源能力。
- `AtomUI.Generator`。

Matrix 不得使用 AtomUI 已经成型的控件包：

- `AtomUI.Controls`
- `AtomUI.Desktop.Controls`
- `AtomUI.Desktop.Controls.Extras`
- `AtomUI.Desktop.Controls.DataGrid`
- `AtomUI.Desktop.Controls.ColorPicker`

第一版不创建 `MatrixToken.cs`。主题通过 Setter 为下列属性接入 Shared Token 默认值：

- `Background` -> `ColorBgContainer`
- `CornerRadius` -> `BorderRadiusLG`
- `Padding` -> `PaddingLG`
- `ActiveBrush` -> `ColorPrimary`
- `InactiveBrush` -> `ColorFillTertiary`

渲染层只读取 StyledProperty 的最终有效值，不直接查询 Token。

主题运行时合同：

- 未设置本地值时，ControlTheme Setter 必须解析为当前 Shared Token 值。
- 应用主题变化后，Shared Token 默认值必须动态更新。
- 开发者设置的 StyledProperty 本地值优先于主题 Setter，主题切换不得覆盖本地值。
- Matrix 是 Visual 控件，Token 资源绑定由 Avalonia 视觉资源宿主管理；不得为此引入全局 Token binding 或额外订阅。

## AXAML 使用合同

Labs 程序集通过 `https://atomui.net/labs` XML 命名空间公开 `MatrixDisplay`。验收必须包含真实编译 AXAML，而不能只通过 C# 构造器证明类型可用：

```xml
<labs:MatrixDisplay Text="axaml 2026"
                    DotSize="7"
                    HorizontalContentAlignment="Center" />
```

编译 AXAML 验收同时覆盖类型解析、StyledProperty 转换、ControlTheme 发现和 Shared Token 资源解析。

## AOT 边界

第一版字模通过显式静态映射注册，不扫描程序集、不反射发现 Provider、不使用字符串 binding，也不通过 `Activator` 创建字模来源。

字模、布局和自动化路径都使用编译期已知类型。Matrix MVP 不引入需要独立释放的 subscription、binding、timer、动态视觉或非 Visual 资源宿主。

真实发布验收使用 Labs Sample 的 Release NativeAOT 配置和专用 `LabsPublishAot` 开关，避免把全局 `PublishAot` 属性传播到 `AtomUI.Generator` Analyzer 项目。`win-x64` NativeAOT 已完成真实 publish；当前剩余 warning 来自 `AtomUI.Core/AppBuilderExtensions.cs` 的既有 Win32 反射配置路径，不来自 Matrix 或 Labs。

## 性能基线

Matrix 使用独立测量程序：

```text
tools/performances/AtomUI.Desktop.Controls.Labs.Performance
```

逐点基线位于 [matrix-performance-baseline.md](matrix-performance-baseline.md)，几何批处理结果位于 [matrix-performance-geometry-batch.md](matrix-performance-geometry-batch.md)，600帧常见动态负载位于 [matrix-performance-dynamic-load.md](matrix-performance-dynamic-load.md)，分配归因与36,000帧长稳结果位于 [matrix-performance-allocation-and-soak.md](matrix-performance-allocation-and-soak.md)。测量范围是 CPU 侧布局与 `DrawingGroup` 命令提交，不包含 GPU 或平台呈现成本，也不把机器相关毫秒数作为单元测试阈值。

当前结论：

- `Clip` 下1000和10000字符均只提交7个可见字模 Geometry 命令，保留二分剔除收益。
- 1000字符 `ScaleDown` 的 Geometry 命令从17000降至1000，本机单次 smoke 的分配约从16.7 MB降至2.83 MB，耗时约从39.6 ms降至4.79 ms。
- 时间数据属于同机单次 smoke，不作为跨机器速度承诺；命令数从每激活点一次降为每字模最多两次是稳定结构收益。
- 6、8、16字符分别在亮点单层和亮暗双层下运行600次固定长度更新；预热后所有场景新增 Geometry 数均为0，最终缓存稳定在10或15种形状。
- 动态负载每帧都按合同重建 layout，但 Geometry 命令数稳定为字符数乘以可见图层数，不随运行帧数增长。
- 16字符双层动态帧约为64.2 KB/帧，其中调用方字符串格式化约120 B，Matrix layout约672 B，固定layout下的绘制命令记录约63.4 KB；当前分配主要不在字模映射、layout或Geometry构建。
- 36,000帧预生成文本长稳测试中，Geometry新增为0、缓存保持`15 -> 15`、Gen2回收为0，强制完整回收后的存活托管内存没有增长；独立复跑得到相同的缓存、GC和存活内存结论。
- 每帧分配包含测量程序新建`DrawingGroup`的CPU命令记录对象，不能直接等同于真实平台渲染器或GPU呈现分配。当前没有证据支持为了约1%的layout分配引入更复杂的缓冲复用。

## NuGet 交付合同

Labs 包必须使用独立标题、描述、标签和 README，明确不保证 Ant Design 视觉一致性，也不要求安装 `AtomUI.Desktop.Controls`。net8/net10 包依赖只允许包含 `AtomUI.Core` 与 Avalonia，不得出现 AtomUI 成型控件包。

## 第一版边界

以下列表记录已冻结的MVP第一版。V2只增量加入静态DotShape系统，完整合同见[matrix-static-visual-system.md](matrix-static-visual-system.md)。

第一版必须完成：

- 单行静态 `5x7` 等宽点阵文本。
- 固定 45 个字模和未知字符 fallback。
- 固定圆点、亮暗互斥绘制。
- 可配置点尺寸、点间距、字符间距和 Padding。
- 背景、圆角、亮点画刷、暗点画刷和暗点开关。
- 水平/垂直内容对齐。
- `Clip` 和显式 `ScaleDown`。
- 自绘、测量、实例级 layout 缓存和自动化支持。
- Shared Token 主题默认值和 Labs sample。
- 可选的单向穿屏Marquee，最小契约见[LED Matrix Marquee最小契约](matrix-marquee-minimum-contract.md)。

第一版不做：

- Provider 接口或自定义字模来源。
- 多套字模规格或任意分辨率配置。
- 中文、CJK、复杂脚本和独立小写字形。
- 方点、圆角方点和点形状切换 API。
- Glow、扫描线、材质和复杂视觉效果。
- 除已冻结单向穿屏Marquee之外的滚动模式、闪烁、多行和自动换行。
- 图片点阵化或硬件 LED 控制。
- 依赖 AtomUI 成型控件包。

## 测试与 sample 验收

自动化测试至少覆盖：

- 全部 45 个字模均可查询，只有空格为全灭字模。
- `A` 等代表字符的 bit 方向正确，不发生左右或上下镜像。
- ASCII 小写转大写，未知字符 fallback 到 `?`。
- Emoji 等补充平面字符按一个 Rune 回退为一个 `?`；孤立代理项稳定回退且不抛异常。
- 固定种子的随机 UTF-16 输入保持映射文本、布局槽位和自动化语义一致。
- 单字符、多字符、空格和空文本的理想尺寸。
- `DotSize`、点间距、字符间距和 Padding 的尺寸公式。
- 负数、NaN、Infinity、`double.MaxValue`、零尺寸和极小 Bounds。
- 圆点总数、亮暗互斥、隐藏暗点和空画刷语义。
- 全部45个字模满足亮点数加暗点数等于35，亮点数等于字模 bit 的 PopCount。
- 每字模最多提交两个 Geometry 命令，重复字形复用缓存。
- 三种DotShape保持相同外接框、35点互斥分区和每字模最多两个Geometry命令。
- DotShape和RoundedSquare圆角比例正确失效并释放旧Geometry；Circle/Square忽略圆角比例缓存变化。
- `Text`、画刷、Padding 和对齐保留几何缓存；`DotSize`、`DotSpacing` 清理并重建。
- 全字模和历史文本缓存上界、参数抖动、旧几何释放和控件 WeakReference 回收。
- 6、8、16字符在亮点单层和亮暗双层下连续600帧更新后，缓存大小和命令数保持稳定。
- 背景和圆角绘制顺序。
- 大空间内容对齐、`Clip`、`ScaleDown` 和绝不放大。
- 重复 Render 复用 layout；文本和尺寸参数正确失效；颜色只重绘。
- 布局属性使 Measure 失效；纯视觉属性只使 Render 失效并保持 Measure 有效。
- Shared Token 默认值、Dark/Compact主题动态更新、StyledProperty本地值优先级，以及主题下显式`null`画刷覆盖。
- 负数、NaN和Infinity圆角输入不导致Render异常。
- BorderBrush与BorderThickness双条件绘制、四边独立测量、内侧视口对齐、ScaleDown不缩放边框，以及复杂边框缓存释放。
- 通过 Labs XML 命名空间在编译 AXAML 中创建 `MatrixDisplay`。
- 自动化名称、显式名称优先级和动态文本同步。
- `A`、`M`、`0`、`8`、`?`、冒号和空格的 Headless 像素语义基线。
- 100%、125%、150%、200% RenderScaling 下的物理内容边界。
- 非均匀圆角边框在100%、125%、150%、200% RenderScaling下保持单一连通区域，并按四边有效厚度落点。
- 像素层面的 Clip、ScaleDown、内容对齐和暗点开关。

Labs sample 至少展示：

- 大写字母和小写输入。
- 数字。
- `? - . : _ + = /` 全部符号。
- 未知字符 fallback。
- 默认主题、隐藏暗点、自定义亮暗颜色。
- 小点、大点、字符间距和 Padding。
- Circle、Square和两个不同圆角比例的RoundedSquare对比。
- 大容器中的不同内容对齐。
- 小空间下的 Clip 和 ScaleDown。
- 长文本裁剪、极小视口缩放和动态 Matrix 数字更新。
- 短文本穿屏、长公告穿屏以及Marquee与Glow组合。

视觉回归使用 Avalonia.Skia Headless 帧缓冲建立像素语义基线：代表字模的每个点必须形成独立连通区域，内容像素边界必须随 RenderScaling 成比例，裁剪区外不得出现亮点。测试不锁定整张 PNG 哈希，避免将 Skia 抗锯齿的非语义字节差异误判为控件回归。

像素测试和几何测试职责不同：像素测试验证最终栅格化结果；字模 bit 方向、布局尺寸、绘制数量、属性失效和自动化合同仍由结构化测试负责，不能只依赖截图或肉眼判断。
