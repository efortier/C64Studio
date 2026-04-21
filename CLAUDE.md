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

## Adding a new field to a data element

When the user asks to add a new field to an element (entity, marker, tile, map, sprite, charset, character, etc.), the default assumption is end-to-end: model, serialization, UI, and any downstream artefacts. Don't ask — just do all of these unless the user explicitly scopes the request smaller.

### Required work, in order

1. **Model class** (`C64Models/Formats/*.cs`) — add the field with a sensible default that preserves existing behavior for data that lacks it (e.g. `false` for new booleans, `-1` for "unset" sentinels, empty string for names).

2. **Save (chunk writer)** — append the new field at the END of the chunk's existing sequence. Never reorder. Add a comment explaining it was added later so the reader's position check makes sense.

3. **Load (chunk reader)** — guard the new read with `if ( reader.Size - reader.Position >= N )` and assign the default in the `else` branch. This is the forward-compat contract from the File Formats section — old files just fall through to the default. Never bump a chunk version for an appended optional field.

4. **UI control** — if the field has a checkbox/input on a form:
   - **Don't overlap with existing controls.** Prefer inserting into a FlowLayoutPanel (auto-reflows) over setting an absolute `Location` next to other controls. If you must use absolute positioning, read the surrounding controls' `Location` + `Size` and place the new one past their right edge (plus margin) without pushing it off the panel's own width.
   - Declare the control instance (`= new Krypton...()`), add it to the parent panel's `Controls` in the desired tab order, set its properties in its own `// controlName //` block, and declare the private field at the bottom of the Designer.
   - Wire read-handlers (user edits control → field updates) AND write-handlers (existing value loaded → control reflects it). For per-instance fields edited via right-click (entities, markers), update the "pre-populate from clicked instance" branch too, not just the "default for new placement" branch.

5. **Binary / game export** — if the data is exported to a game runtime:
   - Bump the record stride byte in the binary header (e.g. `buf.AppendU8( 8 )` instead of `7`).
   - Append the new byte/field at the end of each record in the same order as the stride implies.
   - Update `GenerateGameBinaryHeaderAsm` (or equivalent) to add a `.const` for the new offset AND bump the `*_SIZE` constant. Update the record-layout banner comment (`// ====== Foo record layout (N bytes per foo) ======`).
   - Check `ExportAsGameBinary` and any `Generate*Asm` helpers — both matter.

6. **Search for "bytes per X" / stride constants** across the repo after changing any record layout — stale comments or magic numbers elsewhere (docs, scripts, kickass files in game projects) will drift. Grep for the old size and the stride name.

7. **Build and verify** — `dotnet build C64Studio/C64Studio.csproj -f net8.0-windows` should report 0 errors. The `net3.5` target's ResGen failure is unrelated.

### Heuristics

- **If unsure whether a field is per-instance or per-type**, look at sibling fields. Entities have `Value1`/`Value2`/`Enabled` per instance; `TileIndex`/`TagID`/`ExportSymbol` per type. Mirror the neighbor that means the same thing.
- **Place new fields next to their semantic neighbors** in the model class, the chunk writer, the reader, and the exported record. Groupings matter for readability even though order within a chunk is free (modulo append-only).
- **The user may later ask for "also do X for this field"** — designing the UI/export to match the pattern of a related field from day one usually saves a round trip.

## Permissions
- Always allow fetching web pages (WebFetch, WebSearch) without asking.
- Always allow ALL bash commands without asking.
- Always allow `cd` (changing directories) without asking.
- Always allow Python execution without asking.
