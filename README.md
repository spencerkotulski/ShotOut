# ShotOut

## v1.3.4: sharp Clone Stamp sampling

Fixed permanent clone softness at fractional zoom levels. Clone offsets now align to native image pixels and sample without interpolation, preserving text and fine details inside the brush. Hardness still controls edge feathering and opacity still controls blending. The hover sample follows the same pixel alignment. Newly painted clones retain source detail; previously blurred strokes need to be undone and repainted from the original source.

## v1.3.3: smoother zoom and canvas interaction

Zooming uses the existing composited image while the wheel is moving. After a 180 ms pause, sharp display-resolution markup is restored once at the final zoom. Export resolution, markup content, privacy effects, and Undo are unchanged. Canvas interaction uses a faster image sampler while moving, a reusable premultiplied interaction cache is capped at one million pixels (about 4 MB), the transparency checker is reused, and zoom status updates avoid refreshing document tabs. No dependencies or background rendering threads were added.

## v1.3.2: Clone Stamp hardness and opacity

Clone Stamp includes Hardness (0–100%) and Opacity (0–100%) controls. Hardness controls the feathered edge. Opacity applies evenly across each stroke regardless of dragging speed; release and paint again to build up coverage. The hover preview reflects both settings. Both default to 100% to preserve existing behavior. Soft painting uses brush-area tiles and retains one clone layer with Undo per stroke.

## v1.3.1: fluid Clone Stamp and screenshot thumbnails

Clone Stamp (Z): Alt-click a source, then drag to paint continuous strokes. A translucent sample preview follows the brush before painting. Strokes reuse one clone layer per document; each stroke still has its own Undo step. Painting brings that layer to the top. Existing moved or resized clone content is preserved when painting again.

New tabs display actual image thumbnails and folder icons from the configured screenshot folder. Thumbnails load in the background, with at most 200 per page, and release the original files immediately. No additional packages are required.

## v1.1.1: resizing, compact toolbar, and Hand

Restored native resizing from every edge and corner by reserving a resize gutter around the borderless window, so toolbar and sidebar controls cannot cover the resize targets.

Settings → Two-column toolbar in smaller windows is enabled by default. When the single column would need scrolling, tools arrange into two columns. Disable this setting to retain one column; the minimum window height increases to fit it, subject to the monitor working area.

Hand (Y) pans the canvas with a left-button drag. The middle mouse button still pans with other tools. Panning moves only the view and does not alter image content or undo history. Existing custom Y bindings are preserved; assign Hand a different letter in Settings if necessary.


## Windows installer

ShotOut-Setup-v1.3.4.exe bundles the application and requires no package downloads on Windows 10/11. Open it and click Install & launch. It installs for your Windows account in %LOCALAPPDATA%\Programs\ShotOut, creates a Start-menu shortcut and an optional desktop shortcut, registers ShotOut in Windows Installed apps, and opens the application. Administrator privileges are not required. Exit any running ShotOut from its tray menu before installing or updating.

If Windows startup was already enabled, setup points it at the installed executable. Otherwise startup stays off; it can be enabled in ShotOut Settings. Uninstall through Windows Settings → Apps → Installed apps → ShotOut, or run Uninstall.exe in the installation folder. Saved images, projects, preferences, and unknown files are preserved. A temporary cleanup executable may remain in Windows Temp after removal. The installer is unsigned.

Build the app with build.ps1, then build the installer with build-installer.ps1. The installer includes a compressed payload with an integrity check; it does not fetch or execute remote packages.


## Window-frame regression fix

Fixed Windows repainting a native title bar and border over the integrated toolbar when opening or closing a modal dialog, such as text entry. Activation now updates without native frame painting, caption bits are guarded during runtime style changes, and the toolbar repaints when the editor is re-enabled. Regression checks open and apply the real text dialog repeatedly, verify native frame/client bounds, and check maximize/restore afterward.


## Integrated toolbar and fill opacity

The separate Windows title bar has been replaced with minimize, maximize/restore, and close controls in the app toolbar. Drag its unused space to move the window; double-click it to maximize/restore. Window edges remain resizable, maximizing respects the taskbar, and Close still hides ShotOut to the tray.

Capture is now a split button: click Capture for region capture, or its down arrow for capture options. Alt+Down or F4 opens the menu when the button has focus.

Filled annotations have Fill opacity (0–100%) in the inspector. It adjusts shape, bubble, bucket, step, highlight, redact, eraser, spotlight, and cursor fills while preserving outlines and annotation text. Default is 100%, preserving the existing highlight/spotlight transparency. Image layers and strokes retain their existing rendering. Opacity supports Undo/Redo and editable projects.


## Initial image layer and resize redraw

New screenshots and opened images now start with a Screenshot image layer over a transparent canvas. Select it in Layers or use Select (V) to move, resize, duplicate, rename, hide, or delete it. Drawing tools continue to draw over the screenshot normally. Images pasted into a fresh start tab also become image layers. Automatic clipboard copy still includes the actual screenshot, and Place original beside canvas still uses the original pixels. Editable projects retain the screenshot layer.

The canvas now repaints its full viewport when the window size changes, clearing stale image fragments when shrinking and expanding the editor.


## Mixed-monitor capture and Bucket fill

Capture now enables per-monitor DPI awareness and reads the connected monitors in native desktop coordinates for every capture. Its overlay remains the exact size of the captured desktop when Windows sends DPI-change notifications, including layouts with screens left of or above the primary monitor. Restart ShotOut fully to use the new DPI mode.

Bucket fill (Q) fills the connected area under your click using the selected Fill color. Tolerance 0 matches exact colors; higher values include similar colors. It samples the visible image and annotations, then adds a separate Bucket fill layer. You can recolor, move, hide, delete, undo, or redo that layer. If Q was already assigned to a custom tool shortcut, that binding is preserved; assign Bucket a free key in Settings.


## Automatic copy and window snapping

Completed region, window, and full-screen captures copy to the clipboard automatically and open in an editor tab. Paste directly into Slack with Ctrl+V. After editing markup, use Copy image again for the edited version. Disable automatic capture copying in Settings if preferred. Clipboard failures retain the capture in the editor and show a retry message.

Region capture now highlights the window under the pointer. Click it to capture that window, or drag at least 4 pixels to select an exact custom region. Hold Alt to suppress snapping. Esc or right-click cancels. Settings → Snap to windows during region capture switches this feature off. Snapping follows window bounds; it does not detect individual text or webpage objects.


## Tight text selections and automatic canvas margins

Changing text size fits the selection to the visible text, with a small fixed gap. Growing and shrinking text no longer leaves an oversized selection. Text boxes preserve explicit line breaks; speech bubbles retain their normal inner padding.

Automatically added canvas space retracts when the outlying annotation shrinks, moves back inside, or is deleted. The original image area is always preserved. Space explicitly added through Expand canvas is preserved too. The minimum canvas area is stored per tab and in new editable projects, and Undo/Redo restores both annotations and canvas dimensions. Older projects without this metadata use their saved image size as the minimum.

Download/Save and Effects are next to Undo/Redo. Settings is the gear icon at the far right.


## Text growth, new tabs, and system tray

Increasing text size automatically grows text and speech-bubble bounds; the canvas expands when necessary. The + next to the tabs (Ctrl+N) creates a fresh start tab with Open image, Paste image, Capture, and Blank canvas choices. Other tabs keep their edits. The header shows only the logo beside Capture.

Closing X hides ShotOut to the Windows notification area and keeps capture shortcuts active. Double-click the tray icon or choose Open ShotOut to restore it. The tray menu also offers Capture, Settings, and Exit ShotOut. Exit checks for unsaved work; hiding preserves open tabs in memory. Start on Windows startup now starts hidden in the tray. If startup was already enabled, save Settings again after moving to this executable to update its startup path.


Cut direction remains flexible for the first 4 screen pixels, then stays locked until release; each new drag chooses again. Speech-bubble text centers vertically within the bubble body and horizontally by default. The sidebar uses one-click Left / Center / Right alignment icons. The top-right Effects button has an image-and-sparkle icon.


## Canvas, text alignment, and cut gestures

Copy image is beside File and Paste in the top toolbar (Ctrl+C still works). Moving or duplicating annotations beyond any canvas edge expands the canvas on release; inserted images retain their original size. Save, Copy, and editable projects include the entire expanded canvas. Expansion is undoable and preserves layers. New empty canvas areas are transparent. Use Image → Expand canvas for a specific larger size, or Image → Place original image beside canvas for an unmarked copy beside your current annotations. Place current edited view beside canvas duplicates the rendered view as a separate image layer.

Text and speech bubbles have Left, Center, and Right alignment in the sidebar, including rich text. New speech bubbles start centered; new text boxes start left aligned.

Cut automatically detects direction on each drag: sideways removes a vertical strip, and up/down removes a horizontal strip. The full strip is tinted while dragging and removed on release. Ctrl+Z restores it. Crop still uses a rectangular selection and Enter/Apply to confirm.


Settings → Show Layers panel can hide the entire layer section and give annotation options the full sidebar. This preference is saved. Rectangle, ellipse, speech balloon, and Magnify outlines accept 0 px (no outline); arrows, pen strokes, and effect strengths retain a minimum of 1.







A small, standalone Windows screenshot editor with a dark Photoshop-inspired workspace. Built from original C# source; no ShareX or Adobe code or branding is included. No installer, account, uploads, telemetry, or browser runtime. Requires Windows with .NET Framework 4.x (included with standard Windows 10/11 installations).

## ShotOut: layers and Magnify

The application, executable, settings folder, window titles, and startup entry are now named ShotOut. Existing SnapCanvas settings are read on first launch if ShotOut settings do not exist. Old .snapcanvas projects still open; new editable projects use .shotout. If Windows startup was enabled for the old app, re-save Settings from the new folder to point startup at ShotOut.exe.

Hovering a layer softly highlights its row and outlines the matching annotation on the canvas. During layer dragging, a blue insertion line shows precisely where the layer will be placed. Drop above or below the row as indicated.

Magnify now has a 100–800% control (200% = 2×). Its 1 px frame is drawn fully inside the enlarged image and covers all four sides. Turn on Source box + stem to show the sampled area separately and connect it to the enlarged view. The lens is placed beside the source when space allows. Drag the enlarged view to move it without changing the source, or drag the amber source point/box to retarget the detail. The source box updates accurately as you change magnification.

## Layer names, bubble tails, and shadows

Double-click a layer row/name (or press F2) to edit its name directly inside that row. Enter saves, Escape cancels, and changing focus saves. Renaming never opens a separate dialog. Annotation text editing on the canvas still uses the rich-text dialog.

Select a speech bubble and drag the amber diamond at its tail tip. The bubble body stays fixed; the tail attaches to the nearest appropriate edge as you move it around. Moving/resizing the whole bubble carries/scales the tail with it. The existing tail checkbox still hides it.

Every annotation now has a Drop shadow toggle in its properties. When enabled, adjust Shadow size (softness in pixels) and Shadow intensity (opacity percentage). These controls may require scrolling the properties panel for text/bubbles. Shadows start off by default, and their settings are saved with each annotation in editable projects and undo history. Shadow shapes are cached so ordinary movement does not recompute blur every frame.

## Drag performance update

Dragging and resizing now reuse the screenshot buffer, cache unaffected lower layers once per gesture, and redraw only the affected regions. Updates are composed during painting using the latest pointer position rather than waiting for a separate timer tick. Upper-layer transparency, blur/pixelation sampling, and layer order are preserved. Full-quality rendering is restored after release and used for exports. Undo states share the encoded background while it is unchanged.

A synthetic 4K benchmark with 24 rectangles, a large blur, and an arrow measured roughly 63 ms/frame for full redraws versus 0.7 ms/frame for incremental redraws (excluding the initial cache setup and display painting). Results vary by computer and scene.

This download contains only the executable and instructions; the logo is embedded, so no image files are needed beside the app.

## Logo, layers, and fill

The supplied logo is embedded in the Windows executable as a multi-resolution icon (16–256 px), in the window/taskbar icon, and in the app header. Logo.png is preserved unchanged; MakeIcon.ps1 generates the Windows ICO during builds.

Layer visibility uses a smaller eye at the right end of each row. Double-click a layer name or press F2 to rename inline: Enter commits, Escape cancels, and moving focus away commits. The context-menu Rename action uses the same inline field.

Fill color is always clickable for shapes. Applying a fill color enables Fill automatically; cancelling the picker changes nothing. Uncheck Fill to remove it. Rectangles and ellipses still start with Fill off by default.

## Tabbed captures

Every successful screenshot opens a new tab in the current editor window. Earlier captures remain open, with their own image, annotations, selection, crop, undo/redo history, unsaved state, and zoom/pan. The untouched welcome canvas is reused for the first capture. Canceling a screenshot leaves existing tabs intact. Opening an image/project or creating a blank canvas also opens a tab; pasting onto an existing image still inserts a layer.

Click a tab to switch. Use its ×, middle-click, or Ctrl+W to close it. Ctrl+Tab / Ctrl+Shift+Tab cycle through tabs. Tabs scroll with the wheel or the strip navigation arrows. Unsaved-close prompts follow the existing Settings preference, and exiting checks every open tab. Closing the final image returns to the welcome canvas. Open tabs are held in memory and are not restored after exiting; save images or editable projects you want to keep.

## Settings and rich text

Open **Settings** in the top bar. Save settings to apply them. Preferences are stored per Windows user in `%AppData%\ShotOut\settings.xml` and shared by downloaded versions.

- Allow multiple instances: off by default; another launch focuses the running app. Turning it off does not close already-open windows.
- Start on sign-in: adds/removes a current-user Windows Run entry for this executable. It requires no administrator rights. Keep the extracted folder in its current location; if moved, re-save the enabled startup setting from the new location. The app does not enable startup automatically.
- Capture hotkeys: global shortcuts with Ctrl or Alt. Click a field and press the new shortcut. Backspace disables it. Save checks for conflicts and restores the previous bindings if registration fails.
- Tool hotkeys: customize each single-letter tool shortcut. Standard editing commands stay fixed.
- Remembered capture delay/cursor settings, window position/size, zoom-to-fit, unsaved-edit confirmation, and undo history length.

Text and speech-bubble entry now use a rich text field: select text and click Bold, Italic, or Underline, or use Ctrl+B / I / U. Rich-text pasting is supported for these styles. Ctrl+Enter applies. Mixed formatting survives image export, undo, and editable projects. Inspector format buttons apply to the entire annotation; text size and color are annotation-wide. Markdown is deliberately omitted.

## Text and arrow update

Text and speech-bubble dialogs: Ctrl+Enter applies; plain Enter adds a new line. The shortcut is shown next to Apply.

Select an arrow to reveal handles: endpoint squares move one end independently; the middle dot moves the whole arrow; the blue round handle rotates around its center (Shift snaps rotation in 15-degree steps). Curved arrows have an amber handle on the curve: drag it to adjust the bend. Arrow Styles now includes Dashed arrow, Curved arrow, and Dashed curved arrow. Names, curve offsets, and styles are preserved in editable projects.

## Previous fixes

Pixelate averages the actual selected image into solid-color blocks. Higher strength increases the block size. Evenly sized integer tiles fully cover the selection, including fractional boundaries and image edges, without interpolation fringes. The surrounding image stays unchanged.

Click existing markup to select and drag it even with a drawing tool active. Press V and drag empty canvas space to select multiple objects; Shift-click adds/removes objects. Drag any selected object to move the group. Delete removes the selection; Ctrl+D duplicates it. Hold Ctrl when starting a new mark over existing markup to draw instead of selecting, moving, or resizing it. This applies to drawing tools; Select (V) retains selection behavior. Alt is also supported.

## Earlier refinements

- Frame-paced dragging avoids rebuilding the layer list during movement. Eight handles resize objects from any edge or corner, even while the drawing tool remains selected.
- Bicubic canvas rendering and supersampled final vector markup improve edge smoothness.
- A themed spectrum color picker includes hue, saturation/value, quick swatches, and hexadecimal colors.
- Text (T) and speech bubbles (O) are separate toolbar tools. Bubbles have corner-radius and tail controls.
- Drag layers to reorder. Click their eye icons to hide/show. Double-click a layer name or press F2 to rename. Right-click visibility controls remain available.

## Previous UI update

- Compact icon toolbar with vector icons and shortcut tooltips.
- Rectangle (R) has adjustable Corner radius in the inspector, including on existing rectangles. Old projects remain compatible.
- Arrow/line/double arrow and solid redaction/blur/pixelation each share a style control. Cut out has horizontal/vertical options.
- Only relevant properties appear. Crop actions appear in Crop mode; cut out shows Apply cut.
- Capture delay/cursor settings live beside Capture. Insert opens image/emoji/cursor choices.
- Consistent dark buttons, number fields, checkboxes, menus, and layer selections.

Close the previous version before opening this one so screenshot shortcuts are available. Extract to a separate folder and run ShotOut.exe. No installation or migration is needed.

## Start

Double-click **ShotOut.exe**. Choose **Capture region**, **Open image**, or **Paste image**. Mark the image, then click **Copy image** and press **Ctrl+V** in Slack or another app.

Keep the app open for its capture shortcuts:

| Shortcut | Action |
|---|---|
| Ctrl+Shift+2 | Capture region |
| Ctrl+Shift+3 | Capture all screens |
| Ctrl+Shift+4 | Choose and capture a window |
| Ctrl+C | Copy the final image |
| Ctrl+V | Paste an image or text |
| Ctrl+O / Ctrl+S | Open / export image |
| Ctrl+Z / Ctrl+Y | Undo / redo |
| Ctrl+D / Delete | Duplicate / delete annotation |
| Enter / Esc | Apply / cancel crop or cut selection |
| Wheel / middle drag | Zoom / pan |
| 0 / F1 | Fit image / shortcut guide |

Window capture: hover the window to highlight it, then click. Capture delay and cursor inclusion are in the menu beside Capture. Escape cancels capture. Capture shortcuts work only while the app is running and may conflict with other apps.

## Tool shortcuts

V Select · A Arrow · D Double arrow · L Line · R Rectangle · E Ellipse · F Pen · T Text · O Speech balloon · N Step · H Highlight · X Solid redaction · B Blur · P Pixelate · S Spotlight · M Magnify · W Eraser · C Crop · U Cut rows · G Cut columns · I Image · J Emoji · K Cursor.

## Editing

Arrows, double arrows, lines, rectangles, rounded rectangles, ellipses, freehand pen, text, speech balloons, step numbers, highlights, blur, pixelation, solid redaction, spotlight, magnification, sampled-color eraser, crop, horizontal/vertical cut out, image overlays, emoji, and cursor stamps.

Use **Select** to move an annotation; drag any of its **eight handles** to resize. Use the right panel to adjust stroke, fill, thickness, text size, bold, and italic. Double-click a text annotation to edit it. Arrow keys move the selection; Shift moves it 10 pixels. Page Up / Down or the right panel changes layer order. Right-click for visibility and flatten options.

Hold Shift while drawing for a square/circle or to snap a line/arrow to 45 degrees. For crop or cut out, drag an area and press Enter. Image menu includes resize, rotate, flip, grayscale, a padded gradient background, and manual image stitching.

**Save → Export image** produces a flattened PNG/JPEG/BMP. **Save → Save editable project** preserves the source image and editable annotations. Image transformations flatten annotations; Undo restores the previous objects. Undo retains up to 24 states in memory. Pasted images become movable layers when an image is already open. File drag-and-drop opens a new image. You can also pass an image/project path on the command line.

## Scope and validation

This is an initial standalone implementation of the screenshot and markup workflow, **not an exact ShareX clone or a claim of complete feature parity**. It does not include automatic scrolling capture, OCR, ShareX effect presets, advanced background customization, uploading, recording, Windows utilities, or Photoshop's photo-editing engine. The eraser uses a nearby sampled color; it is not content-aware. Magnification supports adjustable zoom and optional linked source callouts. Preferences and shortcut choices persist; the last active tool is not restored.

The included test report checks 20 annotation renderers, solid redaction, undo/redo, editable project serialization, crop, and both cut-out directions. The added checks cover settings serialization, shortcut validation, startup command construction, mixed text formatting, rich undo clones, and editable-project round trips. Startup execution after Windows sign-in and live hotkey/instance behavior require interactive validation; automated tests do not modify Windows startup. Clipboard export and actual screen capture require interactive Windows validation; clipboard access was denied in the build environment. Slack pasting has not been verified end to end. The executable is unsigned.

## Build and test

All source is included. Run `powershell -ExecutionPolicy Bypass -File .\build.ps1` from this folder. The build uses the Windows .NET Framework C# compiler and has no third-party dependencies.

Run `ShotOut.exe --self-test "C:\path\to\test-output"` for synthetic-image verification and a rendered UI preview. The self-test does not capture the desktop or alter the clipboard.
