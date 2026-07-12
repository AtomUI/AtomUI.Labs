# 工程规范总览

AtomUI Labs 是 AtomUI 生态中实验控件的集合仓库。仓库优先验证控件方向、主题能力、交互模型和 AOT 友好实现；成熟后再迁移到稳定控件包或独立产品仓库。

## 仓库定位

- Labs 不是单一聚合 NuGet 包。
- 每个实验控件必须独立项目、独立 `PackageId`、独立测试目录。
- Labs 包版本跟随 `AtomUIVersion`，不维护独立主版本。
- 实验控件 API 默认不承诺长期稳定；对外暴露前必须在文档中标注实验状态。

## 目录约定

```text
src/<PackageName>/
tests/<PackageName>.Tests/
controlgallery/AtomUILabsGallery/
controlgallery/AtomUILabsGallery.Desktop/
docs/architecture/
docs/engineering/
```

新增控件时，优先使用包名和项目名：

```text
AtomUI.Labs.Controls.<ControlName>
```

测试项目使用：

```text
AtomUI.Labs.Controls.<ControlName>.Tests
```

## 构建约束

- 使用 .NET SDK `10.0.300`。
- Debug 只构建 `net10.0`。
- Release 构建 `net10.0;net8.0`。
- 依赖版本统一放在 `Directory.Packages.props`。
- 版本号统一放在 `build/Version.props`。
- 输出统一进入 `output/`，不要提交构建产物。

## 开发约束

- 优先沿用 AtomUIV6 的项目结构、命名、主题注册和 source generator 模式。
- 不复制 AtomUI 基础库源码；默认通过 NuGet 依赖 `AtomUI.Desktop.Controls`、`AtomUI.Generator`、`AtomUI.Base` 或 `AtomUI.Base.Generator`。
- 代码组织必须围绕真实控件、主题、行为、模块和运行阶段，不创建 `Api/`、`Contracts/`、`Managers/`、`Helpers/` 这类泛化技术桶目录。
- 接口只在存在多个真实实现、测试替身、跨包扩展点或框架要求时引入。
- NativeAOT 和 trimming 友好是硬约束；同一需求存在 AOT 友好实现和运行时反射/动态发现实现时，必须选择 AOT 友好实现。
- 能用 source generator、显式注册或 descriptor 解决的路径，不使用运行时反射扫描。
- 不手动修改 generated files；需要修改生成结果时改 generator 输入或 generator 本身。

## 验证

收尾前至少运行：

```bash
dotnet build AtomUI.Labs.slnx --configuration Debug
git diff --check
```

涉及 Release、打包、AOT 或发布脚本时，额外运行对应 Release build 和 pack。
