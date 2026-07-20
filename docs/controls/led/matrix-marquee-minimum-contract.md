# LED Matrix Marquee 最小契约

> 文档状态：迁移参考。本文于 2026-07-20 从 AtomUI 仓库的 `dev-and-mark/modules/desktop-controls-labs` 复制到 AtomUI.Labs 并适配文档结构。`AtomUI.Labs.Controls.LED` 表示本仓库的目标设计；文中的“已实现”、验证数据及旧项目命令来自迁移前的 `AtomUI.Desktop.Controls.Labs` 参考实现，不表示当前仓库已经包含相应源码、测试或性能工具。

## 领域边界

Marquee与Glow同属LED基础显示之上的可选动态增强领域，但二者是平行模块：

```text
LED/
├── Matrix/       MatrixDisplay、字符、布局和Geometry
├── Segment/      SegmentDisplay及十四段基础实现
├── Glow/         可见Active Geometry的光效增强
└── Marquee/      内容运动策略与动画生命周期
```

`MatrixDisplay`属于Matrix基础显示模块，并作为最终组合入口。Marquee只输出一帧中的内容放置位置，不读取字模、Geometry、Brush或Glow；Glow只处理Matrix已经选择出的可见Active Geometry，不读取Marquee状态。二者不得互相依赖。Segment首版不接入Marquee。

Matrix静态显示在Marquee关闭时必须独立、完整可用。关闭路径不创建Controller或Animation，不保留订阅、绘制命令或持续分配。

## 第一版公共契约

`MatrixDisplay`增加三个StyledProperty：

| 属性 | 类型 | 默认值 | 语义 |
|---|---|---:|---|
| `IsMarqueeEnabled` | `bool` | `false` | 是否启用自动单向穿屏 |
| `MarqueeSpeed` | `double` | `48` | 线性移动速度，单位DIP/秒 |
| `MarqueeRepeatDelay` | `TimeSpan` | `500ms` | 完整离开后到下一轮开始前的停顿 |

第一版不公开方向、缓动、重复次数、暂停、悬停暂停、手动Offset、完成事件、运动策略接口或模式枚举。内部有效速度限制为`0..10000 DIP/s`，重复间隔限制为`0..1分钟`；非法或越界输入只影响内部有效值，不回写StyledProperty。

## 单向穿屏语义

启用后无论文本长短均执行相同规则：

1. 整段文字从内容视口右侧外部开始；
2. 以恒定速度从右向左移动；
3. 整段文字完整离开内容视口左侧；
4. 保持离开位置等待`MarqueeRepeatDelay`；
5. 从右侧外部开始下一轮。

第一轮立即开始，不应用前置延迟。Marquee运行时忽略`HorizontalContentAlignment`，保留`VerticalContentAlignment`；内部按`Clip`语义绘制且不回写`OverflowMode`。空文本、无效视口或有效速度为0时不启动动画。

## 内部扩展结构

```text
MatrixDisplay
    -> LEDMarqueeController：Avalonia Animation生命周期和进度
    -> IMarqueeMotion：无Avalonia绘制依赖的纯运动数学
    -> MarqueeRenderPlan：一帧中一个或多个内容放置位置
    -> Matrix可见字符剔除、Geometry和Render
```

首版只有`LeftThroughMarqueeMotion`。内部帧计划从第一版起允许多个放置位置，使后续官方连续首尾模式无需重写Matrix渲染组合流程；首版不向开发者开放策略注入，不使用反射、动态发现、DI或插件注册。

动画使用Avalonia Animation驱动内部归一化进度，不使用`DispatcherTimer`，也不按帧累加固定像素。Text、点尺寸、间距、Padding、边框、Bounds、速度或重复间隔变化时取消旧周期并从右侧重新开始。Detach、隐藏、禁用和空文本立即释放动画；重新进入可运行状态后从头开始。

## 渲染与性能契约

- Marquee只改变内容X变换，不修改`Text`，不进入layout或Geometry缓存键。
- 每帧继续使用现有二分查找，复杂度保持`O(log n + visible)`。
- 禁止为完整长文本创建位图或提交全部不可见字模。
- Glow跟随当前移动后的可见Geometry，并继续受内容视口裁剪。
- 预热后运动不得持续创建layout、字模Geometry或无界历史状态。
- Controller、Animation Style及控件实例必须在Detach后可回收。

## 验收

- 公共属性默认值、AXAML、失效语义和本地值优先级。
- 起点、穿过、完整离开、等待、循环及短文本一致行为。
- NaN、Infinity、负数、零、极大速度和极大间隔。
- Attach、Detach、隐藏、启停、Text/Bounds/参数变化和WeakReference回收。
- Clip、垂直对齐、Border、Inactive、Glow和不同DPI下的像素边界。
- 1000与10000字符窄视口保持相同数量级的可见绘制命令。
- Labs全量测试、Sample Release、真实Windows窗口长稳和win-x64 NativeAOT。

## 首轮实现验证结果

- Labs全量测试`405/405`通过，包含公共合同、纯运动数学、非法输入、AXAML、渲染位置、长文本视口剔除、600帧缓存稳定和Controller WeakReference释放。
- 100与10000字符在相同窄视口和中段进度下提交相同数量级的可见字模命令，不随完整文本长度线性增长。
- `net8.0`与`net10.0` Release双目标构建通过，0 warning、0 error。
- Sample Release构建通过；普通Win32产物和NativeAOT产物分别持续运行15秒，均未提前退出，关闭路径无异常。
- win-x64 NativeAOT发布成功。警告仍来自`AtomUI.Core/AppBuilderExtensions.cs`既有Win32反射配置，不来自Labs或Marquee。
- Headless后端不会随墙钟等待自动推进Avalonia渲染动画时钟，因此自动测试使用确定性的进度注入验证各位置Render；真实时钟运动由Win32 Smoke和最终人工视觉验收负责。
