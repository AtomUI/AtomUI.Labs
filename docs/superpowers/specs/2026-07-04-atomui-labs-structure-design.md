# AtomUI Labs Structure Design

## Goal

Create an AtomUI Labs repository structure for experimental controls that are still under validation. Labs must support multiple independently packaged controls while sharing AtomUI-compatible build, version, output, and packaging conventions.

## Decisions

AtomUI Labs is a multi-package repository, not a single aggregate NuGet package. Each experiment gets its own project under `src/`, its own package identity, and its own test project under `tests/`.

Labs package versions are strongly bound to AtomUI:

```xml
<AtomUILabsVersion>$(AtomUIVersion)</AtomUILabsVersion>
```

The repository follows the AtomUI DataGrid Commercial build style: root `Directory.*.props`, `build/*.props`, centralized package versions, `output/` for all build artifacts, and small PowerShell scripts for local pack and publish.

## Repository Layout

```text
AtomUI.Labs/
  AtomUI.Labs.slnx
  Directory.Build.props
  Directory.Build.targets
  Directory.Packages.props
  global.json
  build/
    Version.props
    Common.props
    PackageMetaInfo.props
    Output.props
    Output.App.props
  scripts/
    PackToLocal.ps1
    PublishToLocalSources.ps1
  src/
    <ExperimentalControlPackage>/
  tests/
    <ExperimentalControlPackage>.Tests/
  controlgallery/
    AtomUILabsGallery/
    AtomUILabsGallery.Desktop/
  docs/
    engineering/
    architecture/
```

## Build Behavior

`global.json` pins .NET SDK `10.0.300`.

Debug builds target `net10.0`. Release builds target `net10.0;net8.0`. All NuGet, binary, intermediate, and app outputs go under `output/`.

`Directory.Packages.props` centrally manages AtomUI, AtomUI.Base, Avalonia, Roslyn, and test package versions. `build/PackageMetaInfo.props` supplies default Labs package metadata while allowing each experiment to override `PackageId`, `Title`, `Description`, and `PackageTags`.

## Scripts

`scripts/PackToLocal.ps1` restores and builds `AtomUI.Labs.slnx`, runs discovered test projects under `tests/`, and packs discovered packable projects under `src/`.

`scripts/PublishToLocalSources.ps1` calls `PackToLocal.ps1`, then pushes generated `.nupkg` files from `output/Nuget/<Configuration>` to a local feed.

## Naming

New experimental control projects should use:

```text
AtomUI.Labs.Controls.<ControlName>
```

Corresponding test projects should use:

```text
AtomUI.Labs.Controls.<ControlName>.Tests
```

## Validation

The initial structure must validate with:

```bash
dotnet restore AtomUI.Labs.slnx
dotnet build AtomUI.Labs.slnx --configuration Debug
pwsh -NoProfile -File ./scripts/PackToLocal.ps1 -BuildType Debug
git diff --check
```
