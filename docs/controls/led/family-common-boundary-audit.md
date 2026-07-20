# LED 家族公共边界审计

> 文档状态：迁移参考。本文于 2026-07-20 从 AtomUI 仓库的 `dev-and-mark/modules/desktop-controls-labs` 复制到 AtomUI.Labs 并适配文档结构。`AtomUI.Labs.Controls.LED` 表示本仓库的目标设计；文中的“已实现”、验证数据及旧项目命令来自迁移前的 `AtomUI.Desktop.Controls.Labs` 参考实现，不表示当前仓库已经包含相应源码、测试或性能工具。

- 审计日期：2026-07-10
- 审计对象：`LED.Segment` 与 `LED.Matrix`
- 目标：只提取已经由两套稳定实现证明语义相同的内部基础代码
- 公共 API 变化：无

## 最终结论

当前 LED 家族共享两项内部基础设施：

```text
LEDCharacterNormalizer
  ASCII小写 -> 大写

LEDDisplayLayoutMath
  理想尺寸 + 最终Bounds -> ScaleDown比例 + 内容对齐偏移
```

二者都是无状态纯计算，不读取控件属性，不持有缓存，不生成Geometry，也不提交DrawingContext命令。

## 新增共享边界

`LEDDisplayLayoutMath`直接位于`LED/`根目录，不新增`Primitives`、`Shared`或`Internal`目录。

它只包含：

- `CalculateScaleDown(Size desiredSize, Size bounds)`：按宽高限制等比缩小，最大值为1；非正或非有限尺寸返回1。
- `CalculateAlignmentOffset(...)`：根据缩放后内容尺寸计算Left/Center/Right、Top/Center/Bottom偏移，Stretch按Center处理。

Segment和Matrix仍各自判断自己的公开`OverflowMode`是否为`ScaleDown`，共享工具不知道两个公开枚举的存在。

## 保持独立的部分

| 候选 | 结论 | 原因 |
|---|---|---|
| `SegmentOverflowMode` / `MatrixOverflowMode` | 不共享 | 二者是已经冻结的独立公开合同，合并会造成API变更和路线耦合 |
| StyledProperty与控件基类 | 不共享 | 公共基类会扩大公开API，并把两条显示路线强制绑定到同一继承合同 |
| ValueSanitizer | 不共享 | Matrix对布局参数设置`1,000,000`上限；Segment没有该上限且额外支持范围规整 |
| LayoutEngine、Layout、Slot | 不共享 | Segment支持窄符号和最终高度约束；Matrix使用固定5x7等宽Rune布局 |
| CharacterMap与fallback | 不共享 | Segment未知字符为空格，Matrix未知Unicode标量为问号，语义不同 |
| Geometry与缓存 | 不共享 | Segment缓存字符槽拓扑和十四段骨架；Matrix按字模bit缓存亮暗点Geometry |
| AutomationPeer | 不共享 | 外壳相似，但提取需要基类、接口或委托，两个消费者不足以抵消复杂度 |
| ControlTheme | 不共享 | 每个ControlTheme必须保持独立目标类型和可独立演进的主题合同 |
| 背景绘制 | 不共享 | 只有一次`DrawRectangle`调用，提取helper只会隐藏直观代码 |

## 控件保留职责

Segment继续拥有：

- 最终Bounds参与字符高度布局。
- Segment专属几何缓存与Glow绘制。
- 将`SegmentOverflowMode`映射为是否调用共享ScaleDown计算。

Matrix继续拥有：

- 固定5x7布局和Rune字符合同。
- 可见字符二分剔除及视口逆变换。
- 将`MatrixOverflowMode`映射为是否调用共享ScaleDown计算。

## 验证要求

- 共享数学直接测试宽限制、高限制、不放大、非正/非有限尺寸和全部对齐模式。
- Segment与Matrix现有Render、Pixel、Clip和ScaleDown测试必须保持通过。
- Labs完整测试、Sample Debug/Release、net8/net10 pack和依赖扫描必须保持通过。
- `docs`不得修改。

## 验证结果

- `LEDDisplayLayoutMathTests`：16/16通过。
- 布局数学、Segment Render、Matrix Render/Pixel相关测试：92/92通过。
- Labs全量测试：275/275通过，Release net10.0。
- Labs Sample Debug/Release：均为0 warning、0 error。
- Labs NuGet pack：net8/net10成功，依赖仍只有`AtomUI.Core`和Avalonia。
- 共享工具静态扫描：不引用Segment、Matrix、Control、StyledProperty、Geometry或DrawingContext。

本次提取是内部去重，不声明性能收益，也不改变视觉结果。
