# LED Segment 性能回归矩阵

> 文档状态：已实现。本文于 2026-07-20 随 LED 控件从 AtomUI 迁入 AtomUI.Labs，并已按本仓库的包名、目录和验证入口完成适配。历史性能数值仍表示迁移时的基线，后续变更应在本仓库重新验证。

本文定义 `SegmentDisplay` 高频更新和缓存行为的长期回归边界。指标使用确定性的 layout/geometry 重建次数，不使用容易受机器负载影响的单元测试耗时阈值。

## 场景资格

- Labs sample 同时展示超过 5 个 `SegmentDisplay` 实例。
- 动态时钟和计数器会在一次运行期间持续更新 `Text`。
- 高频成本位于控件自己的字符映射、layout 和 geometry 生成路径，不依赖对 Avalonia 内部成本的推测。

## 性能场景

| 场景 | 输入 | layout 期望 | geometry 期望 |
|---|---|---|---|
| 重复 Render | `Text` 和属性不变 | 不重建 | 不重建 |
| 同 topology 文本更新 | `"12" -> "34"`、固定四位计数器 | 重建 | 复用 |
| topology 变化 | 冒号变点号、字符数量变化、Segment 变 Empty | 重建 | 重建 |
| 几何参数变化 | thickness、gap、bevel、dot scale | 不要求重建 layout | 重建 |
| 绘制属性变化 | brush、glow、alignment、overflow | 不重建 | 不重建 |
| 字符布局参数变化 | height、aspect ratio、spacing、padding | 重建 | 重建 |

## 功能矩阵

- [x] geometry 复用后使用当前字符 pattern 绘制，不显示旧字符。
- [x] 冒号和小数点保持各自的 geometry。
- [x] 空格和不支持字符保持 Empty 语义。
- [x] Active、Inactive、Glow 和背景绘制顺序不变。
- [x] Clip、ScaleDown 和内容对齐行为不变。

## 连续更新矩阵

- [x] 连续 2000 次固定四位数字更新不抛异常。
- [x] 每次不同的数字文本都触发 layout 更新。
- [x] 2000 次同 topology 更新不增加 geometry 重建次数。
- [x] 高频复用后再改变 topology，geometry 正确重建。
- [x] 高频复用后再改变几何参数，geometry 正确重建。

## 生命周期与所有权

- geometry 缓存属于单个 `SegmentDisplay` 实例，不跨实例共享。
- 缓存只保留当前一组 prepared slots，不维护随文本增长的历史集合。
- 本优化不创建订阅、binding、timer、动态视觉或跨控件引用，因此没有新增释放配对。

## 性能结论口径

固定四位数字每次更新的整套 geometry cache 重建次数从 `1` 降为 `0`。这是一项确定性的结构收益；在建立同口径、多轮的独立性能测量前，不声明毫秒或页面加载百分比。
