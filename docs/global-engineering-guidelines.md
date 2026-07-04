# 全局工程规范

本仓库遵循 AtomUIV6 风格的集中构建和实验控件管理方式。

## 基本规则

- 仓库承载多个实验控件包，而不是单一聚合包。
- 每个实验控件独立项目、独立 NuGet 包、独立测试项目。
- Labs 包版本与 `AtomUIVersion` 强绑定，`AtomUILabsVersion` 必须直接来自 `$(AtomUIVersion)`。
- 新增实验控件默认使用 `AtomUI.Desktop.Controls.Labs.<ControlName>` 命名。
- Debug 面向当前开发框架 `net10.0`，Release 同时覆盖 `net10.0` 和 `net8.0`。
- 输出目录集中到 `output/`，构建产物不提交。

## 必读文档

- 工程规范总览：[docs/engineering/overview.md](engineering/overview.md)
- 构建、打包与发布：[docs/architecture/build-and-packaging.md](architecture/build-and-packaging.md)

