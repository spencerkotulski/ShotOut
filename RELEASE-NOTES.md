# ShotOut 1.4.3

This release improves tab navigation, screenshot output options, and the editor experience.

## Highlights

- **Faster, steadier tabs:** Reuse rendered previews when switching tabs and keep the gallery visible. Optional recently used tab order lets Ctrl+Tab return to the last tab or cycle through recent tabs while Ctrl is held.
- **More control over JPEG files:** Set separate JPEG quality levels for after-capture saves and editor exports. PNG remains lossless; JPEG uses lossy compression even at 100%.
- **Clearer editor:** A reorganized toolbar groups Capture, File, editing, Save and Export. The annotation toolbox is larger, and Settings uses the ShotOut icon.
- **Update notifications:** ShotOut can check GitHub for stable releases and show a banner linking to release notes and downloads. Automatic checks can be disabled under Settings > Updates.
- **Experimental Print Screen shortcut:** Enable it under Settings > Experimental. Guided setup can help with conflicts, including a user-approved OneDrive restart. Other capture shortcuts remain available.
- **Refinements and fixes:** Improved window resizing, text editing during resize, tab overflow, background tab closing, gallery pagination and dropdown consistency.

## Install

Download `ShotOut-Setup-v1.4.3.exe` below. Exit any running ShotOut instance from its system tray menu before installing. Existing settings and projects are preserved.

The optional portable ZIP contains the app and shortcut guide. `SHA256SUMS.txt` lists checksums for verifying downloads.

Print Screen can still conflict with other software depending on startup order. Top-edge resize behavior was tested with automated checks; desktop-specific smoothness still needs wider confirmation. Light mode is not included.

Validation: Focused checks passed. The full automated run stopped at a focus-sensitive text input test that passed separately; top-edge resize smoothness still needs broader desktop testing.
