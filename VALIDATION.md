# Validation — 1.4.3

- Compiler and NSIS installer build succeeded; application and installer metadata report 1.4.3.0.
- Focused checks passed for recently used tabs, JPEG output quality, Print Screen diagnostics, text/window resize, top/bottom resize flags, and dark edge background.
- The full regression run stopped at a focus-sensitive Delete-key test; that test passed separately. It is not a full-suite pass.
- The earlier drag-expansion check clicked near a resize handle and failed in the previous preview as well; moving the test point to the rectangle center makes the intended drag and undo check pass.
- Top-edge resize smoothness and full installed upgrade were not verified interactively for this build. Light mode is not included.
