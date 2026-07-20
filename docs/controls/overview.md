# 实验控件文档

本目录记录 AtomUI Labs 中各实验控件的长期设计、实现约束和验证资料。每个控件或控件家族使用独立子目录，入口文件统一命名为 `overview.md`。

文档可以先于源码进入本仓库，但必须明确区分目标设计、当前实现和历史验证结果。控件落地后，文档中的包名、公开 API、测试和 Gallery 验收应与源码同步维护。

## 控件目录

- [LED 控件家族](led/overview.md)：已实现的 `AtomUI.Labs.Led` 包，包含十四段 Segment、5x7 Matrix、Glow 和 Marquee。
