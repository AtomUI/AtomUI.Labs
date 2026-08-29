# ImageGallery 缩略图虚拟化测试与性能验收设计

> 文档状态：实现后验证基线，更新于 2026-08-19。首版 `VirtualizingStackPanel` 专用面板、容器回收、16 pixel 缩略图分档、真实默认模板路径、10K/100K 远距离选择的 `Rmax` 自动闸门，以及单固定种子 30 分钟真实默认模板 Headless 长稳已实现并通过；多固定种子异步乱序模型和真实图片/DPI 人眼矩阵仍是发布操作要求，不表示最终 RC 已签署。

本文是 [ImageGallery 首版施工合同](implementation-contract.md) 的专项验证文档，设计理由和推导仍见 [ImageGallery 控件设计](overview.md)。验证对象是缩略图走廊已经确定的固定槽位、单轴虚拟化、Overscan、容器回收、异步加载调度、Lease 生命周期和内存预算。本文不重新定义这些产品行为，也不以测试便利性修改公共 API。

## 核心验收原则

缩略图走廊的工业级验收只接受以下结论：

> 数据总量可以增长，但缩略图容器、解码资源、活跃请求和缓存不得随数据总量线性增长。

必须区分两类资源：

```text
允许随 ItemsSource 数量 N 增长
├── 轻量 Descriptor
├── Key 与 Title
└── IImageGallerySource 引用

必须由走廊 Viewport 和预算约束
├── 已实现的 ImageGalleryThumbnailItem 容器
├── 已解码缩略图资源
├── Visible / Overscan 消费者
├── Queued / Running 加载任务
├── ImageGalleryImageLease
└── 机会性缩略图缓存
```

首版采用保存全部轻量 Descriptor 的直接集合模型，因此不能伪称总内存与 `N` 无关。虚拟化承诺只约束视觉容器、图片加载、解码资源和与视口相关的运行时状态。

## 测试规模与数据矩阵

自动测试不能在仓库中保存十万张真实图片。海量集合由确定性测试项目和可控图片源生成；少量精选真实图片只用于解码、像素、原生资源和真实桌面验证。

| 层级 | 项目数量 | 用途 |
|---|---:|---|
| 边界 | 0、1、2、6、30 | 空集合、单项、可视边界和短集合行为 |
| 常规 | 100、1,000 | 日常功能、滚动、选择和完整交互 |
| 大型 | 10,000 | 每次 PR 必须覆盖的虚拟化验证 |
| 极限 | 100,000 | Nightly 压力测试和发布前验收 |
| 算法极限 | 1,000,000、`int.MaxValue` 附近 | 只验证索引、范围和溢出保护，不构造等量 UI |

规模测试至少交叉覆盖：

- `Top`、`Bottom`、`Left`、`Right` 四种 `ThumbnailFilmstripPlacement`。
- `1.0`、`1.25`、`1.5`、`2.0` Render Scaling。
- `80`、`120`、`240` DIP 的 `ThumbnailFilmstripExtent`。
- `48`、`96`、`257` DIP 的 `ThumbnailItemExtent`。
- `0`、`1`、`8`、`31` DIP 的 `ThumbnailItemSpacing`。
- `0`、`4`、非对称 Thickness 的 `ThumbnailItemAppearance.Padding`，确认图片内容尺寸变化不污染主轴步长。
- 刚好容纳、相差 `1 DIP`、首尾部分可见和只能部分显示末项的 Viewport 边界。
- 即时、延迟、失败、响应取消、忽略取消及乱序完成的图片源。
- `0%`、`1%`、`25%`、`100%` 的受控加载失败率。

算法极限测试的所有加减乘除必须采用不会在中间步骤溢出的实现；不能因为公共索引最终为 `int` 就允许 `offset + length` 等中间表达式溢出后再 Clamp。

## 容器数量硬上限

设：

```text
L = 缩略图走廊主轴 Viewport 长度
E = ThumbnailItemExtent
S = ThumbnailItemSpacing
P = E + S，即 SlotStride
首版 Overscan = 主轴前后各 0.5L
```

稳定布局后的实际容器上限固定为：

```text
Rmax = ceil(2L / P) + 4
```

`+4` 是边缘相交、布局取整和固定保护槽位的最大常量余量，不能从项目总数、滚动距离或选择索引派生。以 `L = 576 DIP`、`E = 96 DIP`、`S = 8 DIP` 为例，可视槽位约为 6 个，包含两侧 Overscan 后约为 12 个，稳态实际容器不得超过 16 个。

硬性规则如下：

- 在相同 Viewport、Item Extent 和 Item Spacing 下，100、10,000、100,000 项具有相同的 `Rmax`。
- 远距离重新定位时允许旧窗口和新窗口短暂共存，但瞬时容器峰值不得超过 `2 × Rmax`。
- 最多经过两个 Dispatcher/Layout 周期，实际容器数必须重新收敛到 `Rmax`。
- 从索引 10 跳到索引 90,000 时，不得实现或逐项遍历中间的 89,990 个视觉容器。
- 当前选中项被用户主动滚出走廊后，不能为了保持选择而永久固定其容器或 Lease。
- 以上数量属于精确正确性闸门，不因机器速度或 Benchmark 结果而放宽。

如果最终 Avalonia 虚拟面板的实际行为无法满足该上限，应先证明额外容器的必要性并修改设计文档，不能在测试中静默增加一个与数据量有关的容差。

## 专用测试基础设施

### 可控图片源

测试项目提供 `ControllableImageGallerySource` 或职责等价的确定性测试替身，至少能够：

- 立即成功。
- 由测试代码手动决定完成时机。
- 正常响应取消。
- 故意忽略取消并迟到返回。
- 返回受控失败。
- 返回指定 `EstimatedMemorySizeBytes` 的 Lease。
- 让多个项目共享同一 Cache Key。
- 按与请求顺序不同的任意顺序完成。

自动测试禁止依赖公网、真实 OSS 或不可控远程服务。HTTP 语义如需覆盖，应使用进程内可控 Handler；测试结果必须可离线、可重复。

### Lease 探针

每个测试 Lease 和底层图片资源至少记录：

```text
CreatedCount
AcquireCount
ReleaseCount
LiveReferenceCount
DuplicateReleaseCount
ReleaseCallbackInvokeCount
PostDisposeImageAccessCount
EstimatedMemorySizeBytes
```

释放断言必须针对统一 Lease 协议：每个已取得 Lease 的 Release Callback 最终恰好执行一次，重复或并发 Dispose 不得二次执行；释放后的 `Image` 访问必须失败；共享资源不能因任一消费者退出而提前销毁。

### 可控时钟与调度

失败抑制、稳定延迟和异步顺序测试使用可注入的内部时钟与可控完成点，不使用真实 `Task.Delay` 或墙钟等待制造偶发测试。测试应能够在一个线程中明确推进：请求入队、请求开始、取消、结果返回、Dispatcher 执行和 Layout 收敛。

### 内部诊断快照

通过 `InternalsVisibleTo` 向测试程序集提供只读诊断快照，至少包含：

```text
DescriptorCount
RealizedContainerCount
CreatedContainerCount
RecycledContainerCount
VisibleDemandCount
OverscanDemandCount
QueuedThumbnailLoadCount
RunningThumbnailLoadCount
PeakConcurrentThumbnailLoads
CacheEntryCount
CacheEstimatedBytes
LiveLeaseCount
RejectedStaleResultCount
```

诊断快照不是公共调试 API，不能进入 ImageGallery 的稳定公共合同。计数器只用于观察真实生产路径，测试不能调用一套仅为通过测试而存在的替代虚拟化实现。

## 确定性正确性测试

### 可视窗口与范围计算

至少覆盖：

- 集合为空、短于 Viewport、刚好填满和超出半个槽位。
- 任意 Offset 对应的 Visible 与 Overscan 索引范围。
- `N × E + (N - 1) × S` 的滚动总长、首末项无额外 Spacing 和 `SlotStride` 远距离定位。
- 集合首尾 Clamp 和部分可见项目。
- 横向与纵向范围算法的一致性。
- 非整数 Render Scaling 下的取整。
- 极大索引、极大长度和极端 Offset 不发生整数溢出。
- 非法 `ThumbnailItemExtent` 在进入范围算法前按照配置合同报错。
- 非法 `ThumbnailFilmstripExtent`、`ThumbnailItemSpacing` 和选中指示器 Thickness 按配置合同报错。

纯算法测试不能代替真实模板测试；它只负责为边界数学提供快速、穷举式证据。

### 远距离选择与定位

在 100,000 项集合中至少执行：

```text
0 -> 99,999 -> 50,000 -> 1 -> 90,000
```

每次定位必须证明：

- 中间项目没有被实现为容器。
- 中间缩略图没有产生加载需求。
- 目标项最终完整可见。
- Selection Identity 和最终容器身份一致。
- 根据固定槽位完成估算定位后，只进行一次基于实际 Bounds 的精确修正。
- 只执行最小必要滚动，不无条件居中。
- 访问项目和产生需求的次数由旧、新两个虚拟窗口约束，不能与跳转距离成正比。

### 快速滚动与容器复用

在旧请求尚未完成时连续滚动数百个虚拟窗口，再以逆序或随机顺序完成全部旧请求。硬性断言：

- 旧结果提交到新 Descriptor 容器的次数为 `0`。
- `BindingGeneration`、Item Identity、Source Identity 或消费者资格任一不匹配时拒绝结果。
- 被拒绝结果对应的消费者 Lease 立即释放。
- 复用后的容器不残留旧标题、旧图片、`:loading`、`:ready`、`:error` 或 `:selected`。
- 相同配置反复往返完成暖机后，`CreatedContainerCount` 不再随循环次数持续增长。

### 并发调度与优先级

分别以 `MaximumConcurrentThumbnailLoads = 1`、`4`、`16` 运行：

- `PeakConcurrentThumbnailLoads` 永远不得超过配置值。
- 当前主图使用独立最高优先级通道，不得被缩略图队列占满后阻塞。
- Selected Thumbnail 优先于 Visible，Visible 优先于 Overscan。
- 相同 Cache Key 的多个消费者只启动一个底层任务。
- 最后一个消费者离开后，未开始任务立即移出队列；运行任务收到取消。
- 旧任务即使忽略取消并迟到完成，也不得污染当前容器、当前选择或当前缓存资格。
- 请求从 Overscan 晋升为 Visible 或 Selected 时提升现有共享需求，不创建第二个底层任务。

取消、消费者归零和过期结果不得计入缩略图 `Error`。

### 缓存与预算

使用 1 MiB、8 MiB、32 MiB、40 MiB 等受控 Lease 验证：

- 缓存额外持有的唯一解码资源估算总量不得超过配置预算。
- Visible 和 Overscan 容器持有的消费者 Lease 不计入机会性缓存预算，但必须有明确消费者。
- 超预算时按照 LRU 或已确定的等价顺序释放缓存引用。
- 运行中缩小预算后立即收敛；增大预算不得触发全量预加载。
- 预算为 `0` 时，项目离开需求窗口且没有其他消费者后不再由缓存持有。
- 同一共享解码资源即使有多个消费者也只计入一次；不同解码尺寸分别计入。
- 缓存淘汰不得 Dispose 仍由可视容器使用的共享图片。
- 超过预算的单个非当前结果不得为了已经完成的工作强行进入缓存。

### 生命周期与集合变化

必须组合覆盖：

- 容器回收。
- Add、Remove、Move、Replace、Reset。
- Placement 以及合法 `ThumbnailFilmstripExtent`、`ThumbnailItemExtent`、`ThumbnailItemSpacing` 变化。
- Gallery Detach/Attach。
- 根控件模板重建与内部模板部件重建。
- 快速选择变化。
- 应用关闭等价的最终清理。

集合合同还必须单独证明：

- 普通有限数组或 `List<T>` 在设置时只建立一次快照；未发送集合通知的后续原地修改不会被伪装成自动更新能力。
- `ObservableCollection<IImageGalleryItem>` 的 Add、Remove、Move、Replace 和 Reset 均通过真实 `INotifyCollectionChanged` 链路驱动 Descriptor、选择及虚拟范围更新。
- 分批追加后，全部业务项与轻量 Descriptor 数量按 `N` 增长，但稳定视觉容器、活跃加载和解码资源仍遵守与 Viewport、Overscan 和预算有关的既有上限。
- 从后台线程设置 `ItemsSource` 或发送集合变化通知时抛出明确 `InvalidOperationException`，且内部 Descriptor、Selection、请求版本和可视容器不发生部分提交；测试不得通过测试替身自动切回 UI 线程掩盖违规调用。
- 有限延迟枚举器在设置时被完整消费一次；枚举过程不得触发网络等待或用无限枚举冒充分页 Provider。分页测试使用“后台准备有限批次、UI 线程追加”的真实合同。

Gallery Detach 并清空缓存后，必须满足：

```text
Running load          = 0
Registered consumer   = 0
Live Lease            = 0
Release callback      = 每个 Lease 恰好一次
Post-dispose Image access = 零次
```

迟到结果不能重新填充已经 Detach 的 Gallery。再次 Attach 只恢复当前选择和新 Visible/Overscan 窗口的合法需求，不恢复已经失效的旧容器消费者。

### 确定性随机状态测试

测试项目建立一个不依赖控件实现的轻量参考模型，并随机执行：

```text
Scroll
Select
Add / Remove / Move / Replace / Reset
Resize
Placement change
ThumbnailFilmstripExtent / ThumbnailItemExtent / ThumbnailItemSpacing change
Cache budget change
Detach / Attach
Complete / fail / cancel pending load
```

每次 PR 至少执行 100 个固定随机种子，每个种子 2,000 个操作。Nightly 扩大操作总量并增加种子。失败输出必须包含种子、配置、集合摘要、全部操作历史和最后诊断快照，使同一失败能够百分之百复现；禁止只报告“偶发失败”。

## 三层执行策略

### PR 硬闸门

每次提交至少运行最大 10,000 项的：

- 全部确定性正确性测试。
- 稳态和瞬时容器上限。
- 并发上限与共享请求合并。
- 迟到结果隔离和 BindingGeneration。
- Lease 所有权平衡。
- 缓存预算。
- 远距离跳转复杂度。
- 真实编译 AXAML 与默认主题链路。

PR 闸门主要使用计数、身份、状态和布局周期等确定性断言，不以墙钟耗时作为普通测试成败依据，避免用机器快慢掩盖逻辑错误或制造随机失败。

### Nightly 压力与长稳

Nightly 至少运行：

- 100,000 个轻量 Descriptor。
- 不少于 100,000 次滚动、选择和容器回收操作。
- 多组固定随机种子和异步乱序完成。
- 30 分钟连续长稳。
- 暖机后的相同区间反复往返。
- 完整 GC 后的弱引用和资源探针检查。

暖机后新增容器数量必须形成平台，不得随往返次数继续增长。进程 Working Set、Private Bytes 和托管堆大小只作为趋势指标，不能单独判定泄漏；硬性泄漏判据是消费者、Lease、缓存引用、解码图片资源和失去外部引用的容器能否正确归零或被回收。

### Release 真实桌面验收

在记录了 CPU、内存、GPU、Windows 版本、.NET SDK、Avalonia/AtomUI 版本、主题、DPI 和构建配置的固定参考环境中运行：

- 100,000 项轻量数据源。
- 小型、4K、8K、横幅、竖图、透明图片和损坏图片。
- 真实缩略图读取与解码。
- 快速滚动、远距离选择和连续相邻选择。
- Release 构建及 NativeAOT 真实宿主。

首版性能目标为：

- 快速滚动期间 p95 帧耗时不超过 `16.67 ms`。
- p99 帧耗时不超过 `33.3 ms`。
- 不出现超过 `100 ms` 的 UI 线程停顿。
- 图片读取和解码不允许同步阻塞 UI 线程。

前三项在第一版实现完成、测量链路校准并形成稳定基线后，才转为固定参考环境上的发布硬闸门；在此之前它们是明确目标，不能伪装成已经验证的性能承诺。同步阻塞 UI 线程和突破资源正确性上限从首次实现起就是硬失败。

## Benchmark 与回归判定

墙钟 Benchmark 必须在独立的非打包性能入口运行，不能把易受机器噪声影响的耗时断言塞入普通单元测试。至少测量：

- Descriptor 建立和 Reset。
- 可视范围计算。
- 相邻窗口滚动。
- 远距离选择与两阶段定位。
- 容器回收和重新绑定。
- 请求入队、提升、取消和共享合并。
- 缓存命中、接纳和淘汰。
- 每项操作的托管分配、GC 和存活资源趋势。

第一版实现形成基线后，保存测试环境、输入规模、暖机参数、原始结果和摘要。后续相同环境中的回归规则为：

- 中位耗时回退超过 `15%`，必须失败或进行书面性能审查。
- 单次操作托管分配增长超过 `10%`，必须失败或说明原因并更新基线。
- 出现新的 Gen2/LOH 持续增长，直接阻止发布。
- 实际容器数、活跃请求数、缓存字节数突破正确性上限时直接失败，不能用机器差异解释。
- Benchmark 至少独立运行三轮，以中位结果判定，不能只挑选最好的一轮。
- 更新基线必须连同原因、环境差异和前后原始数据一起评审，不能仅覆盖旧文件。

原生 Bitmap 可能主要占用非托管资源，不能只看 `GC.GetTotalMemory` 宣称没有泄漏。真实图片验收必须结合 Lease 探针、Dispose 计数、WeakReference、进程内存趋势和进程隔离复跑。

## 反作弊与证据交叉核对

完整虚拟化测试必须同时核对：

1. 内部诊断快照中的容器和需求计数。
2. 实际 Visual Tree 中的 `ImageGalleryThumbnailItem` 数量和身份。
3. 图片源实际收到的加载、取消和完成次数。

不能只相信单一计数器，也不能通过关闭图片加载、缩小 ItemsSource、直接调用内部范围函数或替换默认模板来冒充完整测试。至少一组 PR 测试和全部 Release 验收必须通过真实编译 AXAML、包内固定默认 `ControlTheme`、内部 `ScrollViewer`、`ItemsPresenter` 和虚拟化面板完整链路运行；Appearance 测试只能改变公开视觉属性，不能更换走廊或容器模板。

确定性测试源负责证明调度和生命周期；真实图片负责证明解码、绘制和原生资源释放。两者不能互相替代。Gallery 人眼用例只负责视觉品质、滚动感受和输入体验，不能替代容器数量、Lease 平衡或内存预算的自动证据。

## 建议工程入口

正式施工时建议建立：

```text
tests/AtomUI.Labs.Controls.ImageGallery.Tests/
├── ThumbnailFilmstrip/
│   ├── RangeAndRealizationTests
│   ├── FarJumpTests
│   ├── RecyclingAndGenerationTests
│   ├── LoadSchedulingTests
│   ├── CacheBudgetTests
│   ├── LifecycleTests
│   └── ModelBasedStressTests
├── Infrastructure/
│   ├── ControllableImageGallerySource
│   ├── ImageGalleryLeaseProbe
│   ├── ManualImageGalleryClock
│   └── ImageGalleryAxamlHost
└── TestAssets/

tests/AtomUI.Labs.Controls.ImageGallery.Benchmarks/
└── 独立非打包 Benchmark 入口
```

具体测试类名可以随实现语言组织调整，但职责不能被合并成一个无法定位失败原因的巨大测试。Benchmark 项目必须设置 `IsPackable=false`，也不能成为 ImageGallery 正式 NuGet 的依赖。

## 本议题完成条件

2026-08-19 已取得一轮 100,000 项、固定种子 `20260818` 的 30 分钟 Headless 长稳证据：1,766,316 次操作、110,197 个收敛采样，最大真实及 Visual Tree 容器均为 23（允许上限峰值 27），唯一观察到的容器身份为 24；828,417 个已完成 Lease 全部释放，Detach 后残留和释放后访问均为 0。完整命令、内存趋势和原始 JSON 路径见 [整体性能、NativeAOT 与 Gallery 发布验收设计](release-and-gallery-verification.md)。

只有同时满足以下条件，才能宣称缩略图虚拟化通过工业级验收：

- PR 中所有确定性红线通过。
- 100,000 项 Nightly 长稳没有资源和容器无界增长。
- 真实桌面和真实解码没有同步阻塞 UI 线程。
- Release NativeAOT 宿主通过完整默认主题链路。
- 第一版基线已经记录，性能回归规则已进入持续执行入口。
- Gallery 完成人眼滚动、部分裁剪、四方向布局和加载状态验收。

任何单独一次“看起来滚动流畅”、Debug 构建成功或小集合测试通过，都不能替代上述证据。
