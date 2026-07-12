# 构建、打包与发布设计

AtomUI Labs 使用与 AtomUI DataGrid Commercial 一致的集中式 MSBuild 配置，但仓库定位从单个商业控件包调整为多个实验控件包集合。

## Target Framework

`build/Common.props` 定义统一目标框架：

- 开发目标框架：`net10.0`
- 生产目标框架：`net8.0`
- Debug：只构建 `net10.0`
- Release：构建 `net10.0;net8.0`

新增实验控件项目应通过 `TargetFrameworks` 使用 `$(AtomUITargetFrameworks)`。测试、工具和 Gallery host 可以直接使用 `$(AtomUIDevelopTargetFramework)`。

## 版本绑定

Labs 包版本必须跟随 AtomUI 主版本：

```xml
<AtomUILabsVersion>$(AtomUIVersion)</AtomUILabsVersion>
```

各实验包默认使用 `$(AtomUILabsVersion)` 作为 `Version`。不为 Labs 维护独立主版本线。

## 包身份

每个实验控件独立发布 NuGet 包：

- 项目路径：`src/<PackageName>/<PackageName>.csproj`
- 包名：默认等于项目名，项目可显式覆盖 `PackageId`
- 默认标题：`AtomUI Labs Controls`
- 默认描述：`Experimental controls for AtomUI desktop applications.`

推荐包名前缀为 `AtomUI.Labs.Controls.<ControlName>`。实验控件转正后，应迁移到稳定包名并在 Labs 包中记录废弃策略。

## 输出路径

`build/Output.props` 将产物统一写入：

- NuGet 包：`output/Nuget/<Configuration>`
- 二进制：`output/bin/<Configuration>`
- 中间产物：`output/<ProjectName>/obj`

`build/Output.App.props` 将 Gallery 或桌面应用产物写入：

- app：`output/App/<Configuration>/<ProjectName>`

`output/` 不提交到仓库。

## 打包脚本

`scripts/PackToLocal.ps1` 执行：

1. restore `AtomUI.Labs.slnx`
2. build `AtomUI.Labs.slnx`
3. test `tests/**/*.csproj`
4. pack `src/**/*.csproj` 中未显式设置 `IsPackable=false` 的项目

`scripts/PublishToLocalSources.ps1` 调用打包脚本后，将 `output/Nuget/<Configuration>/*.nupkg` 推送到指定本地 feed。

## 验证

结构或构建脚本调整后至少运行：

```bash
dotnet restore AtomUI.Labs.slnx
dotnet build AtomUI.Labs.slnx --configuration Debug
pwsh -NoProfile -File ./scripts/PackToLocal.ps1 -BuildType Debug
git diff --check
```

新增真实实验包后，还必须运行对应测试项目和 Release pack。
