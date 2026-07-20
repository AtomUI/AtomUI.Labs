# LED Glow 当前技术合同

> 文档状态：当前实现与发布契约，更新于 2026-07-20。候选路线、原型过程和旧性能数据见 [LED Glow 原型评估](glow-prototype-evaluation.md)，不作为当前运行时事实。

## 当前结论

Segment 与 Matrix 使用同一个内部 `Glow/LedGlowRenderer.cs`。正式路线是 Avalonia 公共 API `DrawingContext.PushEffect(BlurEffect, bounds)`：控件先生成并剔除可见 Active Geometry，在一个局部 Effect 作用域内用 `GlowBrush` 绘制一次，再退出作用域绘制清晰 Active 本体。

当前实现没有应用层 Alpha Mask、离屏位图、LRU、字符级 Glow 缓存、`MaskBuildCount` 或 `BlurBuildCount`。这些概念只属于被否决或未采用的历史候选路线。

```text
Background / Inactive
    -> 已剔除的可见 Active Geometry
    -> PushOpacity(GlowOpacity)
    -> PushEffect(共享 BlurEffect, Active Bounds)
    -> GlowBrush 绘制 Active Geometry
    -> 退出 Effect
    -> ActiveBrush 绘制清晰本体
    -> Border（Matrix）
```

Glow 是可选视觉增强，不参与字符映射、字模、SegmentParts、Measure 或 DesiredSize。关闭 Glow 后，两个基础控件必须保持完整可用。

## 公共属性

Segment 与 Matrix 分别注册同名、同类型、同默认值的 StyledProperty：

| 属性 | 类型 | 默认值 | 内部有效值 |
|---|---|---:|---|
| `GlowBrush` | `IBrush?` | `null` | `null` 表示硬关闭 |
| `GlowOpacity` | `double` | `0.35` | 非有限值回退默认值，之后限制到 `0..1` |
| `GlowRadius` | `double` | `6` | 非有限值回退默认值，之后限制到 `0..24` DIP |

规整只影响内部有效值，不回写 StyledProperty。当前不公开 `GlowEnabled`、Offset、Spread、Quality、RenderMode、LayerCount 或专用动画属性。

## 绘制与资源语义

- `GlowBrush=null`、有效 `GlowOpacity=0`、有效 `GlowRadius=0` 或无可用 Active Bounds 时不建立 Effect 作用域。
- `GlowBrush=null` 时控件释放其 `LedGlowRenderer` 引用；这是资源和语义上的硬关闭。
- `GlowOpacity=0` 只暂停 Effect 提交，并允许保留已经创建的 `BlurEffect`，避免呼吸动画经过零点时重建。
- 每个控件实例最多持有一个 `LedGlowRenderer` 和一个可复用 `BlurEffect`；没有全局静态资源表。
- 每个控件每帧最多建立一个 Glow Effect 作用域，不逐段、逐点或逐字模建立 Effect。
- `GlowBrush`、`GlowOpacity` 和 `GlowRadius` 不进入 Segment/Matrix 基础 Geometry 缓存键。
- Brush 或 Opacity 变化不得重建基础 Geometry；Radius 变化只更新复用 Effect 的半径。
- Effect Bounds 必须有限且具有正面积。异常 Bounds 跳过 Glow，但不得阻止清晰 Active 本体绘制。
- Glow 由现有内容裁剪约束：Matrix 不越过内容视口/边框内缘，Segment 不越过控件 Bounds。开发者使用 Padding 为完整光晕预留空间。

## 失效与生命周期

Glow 属性只触发重绘，不触发 Measure。`GlowBrush` 变为 `null` 时立即丢弃 Renderer；控件不可达后 Renderer 和 Effect 随实例回收。Glow 不创建计时器、不订阅事件，也不拥有动画生命周期。动画由 Avalonia Animation 修改现有 StyledProperty。

## 自动化验证

必须覆盖：

- 三项公共属性的名称、类型、默认值、CLR wrapper 和 AXAML 转换。
- `NaN`、正负 Infinity、负数、零、边界值和极大有限值的内部规整。
- Background、Inactive、Active、Glow 和 Border 的图层隔离。
- null Brush、零 Opacity、零 Radius 和无效 Bounds 均不建立 Effect。
- 静态 Glow 预热后不新增 Effect；每帧恰好一个 Effect scope。
- Brush、Opacity、Radius 动画不重建基础 Geometry，缓存数量不随帧数增长。
- Matrix 可见字形剔除包含有效 GlowRadius，长文本工作量受视口约束。
- Segment 使用一份可见 Active 聚合 Geometry 完成一次 Glow 和一次清晰本体绘制。

## 性能门禁

`tools/performances/AtomUI.Labs.Led.Performance --formal-glow` 是当前正式命令提交基准。它测量 DrawingGroup 构建和命令提交，不宣称代表 GPU 呈现时间。

PR 级单进程门禁：

- 默认预热后测量至少 6000 帧。
- 两种关闭路径的 Effect builds 和 Effect scopes 都必须为 0。
- 静态 Glow 预热后 Effect builds 必须为 0，Effect scopes 必须等于测量帧数。
- runner 在同一进程内执行 5 次交错顺序的配对测量，并以倍率中位数判定。以 `GlowBrush=null` 为硬关闭基线；零 Opacity 路径仅在耗时超过基线 `1.05x` 且新增耗时同时超过 `0.25 us/frame` 时失败。绝对阈值用于避免 Segment 约数微秒基线把亚微秒计时噪声放大成虚假倍率回归。
- 零 Opacity 相对 null Brush 的稳态新增分配必须同时不超过基线的 `2.5%` 和 `4096 bytes/frame`。两条路径语义不同：null Brush 会释放 Renderer，零 Opacity 允许保留 Effect，因此不要求逐字节相等；零 Opacity 低于基线不视为回归。

发布级门禁：

- 使用 Release 构建，至少运行 5 个独立进程。
- 报告耗时倍率和分配差异的 Median，并保留每个进程的原始结果。
- Median 必须满足上述耗时和分配阈值；单个进程的偶发超限要记录，但不替代 Median 判定。
- 额外执行 100000 帧静态/动态长稳、固定长度文本更新、拓扑变化和完整回收检查；缓存、Effect 数和存活内存不得随历史帧数或文本无界增长。

## 发布失败条件

以下任一情况阻止发布：

- Glow 关闭仍提交 Effect、创建后端 Effect 或增加 Glow 绘制命令。
- 静态 Glow 在稳态帧重复创建 Effect。
- 一个控件一帧建立多个逐点、逐段或逐字模 Effect scope。
- Background、Inactive 或 Border 被纳入 Glow。
- Glow 属性变化破坏基础 Geometry 缓存或影响 DesiredSize。
- Release 五进程 Median 超过正式耗时或分配阈值。

## 历史路线边界

多层矢量扩张、应用层 Alpha Mask、离屏 Blur、LRU 和 Skia 自定义绘制曾作为候选方案评估。它们的工程约束和原型证据保留在 [LED Glow 原型评估](glow-prototype-evaluation.md) 及带日期的性能文档中。除非形成新的书面技术决议，否则不得把这些历史设计描述为当前实现，也不得为满足旧路线指标而在运行时引入 Mask 或缓存系统。
