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

---

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

---

# ShotOut 1.4.2

This release brings together all improvements made since the 1.4.1 release, including the fourteen local preview iterations.

## After-capture workflow
- Added an After capture task checklist: Save, Save as, Open containing folder, Open in the default photo viewer, Copy to clipboard, Open in ShotOut editor, Play sound, Show capture preview, and Copy file path.
- Copy to clipboard is enabled by default. File tasks and sound are off by default; existing editor and sound choices are preserved.
- Save and Save as are alternatives. Cancelling Save as keeps the capture and clipboard image while skipping file tasks. Opening a folder/viewer or copying a file path saves first when needed.
- Added save destination, monthly folders, PNG/JPEG/BMP format, filename tokens and a live naming example. Name collisions receive numbered suffixes.
- Added Custom, Quick copy, Save quietly, and Capture and edit presets. Sound selection, volume and Preview now live under Sound options on the same page.

## Interface and accessibility
- Added UI sizing from 75% to 200% in 5% steps in Settings > Display. Ctrl+plus and Ctrl+minus adjust the same saved setting.
- Scaling includes toolbars, annotation properties, layers, image effects and their adjustment controls. The logo stays aligned; oversized toolbars scroll.
- Added optional names beside toolbox icons. Ctrl+K now searches and activates tools alongside documents, annotations, saved files and recently closed tabs.
- Matched interface accents to the logo blue, refreshed the settings cog, and redrew the Highlighter, Spotlight and Bucket fill icons. The bucket's paint becomes dark when selected.
- Renamed the Balloon tool to Speech balloon and Bucket to Bucket fill in visible labels, keeping existing project and shortcut identifiers compatible.
- Blank top-toolbar areas now support window dragging and double-click maximize/restore. Cleaned up the capture-delay menu; Include cursor is under Settings > Capture.

## Tabs and recovery
- Added horizontal tab scrolling and a draggable overflow scrollbar. Ctrl+Tab and Ctrl+Shift+Tab cycle tabs; Ctrl+T opens a new tab alongside Ctrl+N.
- Added Settings > Tabs controls for optional automatic closing: tab limit, minimum idle age, and separate saved/unsaved eligibility. Automatic closing is off by default and protects the active tab.
- Added recovery for the last 20 recently closed tabs using Ctrl+Shift+T, the tab-strip right-click menu or Ctrl+K. History persists across launches.
- Tabs receive complete editable recovery copies before closing. Reopening restores the original name, save paths and unsaved state. Recovery copies remain in the project folder until manually deleted.

## Annotation workflow
- Added Paste to the canvas right-click menu, placing clipboard images, text or editable annotations at the clicked location.
- Context menus hide unavailable commands and group advanced actions under Layer options.
- Added ten recent applied colors, a clickable save-state indicator with saved-path hints, and optional first-use tool hints.
- Right-click Undo or Redo to see available actions and jump multiple steps. History remains per tab; new edits clear the redo branch.
- Removed redundant Apply Cut/Cancel buttons for cut rows/columns. Cuts still apply on mouse release and can be undone; Crop retains its confirmation controls.
- Increased ratio-based default text and speech-balloon sizes by 25% at the reference image size. Manual sizes and saved styles remain overrides.
- Clone stamp previews the sampled brush pixels during hover and painting, with neutral dashed outlines.
- Emoji insertion uses the native Windows picker. Emoji keep their Windows colors in previews, the canvas, copied/exported images and reopened editable projects, with a compatibility fallback where native color rendering is unavailable.

## Precision, responsiveness and installation
- Fixed screenshot pixels shifting when dragging annotations at fractional zoom levels.
- Fixed speech balloons appearing to resize while moving other annotations: body/tail measurements and text padding now scale consistently between resting and dragging renders.
- Reduced the initial pause when starting annotation drags in layered screenshots by skipping whole-document rebuilds when lower effects are outside the affected region. In the same local 4K benchmark, setup fell from 293 ms to 12 ms. Overlapping effects retain the full rendering path; actual timings vary by document and computer.
- Branded the existing NSIS installer/uninstaller with ShotOut icons and high-resolution artwork. Corrected the upper-right logo proportions and clarified setup text.
- Updated the in-app F1 guide and standalone shortcut documentation.

## Updating and validation
Exit ShotOut from its tray menu before installing. The app and installer remain unsigned. Settings, screenshots and projects are preserved by the managed update workflow.

Automated editor regression checks, focused balloon/drag checks and isolated NSIS install, upgrade, locked-file and uninstall checks are included with the source. Physical mouse/display latency, speaker output, every monitor/DPI combination and antivirus approval are not established by these checks.

---

## Archived development history for 1.4.2

# ShotOut 1.4.1 — preview iteration 14

This is a local preview, not a new GitHub release. Product and file versions remain 1.4.1.

## Iteration 14
- Fixed speech balloons appearing to resize when other annotations move. Tail space, tail width and text padding now scale consistently between resting and dragging renders. Saved dimensions remain unchanged.
- Renamed visible Balloon tool labels to Speech balloon, including toolbox names, tooltips, properties, search, layers and shortcut settings. Existing project and shortcut identifiers remain compatible.
- Ctrl+T opens a new tab alongside Ctrl+N. Ctrl+Shift+T continues to reopen closed tabs.

## Iteration 13
- Corrected the NSIS upper-right header artwork proportions; the app icon and welcome sidebar are unchanged.
- Bucket fill's paint now follows the icon foreground: light normally and dark when selected.
- Added recently closed recovery for the last 20 tabs: Ctrl+Shift+T, right-click the tab strip, or search with Ctrl+K. Manual and automatic closures retain complete editable copies before disposing the tab. History persists across launches; reopened tabs retain their original name, save paths and unsaved state. Recovery files remain on disk until manually removed.
- Added Settings > Display > Show names beside toolbox icons. Labels use a scrolling single-column toolbox and follow the UI size setting. Compact icons remain the default.
- Ctrl+K now searches and activates tools, with a Tools filter, shortcut hints, and related terms such as blur/pixelate. It also finds recently closed tabs.
- Reduced first-drag preparation by avoiding a whole-document rebuild for lower effects outside the affected area. In the same synthetic layered 4K benchmark, preparation fell from 293.28 ms to 11.76 ms; steady compositing measured 0.34 ms/frame. These are local measurements, not physical pointer-to-display latency. Overlapping effects retain the full rendering path and pixel checks pass.

## Iteration 12
- Redrew Highlight as a chisel-tip marker and Spotlight as a flashlight. Bucket fill retains its paint drop and now has a white paint area inside the pail.
- Renamed visible Bucket labels to Bucket fill in the toolbox, properties, status, shortcut settings, guide bindings and relevant layer/search labels. Internal identifiers remain compatible with existing shortcuts and projects.
- Branded the NSIS installer and uninstaller with ShotOut icons, high-resolution sidebar/header artwork and clearer welcome/finish copy. Fixed Modern UI defaults overriding the supplied executable icon. Kept the existing NSIS install, migration and uninstall logic.
- Fixed the fractional-versus-rounded render-origin mismatch between resting and dragging views. Dragging now shares the resting raster phase and retains the cached live-vector path. Tests at 37%, 65%, and 125% zoom found no image displacement; remaining per-channel differences were at most two levels of rounding, with exact screenshot pixels restored after release. Synthetic input + paint remained about 5–7 ms/frame; this is not a physical monitor-latency measurement.
- Included an objective-priority UI / workflow review in UI-NEXT-STEPS.md.

## Iteration 11
- Added native Windows color-font rendering for emoji annotations. Windows supplies the emoji artwork and shaping, including supported skin tones and joined sequences; no emoji images or fonts are downloaded or bundled.
- Full color is shared by the canvas, clipboard/image export, thumbnails and reopened editable projects. Existing emoji annotations use color automatically.
- Added an actual-color preview in the emoji insertion box while preserving native picker and keyboard input.
- Removed the misleading monochrome text-color control for emoji. Font size and normal positioning remain available.
- Cached rendered emoji with a bounded 32 MiB / 32-entry cache to avoid repeated native rendering during redraws. Native graphics resources are released after each render.
- Verified multiple emoji types, exported PNG color, project reopening, and cache reuse. The original monochrome path remains a compatibility fallback if native color rendering is unavailable.

## Iteration 10
- Increased new text and speech-balloon defaults by 25% at the 1000px reference size. Text scales with the longer image edge, with a 27.5px minimum and 160px cap. Manual font-size choices and saved styles remain overrides.
- Added a draggable horizontal scrollbar for overflowing tabs, retaining wheel/Shift+wheel scrolling and arrow controls. Ctrl+Tab / Ctrl+Shift+Tab cycle documents even when editor fields have focus.
- Added Settings > Tabs: scrollbar visibility, opt-in auto-closing, open-tab limit (default 20), minimum idle age (default 15 minutes), and separate saved/unsaved eligibility switches.
- Automatic closing uses least recent activity. Visits, captures and edits refresh activity; the active tab and ongoing interactions are protected. If no eligible tabs exist, the limit is allowed to be exceeded.
- Unsaved tabs close only after a complete editable copy is written under the project folder / Auto-closed tabs. Failed writes retain the tab. Copies remain until manually deleted and retain original screenshot pixels.
- Selecting Emoji and clicking the screenshot opens an insertion box and invokes the Windows emoji picker. Win + . or the Windows emoji button reopens it. Confirm Place emoji to insert at the original point. Existing emoji annotations use the same picker for editing. Unicode skin tones and joined sequences are preserved; the existing export renderer remains monochrome.
- Kept the accepted logo-blue theme.

## Iteration 9
- Replaced the settings ship-wheel glyph with a compact outlined cog, retaining the same icon bounds and button size.
- Matched primary buttons, selected tool/navigation buttons, toggles and sliders to the logo blue (#1A82F8). Updated related selection and hover tints. Bright buttons use dark text/icons for readability.
- Kept the logo, dark neutral backgrounds and annotation/image colors unchanged. Theme colors are centralized for easy adjustment.

## Iteration 8
- Right-click the Undo or Redo toolbar button to see the available history, newest/next action first. Choose an entry to jump that many steps in one operation. Long menus scroll. History remains per tab and follows the configured history limit.
- Added action names to in-memory history; saved project files remain compatible. New edits discard the redo branch as before.
- Removed Apply Cut and Cancel from the cutout tool. Cut rows/columns still apply on mouse release and can be undone. Crop keeps Apply and Cancel.

## Iteration 7
- Simplified annotation context menus: unavailable actions and empty groups are hidden; advanced operations are under Layer options. Canvas right-click selects the clicked annotation, or clears selection on the screenshot background.
- Added ten recent colors to the color picker, newest first, saved across sessions. Only applied choices are remembered; favorites remain separate.
- Added a persistent clickable save indicator: Not saved, Copied · not saved, Unsaved changes, Image saved, or Project saved, with the file path on hover.
- Added nonmodal first-use tool hints with Got it and Turn off tips. Settings > Editor can enable or reset hints.
- Capture last region remains unimplemented pending user selection.

## Iteration 6
- Added Paste (Ctrl+V) to the annotation right-click menu for copied images, text and editable annotations. Canvas-menu paste uses the clicked location; ordinary keyboard paste retains its existing placement. Unsupported or busy clipboards disable the menu item.

## Iteration 5
- Blank top-toolbar spaces, dividers and logo support window dragging and double-click maximize/restore without changing button behavior.
- Clone stamp uses a translucent sampled-brush preview during hover and painting, with neutral, widely spaced dashed outlines and no colored source crosshair.
- Capture dropdown delay uses the menu background and a single compact row. Include cursor is configured in Settings > Capture.

## Iteration 4
- Extended UI sizing to annotation properties, layers, effect cards and dynamically opened adjustment, background and transform controls.
- Aligned the logo with the capture toolbar buttons at every UI size.
- Added a moving clone source footprint and crosshair; amber indicates sampling near an image edge. Source and destination cursors repaint during painting, including outside valid sampling bounds.

## Iteration 3
- Added Save options to After capture: destination and Browse, monthly folders, filename pattern with {date}, {time}, and {milliseconds}, live example, and PNG/JPEG/BMP output. Existing filenames receive numbered suffixes.
- Added Show capture preview, independent of opening the editor.
- Added Copy file path. It saves first if needed and retains the image clipboard format when Copy to clipboard is enabled too.
- Added Custom, Quick copy, Save quietly, and Capture and edit task presets. Each remembers its task checkbox choices; save and sound options are shared.
- Added Display > UI size: 75% through 200% in 5% steps. Default 75% preserves the original toolbar size. Ctrl+plus and Ctrl+minus (including the number pad) adjust toolbar buttons, icons, and labels and persist the selection. Larger toolbars scroll when necessary. Image zoom and annotation sizes remain unchanged.
- Updated the shortcut guide and settings navigation.

## Earlier previews since release 1.4.1
- Added the After capture task checklist: Save, Save as, Open containing folder, Open in default photo viewer, Copy to clipboard, Open in ShotOut editor, and Play sound.
- File tasks are off by default; clipboard copying is on by default. Existing sound and editor preferences are preserved.
- Save and Save as are alternatives. Cancelling Save as keeps the screenshot and image clipboard copy, skipping file tasks.
- Moved screenshot sound customization below the task list.

Exit ShotOut from its tray menu before installing. Executables remain unsigned.


# ShotOut 1.4.1 — preview iteration 11

This is a local preview, not a new GitHub release. Product and file versions remain 1.4.1.

## Iteration 11
- Added native Windows color-font rendering for emoji annotations. Windows supplies the emoji artwork and shaping, including supported skin tones and joined sequences; no emoji images or fonts are downloaded or bundled.
- Full color is shared by the canvas, clipboard/image export, thumbnails and reopened editable projects. Existing emoji annotations use color automatically.
- Added an actual-color preview in the emoji insertion box while preserving native picker and keyboard input.
- Removed the misleading monochrome text-color control for emoji. Font size and normal positioning remain available.
- Cached rendered emoji with a bounded 32 MiB / 32-entry cache to avoid repeated native rendering during redraws. Native graphics resources are released after each render.
- Verified multiple emoji types, exported PNG color, project reopening, and cache reuse. The original monochrome path remains a compatibility fallback if native color rendering is unavailable.

## Iteration 10
- Increased new text and speech-balloon defaults by 25% at the 1000px reference size. Text scales with the longer image edge, with a 27.5px minimum and 160px cap. Manual font-size choices and saved styles remain overrides.
- Added a draggable horizontal scrollbar for overflowing tabs, retaining wheel/Shift+wheel scrolling and arrow controls. Ctrl+Tab / Ctrl+Shift+Tab cycle documents even when editor fields have focus.
- Added Settings > Tabs: scrollbar visibility, opt-in auto-closing, open-tab limit (default 20), minimum idle age (default 15 minutes), and separate saved/unsaved eligibility switches.
- Automatic closing uses least recent activity. Visits, captures and edits refresh activity; the active tab and ongoing interactions are protected. If no eligible tabs exist, the limit is allowed to be exceeded.
- Unsaved tabs close only after a complete editable copy is written under the project folder / Auto-closed tabs. Failed writes retain the tab. Copies remain until manually deleted and retain original screenshot pixels.
- Selecting Emoji and clicking the screenshot opens an insertion box and invokes the Windows emoji picker. Win + . or the Windows emoji button reopens it. Confirm Place emoji to insert at the original point. Existing emoji annotations use the same picker for editing. Unicode skin tones and joined sequences are preserved; the existing export renderer remains monochrome.
- Kept the accepted logo-blue theme.

## Iteration 9
- Replaced the settings ship-wheel glyph with a compact outlined cog, retaining the same icon bounds and button size.
- Matched primary buttons, selected tool/navigation buttons, toggles and sliders to the logo blue (#1A82F8). Updated related selection and hover tints. Bright buttons use dark text/icons for readability.
- Kept the logo, dark neutral backgrounds and annotation/image colors unchanged. Theme colors are centralized for easy adjustment.

## Iteration 8
- Right-click the Undo or Redo toolbar button to see the available history, newest/next action first. Choose an entry to jump that many steps in one operation. Long menus scroll. History remains per tab and follows the configured history limit.
- Added action names to in-memory history; saved project files remain compatible. New edits discard the redo branch as before.
- Removed Apply Cut and Cancel from the cutout tool. Cut rows/columns still apply on mouse release and can be undone. Crop keeps Apply and Cancel.

## Iteration 7
- Simplified annotation context menus: unavailable actions and empty groups are hidden; advanced operations are under Layer options. Canvas right-click selects the clicked annotation, or clears selection on the screenshot background.
- Added ten recent colors to the color picker, newest first, saved across sessions. Only applied choices are remembered; favorites remain separate.
- Added a persistent clickable save indicator: Not saved, Copied · not saved, Unsaved changes, Image saved, or Project saved, with the file path on hover.
- Added nonmodal first-use tool hints with Got it and Turn off tips. Settings > Editor can enable or reset hints.
- Capture last region remains unimplemented pending user selection.

## Iteration 6
- Added Paste (Ctrl+V) to the annotation right-click menu for copied images, text and editable annotations. Canvas-menu paste uses the clicked location; ordinary keyboard paste retains its existing placement. Unsupported or busy clipboards disable the menu item.

## Iteration 5
- Blank top-toolbar spaces, dividers and logo support window dragging and double-click maximize/restore without changing button behavior.
- Clone stamp uses a translucent sampled-brush preview during hover and painting, with neutral, widely spaced dashed outlines and no colored source crosshair.
- Capture dropdown delay uses the menu background and a single compact row. Include cursor is configured in Settings > Capture.

## Iteration 4
- Extended UI sizing to annotation properties, layers, effect cards and dynamically opened adjustment, background and transform controls.
- Aligned the logo with the capture toolbar buttons at every UI size.
- Added a moving clone source footprint and crosshair; amber indicates sampling near an image edge. Source and destination cursors repaint during painting, including outside valid sampling bounds.

## Iteration 3
- Added Save options to After capture: destination and Browse, monthly folders, filename pattern with {date}, {time}, and {milliseconds}, live example, and PNG/JPEG/BMP output. Existing filenames receive numbered suffixes.
- Added Show capture preview, independent of opening the editor.
- Added Copy file path. It saves first if needed and retains the image clipboard format when Copy to clipboard is enabled too.
- Added Custom, Quick copy, Save quietly, and Capture and edit task presets. Each remembers its task checkbox choices; save and sound options are shared.
- Added Display > UI size: 75% through 200% in 5% steps. Default 75% preserves the original toolbar size. Ctrl+plus and Ctrl+minus (including the number pad) adjust toolbar buttons, icons, and labels and persist the selection. Larger toolbars scroll when necessary. Image zoom and annotation sizes remain unchanged.
- Updated the shortcut guide and settings navigation.

## Earlier previews since release 1.4.1
- Added the After capture task checklist: Save, Save as, Open containing folder, Open in default photo viewer, Copy to clipboard, Open in ShotOut editor, and Play sound.
- File tasks are off by default; clipboard copying is on by default. Existing sound and editor preferences are preserved.
- Save and Save as are alternatives. Cancelling Save as keeps the screenshot and image clipboard copy, skipping file tasks.
- Moved screenshot sound customization below the task list.

Exit ShotOut from its tray menu before installing. Executables remain unsigned.


# ShotOut 1.4.1 — preview iteration 10

This is a local preview, not a new GitHub release. Product and file versions remain 1.4.1.

## Iteration 10
- Increased new text and speech-balloon defaults by 25% at the 1000px reference size. Text scales with the longer image edge, with a 27.5px minimum and 160px cap. Manual font-size choices and saved styles remain overrides.
- Added a draggable horizontal scrollbar for overflowing tabs, retaining wheel/Shift+wheel scrolling and arrow controls. Ctrl+Tab / Ctrl+Shift+Tab cycle documents even when editor fields have focus.
- Added Settings > Tabs: scrollbar visibility, opt-in auto-closing, open-tab limit (default 20), minimum idle age (default 15 minutes), and separate saved/unsaved eligibility switches.
- Automatic closing uses least recent activity. Visits, captures and edits refresh activity; the active tab and ongoing interactions are protected. If no eligible tabs exist, the limit is allowed to be exceeded.
- Unsaved tabs close only after a complete editable copy is written under the project folder / Auto-closed tabs. Failed writes retain the tab. Copies remain until manually deleted and retain original screenshot pixels.
- Selecting Emoji and clicking the screenshot opens an insertion box and invokes the Windows emoji picker. Win + . or the Windows emoji button reopens it. Confirm Place emoji to insert at the original point. Existing emoji annotations use the same picker for editing. Unicode skin tones and joined sequences are preserved; the existing export renderer remains monochrome.
- Kept the accepted logo-blue theme.

## Iteration 9
- Replaced the settings ship-wheel glyph with a compact outlined cog, retaining the same icon bounds and button size.
- Matched primary buttons, selected tool/navigation buttons, toggles and sliders to the logo blue (#1A82F8). Updated related selection and hover tints. Bright buttons use dark text/icons for readability.
- Kept the logo, dark neutral backgrounds and annotation/image colors unchanged. Theme colors are centralized for easy adjustment.

## Iteration 8
- Right-click the Undo or Redo toolbar button to see the available history, newest/next action first. Choose an entry to jump that many steps in one operation. Long menus scroll. History remains per tab and follows the configured history limit.
- Added action names to in-memory history; saved project files remain compatible. New edits discard the redo branch as before.
- Removed Apply Cut and Cancel from the cutout tool. Cut rows/columns still apply on mouse release and can be undone. Crop keeps Apply and Cancel.

## Iteration 7
- Simplified annotation context menus: unavailable actions and empty groups are hidden; advanced operations are under Layer options. Canvas right-click selects the clicked annotation, or clears selection on the screenshot background.
- Added ten recent colors to the color picker, newest first, saved across sessions. Only applied choices are remembered; favorites remain separate.
- Added a persistent clickable save indicator: Not saved, Copied · not saved, Unsaved changes, Image saved, or Project saved, with the file path on hover.
- Added nonmodal first-use tool hints with Got it and Turn off tips. Settings > Editor can enable or reset hints.
- Capture last region remains unimplemented pending user selection.

## Iteration 6
- Added Paste (Ctrl+V) to the annotation right-click menu for copied images, text and editable annotations. Canvas-menu paste uses the clicked location; ordinary keyboard paste retains its existing placement. Unsupported or busy clipboards disable the menu item.

## Iteration 5
- Blank top-toolbar spaces, dividers and logo support window dragging and double-click maximize/restore without changing button behavior.
- Clone stamp uses a translucent sampled-brush preview during hover and painting, with neutral, widely spaced dashed outlines and no colored source crosshair.
- Capture dropdown delay uses the menu background and a single compact row. Include cursor is configured in Settings > Capture.

## Iteration 4
- Extended UI sizing to annotation properties, layers, effect cards and dynamically opened adjustment, background and transform controls.
- Aligned the logo with the capture toolbar buttons at every UI size.
- Added a moving clone source footprint and crosshair; amber indicates sampling near an image edge. Source and destination cursors repaint during painting, including outside valid sampling bounds.

## Iteration 3
- Added Save options to After capture: destination and Browse, monthly folders, filename pattern with {date}, {time}, and {milliseconds}, live example, and PNG/JPEG/BMP output. Existing filenames receive numbered suffixes.
- Added Show capture preview, independent of opening the editor.
- Added Copy file path. It saves first if needed and retains the image clipboard format when Copy to clipboard is enabled too.
- Added Custom, Quick copy, Save quietly, and Capture and edit task presets. Each remembers its task checkbox choices; save and sound options are shared.
- Added Display > UI size: 75% through 200% in 5% steps. Default 75% preserves the original toolbar size. Ctrl+plus and Ctrl+minus (including the number pad) adjust toolbar buttons, icons, and labels and persist the selection. Larger toolbars scroll when necessary. Image zoom and annotation sizes remain unchanged.
- Updated the shortcut guide and settings navigation.

## Earlier previews since release 1.4.1
- Added the After capture task checklist: Save, Save as, Open containing folder, Open in default photo viewer, Copy to clipboard, Open in ShotOut editor, and Play sound.
- File tasks are off by default; clipboard copying is on by default. Existing sound and editor preferences are preserved.
- Save and Save as are alternatives. Cancelling Save as keeps the screenshot and image clipboard copy, skipping file tasks.
- Moved screenshot sound customization below the task list.

Exit ShotOut from its tray menu before installing. Executables remain unsigned.


# ShotOut 1.4.1 — preview iteration 9

This is a local preview, not a new GitHub release. Product and file versions remain 1.4.1.

## Iteration 9
- Replaced the settings ship-wheel glyph with a compact outlined cog, retaining the same icon bounds and button size.
- Matched primary buttons, selected tool/navigation buttons, toggles and sliders to the logo blue (#1A82F8). Updated related selection and hover tints. Bright buttons use dark text/icons for readability.
- Kept the logo, dark neutral backgrounds and annotation/image colors unchanged. Theme colors are centralized for easy adjustment.

## Iteration 8
- Right-click the Undo or Redo toolbar button to see the available history, newest/next action first. Choose an entry to jump that many steps in one operation. Long menus scroll. History remains per tab and follows the configured history limit.
- Added action names to in-memory history; saved project files remain compatible. New edits discard the redo branch as before.
- Removed Apply Cut and Cancel from the cutout tool. Cut rows/columns still apply on mouse release and can be undone. Crop keeps Apply and Cancel.

## Iteration 7
- Simplified annotation context menus: unavailable actions and empty groups are hidden; advanced operations are under Layer options. Canvas right-click selects the clicked annotation, or clears selection on the screenshot background.
- Added ten recent colors to the color picker, newest first, saved across sessions. Only applied choices are remembered; favorites remain separate.
- Added a persistent clickable save indicator: Not saved, Copied · not saved, Unsaved changes, Image saved, or Project saved, with the file path on hover.
- Added nonmodal first-use tool hints with Got it and Turn off tips. Settings > Editor can enable or reset hints.
- Capture last region remains unimplemented pending user selection.

## Iteration 6
- Added Paste (Ctrl+V) to the annotation right-click menu for copied images, text and editable annotations. Canvas-menu paste uses the clicked location; ordinary keyboard paste retains its existing placement. Unsupported or busy clipboards disable the menu item.

## Iteration 5
- Blank top-toolbar spaces, dividers and logo support window dragging and double-click maximize/restore without changing button behavior.
- Clone stamp uses a translucent sampled-brush preview during hover and painting, with neutral, widely spaced dashed outlines and no colored source crosshair.
- Capture dropdown delay uses the menu background and a single compact row. Include cursor is configured in Settings > Capture.

## Iteration 4
- Extended UI sizing to annotation properties, layers, effect cards and dynamically opened adjustment, background and transform controls.
- Aligned the logo with the capture toolbar buttons at every UI size.
- Added a moving clone source footprint and crosshair; amber indicates sampling near an image edge. Source and destination cursors repaint during painting, including outside valid sampling bounds.

## Iteration 3
- Added Save options to After capture: destination and Browse, monthly folders, filename pattern with {date}, {time}, and {milliseconds}, live example, and PNG/JPEG/BMP output. Existing filenames receive numbered suffixes.
- Added Show capture preview, independent of opening the editor.
- Added Copy file path. It saves first if needed and retains the image clipboard format when Copy to clipboard is enabled too.
- Added Custom, Quick copy, Save quietly, and Capture and edit task presets. Each remembers its task checkbox choices; save and sound options are shared.
- Added Display > UI size: 75% through 200% in 5% steps. Default 75% preserves the original toolbar size. Ctrl+plus and Ctrl+minus (including the number pad) adjust toolbar buttons, icons, and labels and persist the selection. Larger toolbars scroll when necessary. Image zoom and annotation sizes remain unchanged.
- Updated the shortcut guide and settings navigation.

## Earlier previews since release 1.4.1
- Added the After capture task checklist: Save, Save as, Open containing folder, Open in default photo viewer, Copy to clipboard, Open in ShotOut editor, and Play sound.
- File tasks are off by default; clipboard copying is on by default. Existing sound and editor preferences are preserved.
- Save and Save as are alternatives. Cancelling Save as keeps the screenshot and image clipboard copy, skipping file tasks.
- Moved screenshot sound customization below the task list.

Exit ShotOut from its tray menu before installing. Executables remain unsigned.


# ShotOut 1.4.1 — preview iteration 8

This is a local preview, not a new GitHub release. Product and file versions remain 1.4.1.

## Iteration 8
- Right-click the Undo or Redo toolbar button to see the available history, newest/next action first. Choose an entry to jump that many steps in one operation. Long menus scroll. History remains per tab and follows the configured history limit.
- Added action names to in-memory history; saved project files remain compatible. New edits discard the redo branch as before.
- Removed Apply Cut and Cancel from the cutout tool. Cut rows/columns still apply on mouse release and can be undone. Crop keeps Apply and Cancel.

## Iteration 7
- Simplified annotation context menus: unavailable actions and empty groups are hidden; advanced operations are under Layer options. Canvas right-click selects the clicked annotation, or clears selection on the screenshot background.
- Added ten recent colors to the color picker, newest first, saved across sessions. Only applied choices are remembered; favorites remain separate.
- Added a persistent clickable save indicator: Not saved, Copied · not saved, Unsaved changes, Image saved, or Project saved, with the file path on hover.
- Added nonmodal first-use tool hints with Got it and Turn off tips. Settings > Editor can enable or reset hints.
- Capture last region remains unimplemented pending user selection.

## Iteration 6
- Added Paste (Ctrl+V) to the annotation right-click menu for copied images, text and editable annotations. Canvas-menu paste uses the clicked location; ordinary keyboard paste retains its existing placement. Unsupported or busy clipboards disable the menu item.

## Iteration 5
- Blank top-toolbar spaces, dividers and logo support window dragging and double-click maximize/restore without changing button behavior.
- Clone stamp uses a translucent sampled-brush preview during hover and painting, with neutral, widely spaced dashed outlines and no colored source crosshair.
- Capture dropdown delay uses the menu background and a single compact row. Include cursor is configured in Settings > Capture.

## Iteration 4
- Extended UI sizing to annotation properties, layers, effect cards and dynamically opened adjustment, background and transform controls.
- Aligned the logo with the capture toolbar buttons at every UI size.
- Added a moving clone source footprint and crosshair; amber indicates sampling near an image edge. Source and destination cursors repaint during painting, including outside valid sampling bounds.

## Iteration 3
- Added Save options to After capture: destination and Browse, monthly folders, filename pattern with {date}, {time}, and {milliseconds}, live example, and PNG/JPEG/BMP output. Existing filenames receive numbered suffixes.
- Added Show capture preview, independent of opening the editor.
- Added Copy file path. It saves first if needed and retains the image clipboard format when Copy to clipboard is enabled too.
- Added Custom, Quick copy, Save quietly, and Capture and edit task presets. Each remembers its task checkbox choices; save and sound options are shared.
- Added Display > UI size: 75% through 200% in 5% steps. Default 75% preserves the original toolbar size. Ctrl+plus and Ctrl+minus (including the number pad) adjust toolbar buttons, icons, and labels and persist the selection. Larger toolbars scroll when necessary. Image zoom and annotation sizes remain unchanged.
- Updated the shortcut guide and settings navigation.

## Earlier previews since release 1.4.1
- Added the After capture task checklist: Save, Save as, Open containing folder, Open in default photo viewer, Copy to clipboard, Open in ShotOut editor, and Play sound.
- File tasks are off by default; clipboard copying is on by default. Existing sound and editor preferences are preserved.
- Save and Save as are alternatives. Cancelling Save as keeps the screenshot and image clipboard copy, skipping file tasks.
- Moved screenshot sound customization below the task list.

Exit ShotOut from its tray menu before installing. Executables remain unsigned.


# ShotOut 1.4.1 — preview iteration 7

This is a local preview, not a new GitHub release. Product and file versions remain 1.4.1.

## Iteration 7
- Simplified annotation context menus: unavailable actions and empty groups are hidden; advanced operations are under Layer options. Canvas right-click selects the clicked annotation, or clears selection on the screenshot background.
- Added ten recent colors to the color picker, newest first, saved across sessions. Only applied choices are remembered; favorites remain separate.
- Added a persistent clickable save indicator: Not saved, Copied · not saved, Unsaved changes, Image saved, or Project saved, with the file path on hover.
- Added nonmodal first-use tool hints with Got it and Turn off tips. Settings > Editor can enable or reset hints.
- Capture last region remains unimplemented pending user selection.

## Iteration 6
- Added Paste (Ctrl+V) to the annotation right-click menu for copied images, text and editable annotations. Canvas-menu paste uses the clicked location; ordinary keyboard paste retains its existing placement. Unsupported or busy clipboards disable the menu item.

## Iteration 5
- Blank top-toolbar spaces, dividers and logo support window dragging and double-click maximize/restore without changing button behavior.
- Clone stamp uses a translucent sampled-brush preview during hover and painting, with neutral, widely spaced dashed outlines and no colored source crosshair.
- Capture dropdown delay uses the menu background and a single compact row. Include cursor is configured in Settings > Capture.

## Iteration 4
- Extended UI sizing to annotation properties, layers, effect cards and dynamically opened adjustment, background and transform controls.
- Aligned the logo with the capture toolbar buttons at every UI size.
- Added a moving clone source footprint and crosshair; amber indicates sampling near an image edge. Source and destination cursors repaint during painting, including outside valid sampling bounds.

## Iteration 3
- Added Save options to After capture: destination and Browse, monthly folders, filename pattern with {date}, {time}, and {milliseconds}, live example, and PNG/JPEG/BMP output. Existing filenames receive numbered suffixes.
- Added Show capture preview, independent of opening the editor.
- Added Copy file path. It saves first if needed and retains the image clipboard format when Copy to clipboard is enabled too.
- Added Custom, Quick copy, Save quietly, and Capture and edit task presets. Each remembers its task checkbox choices; save and sound options are shared.
- Added Display > UI size: 75% through 200% in 5% steps. Default 75% preserves the original toolbar size. Ctrl+plus and Ctrl+minus (including the number pad) adjust toolbar buttons, icons, and labels and persist the selection. Larger toolbars scroll when necessary. Image zoom and annotation sizes remain unchanged.
- Updated the shortcut guide and settings navigation.

## Earlier previews since release 1.4.1
- Added the After capture task checklist: Save, Save as, Open containing folder, Open in default photo viewer, Copy to clipboard, Open in ShotOut editor, and Play sound.
- File tasks are off by default; clipboard copying is on by default. Existing sound and editor preferences are preserved.
- Save and Save as are alternatives. Cancelling Save as keeps the screenshot and image clipboard copy, skipping file tasks.
- Moved screenshot sound customization below the task list.

Exit ShotOut from its tray menu before installing. Executables remain unsigned.


# ShotOut 1.4.1 — preview iteration 6

This is a local preview, not a new GitHub release. Product and file versions remain 1.4.1.

## Iteration 6
- Added Paste (Ctrl+V) to the annotation right-click menu for copied images, text and editable annotations. Canvas-menu paste uses the clicked location; ordinary keyboard paste retains its existing placement. Unsupported or busy clipboards disable the menu item.

## Iteration 5
- Blank top-toolbar spaces, dividers and logo support window dragging and double-click maximize/restore without changing button behavior.
- Clone stamp uses a translucent sampled-brush preview during hover and painting, with neutral, widely spaced dashed outlines and no colored source crosshair.
- Capture dropdown delay uses the menu background and a single compact row. Include cursor is configured in Settings > Capture.

## Iteration 4
- Extended UI sizing to annotation properties, layers, effect cards and dynamically opened adjustment, background and transform controls.
- Aligned the logo with the capture toolbar buttons at every UI size.
- Added a moving clone source footprint and crosshair; amber indicates sampling near an image edge. Source and destination cursors repaint during painting, including outside valid sampling bounds.

## Iteration 3
- Added Save options to After capture: destination and Browse, monthly folders, filename pattern with {date}, {time}, and {milliseconds}, live example, and PNG/JPEG/BMP output. Existing filenames receive numbered suffixes.
- Added Show capture preview, independent of opening the editor.
- Added Copy file path. It saves first if needed and retains the image clipboard format when Copy to clipboard is enabled too.
- Added Custom, Quick copy, Save quietly, and Capture and edit task presets. Each remembers its task checkbox choices; save and sound options are shared.
- Added Display > UI size: 75% through 200% in 5% steps. Default 75% preserves the original toolbar size. Ctrl+plus and Ctrl+minus (including the number pad) adjust toolbar buttons, icons, and labels and persist the selection. Larger toolbars scroll when necessary. Image zoom and annotation sizes remain unchanged.
- Updated the shortcut guide and settings navigation.

## Earlier previews since release 1.4.1
- Added the After capture task checklist: Save, Save as, Open containing folder, Open in default photo viewer, Copy to clipboard, Open in ShotOut editor, and Play sound.
- File tasks are off by default; clipboard copying is on by default. Existing sound and editor preferences are preserved.
- Save and Save as are alternatives. Cancelling Save as keeps the screenshot and image clipboard copy, skipping file tasks.
- Moved screenshot sound customization below the task list.

Exit ShotOut from its tray menu before installing. Executables remain unsigned.


# ShotOut 1.4.1 — preview iteration 5

This is a local preview, not a new GitHub release. Product and file versions remain 1.4.1.

## Iteration 5
- Blank top-toolbar spaces, dividers and logo support window dragging and double-click maximize/restore without changing button behavior.
- Clone stamp uses a translucent sampled-brush preview during hover and painting, with neutral, widely spaced dashed outlines and no colored source crosshair.
- Capture dropdown delay uses the menu background and a single compact row. Include cursor is configured in Settings > Capture.

## Iteration 4
- Extended UI sizing to annotation properties, layers, effect cards and dynamically opened adjustment, background and transform controls.
- Aligned the logo with the capture toolbar buttons at every UI size.
- Added a moving clone source footprint and crosshair; amber indicates sampling near an image edge. Source and destination cursors repaint during painting, including outside valid sampling bounds.

## Iteration 3
- Added Save options to After capture: destination and Browse, monthly folders, filename pattern with {date}, {time}, and {milliseconds}, live example, and PNG/JPEG/BMP output. Existing filenames receive numbered suffixes.
- Added Show capture preview, independent of opening the editor.
- Added Copy file path. It saves first if needed and retains the image clipboard format when Copy to clipboard is enabled too.
- Added Custom, Quick copy, Save quietly, and Capture and edit task presets. Each remembers its task checkbox choices; save and sound options are shared.
- Added Display > UI size: 75% through 200% in 5% steps. Default 75% preserves the original toolbar size. Ctrl+plus and Ctrl+minus (including the number pad) adjust toolbar buttons, icons, and labels and persist the selection. Larger toolbars scroll when necessary. Image zoom and annotation sizes remain unchanged.
- Updated the shortcut guide and settings navigation.

## Earlier previews since release 1.4.1
- Added the After capture task checklist: Save, Save as, Open containing folder, Open in default photo viewer, Copy to clipboard, Open in ShotOut editor, and Play sound.
- File tasks are off by default; clipboard copying is on by default. Existing sound and editor preferences are preserved.
- Save and Save as are alternatives. Cancelling Save as keeps the screenshot and image clipboard copy, skipping file tasks.
- Moved screenshot sound customization below the task list.

Exit ShotOut from its tray menu before installing. Executables remain unsigned.


# ShotOut 1.4.1 — preview iteration 4

This is a local preview, not a new GitHub release. Product and file versions remain 1.4.1.

## Iteration 4
- Extended UI sizing to annotation properties, layers, effect cards and dynamically opened adjustment, background and transform controls.
- Aligned the logo with the capture toolbar buttons at every UI size.
- Added a moving clone source footprint and crosshair; amber indicates sampling near an image edge. Source and destination cursors repaint during painting, including outside valid sampling bounds.

## Iteration 3
- Added Save options to After capture: destination and Browse, monthly folders, filename pattern with {date}, {time}, and {milliseconds}, live example, and PNG/JPEG/BMP output. Existing filenames receive numbered suffixes.
- Added Show capture preview, independent of opening the editor.
- Added Copy file path. It saves first if needed and retains the image clipboard format when Copy to clipboard is enabled too.
- Added Custom, Quick copy, Save quietly, and Capture and edit task presets. Each remembers its task checkbox choices; save and sound options are shared.
- Added Display > UI size: 75% through 200% in 5% steps. Default 75% preserves the original toolbar size. Ctrl+plus and Ctrl+minus (including the number pad) adjust toolbar buttons, icons, and labels and persist the selection. Larger toolbars scroll when necessary. Image zoom and annotation sizes remain unchanged.
- Updated the shortcut guide and settings navigation.

## Earlier previews since release 1.4.1
- Added the After capture task checklist: Save, Save as, Open containing folder, Open in default photo viewer, Copy to clipboard, Open in ShotOut editor, and Play sound.
- File tasks are off by default; clipboard copying is on by default. Existing sound and editor preferences are preserved.
- Save and Save as are alternatives. Cancelling Save as keeps the screenshot and image clipboard copy, skipping file tasks.
- Moved screenshot sound customization below the task list.

Exit ShotOut from its tray menu before installing. Executables remain unsigned.


# ShotOut 1.4.1 — preview iteration 3

This is a local preview, not a new GitHub release. Product and file versions remain 1.4.1.

## Iteration 3
- Added Save options to After capture: destination and Browse, monthly folders, filename pattern with {date}, {time}, and {milliseconds}, live example, and PNG/JPEG/BMP output. Existing filenames receive numbered suffixes.
- Added Show capture preview, independent of opening the editor.
- Added Copy file path. It saves first if needed and retains the image clipboard format when Copy to clipboard is enabled too.
- Added Custom, Quick copy, Save quietly, and Capture and edit task presets. Each remembers its task checkbox choices; save and sound options are shared.
- Added Display > UI size: 75% through 200% in 5% steps. Default 75% preserves the original toolbar size. Ctrl+plus and Ctrl+minus (including the number pad) adjust toolbar buttons, icons, and labels and persist the selection. Larger toolbars scroll when necessary. Image zoom and annotation sizes remain unchanged.
- Updated the shortcut guide and settings navigation.

## Earlier previews since release 1.4.1
- Added the After capture task checklist: Save, Save as, Open containing folder, Open in default photo viewer, Copy to clipboard, Open in ShotOut editor, and Play sound.
- File tasks are off by default; clipboard copying is on by default. Existing sound and editor preferences are preserved.
- Save and Save as are alternatives. Cancelling Save as keeps the screenshot and image clipboard copy, skipping file tasks.
- Moved screenshot sound customization below the task list.

Exit ShotOut from its tray menu before installing. Executables remain unsigned.


# Preview iteration 2 (version remains 1.4.1)

- Moved Play sound into the After capture task checklist. Sound options below the checklist contain the clip selector, volume, and Preview. Existing preferences remain unchanged.

# ShotOut 1.4.1 — preview iteration 1

Not a new GitHub release. Application and installer versions remain 1.4.1.

- Added a dedicated After capture settings page with a checkbox task list.
- Added automatic Save using the configured screenshot folder and monthly organization, with unique PNG filenames.
- Added optional Save as for custom names and locations; selecting Save or Save as turns off the other.
- Added Open containing folder and Open in default photo viewer. Both use the saved screenshot and save first if necessary.
- Moved Copy to clipboard (on by default) and Open in ShotOut editor into the checklist.
- File tasks are off by default; existing clipboard/editor preferences are preserved.
- Cancelling Save as retains the capture and clipboard copy, and skips file tasks. File or shell errors preserve the screenshot and do not block unrelated tasks.
- Updated the in-app shortcut guide.

Exit ShotOut using its tray menu before installing. Installer and application remain unsigned.

# ShotOut 1.4.1

- Replaced the custom C# installer with the free NSIS installer. Per-user installation, Start menu/desktop shortcuts, Windows installed-app registration and uninstall are handled by NSIS.
- Existing managed installations migrate in place. Screenshots, projects, settings and existing startup preferences are preserved. The installer requires ShotOut to be exited before updating and refuses unmanaged nonempty target folders.
- Added optional screenshot audio in Settings > Capture, off by default. Choose Film camera Nikon F3, Camera shutter, or Camera 7; use Preview and adjust volume.
- All three supplied recordings are embedded unchanged. Sound plays after a successful full-screen, window or region capture, independently of automatic clipboard copying. Cancelled captures remain silent.
- Audio playback runs on a lazy background thread using Windows multimedia components. Capture does not wait for sound, and playback failures do not discard the screenshot.
- Updated the shortcut guide and sound provenance document. Windows MP3 decoding, muted native playback, capture-trigger behavior, settings persistence, and isolated NSIS install/upgrade/uninstall checks were validated.

Exit ShotOut through its tray menu before installing. This build is unsigned; switching to NSIS does not guarantee an antivirus product will approve it. Audible speaker output was not assessed in automated tests.

# ShotOut 1.4.0

## Changes since the previous GitHub release preparation

This changelog covers changes since v1.3.4. Features refined across several development builds are described using their final behavior. The public release packaged here is version 1.4.0.

## Shortcuts and learning

- Added a complete beginner-oriented guide organized by screen and task: capture, smart regions, the new-tab library, tabs and files, canvas navigation, annotation tools, size controls, text, layers, image effects, clone stamp, color picking, search, and Settings.
- Replaced the short F1 message box with a searchable, scrollable guide inside the app. Jump between sections, find text with Ctrl+F, repeat searches with F3 or Shift+F3, and close with Escape.
- The in-app guide includes the user's current capture and tool bindings as well as factory defaults.
- Added an offline HTML guide with section navigation and print support, plus the matching Markdown document for GitHub.
- Documented context-sensitive behavior, including field scrolling versus canvas zoom, Escape during typing versus capture, and selected-annotation copying versus full-image copying.

## Capture and smart region selection

- Added optional experimental detection of controls and message regions inside windows. This setting replaces normal region selection when enabled and is off by default.
- Hover to highlight a detected region, click to capture it, or drag a custom rectangle. Scroll up to expand the scope and down to shrink it. Shift temporarily selects the window; Alt bypasses snapping.
- Kept selection stable during pointer movement within its bounds. Automatic detection may refine inward, but does not expand or shift sideways while the pointer remains inside. A scope selected with the wheel stays selected until the pointer exits it.
- Both normal window snapping and smart region capture filter hidden, minimized, cloaked, click-through, fully transparent, and known layered helper windows. Shaped window regions are respected.
- Detection runs on a background worker after a brief hover. Traversal is bounded, a slow lookup triggers window-selection fallback, and at most one provider lookup is outstanding across the process.
- Fixed Escape cancellation in regular region, smart region, and window capture, including during a drag or when another app takes keyboard focus. Cancellation releases mouse capture, stops capture timers, and creates no screenshot.
- Preserved right-click cancellation, free dragging, and the existing global capture bindings.

## Text and speech balloons

- Added direct on-canvas text editing with a transparent typing background. Double-click existing text to edit it again.
- Enter, Ctrl+Enter, or Escape keeps text and finishes editing; Shift+Enter adds a new line. Delete edits characters while typing and removes a selected object outside typing mode.
- Added a compact font-family picker, sizes in points, and balanced bold, italic, and underline controls. Ctrl+B, Ctrl+I, and Ctrl+U apply formatting while typing.
- Added bulleted and numbered lists. Shift+Enter continues a list; using it on an empty item ends the list.
- Improved multiline bounds, caret visibility, color consistency, and text fitting. New lines retain their intended color in the live preview.
- Speech balloons can be drawn to size before typing, grow to fit wrapped text, and remain present when left empty.
- Color-only edits preserve mixed text styling and geometry. Fonts and mixed formatting remain part of editable project data.
- Reduced typing flicker through batched layout and formatting updates.

## Annotation styles colors and layout

- Added saved annotation styles with apply, rename, and delete actions. Styles preserve supported colors, opacity, stroke width, fonts, formatting, alignment, arrow style, corner radius, shadows, balloon tails, and magnifier settings.
- Added alignment and equal-spacing controls for selected objects and linked groups. Linked image and markup groups stay together during arrangement.
- Added a desktop eyedropper with pixel magnification and a hex preview, plus saved favorite colors.
- Fixed style and favorite-color context-menu lifetime errors, right-click color removal, and off-center swatches. The eyedropper now uses a compact pipette button.
- Moved opacity controls into the relevant stroke, text, and fill color pickers. Added percentage fields and transparency previews while retaining direct shadow and tail controls.
- Added exact X, Y, width, and height controls with optional proportional resizing. Numeric changes support Undo.
- Added 1-pixel arrow-key nudging and 10-pixel Shift+arrow nudging. Text and numeric fields retain their editing behavior.
- Enlarged resize handles and their hit areas. Default outline thickness scales with image dimensions; the scaling ratio is now 18 percent larger. Manual choices and existing annotation values are preserved.
- Added a starting-number control for numbered circles. Circle size updates selected circles and establishes the size for future ones; dragging a circle's handles also updates the future default.
- Added independently collapsible Text options and Position / size sections that remember their state.

## Arrows selection copying and layers

- Added visual arrow-style choices and bend controls for every arrow. The amber center handle bends, the blue handle rotates, and dragging the shaft moves the arrow. Rotation cursors have a contrasting white outline.
- Starting screenshots and opened images are movement-locked by default. Added and duplicated images can be moved independently; lock controls are available in Layers and image context menus.
- Drawing tools draw over unlocked images; use Select or explicitly select an image layer to move it.
- Ctrl+C copies selected annotations without including an unrelated screenshot. Pasted annotations remain editable; linked groups receive independent identities.
- Clicking blank canvas clears stale selection, allowing Ctrl+C to copy the whole composition. Ctrl+Shift+C and Copy Image provide explicit full-image copying outside text editing.
- Copy confirmation displays “Copied image” for about five seconds.
- Reduced layer flicker by preserving rows, selection, and scroll position while updating the affected content.
- Escape twice returns to Select and leaves active effects or sizing controls while preserving committed text.

## Image adjustments backgrounds and effects

- Added reversible image-specific exposure, contrast, brightness, saturation, temperature, overall strength, enable/disable, and Reset controls. Annotation colors remain separate from image adjustments.
- Added Before and After comparison controls. Comparison preview does not change saved or copied output.
- Added soft feathered local adjustment zones, with move, resize, rotation, independent settings, and deletion. Zones may extend off the image without expanding the canvas.
- Grouped effects into focused cards with back navigation. Opening a card hides the other cards and exposes its controls in a wider pane.
- Grouped rotation, flipping, resize, canvas expansion, and arrangement actions under Transform. Added visual rotate/flip buttons and previews for side-by-side or appended layouts.
- Added draggable image sizing and canvas expansion with linked proportions and exact pixel entry.
- Added Grayscale, Film, and Vintage filters.
- Nested general adjustments, filters, and zones beneath their source image in Layers. Effects can be edited, hidden, removed, or reordered within the image; ordering affects rendering and supports Undo.
- Replaced basic presentation controls with transparent-by-default Background controls for margin, padding, smart padding, rounded corners, shadows, solid/gradient colors, opacity, and aspect ratios.
- Corrected background colors and gradients to fill the intended margin instead of the wrong padding region.
- Cuts apply on release; Cancel or Escape reverses the latest pending cut, while switching tools keeps it. Editable crop behavior and Undo remain available.

## Library projects and saving

- Added Images and Projects library views, saved recent projects, name/date sorting, and visual blank-canvas presets with a Custom size option.
- Improved multi-selection and opening images as separate tabs or as independent layers in one tab. Single-click selects; double-click activates.
- Added project save-folder selection and optional monthly screenshot folders. Standard Save reuses the document's image or project destination.
- Added Ctrl+K search for saved files, open documents, and annotation text or layer names. Saved-file search includes supported nested folders and remembered external assets, with bounded scanning and background filtering.
- Added library Edit image, Open file, and Open file location commands. E edits a selected library item without interfering with the editor's Ellipse shortcut.
- Simplified library navigation with parent-folder and refresh icons. Previous/Next paging appears only when needed.
- Fixed the Edit image button retaining an enabled state after the library selection is cleared or refreshed.
- New tabs show the library without annotation sidebars or editing-only controls. Opening an image or canvas restores the editor workspace and pointer behavior.

## Interface and settings

- Reorganized Settings into six pages: Capture, Editor, Files & folders, Startup & recovery, Capture shortcuts, and Tool shortcuts. Pages scroll independently; Save and Cancel remain available.
- Preserved existing settings and shortcut customization. Added context explanations for smart selection and file types.
- Replaced the app mark with a rounded blue tile, subtle blue gradient, and white capture corners. Reduced and centered the toolbar logo and aligned the toolbar controls.
- Updated app, tray, executable, and installer icons. Installation refreshes app shortcuts and matching taskbar pins using a versioned icon resource.
- Improved dark context-menu spacing, text alignment, hover states, icons, separators, and shortcut labels.
- Added a subtle dark transparency checkerboard that preserves exported transparency, and native rounded window corners where Windows supports them.
- Added the option to disable unsaved-change prompts; confirmation remains enabled by default.

## Rendering and performance

- Improved drawing, object dragging, resizing, crop interaction, canvas panning, zooming, typing, and rapid size adjustments through cached backgrounds, visible-region rendering, and incremental redraws.
- Kept live annotations separate from cached image content where possible, reducing repeated full-image flattening during interaction.
- Reduced repeated image decoding, pen-bound calculations, inspector layout work, and layer rebuilding. Preview caches remain bounded.
- Improved text and vector sharpness after editing and navigation. Full-quality rendering returns after interaction settles; output dimensions remain unchanged.
- Grouped rapid adjustments into practical Undo steps while preserving individual editable objects and effects.
- Fixed stale pixel fragments, lost mouse-release interactions, invalid canvas dimensions, and zero-area canvas-edge copies.
- Performance depends on image size, effects, layer count, and hardware. This release does not claim zero latency or benchmark parity with another application.

## Compatibility and limitations

- Windows application with an installer and portable executable; no updater or cloud service was added.
- Existing user settings and editable project data are retained by the update workflow. Exit ShotOut from its tray menu before installation.
- Smart detection uses another app's Windows accessibility information; it cannot guarantee recognition of every visual rectangle or Slack message. Rounded controls use rectangular capture bounds.
- Live verification of Wispr Flow and every mixed-DPI monitor configuration remains outstanding. Windows may require repinning an old cached taskbar shortcut after an update.
- The executable is unsigned. The release bundle includes source, documentation, validation results, and SHA-256 checksums.


