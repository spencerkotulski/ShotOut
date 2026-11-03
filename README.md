# ShotOut 1.4.3

ShotOut is a Windows screenshot and annotation app. Capture a region, window or desktop, add editable markup and image effects, then copy or save the result.

## Print Screen setup
In Settings > Experimental, turn on Use Print Screen for ShotOut region capture. If the key is free, it works immediately. This switch saves immediately; Cancel does not undo it. Other settings still use Save settings.

If the key is blocked, guided setup offers the relevant next step. When Windows' own capture option is on, open its keyboard settings, turn that option off and choose Try again. When OneDrive may hold the key, Restart OneDrive and enable explains and performs a brief sync interruption, claims the key for ShotOut, then reopens OneDrive. This runs only when you explicitly click that action. Other apps are not closed automatically.

Keep ShotOut running, including in the tray. If a later Windows login or exiting ShotOut lets another app reclaim the key, choose Set up Print Screen again. ShotOut does not silently restart OneDrive at login or change OneDrive/Windows settings. Check Print Screen remains available for detailed diagnostics.

## Install or run
- Recommended: run `ShotOut-Setup-v1.4.3.exe`. The NSIS installer works per Windows user and includes Start menu and optional desktop shortcuts.
- Portable: extract `ShotOut-Windows-v1.4.3.zip` and run `ShotOut.exe`.
- Exit the existing app from its tray menu before updating. Closing the editor window can leave ShotOut running in the tray.
- Requires Windows 10/11 with the Windows .NET Framework. Native emoji artwork follows your installed Windows fonts.

## Start here
1. Press Ctrl+Shift+2 to capture a region, Ctrl+Shift+3 for the desktop, or Ctrl+Shift+4 for a window. Escape cancels capture.
2. Press A for Arrow, R for Rectangle, T for Text or O for Speech balloon. Press V to select annotations. Tool shortcuts can be changed in Settings.
3. Scroll over Stroke or Font size to adjust it quickly; scroll over the canvas to zoom. Ctrl+plus/minus changes interface size instead.
4. Right-click the canvas for available commands, including Paste. Ctrl+C copies selected annotations when selected; Ctrl+Shift+C copies the finished image outside text editing.
5. Ctrl+S saves. Ctrl+Shift+S offers Save As choices. Editable `.shotout` projects preserve layers for later editing.
6. Press F1 for the searchable shortcut guide, or Ctrl+K to find tools, tabs, saved files and annotations.

## Capture tasks and settings
Settings > After capture contains checkboxes for Save, Save as, opening a containing folder/photo viewer/editor, copying the image/path, showing a capture preview and playing sound. Image copying is on by default; file tasks and sound are off by default. Existing editor and sound choices are preserved.

Save options include the destination, monthly subfolders, PNG/JPEG/BMP format and filename patterns using `{date}`, `{time}` and `{milliseconds}`. Save as prompts for a custom name and location. Cancelling that prompt keeps the capture and clipboard image. Opening a folder/viewer or copying its path saves first when needed. Task presets remember the checkbox choices; destination, format and sound settings are shared.

Sound options on the same page offer three clips, volume and Preview. Sound plays only after successful capture. See SOUND-CREDITS.md for provenance.

Settings > Capture contains smart element selection and Include cursor. Smart selection is optional and depends on the regions other apps expose to Windows. Scroll changes scope; Shift selects the window and Alt bypasses snapping. Dragging still defines a custom rectangle.

Settings > Display offers 75%–200% UI sizing in 5% steps and optional tool labels. Large toolbars scroll. Image zoom and annotation sizes are separate.

Settings > Tabs controls overflow scrolling and optional idle-tab closing. Choose the maximum count, idle age and saved/unsaved eligibility. The active tab is protected. Recovery copies are saved before tabs close; the tab limit is soft when no eligible tabs exist.

## Tabs and recovery
Ctrl+N or Ctrl+T opens a new tab. Ctrl+W closes one. Ctrl+Tab / Ctrl+Shift+Tab cycles tabs. Scroll over the strip, drag its scrollbar or use its arrows when tabs overflow.

Ctrl+Shift+T reopens the latest closed tab. Right-click the tab strip to choose from the last 20 closures, or find them through Ctrl+K. History persists across launches; reopened documents retain editable marks, original save paths and unsaved state. Recovery files remain under the project folder's Recently closed tabs or Auto-closed tabs directories until manually deleted.

## Editing tips
- Drag blank top-toolbar spaces to move the window; double-click to maximize or restore.
- Right-click Undo or Redo to jump to an available history entry. Ctrl+Z / Ctrl+Y remain single-step actions.
- Cut rows/columns apply on mouse release; Ctrl+Z undoes the cut. Crop retains Apply and Cancel.
- The save indicator distinguishes unsaved, copied and saved documents. Hover for the saved path or click to save.
- The color picker keeps recent applied colors separately from favorites. First-use hints can be enabled/reset under Settings > Editor.
- Clone stamp: Alt+click selects a source. The brush preview shows sampled pixels during hover and painting.
- Emoji: select the Emoji tool and click the canvas, choose with the native Windows picker, then confirm Place emoji. Win+. reopens the picker. Native colors are retained in display and export when Windows color rendering is available.

## Documentation
- Shortcuts.html: standalone offline guide, printable from a browser.
- Shortcuts.md: Markdown shortcut guide; F1 shows the embedded guide with current custom bindings.
- RELEASE-NOTES.md: changes since release 1.4.1.
- CHANGELOG.md: cumulative history, including archived development iterations.
- GITHUB-RELEASE.md: release upload directions.
- PREVIEW-CHANGES.md: preview changes and current Print Screen validation limits.
- VALIDATION.md: release validation and remaining limitations.

## Build from source
Run `powershell -ExecutionPolicy Bypass -File .\build.ps1` on Windows. The script uses the Windows .NET Framework C# compiler and embeds the icon, guide and supplied sound files. Keep all source files and the Sounds folder together.

For the installer, install the free NSIS compiler or pass its path: `powershell -ExecutionPolicy Bypass -File .\build-installer.ps1 -NsisCompiler C:\path\to\makensis.exe`. This regenerates InstallerAssets with MakeInstallerArt.ps1 and compiles ShotOut.nsi. No installer-backend change was made in this release.

Executables are unsigned. There is no built-in updater; install newer releases manually. Branding and NSIS do not guarantee antivirus approval. Managed updates retain settings and user work.


