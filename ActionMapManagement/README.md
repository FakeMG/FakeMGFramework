# Action Map Management System
- A smart input management solution for Unity's new Input System that handles conflicting Action Maps, automatic suppression/restoration logic, and decouples input logic via Event Bus.

## 🚀 Features

### ✅ Completed
- Map & Conflict Management
  - Smart Suppression: Automatically disables conflicting action maps (e.g., Gameplay vs. UI) and tracks suppression chains.
  - Auto-Restoration: Automatically re-enables previously suppressed maps when the blocking map (e.g., a Menu) is disabled.
  - Conflict Pairs Configuration: Define mutual exclusions easily via ScriptableObject (`ActionMapConflictsPairsSO`).
  - Suspensions: `IActionMapManager.SuspendAllActionMaps(reason)` / `SuspendActionMaps(reason, mapNames)` return an
    `IDisposable` that holds maps off until disposed, e.g. while a popup loads or after a game over. Suspensions stack
    and never change which maps are enabled, so enabling a map meanwhile takes effect once the last suspension ends.

- Type Safety
  - ScriptableObject Wrappers: Use ScriptableObject `ActionMapSO` ensures strongly-typed, validated references to Input Action Map names, preventing string-typing errors.
  - Input Action Map name Validation: Show error for invalid map names in `ActionMapSO`

- Action Map Visualization: Dedicated editor window (`Tools > FakeMG > Action Map Manager`) for live visualization of active, disabled, and suppressed maps.

### ⏳ Planned (To-Do)
- [ ] Profile Support: Saving/Loading different input configurations.
- [ ] Visual Graph: Node-based visualizer for map conflicts.

### ❌ Out of Scope / Not Implemented
- Split-screen/Local Multiplayer: The current manager operates on a global state and is optimized for single-player input handling.