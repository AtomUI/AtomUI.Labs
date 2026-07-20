# Matrix V2 静态视觉系统

> 文档状态：迁移参考。本文于 2026-07-20 从 AtomUI 仓库的 `dev-and-mark/modules/desktop-controls-labs` 复制到 AtomUI.Labs 并适配文档结构。`AtomUI.Labs.Controls.LED` 表示本仓库的目标设计；文中的“已实现”、验证数据及旧项目命令来自迁移前的 `AtomUI.Desktop.Controls.Labs` 参考实现，不表示当前仓库已经包含相应源码、测试或性能工具。

本文定义`MatrixDisplay`第二阶段的静态点轮廓系统。它只改变每个点的外轮廓，不改变字模、点位坐标、测量尺寸、颜色合同、溢出策略或动态文本行为。

后续增量加入可选面板边框。边框属于显示器外壳，不属于点轮廓，也不由Labs主题强制开启。

## 视觉目标

三种形状表达三种明确视觉语言：

| 形状 | 肉眼观感 | 适合场景 |
|---|---|---|
| `Circle` | 四角留白最多，柔和，像独立LED灯珠 | 仪表、电子设备、复古点阵屏 |
| `Square` | 四角填满，同样DotSize下视觉面积最大，字符更粗、更密、更像像素屏 | 公告牌、像素字体、低分辨率终端 |
| `RoundedSquare` | 密度接近方形但边缘更柔和，位于圆形和方形之间 | 现代信息面板、状态屏和较大点尺寸 |

三种形状都占据同一个`DotSize x DotSize`外接框。切换形状不能改变控件`DesiredSize`、字符间距、Padding、对齐或ScaleDown比例。

`DotSize`在当前合同中表示点轮廓外接框边长：Circle下它同时是圆直径；Square和RoundedSquare下它是正方形边长。

## 公共API

```csharp
public enum MatrixDotShape
{
    Circle,
    Square,
    RoundedSquare
}
```

`MatrixDisplay`新增：

| 属性 | 类型 | 默认值 | 作用 |
|---|---|---:|---|
| `DotShape` | `MatrixDotShape` | `Circle` | 选择点轮廓 |
| `DotCornerRadiusRatio` | `double` | `0.25` | RoundedSquare圆角半径相对DotSize的比例 |

圆角比例有效范围为`0..0.5`：

```text
0.00  完全直角，视觉等同Square
0.10  轻微削弱尖角，仍明显像方形
0.25  默认平衡值
0.50  圆角半径达到半边长，轮廓趋近Circle
```

负数截断为0，超过0.5截断为0.5，NaN和Infinity使用0。Circle和Square忽略圆角比例。非法`MatrixDotShape`枚举值按Circle处理。

开发者设置的原始StyledProperty值不回写；规整只作用于Geometry和缓存键，保持Avalonia binding优先级不变。

## Geometry实现

`MatrixGlyphGeometryFactory`继续在固定7行、5列循环中把35个点互斥分流到亮点和暗点Geometry。亮暗状态使用同一种形状。

- Circle：保留MVP原有的两段ArcTo闭合圆路径，确保默认像素视觉不变。
- Square：使用四条直边形成闭合填充子路径。
- RoundedSquare：使用四条直边和四段四分之一圆弧形成闭合子路径。

每个点仍是独立子路径。不得把相邻方点连接成一个多点矩形，也不得为三种形状建立三套Render流程。

## 缓存和失效

Geometry缓存键包含：

```text
Glyph Bits
有效DotSize
有效DotSpacing
有效DotShape
有效DotCornerRadiusRatio
```

规则：

- `DotShape`变化：清空当前实例Geometry缓存，只触发Render。
- RoundedSquare下`DotCornerRadiusRatio`变化：清空Geometry缓存，只触发Render。
- Circle或Square下圆角比例变化：不清Geometry缓存，但StyledProperty仍触发Render。
- 形状和圆角比例不进入layout缓存键，不触发Measure。
- 形状历史不得在实例缓存中累计；缓存只保留当前视觉参数对应的固定字模集合。
- 每字模仍最多提交一个亮点Geometry和一个暗点Geometry命令。

## 主题边界

本轮不创建Matrix Control Token，也不在ControlTheme中为形状增加Setter。代码默认Circle保证MVP兼容；开发者通过StyledProperty、Style或ControlTheme覆盖形状和圆角比例。

背景`CornerRadius`与点的`DotCornerRadiusRatio`职责不同：前者控制整个面板背景，后者只控制单个RoundedSquare点位。

## 可选面板边框

`MatrixDisplay`增量公开`BorderBrush`和`BorderThickness`。默认分别为`null`和`0`，因此现有视觉、测量和绘制命令保持不变。边框只有在Brush非空且至少一边有效厚度大于0时绘制；Thickness即使在Brush为空时仍参与布局，允许开发者隐藏边框而不引发布局跳动。

```text
控件Bounds
┌──────────── Border ────────────┐
│  ┌──────── Content viewport ─┐ │
│  │ Padding + Matrix glyphs   │ │
│  └───────────────────────────┘ │
└────────────────────────────────┘
```

Background先铺满外框，字符只在Border内侧视口中对齐、裁剪或ScaleDown，Border最后绘制。ScaleDown不得缩放边框DIP厚度。均匀边框使用Pen快速路径；非均匀边框使用外、内圆角Geometry排除形成单个填充环。复杂Geometry缓存只保留当前Bounds、有效Thickness和CornerRadius对应的一份，不随历史属性值增长。

边框不创建Control Token，不在Matrix ControlTheme中设置默认值。开发者通过StyledProperty、Style或自定义ControlTheme主动开启。

## 非目标

本阶段不实现：

- 亮点和暗点使用不同形状。
- 椭圆、菱形、六边形、自定义Geometry或旋转。
- 亮暗点独立尺寸和缩放比例。
- Glow、阴影、扫描线、材质和高光。
- 闪烁、呼吸、滚动字幕或其它动画。

## 待决议 TODO

`ActiveDotScale`和`InactiveDotScale`是候选的风格化能力，用于通过不同的点位尺寸进一步区分亮点和暗点。它不是工业LED点阵的通用默认行为，不进入当前实现计划，也不承诺一定实现。

当前行为继续保持亮点和暗点尺寸相同，只通过`ActiveBrush`、`InactiveBrush`、Brush透明度和`ShowInactiveDots`区分状态。

重新评估该能力前必须明确：

- 是否存在仅靠Brush无法满足的真实使用场景，例如低对比度、无障碍或明确的风格化需求。
- 扩充公共API和Geometry缓存键是否值得。
- 产品是否接受它属于软件视觉增强，而不是对物理LED灯珠的严格模拟。
- 若决定实现，两个属性的默认值必须都是`1.0`，确保现有视觉和布局行为不变。

## 验收

- Circle默认像素和MVP基线保持一致。
- 三种形状在100%、125%、150%、200% RenderScaling下保持每个点独立连通。
- 同样参数下视觉面积满足Circle小于RoundedSquare、RoundedSquare小于Square。
- Shape切换和RoundedSquare圆角变化正确释放旧Geometry，不增长历史缓存。
- Sample使用完全相同的文本、尺寸、间距和颜色展示三种形状，并展示至少两个RoundedSquare比例。
- 可选边框覆盖默认无边框、均匀边框、非均匀边框和半透明边框；四边厚度与高DPI连通性由像素测试验证。

## 实现验证记录

2026-07-10完成以下自动验证：

- Labs全量测试：315/315通过。
- Matrix定向测试：154/154通过。
- Sample Debug和Release构建：0警告、0错误。
- Labs性能基准项目Release构建：0警告、0错误。
- NuGet打包：同时产出net8.0和net10.0程序集，依赖仅包含AtomUI.Core与Avalonia。
- Sample win-x64 NativeAOT发布成功；仅保留AtomUI.Core既有的3条裁剪/AOT告警，Matrix静态视觉实现未新增告警。

真实窗口中的三种形状对比仍需人工视觉验收，自动像素测试不能替代该步骤。

2026-07-11完成可选面板边框自动验证：

- Matrix定向测试：173/173通过。
- Labs全量测试：334/334通过。
- Sample Debug和Release构建：0警告、0错误。
- Labs性能基准项目Release构建：0警告、0错误。
- NuGet继续同时产出net8.0和net10.0，依赖仅包含AtomUI.Core与Avalonia。
- Sample win-x64 NativeAOT发布成功；仍只有AtomUI.Core既有的3条裁剪/AOT告警，边框实现未新增告警。

真实窗口中的四组边框Sample仍需人工视觉验收。

## 相关设计

- [LED Glow 技术路线选型](glow-technical-options.md)：Glow属于Matrix基础显示之上的可选增强层，当前仍处于技术选型阶段。
