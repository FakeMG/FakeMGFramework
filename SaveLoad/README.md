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
- Use Easy Save 3: Leverages the Easy Save 3 asset for serialization, compression, and encryption

### ⏳ Planned (To-Do)
- Versioning: Handle different versions of save data for backward compatibility
- Separate save file into smaller files for different systems (e.g., player data, world state)
- UI Integration: Provide user interface components for save/load operations

### ❌ Out of Scope / Not Implemented