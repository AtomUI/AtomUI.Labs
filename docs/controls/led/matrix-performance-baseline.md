# Matrix Interaction Performance Baseline

> 文档状态：历史性能快照。下列数值只适用于文内日期、配置和机器，不代表当前性能；回归时必须重新运行本仓库 runner。

- Date: 2026-07-10 17:22:19 +08:00
- Configuration: Release, .NET 10
- Runner: `tools/performances/AtomUI.Labs.Led.Performance --count <N>`
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
