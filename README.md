# SecureDelete

A small, safe **Windows Explorer wrapper** around Microsoft Sysinternals
**SDelete**. It adds a **Secure Delete** command to the right-click menu for
files and folders, shows a clear confirmation dialog, and then invokes SDelete
to securely overwrite and delete the selected item.

SecureDelete does not perform the overwrite itself — it is a thin, careful
front-end. All secure-erase work is done by Microsoft's official SDelete.

---

## What it does

- Registers a classic Explorer context-menu verb, **Secure Delete**, for files and folders.
- Always shows a confirmation dialog before doing anything destructive.
- Lets you choose the number of overwrite passes (1, 3, 7, or 10; default 1).
- Runs `SDelete.exe` out-of-process with the target passed as a single argument.
- Shows a compact progress view and a clear success/failure result.
- Runs entirely as the current user, with **no elevation**.

## It wraps Microsoft Sysinternals SDelete

The actual secure overwrite is performed by
[**SDelete**](https://learn.microsoft.com/en-us/sysinternals/downloads/sdelete),
a free Microsoft Sysinternals tool that implements the DoD 5220.22-M clearing
pattern.

### SDelete is a prerequisite and is **not** bundled

Under Microsoft's Sysinternals license, SDelete **may not be redistributed by
third parties**. SecureDelete therefore does **not** include it and never
downloads it automatically. You must obtain it from Microsoft:

> **Official download:** https://learn.microsoft.com/en-us/sysinternals/downloads/sdelete

After downloading, place **`sdelete64.exe`** (or `sdelete.exe`) either:

1. next to `SecureDelete.exe` in the installation folder, or
2. anywhere on your `PATH`.

If SDelete cannot be found when you run **Secure Delete**, SecureDelete shows a
dialog with a button that opens the official Microsoft download page.

### How SDelete is discovered

SecureDelete looks for SDelete in this order:

1. `sdelete64.exe`, then `sdelete.exe`, in the same folder as `SecureDelete.exe`.
2. `sdelete64.exe`, then `sdelete.exe`, on the `PATH`.

(A configured location could be added between these two steps in future; it is
not currently implemented.)

---

## Installation

1. Download and install **SDelete** from Microsoft (see above).
2. Run **`SecureDelete-1.0.0-Setup.exe`**.
   - The installer is **per-user** and does **not** require administrator
     rights for normal installation. It installs to
     `%LocalAppData%\Programs\SecureDelete` and registers the context-menu verb
     under `HKEY_CURRENT_USER`.
   - No service, no startup entry, no desktop/Start-menu shortcuts, no telemetry.
3. Place `sdelete64.exe` next to `SecureDelete.exe`, or ensure it is on your `PATH`.

### Explorer integration

The installer writes a **classic** shell verb to the registry:

```
HKCU\Software\Classes\*\shell\SecureDelete            (files)
HKCU\Software\Classes\Directory\shell\SecureDelete    (folders)
    (Default)      = "Secure Delete"
    Icon           = "<install>\SecureDelete.exe,0"
    CommandFlags   = 0x60      ; separator above + below
    command\(Default) = "<install>\SecureDelete.exe" --delete "%1"
```

- The verb is registered for **files** and **folders** only — never for drives.
- `CommandFlags = 0x60` requests a **separator above and below** the item
  (`ECF_SEPARATORBEFORE | ECF_SEPARATORAFTER`), where Explorer honors it.
- The application icon is used for the menu item.
- There is **no cascading submenu**.
- The target is passed via `%1` as a single quoted argument and handed to
  `ProcessStartInfo.ArgumentList` — it is never concatenated into a shell
  command line, so there is no command-injection surface.

### Windows 11 "Show more options"

On Windows 11, classic shell verbs appear under **Show more options** (the
legacy context menu, also reachable with **Shift+F10**), not in the compact
primary menu. This is expected and intentional: placing the command in the
primary Windows 11 menu would require an `IExplorerCommand` COM shell
extension, which this project deliberately avoids for simplicity and
maintainability.

---

## Using it

1. Right-click a file or folder → (on Windows 11) **Show more options** →
   **Secure Delete**.
2. A confirmation dialog appears showing the full target path:
   - For a file: *"Securely delete this file?"*
   - For a folder: *"Securely delete this folder and its contents?"*
   - Choose **Overwrite passes** (1 / 3 / 7 / 10; default 1).
   - Click **Cancel** to abort with no changes, or **Secure Delete** to proceed.
3. A progress view is shown while SDelete runs, followed by a clear
   success or failure result. On failure, the SDelete exit code and error
   output are available under **Details**.

Confirmation is **always** shown and can never be disabled or remembered.

### Overwrite passes

The selector offers **1, 3, 7, 10**, defaulting to **1**. The value is
validated before use even though it comes from a fixed list. More passes take
longer and, on modern storage, rarely add meaningful assurance (see
limitations below).

### Cancellation

- The confirmation dialog is always cancellable (**Cancel**).
- While SDelete is running, an **Abort** button terminates the SDelete process
  (and its process tree).

**Aborting is not an undo.** If you abort mid-run, the item may have been
**partially overwritten and/or partially deleted**. Nothing is recovered or
restored, and no claim is made that the data is intact or recoverable.

---

## Secure-deletion limitations (please read)

SecureDelete is a convenient safe wrapper around SDelete. It does **not** change
SDelete's underlying guarantees, and overwrite-based deletion has real limits.

**Overwriting a logical file does not guarantee physical erasure** on, among
others:

- **SSD / NVMe / flash** storage with wear leveling and over-provisioning
- **copy-on-write** filesystems (e.g. ReFS)
- filesystem or storage **snapshots**
- **backups** (local or cloud)
- **remote / network storage** (SMB/UNC shares, cloud-synced folders)
- **storage virtualization** / thin provisioning

On these technologies the physical media may retain earlier copies of the data
that a logical overwrite cannot reach. SecureDelete warns when the target is on
a **network location**, where secure overwrite generally cannot be guaranteed.

For disposing of a storage **device**, rely on **full-disk encryption** and
**cryptographic erasure** (destroying the key), and/or vendor secure-erase /
physical destruction — not file-level overwriting.

### Reparse points (junctions, symlinks, mount points)

To avoid destroying data outside the visible target:

- If the **target itself** is a reparse point, SecureDelete **refuses** it.
- For a **folder**, SecureDelete scans the tree first and **refuses** if any
  descendant is a reparse point, so SDelete's recursion cannot escape into a
  linked location. If a folder cannot be inspected (access denied), it **fails
  closed** rather than proceeding.

---

## No elevation

SecureDelete uses an **`asInvoker`** manifest and runs strictly as the current
interactive user. Neither SecureDelete nor the SDelete process it launches
requests elevation. If you lack permission to delete the target, the operation
fails with a clear access-denied message — there is **no** automatic UAC
elevation offer. (The installer itself may prompt for elevation only if you
explicitly choose a machine-wide install location.)

---

## Privacy and logging

- No telemetry, no cloud dependency, no network calls (except when you click the
  SDelete download link).
- File contents are never read or logged.
- Full paths are treated as potentially sensitive and are **not** written to any
  persistent log.

---

## Uninstall

Uninstall from **Settings → Apps** (or *Add or remove programs*). Uninstalling
removes the application files and **all** context-menu registry entries
(`uninsdeletekey` on the verb keys). SDelete, being a separate Microsoft tool
you installed yourself, is left untouched.

---

## Build from source

Requirements: **.NET 10 SDK**, Windows x64. (Inno Setup 6 for the installer.)

```powershell
# Restore, build, and test
dotnet test  SecureDelete.sln -c Release

# Publish the app (self-contained x64; includes the .NET runtime + WPF)
dotnet publish src\SecureDelete\SecureDelete.csproj -c Release -r win-x64 `
    --self-contained true -p:PublishSingleFile=false -o artifacts\publish

# Build the installer (adjust the ISCC.exe path as needed)
& "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe" installer\SecureDelete.iss
```

Artifacts:

- Executable: `artifacts\publish\SecureDelete.exe`
- Installer:  `artifacts\installer\SecureDelete-1.0.0-Setup.exe`

### Release packaging notes

The app is published **self-contained** (framework + WPF included) so end users
do not need to install the .NET runtime separately — they only need SDelete.
**Single-file** publishing is intentionally **not** used, to avoid issues with
WPF native resources, the application icon, and process-location logic. The
Inno Setup installer compresses the ~131 MB publish output to roughly 43 MB.

## Replacing the icon

The placeholder icon is `src\SecureDelete\Resources\appicon.ico` (a multi-size
PNG-in-ICO). Replace that file and rebuild to change the executable, window, and
context-menu icon.

## Project layout

```
SecureDelete.sln
src/SecureDelete/         WPF app (see source files below)
tests/SecureDelete.Tests/ xUnit tests
installer/SecureDelete.iss Inno Setup script
installer/SDELETE-NOTE.txt Prerequisite notice shown during install
docs/MANUAL-TEST-CHECKLIST.md
```

Key production classes: `CommandLineOptions`, `DeleteRequest`,
`TargetInspector`, `SDeleteLocator`, `SDeleteArguments`, `SDeleteRunner`
(+ `ISDeleteRunner`), `SDeleteResult`, plus the WPF `App`, `MainWindow`, and
`SDeleteMissingWindow`.

## Trademarks

SDelete and Sysinternals are products of Microsoft. SecureDelete is an
independent wrapper and is not affiliated with or endorsed by Microsoft.
