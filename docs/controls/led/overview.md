# LED 控件家族设计

> 文档状态：当前架构与公共契约，更新于 2026-07-20。历史审计、原型和性能数据在导航中单独标识，不作为当前实现事实。

本文记录 `AtomUI.Labs.Controls.Led` 的组件域设计。LED 是 Labs 下的实验控件家族名，不是单一控件名。

## 文档导航

- Segment：[实现原理](segment-implementation.md)、[性能回归矩阵](segment-performance-regression.md)。
- Matrix：[实现原理](matrix-implementation.md)、[Marquee 最小契约](matrix-marquee-minimum-contract.md)；[静态视觉系统](matrix-static-visual-system.md)和[MVP 收口审计](matrix-mvp-audit.md)是历史设计/审计记录。
- Matrix 历史性能证据：[逐点基线](matrix-performance-baseline.md)、[几何批处理](matrix-performance-geometry-batch.md)、[动态负载](matrix-performance-dynamic-load.md)、[分配与长稳](matrix-performance-allocation-and-soak.md)。
- 家族增强与边界：[Glow 技术选型](glow-technical-options.md)记录当前技术决议；[公共边界审计](family-common-boundary-audit.md)和[Glow 原型评估](glow-prototype-evaluation.md)是历史证据。

## 定位

`AtomUI.Labs.Controls.Led` 用于承载 LED 风格显示控件。它不表示硬件 LED 控制器，也不表示普通文本控件。

LED 家族目标包含两条并列路线：

```text
AtomUI.Labs.Controls.Led
  Segment  十四段数码管路线
  Matrix   点阵屏路线
```

`Segment` 和 `Matrix` 不是超集关系，也不是升级关系。它们分别面向不同的显示模型：

- `Segment` 以“段”为最小视觉单元，适合数字、英文字母、仪表读数和电子设备面板风格。
- `Matrix` 以“点阵像素”为最小视觉单元，适合字符屏、公告屏、滚动文字和更自由的符号表达。

共享基础代码直接放在 `src/AtomUI.Labs.Controls.Led/` 根目录下，当前包括 `LedCharacterNormalizer` 和 `LedDisplayLayoutMath`，不创建 `Primitives`、`Shared` 或 `Internal` 等独立目录。

LED 家族主题必须采用聚合入口：

```text
LedThemesProvider.axaml
  -> Themes/LedThemes.axaml
      -> Segment/Themes/SegmentThemes.axaml
      -> Matrix/Themes/MatrixThemes.axaml
```

包级 `LedThemesProvider.axaml` 只引用 `Themes/LedThemes.axaml`，不直接引用某个子控件的最底层主题文件。

## 显示模型

LED 家族需要先区分三种常见显示模型：

| 模型 | 核心单元 | 适合内容 | 优点 | 缺点 | Labs 定位 |
|---|---|---|---|---|---|
| 七段 | 7 个发光段 | 数字、少量符号 | 简单、经典、计算量低 | 字母表现差，很多字符不可读 | 不作为独立第一路线，可作为 Segment 的简化能力评估 |
| 十四段 | 14 个发光段 | 数字、A-Z、常用符号 | 保留数码管风格，能覆盖英文字母 | 字符映射没有唯一标准，实现复杂度高于七段 | `Led.Segment` 的主要方向 |
| 点阵 | 点阵像素 | 文本、符号、滚动屏 | 表达能力强，可读性更可控 | 风格变成像素屏，需要字模系统 | `Led.Matrix` 的主要方向 |

第一阶段文档约定：

- `Segment` 使用十四段路线，目标是数字、英文字母和常用符号。
- `Matrix` 使用点阵路线，目标是字符屏和点阵文本表达。
- 二者共享输入、颜色、布局和渲染辅助思想，但不强行共享具体绘制算法。

## Segment 路线

`Led.Segment` 是十四段数码管路线。

工程核心：

```text
Text
  -> 字符规范化
  -> 字符到十四段映射
  -> 字符格布局
  -> 绘制亮段和暗段
```

第一阶段建议支持：

- 数字 `0-9`
- 大写字母 `A-Z`
- 小写输入统一转大写
- 冒号 `:`
- 小数点 `.`
- 负号 `-`
- 空格

十四段字符映射没有唯一行业标准。Labs 应在实现文档中明确自己的映射表，并把它视为实验视觉的一部分，不承诺与某一种硬件设备完全一致。

## Matrix 路线

`Led.Matrix` 是点阵屏路线。

工程核心：

```text
Text
  -> 字符规范化
  -> 字符到点阵字模
  -> 点阵网格布局
  -> 绘制亮点和暗点
```

Matrix 需要额外考虑：

- 当前固定 `5x7` 字模规格，不开放任意行列配置。
- 当前在相同点位外接框内支持 Circle、Square 和 RoundedSquare 静态轮廓，不改变字模和布局。
- 点尺寸、点间距、字符间距、Padding 和内容对齐。
- 小空间下的裁剪和显式等比缩小。

这些能力不应塞入 `Segment`。只有两条路线出现真实且稳定的重复后，才能评估 LED 家族根目录中的共享布局代码。

Matrix静态轮廓的完整合同见[matrix-static-visual-system.md](matrix-static-visual-system.md)。

## 共享基础代码

`Segment` 和 `Matrix` 可以共享 LED 家族内部基础代码，但第一版不可为了架构感过度抽象。共享代码直接放在包项目根目录；只有当真实重复稳定后，再评估是否增加 `Primitives`、`Shared` 或 `Internal` 等目录。

子控件内部目录应按稳定职责组织。`Segment` 当前采用：

```text
Segment/
  SegmentDisplay.cs
  SegmentValueSanitizer.cs
  Character/
  Layout/
  Rendering/
  Themes/
```

`Matrix` 采用同等工程标准，但不强行复刻 `Segment` 的目录名；按点阵路线自己的稳定职责拆分。

当前已经共享的内容：

- `LedCharacterNormalizer`：ASCII小写转大写。
- `LedDisplayLayoutMath`：ScaleDown比例和内容对齐偏移纯计算。

已经统一行为但不提取代码的内容：

- 文本布局框架思想，但不强行共用同一个 LayoutEngine。
- 测量结果结构命名习惯，例如都包含 DesiredSize 和 Slots。
- 内容对齐和溢出策略的行为约定，但不强行共享枚举类型。
- Shared Token默认值接入模式，但每个ControlTheme保持独立。
- 亮暗画刷、暗态开关、字符间距和Padding的属性语义，但不引入公共控件基类。

第一版不应该共享的内容：

- 十四段的段位映射。
- `SegmentParts`。
- `SegmentCharacterMap`。
- `SegmentGeometryFactory`。
- `MatrixGlyph`。
- `MatrixFiveBySevenGlyphMap`。
- 点阵的字模数据。
- Matrix dot 绘制逻辑。
- 两条路线各自的几何生成细节。

原则上，第一版只共享输入、状态、选项和少量数学辅助；不共享字符映射、字模、几何生成和具体绘制。公共抽象必须来自真实重复，不能先设计一个看起来通用但掩盖路线差异的 `LedLayoutEngine`、`LedRenderer` 或 `LedGeometryFactory`。

公共边界的逐项审计结论见[family-common-boundary-audit.md](family-common-boundary-audit.md)。

## 渲染方案

LED 家族实现前需要评估三种渲染方案：

| 方案 | 做法 | 优点 | 缺点 | 建议 |
|---|---|---|---|---|
| 自绘 Control | 在控件 `Render(DrawingContext)` 中直接绘制段或点 | 视觉树少，性能稳定，适合大量字符；不依赖 AtomUI 成型控件 | 需要自己处理测量、命中、缩放和几何细节 | 推荐作为 Segment 和 Matrix 的默认实现方向 |
| 模板拼元素 | 用 Avalonia `Path`、`Border` 等元素拼出每段或每点 | 样式直观，容易局部调试 | 字符多时视觉树膨胀，性能和测量复杂 | 只适合原型或极少字符场景 |
| Canvas/子控件组合 | 每个段或点作为子元素放入 Canvas | 坐标表达直观，交互扩展容易 | 控件树复杂，布局和虚拟化成本高 | 不作为第一版默认方案 |

第一版建议优先采用自绘 Control。自绘不等于放弃主题能力，各路线支持的颜色、尺寸和间距参数仍可通过 StyledProperty 与 AtomUI Shared Token 默认值接入。

自绘控件必须明确处理父容器最终给出的空间：

- 父容器给出更多空间时，通过内容对齐决定显示内容停靠位置。
- 父容器给出更小空间时，默认应裁剪到控件 bounds 内，避免绘制污染相邻区域。
- 需要完整显示时，可提供显式等比缩小策略，但不能默认偷偷缩小，否则会掩盖布局问题。

## 主题与 Token

LED 家族可以继续使用 AtomUI 基础设施：

- `AtomUI.Core` 的 `ThemeManager`、Shared Token、资源绑定和运行时基础能力。
- `AtomUI.Generator` 的生成能力。
- 必要时使用 `AtomUI.Controls.Shared` 的共享契约和工具。

LED 家族不得使用 AtomUI 已经成型的控件包：

- `AtomUI.Controls`
- `AtomUI.Desktop.Controls`
- `AtomUI.Desktop.Controls.Extras`
- `AtomUI.Desktop.Controls.DataGrid`
- `AtomUI.Desktop.Controls.ColorPicker`

第一阶段不强制定义 LED Control Token。建议先用 StyledProperty 暴露实验视觉参数，默认值通过 Shared Token 取得。等 Segment 和 Matrix 的稳定视觉语义形成后，再评估是否提取 LED 家族 Token 或路线级 Token。

## 非目标

当前不处理：

- 真实硬件 LED 控制。
- 中文、复杂脚本或富文本排版。
- Segment 内建动画，以及 Matrix 单向穿屏之外的滚动、闪烁或故障动画。
- 把 `Segment` 和 `Matrix` 合并为一个万能控件。
- 提前固定 LED 家族公共代码目录名。

## 实现验证

实现阶段至少需要验证：

- Labs 项目构建通过。
- `controlgallery/AtomUILabsGallery.Desktop` 构建和 `win-x64` NativeAOT 发布通过。
- 搜索确认 LED 家族没有引用 AtomUI 成型控件包。
- Gallery 能展示 Segment、Matrix、Glow 和 Marquee 的基础视觉与交互。
- `git diff --check` 通过。

## 当前契约追踪

| 契约 | 主要源码 | 主要自动化验证 |
|---|---|---|
| Segment 理想尺寸、对齐、Clip/ScaleDown | `Segment/Layout/SegmentLayoutEngine.cs`、`SegmentDisplay.cs` | `SegmentLayoutEngineTests`、`SegmentDisplayMeasureTests`、`SegmentDisplayRenderTests` |
| Segment 数值、圆角和主题鲁棒性 | `SegmentValueSanitizer.cs`、`SegmentDisplayTheme.axaml` | `SegmentDisplayRenderTests`、`SegmentDisplayThemeTests` |
| Matrix 字模、点形、边框和主题 | `MatrixDisplay.cs`、`Matrix/Character/`、`Matrix/Rendering/` | `MatrixDisplay*Tests`、`MatrixGlyph*Tests` |
| Glow scoped blur 与 Effect 复用 | `Glow/LedGlowRenderer.cs`、两个 Display 的 Render 路径 | `LedGlow*Tests`、Segment/Matrix Render 与正式性能测试 |
| Marquee 运动、静态回退与生命周期 | `Marquee/`、`MatrixDisplay.cs` | `MatrixMarqueeMotionTests`、`MatrixMarqueeLifecycleTests`、`MatrixDisplayRenderTests` |

## 相关设计

- [LED 家族公共边界审计](family-common-boundary-audit.md)：记录 Segment 与 Matrix 之间已验证的共享边界。
- [LED Glow 当前技术合同](glow-technical-options.md)：记录正式 scoped BlurEffect 路线的公共属性、绘制和性能契约。
- [LED Glow 原型评估](glow-prototype-evaluation.md)：记录候选路线、正式控件接入和性能门禁的历史验证。
- [LED Matrix Marquee最小契约](matrix-marquee-minimum-contract.md)：记录单向穿屏公共契约、动态增强边界和后续官方运动模式的内部扩展结构。
