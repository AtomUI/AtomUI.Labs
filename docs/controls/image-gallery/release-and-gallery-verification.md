# ImageGallery 整体性能、NativeAOT 与 Gallery 发布验收设计

> 文档状态：候选发布自动化基线，更新于 2026-08-24。2026-08-23 的 10K/100K 虚拟化、30 分钟长稳和 Benchmark 基线仍保留；主图 ResourceOnly/外部 Lease 扩展已取得新的 Build、119 项 Headless、Package 与真实 win-x64 NativeAOT Runtime Smoke 证据。由于资源槽与共享加载所有权发生实质变化，完整 Gallery 人眼矩阵和提交后干净检出仍须完成，当前结论不是最终 RC 签署。

本文是 [ImageGallery 首版施工合同](implementation-contract.md) 的发布验证专项文档，设计理由和完整推导仍见 [ImageGallery 控件设计](overview.md)。缩略图海量集合的容器、加载、缓存和长稳细节继续以 [缩略图虚拟化测试与性能验收设计](thumbnail-virtualization-verification.md) 为准；本文负责其余整体性能、真实 NativeAOT 桌面宿主、NuGet 边界、Gallery 自动 Smoke 与人眼验收。

## 2026-08-23 自动验收基线

本节记录最终优化工作树的实际结果，原始 JSON 和二进制只保存在忽略提交的 `output/` 验收目录中。当前 `HEAD` 为 `18ea3f9dbbf49f8ab1b1cac04962aac616a3b389`，但 ImageGallery 仍位于未提交工作树，因此包内 repository commit 只能视为仓库基线而不是制品源码证明；最终发布流水线必须在用户提交后的干净检出上重跑并替换本节标识和哈希。

| 闸门 | 本轮结果 |
|---|---|
| Release Build | 整个 `AtomUI.Labs.slnx` 通过，ImageGallery 正式包同时生成 `net10.0` 与 `net8.0`，0 个编译警告、0 个错误 |
| 自动测试 | ImageGallery 106 项 Headless/单元测试全通过；同时执行 Led 429 项回归测试 |
| 虚拟化 | 10,000 与 100,000 项均通过真实默认 AXAML、ScrollViewer、ItemsPresenter 和 `ImageGalleryVirtualizingPanel` 路径；远距离选择后容器数满足 `Rmax = ceil(2L/P) + 4` |
| 30 分钟长稳 | 100,000 项、固定种子 `20260818`、真实默认模板 Headless 视觉树连续运行 `30:00.0116992`；1,771,173 次操作、110,500 个收敛采样、失败 0，最终 Lease 残留 0 |
| Benchmark 环境 | Windows 10.0.26200、x64、16 logical processors、.NET 10.0.10、Release、每个场景 3 轮暖机 + 7 轮测量 |
| NuGet | `AtomUI.Labs.Controls.ImageGallery.6.0.8.nupkg` 同时包含 net10/net8；运行时依赖仅有 `AtomUI.Core 6.0.8` 与 `Avalonia 12.0.5`；唯一版本隔离缓存下的双框架最小消费编译通过，消费 DLL 与 Release DLL 哈希一致；正式 nupkg SHA-256 为 `7D97C777238E7466FE72A136BFFBB6FA08055869DE3AE30FBD1A391BE3BD4E08` |
| NativeAOT | `win-x64` self-contained publish 成功；最终优化版原生 exe 运行 1000 项 Compiled AXAML Smoke，7/7 检查通过，退出码 0 |
| AOT 归因 | ImageGallery 程序集没有产生 ILLink/NativeAOT 警告；发布日志中的 IL2104/IL3053 来自上游 AtomUI.Core、ReactiveUI.Avalonia 与 ReactiveUI，未用 NoWarn 掩盖 |

三次独立 Benchmark 的各进程中位数如下：

| 场景 | Run 1 | Run 2 | Run 3 | 三次中位 | 中位分配 |
|---|---:|---:|---:|---:|---:|
| Descriptor Snapshot 10K | 1.1808 ms | 0.8754 ms | 1.0285 ms | 1.0285 ms | 1,954,888 B |
| Descriptor Snapshot 100K | 7.4075 ms | 6.4421 ms | 7.4360 ms | 7.4075 ms | 18,704,016 B |
| Viewport Math 1M | 4.5464 ms | 4.4598 ms | 4.8052 ms | 4.5464 ms | 40 B |

最新原生 Smoke 摘要：

```text
RuntimeIdentifier     win-x64
Framework             .NET 10.0.10
Duration              346.275 ms
Checks                7 / 7 Passed
Checks covered        Initial Ready, template/layout, 1000-item virtualization,
                      Error + stable navigation, viewport commands, far selection,
                      Detach/Attach
Native exe SHA-256    B0D12DCD86914409F34DB6223C067E9FE6AC01E9B9D930D187621AFE36C7BD27
Native exe bytes      61,901,824
```

## 2026-08-24 主图资源扩展增量证据

本轮为 `MainImageMode`、`MainImageDecodeSizeHint`、`TryAcquireCurrentImage`、`CurrentImageResourceChanged`、当前资源槽和共享加载 Waiter 所有权改造后的增量证据。长稳 Runner 已同步扩展为随机切换 Presented/ResourceOnly、改变解码尺寸提示、限量持有外部 Lease，并在周期 Detach 和最终 Detach 后读取外部 Lease；最终仍要求底层完成/释放严格平衡。完整 Gallery 人眼矩阵不能由这些自动证据替代。

| 闸门 | 本轮结果 |
|---|---|
| Debug / Release Build | 整个 `AtomUI.Labs.slnx` 通过；ImageGallery `net10.0` / `net8.0` 均为 0 警告、0 错误 |
| 自动测试 | ImageGallery Release 119/119 通过；新增覆盖 10,000 次并发 Lease、零缓存预算 Waiter 竞态、事件矩阵、零 Viewport、Loading Hint 追赶、ResourceOnly 穿透与 Overlay 输入、Detach 后外部 Lease 存活及同选择重连视图状态保持 |
| NuGet | `AtomUI.Labs.Controls.ImageGallery.6.0.8.nupkg` 包含 net10/net8；依赖仍仅为 `AtomUI.Core 6.0.8` 与 `Avalonia 12.0.5`；2026-08-24 重新生成并刷新标准 Release 路径后的 SHA-256 为 `86B4A7E63D290356B05A804B37D8808C797FF5FC7C036057302ED6A48C2BB35F` |
| NativeAOT Publish | `Generating native code` 成功；publish 目录不含 `coreclr.dll`；ImageGallery 未新增 ILLink/AOT 警告，已有 IL2104/IL3053 仍归因于 AtomUI.Core、ReactiveUI.Avalonia 与 ReactiveUI |
| NativeAOT Runtime | 原生 exe 8/8 Smoke 通过、退出码 0；新增 `ResourceOnlyLease` 检查通过；耗时 `344.430 ms` |
| NativeAOT 制品 | exe `61,980,160` bytes；SHA-256 `9F94107C9EBECEFE30FF826ABE9D09B84C367DA1BB65D1F99285540F8BA0A380` |
| 30 分钟资源长稳 | 100,000 项、固定种子 `20260818`，连续 `30:00.1894111`；1,175,410 次操作、73,130 个收敛采样、失败 0；ResourceOnly、Hint 与外部 Lease 路径均实际命中，最终底层 Lease 残留 0 |
| 标准聚合脚本 | 首次运行因孤儿 `AtomUI.Labs.Controls.ImageGallery.Tests.exe` 未退出而被 10 分钟监督终止；确认并终止该本轮遗留进程后，同一命令复跑 `24.2s` 成功，ImageGallery 119 项与 Led 429 项全过并完成 Pack。该首次异常保留记录，但当前不可稳定复现。 |

原生 Smoke 检查为：`InitialReady`、`TemplateAndLayout`、`ResourceOnlyLease`、`VirtualizedThousandItems`、`StableNavigationCommands`、`ViewportCommands`、`FarSelection`、`DetachAttach`。结构化结果保存在忽略提交的 `output/image-gallery-resource-nativeaot-smoke.json`。

30 分钟长稳使用以下独立非打包入口：

```powershell
dotnet output\bin\Release\net10.0\AtomUI.Labs.Controls.ImageGallery.Benchmarks.dll `
  --soak-minutes 30 `
  --output output\benchmarks\image-gallery-resource-soak-30m.json
```

本轮长稳结构化摘要：

| 指标 | 实测值 |
|---|---:|
| 数据项 / 随机种子 | 100,000 / `20260818` |
| Duration / Operations / Samples | `30:00.1894111` / 1,175,410 / 73,130 |
| 最大真实容器 / Visual Tree 容器 / 允许上限 | 23 / 23 / 27 |
| 唯一观察到的容器身份 | 23 |
| Placement / Resize / Budget 变化 | 575 / 422 / 346 |
| Presented-ResourceOnly / Decode Hint 变化 | 490 / 382 |
| 集合变化 / Detach-Attach | 187 / 284 |
| 外部 Lease 获取 / Detach 后读取 / 最大同时持有 | 107 / 2,231 / 8 |
| CurrentImageResourceChanged | 1,170,697 |
| Started / Completed / Canceled Load | 791,948 / 790,240 / 1,708 |
| Completed / Released Lease | 790,240 / 790,240 |
| Detach 后 Lease / 释放后 Image 访问 | 0 / 0 |
| 托管堆：初始 / 最终 / 增量 / 峰值 | 32,921,768 / 33,799,344 / +877,576 / 86,405,888 B |
| Working Set：初始 / 最终 / 峰值 | 104,943,616 / 124,936,192 / 173,686,784 B |
| Private Bytes：初始 / 最终 / 峰值 | 64,806,912 / 75,284,480 / 126,717,952 B |

长稳通过的硬依据是实际容器始终受 `Rmax` 约束、容器身份数量形成平台、ResourceOnly/Hint/外部 Lease 路径均实际命中、外部 Lease 在 Gallery Detach 后仍可访问、底层 Lease 完成与释放严格相等、Gallery Detach 后内部资源归零以及释放后访问为零。Working Set、Private Bytes 和托管堆峰值只记录为趋势，不被单独包装成“无泄漏证明”。原始结构化报告保存在忽略提交的 `output/benchmarks/image-gallery-resource-soak-30m.json`。

增强预检曾稳定复现集合 Add/Remove/Move 后选择 Key 已保留、但走廊仍停在旧虚拟窗口的问题；修复为 Descriptor 快照与选择提交完成后无条件重新发布当前选择的最小必要定位请求，并增加 100,000 项集合重排回归用例。修复后 36 秒增强预检和正式 30 分钟长稳均为失败 0。

本轮三进程复核曾发现 100K Descriptor 中位耗时受零容量 `List` / `Dictionary` 反复扩容影响并超过 15% 相对审查线。实现现已对 `ICollection` 数据源使用已知 Count 预分配；最终三进程中位耗时相对旧基线没有回退，100K 分配由约 25.4 MB 降至约 18.7 MB。优化后的完整 30 分钟长稳、Package 消费和 NativeAOT Runtime Smoke 均重新执行，未沿用优化前证据。

尚未取得、因此不能冒充通过的证据：

- Light/Dark/Compact/高对比度、100%～200% DPI、触控 Pinch 和多种真实图片的完整人眼矩阵仍需用户打开 Gallery 验收。
- 多固定随机种子的刻意异步乱序完成、真实 4K/8K 解码和原生 Bitmap 趋势仍应按 Nightly/真实桌面矩阵继续执行；本次单种子 Headless 长稳不能替代它们。
- 正式发布前应在用户提交后的干净检出上保存 publish binlog、最终提交 SHA 和重新计算的原生文件哈希。

## 验收结论的严格含义

下列结论互不替代：

```text
Build 通过
    只证明源码和 AXAML 能够编译

Headless 测试通过
    证明确定性状态、布局、输入和资源合同

Benchmark 通过
    证明固定环境中的耗时、分配和增长趋势符合门槛

NativeAOT Publish 通过
    只证明裁剪和原生编译成功

NativeAOT Runtime Smoke 通过
    证明发布产物实际启动并走通 ImageGallery 核心链路

Gallery 人眼验收通过
    证明真实窗口中的视觉品质和交互感受合格

Package 验收通过
    证明最终 NuGet 内容、TFM 和依赖边界正确
```

只有全部适用证据同时通过，才能声明 ImageGallery 当前提交具备候选发布能力。不得用“Gallery 能启动”“Debug 看起来正常”或单次开发机测量替代完整结论。

## 验证分层

### PR 必跑

- `AtomUI.Labs.Controls.ImageGallery` Debug 与 Release 编译。
- ImageGallery 全部确定性单元、Headless、编译 AXAML 和资源生命周期测试。
- 最大 10,000 项的缩略图虚拟化正确性闸门。
- 非墙钟的复杂度、容器数量、请求并发、缓存预算和 Lease 平衡断言。
- Gallery Debug 编译以及 ImageGallery ShowCase 编译。
- `git diff --check`、依赖边界和公开 API 基线检查。

PR 中不以不稳定的帧时间作为普通单元测试断言。任何精确资源上限、身份一致性、过期结果隔离或 Dispose 次数失败均不得以“机器较慢”为由重试放过。

### Nightly

- 100,000 项缩略图压力与至少 30 分钟长稳。
- 固定随机种子的集合变化、滚动、选择、取消、Detach/Attach 和乱序完成。
- ImageGallery 整体 Benchmark 的独立进程测量。
- Release trimmed build。
- `win-x64` NativeAOT publish 和具备交互式桌面的运行 Smoke。
- 真实图片解码、缓存、原生资源释放和进程内存趋势。

### Release Candidate

- 干净检出、锁定 SDK 和依赖版本后的全量重跑。
- `net10.0` 与 `net8.0` 正式控件包编译、测试与 pack。
- NuGet 内容及依赖白名单审计。
- 固定参考 Windows 环境上的三轮整体 Benchmark 和虚拟化 Benchmark。
- `win-x64` NativeAOT 发布产物运行 Smoke。
- 完整 Gallery 视觉矩阵和人工交互清单。
- 保存可追溯验收产物，并由验收人签署结果。

首版只把 `win-x64` 作为必须通过的 NativeAOT 运行平台。未实际执行的平台不得因为代码“理论上跨平台”而宣称通过；后续支持 `win-arm64`、macOS 或 Linux 时，必须增加对应真实宿主和平台结果。

## ImageGallery 整体 Benchmark

缩略图专项以外，整体 Benchmark 至少拆分以下职责，不能用一个“打开 Gallery 总耗时”掩盖瓶颈。

### 测量场景

| 场景 | 规模/变体 | 主要指标 |
|---|---|---|
| Descriptor 建立 | 100、1,000、10,000、100,000 项 | 耗时、分配、线性增长率 |
| Reset 与 Key 验证 | 相同 Key、全部新 Key、重复 Key 位于末尾 | 耗时、分配、错误检测完整性 |
| 相邻选择提交 | 缓存命中、缓存未命中、旧请求取消 | UI 协调开销、分配、状态提交 |
| 远距离选择 | `0 -> 99,999` 等 | 与跳转距离无关的协调开销、实际容器数 |
| 主图结果提交 | 当前结果、过期结果、Source Identity 改变 | 提交/拒绝耗时、Lease 平衡 |
| 主图高速切换 | `A -> B -> C`、逆序完成、当前失败、缓存命中 | Loading 旧帧连续性、最终选择唯一性、提交后释放顺序、空帧数 |
| Viewport Measure/Arrange | Fit、ActualSize、Custom，常见和极小尺寸 | 耗时、分配、无效布局次数 |
| Viewport Render | 1 MP、4K、8K，Fit/Zoom/Pan/Rotate | CPU 提交耗时、帧时间、分配 |
| 连续直接输入 | Wheel Zoom、Pan、Pinch | 输入到视觉提交延迟、帧时间、状态稳定性 |
| Overlay 响应式布局 | 四种 Placement、同边/邻边冲突、三档状态 | Measure/Arrange 次数、滞回稳定性 |
| 主图缓存和预取 | Disabled/Adjacent、命中、淘汰、超预算 | 请求数、缓存字节、Lease 生命周期 |
| Attach/Detach | 100、1,000 次循环及迟到请求 | 存活资源、事件订阅、恢复耗时 |

协调器和布局微基准使用确定性测试 Source 或轻量 `IImage`，隔离磁盘、网络和真实解码噪声。真实图片基准另行覆盖文件读取、解码和原生资源，但必须把 Source 耗时与 ImageGallery 自身开销分别记录。

### 复杂度硬约束

以下约束从首次实现起即为正确性门槛，不等待硬件基线：

- 建立全部 Descriptor、Reset 全集合和唯一 Key 验证允许为 `O(N)`，但不能退化为 `O(N²)`。
- 已知索引的相邻选择、缓存查找、请求版本校验和稳定 Selection 提交目标为摊销 `O(1)`。
- 远距离选择不得逐项实现中间容器或加载中间图片；视觉工作量由新旧虚拟窗口控制，而不是由索引距离控制。
- 主图 Render、Zoom、Pan 和 Rotate 的控件协调开销不得随 ItemsSource 数量增长。
- Placement 切换和响应式布局不得重新解码当前主图、重建全部 Descriptor 或扫描全部视觉容器。
- Detach 清理由当前活跃请求、缓存项和已实现容器数量约束，不得为了清理逐个加载或创建未实现项目。

复杂度通过不同 `N` 的操作计数、访问计数和增长曲线共同验证，不能只凭源码目测或一次绝对耗时判断。

### 固定参考环境目标

真实窗口性能在记录了 CPU、内存、GPU、Windows、.NET SDK、Avalonia/AtomUI 版本、DPI、主题和构建提交的固定机器上判定。Release、无调试器、完成暖机后，首版目标为：

- 连续滚动、Pan、Wheel Zoom 和 Pinch 的 p95 帧耗时不超过 `16.67 ms`。
- p99 帧耗时不超过 `33.3 ms`。
- 稳态交互不出现超过 `100 ms` 的 UI 线程停顿。
- 相邻选择的 Selection、Loading/Ready 逻辑提交开销，排除 Source I/O/解码后，p95 不超过一个 `16.67 ms` 帧预算。
- 有效主图缓存命中后，从选择到可绘制状态的 p95 不超过 `33.3 ms`。
- 图片文件、HTTP/OSS 读取和解码不在 UI 线程同步执行。
- 暖机后的稳定 Viewport Render 不因历史选择次数、历史缩放次数或集合总量持续增加分配。

这些数值在第一版测量链路校准前是明确目标，不是已经实现的承诺。第一版若无法达到，必须先定位和修复；确有平台事实证明目标不合理时，只有带原始数据、环境和书面理由的设计评审才能修改，不能仅把失败阈值调大。

### 回归规则

- 每个 Benchmark 独立进程预热后至少运行三轮，以中位数判定并保存原始数据。
- 同一参考环境中，中位耗时回退超过 `15%` 必须失败或进入书面性能审查。
- 单次操作托管分配增长超过 `10%` 必须失败或解释并审查基线更新。
- 新的持续 Gen2/LOH 增长、Lease/缓存/容器无界增长或 UI 线程同步 I/O 直接阻止发布。
- 绝对帧目标和相对回归目标同时生效；不能因为新版本仍比基线快就忽略其超过绝对目标，也不能因为仍低于绝对目标就接受大幅退化。
- 更新基线必须保存旧值、新值、提交、环境差异和原因；禁止只覆盖基线文件。

## NativeAOT 真实桌面宿主

### 宿主选型

首版唯一正式 NativeAOT 视觉宿主为：

```text
controlgallery/AtomUILabsGallery.Desktop
```

该项目当前 Release 配置已经声明 `IsAotCompatible`、`IsTrimmable`、`PublishTrimmed`、`SelfContained` 和由 `GalleryPublishAot` 控制的 `PublishAot`。ImageGallery 继续使用这个真实 AtomUI Gallery 桌面应用，不另建一个绕过真实主题、路由和应用启动链路的“假宿主”。

Gallery 可以依赖 `AtomUI.Desktop.Controls` 和 `AtomUI.Toolkits.GalleryBase` 组织 ShowCase，但这些依赖不得进入 `AtomUI.Labs.Controls.ImageGallery` 正式包。NativeAOT 宿主通过不等于运行时包依赖边界通过，两者必须分别审计。

### 专用 Smoke 模式

Desktop Host 增加仅服务验收的命令行入口：

```text
AtomUILabsGallery.Desktop.exe
    --image-gallery-smoke
    --result <absolute-json-path>
    --timeout-seconds 90
```

Smoke 代码位于 Gallery/Desktop 验收层，不进入 ImageGallery NuGet。它通过显式静态注册启动专用编译 AXAML 页面，禁止反射扫描 ShowCase、按字符串创建 View 或在 AOT 模式下替换为简化控件。

Smoke 必须创建并显示真实 Window，完成 VisualTree Attach、Measure、Arrange 和至少一次有效 Render。仅构造 `new ImageGallery()`、创建不可见控件或调用内部算法不能算运行通过。CI 没有交互式 Windows Desktop Session 时，可以完成 publish，但该任务必须明确标记“Runtime Smoke 未执行”，不能计为发布通过；Release Candidate 必须在有交互桌面的受控 Windows Agent 上补齐。

### AOT Smoke 页面

专用页面必须同时满足：

- 使用 `https://atomui.net/labs` XML 命名空间和 `atom.labs` 前缀。
- 使用默认 `ImageGalleryThemesProvider`、固定根 `ControlTheme` 和全部包内角色 Theme；不得通过外部模板替换绕过真实视觉链路。
- 设置准确 `x:DataType`，以 `{CompiledBinding GalleryItems}` 提供 ItemsSource。
- 项目通过 `IImageGalleryItem` 或正式 `ImageGalleryItem` 进入控件。
- 图片只来自随宿主发布的 Avalonia Resource、运行期临时文件和确定性自定义 Source，不依赖公网。
- 同时包含 Ready、受控 Loading、Error、专用 Thumbnail Source 和主图派生 Thumbnail 路径。
- 数据量足以证明虚拟化真实生效，但不把 100,000 项长稳塞进快速启动 Smoke；快速 Smoke 使用至少 1,000 项，Nightly 另运行 100,000 项模式。

Smoke 数据和路径不得包含开发者机器专用绝对路径，临时文件在进程结束时清理。

### 必须走通的运行链路

自动 Smoke 依次验证：

1. 进程启动、AtomUI 初始化和 Main Window 建立。
2. ImageGallery 页面通过真实 Gallery 路由加载。
3. 默认 Theme、Toolbar、Filmstrip、Navigation Button、Add Button 和 Viewport 模板部件建立且 Bounds 合法。
4. CompiledBinding 取得 ItemsSource，首个有效项目自动选中。
5. 资源图片成功进入 Ready，标题和选中缩略图对应相同 Key。
6. 缩略图实际容器数量满足专项 `Rmax`，未一次创建全部 1,000 项。
7. Next、Previous、Thumbnail 选择和远距离选择通过公共命令/交互入口改变同一 SelectionModel。
8. ZoomIn、ZoomOut、Fit、ActualSize 和 RotateClockwise 公共命令产生合法状态。
9. Error Source 显示默认失败兜底而不使进程崩溃。
10. 受控迟到结果不能覆盖新选择。
11. Detach 页面后释放当前消费者和 Lease；重新导航进入后只加载最终有效选择与新可视窗口。
12. Window 正常关闭、进程主动退出并写出完整结果。

自动化必须通过真实公开控制面、模板视觉或与用户入口相同的命令路径，不能直接修改内部字段伪造结果。对模板部件的检查只用于证明默认 Theme 完整，不能把内部名称变成公共 API。

### 结果协议和退出码

Smoke 输出带版本的 JSON，至少包含：

```text
SchemaVersion
ControlVersion
AtomUIVersion
RuntimeIdentifier
FrameworkDescription
Commit
StartedAt / Duration
Checks[]: Name, Status, Duration, Diagnostic
UnhandledException
Result
```

退出码固定为：

```text
0  全部检查通过
2  参数或测试资源配置错误
3  超时
4  验收断言失败
5  未处理异常或进程生命周期异常
```

超时由进程外监督器执行最终终止，进程内控制器负责写入已完成步骤。进程提前退出、结果文件缺失、JSON 无法解析或检查数量不完整都判定失败。测试失败后可以保留截图、日志和结果用于诊断，但自动重跑不得把首次失败改写为通过。

### Publish 与警告门禁

正式入口使用干净输出目录，执行等价于：

```powershell
dotnet publish controlgallery/AtomUILabsGallery.Desktop/AtomUILabsGallery.Desktop.csproj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --property:GalleryPublishAot=true
```

要求：

- ImageGallery 源码、AXAML 和 Generator 生成路径不得新增 ILLink/NativeAOT 警告。
- 依赖程序集的既有警告按诊断 ID、程序集和成员保存显式基线；任何新增或归因变化都失败并重新审查。
- 禁止用全局 `NoWarn`、关闭 trimming、关闭 AOT 或无边界的 DynamicDependency 让发布“变绿”。
- 发布日志和 binlog 保存为验收产物；必须运行本次干净发布生成的可执行文件，不能误用旧输出。
- Release Candidate 记录可执行文件哈希、大小和发布目录清单，使 Smoke 结果能够对应唯一产物。

## NuGet 与消费边界验收

NativeAOT Gallery 使用源码项目引用，不能单独证明最终 NuGet 正确。Release Candidate 还必须：

- Pack `AtomUI.Labs.Controls.ImageGallery` 的 `net10.0` 与 `net8.0` 资产。
- 解压审计 nuspec、lib 资产、AXAML 资源、主题 Provider 和必要生成结果。
- 依赖只允许本文已确定的 Avalonia、`AtomUI.Core` 及实际必要的运行时基础依赖。
- `AtomUI.Generator` 若使用，必须为编译期 `PrivateAssets=all`，不能成为运行时传递依赖。
- 包中不得出现 `AtomUI.Desktop.Controls`、`AtomUI.Toolkits.GalleryBase`、Gallery ShowCase、测试资源、Benchmark 或本机路径。
- 使用本轮生成的本地 nupkg 完成一次干净还原和最小消费编译，禁止因 NuGet 全局缓存误用旧包。
- 对包内程序集运行公开 API 基线和 AOT/trimming 分析，确认项目引用构建与包消费没有不同结果。

Package 验收失败不能因为 Gallery 的项目引用版本运行正常而放行。

## Gallery ShowCase 验收矩阵

ImageGallery Gallery 页面不是宣传截图集合，而是工程验收工作台。至少提供以下互相独立的 Case：

1. Empty 与单图片：空结构、单缩略图、Add Button 显隐。
2. Basic：混合宽高比、主图导航、Thumbnail 选择、标题和默认 Toolbar。
3. Main Viewport：Fit、ActualSize、Zoom、Pointer Anchor、Pan、四分之一圈 Rotate。
4. Navigation：Viewport/Filmstrip Previous/Next、循环开关、首尾禁用状态。
5. Filmstrip Placement：Top、Bottom、Left、Right、交叉轴 Extent、边缘 Gap、Item Extent、Item Spacing 与两级 Padding。
6. Overlay Collision：Toolbar 与 Filmstrip 同边规整、邻边避让、Normal/Compact/Minimal。
7. Appearance Styling：根控件标准外观属性、各公开 Appearance、Toolbar 宽高、Region/Item Spacing、外壳背景/圆角/透明度、按钮交互状态、选中指示器和末端 `ToolbarContent`；同时验证未开放模板替换入口。
8. Async Sources：Ready、慢加载、忽略取消、乱序完成、专用缩略图和失败兜底。
9. Large Collection：至少 100,000 个轻量 Descriptor，显示实时诊断但不进入正式控件 API。
10. Memory/Lifecycle：缓存预算、预取、Tab/路由 Detach 与重新 Attach。
11. High Contrast/Reduced Motion：普通、高对比度和减少动态效果对照。
12. AOT Smoke：与自动 NativeAOT Smoke 使用相同的编译 AXAML 和数据合同入口。

ShowCase 默认首次打开 Basic，不默认停在压力、Error 或 AOT 页面。工程诊断按钮、故障注入和计数器只存在于 Gallery 层，不能污染控件公共 API。

## 人眼验收

### 环境矩阵

至少覆盖：

- Light、Dark、Compact 和高对比度资源。
- 100%、125%、150%、200% Render Scaling。
- 1280×720、1920×1080 和 4K 等代表窗口；还要手动拖拽经过响应式阈值。
- 鼠标、高精度触控板；具备触屏设备时验证 Pinch。
- 明亮雪景、深色夜景、高纹理、透明、横幅、竖图、超大图和损坏图。

### 视觉与交互清单

- Toolbar、Filmstrip 和按钮覆盖在任意主图上仍可辨识。
- 主图 Fit/ActualSize、旋转后边界、锚点缩放和平移 Clamp 没有跳变、空洞或裁剪错误。
- 主图切换后标题、选中缩略图、主图和命令状态始终指向同一项目。
- 四种走廊方向的 Previous/Next 图标、滚轮轴和部分缩略图裁剪符合方向语义。
- 走廊 Extent、Item Spacing、走廊 Padding 和缩略图 Padding 职责分离；方向切换后主轴 Gap 正确且远距离定位无累计偏移。
- Toolbar 在上下边缘使用水平默认排列、左右边缘使用垂直默认排列；Region/Item Spacing 正确且不改写自定义内容内部布局。
- Toolbar、Filmstrip、Navigation Button 的背景、圆角、Alpha/Opacity 和 Appearance 固定尺寸在安全布局范围内生效。
- 同边冲突先移动 Toolbar；邻边冲突先缩短 Toolbar，再按已确定顺序降级，主图 Viewport 始终保留。
- 快速滚动和快速选择时，Loading 保留帧连续可绘制，没有空白闪烁；过期候选没有旧图闪回、容器串图、错误状态污染或明显输入冻结。
- 主图成功交接时 Release Callback 只能在新 Lease 已安装后执行；当前候选失败、集合变空和 Detach 必须清除保留帧，不能把上一选择重新标为 Ready。
- Loading、Error、Empty、Disabled、Selected 和 PointerOver 状态清楚且布局稳定。
- 高对比度不依赖半透明图片背景，选中状态不只靠颜色。
- 减少动态效果下没有持续装饰动画，直接操作反馈仍及时。
- Detach/Attach、主题切换、DPI 切换和窗口 Resize 后状态与资源行为正确。

人眼验收必须记录提交、构建配置、机器、主题/DPI矩阵、验收人、日期和结论。截图用于证据和比较，但不能只锁整张 PNG 哈希；抗锯齿、平台字体和 GPU 差异由结构化与像素语义测试补充判断。

## 缺陷严重性和发布闸门

```text
P0 / Blocker
    崩溃、NativeAOT 无法启动、错误图片提交、Use-after-dispose、数据损坏、无界资源增长

P1 / Critical
    选择/主图/缩略图身份不一致、虚拟化失效、UI线程同步I/O、核心导航失效、Detach泄漏

P2 / Major
    主要布局/主题/高对比度/输入体验不符合合同，但不存在立即崩溃或资源破坏

P3 / Minor
    不影响任务完成的轻微视觉或诊断问题
```

候选发布要求：

- P0、P1、P2 未关闭缺陷均为 `0`。
- 全部自动硬闸门通过且无无法解释的新警告。
- P3 必须有书面评估、明确影响和后续跟踪，不能用 P3 标签隐藏合同违背。
- Flaky 测试视为测试系统缺陷；不得用重跑成功覆盖首次失败。
- 任何跳过项必须标明原因和未获得的结论；缺少交互式桌面意味着“未验证”，不是“通过”。

## 验收产物

每个 Release Candidate 至少保存：

```text
commit-and-versions.txt
build-and-test-results/
benchmark-raw-and-summary/
nativeaot-publish.log
nativeaot-publish.binlog
nativeaot-output-manifest-and-hash.txt
image-gallery-smoke-result.json
image-gallery-smoke.log
package-manifest-and-dependency-audit.txt
gallery-visual-checklist.md
gallery-screenshots/
known-aot-warning-baseline.txt
```

产物路径由 CI 或发布脚本决定，不提交二进制和临时输出到源码仓库。文档中只提交经审查的基线摘要、测试方法和必要的长期趋势结果。

## 本议题完成条件

设计层面的完成条件是：Benchmark 场景、复杂度红线、真实宿主、Smoke 协议、NuGet 审计、Gallery Case、人眼矩阵、缺陷等级和证据产物均已明确。本文已经满足设计层面的条件。

工程层面已经取得首份 Build、自动测试、Benchmark、30 分钟 Headless 长稳、NativeAOT Runtime Smoke 和包审计基线。完整 Gallery 人眼矩阵、真实图片/DPI/触控验收以及提交后干净检出重跑仍未签署，因此当前可以认定为自动化候选发布基线通过，不能认定为最终 RC 已签署。
