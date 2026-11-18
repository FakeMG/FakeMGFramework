# Key Binding

Lets the player rebind actions per device family, with primary and secondary slots, conflict swapping, persisted
overrides, and a device diagram that highlights the selected binding.

## How it fits together

- **`KeyBindingService`** (Application): applies saved overrides from boot, rebinds a slot interactively, resets one slot
  or a whole family to defaults, and persists overrides through `KeyBindingOverridesSettingSO` (a `FakeMG.Settings`
  setting).
- **`KeyBindingConflictRules`** (Domain, engine-free): decides which slot takes the replaced key. Maps listed in
  `ActionMapConflictsPairsSO` may share keys.
- **`BindingDeviceFamilySO`**: the binding group a family owns, its slots per action, the glyph sets that draw it, and
  which controls a rebind may capture. The service checks those itself: Unity's own include paths cannot narrow a rebind,
  because a binding's control scheme already admits its whole devices.
- **Presentation**: `KeyBindingListView` and its rows fill the settings panel's Controls page. A slot is rebound by
  clicking it and reset by right-clicking it or by the reset-selected action. Rows implement
  `ISettingRow`, so the settings details panel shows their title and description. `BindingDiagramSubscriber` follows
  `SettingFocusTracker` and fills the selected binding on the active glyph set's diagram (`InputGlyphSetSO` masks), or
  draws the key large when the set has no diagram.

## Setup

1. Create a `Key Binding Config` (Create > FakeMGFramework > Key Binding). Assign the game's input actions, a
   `Key Binding Catalog` listing the rebindable actions, the device families in `ScriptableObjects/Input`, the
   overrides setting in `ScriptableObjects`, and the game's `ActionMapConflictsPairsSO`.
2. In the persistent scope, call `KeyBindingInstaller.InstallPersistent(builder, keyBindingConfigSO)`.
3. Give every action an empty binding in a family's group for each slot it leaves free, so the player can fill it.

## Extension points

- **New device families.** Create a `BindingDeviceFamilySO` for the binding group and list its glyph sets.
- **Other screens.** `KeyBindingService` has no UI dependency; a different screen can list `GetSlots` and call
  `StartRebind`.
