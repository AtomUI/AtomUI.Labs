# LED Glow 首轮原型评估

> 文档状态：迁移参考。本文于 2026-07-20 从 AtomUI 仓库的 `dev-and-mark/modules/desktop-controls-labs` 复制到 AtomUI.Labs 并适配文档结构。`AtomUI.Labs.Controls.LED` 表示本仓库的目标设计；文中的“已实现”、验证数据及旧项目命令来自迁移前的 `AtomUI.Desktop.Controls.Labs` 参考实现，不表示当前仓库已经包含相应源码、测试或性能工具。

## 评估状态

本报告记录2026-07-11的首轮可行性Smoke，不是最终选型或正式性能结论。原型只存在于`AtomUI.Desktop.Controls.Labs.Performance`工具，不进入Labs运行时程序集。

## Avalonia公开API审计

Avalonia 12.0.5公开以下能力：

- `DrawingContext.PushEffect(IEffect, Rect)`：Effect只包围作用域内绘制命令；传入内容的预膨胀Bounds，DrawingContext按Effect输出Padding扩张。
- `BlurEffect.Radius`：仓库已有AXAML使用案例。
- `RenderTargetBitmap.CreateDrawingContext()`和`Bitmap.CopyPixels(...)`：允许建立CPU读回链路，但Avalonia高层公开API未提供独立的`Blur(Bitmap)`操作。

依据来自本机NuGet包`Avalonia.Base.xml`及编译/Headless Skia像素原型。仓库当前没有`.referenceprojects/Avalonia`源码镜像，因此不把未测量的Avalonia内部成本当作事实。

## 原型实现

### 路线A

`VectorExpansionGlowPrototype`使用4层缓存Pen和递增透明度，再绘制低透明度填充。它只使用公开Geometry绘制API。

### 路线B

独立的应用层Alpha Mask + CPU Gaussian Blur原型在Gate 1停止：公开API可以创建/读回位图，但需要AtomUI自行实现和维护像素模糊、边缘扩张、降采样、格式转换和多缓冲生命周期。该路径重复渲染后端能力且不符合第一版复杂度约束。

路线B的视觉语义没有被否定；Avalonia后端通过Scoped Blur Effect执行的Mask/Blur属于路线C候选，不需要AtomUI手写CPU模糊。

### 路线C

`ScopedBlurEffectGlowPrototype`在`PushOpacity`和`PushEffect(new BlurEffect { Radius = radius }, source.Bounds)`作用域中仅绘制GlowBrush Geometry，退出作用域后绘制清晰Active本体。没有设置整个Control的`Visual.Effect`，也没有新增子Visual。

Skia自定义`ICustomDrawOperation`没有启动。公开Scoped Effect已通过首轮可行性Gate，在其失败前没有理由承担后端耦合。

## 像素语料

首轮覆盖：

- Matrix：Circle、Square、RoundedSquare。
- Segment：Horizontal、Vertical、Diagonal、ColonDots。
- GlowRadius：6、12、24。
- RenderScaling：100%、125%、150%、200%。
- 两条可运行路线各84组，总计168组。

判定条件为源Geometry Bounds外存在可见非背景像素。结果：

| 路线 | 通过 | 总数 | 最少外部可见像素 |
|---|---:|---:|---:|
| A.VectorExpansion | 84 | 84 | 406 |
| C.ScopedBlurEffect | 84 | 84 | 108 |

该Gate只证明真实外扩，不证明视觉质量、图层隔离、缓存正确性或最终性能。

## 命令提交Smoke

场景为40x40 Circle、Radius 12、Opacity 0.65、600帧、单进程。数据包含每帧新建DrawingGroup的固定成本，不包含真实GPU展示；只能用于初筛。

| 路线 | 微秒/帧 | 字节/帧 | 相对无Glow耗时 | 相对无Glow分配 |
|---|---:|---:|---:|---:|
| Baseline.NoGlow | 5.88 | 1520.1 | 1.00x | 1.00x |
| A.VectorExpansion | 39.42 | 9920.1 | 6.70x | 6.53x |
| C.ScopedBlurEffect | 14.92 | 4008.1 | 2.54x | 2.64x |

不得把该单次Smoke描述为性能证明。正式比较仍需5个独立进程、600帧预热、6000帧测量和真实窗口数据。

## 首轮结论

- 路线A通过公开API和外扩像素Gate，保留为工程保底，但命令数和提交成本需要重点审计。
- 独立手写路线B在Gate 1停止，不进入运行时或后续长稳测试。
- 路线C的Scoped Blur Effect通过公开API和全部84组外扩像素Gate，进入下一轮图层隔离、Brush、裁剪和真实窗口验证。
- Skia自定义路线未获得启动条件。
- 当前没有最终技术选型，不修改Matrix或Segment公共API与Render路径。

## 下一轮

- 验证Background、Inactive和Border不进入Scoped Blur。
- 验证Solid、Alpha、LinearGradient、RadialGradient Brush。
- 验证Padding、Border内缘裁剪、ScaleDown和Radius随内容缩放。
- 增加无Glow、Opacity变化和Radius变化的Effect/命令缓存计数。
- 运行多进程性能审计和真实窗口视觉验收。

## 第二轮：Brush、隔离、裁剪与Scale

2026-07-11扩展原型后使用相同路线覆盖：

- 3种Matrix Geometry和4类Segment Geometry。
- Solid、Alpha Solid、LinearGradient、RadialGradient四类Brush。
- Radius 6、12、24。
- RenderScaling 100%、125%、150%、200%。
- 2条路线，共672组像素Case。

结果：路线A `336/336`、Scoped Blur `336/336`全部在源Geometry Bounds外产生可见像素。Gradient Brush在公开Scoped Effect路径中可运行，没有纯色强制转换。

Scoped Blur行为Gate：

| Gate | 结果 | 证据 |
|---|---|---|
| Brush/Opacity复用Effect | 通过 | EffectBuildCount保持1 |
| Radius更新复用当前Effect | 通过 | EffectBuildCount=1，RadiusUpdateCount=1 |
| 图层隔离 | 通过 | Background保持Black，Inactive保持Gray，Border保持Yellow |
| 严格裁剪 | 通过 | Clip Bounds外可见像素为0 |
| Glow跟随Content Scale | 通过 | Scale 1.0外扩5 DIP，Scale 0.5外扩3 DIP，比值0.600 |

图层颜色探针按`ILockedFramebuffer.Format`区分RGBA/BGRA，不能假定固定字节顺序。严格裁剪Gate验证的是最终像素不越界；Padding和真实Matrix Border内缘仍需在正式控件接入前成对验证。

### 五进程6000帧命令提交

每个进程都重新执行672组像素Gate，然后对40x40 Circle、Radius 12、Opacity 0.65测量6000帧DrawingGroup命令提交。该数据仍不包含真实GPU展示成本。

| 进程 | NoGlow μs/frame | 路线A μs/frame | Scoped Blur μs/frame |
|---:|---:|---:|---:|
| 1 | 7.58 | 45.43 | 11.38 |
| 2 | 7.97 | 44.38 | 10.64 |
| 3 | 7.99 | 46.76 | 12.21 |
| 4 | 6.99 | 43.54 | 11.75 |
| 5 | 7.21 | 43.93 | 11.98 |
| Median | 7.58 | 44.38 | 11.75 |
| P95（线性插值） | 7.99 | 46.50 | 12.16 |

每帧托管分配在五个进程中一致：NoGlow `1520` bytes、路线A `9920` bytes、Scoped Blur `4008` bytes。相对NoGlow中位命令提交耗时，路线A约`5.85x`，Scoped Blur约`1.55x`；相对固定DrawingGroup基线的新增分配分别为`8400`和`2488` bytes/frame。

这些分配主要来自原型每帧创建DrawingGroup和提交Effect/Opacity节点，尚未证明正式控件稳态分配契约。下一轮必须在真实控件复用Render路径和真实窗口渲染器中分离固定框架开销。

### 第二轮结论

- Scoped Blur通过本轮全部正确性Gate，继续作为主候选。
- 路线A仍通过正确性Gate，但命令提交中位耗时和分配都明显高于Scoped Blur，仅保留工程保底资格。
- 独立CPU路线B和Skia自定义路线仍无启动条件。
- 仍未完成真实窗口GPU成本、正式控件缓存生命周期和Glow关闭零成本证明，因此不能宣布最终选型。

## 桌面真实窗口原型入口

新增独立工具项目：

```text
tools/performances/
  AtomUI.Desktop.Controls.Labs.GlowPrototype.Desktop/
```

该项目引用Performance试验程序集并通过friend assembly复用同一份路线A与Scoped Blur Renderer，不复制算法，也不引用或修改Labs运行时Glow实现。它并排展示：

- 路线A与Scoped Blur。
- Radius 6、12、24。
- Circle、RoundedSquare、Segment diagonal和Colon dots。
- 灰色Inactive、白色Active、青色Glow和黄色裁剪边框。
- Radius 24下的严格Clip案例。

运行：

```powershell
dotnet run --project tools\performances\AtomUI.Desktop.Controls.Labs.GlowPrototype.Desktop\AtomUI.Desktop.Controls.Labs.GlowPrototype.Desktop.csproj -c Release
```

Release构建为0警告、0错误。短时真实Win32进程Smoke保持运行5秒且未提前退出；该结果只证明桌面生命周期和窗口建立成功，不代表人工视觉验收或真实GPU性能已经通过。

桌面原型代码不进入NuGet包。最终选型后，失败路线和只服务对比的桌面原型应删除；有长期价值的性能Case改为直接测试正式LEDGlowRenderer和真实Matrix/Segment。

## 第三轮：真实窗口Render回调压力Gate

桌面原型新增`--benchmark`自动化入口。每个进程只运行一条路线，创建64个独立Geometry实例，预热30次Dispatcher Tick，再执行120次测量Tick。每次Tick使全部实例失效，窗口使用真实Win32 Avalonia后端；进程完成后自动退出并写出原始报告。

该方法记录窗口Render回调和命令提交，但计时包含Dispatcher调度、失效合并、渲染线程及窗口后端工作，不是隔离的GPU计时。`Expected callbacks`是请求上限，不保证Avalonia必须逐请求绘制；完成率下降表示测量窗口内发生了失效合并或渲染未赶上请求速率，不等同于丢失业务状态。

运行示例：

```powershell
dotnet run --project tools/performances/AtomUI.Desktop.Controls.Labs.GlowPrototype.Desktop/AtomUI.Desktop.Controls.Labs.GlowPrototype.Desktop.csproj -c Release --no-build -- --benchmark --route scoped --instances 64 --warmup 30 --ticks 120
```

### 五个独立进程

| 路线 | 进程 | 耗时ms | Render回调完成率 | bytes/已完成回调 |
|---|---:|---:|---:|---:|
| NoGlow | 1 | 2449.16 | 100.00% | 632.9 |
| NoGlow | 2 | 2493.62 | 100.00% | 639.4 |
| NoGlow | 3 | 2498.92 | 100.00% | 632.8 |
| NoGlow | 4 | 2314.19 | 100.00% | 632.9 |
| NoGlow | 5 | 2581.39 | 100.00% | 660.7 |
| Vector | 1 | 2456.07 | 99.17% | 1430.4 |
| Vector | 2 | 2360.49 | 100.00% | 1429.9 |
| Vector | 3 | 2435.24 | 100.00% | 1458.8 |
| Vector | 4 | 2388.59 | 99.17% | 1431.7 |
| Vector | 5 | 2958.37 | 85.00% | 1433.8 |
| Scoped Blur | 1 | 2611.95 | 89.17% | 923.7 |
| Scoped Blur | 2 | 2722.65 | 85.83% | 926.1 |
| Scoped Blur | 3 | 2612.66 | 89.17% | 919.0 |
| Scoped Blur | 4 | 2606.79 | 85.83% | 932.2 |
| Scoped Blur | 5 | 2844.43 | 67.50% | 924.4 |
| NoGlow中位数 | - | 2493.62 | 100.00% | 632.9 |
| Vector中位数 | - | 2435.24 | 99.17% | 1431.7 |
| Scoped Blur中位数 | - | 2612.66 | 85.83% | 924.4 |

### 关闭路径契约

五个NoGlow进程的Glow提交数均为0，证明该原型在关闭Glow时通过空Renderer分支完全跳过Glow命令提交。该证据只适用于桌面试验宿主；正式控件接入后必须在Matrix和Segment各自的Render路径重新证明，不能直接继承本结论。

### 第三轮结论

- Scoped Blur每次已完成回调的托管分配中位数比Vector低约35.4%，方向与Headless命令提交测试一致。
- Scoped Blur的Render回调完成率中位数只有85.83%，低于Vector的99.17%；真实窗口证据不支持立即宣布Scoped Blur胜出。
- 当前测试无法把Dispatcher失效合并、CPU命令构造、渲染线程和GPU Effect成本拆开，因此不根据耗时列做纯GPU性能推断。
- NoGlow关闭路径契约通过，但仍没有正式控件零开销证据。
- Gate结论为`BLOCKED FOR PRODUCTION INTEGRATION`：保留Scoped Blur主候选和Vector保底候选，禁止把Glow接入正式Matrix/Segment，直到完成帧呈现时间来源审计、不同实例规模拐点测试和长稳资源生命周期测试。

## 第四轮：原因拆分与实例规模拐点

基准入口增加以下变量：

- `--instances`：1、4、16、32、64、128。
- `--hz`：0表示静态观察，1表示时钟类低频更新，60表示高频压力。
- `--radius`：0、6、12、24 DIP。
- `--route`：NoGlow、Geometry、Opacity、Vector、Scoped。
- `--topology batch`：在单一根Visual中绘制N个Geometry，减少多子Visual失效合并对原因定位的干扰。

Cells拓扑的NoGlow也会随机出现低于100%的回调完成率，证明旧指标混入了子Visual失效合并和Dispatcher波动。后续原因定位使用Batch拓扑；Cells拓扑只保留为接近多控件场景的系统压力证据。

### Batch拓扑实例阶梯

单进程、60Hz、Radius 12的探索数据如下。它用于定位需要重复验证的区间，不作为统计稳定的最终百分比：

| Geometry数量 | NoGlow | Vector | Scoped Blur |
|---:|---:|---:|---:|
| 1 | 85.00% | 93.33% | 93.33% |
| 4 | 91.67% | 93.33% | 91.67% |
| 16 | 90.00% | 83.33% | 90.00% |
| 32 | 100.00% | 98.33% | 100.00% |
| 64 | 98.33% | 100.00% | 88.33% |
| 128 | 88.33% | 83.33% | 38.33% |

由于Windows桌面调度存在明显单进程噪声，不能把1到32实例的非单调变化解释为算法复杂度。可信信号是Scoped在64到128规模重复出现明显下降，因此原因拆分聚焦这两个规模。

### Effect与Radius拆分

| Geometry数量 | Geometry | Opacity | Scoped R0 | Scoped R6 | Scoped R12 | Scoped R24 |
|---:|---:|---:|---:|---:|---:|---:|
| 64 | 86.67% | 95.00% | 88.33% | 91.67% | 85.00% | 86.67% |
| 128 | 100.00% | 100.00% | 80.00% | 71.67% | 63.33% | 63.33% |

128 Geometry时，Geometry和PushOpacity均完成100%，加入`PushEffect(BlurEffect Radius=0)`后降至80%。这证明主要固定压力从Effect作用域开始，不是Geometry或Opacity本身导致。Radius从0增至6后继续下降，但12到24没有继续下降；当前只能确认Effect固定成本和非零Blur均有影响，不能声称成本与Radius线性相关。

### 静态与1Hz常见负载

| 场景 | Geometry数量 | NoGlow | Scoped Blur |
|---|---:|---:|---:|
| 1Hz | 64 | 100% | 100% |
| 1Hz | 128 | 100% | 100% |
| 静态观察 | 128 | 0次持续Render、0次Glow提交 | 0次持续Render、0次Glow提交 |

Scoped Blur在64和128 Geometry的1Hz低频场景完成全部请求；静态场景没有持续Render或Glow提交。该结果支持时钟、仪表盘等低频LED显示的可行性，但不解除高频动画场景的规模限制。

### 当前原因判断与边界

- 已排除Geometry和PushOpacity是128 Geometry高频下降的主要原因。
- 已定位到Scoped Effect作用域存在显著固定压力，非零Blur进一步增加压力。
- 60Hz压力下的实用拐点位于32到64个Scoped Geometry之间；精确上限仍需围绕该区间做多进程重复。
- 1Hz下至少128 Geometry稳定，静态状态不持续重绘。
- 生产集成仍保持阻断：下一Gate是10分钟长稳、窗口反复创建关闭、Brush/Opacity/Radius切换、Glow启停和对象可回收验证。

## 第五轮：生命周期与长稳Gate

桌面原型新增`--lifecycle`模式，由协调窗口反复创建和关闭真实Win32子窗口。每个子窗口包含64个Scoped Glow Renderer，并在60Hz Tick中循环执行：

- Glow启用与关闭。
- Cyan与Magenta Brush切换。
- Opacity按0、0.2、0.4、0.6、0.8循环。
- Radius按0、6、12、24循环。
- Renderer复用同一BlurEffect并更新状态。
- 子窗口关闭时停止Timer、解绑Tick/Open/Closed事件并清空Content。

协调器对Window和Batch Surface只持有`WeakReference`，每20个窗口执行三轮`GC.Collect + WaitForPendingFinalizers`并记录稳定托管内存。最后静置500ms，避免把Closed事件调用栈和渲染后端延迟释放误判为泄漏。

### 校准

20窗口、每窗口16实例、每窗口2 Tick的首次即时GC判定残留最后1个Window和Surface。增加关闭后500ms静置后重新校准，残留均为0。这说明生命周期判定必须等待关闭事件调用栈和后端清理完成，不能在`Closed`回调内部立即断言回收。

### 长时运行与完整收尾

首次正式配置为600窗口、每窗口64实例、每窗口60 Tick。进程持续754秒无崩溃或未处理异常，但在外部命令超时前没有完成600窗口，因此该次运行只能作为超过12分钟的持续稳定性Smoke，不能作为最终弱引用回收证据。

随后运行可完整收尾的100窗口轮次：

| 指标 | 结果 |
|---|---:|
| 窗口周期 | 100 |
| 每窗口Renderer | 64 |
| 每窗口状态变更Tick | 60 |
| Retained Window | 0 |
| Retained Surface | 0 |
| 稳定内存采样 | 918632、914480、916800、920920、921896 bytes |
| 首尾差值 | +3264 bytes |
| 弱引用生命周期契约 | PASS |

稳定内存先下降再小幅波动，不是逐采样单调增长；首尾3264 bytes只占首个采样约0.36%。RSS未作为泄漏判据。Window和Surface全部回收，间接证明其持有的Renderer、BlurEffect、Brush和Geometry对象图没有被原型的托管引用永久保留。

### 第五轮结论

- 超过12分钟的真实窗口高频运行没有崩溃，但因外部超时不提供最终回收结论。
- 完整100窗口轮次通过弱引用和稳定托管内存Gate。
- 原型级Scoped Glow没有发现Window、Surface或Renderer对象图泄漏。
- 该结论不自动适用于正式Matrix/Segment。正式接入后必须重新覆盖控件Attach/Detach、Glow关闭路径、Theme/Token资源绑定和窗口关闭回收。
- 生命周期阻断项已解除；高频大规模Effect成本限制仍然存在。

## 第六轮：Effect粒度最终选型

在Scoped Blur路线内比较四种组织粒度：

- `PerGeometry`：每个Active Geometry一次`PushEffect`，作为旧基线。
- `PerControl`：每个控件一次`PushEffect`，在作用域内绘制全部已剔除的可见Active Geometry。
- `Batch8`、`Batch16`：每8或16个Geometry一次Effect，用于验证大Bounds是否需要折中。

相邻Active Geometry的Glow允许自然叠加融合。四种粒度每帧提交相同数量的Glow Geometry；32 Geometry计数校准中，每帧Effect作用域分别为32、1、4、2，证明比较没有通过减少源Geometry作弊。

### 五进程核心结果

| Geometry | 粒度 | 中位测量窗口ms | P95测量窗口ms | 中位回调完成率 | bytes/完成回调 |
|---:|---|---:|---:|---:|---:|
| 64 | PerGeometry | 2458.13 | 2909.66 | 99.17% | 35879.4 |
| 64 | PerControl | 2235.28 | 2472.88 | 100.00% | 25835.7 |
| 64 | Batch8 | 2401.19 | 2696.73 | 94.17% | 27717.9 |
| 64 | Batch16 | 2547.63 | 2666.24 | 90.83% | 26813.8 |
| 128 | PerGeometry | 2812.54 | 2911.08 | 46.67% | 65502.5 |
| 128 | PerControl | 2521.92 | 2647.78 | 90.83% | 44485.1 |
| 128 | Batch8 | 2687.26 | 2712.70 | 90.00% | 48375.7 |
| 128 | Batch16 | 2530.93 | 2684.96 | 88.33% | 46355.4 |

64 Geometry下，PerControl相对PerGeometry中位测量窗口降低约9.1%、P95降低约15.0%、完成回调分配降低约28.0%，Effect作用域从每帧64个降为1个。128 Geometry下，回调完成率中位数提高44.16个百分点，分配降低约32.1%，Effect作用域从每帧128个降为1个。

这些真实窗口耗时仍包含Dispatcher和渲染后端，不描述为纯GPU时间。Effect作用域和分配是结构性证据；完成率是相同机器、相同请求策略下的系统压力结果。

### 稀疏Bounds反证测试

64 Geometry间距从40 DIP扩大到80 DIP后运行五进程：

| 粒度 | 中位窗口ms | P95窗口ms | 中位完成率 | bytes/完成回调 |
|---|---:|---:|---:|---:|
| PerControl | 2494.54 | 2664.30 | 93.33% | 25944.6 |
| Batch8 | 2557.41 | 2617.01 | 92.50% | 27766.4 |
| Batch16 | 2611.72 | 2628.34 | 92.50% | 26769.9 |

PerControl的P95比最佳Batch16高约1.4%，没有达到预先约定的15%改选阈值；其中位完成率和分配仍为三者最佳。因此不引入Batch运行时复杂度。

### 上限与正确性

- 256 Geometry单进程压力Smoke：PerGeometry完成率27.50%，PerControl 91.67%，Batch8/16均为83.33%。该单进程结果只证明继续聚合的方向，不作为稳定百分比。
- PerControl在Radius 0、6、12、24下均可运行；128 Geometry、Radius 24、1Hz完成率100%，静态为0 Render、0 Glow提交、0 Effect作用域。
- Headless分组像素Gate覆盖100%、125%、150%、200% RenderScaling和Radius 6、12、24，共12/12通过。
- 原有672组像素、Brush、图层隔离、严格裁剪和Scale Gate继续全部通过。

### 最终粒度决议

- 正式候选固定为`PerControl Scoped Blur Effect`。
- 正式Renderer先执行包含GlowRadius的可见性剔除，再在一个Effect作用域内绘制当前控件全部可见Active Geometry。
- 相邻Geometry的Glow允许自然融合；退出Effect后重新绘制清晰Active本体。
- `PerGeometry`、`Batch8`和`Batch16`不进入正式运行时，不增加粒度公开属性，也不根据运行负载动态切换算法。
- 该决议结束技术路线与Effect粒度游移。下一阶段直接设计共享`LEDGlowRenderer`并接入Matrix/Segment。

## 第七轮：正式控件接入

Scoped Blur与PerControl粒度已进入`AtomUI.Desktop.Controls.Labs`正式运行时。共享实现位于`LED/Glow`，Matrix和Segment不复制Effect创建、数值规整或作用域释放逻辑。

正式公共契约：

| 控件 | 属性 | 默认值 | 有效语义 |
|---|---|---:|---|
| MatrixDisplay | `GlowBrush` | `null` | null关闭Glow |
| MatrixDisplay | `GlowOpacity` | `0.35` | 内部规整为0..1 |
| MatrixDisplay | `GlowRadius` | `6` | 内部规整为0..24 DIP，0关闭 |
| SegmentDisplay | `GlowBrush` | `null` | null关闭Glow |
| SegmentDisplay | `GlowOpacity` | `0.35` | 内部规整为0..1 |
| SegmentDisplay | `GlowRadius` | `6` | 内部规整为0..24 DIP，0关闭 |

原始StyledProperty值不被内部规整回写。NaN和正负Infinity按0处理。Segment旧同形叠色Glow已删除，不保留兼容开关。

正式Render顺序固定为Background、Inactive、一次Scoped Glow、清晰Active、Matrix Border。Matrix和Segment均先按内容视口加有效GlowRadius执行可见字符剔除，再计算可见Active Geometry联合Bounds；不可见长文本不进入Effect。Glow处于现有内容Clip和布局Transform中，不参与Measure，ScaleDown同时缩放Geometry和Glow语义。

关闭路径采用惰性Renderer：控件构造和`GlowBrush=null`稳态不创建`LEDGlowRenderer`或`BlurEffect`，不提交Effect作用域。首次有效Glow创建一个BlurEffect；Brush、Opacity和Radius变化复用同一实例。`GlowBrush`清回null时释放Renderer及其BlurEffect引用。

正式测试从334项增加到371项，新增覆盖：

- Matrix与Segment公共属性名称、默认值和配置值。
- Opacity 8组、Radius 11组数值边界。
- Glow关闭零Effect、单Render单Effect、Brush/Opacity/Radius复用Effect。
- GlowBrush清空释放Renderer状态。
- Matrix与Segment在100%/200% RenderScaling、Radius 6/24下的8组真实像素比较。
- Matrix Border像素保持不变，Active白色核心保持清晰。

Labs Sample增加Matrix默认关闭、Radius 6/12/24和多色Glow案例，以及Segment Radius 6/12/24案例。Release构建和真实Win32窗口8秒启动Smoke通过。

## 第八轮：正式控件性能门禁

性能工具新增`--formal-glow`入口，直接测量正式`MatrixDisplay`和`SegmentDisplay`，不再以原型Renderer代替。固定文本为16字符、视口800x140，覆盖：

- `DisabledNullBrush`：`GlowBrush=null`。
- `DisabledZeroOpacity`：Brush非空、`GlowOpacity=0`。
- `Static`：Radius 6、Opacity 0.35。
- `OpacityAnimation`：0..1循环。
- `RadiusAnimation`：0..24往返。

每场景先执行100帧控件预热；完整测量函数再预热1000帧以跨过Tiered JIT阈值，正式测量6000帧。首批只预热10帧的数据出现第一个Matrix NullBrush场景异常偏慢，确认为顺序/JIT污染并作废，不进入正式结论。

### 五进程正式结果

| 控件 | 模式 | Median μs/frame | P95 μs/frame | bytes/frame |
|---|---|---:|---:|---:|
| Matrix | DisabledNullBrush | 86.56 | 96.44 | 88560.0 |
| Matrix | DisabledZeroOpacity | 91.97 | 99.52 | 88560.0 |
| Matrix | Static | 139.24 | 143.57 | 132352.0 |
| Matrix | OpacityAnimation | 141.16 | 151.32 | 132067.1 |
| Matrix | RadiusAnimation | 138.02 | 151.18 | 130717.3 |
| Segment | DisabledNullBrush | 138.30 | 152.72 | 169104.0 |
| Segment | DisabledZeroOpacity | 144.66 | 163.65 | 169104.0 |
| Segment | Static | 208.59 | 229.40 | 242760.0 |
| Segment | OpacityAnimation | 214.94 | 218.80 | 242226.2 |
| Segment | RadiusAnimation | 210.41 | 220.13 | 237992.9 |

数据是Headless Skia下DrawingGroup命令提交，包含DrawingGroup和GeometryDrawing分配，不是隔离GPU呈现成本。动画模式经过Opacity或Radius为0的帧会跳过Glow，因此其平均分配可能略低于静态Glow，不能解释为动画比静态渲染更便宜。

### 关闭路径配对门禁

为排除同进程前后环境漂移，每轮在同一循环内交替执行NullBrush与ZeroOpacity，并逐帧使用`Stopwatch.GetTimestamp`累计。五进程结果：

| 控件 | Median slower/faster | P95 | 最大值 | 1.05x Gate |
|---|---:|---:|---:|---|
| Matrix | 1.002 | 1.010 | 1.012 | PASS |
| Segment | 1.002 | 1.041 | 1.049 | PASS |

两个关闭模式的`bytes/frame`、Geometry命令数完全相同，EffectBuildCount和EffectScopeCount均为0。静态Glow在预热后的EffectBuild增量为0，每个正式测量帧恰好一个Effect作用域。Opacity与Radius动画不重建BlurEffect；作用域数只在有效值为0的帧减少。

运行方式：

```powershell
dotnet run --project tools/performances/AtomUI.Desktop.Controls.Labs.Performance/AtomUI.Desktop.Controls.Labs.Performance.csproj -c Release --no-build -- --formal-glow --frames 6000 --markdown output/formal-glow.md
```

## 第九轮：正式控件真实Win32窗口门禁

桌面性能宿主新增`--formal-controls`，真实创建`MatrixDisplay`或`SegmentDisplay`控件，而不是绘制原型Geometry。模式语义严格分离：

- `noglow`：Glow关闭，只请求重绘。
- `static`：静态Glow，只请求重绘。
- `opacity`：只改变GlowOpacity。
- `radius`：只改变GlowRadius。
- `dynamictext`：静态Glow并改变Text，包含布局和Geometry更新成本。

早期校准把Static与动态Text绑定，名称与负载不一致，该批数据作废。修正后窗口固定为1600x1100，确保50实例均处于可见布局区域。每场景预热30 Tick、测量120 Tick；10实例核心场景运行5个独立进程。

### 10实例、60Hz五进程

| 控件 | 模式 | 中位Render回调完成率 | P5完成率 | bytes/完成回调中位数 |
|---|---|---:|---:|---:|
| Matrix | NoGlow | 100.00% | 99.34% | 3447.1 |
| Matrix | Static | 99.17% | 95.83% | 4795.0 |
| Matrix | Opacity | 100.00% | 100.00% | 4857.6 |
| Matrix | Radius | 100.00% | 100.00% | 4884.5 |
| Matrix | DynamicText | 100.00% | - | 5317.6 |
| Segment | NoGlow | 61.67% | 57.00% | 6699.6 |
| Segment | Static | 63.33% | 54.83% | 8618.8 |
| Segment | Opacity | 61.67% | 60.17% | 8729.1 |
| Segment | Radius | 78.33% | 70.50% | 8714.4 |
| Segment | DynamicText | 63.33% | - | 9894.6 |

Render回调完成率表示测量窗口内实际Render回调与失效请求上限之比。低于100%说明Avalonia合并失效或渲染未赶上请求速率，不表示最终属性状态丢失。Radius动画经过0附近时会跳过Effect，因此Segment Radius完成率高于持续有效Glow，不能解释为Radius动画本身更便宜。

Segment在10实例60Hz时，NoGlow、Static和DynamicText均处于约62%同一量级；主要压力来自每个Segment控件基础层约179个Geometry命令，Glow不是独立性能断崖。Matrix在相同条件下全部核心模式接近或达到100%。

### 实例阶梯与常见负载

单进程60Hz探索：

| 实例 | Matrix NoGlow | Matrix Static | Segment NoGlow | Segment Static |
|---:|---:|---:|---:|---:|
| 1 | 99.17% | 96.67% | 100.00% | 100.00% |
| 10 | 96.67% | 98.33% | 51.67% | 50.83% |
| 50 | 65.83% | 50.83% | 12.50% | 10.00% |

该表是拐点探索，不作为统计稳定百分比。50实例、1Hz下Matrix/Segment的NoGlow与Static四个场景均完成100%，支持时钟和仪表盘等低频常见负载。50实例60Hz属于明确压力场景，不应承诺满帧。

运行示例：

```powershell
dotnet run --project tools/performances/AtomUI.Desktop.Controls.Labs.GlowPrototype.Desktop/AtomUI.Desktop.Controls.Labs.GlowPrototype.Desktop.csproj -c Release --no-build -- --formal-controls --control matrix --mode opacity --instances 10 --hz 60 --warmup 30 --ticks 120
```

## 第十轮：Segment基础Geometry命令聚合

真实窗口门禁定位到Segment在NoGlow和Glow下均受大量Geometry命令限制。旧实现对每个段分别提交命令：16字符正式性能场景中NoGlow为179条、Glow为258条。优化后按当前可见范围构建并缓存两个`GeometryGroup`：

- Inactive层聚合为一个Geometry命令。
- Active层聚合为一个Geometry命令，同一Geometry同时供Glow和清晰Active绘制。
- Background仍独立；因此无背景正式性能场景NoGlow为2条、Glow为3条。
- 仍然先按视口加GlowRadius剔除，100与1000字符窄视口命令数保持一致。

聚合缓存键包含LayoutCacheVersion、GeometryCacheVersion和可见索引范围。Brush、GlowBrush、Opacity和Radius变化不重建聚合Geometry；Text模式变化重建Active聚合但复用基础段Geometry。

第一轮同时重建Active与Inactive聚合，虽然静态和动画性能显著改善，但DynamicText分配从优化前约9894.6上升到22394 B/完成回调，因此不能收工。第二轮把缓存所有权拆开：Inactive仅由GeometryVersion和可见范围决定，同槽位数字文本变化继续复用；Active随LayoutVersion更新。不保留历史文本缓存，长期只持有当前Active和当前Inactive各一代。

### Headless五进程结果

| 模式 | 优化前Median μs/frame | 优化后Median | 优化前bytes/frame | 优化后bytes/frame | 命令数 |
|---|---:|---:|---:|---:|---:|
| DisabledNullBrush | 138.30 | 11.64 | 169104.0 | 5472.0 | 179→2 |
| DisabledZeroOpacity | 144.66 | 11.30 | 169104.0 | 5472.0 | 179→2 |
| Static | 208.59 | 11.56 | 242760.0 | 7768.0 | 258→3 |
| OpacityAnimation | 214.94 | 14.50 | 242226.2 | 8396.9 | 258→3 |
| RadiusAnimation | 210.41 | 16.39 | 237992.9 | 9219.9 | 258→3 |

命令结构降幅约98.8%；静态Glow中位命令提交耗时降低约94.5%，分配降低约96.8%。聚合后单帧耗时进入约5到16微秒区间，原逐帧时间戳配对门禁分辨率不足并出现一次1.078误报；改为每个时间戳测16帧批次后，五进程关闭门禁全部通过，最大slower/faster为1.040。

### 正式Win32窗口10实例、60Hz五进程

| 模式 | 优化前中位完成率 | 优化后 | 提升 | 优化前bytes/回调 | 优化后 | 分配改善 |
|---|---:|---:|---:|---:|---:|---:|
| NoGlow | 61.67% | 91.67% | +30.00pp | 6699.6 | 1474.6 | 78.0% |
| Static | 63.33% | 87.50% | +24.17pp | 8618.8 | 1831.3 | 78.8% |
| Opacity | 61.67% | 88.33% | +26.66pp | 8729.1 | 1916.5 | 78.0% |
| Radius | 78.33% | 90.83% | +12.50pp | 8714.4 | 1966.1 | 77.4% |
| DynamicText | 63.33% | 81.67% | +18.34pp | 9894.6 | 8893.9 | 10.1% |

生命周期测试证明Pattern变化后旧Active聚合可回收、Inactive聚合复用；Geometry参数变化后旧Inactive聚合可回收。Segment专项测试146项、Labs全量376项、Sample、win-x64 NativeAOT和真实窗口Smoke均通过。

## 第十一轮：视觉验收工作台与动画契约

Labs Sample新增`GlowWorkbench`工程验收面板，同屏显示Matrix与Segment，并提供：

- Enabled开关；关闭时取消动画并设置`GlowBrush=null`。
- Cyan、Red、Magenta、Green、Amber五种Brush；Red使用高饱和颜色，便于直观确认Brush切换生效。
- Opacity 0..1滑块。
- Radius 0..24滑块。
- Static、Breathe、Pulse三种模式。

工作台不增加Labs公共API。动画直接使用Avalonia`Animation`和现有`GlowOpacity`、`GlowRadius` StyledProperty，不使用DispatcherTimer或Sample自制插值器。切换配置先取消并释放旧CancellationTokenSource，窗口关闭时解除控件事件并取消动画。

动画周期与参数关系为：

```text
Breathe: 2.4秒循环，Opacity 用户值×35% -> 用户值 -> 用户值×35%
                       Radius 保持用户值
Pulse:   1.2秒循环，Opacity 用户值×50% -> 用户值 -> 用户值×50%
                       Radius 用户值×50% -> 用户值 -> 用户值×50%
```

初版默认模式为Breathe，使Sample启动Smoke实际覆盖无限动画运行和取消路径。真实Win32窗口持续10秒未提前退出，关闭时无异常；Sample Release和win-x64 NativeAOT发布通过。

交互复核发现初版动画关键帧使用固定Opacity和Radius，导致动画优先级覆盖滑块值，Workbench展示的可调契约与实际行为不一致。修正后默认模式改为Static，所有选项直接映射到控件属性；Breathe以用户Opacity的35%到100%循环并保持用户Radius，Pulse以用户Opacity和Radius的50%到100%循环。动画模式不再忽略用户参数。

人工视觉验收需要在工作台中比较同一参数下Matrix和Segment，重点观察深色背景上的灯珠融合、Segment斜段边缘、Radius 12以上的字符间融合以及Opacity 0.6附近是否刺眼。自动像素测试继续负责Active核心清晰、Border不受Blur和DPI裁剪，不替代审美判断。
