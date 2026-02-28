# Localization

## 🚀 Features

### ✅ Completed
- Type safe localization: Keys and Arguments are all generated into code from a csv file via a code generation tool.
- Static usage tracking: All usages of localization keys are tracked via code generation. Can be used to find unused keys.
- Enfore argument count: When calling the generated code, the number of arguments must match the number of arguments in the csv. (Currently only enfore argument count, not type)
- Drop down to select localization key: Used in ScriptableObject, Prefab to select localization key instead of calling the generated code directly. (Cause writing different code for different SO is tedious)
- Sync Unity Localization Table to csv via AssetPostprocessor: When the csv file is modified, the Unity Localization Table is automatically updated to reflect the changes.
  - Remove keys from table that don't exist in the csv.
  - Add new keys to the table that exist in the csv but not in the table.
  - Update existing keys in the table if the value has changed in the csv.
  - Auto set smart string in the table.
- Support Unity smart string formatters
- Multi-table support
- General function that takes in key and arguments

### ⏳ Planned (To-Do)
- Sync localization csv to Unity Localization Table automatically when the table is modified.
- Enfore argument type.

### ❌ Out of Scope / Not Implemented

## ⚠️ Caution
- Switching key will make the existing usages point to wrong data.
- The key in different tables must be unique, otherwise the generated code will have duplicate keys and cause compile error.