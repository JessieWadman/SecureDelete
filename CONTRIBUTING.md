# Contributing to SecureDelete

This document explains how to build, test, and ship SecureDelete, including how
the automated versioning and winget publishing work. Read it before opening a
pull request or cutting a release.

## Prerequisites

- **.NET 10 SDK** (Windows x64)
- **Inno Setup 6** — only needed to build the installer locally
  (`winget install JRSoftware.InnoSetup`)
- **Microsoft Sysinternals SDelete** — only needed to exercise real deletions
  during manual testing (`winget install Microsoft.Sysinternals.SDelete`)

## Repository layout

```
SecureDelete.sln
src/SecureDelete/          WPF app
tests/SecureDelete.Tests/  xUnit tests
installer/SecureDelete.iss Inno Setup script (version injected in CI)
winget/                    winget manifest templates (placeholders filled in CI)
docs/MANUAL-TEST-CHECKLIST.md
.github/workflows/         ci, release-please, release
```

## Build, test, package (local)

```powershell
# Restore, build, run all tests
dotnet test SecureDelete.sln -c Release

# Publish the app (self-contained x64; includes .NET runtime + WPF)
dotnet publish src/SecureDelete/SecureDelete.csproj -c Release -r win-x64 `
    --self-contained true -p:PublishSingleFile=false -o artifacts/publish

# Build the installer (version defaults to the .iss value locally;
# CI injects the real version with /DAppVersion=x.y.z)
& "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe" installer/SecureDelete.iss
```

Outputs: `artifacts/publish/SecureDelete.exe`,
`artifacts/installer/SecureDelete-<version>-Setup.exe`.

`bin/`, `obj/`, and `artifacts/` are git-ignored — never commit build output.

## Branching and commit messages

- Work on a branch and open a PR into `main`. Keep PRs focused.
- **Commit messages use [Conventional Commits](https://www.conventionalcommits.org/).**
  This is what drives automatic versioning — the type of your commits decides the
  next version number.

  | Commit prefix                         | Example                          | Version effect      |
  | ------------------------------------- | -------------------------------- | ------------------- |
  | `fix:`                                | `fix: handle locked file error`  | patch (1.0.0→1.0.1) |
  | `feat:`                               | `feat: add 20-pass option`       | minor (1.0.0→1.1.0) |
  | `feat!:` / `BREAKING CHANGE:` footer  | `feat!: drop Win10 support`      | major (1.0.0→2.0.0) |
  | `chore:`/`ci:`/`docs:`/`refactor:`/`test:` | `docs: fix typo`            | no release          |

- Do **not** add co-author or tool-generated attribution trailers to commits or PRs.

## How a release happens (automated)

Releasing is driven by [release-please](https://github.com/googleapis/release-please)
and two GitHub Actions workflows. You normally do **not** tag or edit version
numbers by hand.

1. **Merge conventional commits into `main`.**
2. **release-please** (`.github/workflows/release-please.yml`) opens/updates a
   *release PR* titled `chore(main): release X.Y.Z`. It computes `X.Y.Z` from the
   commits and updates `CHANGELOG.md`, `.release-please-manifest.json`, and the
   `<Version>` in `SecureDelete.csproj`.
3. **Merge the release PR.** release-please then creates the `vX.Y.Z` tag and the
   GitHub Release.
4. Because release-please runs under a PAT (see *Secrets* below), publishing the
   release triggers **`.github/workflows/release.yml`**, which:
   - runs tests,
   - publishes the self-contained app with the release version,
   - installs Inno Setup and builds the installer (`/DAppVersion=X.Y.Z`),
   - attaches `SecureDelete-X.Y.Z-Setup.exe` to the Release,
   - renders the `winget/` manifest templates and submits them to
     `microsoft/winget-pkgs` via `wingetcreate submit`.

That is the whole release: **merge conventional commits → merge the release PR.**

### Forcing a specific version

Add a footer to any commit that release-please will process:

```
Release-As: 1.0.0
```

(Used once to make the first release `1.0.0` instead of `1.1.0`.)

### Manual release (fallback)

If you need to rebuild/re-submit a version that already has a tag+release, run the
**Release** workflow manually (Actions → Release → *Run workflow*) with the
`version` input, or:

```powershell
gh workflow run "Release" -f version=1.0.0
```

## winget publishing details

- The package identifier is **`JessieWadman.SecureDelete`**; users can install with
  `winget install securedelete` (via the `Moniker`).
- The three manifest templates live in `winget/` with placeholders
  (`__VERSION__`, `__INSTALLER_URL__`, `__INSTALLER_SHA256__`) that the release
  workflow fills in. Editing package metadata (description, tags, URLs) is done
  in `winget/JessieWadman.SecureDelete.locale.en-US.yaml`.
- `wingetcreate submit` on a complete manifest works for both the **first**
  submission and every **update** — there is no separate `new` vs `update` path.
- The installer manifest declares a dependency on
  **`Microsoft.Sysinternals.SDelete`**, so `winget install` pulls SDelete from
  Microsoft first. SDelete is never bundled or redistributed by this project.
- After the workflow submits, the change is a PR against `microsoft/winget-pkgs`;
  Microsoft's bot validates it and maintainers merge it. That step is outside this
  repo's control — watch the PR for validation feedback.

## Secrets and one-time setup

- **`WINGET_TOKEN`** (repository secret) — a classic PAT with **`repo`** and
  **`workflow`** scopes, belonging to the account that owns a fork of
  `microsoft/winget-pkgs`. It is used both by release-please (so releases trigger
  downstream workflows) and by `wingetcreate` (to open the manifest PR).
- The PAT owner must have a **fork of `microsoft/winget-pkgs`**
  (`gh repo fork microsoft/winget-pkgs`).
- Repository setting **Actions → General → Workflow permissions** must allow
  *“Allow GitHub Actions to create and approve pull requests.”*

## Checking status

```powershell
gh run list --repo JessieWadman/SecureDelete            # workflow runs
gh pr list  --repo JessieWadman/SecureDelete            # release PR
gh release list --repo JessieWadman/SecureDelete        # releases
gh pr list  --repo microsoft/winget-pkgs --author JessieWadman   # winget PRs
```

## Manual testing

Explorer integration and the GUI cannot be unit-tested. Before a notable release,
run through [`docs/MANUAL-TEST-CHECKLIST.md`](docs/MANUAL-TEST-CHECKLIST.md).
