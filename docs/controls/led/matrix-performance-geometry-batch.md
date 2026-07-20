# Matrix Interaction Performance Baseline

> 文档状态：迁移参考。本文于 2026-07-20 从 AtomUI 仓库的 `dev-and-mark/modules/desktop-controls-labs` 复制到 AtomUI.Labs 并适配文档结构。`AtomUI.Labs.Controls.LED` 表示本仓库的目标设计；文中的“已实现”、验证数据及旧项目命令来自迁移前的 `AtomUI.Desktop.Controls.Labs` 参考实现，不表示当前仓库已经包含相应源码、测试或性能工具。

- Date: 2026-07-10 18:21:22 +08:00
- Configuration: Release, .NET 10
- Runner: `tools/performances/AtomUI.Desktop.Controls.Labs.Performance --count <N>`
- Scope: CPU-side layout and DrawingGroup command submission; excludes GPU/platform presentation cost

| Scenario | Characters | Updates | Total ms | us/update | KB total | bytes/update | Geometry commands |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Matrix.CachedRender.8.Clip | 8 | 20 | 0.90 | 45.09 | 461.7 | 23636.8 | 7 |
| Matrix.DynamicText.8.Clip | 8 | 20 | 1.00 | 50.17 | 476.4 | 24394.0 | 7 |
| Matrix.CachedRender.1000.Clip | 1000 | 20 | 0.64 | 32.21 | 461.7 | 23636.8 | 7 |
| Matrix.CachedRender.10000.Clip | 10000 | 20 | 0.64 | 32.05 | 461.7 | 23636.8 | 7 |
| Matrix.CachedRender.1000.ScaleDown | 1000 | 20 | 95.89 | 4794.65 | 55242.3 | 2828404.8 | 1000 |

## Before/After

The baseline is [matrix-performance-baseline.md](matrix-performance-baseline.md). Timing is a same-machine single-run smoke comparison, not a cross-machine speed guarantee. Geometry command counts are deterministic structural measurements.

| Scenario | Metric | Baseline | Geometry batch | Formula | Improvement | Conclusion |
| --- | --- | ---: | ---: | --- | ---: | --- |
| Cached 8 Clip | Geometry commands/render | 97 | 7 | `(97-7)/97` | 92.8% fewer | Visible glyphs submit one active geometry each |
| Cached 8 Clip | Allocated bytes/render | 98284.8 | 23636.8 | `(98284.8-23636.8)/98284.8` | 76.0% lower | Cached geometry removes per-dot recording objects |
| Dynamic 8 Clip | Geometry commands/render | 133 | 7 | `(133-7)/133` | 94.7% fewer | Text changes reuse existing digit geometries |
| Dynamic 8 Clip | Allocated bytes/update | 129764.8 | 24394.0 | `(129764.8-24394.0)/129764.8` | 81.2% lower | Dynamic layout rebuild no longer rebuilds dot commands |
| Cached 10000 Clip | Geometry commands/render | 119 | 7 | `(119-7)/119` | 94.1% fewer | Binary visibility culling remains effective |
| Cached 10000 Clip | Smoke time/update | 193.89 us | 32.05 us | `(193.89-32.05)/193.89` | 83.5% lower | Local smoke only |
| Cached 1000 ScaleDown | Geometry commands/render | 17000 | 1000 | `(17000-1000)/17000` | 94.1% fewer | Complete ScaleDown now submits one active geometry per glyph |
| Cached 1000 ScaleDown | Allocated bytes/render | 16712458.0 | 2828404.8 | `(16712458.0-2828404.8)/16712458.0` | 83.1% lower | Extreme path remains allocation-heavy but materially reduced |
| Cached 1000 ScaleDown | Smoke time/update | 39555.08 us | 4794.65 us | `(39555.08-4794.65)/39555.08` | 87.9% lower | Local smoke only; still not a frame-rate target |
