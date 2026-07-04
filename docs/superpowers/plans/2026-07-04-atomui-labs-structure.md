# AtomUI Labs Structure Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create the AtomUI Labs repository and build structure for independently packaged experimental controls.

**Architecture:** The repository follows the AtomUI DataGrid Commercial build pattern, with centralized root MSBuild props and scripts. It adapts that single-package structure into a multi-package Labs structure where each experiment lives under `src/` and can be packed independently.

**Tech Stack:** .NET SDK 10.0.300, MSBuild, Central Package Management, PowerShell packaging scripts, Avalonia, AtomUI.

## Global Constraints

- Labs is a multi-package experimental controls repository, not a single aggregate NuGet package.
- `AtomUILabsVersion` must be bound to `$(AtomUIVersion)`.
- Debug builds target `net10.0`.
- Release builds target `net10.0;net8.0`.
- Build output must go under `output/`.
- Existing staged user files must not be reverted or included in unrelated commits.

---

### Task 1: Root Repository Metadata

**Files:**
- Create: `.editorconfig`
- Create: `.gitattributes`
- Create: `.gitignore`
- Create: `AGENTS.md`
- Create: `README.md`
- Create: `CHANGELOG.md`
- Create: `COPYRIGHT.md`
- Create: `LICENSE`
- Create: `README.nuget.md`

**Interfaces:**
- Consumes: the user-approved multi-package Labs direction.
- Produces: repository-level metadata used by developers and package builds.

- [ ] **Step 1: Add root metadata files**

Create the repository metadata files with LF line endings, .NET output ignores, and Labs-specific README content.

- [ ] **Step 2: Verify metadata files are tracked by git status**

Run: `git status --short`

Expected: the new root metadata files appear as untracked or staged files, and `resources/images/atomui-labs.png` remains unchanged.

### Task 2: Central Build Configuration

**Files:**
- Create: `AtomUI.Labs.slnx`
- Create: `global.json`
- Create: `Directory.Build.props`
- Create: `Directory.Build.targets`
- Create: `Directory.Packages.props`
- Create: `build/Version.props`
- Create: `build/Common.props`
- Create: `build/PackageMetaInfo.props`
- Create: `build/Output.props`
- Create: `build/Output.App.props`

**Interfaces:**
- Consumes: DataGrid-style centralized MSBuild structure.
- Produces: shared build properties for future Labs projects.

- [ ] **Step 1: Add solution and SDK pin**

Create `AtomUI.Labs.slnx` and `global.json` using SDK `10.0.300`.

- [ ] **Step 2: Add central props**

Create root MSBuild props and package version management. Set `AtomUILabsVersion` to `$(AtomUIVersion)`.

- [ ] **Step 3: Verify restore and build**

Run: `dotnet restore AtomUI.Labs.slnx`

Expected: restore exits successfully.

Run: `dotnet build AtomUI.Labs.slnx --configuration Debug`

Expected: build exits successfully.

### Task 3: Packaging Scripts

**Files:**
- Create: `scripts/PackToLocal.ps1`
- Create: `scripts/PublishToLocalSources.ps1`

**Interfaces:**
- Consumes: `AtomUI.Labs.slnx`, `src/**/*.csproj`, `tests/**/*.csproj`, and centralized output paths.
- Produces: local NuGet packages in `output/Nuget/<Configuration>` and optional push to a local feed.

- [ ] **Step 1: Add pack script**

Create `PackToLocal.ps1` to restore, build, test discovered test projects, and pack discovered packable projects under `src/`.

- [ ] **Step 2: Add publish script**

Create `PublishToLocalSources.ps1` to call `PackToLocal.ps1` and push generated `.nupkg` files to a local source.

- [ ] **Step 3: Verify script syntax and empty-repo behavior**

Run: `pwsh -NoProfile -File ./scripts/PackToLocal.ps1 -BuildType Debug`

Expected: restore/build succeeds and the script reports that no packable projects were found under `src/`.

### Task 4: Documentation and Placeholders

**Files:**
- Create: `docs/global-engineering-guidelines.md`
- Create: `docs/engineering/overview.md`
- Create: `docs/architecture/build-and-packaging.md`
- Create: `src/.gitkeep`
- Create: `tests/.gitkeep`
- Create: `controlgallery/.gitkeep`

**Interfaces:**
- Consumes: the agreed Labs repository model.
- Produces: documented conventions for future controls and tracked empty directories.

- [ ] **Step 1: Add engineering docs**

Create engineering and architecture docs covering package naming, version binding, output layout, and validation commands.

- [ ] **Step 2: Add tracked placeholders**

Add `.gitkeep` files to keep `src/`, `tests/`, and `controlgallery/` visible before the first experimental control is added.

- [ ] **Step 3: Verify whitespace**

Run: `git diff --check`

Expected: no whitespace errors.

