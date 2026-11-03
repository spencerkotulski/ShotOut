# ShotOut 1.4.2 — preview iteration 3

This is a local preview, not a new GitHub release. Application and installer versions remain 1.4.2.0.

## Iteration 3
- Simplified Print Screen setup for beginners. The Settings > Capture switch now applies immediately, separately from other settings, and displays Ready, Off or Needs setup feedback.
- If the key is available, enabling the switch immediately registers it and saves that preference without requiring the overall Save settings button.
- If Windows' own Print Screen setting is on, guided setup offers its Windows settings button and Try again. ShotOut does not change that Windows setting automatically.
- If Print Screen is reserved and a restartable Microsoft OneDrive process is running in the same session, setup offers Restart OneDrive and enable. The wizard explains the brief sync interruption before that explicit action.
- The approved handoff requests OneDrive's graceful /shutdown, waits for it to exit, lets ShotOut claim and save Print Screen, then restarts OneDrive in the background. It attempts the restart even if stopping, claiming or saving fails, and checks that OneDrive appears again. It never force-kills OneDrive or changes its files/settings.
- Other conflicts receive guidance and Try again. ShotOut does not close unknown apps or automatically restart OneDrive at startup. No keyboard hook was added.
- The Set up Print Screen button provides an easy way to repeat the handoff if startup order or exiting ShotOut lets OneDrive reclaim the shortcut.
- Focused tests simulate handoff order, failed claims, stop/save exceptions and restart cleanup. UI tests verify that sync-interruption guidance is visible before the restart button; live OneDrive is not stopped by the tests.

## Iteration 2
- Corrected Print Screen failure guidance: error 1409 reports an existing shortcut reservation, access-denied errors are identified separately, and other native errors retain their code and description.
- Reads the Windows Print Screen setting without changing it. When the setting is already off, feedback says so instead of telling the user to disable it again. Unknown values are reported as unknown.
- Added Settings > Capture > Check Print Screen. It recognizes the current ShotOut instance's own registration and uses a temporary, immediately released registration to check availability otherwise. Other capture shortcuts stay registered during this check.
- Updated startup conflict notices to point to the diagnostic check instead of assuming Windows' toggle is on.
- Confirmed on the desktop: the Windows toggle was off and the native registration attempt returned 1409 with and without no-repeat. A running ShareX instance was observed, but Windows does not identify the shortcut owner. No other apps were closed and no Windows or ShareX settings were changed.
- Focused checks cover disabled/enabled/unknown setting states, conflict versus access-denied errors, self-owned registration detection, temporary registration release and preservation of normal shortcuts. The diagnostics do not forcibly take an occupied key.

## Changes since release 1.4.2
- Added Settings > Capture > Use Print Screen for ShotOut region capture. It is optional and off by default, and works while ShotOut is running, including in the tray.
- Print Screen is an additional global shortcut. Existing configured region, desktop and window shortcuts are preserved; disabling the option releases Print Screen.
- Added Open Windows keyboard settings. Users must turn off Windows' Print Screen screen-snipping option themselves; ShotOut does not change that Windows setting or remove Snipping Tool.
- Added clear conflict messages. If the optional key cannot register, the existing capture shortcuts stay active. Saving a conflicting setting is rejected and previous bindings are restored; startup also displays a status message and a tray notice for a Print Screen conflict.
- Uses the existing RegisterHotKey system with no-repeat behavior. No keyboard hook, keyboard polling or Win+Shift+S interception was added.
- Updated the shortcut guide, including the current enabled/disabled preference shown by F1.

## Validation
Focused checks cover opt-in defaults, migration, persistence, cloning, hotkey registration arguments, preservation of other bindings, conflict fallback, disabling, routing and the Settings checkbox. Registration/conflicts are simulated in focused tests so the tests do not take over the user's Print Screen key. Actual global Print Screen behavior still needs a manual check after disabling Windows' screen-snipping binding on the user's computer.

Exit ShotOut from its tray menu before installing this preview. Public release version remains 1.4.2.

## 1.4.2-it4 — Updates and Experimental settings
- Moved Print Screen setup to Experimental, preserving behavior and preferences.
- Added default-on background GitHub stable-release checks, dismissible top banner and official release link.
- Added Settings > Updates with automatic-check toggle and Check now.
- Existing NSIS installer remains the installation method; updates require a user download/install.
- Verified version filtering, URL validation, settings placement, banner creation, Print Screen regression and simulated handoff. Live API returned v1.4.2.
- No public version bump or remote publication. See UPDATES.md for maintainer steps.

## 1.4.2-it5 — Dropdown and tab polish
- Arrow style uses the same dropdown triangle as Annotation styles.
- Background tabs that do not need a save prompt close without activating or rendering their workspace. Unsaved confirmation and closed-tab recovery remain available.
- Tab strip reserves scrollbar space while enabled, avoiding canvas movement at the overflow threshold; disabling the scrollbar retains the compact strip.
- Document reads group layout updates; editor background painting is double-buffered.
- Focused checks passed for document state/undo/zoom, inactive-close preview preservation, overflow height stability, workspace visibility, closed-tab recovery, and existing arrow/release-polish behavior. Visual arrow dropdown inspected. No claim of measured speed improvement.
- Version remains 1.4.2; no GitHub release or remote upload.

## 1.4.2-it6 — Fast tab switching and stable gallery
- Reuse recently visited tabs' full-quality rendered previews instead of rendering annotations on every switch. Retained previews use a 128 MiB budget (the active image may exceed it); evicted tabs render normally when reopened.
- Edits replace their rendered cache; closing tabs and idle-tab cleanup release cached previews.
- New welcome tabs skip blank-image rendering. Switching tabs no longer clones the entire screenshot to warm undo snapshots.
- Tab visits preserve the existing file gallery and thumbnails instead of clearing/reloading them. A changed library home still reloads; F5, folder navigation and saving retain their refresh behavior.
- Gallery refresh keeps the current listing visible until results arrive, and unchanged listings retain their thumbnail rows.
- Focused local 4K test: new tab about 5 ms, cached switch about 3 ms. Actual results depend on images and machine; evicted images and save/recovery work may take longer.
- Public version remains 1.4.2; no release or remote publication.

- Full regression suite passed. Local timings across focused/full checks varied: new tab about 5–26 ms; cached 4K switch about 3–21 ms. Final gallery-refresh adjustment preserves selection controls and pagination while results load; focused recheck recorded separately. Interactive validation remains outstanding.

## 1.4.2-it7 — Larger annotation toolbox
- Enlarged only left annotation toolbox buttons and glyphs by 15%: base buttons 40x32 to 46x37; icons 20 to 23 pixels, before the existing UI scale.
- Rail and separator widths accommodate the larger tools in single-column, compact two-column and named-tool layouts; overflow stays scrollable.
- Top toolbar, inspector, global UI percentage and annotation sizes keep their prior sizing.
- Existing tool labels/scaling/recovery, workspace and interaction checks passed; sidebar layout visually inspected. No public version change.

## 1.4.2-it8 — Text editing during window resize
- Fixed reuse/disposal of equivalent live text fonts. WinForms can retain the existing Font when an equal replacement is assigned; disposing that retained object caused Font.Height to throw Parameter is not valid during resize.
- Beginning a native window move/resize finishes live text editing, keeps typed text and deselects the annotation before continuing. Ordinary viewport repositioning also keeps a valid font.
- Focused repeated-font-refresh/window-size/zoom checks passed, including preserved text, undo/redo and empty-text cleanup. Existing inline text and text workflow checks passed.
- Top-bar recommendations only; no top-bar layout changes. Public version remains 1.4.2.

## 1.4.2-it9 — Clearer top toolbar
- Grouped top bar into Capture, File, Undo/Redo with Effects, and labeled Save/Export. Settings remains at the far right.
- File menu includes New tab, Open, Paste, existing recent images/projects, Save, image Save as and editable project Save as.
- Export menu offers image export (PNG/JPEG/BMP chooser) and Copy image; existing keyboard shortcuts and Undo/Redo history remain.
- Recent files omit missing paths, deduplicate entries, show full-path tooltips and preserve existing history storage. New toolbar menus use existing popup lifetime handling and scale with UI size.
- Menu structure and text-resize regression checks passed; toolbar screenshot visually reviewed. Updated shortcut guide. No release/version change.

## 1.4.2-it10 — Dropdown consistency and Settings branding
- Removed the inner divider between Capture and its dropdown area. Capture, File and Export now use the same small triangle glyph and font.
- Capture keeps separate main-action/menu click targets and Alt+Down/F4 support; dropdown hit area scales with UI size.
- Settings title bar now uses the embedded ShotOut icon rather than the default Windows form icon, with owned icon cleanup.
- Build and existing toolbar/settings checks passed; both toolbar and Settings screenshots visually inspected. Public version remains 1.4.2.

## 1.4.2-it11 — Toolbar spacing cohesion
- Capture, File and Export now share dropdown rendering, identical icon/text/arrow padding and content-fitted widths. Capture no longer reserves excessive empty space.
- Added matching toolbar divider between Save and Export.
- Existing menu structure check passed; toolbar visually inspected. No other UI layout or public version change.

## 1.4.2-it12 — Recently used tab cycling
- Added optional Settings > Tabs > Ctrl+Tab cycles tabs in recently used order (off by default).
- One Ctrl+Tab switches to the last tab. Releasing Ctrl commits the visit so the next tap returns. Holding Ctrl freezes the order for repeated Tab presses; Ctrl+Shift+Tab reverses it.
- Mouse visits and new tabs update recency; closed tabs are excluded; leaving the app ends cycling. Auto-close maintenance waits until cycling ends. Existing text-edit Ctrl+Tab routing is preserved.
- Kept existing idle/activity timestamps separate from tab-visit order, and retained cached preview switching.
- Focused tests passed for toggling, traversal, reverse order, new/closed tabs, release/deactivation, off/default behavior, preference persistence and preview reuse. Updated shortcut guide. No version bump or release upload.

## 1.4.2-it13 — JPEG output quality
- Added independent 1–100% JPEG controls, both defaulting to 100: After capture > Save options for capture task files; Files & folders > Output quality for editor saves/exports.
- Added matching sliders and numeric fields. Values persist in settings and old settings inherit 100%.
- Explicit JPEG encoder quality is applied only when saving JPEGs. PNG remains lossless, BMP/projects/clipboard and capture resolution remain unchanged. JPEG at 100% is still lossy.
- Capture acquisition has no new quality-processing step; saving already required encoding and its duration can vary with JPEG quality. No guarantee of identical save times.
- Focused quality tests passed: independent routing/persistence, JPEG size difference, image dimensions and exact PNG pixels; existing Settings checks passed. Shortcut guide updated. No public release/version change.

## 1.4.2-it14 — Window resize drawing
- Prevent Windows from copying stale client pixels during size changes; pure window moves retain their existing behavior.
- Reuse unchanged toolbox layouts instead of resetting scrolling and repositioning every control on each resize step.
- Batch native gallery item positioning, buffer its drawing, and skip gallery reflow for height-only changes.
- Buffer main toolbar/workspace container painting and avoid redundant maximize-button/border updates.
- During a live edge resize, draw the existing image using a lighter display path; repaint at full display quality when resizing ends. Saved pixels remain unchanged. No delayed animation or synchronous whole-window redraw loop.
- Focused checks cover resize flags, unchanged toolbox/gallery layout, preview reuse, maximize/restore bounds, live text and tab switching. Desktop motion/flicker perception requires user verification.
- Windows API reference: https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-windowpos (SWP_NOCOPYBITS).
- Public version remains 1.4.2; no GitHub publication.

## 1.4.2-it15 — Troubleshooting and gallery pagination
- Fixed missing or stale next-page control when the first 200 gallery entries remain unchanged but a 201st item is added or removed.
- Focused pagination, recently used tab order, JPEG quality and Print Screen diagnostic checks passed. Prior it14 full run stopped at a screen-dependent text assertion that passed separately; interactive resize flicker still needs desktop validation.
- Current version remains 1.4.2; no release.

## 1.4.2-it16 — Resize edge flashing
- During an interactive window resize, explicitly paint exposed window background with the existing dark window color instead of allowing a white background flash. Normal painting and prior resize optimizations remain.
- Focused dark background pixel, resize/maximize, cached preview and live-text resize checks passed. Actual visible edge flashing needs user desktop verification. No public release or version change.

## 1.4.2-it17 — Top-edge resize stability
- During live resizing, preserve existing painted pixels when the top edge moves; keep the prior no-copy behavior when resizing from the bottom. Dark exposed background and live preview remain in place.
- Focused top/bottom resize flag, background, maximize/restore, cached preview and live-text checks passed. Actual perceived smoothness and any stale-pixel flicker need user desktop validation. No public version change.
