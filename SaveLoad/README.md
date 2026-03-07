# Save Load System
- Only save needed data; save a key that references to other big data (id)
  - Pass the keys (id) to DataManager to load the big data
- Process (DataManager in a separate Manager scene) (single source of truth)
  - load gameplay scene + load data
  - data loaded -> Data Manager find the actual data from the keys (id)
  - scene loaded -> systems register to the Data Application Manager -> systems apply the loaded data
  - wait for all systems to apply data -> hide scene transition

## 🚀 Features

### ✅ Completed
- Auto Save: Automatically saves game state at defined intervals
- Manual Save/Load: Provides functions to manually save and load game state
- Multiple Save File Support: Ability to handle multiple save files
- Versioning: Handle different versions of save data for backward compatibility
  - Manually handle in each Migration step:
    - Add fields (ES3 assigns C# default values for new fields, but we may want different default values for new fields)
    - Change existing field data type (ES3 assigns C# default value for the new type, but this may cause issues if the old data cannot be converted to the new type)
    - Rename keys
  - Handle in the SaveLoadSystem or by Easy Save 3:
    - Add new keys (Handled by the SaveLoadSystem, if a key is missing in the save file, it will restore the default state for that key)
    - Remove fields (Don't need to do anything, ES3 ignores removed fields)
    - Remove keys (Don't need to do anything, the unused data will be ignored when loading and removed when saving)
- Use Easy Save 3: Leverages the Easy Save 3 asset for serialization, compression, and encryption
- Editor tool to edit save data directly in the editor when the data is encrypted
  - Edit existing key
  - Remove existing key
  - Add new key with a typed initial value for any supported creatable runtime type
  - Raw JSON editing mode
  - Metadata validation prevents renaming metadata keys or fields and blocks metadata deletion

### ⏳ Planned (To-Do)
- Separate save file into smaller files for different systems (e.g., player data, world state)
- UI Integration: Provide user interface components for save/load operations
- Editor tool
  - Support Ctrl + Z undo

### ❌ Out of Scope / Not Implemented