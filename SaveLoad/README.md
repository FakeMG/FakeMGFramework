# Save Load System

### ⚠️ Caution
- Versioning: Handle different versions of save data for backward compatibility
  - Manually handle in each Migration step:
    - Add fields (ES3 assigns C# default values for new fields, but we may want different default values for new fields)
    - Change existing field data type (ES3 assigns C# default value for the new type, but this may cause issues if the old data cannot be converted to the new type)
    - Rename keys
  - Handle in the SaveLoadSystem or by Easy Save 3:
    - Add new keys (Handled by the SaveLoadSystem, if a key is missing in the save file, it will restore the default state for that key)
    - Remove fields (Don't need to do anything, ES3 ignores removed fields)
    - Remove keys (Don't need to do anything, the unused data will be ignored when loading and removed when saving)

### ⏳ Planned (To-Do)
- UI Integration: Provide user interface components for save/load operations
- Replace Easy Save
- Editor tool
  - Metadata validation: prevent unsupported types
  - Support Ctrl + Z undo
- Make sure each field in the loaded data is initialized
  - Encryption
  - Compression
  - Caching: RAM-based caching to perform many reads/writes cheaply, then flush it to storage rather than constantly touching disk

### ❌ Out of Scope / Not Implemented
- Separate files per data. Save each data file individually instead of a central file like current implementation