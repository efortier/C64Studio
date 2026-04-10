# C64Studio (RetroDevStudio) - Development Rules

## Project Overview
C64Studio is a Windows Forms IDE for Commodore 64 / retro computer development. The solution (`C64Studio.sln`) is a .NET project targeting both `net3.5` and `net8.0-windows`.

### Key Projects
- **C64Studio/** - Main IDE application (WinForms). Contains editors, dialogs, controls, and the main form.
- **C64Models/** - Shared types, file formats, and data models. No UI dependencies.
- **Common/** / **CommonWindows/** - Shared utility libraries.
- **FastColoredTextBox/** - Source code editor control (forked/embedded).
- **Tiny64Emu/** - Embedded C64 emulator.
- **C64Ass/** - Assembler.

## Architecture Patterns

### File Formats (C64Models/Formats/)
- All project formats use a **chunk-based binary format** via `GR.IO.FileChunk`.
- Each format class has `SaveToBuffer()` and `ReadFromBuffer()` methods.
- Forward compatibility: use `subMemIn.Position < subChunk.Length` checks before reading optional new fields.
- Bump the version number when adding fields that change the sequential (non-chunk) portion.
- New optional fields appended to existing chunks do NOT require a version bump since the position check handles backward compatibility.

### Editor Documents (C64Studio/Documents/)
- All editors inherit from `BaseDocument`.
- Override `SaveToBuffer()` to serialize; override `LoadDocument()` to deserialize.
- Call `SetModified()` or set `Modified = true` when user makes changes. This adds "*" to the tab title.
- `RefreshDisplayOptions()` is called when theme/display settings change.

### Controls (C64Studio/Controls/)
- `CharacterEditor` is a reusable control embedded in `CharsetEditor` and `MapEditor`.
- It exposes a `Modified` event (`ModifiedHandler` delegate) that parent editors subscribe to.
- Call `RaiseModifiedEvent()` when the user changes data through the control.

### Theming (C64Studio/CustomRenderer/)
- `StudioTheme` handles dark/light/custom theming via `ColorableElement` enum.
- `ApplyTheme(Form)` recursively recolors all controls.
- Dialogs get themed in their constructor or load via `Core.Theming.ApplyTheme(this)`.
- Colors are accessed via `Core.Settings.FGColor(element)` / `Core.Settings.BGColor(element)`.

## Coding Conventions

### Style
- Use **spaces** for indentation (2 spaces).
- Allman brace style (braces on their own line).
- Extra spacing around parentheses in conditions: `if ( condition )`.
- Member variables use `m_` prefix (e.g., `m_MapProject`, `m_CurrentColor`).
- Private backing fields with public properties: `private int m_ColorSwatchSize; public int SwatchSize { get; set; }`.

### Safety
- Guard against divide-by-zero for any user-configurable numeric value used in division (swatch sizes, grid sizes, etc.).
- Clamp loaded values from files to valid ranges - corrupt or old files should not crash the application.
- Use `DoNotUpdateFromControls` flag to prevent recursive update loops during initialization.

## Building
- Use `dotnet build C64Studio.sln` from the repository root.
- The `net3.5` target will fail with `ResGen.exe not supported` under modern .NET SDK - this is a known pre-existing issue. The `net8.0-windows` target is the active development target.
- Check for `error CS` in build output to verify compilation success (ignore the ResGen error).

## Testing Changes
- There is no automated test suite. Changes should be verified by running the application.
- When modifying file formats, ensure backward compatibility: old files must still load correctly.

## Permissions
- Always allow fetching web pages (WebFetch, WebSearch) without asking.
- Always allow ALL bash commands without asking.
- Always allow `cd` (changing directories) without asking.
- Always allow Python execution without asking.
