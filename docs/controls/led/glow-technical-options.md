# LED Glow 技术路线选型

> 文档状态：已实现。本文于 2026-07-20 随 LED 控件从 AtomUI 迁入 AtomUI.Labs，并已按本仓库的包名、目录和验证入口完成适配。历史性能数值仍表示迁移时的基线，后续变更应在本仓库重新验证。

本文记录 `AtomUI.Labs.Led` 家族真实静态 Glow 的技术路线。候选路线、评估过程和迁移前参考实现的最终决议均保留在本文中；对应运行时代码现已迁入本仓库。

Glow是Matrix与Segment之上的可选视觉增强层。两个基础控件在没有Glow时必须保持完整、可生产使用。Glow只接收已经生成的Active Geometry，不读取字符、字模、点阵行列或SegmentParts。

```text
Matrix Active Geometry  ─┐
                         ├─> LED Glow增强层 -> Active本体绘制
Segment Active Geometry ─┘
```

Segment当前的`GlowBrush`和`GlowOpacity`只是使用相同Geometry做一次透明叠色，没有向Geometry外部扩散。该实现是历史现状，不视为本文定义的真实空间Glow。

## 第一版契约约束

候选公共属性严格限制为：

| 属性 | 类型 | 默认值 | 语义 |
|---|---|---:|---|
| `GlowBrush` | `IBrush?` | `null` | 光晕画刷；`null`关闭Glow |
| `GlowOpacity` | `double` | `0.35` | 光晕强度，有效范围`0..1` |
| `GlowRadius` | `double` | `6` DIP | 光晕在局部绘制坐标中的扩散范围；有效范围`0..24` DIP |

不增加`GlowEnabled`、`GlowColor`、`GlowIntensity`、`GlowBlurRadius`、`GlowSpread`、`GlowOffsetX/Y`、`GlowLayerCount`、`GlowQuality`或专用动画属性。动画由Avalonia Animation驱动上述静态StyledProperty。

`GlowRadius`第一版使用固定安全范围：

```text
负数、NaN、正负Infinity -> 0
0..24                   -> 保持原值
大于24                  -> 内部按24处理
```

规整只作用于内部有效值，不回写开发者设置的StyledProperty，避免破坏Binding和属性优先级。Matrix与Segment必须使用完全相同的范围和规则。视觉语义建议为：`0`关闭、`2`轻微柔光、`6`默认、`12`明显、`24`第一版强Glow上限。

Radius上限只限制Geometry外扩，不能限制超大字符本体产生的Mask。因此正式实现仍必须同时定义单个离屏缓冲区物理像素上限和单控件Glow缓存总字节预算。

路线B单个离屏Glow Mask使用以下内部安全上限：

```text
单边最大值：1024物理像素
总面积上限：262144物理像素（等价于512 x 512）
```

请求尺寸超限时只降低Glow Mask分辨率，Active Geometry本体仍按正常RenderScaling清晰绘制。降采样比例为：

```text
scaleByWidth  = 1024 / requestedWidth
scaleByHeight = 1024 / requestedHeight
scaleByArea   = sqrt(262144 / requestedArea)
maskScale     = min(1, scaleByWidth, scaleByHeight, scaleByArea)
```

降采样后必须保持逻辑Glow Bounds和Radius语义不变。若极端输入或后端错误导致仍无法安全创建资源，只跳过该Glow绘制，基础Active Geometry必须继续正常显示；第一版不回退到路线A，避免同一属性在不同尺寸下静默切换视觉算法。

降采样或跳过只能按控件和同类配置记录一次诊断，禁止每帧刷日志。诊断至少包含请求/实际物理尺寸、RenderScaling、Content Scale、GlowRadius和降级结果。不公开`GlowQuality`，分辨率控制属于内部资源安全策略。

路线B单个控件的Glow派生资源缓存初始总预算固定为`16 MiB`。预算统计所有长期保留的Alpha Mask、Blurred Mask、后端纹理/Image和缓存项专属辅助缓冲，不得只统计托管对象或缓存项数量。该数值是内部安全预算，不公开为开发者属性；原型数据证明不合理时必须保留原始数据并形成书面调整决议。

缓存淘汰使用LRU，但当前帧可见的不同字符级Glow Source组成帧内Pin工作集，绘制当前帧时不得被淘汰。每帧先去重并估算工作集字节；工作集超过16 MiB时，对该帧全部Glow Mask统一降低分辨率，禁止同一画面混用无依据的高低质量。达到内部最低Mask比例后仍超限时，跳过无法安全容纳的Glow并继续绘制Active本体，禁止进入每帧反复构建和淘汰的缓存抖动路径。

`GlowBrush=null`表示明确关闭，必须立即释放全部Glow缓存并令CachedBytes和EntryCount归零。`GlowOpacity=0`只跳过合成并保留缓存，保证呼吸动画经过零点时不触发重建。

Glow不参与Measure。开发者通过Padding预留完整显示空间；光晕允许进入Padding，但Matrix裁剪在Border内缘，Segment裁剪在控件Bounds。绘制层次固定为：

```text
Background
  -> 全部Inactive Geometry
  -> 全部Active Geometry的Glow
  -> 全部Active Geometry本体
  -> Matrix Border
```

## Matrix与Segment的共享边界

Matrix与Segment不仅要保持Glow语义一致，还必须共享同一套Glow核心算法。不得在两个控件中分别复制Mask、Blur、着色、缓存和资源释放实现。

```text
MatrixDisplay
  -> Matrix字符级Active Geometry适配 ─┐
                                      ├─> LedGlowRenderer / LedGlowCache
SegmentDisplay                        │
  -> Segment活跃段Geometry集合适配 ───┘
```

分层职责固定为：

| 层 | Matrix | Segment | 是否共享 |
|---|---|---|---|
| 公共StyledProperty | 分别注册`GlowBrush/GlowOpacity/GlowRadius` | 分别注册同名、同类型、同默认值属性 | 共享契约，不共享属性所有者 |
| 基础显示 | 字模映射、点阵布局、点Geometry | 段位映射、Segment布局、段Geometry | 不共享 |
| 输入适配 | 提供聚合后的字符Active Geometry | 提供一个字符的活跃段Geometry集合 | 各自实现薄适配 |
| Glow核心 | Mask、Blur、Brush着色、Opacity合成、Radius和DPI处理 | 使用完全相同实现 | 真实共享 |
| 派生资源 | 字符级Glow缓存 | 字符级Glow缓存 | 共享缓存实现，实例分别拥有 |

不建立 `LedGlowControl` 公共控件基类，不让 Matrix 通过 `SegmentDisplay.GlowBrushProperty.AddOwner` 依赖 Segment，也不为了 Glow 合并字符映射、布局、Overflow 或基础 Geometry 缓存。分别注册 StyledProperty 是为了保持控件所有权边界，不代表允许复制 Glow 算法。

最终内部结构允许类似：

```text
src/AtomUI.Labs.Led/
  Glow/
    LedGlowRenderOptions
    LedGlowRenderer
    LedGlowCache
    选定路线的Mask/Blur实现
  Matrix/
    Matrix自己的基础显示和Glow输入适配
  Segment/
    Segment自己的基础显示和Glow输入适配
```

Segment现有同形叠色路径升级为真实空间Glow后必须删除，不保留`LegacyGlowMode`、`UseOldGlow`或两套并行算法。默认`GlowBrush=null`保证未启用Glow的基础视觉不变；已显式启用Glow的Segment获得与Matrix相同的真实外围光晕语义。

## 路线A：多层矢量扩张

围绕Active Geometry绘制多层不同宽度和透明度的描边，使用离本体越远越透明的层模拟光晕。

```text
Active Geometry
  -> 宽描边、低透明度
  -> 中描边、中低透明度
  -> 窄描边、较高透明度
  -> Active本体
```

### 优势

- 直接消费Avalonia Geometry，不需要栅格化或离屏位图。
- 可仅使用Avalonia公共矢量绘制API，跨渲染后端和NativeAOT风险较低。
- 裁剪、Transform和Brush语义与现有Matrix/Segment绘制路径一致。
- Geometry和Pen可按Radius档位缓存，生命周期容易限定在控件实例或Glow渲染器实例。
- Headless测试可以检查绘制命令、像素外扩范围和缓存上界。

### 劣势

- 多层描边是离散近似，不是真正连续高斯模糊；层数不足时可能出现色带。
- 增加层数会线性增加绘制命令和合成成本。
- 大Radius或高Opacity下容易呈现粗轮廓，而不是柔和空气光。
- 描边扩张可能填平Segment尖角，并在Matrix相邻灯珠之间过早连成一片。
- Geometry为填充区域而非单一路径时，需要确认描边对内孔、组合Geometry和自交路径的行为。

### 定位

路线A是低风险保底方案，适合较小Radius、克制的工业视觉和无法安全使用离屏模糊的后端。它不能在没有视觉证据时被描述为与真实高斯Glow等价。

## 路线B：Alpha Mask加模糊

路线B先把Active Geometry栅格化为只记录透明度的遮罩，再模糊遮罩、使用GlowBrush着色并合成到主DrawingContext。

```text
Active Geometry
  -> 栅格化到透明离屏缓冲区
  -> Alpha Mask
  -> Blur(GlowRadius)
  -> 乘以GlowBrush和GlowOpacity
  -> 合成模糊光晕
  -> 绘制清晰Active本体
```

Alpha Mask只表达发光源覆盖率，不提前写入Glow颜色。因此同一份轮廓可以使用不同GlowBrush着色，Brush和Geometry职责保持分离。

```text
原始Mask             模糊后的Mask

    █████               ·······
  █████████          ··░░░░░░░░░··
  █████████         ·░░▒▒█████▒▒░░·
    █████               ·······
```

### 优势

- 透明度连续衰减，最接近网页Neon、真实灯珠和柔和空气光。
- 算法只依赖Alpha轮廓，不关心输入是Circle、Square、RoundedSquare还是十四段Geometry。
- GlowBrush、GlowOpacity和GlowRadius三项契约都能获得直接、可解释的视觉含义。
- 合理实现后可由渲染后端加速模糊与合成。
- 不需要通过公开LayerCount或Quality暴露算法内部细节。

### 劣势

- 必须管理离屏像素缓冲区。缓冲区大致为`Geometry.Bounds + 四周GlowRadius`，物理像素还要乘以RenderScaling。
- 内存、栅格化和模糊成本同时受可见面积、DPI和Radius影响；Radius动画可能导致缓冲尺寸或模糊核持续变化。
- 不能把超长Matrix文本整行渲染到一张巨大位图。必须按可见字模或有限批次处理，并给Matrix字符剔除范围增加Radius外扩。
- 缓存键至少涉及Geometry身份或版本、Radius、RenderScaling和可能影响Mask的Transform；失效和释放比路线A复杂。
- Brush变化原则上应复用Alpha Mask，但若底层API把着色与模糊绑定在一起，可能无法做到。
- Headless、不同平台后端和NativeAOT发布都需要真实验证，不能只依靠桌面Skia肉眼效果。

### 必须证明的工程条件

- 使用公开且稳定的Avalonia API完成局部Alpha Mask和Blur，或者明确记录所需后端边界。
- Glow关闭后不创建离屏缓冲、不增加绘制命令或持续分配。
- 静态Radius稳态帧不反复创建大型位图；缓存有固定上界且旧资源可释放。
- Matrix按可见字模或有限批次处理，Segment按可见字符/Geometry集合处理。
- Glow只作用于Active层，Background、Inactive和Border不得进入Mask。

### 定位

路线B是当前视觉质量首选。只有在公开API、局部缓冲、稳态分配和跨平台验证全部通过后才能成为正式方案；不能只因为效果最好而忽略资源成本。

## 路线C：Avalonia Effect或Skia自定义效果

路线C优先评估Avalonia现有`BlurEffect`、`DropShadowEffect`等后端效果。Avalonia 12公开`DrawingContext.PushEffect(IEffect, Rect)`，可以把Effect限制在一组Active Geometry绘制命令内，并按Effect输出Padding扩张给定的预膨胀Bounds；该能力必须通过原型证明实际像素隔离。公开Effect仍无法满足时，再评估`ICustomDrawOperation`或Skia自定义绘制。

### 优势

- 可能直接使用Avalonia渲染后端或GPU完成模糊与合成。
- 模糊质量和大Radius性能可能优于应用层多次矢量绘制。
- 若公开Effect能够作用于独立Active视觉层，业务代码可以较少。

### 劣势

- 直接设置`Visual.Effect`仍会把MatrixDisplay或SegmentDisplay的Background、Inactive、Active和Border一起处理，不符合Glow契约；必须使用绘制作用域隔离Active层。
- 为隔离Active层而新增子Visual、离屏Visual或模板层，会改变当前自绘控件结构、生命周期和命令组织。
- Skia自定义路径绑定具体渲染后端，削弱Avalonia跨平台后端边界。
- 自定义绘制需要处理渲染线程资源、相等性、失效、设备上下文变化和释放，测试与维护成本最高。
- Headless实现与真实Skia/GPU表现可能不同；NativeAOT和平台发布风险也更高。

### 定位

Avalonia公开`PushEffect`原型已经证明可在不新增子Visual的情况下产生Geometry外像素；是否成为正式方案仍需通过图层隔离、Brush、裁剪、真实窗口、缓存和完整性能门禁。Skia自定义实现是最后备选，不作为第一版优先路线。

## 统一评估矩阵

三个原型必须使用相同输入和指标：

| 维度 | 验收要求 |
|---|---|
| 视觉真实性 | 光晕必须扩散到Geometry外，不能只是同形叠色 |
| 输入覆盖 | Matrix三种点形、Segment典型横段/竖段/斜段/符号 |
| 图层隔离 | Background、Inactive、Border不参与Glow |
| 裁剪 | Glow进入Padding但不越过外壳边界 |
| 布局 | GlowRadius不影响Measure和DesiredSize |
| DPI | 100%、125%、150%、200%下无明显断层或异常裁剪 |
| 动态 | GlowOpacity和GlowRadius变化不破坏Geometry基础缓存 |
| 性能 | 记录首次构建、稳态帧分配、命令数、缓冲区尺寸和Radius动画成本 |
| 缓存 | 历史文本、Radius和DPI变化不导致无界增长，旧资源可回收/释放 |
| 长文本 | Matrix不能创建整行无上限离屏缓冲；可见性剔除包含GlowRadius |
| 发布 | Labs测试、Sample、性能工具和win-x64 NativeAOT通过 |

## 性能契约与测试强度

Glow选型不得凭单次肉眼观察或单次Benchmark结果通过。验证分为PR门禁、专项性能审计和发布前长稳三层；每个Case必须对应明确风险，禁止用重复但无判定价值的测试数量制造虚假覆盖。

### 硬性性能契约

Glow关闭时：

- `GlowBrush=null`不得创建Glow Renderer后端资源、Mask、Blur结果或Glow缓存。
- 不增加Glow绘制命令，不查询Glow缓存，不启动计时器或订阅事件。
- 稳态托管分配必须与当前无Glow基线相同；相同场景中位耗时不得超过基线`1.05x`。

静态Glow预热后：

- 重复Render的Mask和Blur新增数必须为0。
- Brush或Opacity变化不得重建Geometry、Mask或Blur结果。
- 缓存数量不得超过当前实例出现过的不同字符级Active Geometry数量。
- 重复字符只允许复用同一份字符级Glow资源。
- 单控件长期保留资源不得超过16 MiB；缓存必须同时报告EntryCount和CachedBytes。

动画时：

- Opacity动画不得重建Geometry、Mask或Blur，缓存数量全程不变。
- Radius动画允许重建Blur派生资源，但只允许保留当前配置的一代缓存；历史Radius不得累计。
- 动画停止并强制完整回收后，存活托管内存和后端资源数量不得呈持续增长趋势。

缓冲区与长文本：

- 单个离屏缓冲区只能覆盖单字符或明确有上界的有限批次，物理尺寸按`(CharacterBounds + 2 * GlowRadius) * RenderScaling * ContentScale`核算。
- 单边不得超过1024物理像素，单Mask不得超过262144物理像素；超限时按统一公式降采样Glow Mask。
- 禁止按完整长文本宽度创建无上限离屏缓冲。
- Matrix必须先执行包含GlowRadius外扩的可见字符剔除，再进入Mask、Blur和合成。

### 第一层：PR确定性门禁

该层进入常规Labs测试，要求快速、可重复，不使用墙钟耗时作为断言。

数值边界Case：

- `GlowOpacity`覆盖`NaN`、正负Infinity、负数、0、接近0、0.35、接近1、1和大于1，至少10组。
- `GlowRadius`覆盖`NaN`、正负Infinity、负数、0、亚像素值、2、6、12、24、刚超过24和极大有限值，至少12组；验证大于24时内部有效值固定为24。
- 原始StyledProperty值不得因内部规整被回写。

Brush Case：

- `null`、不透明SolidColorBrush、带Alpha的SolidColorBrush、LinearGradientBrush、RadialGradientBrush、Brush实例替换和DynamicResource更新，至少7组。
- Brush与Opacity变化必须通过计数器证明Mask/Blur未重建。

Matrix像素Case：

- 3种DotShape × 4种RenderScaling × 4种Radius × 亮点稀疏/密集两类代表字模，共至少96组。
- 另测无Border、有Border、Padding不足、Padding充足、Clip、ScaleDown、左中右及上中下边界组合。
- Glow像素必须出现在Active Geometry外部，Background、Inactive和Border像素不得被模糊。

Segment像素Case：

- 横段、竖段、斜段、交汇段、冒号、小数点至少6类 × 4种RenderScaling × 4种Radius，共至少96组。
- 验证字符级Glow一次处理全部活跃段，不能退化为逐段Blur。

缓存与生命周期Case：

- 重复字模、全支持字模、历史文本、Geometry参数抖动、Radius抖动、RenderScaling切换和Content Scale切换。
- 每项参数抖动至少1000次；每轮后断言缓存上界和当前代资源数量。
- LRU验收覆盖命中、淘汰顺序、帧内Pin、工作集统一降采样、最低比例超限和Brush关闭清空。
- Geometry失效、`GlowBrush=null`、Detach和控件失去引用分别执行WeakReference/显式资源释放验收。

### 第二层：专项性能审计

使用Labs性能工具独立运行，不放入普通PR单元测试时长预算。三条技术路线必须使用相同输入、相同进程配置和相同预热策略。

场景矩阵：

- 字符数量：6、16、64、256。
- RenderScaling：100%、125%、150%、200%。
- GlowRadius：2、6、12、24。
- Matrix：Circle、Square、RoundedSquare，分别测试仅亮点和亮暗双层。
- Segment：数字、字母、符号混合，覆盖低活跃段和高活跃段字符。
- 状态：Glow关闭、静态Glow、Opacity动画、Radius动画、动态文本。

每个静态场景至少预热600帧，再测量6000帧。每组Benchmark至少使用5个独立进程；报告Median、P95、Allocated Bytes、Gen0/1/2、MaskBuildCount、BlurBuildCount、绘制命令数、离屏物理像素总量和最终缓存数，不得只报告平均值。

初始时间目标：

- 16字符、200% RenderScaling、Radius=6的静态Glow，真实窗口CPU侧Glow处理P95目标不超过4ms。
- 64字符、200% RenderScaling、Radius=6的压力场景，整帧P95目标不超过16.67ms。
- Glow关闭路径相对无Glow基线中位耗时不得超过`1.05x`，分配必须相同。

这些是原型选型门槛。若测试环境证明指标不可比或目标不合理，必须保留原始数据、解释测量偏差并形成新的书面决议；不得静默放宽。

### 第三层：发布前长稳

- 静态Glow：固定6、16、64字符分别运行100000帧，预热后Mask/Blur新增数必须为0。
- Opacity呼吸：至少36000帧，Mask/Blur新增数必须为0，缓存数量恒定。
- Radius往返动画：至少36000帧，缓存始终只有当前配置代，旧后端资源及时释放。
- 动态文本：至少100000次固定长度更新和10000次长度/拓扑变化，缓存不得按历史槽位增长。
- DPI/Scale切换：100%、125%、150%、200%往返至少1000轮，旧配置资源不得存活累积。
- 多实例压力：1、10、50个Glow控件分别覆盖关闭、重复字模和不同字模；关闭Glow的50个控件必须保持零Glow缓存，移除控件后实例资源必须释放。
- 真实窗口连续运行至少30分钟，采集进程工作集、托管堆、Gen2次数、帧时间P95/P99和后端资源计数；内存曲线不得持续单调增长。
- 长稳结束后停止动画、Detach控件、释放窗口并强制完整回收；控件、Glow缓存和可释放后端资源必须通过生命周期验收。
- 离屏安全测试覆盖刚低于、等于、刚超过面积上限，单边超限、双边与面积同时超限、极端CharacterHeight、100%/200% DPI以及ScaleDown重新进入安全范围。
- 连续1000次跨越降采样阈值时，逻辑Glow Bounds保持一致、Active像素不受影响且旧Mask不累积。

### 选型失败条件

出现任一情况即阻止该路线进入正式实现：

- Glow关闭路径产生额外Mask、Blur、命令、订阅或持续分配。
- Opacity动画触发Mask或Blur重建。
- 缓存随历史文本、Radius、DPI或动画帧数无界增长。
- Background、Inactive或Border被错误纳入Glow。
- 依赖AtomUI成型控件包、非公开Avalonia API或未被明确批准的Skia后端耦合。
- Headless通过但真实窗口出现裁剪、DPI断层、资源泄漏或无法满足性能门槛。
- NativeAOT新增未解释的动态代码、反射或裁剪告警。

## 最终选型决议

```text
正式路线：路线C，Avalonia公开Scoped BlurEffect
Effect粒度：每个控件一次
路线A：仅保留实验基线，不进入正式运行时
路线B：停止，不进入正式运行时
Skia自定义：未启动，不进入正式运行时
```

最终实现使用`DrawingContext.PushEffect(BlurEffect, bounds)`隔离全部可见Active Geometry。Matrix与Segment分别完成基础Geometry和可见性剔除，共享同一个内部`LedGlowRenderer`，每个控件每帧最多建立一个Glow Effect作用域。相邻Active Geometry的Glow允许自然融合，清晰Active本体在Effect作用域退出后重新绘制。

路线B章节中的Alpha Mask、LRU、16 MiB派生缓存、单Mask尺寸和降采样公式只记录被评估路线的工程要求，不再是正式路线C的实现契约。正式路线不得为了机械满足路线B要求而创建应用层Mask或Glow缓存。路线C仍必须执行以下资源安全约束：Effect Bounds只来自已剔除的可见Active Geometry并受控件内容视口限制；异常或空Bounds跳过Glow但保留Active本体；Glow关闭不提交Effect命令、不创建后端资源。

不实现`PerGeometry`、Batch8或Batch16运行时分支，不增加`GlowQuality`、`GlowRenderMode`或粒度配置。不得根据字符数量、帧率或全局负载自动切换算法或关闭Glow。

首轮原型事实和Smoke数据见 [glow-prototype-evaluation.md](glow-prototype-evaluation.md)。
