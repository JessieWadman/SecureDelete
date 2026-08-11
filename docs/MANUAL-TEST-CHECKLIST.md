# SecureDelete — Manual Windows Test Checklist

Automated tests cover parsing, validation, argument generation, discovery, and
process-result interpretation. The items below must be verified manually on a
real Windows desktop because they involve Explorer integration and the GUI.

> Use **throwaway files/folders** on a **non-SSD** volume where possible. Secure
> deletion is irreversible.

## Setup

- [ ] Install SDelete from Microsoft; place `sdelete64.exe` next to
      `SecureDelete.exe` or on `PATH`.
- [ ] Run `SecureDelete-1.0.0-Setup.exe` (per-user; no elevation prompt).

## Windows 11 context menu

- [ ] Right-click a **file** → **Show more options** → **Secure Delete** is present.
- [ ] Right-click a **folder** → **Show more options** → **Secure Delete** is present.
- [ ] The menu item shows the **application icon**.
- [ ] There is a **separator above** the item.
- [ ] There is a **separator below** the item.
- [ ] Verify the same on the classic menu via **Shift+F10** / **Shift+Right-click**.
- [ ] Right-click a **drive** (C:) → **Secure Delete is NOT present**.

## Windows 10 (if available)

- [ ] Item appears directly on the right-click menu for files and folders.

## Confirmation dialog

- [ ] For a file, heading reads *"Securely delete this file?"*.
- [ ] For a folder, heading reads *"Securely delete this folder and its contents?"*.
- [ ] The **full target path** is shown; a very long path wraps/ellipsizes and the
      **tooltip** shows the complete path.
- [ ] **Overwrite passes** selector offers **1, 3, 7, 10** and defaults to **1**.
- [ ] Clicking **Cancel** closes the dialog and **changes nothing** (file still exists).

## Deletion

- [ ] **1 pass**: delete a file → progress shown → *"Completed successfully."* → file gone.
- [ ] **3 passes**: delete a file → succeeds → file gone.
- [ ] Delete a **folder** with nested files → succeeds → folder gone.
- [ ] Path with **spaces** deletes correctly.
- [ ] Path with **Unicode** characters deletes correctly.

## Error handling

- [ ] **Read-only file**: deletes successfully (SDelete `-r`).
- [ ] **Locked file** (open in another app): fails with a clear "locked/in use" message
      and a non-zero SDelete exit code under **Details**.
- [ ] **Access denied** (a file you cannot delete): fails with a clear access-denied
      message; **no** UAC elevation is offered.
- [ ] **Missing SDelete** (temporarily rename it away): the SDelete-required dialog
      appears with a working **download page** button.
- [ ] **SDelete failure** in general: failure is shown with exit code + stderr in
      **Details**, and **no** stack trace is shown in the normal UI.
- [ ] **File removed after confirmation** (delete it manually between opening the
      dialog and clicking Secure Delete): a clear "no longer exists" message is shown.

## Reparse points

- [ ] Right-click a **junction/symlink** target → **Secure Delete** → it is **refused**
      with an explanation (does not follow the link).
- [ ] A folder that **contains** a junction/symlink → refused with an explanation.

## Network / UNC

- [ ] A file on a **UNC/network share** shows the network-location warning in the dialog.

## Cancellation

- [ ] Start deleting a large folder, click **Abort** → operation stops; result states it
      was **aborted** and that the item may be **partially** deleted (no recovery claim).

## No elevation

- [ ] Confirm (e.g. via Task Manager) that `SecureDelete.exe` and the `sdelete64.exe`
      child run **without** an elevated ("Administrator") token.

## Uninstall / cleanup

- [ ] Uninstall via Settings → Apps.
- [ ] Right-click a file/folder → **Secure Delete is gone**.
- [ ] `HKCU\Software\Classes\*\shell\SecureDelete` and
      `HKCU\Software\Classes\Directory\shell\SecureDelete` are **removed**.
