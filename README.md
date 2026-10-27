# Orbit Library

A portable Windows emulator frontend with a dark space theme, transparent
console logos, fullscreen navigation, and a three-second animated Orbit intro.

[**Download Orbit Library for Windows**](https://github.com/themostonoon/orbit-library/releases/latest/download/OrbitLibrary-Windows.zip)

![Orbit Library preview with a demonstration library](docs/preview.png)

## Start playing

1. Download the Windows ZIP from Releases and extract the entire folder.
2. Run `OrbitLibrary.exe` on Windows 10/11 with .NET Framework 4.8 enabled.
3. Open **System setup** and choose a system, ROM folder, and emulator EXE.
4. Save the setup, scan your library, select a game, and press Enter.

This frontend launches your existing emulators. Games, BIOS, firmware, cores,
and emulators are not included. The executable is unsigned.

## Features

- 29 configurable console, handheld, arcade, and DOS system presets.
- Standalone emulator commands and RetroArch core launches.
- Search, favorites, play history, local cover artwork, and folder import.
- A virtualized gallery and background scans/artwork decoding.
- Cached stars and planets, smooth selection easing, and XInput navigation.
- Fullscreen startup; F11 switches back to a window.
- Portable settings and library data stored in `data/library.json`.

Keep the full application folder together. Your library remains local;
the repository and release contain no personal library data or game files.
The preview uses fictional demonstration titles.

## Controls

| Input | Action |
| --- | --- |
| Up / Down | Focus consoles / games |
| Left / Right | Move selection |
| Enter | Confirm a console or launch a game |
| F | Toggle favorite |
| Ctrl+K | Search |
| F11 | Toggle fullscreen |
| F1 | Help |
| XInput D-pad / left stick | Navigate |
| A / X | Confirm or launch / favorite |
| LB / RB | Switch category or system |

## Build from source

Run `build.cmd` on Windows. It uses the Windows .NET Framework C# compiler
and requires no package restore, Node.js, or Python. Keep `assets/` and
`orbit.ico` beside the resulting executable.

Run `OrbitLibrary.exe --self-test` for the built-in validation suite.
The `validation/` folder includes additional benchmark and stress-test sources.

See [START-HERE.txt](START-HERE.txt) for launch arguments and setup details,
and [PERFORMANCE.md](PERFORMANCE.md) for local before/after measurements.
The virtual gallery was stress-tested with 10,000 synthetic entries;
real emulator/game performance and physical controller feel were not measured.

## Artwork

Console logo images were generated with the built-in image generator and
retain transparent alpha. Prompts are in `assets/GENERATION-PROMPTS.json`.
Console names and marks identify their systems and belong to their respective
owners. Orbit Library is an original frontend and is not affiliated with
console manufacturers, Cocoon, or ES-DE.
