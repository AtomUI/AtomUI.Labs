<p align="center">
  <img src="resources/images/atomui-labs.png" alt="AtomUI Labs" width="100%" />
</p>

# AtomUI Labs

[简体中文](README.zh-CN.md)

AtomUI Labs is the experimental desktop controls workspace for the AtomUI ecosystem. It is where new desktop controls can be designed, validated, packaged, and tested before they are promoted into stable AtomUI packages.

## Purpose

AtomUI Labs keeps experimental controls independent from the stable AtomUI surface while still following the same engineering discipline:

- every experimental control has its own project and NuGet package;
- behavior changes should be covered by a matching test project;
- package versions stay aligned with the AtomUI version used by the repository;
- build, pack, publish, and output conventions are centralized at the repository root.

## Installation

AtomUI Labs does not publish a single aggregate package. Install the Labs package for the control you want to use.

Labs package names follow this pattern:

```bash
dotnet add package AtomUI.Labs.Controls.<ControlName>
```

For example, install the currently implemented LED family with:

```bash
dotnet add package AtomUI.Labs.Controls.Led
```

Use a Labs package version that matches your AtomUI package version.

## Common Commands

```bash
dotnet restore AtomUI.Labs.slnx
dotnet build AtomUI.Labs.slnx --configuration Debug
pwsh ./scripts/PackToLocal.ps1 -BuildType Release
pwsh ./scripts/PublishToLocalSources.ps1 -LocalSourcesDir /path/to/local-feed -BuildType Release
git diff --check
```

Package artifacts are written to `output/Nuget/<Configuration>`.

## Documentation

- [Engineering overview](docs/engineering/overview.md)
- [Build and packaging](docs/architecture/build-and-packaging.md)
- [Experimental controls](docs/controls/overview.md)
- [LED control family](docs/controls/led/overview.md)
- [Global engineering guidelines](docs/global-engineering-guidelines.md)

## Status

Labs packages are experimental. Public APIs may change before a control is promoted to a stable AtomUI package.
