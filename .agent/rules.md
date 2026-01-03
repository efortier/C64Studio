## Rules

## Coding Standards
- **Indentation**: Use 2 spaces for indentation.
- **Formatting**: Add spaces inside parentheses, e.g., `if ( condition )`.
- **Member Naming**: Prefix private fields with [m_], e.g., `m_ApplyingSettings`.
## Workflow & Verification
- **Build Verification**: Always run `dotnet build` to verify changes before completing a task.
- **UI Modifications**: 
  - Modify [Designer.cs] directly for adding static controls.
  - Avoid creating UI controls at runtime unless absolutely necessary (e.g., dynamic lists).
- **Persistence**: 
  - When modifying project settings, always increment the chunk version (e.g., `MAP_PROJECT_EXPORT_SETTINGS`) to ensure backward compatibility.
  - Ensure new settings are read/written in [ReadFromBuffer](cci:1://file:///z:/RepositoriesC64/C64Studio/C64Models/Formats/MapProject.cs:342:4-581:5) and [SaveToBuffer](cci:1://file:///z:/RepositoriesC64/C64Studio/C64Studio/Documents/MapEditor.cs:1992:4-1996:5).
## Architecture
- **Refactoring**: Extract reusable logic into helper methods (e.g., [AppendMarkerGlobalTables](cci:1://file:///z:/RepositoriesC64/C64Studio/C64Models/Formats/MapProject.cs:1995:4-2110:5)) rather than duplicating code across export types.
- **Export Logic**: 
  - Ensure new export features are implemented for both "Data" and "Assembly" export paths where applicable.
  - Respect existing formatting settings (Label Prefix, Hex/Dec format, etc.).
## Interaction
- Everytime the user asks you for changes, evaluate the changes and rank them according to ambiguity. In case of medium or high ambiguity, ask the user about the two most ambiguous changes for clarification before proceeding.
