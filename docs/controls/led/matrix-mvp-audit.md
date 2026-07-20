# Matrix MVP 收口审计

> 文档状态：已实现。本文于 2026-07-20 随 LED 控件从 AtomUI 迁入 AtomUI.Labs，并已按本仓库的包名、目录和验证入口完成适配。历史性能数值仍表示迁移时的基线，后续变更应在本仓库重新验证。

- 审计日期：2026-07-10
- 审计对象：`AtomUI.Labs.Led.Matrix.MatrixDisplay`
- 审计类型：MVP 交付收口，不进行行为修改或性能优化
- 结论：未发现高严重度运行缺陷；审计发现已于同日修复并进入回归验证

## 审计边界

```text
主要职责：5x7 单行点阵文本映射、测量、对齐、溢出处理、自绘和自动化语义
状态所有者：MatrixDisplay 的 StyledProperty；当前 layout 缓存；当前实例字模 Geometry 缓存
生命周期：无事件订阅、timer、binding、动态视觉或全局缓存；实例缓存随控件释放
字符流：Text -> Rune -> 规范化/fallback -> glyph slot -> Geometry -> DrawingContext
公开合同：MatrixDisplay、MatrixOverflowMode、13个 StyledProperty、Labs AXAML namespace
主题合同：Shared Token -> ControlTheme Setter -> StyledProperty 最终有效值
本轮不改：Public API、默认值、字模、视觉输出、缓存策略、Sample 行为和包依赖
```

## 审计发现

以下问题均已完成对应修复；保留原始发现和证据，避免丢失审计上下文。

### 中：Labs 架构文档存在阶段性漂移

`architecture.md` 和 `overview.md` 仍保留 Matrix 落地前的描述：

- 项目结构树只展开 Segment，没有展开当前 Matrix 的 Character、Layout、Rendering 和 Themes 结构。
- LED 家族章节仍写“未来 Matrix 应与 Segment 同级”，与当前源码不符。
- 主题聚合链只写到 Segment，没有记录 `LedThemes.axaml -> MatrixThemes.axaml -> MatrixDisplayTheme.axaml`。
- Namespace 策略仍把 `https://atomui.net/labs` 写成待评估选项，但程序集已经正式使用该 namespace。
- 模块概览仍写“第一阶段只放入 Dashboard”，不能反映当前 Dashboard、Segment、Matrix 三个入口。
- Token 章节包含 Matrix 落地前的后续假设，不符合当前状态文档应只描述已实现事实的要求。

影响：不会造成运行错误，但会让维护者对目录、主题入口、AXAML namespace 和当前控件范围形成错误判断。

建议：下一轮先修正文档为当前状态，删除阶段性和未来式表述；只修改 `dev-and-mark`，不得修改 `docs`。

### 中：主题优先级合同仍有两个直接测试缺口

当前测试已经验证：

- ControlTheme 能解析五个 Shared Token 默认值。
- Dark ThemeVariant 切换后 Token 默认值动态刷新。
- 非空本地 `ActiveBrush` 在主题切换后保持本地优先级。

仍未直接验证：

- Compact ThemeVariant 下 `PaddingLG` 等 Shared Token 默认值是否更新到 Matrix。
- 在主题已经提供画刷默认值时，开发者显式设置 `ActiveBrush = null` 或 `InactiveBrush = null` 是否仍能压过主题 Setter，并保持“禁用亮点/暗点”的公开语义。

这不是已证实的运行 bug，而是绑定优先级和 nullable 禁用语义的回归盲点。下一轮应补测试，失败时再做根因修复。

### 低：Sample 不能完成主题切换的人工视觉验收

Sample 已覆盖默认值、大小写、数字、符号、fallback、暗点开关、自定义颜色、内容对齐、Clip、ScaleDown、极小视口和动态计数器，但没有 Light/Dark/Compact 切换入口。

此外，Sample 分组标签固定使用 `Brushes.Black` 和 `Brushes.DimGray`。如果加入 Dark 主题切换，这些标签可能不再适合作为可读的主题验收界面。

建议：使用 Avalonia 原生控件提供简洁主题切换入口，标签颜色使用可随主题变化的资源；Sample 不得因此依赖 AtomUI 成型控件。

### 低：NuGet README 没有完整列出当前 Labs 控件

包内 README 只给出 `MatrixDisplay` 示例，没有说明同包还包含 `SegmentDisplay` 和用于链路验证的 `Dashboard`。这不影响 Matrix 使用，但会降低包的可发现性。

建议：在不承诺稳定 API 的前提下，列出当前实际控件和最小用法；不要写未来控件清单。

### 低：异常 CornerRadius 尚无专项鲁棒性测试

Matrix 对 `DotSize`、间距和 Padding 有明确数值规整，但 `CornerRadius` 直接传给 `RoundedRect`。当前测试只覆盖正常圆角，没有覆盖负数、NaN 或 Infinity。

目前没有证据证明这里存在 Avalonia 运行错误，因此不能直接增加私有规整逻辑。下一轮先添加不抛异常和有限绘制边界测试；只有测试暴露问题时才修复。

## 已确认正确的边界

- 13个 StyledProperty 的名称、代码默认值和 CLR wrapper 与设计文档一致。
- `Clip`、`ScaleDown`、Left/Center/Right、Top/Center/Bottom 和 Stretch-as-Center 语义与文档一致。
- `ActiveBrush = null`、`InactiveBrush = null`、`ShowInactiveDots = false` 的绘制语义在非主题宿主下已有结构化测试。
- Unicode 以 Rune 为边界；补充平面字符和非法代理项均只产生一个 fallback。
- 字模固定45种，实例 Geometry 缓存有界；参数变化释放旧缓存，控件整体可回收。
- 长文本 Clip 使用二分定位可见槽位；ScaleDown 保留全部字符。
- 自动化使用只读 Text 类型、规范化显示名称和动态 Name 变化通知。
- 无 `_ignore`/`_suppress` 状态补丁，无 C# binding，无事件订阅，无静态 Geometry 缓存。
- Labs 项目没有引用 `AtomUI.Controls`、`AtomUI.Desktop.Controls` 或其它成型控件包。
- NuGet net8/net10 依赖只有 `AtomUI.Core` 和 Avalonia。

## 验证结果

- Matrix 专项测试：114/114 通过，Release net10.0。
- Labs 全量测试：259/259 通过，Release net10.0。
- Labs NuGet pack：迁移后成功生成 `AtomUI.Labs.Led.6.0.8.nupkg`，同时包含 `net10.0` 与 `net8.0` 资产。
- 包目标：`lib/net8.0`、`lib/net10.0`。
- 包依赖：`AtomUI.Core 6.0.8`、`Avalonia 12.0.5`。
- Sample Debug和Release build：均为0 warning，0 error。
- Sample win-x64 NativeAOT publish：成功；3条既有IL2026/IL3050/IL2060警告来自`AtomUI.Core/AppBuilderExtensions.cs`，不来自Labs或本轮改动。
- Sample真实窗口人工视觉验收：用户确认显示、主题切换和交互观察完全正常。
- 前序长稳测试：36,000帧 Geometry 新增0，缓存`15 -> 15`，完整GC后无存活内存增长。

## 修复结果

- `architecture.md`和`overview.md`已改为Dashboard、Segment、Matrix当前结构，补全Matrix主题聚合链和既定AXAML namespace，删除阶段性未来式描述。
- 新增Compact ThemeVariant下Shared Token Padding刷新测试。
- 新增主题已生效时显式`ActiveBrush = null`、`InactiveBrush = null`跨主题切换优先级测试。
- 新增负数、NaN、正负Infinity CornerRadius Render探测测试；Avalonia现有绘制路径全部通过，因此没有增加Matrix私有规整逻辑。
- Sample新增Avalonia原生Dark和Compact独立开关，支持Light、Dark、Compact、Dark+Compact四种组合。
- Sample分组和案例标签改用Shared Token动态样式，不再硬编码黑色和灰色。
- NuGet README已列出Dashboard、SegmentDisplay、MatrixDisplay，并包含两种LED显示控件的最小AXAML。

## 实施顺序

1. 修正 `dev-and-mark` 中 Labs 架构和概览文档的当前状态。
2. 补 Compact ThemeVariant 和主题下显式 null 画刷优先级测试。
3. 补异常 CornerRadius 探测测试；仅在测试失败时修改实现。
4. 为 Sample 增加 Light/Dark/Compact 视觉验收入口并调整标签资源。
5. 更新包内 README 的当前控件清单。
6. 重跑 Matrix 专项、Labs 全量、Sample Debug/Release、pack 和 `git diff --check`。

以上步骤均已完成。

本审计不建议继续进行极端性能优化。若真实 Avalonia 宿主出现掉帧或内存问题，应建立平台渲染器级证据后再重新开启性能工作。

Matrix MVP第一版至此完成，当前公共合同进入冻结状态；新增视觉能力或行为应作为独立版本设计，不与MVP收口修复混合。
