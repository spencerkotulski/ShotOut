# ShotOut shortcuts and mouse controls

Version 1.4.3 for Windows

Use this guide to learn the controls for the screen you are using. Start with the quick workflow, then browse the sections as you need them. The keyboard bindings below are factory defaults. Your saved capture and tool bindings may differ; Settings shows your current bindings. Mouse controls and context-sensitive commands are described separately so the same key is not mistaken for the same action everywhere.

## Start here

1. Press Ctrl+Shift+2 to start region capture. Drag a rectangle, or click a highlighted window. Press Escape to cancel.
2. In the editor, press A for an arrow, T for text, or R for a rectangle. Drag to draw; with Text, click and type. Press Enter to finish typing. If the editor does not open after capture, open it from the capture preview or tray.
3. Point at the Stroke or Font size field and scroll to adjust it quickly. Scroll over the canvas to zoom instead.
4. Press V to select an annotation. Drag it to move it; use arrow keys for precise positioning.
5. Press Ctrl+Shift+C to copy the entire finished image. Ctrl+C copies only selected annotations when a selection exists.
6. Press Ctrl+S to save. Choose an editable ShotOut project when you want to continue editing layers later.

Press F1 to open this guide. In the guide, use the section list to jump, or Ctrl+F to search. Press Escape to close the guide.

## Capture from any app

These shortcuts work outside ShotOut while it is running, including in the tray. Change them in Settings > Capture shortcuts. Another app may already own a global shortcut; Settings reports conflicts.

| Shortcut or gesture | What it does |
| --- | --- |
| Ctrl+Shift+2 | Start region capture. Uses smart element selection when that setting is enabled. |
| Print Screen (optional) | Start the same region capture when enabled under Settings > Capture. Requires Windows' Print Screen screen-snipping setting to be off. |
| Ctrl+Shift+3 | Capture the desktop across all screens. |
| Ctrl+Shift+4 | Start window capture. Hover over a visible window, then click it. |
| Single-click the ShotOut tray icon | Start region capture after the double-click detection delay. |
| Double-click the tray icon | Open or restore the editor. |
| Right-click the tray icon | Open capture commands, Settings, or Exit. |
| Alt+Down or F4 on the Capture button | Open its capture options menu when the button has focus. |

UI size is configured in Settings > Display: 75% (default) to 200% in 5% steps. Ctrl+plus (Ctrl+Shift+= or Ctrl+numpad+) increases toolbar size; Ctrl+minus (or Ctrl+numpad−) decreases it. This changes toolbars, annotation properties, layers, and image-effects controls without changing image zoom or annotation sizes. Large toolbars scroll to keep tools accessible.

After-capture tasks are configured in Settings > After capture. Copy to clipboard is on by default. Save automatically writes a PNG to the screenshot folder (including monthly folders when enabled). Save as prompts for a custom filename and location instead. Open containing folder and Open in default photo viewer save the capture first if needed. Cancelling Save as skips file tasks but keeps the capture and clipboard copy. Open in ShotOut editor, Show capture preview, Play sound, and Copy file path are also available. Copy file path saves the screenshot first and preserves clipboard image data when image copying is enabled.

After capture > Save options contains destination/Browse, monthly folders, PNG/JPEG/BMP format, and a filename pattern using {date}, {time}, and {milliseconds}, with an example. Name collisions receive numbered suffixes. Task presets (Custom, Quick copy, Save quietly, Capture and edit) remember their checkbox choices; destination, format, naming and sound settings are shared.

Screenshot audio is optional and off by default. In Settings > After capture > Sound options, choose one of the three clips, adjust volume, and use Preview to listen. Enable Play sound in the After capture checklist. The sound plays only after successful capture; cancelling is silent.

Print Screen is optional and off by default. In Settings > Capture, turn on Use Print Screen for ShotOut region capture. The switch saves immediately and displays Ready when registered. If it is blocked, guided setup offers Try again, the Windows keyboard settings page when needed, or an explicitly approved OneDrive restart when OneDrive may hold the key. That action briefly pauses syncing, lets ShotOut claim the key, then reopens OneDrive. It does not change OneDrive or Windows settings or close other apps. Other settings still need Save settings; Cancel does not undo the immediate Print Screen switch.

ShotOut must remain running; the tray is sufficient. Your other capture shortcuts and Win+Shift+S are unchanged. If startup order or exiting ShotOut causes a new conflict, choose Set up Print Screen again. OneDrive is not silently restarted at startup. Check Print Screen offers detailed diagnostics; an off Windows toggle alone does not guarantee availability.

## Region and smart region capture

Enable window snapping or experimental control detection in Settings > Capture. Smart selection depends on the regions another app exposes to Windows. Rounded controls are captured using rectangular bounds. Slow detection falls back to window snapping. Capture ignores hidden and known invisible helper windows.

| Shortcut or gesture | What it does |
| --- | --- |
| Left-drag | Draw a custom rectangular capture, overriding snapping. You can drag in any direction. |
| Click a highlighted window or region | Capture its displayed bounds. |
| Hover briefly in smart mode | Detect a control or message region. Moving inside a selected region keeps it stable; automatic detection may refine to a smaller region. |
| Scroll up in smart mode | Expand to a larger containing region or the window. |
| Scroll down in smart mode | Return to a smaller detected region. |
| Move within a scope chosen with the wheel | Keep that scope until the pointer leaves its bounds. |
| Hold Shift in smart mode | Temporarily select the entire hovered window. Release to resume element selection. |
| Hold Alt | Bypass snapping for free region selection. Drag to capture; an Alt-click alone does not capture a region. |
| Escape | Cancel the screenshot overlay, including during a drag or if keyboard focus moves elsewhere. No screenshot is created. |
| Right-click | Cancel the screenshot overlay. |

Window capture uses click-to-window selection. Wheel scope selection and Shift-to-window are controls for smart region mode, not separate window capture.

## New tab and image or project library

These controls apply on the browser page in a new tab. E has a different meaning in the editor: it selects Ellipse there. Shortcuts do not replace normal typing when a text field has focus.

| Shortcut or gesture | What it does |
| --- | --- |
| Ctrl+N, Ctrl+T or click the plus tab | Open a new tab with the library and blank-canvas choices. |
| E | Open selected library images or projects for editing. |
| Double-click a library item or press Enter with the list focused | Activate the item; open an image or project, or browse a folder. |
| Ctrl+click items | Select or deselect individual items. |
| Shift+click items | Select a range in the list. |
| Alt+Up | Browse to the parent folder. |
| F5 | Refresh the library. |
| Right-click an item | Open actions such as Edit, Open file, or Open file location. |
| Ctrl+O | Open an image or ShotOut project through the file picker. |
| Ctrl+K | Search and activate tools, recently closed tabs, saved files, open documents, and annotations. |

Select Images or Projects with the library buttons. Large folders have Previous and Next buttons for paging; no dedicated page-change shortcut is assigned.

## Tabs and files in the editor

| Shortcut or gesture | What it does |
| --- | --- |
| Ctrl+N or Ctrl+T | Open a new browser tab. |
| Ctrl+W | Close the current tab. Unsaved work may prompt for saving. |
| Ctrl+Shift+T | Reopen the most recently closed tab, including editable annotations and unsaved changes. |
| Right-click the tab strip | Reopen the last closed tab or choose from the last 20 recently closed tabs. |
| Ctrl+Tab | Switch to the next tab. |
| Ctrl+Shift+Tab | Switch to the previous tab. |
| Middle-click a tab or click its close icon | Close that tab. |
| Scroll over the tab strip | Move along the strip when tabs overflow. This does not zoom the canvas. |
| Ctrl+O | Open an image or editable project. |
| Ctrl+S | Save using the app's save workflow for the current document. |
| Ctrl+Shift+S | Open Save As choices for an image or editable project. |
| F1 | Open the full shortcuts guide, including while editing text. |
| / on a US keyboard | Also open the guide when the canvas has focus. F1 is the dependable choice across keyboard layouts. |

## Canvas navigation and selection

Click the canvas before using annotation commands. While typing or editing a numeric field, keys act on that field instead of moving or deleting annotations.

| Shortcut or gesture | What it does |
| --- | --- |
| Scroll over the canvas | Zoom in or out around the pointer; roughly 15 percent per notch. |
| Middle-button drag | Pan the canvas with any drawing tool selected. |
| Y then left-drag | Select the Hand tool and pan. |
| 0 on the number row or the Fit button | Fit the image to the available canvas. |
| Click 1:1 | View at actual pixel size. There is no dedicated 1:1 keyboard binding. |
| V then click an annotation | Select it. Locked images cannot be moved until unlocked. |
| Drag an annotation | Move it. Use Select for image layers; drawing tools draw over images. |
| V then drag empty space | Draw a selection marquee around multiple annotations. |
| Shift+click an annotation | Add it to or remove it from the current selection. |
| Drag a selection handle | Resize the selected object. |
| Arrow keys | Nudge selected objects by 1 canvas pixel. |
| Shift+arrow keys | Nudge selected objects by 10 canvas pixels. |
| Alt while dragging an image | Bypass alignment snapping and move freely. |
| Right-click the canvas | Open context actions; during a drawing drag it cancels that drag. |
| Escape | Cancel the current gesture or clear the selection, depending on what is active. |
| Escape twice with no other command between presses | Return to Select and leave active effect or sizing controls. Finish text first if typing. |

Image movement locks also block dragging and keyboard nudging. Use the lock icon beside the image layer or its context menu to unlock it. Tab-bar scrolling, sidebar scrolling, and canvas zoom are separate controls determined by where the pointer is.

## Annotation tool keys

These single-key defaults apply in the editor when you are not typing in a field. Change them in Settings > Tool shortcuts. Some tools share a toolbar button or appear in a tool-style or insert menu.

| Key | Tool |
| --- | --- |
| V | Select |
| A | Arrow |
| D | Double arrow |
| L | Plain line |
| R | Rectangle |
| E | Ellipse |
| F | Pen |
| T | Text |
| O | Speech balloon |
| N | Numbered circle |
| H | Highlight |
| B | Blur |
| P | Pixelate |
| C | Crop |
| M | Magnify |
| S | Spotlight |
| W | Eraser |
| J | Emoji |
| K | Cursor annotation |
| X | Solid redaction |
| U | Cut rows |
| G | Cut columns |
| I | Insert image |
| Q | Bucket fill |
| Y | Hand |
| Z | Clone stamp |

## Drawing and quick size adjustments

For quick adjustments, hover directly over the number field. Scrolling elsewhere may scroll a panel or zoom the canvas instead. For an exact value, click the field and type it. If nothing is selected, applicable controls set the style for new annotations.

| Shortcut or gesture | What it does |
| --- | --- |
| Scroll up or down over Stroke | Increase or decrease thickness using accelerated steps: about 15 percent of the current value, rounded, with a minimum step of 2. |
| Scroll over Font size | Increase or decrease text size using the same accelerated rule, in points. For example, 20 pt changes by 3 pt per notch. |
| Click a number field's up or down arrow | Adjust by 1 unit for precise changes, including 1 px for stroke or 1 pt for font size. |
| Up or Down while typing in a number field | Increase or decrease its value by 1 unit. |
| Type a value in a number field | Set an exact size, position, or other supported value, within its limits. |
| Enter or Escape in an editor number field | Return focus to the canvas. Escape does not promise to undo the value you typed. |
| Scroll over Circle size or Clone brush size | Use accelerated size steps, like Stroke and Font size. |
| Scroll over an ordinary number field | Usually adjust by 1 unit per notch. |
| Hold Shift while drawing a rectangle or ellipse | Constrain it to a square or circle. Supported rectangular annotation tools also use square geometry. |
| Hold Shift while drawing or aiming a line or arrow endpoint | Snap its angle to 45 degree increments. |
| Hold Shift while resizing a supported shape | Keep square geometry. This is not a universal image aspect-ratio shortcut. |
| Ctrl+drag or Alt+drag with a drawing tool | Force a new annotation instead of moving an existing one under the pointer. Clone stamp reserves Alt+click for sampling. |
| Drag an arrow's shaft or endpoints | Move the arrow, or adjust its start and end. |
| Drag an arrow's amber handle | Bend the arrow. |
| Drag an arrow's blue rotation handle | Rotate it; hold Shift for 15 degree rotation increments. |
| Drag a speech balloon's amber tail handle | Reposition its tail. |
| Drag a magnifier's source handle | Change which detail it magnifies when the source is linked. |

Default outline thickness scales with image dimensions. A manual stroke choice overrides that automatic default. There are no dedicated bracket-key or Ctrl+plus font-size shortcuts in this release; use the size fields and their wheel controls.

## Typing and formatting text

Press T and click to start text. For a speech balloon, press O and drag its box. Double-click an existing text annotation to edit it. In text editing, ordinary letters type characters rather than selecting tools.

| Shortcut or gesture | What it does |
| --- | --- |
| Enter or Ctrl+Enter | Finish and keep on-canvas text editing. |
| Escape | Also finish and keep on-canvas text. It does not discard your typing. Use Undo if needed. |
| Shift+Enter | Insert a new line without finishing the annotation. |
| Ctrl+B | Toggle bold on the selected text or subsequent typing. |
| Ctrl+I | Toggle italic. |
| Ctrl+U | Toggle underline. |
| Ctrl+A | Select all text in the active text field. It is not a Select All Layers command. |
| Ctrl+C, Ctrl+X, Ctrl+V | Copy, cut, or paste text inside the active text field. |
| Backspace or Delete | Delete characters or selected text, not the annotation layer. |
| Arrow keys, Home, End, Ctrl+arrow keys | Move the text caret using standard Windows text controls. |
| Shift plus a caret movement key | Extend the text selection. |
| Tab in on-canvas text | Insert a tab character. |
| Shift+Enter in a bulleted or numbered list | Continue the list, including the next number. |
| Shift+Enter on an empty list item | Remove its prefix and end the list. |
| Ctrl+S or Ctrl+Shift+S while typing | Finish text, then save or open Save As choices. |
| Ctrl+W or Ctrl+Tab or Ctrl+Shift+Tab while typing | Finish text, then close the tab or switch tabs. |
| Win+. in the emoji entry field | Open the Windows emoji picker. Use the dialog's Apply button or Ctrl+Enter to accept. |

Use the bullet or numbered-list buttons under Text options; there is no dedicated list-toggle shortcut. To change the size of an annotation, select it and adjust Font size. For formatting just some words, edit the text and select those words before applying bold, italic, or underline. Normal text-field undo behavior is separate from the document's annotation undo history.

## Copy undo and layer organization

| Shortcut or gesture | What it does |
| --- | --- |
| Ctrl+C with annotations selected | Copy those annotations, keeping editable data for pasting back into ShotOut. Linked layers are included together. |
| Ctrl+C with nothing selected | Copy the full finished image. |
| Ctrl+Shift+C outside text editing | Copy the full finished image even when annotations are selected. |
| Ctrl+V outside text editing | Paste copied annotations, an image, or text from the clipboard, as appropriate. |
| Ctrl+D | Duplicate selected annotations. |
| Ctrl+Z | Undo an annotation or document operation. |
| Ctrl+Y or Ctrl+Shift+Z | Redo. |
| Delete outside text editing | Remove selected annotations; in zone adjustment mode, remove the selected zone. |
| Page Up or Page Down | Move selected layers forward or backward in stacking order. |
| F2 or double-click a layer name | Rename that layer inline. |
| Enter while renaming | Keep the new name. |
| Escape while renaming | Cancel the rename. This differs from on-canvas text editing. |
| Click outside the rename field | Keep the new layer name. |
| Ctrl+click or Shift+click in Layers | Select individual layers or a range using Windows list selection. |
| Drag a layer row | Reorder layers. |
| Click a layer's visibility area | Show or hide it. |
| Right-click a layer | Access arrangement, linking, unlinking, flattening, and image-lock actions. |

Link, Unlink, Flatten, and image movement locking use menus or buttons; this release does not assign separate keyboard shortcuts to them.

## Crop cut and image effects

| Shortcut or gesture | What it does |
| --- | --- |
| C then drag crop edges or handles | Adjust the crop rectangle. |
| Drag inside the crop rectangle | Move it. |
| Enter while a crop is active | Apply the crop. |
| Escape while cropping | Cancel the crop selection. |
| Click a crop ratio preset | Choose Free, 1:1, 3:4, 4:3, or 5:4. Use these presets rather than a Shift shortcut for crop ratios. |
| U or G then drag and release | Cut out rows or columns. Releasing the mouse applies the cut immediately; switching tools keeps it. |
| Escape while a cut is active | Cancel or reverse the latest pending cut. Ctrl+Z remains available for undo. |
| Enter or Ctrl+Enter during Transform sizing | Apply the resize or canvas expansion. |
| Escape during Transform sizing | Cancel sizing. |
| Shift while dragging a Transform size handle | Temporarily keep the current aspect ratio. |
| Shift while drawing an adjustment zone | Constrain its geometry to a square in image pixels. |
| Shift while rotating an adjustment zone | Snap its rotation to 15 degree increments. |
| Space on a selected image-effect row in Layers | Toggle that effect's visibility. |
| Enter on a selected image-effect row | Open its controls. |
| Delete on a selected image-effect row | Remove the effect. |
| Drag an image-effect row | Change effect ordering within its image. |
| Escape twice | Leave effects or sizing controls and return to Select. |
| Double-click an adjustment slider label | Reset that adjustment to its default; Strength resets to 100 and the light and color adjustments reset to 0. |

## Clone stamp and color picker

| Shortcut or gesture | What it does |
| --- | --- |
| Z then Alt+click on the image | Sample a clone source. Alt+click again to choose another source. |
| Left-drag after sampling | Paint with the clone stamp. |
| Escape during a clone stroke | Cancel the current stroke. |
| Scroll over Clone brush size | Adjust the brush quickly. |
| Move the screen eyedropper | Preview the color under the pointer. |
| Left-click or Enter in the screen eyedropper | Accept the sampled color. |
| Right-click or Escape in the screen eyedropper | Cancel sampling. |
| Left or Right on a focused opacity slider | Change opacity by 1 percentage point. |
| Scroll over the opacity slider | Change opacity by 5 percentage points. |

Open the eyedropper from the color picker. No separate global eyedropper shortcut is assigned.

## Quick search

Press Ctrl+K in the main app. Search can find tools, recently closed tabs, saved images and projects, open tabs, and annotations in open documents. The filter buttons select All, Tools, Projects, Images, or Annotations. Type a tool name (for example, Bucket fill or blur), select its result and press Enter to activate it. Tool results show your current shortcut. Recently closed results reopen editable recovery copies.

| Shortcut or gesture | What it does |
| --- | --- |
| Type in the search field | Filter results by words in names or available annotation text. |
| Up or Down | Move through results. |
| Enter or double-click a result | Open it or jump to it. |
| Escape or Ctrl+K | Close search. |

## Settings and other menus

Settings has Capture, After capture, Tabs, Display, Editor, Files & folders, Startup & recovery, Capture shortcuts, and Tool shortcuts pages. Click a category, then scroll that page. Save settings applies changes from every page; Cancel discards unsaved settings.

| Shortcut or gesture | What it does |
| --- | --- |
| Click a shortcut field, then press a key combination | Assign a capture or tool shortcut. Capture shortcuts need Ctrl or Alt; tool shortcuts use single keys. |
| Backspace or Delete in a shortcut field | Disable that binding. |
| Tab or Shift+Tab in settings and dialogs | Move focus between available controls. |
| Space on a focused checkbox or button | Toggle the checkbox or activate the button. |
| Escape in Settings | Cancel and close without saving. |
| Up or Down in an open menu, then Enter | Choose a menu item using standard Windows menu controls. |
| Escape in an open menu | Close that menu. |
| Alt+F4 | Close the active window using Windows behavior. The main ShotOut window may remain running in the tray; use tray > Exit to quit completely. |
| Drag empty top-toolbar space or double-click it | Move the main window, or toggle maximize and restore. |

Ctrl+K and Ctrl+Shift+T are reserved for app search and tab recovery, respectively, and cannot be used as a global capture binding. Shortcuts already assigned elsewhere may conflict. Saved custom bindings take precedence over this guide's factory tool-key table.

## Practice a fast workflow

Try this on a disposable screenshot: capture with Ctrl+Shift+2; draw an arrow with A; hold Shift to aim it precisely; point at Stroke and scroll; add text with T; use Shift+Enter for a second line and Enter to finish; select with V; nudge with Shift+arrow; duplicate with Ctrl+D; undo with Ctrl+Z; copy the finished image with Ctrl+Shift+C; save an editable project for later.

When something unexpected happens, check where your pointer and keyboard focus are. Canvas scrolling zooms, field scrolling changes a value, and list scrolling moves the list. Escape cancels screenshot capture, keeps typed annotation text, or cancels a layer rename depending on the active screen.

Clone stamp previews sampled brush pixels while hovering and painting, with neutral, widely spaced dashed outlines. Alt+click selects a new source.

Drag blank areas of the top toolbar to move the window; double-click to maximize or restore. Capture dropdown contains delay; Include cursor is in Settings > Capture.

Right-click the screenshot canvas and choose Paste to insert clipboard images, text or copied annotations at that point. Ctrl+V remains available.

Annotation context menus hide unavailable commands; advanced actions are under Layer options. The color picker remembers the last ten applied colors, separate from favorites.

The save indicator beside the image information distinguishes Not saved, Copied · not saved, Unsaved changes, Image saved, and Project saved. Hover for the saved path; click it or press Ctrl+S to save.

First-use tool tips appear without blocking editing. Got it dismisses a tip; Turn off tips disables them. Settings > Editor lets you enable or reset tool hints.

Right-click Undo or Redo in the top toolbar to open its history. Each row shows an action and step count; selecting a row undoes or redoes all steps through that entry. History is local to each tab and bounded by Settings > Editor > Undo history. Ctrl+Z and Ctrl+Y remain single-step shortcuts. New edits clear redo history.

Cut rows and Cut columns apply when you release the mouse; their top Apply Cut and Cancel controls have been removed. Ctrl+Z reverses a cut. Crop retains Apply crop and Cancel.

New text defaults are 25% larger at the 1000px reference size, proportional to image pixels with a readable minimum. Manual font sizes and saved styles remain explicit choices.

Settings > Tabs controls the overflow scrollbar and optional automatic closing. Drag the bar, scroll the wheel/Shift+wheel, use the arrows, or cycle with Ctrl+Tab / Ctrl+Shift+Tab. Auto-closing is off by default. Configure a maximum tab count and idle age, then select saved and/or unsaved tabs. Current and recently visited/edited tabs are protected. The limit is soft when nothing is eligible. Unsaved tabs close only after an editable copy is saved in Projects / Auto-closed tabs. Use the folder button in Tabs settings to find these copies and reopen a .shotout file. These copies are kept until manually deleted.

Emoji: choose the Emoji tool, click the intended location, and choose from the native Windows picker. Win + . or Windows emoji… reopens it. Click Place emoji to confirm. Cancel leaves the document unchanged. Skin tones and joined sequences are retained; exported emoji use native Windows color-font rendering.

Emoji now keep their native Windows colors in the selection preview, canvas, copied/exported images, and reopened projects. Existing emoji annotations update automatically. Supported artwork follows the installed Windows emoji font, including skin tones and joined sequences. Text-color tinting is hidden for emoji because their native palette supplies their colors.

Tool icons: Highlight uses a chisel-tip marker, Spotlight uses a flashlight, and Bucket fill shows paint inside its pail. Bucket fill keeps the Q shortcut.

Settings > Display > Show names beside toolbox icons adds tool labels in a scrolling single column. Compact icons remain the default. Labels follow Ctrl+plus/minus UI sizing. Recently closed recovery lists the last 20 closures across launches; editable copies remain in Projects / Recently closed tabs or Auto-closed tabs until you delete them. Reopening restores the original name, save paths and unsaved state.

## Top toolbar menus
- File: New tab (Ctrl+T or Ctrl+N), Open image or project (Ctrl+O), Paste (Ctrl+V), Recent files, Save (Ctrl+S), Save image as, and Save editable project as.
- Export: Export image (choose PNG, JPEG or BMP) or Copy image (Ctrl+Shift+C). Ctrl+Shift+S still opens Save as choices.
- Undo and Redo retain their right-click history menus. Effects is beside these editing actions; Settings stays at the far right.

## Recently used tab order (optional)
Enable Settings > Tabs > Ctrl+Tab cycles tabs in recently used order.
- Tap Ctrl+Tab, releasing Ctrl afterward: switch to your last tab. Tap again to return.
- Hold Ctrl and press Tab repeatedly: move farther back through recently used tabs.
- While holding Ctrl, use Shift+Tab to reverse direction. Release Ctrl to finish the switch.
- Clicking a tab, opening a tab, or leaving the app ends the current cycling sequence. Closed tabs are excluded.
- With the option off (default), Ctrl+Tab and Ctrl+Shift+Tab follow the tab strip order.


## JPEG output quality
- After capture > Save options > After-capture JPEG quality controls JPEGs saved by capture tasks.
- Files & folders > Output quality > Editor JPEG quality controls JPEGs saved/exported from the editor.
- Both start at 100%. Drag the slider or type a percentage (1–100). Lower quality generally creates smaller files.
- PNG stays lossless, BMP stays uncompressed, and editable projects/clipboard keep full-quality images. JPEG at 100% is still lossy. Capture resolution and capture work are unchanged; JPEG encoding time can vary when saving.


