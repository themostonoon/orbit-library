# Orbit Library 1.3 — rendering performance

Measured on this PC at 2560 × 1440 using the same 1,000 synthetic game records.
Both windows were focused throughout the four-second animation sample.

| Measurement | Version 1.2 | Version 1.3 |
| --- | ---: | ---: |
| Build the library view (ms) | 1226.3 | 88.4 |
| Show and lay out the window (ms) | 7901.7 | 8.3 |
| Full diagnostic screenshot (ms) | 340.4 | 59.5 |
| Background update (ms) | 69.9 | 0.9 |
| UI message-loop interval, 95th percentile (ms) | 341.0 | 35.1 |
| Process CPU time during four seconds of animation (ms) | 3734.4 | 218.8 |
| Private memory (MiB) | 73.8 | 77.3 |

These are local measurements, not a guaranteed frame rate. The message-loop
probe uses a Windows Forms timer and includes timer scheduling. A full
DrawToBitmap diagnostic screenshot is substantially heavier than a normal
partial repaint. The 1.3 memory measurement includes persistent scene buffers;
its improvement is lower CPU use and bounded work, rather than lower idle RAM.

The gallery paints only visible games, with a small tile cache. Background
artwork decoding uses a worker and a bounded 32 MiB cache. Display-size artwork
has a separate 24 MiB cache. Background gradients and planet sprites are
created once per size; subsequent frames restore and repaint small regions
using persistent native buffers. Interactive easing is elapsed-time based.
Slow-moving scenery updates at 30 Hz; UI transitions use a separate faster
timer. Animation pauses when the window is inactive or minimized. Console
logos preload during the existing three-second intro.

Folder scans run in the background. Results merge with the current library
on the UI thread so favorites and games added during a scan are preserved.
Search waits briefly for typing to settle before filtering.

Validation passed for a 10,000-game library, first/last navigation, bounded
controls and tile cache, search, system/favorites filters, background import
and repeated scans, favorite preservation, asynchronous thumbnails, and
GDI handle stability through repeated resizing. Existing launch, persistence,
fullscreen, alpha-logo, intro-duration, and data-recovery checks also passed.
Empty and populated fullscreen layouts were visually checked. Physical
controller feel and real emulator/game performance were not measured.

Raw measurements and stress-test sources are in validation/. Generated
logo originals are unchanged. This update does not alter emulator settings,
game files, or the existing library data.
