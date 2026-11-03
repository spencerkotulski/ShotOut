# Archived previews incorporated in release 1.4.2

The entries below document development after 1.4.1. All fourteen iterations are included in 1.4.2; they were not separate public releases. See RELEASE-NOTES.md for the consolidated release notes.

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
