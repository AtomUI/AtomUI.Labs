# Matrix Interaction Performance Baseline

> 文档状态：迁移参考。本文于 2026-07-20 从 AtomUI 仓库的 `dev-and-mark/modules/desktop-controls-labs` 复制到 AtomUI.Labs 并适配文档结构。`AtomUI.Labs.Controls.LED` 表示本仓库的目标设计；文中的“已实现”、验证数据及旧项目命令来自迁移前的 `AtomUI.Desktop.Controls.Labs` 参考实现，不表示当前仓库已经包含相应源码、测试或性能工具。

- Date: 2026-07-10 17:22:19 +08:00
- Configuration: Release, .NET 10
- Runner: `tools/performances/AtomUI.Desktop.Controls.Labs.Performance --count <N>`
- Scope: CPU-side layout and DrawingGroup command submission; excludes GPU/platform presentation cost

| Scenario | Characters | Updates | Total ms | us/update | KB total | bytes/update | Submitted dots |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Matrix.CachedRender.8.Clip | 8 | 20 | 4.21 | 210.59 | 1919.6 | 98284.8 | 97 |
| Matrix.DynamicText.8.Clip | 8 | 20 | 4.22 | 210.88 | 2534.5 | 129764.8 | 133 |
| Matrix.CachedRender.1000.Clip | 1000 | 20 | 4.47 | 223.41 | 2328.7 | 119228.8 | 119 |
| Matrix.CachedRender.10000.Clip | 10000 | 20 | 3.88 | 193.89 | 2328.7 | 119228.8 | 119 |
| Matrix.CachedRender.1000.ScaleDown | 1000 | 20 | 791.10 | 39555.08 | 326415.2 | 16712458.0 | 17000 |

## Interpretation

- `Clip` keeps command submission bounded by the visible viewport: the 1000- and 10000-character cached scenarios have the same submitted-dot count and similar allocation.
- Fixed-length dynamic text includes string creation, Rune mapping, layout rebuild and drawing command submission.
- `ScaleDown` intentionally keeps the complete text visible. At 1000 characters it submits 17000 active dots and is not suitable for frame-rate-sensitive updates under the current contract.
- These values are a local baseline, not CI pass/fail thresholds. Compare future runs on the same machine and configuration.
