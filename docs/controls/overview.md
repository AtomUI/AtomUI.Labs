# 实验控件文档

本目录记录 AtomUI Labs 中各实验控件的长期设计、实现约束和验证资料。每个控件或控件家族使用独立子目录，入口文件统一命名为 `overview.md`。

文档可以先于源码进入本仓库，但必须明确区分目标设计、当前实现和历史验证结果。控件落地后，文档中的包名、公开 API、测试和 Gallery 验收应与源码同步维护。

## 控件目录

- [LED 控件家族](led/overview.md)：已实现的 `AtomUI.Labs.Controls.Led` 包，包含十四段 Segment、5x7 Matrix、Glow 和 Marquee。
- [ImageGallery](image-gallery/overview.md)：已完成首版源码施工的沉浸式图片集合查看控件，并已加入 `ResourceOnly`、当前主图安全 Lease 获取和解码尺寸提示；施工与复审从 [首版施工合同](image-gallery/implementation-contract.md) 进入，专项虚拟化与发布证据继续由该目录内验证文档约束。最终 RC 仍以完整 Gallery 人眼、真实图片/DPI/触控和发布闸门证据为准。
