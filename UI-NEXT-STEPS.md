# Recommended next steps — ShotOut 1.4.1-it13

These priorities follow inspection of the current implementation and layouts, rather than a formal user study. I would prioritize consistency and recovery before adding more annotation tools.

| Priority | Current observation | Recommended change | User benefit |
| --- | --- | --- | --- |
| 1 | Preview 13 reduced first-drag preparation from 293 ms to 12 ms in the layered 4K benchmark. Overlapping lower effects still use the full renderer. | Profile overlapping-effect scenes separately and retain the pixel-quality regression checks. | Removes initial stickiness in complex captures without sacrificing continuous-drag speed. |
| 1 | Display sizing covers editing toolbars and properties, while settings, the color picker and emoji dialog use separate fixed layouts. | Make dialog sizing and text follow the same accessibility setting, with responsive layouts and checks at mixed monitor DPI. | Larger controls remain readable throughout the entire workflow. |
| 4 | Useful capture presets already exist, but new users must find them inside Settings. | Offer a brief, optional first-run setup: confirm the capture shortcut, choose Quick copy / Save quietly / Capture and edit, and confirm the save destination. | Establishes a predictable capture workflow and explains copying versus saving. |
| 5 | After-capture tasks, save options and sound settings occupy a long scrolling page. | Add a short active-task summary and collapsible Save options / Sound options sections. | Users can quickly understand what the next capture will do. |
| Before a public release | Branding now identifies ShotOut visually, while code signing is not configured. | Establish publisher signing for the app and installer, plus publish the existing hashes. Treat antivirus false-positive review separately. | Clearer publisher identity; branding by itself does not guarantee antivirus approval. |

Validation to retain: stationary screenshot pixels during shape dragging at fractional zoom; precise back-and-forth moves; multiple DPI / UI sizes; install/upgrade/uninstall checks. Existing synthetic tests are useful, but physical mouse/display smoothness should also be checked on the user's normal monitors.

A recovery-management screen should eventually show how much space auto-closed copies use. Any deletion/retention option should be explicit so users can keep important unsaved work.


Implemented in preview 13: recently closed recovery (Ctrl+Shift+T, tab menu, Ctrl+K), optional toolbox labels, tool search through Ctrl+K, and faster drag setup when lower effects are outside the affected region. A recovery-storage management screen and optional thumbnails remain future work.
