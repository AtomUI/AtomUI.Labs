# Matrix Interaction Performance Baseline

> 文档状态：已实现。本文于 2026-07-20 随 LED 控件从 AtomUI 迁入 AtomUI.Labs，并已按本仓库的包名、目录和验证入口完成适配。历史性能数值仍表示迁移时的基线，后续变更应在本仓库重新验证。

- Date: 2026-07-10 18:53:37 +08:00
- Configuration: Release, .NET 10
- Runner: `tools/performances/AtomUI.Labs.Led.Performance --count <N> --frames <N>`
- Scope: CPU-side layout and DrawingGroup command submission; excludes GPU/platform presentation cost

| Scenario | Characters | Updates | Total ms | us/update | KB total | bytes/update | Geometry commands |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Matrix.CachedRender.8.Clip | 8 | 20 | 1.06 | 53.02 | 461.7 | 23636.8 | 7 |
| Matrix.DynamicText.8.Clip | 8 | 20 | 0.81 | 40.61 | 476.4 | 24394.0 | 7 |
| Matrix.CachedRender.1000.Clip | 1000 | 20 | 0.71 | 35.38 | 461.7 | 23636.8 | 7 |
| Matrix.CachedRender.10000.Clip | 10000 | 20 | 0.64 | 31.86 | 461.7 | 23636.8 | 7 |
| Matrix.CachedRender.1000.ScaleDown | 1000 | 20 | 110.26 | 5513.25 | 55242.3 | 2828404.8 | 1000 |

## Fixed-Length Dynamic Load

| Characters | Inactive dots | Frames | Total ms | us/frame | bytes/frame | Layout builds | Geometry builds | Final cache | Geometry commands/frame |
| ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 6 | Off | 600 | 21.61 | 36.01 | 21216.1 | 600 | 0 | 10 | 6 |
| 6 | On | 600 | 26.38 | 43.97 | 26592.1 | 600 | 0 | 10 | 12 |
| 8 | Off | 600 | 26.23 | 43.72 | 26896.1 | 600 | 0 | 10 | 8 |
| 8 | On | 600 | 34.45 | 57.42 | 34064.1 | 600 | 0 | 10 | 16 |
| 16 | Off | 600 | 49.91 | 83.19 | 49848.1 | 600 | 0 | 15 | 16 |
| 16 | On | 600 | 61.52 | 102.53 | 64184.1 | 600 | 0 | 15 | 32 |

## Interpretation

- Warmup covers all decimal digits and the fixed `TEMP`/`RPM` letters. Every measured scenario reports zero new geometry builds across 600 frames.
- Final geometry caches remain bounded at 10 numeric shapes or 15 numeric/status shapes; they do not grow with frame count.
- Layout rebuilds equal frame count because every frame changes `Text`. Geometry reuse does not incorrectly reuse text layout.
- Geometry commands remain exactly one per character with inactive dots off and two per character with inactive dots on.
- Timing is a same-machine Release smoke measurement, not a CI threshold. The 16-character dual-layer case is about 102.5 us/frame, while its 64.2 KB/frame allocation identifies layout and drawing-command recording as the next measurable cost.
