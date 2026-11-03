# Preview it14 validation

- Build and branded NSIS compile succeeded; app/installer versions remain 1.4.2.0.
- Focused resize checks passed: no-copy flag only for size changes; unchanged toolbox layout skips work; height-only gallery sizing skips item repositioning; cached preview and gallery generation survive resizing; repeated maximize/restore preserves window bounds.
- Focused live-text resize and tab-preview checks passed.
- Separate inline-text, sharp display/export and responsive gallery checks passed, including transparent on-canvas text and gallery widths at 1100/1320/1800.
- Full self-test run stopped at the screen-dependent transparent live-text screenshot assertion. That check passed separately. The full run is NOT recorded as a pass. Earlier full-run portions including frame, resize, gallery, undo and display tests passed up to that point.
- Static restored-window screenshot inspected. Actual perceived flicker and desktop/GPU-specific transitions need user verification.
- No reliable before/after speed measurement is claimed. Synthetic resize timings include native painting and test-machine load. Changes avoid repeated layout/raster cache work; no animation delay, global WS_EX_COMPOSITED, or synchronous repaint loop was added.

Windows API reference for discarding stale client pixels:
https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-windowpos
