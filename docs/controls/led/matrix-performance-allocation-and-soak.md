# Matrix Interaction Performance Baseline

> 文档状态：历史性能快照。下列数值只适用于文内日期、配置和机器，不代表当前性能；回归时必须重新运行本仓库 runner。

- Date: 2026-07-10 19:08:33 +08:00
- Configuration: Release, .NET 10
- Runner: `tools/performances/AtomUI.Labs.Controls.Led.Performance --count <N> --frames <N> --soak-frames <N>`
- Scope: CPU-side layout and DrawingGroup command submission; excludes GPU/platform presentation cost

| Scenario | Characters | Updates | Total ms | us/update | KB total | bytes/update | Geometry commands |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Matrix.CachedRender.8.Clip | 8 | 20 | 0.87 | 43.51 | 461.7 | 23636.8 | 7 |
| Matrix.DynamicText.8.Clip | 8 | 20 | 0.77 | 38.74 | 476.4 | 24394.0 | 7 |
| Matrix.CachedRender.1000.Clip | 1000 | 20 | 0.67 | 33.48 | 461.7 | 23636.8 | 7 |
| Matrix.CachedRender.10000.Clip | 10000 | 20 | 0.77 | 38.67 | 461.7 | 23636.8 | 7 |
| Matrix.CachedRender.1000.ScaleDown | 1000 | 20 | 113.12 | 5656.17 | 55242.3 | 2828404.8 | 1000 |

## Fixed-Length Dynamic Load

| Characters | Inactive dots | Frames | Total ms | us/frame | bytes/frame | Layout builds | Geometry builds | Final cache | Geometry commands/frame |
| ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 6 | Off | 600 | 30.06 | 50.10 | 21216.1 | 600 | 0 | 10 | 6 |
| 6 | On | 600 | 33.26 | 55.43 | 26592.1 | 600 | 0 | 10 | 12 |
| 8 | Off | 600 | 31.03 | 51.71 | 26896.1 | 600 | 0 | 10 | 8 |
| 8 | On | 600 | 41.28 | 68.80 | 34064.1 | 600 | 0 | 10 | 16 |
| 16 | Off | 600 | 57.18 | 95.29 | 49848.1 | 600 | 0 | 15 | 16 |
| 16 | On | 600 | 71.31 | 118.85 | 64184.1 | 600 | 0 | 15 | 32 |

## Allocation Attribution

All dynamic Matrix attribution scenarios use 16 characters with inactive dots enabled. `Precomputed` scenarios exclude caller-side string construction. The rows are independently measured and are not mathematically additive.

| Stage | Operations | Total ms | us/operation | bytes/operation |
| --- | ---: | ---: | ---: | ---: |
| Caller.TextFormatting.16 | 600 | 0.14 | 0.23 | 120.1 |
| Harness.EmptyDrawingGroup | 600 | 0.36 | 0.59 | 568.1 |
| Matrix.LayoutOnly.Precomputed.16 | 600 | 3.28 | 5.46 | 672.1 |
| Matrix.CachedRenderOnly.EmptyText | 600 | 4.91 | 8.19 | 3776.1 |
| Matrix.CachedRenderOnly.16.InactiveOff | 600 | 54.23 | 90.38 | 49056.1 |
| Matrix.CachedRenderOnly.16.InactiveOn | 600 | 68.24 | 113.74 | 63392.1 |
| Matrix.DynamicPrecomputed.16.InactiveOn | 600 | 74.53 | 124.21 | 64064.1 |
| EndToEnd.DynamicFormatted.16.InactiveOn | 600 | 72.31 | 120.52 | 64184.1 |

## Long-Running Soak

The soak uses a precomputed 1,000-value text ring after warmup. Natural GC counts are captured without forced collections during the measured loop. Retained bytes are compared only after full collections before and after the loop.

| Frames | Total ms | us/frame | bytes/frame | Gen0 | Gen1 | Gen2 | Live before | Live after | Live delta | Sampled peak | Layout builds | Geometry builds | Cache | Commands/frame |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | ---: |
| 36000 | 1560.05 | 43.33 | 63526.1 | 273 | 83 | 0 | 354080 | 281928 | -72152 | 8712128 | 36000 | 0 | 15->15 | 32 |

## 归因结论

- 16字符双层动态帧的完整分配为 `64,184.1 bytes/frame`。调用方字符串格式化只占 `120.1 bytes/frame`，约 `0.19%`。
- 使用预生成文本时，动态布局和绘制为 `64,064.1 bytes/frame`；其中单独布局为 `672.1 bytes/frame`，约占 `1.05%`。
- 固定布局下的双层绘制命令记录为 `63,392.1 bytes/frame`，约占预生成文本动态帧的 `98.95%`。因此当前分配主要不在字模映射、layout 对象或 Geometry 构建。
- 空 `DrawingGroup` 自身为 `568.1 bytes/frame`；空文本 Matrix Render 为 `3,776.1 bytes/frame`。16字符仅亮点为 `49,056.1 bytes/frame`，亮暗双层为 `63,392.1 bytes/frame`，分配随可见字符和 Geometry 命令层数增长。
- 各行是独立测量，不能简单相加；百分比仅用于识别主要成本所在。

## 长稳结论

- 36,000帧累计约产生 `2.29 GB` 短生命周期托管分配流量；自然 GC 为 Gen0 `273`、Gen1 `83`、Gen2 `0`。
- 预热后 Geometry 新建数为 `0`，缓存从 `15` 保持到 `15`，布局版本严格增加36,000，最终每帧仍为32个 Geometry 命令。
- 强制完整回收前后的存活托管内存从 `354,080` 变为 `281,928` bytes，差值 `-72,152` bytes；采样峰值约 `8.71 MB`。没有观察到单调存活内存增长。
- 独立第二次36,000帧复跑得到 `63,522.8 bytes/frame`、Gen0/Gen1/Gen2=`273/83/0`、存活内存差值同为 `-72,152` bytes，缓存和命令计数完全一致。

## 解释边界

- 这些结果证明当前实例级 layout/Geometry 缓存没有随帧数增长，也没有形成可观测的托管对象滞留。
- 测量程序每帧新建 `DrawingGroup` 来记录 CPU 侧绘制命令，因此 `bytes/frame` 包含测试记录对象。它不能直接等同于 Avalonia 平台渲染器、合成器或 GPU 呈现阶段的生产分配。
- 时间数据是同机 Release 单次 smoke，只用于量级检查，不作为跨机器性能承诺或 CI 阈值。
- 目前不应为了约1%的 layout 分配引入数组池、可变槽位缓冲或更复杂的生命周期。若继续优化绘制分配，必须先建立贴近真实渲染器的测量方法，并证明改动降低实际成本且不破坏可见结果。
