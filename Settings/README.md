# Settings

Player settings that every game shares: display, audio and key bindings. They are saved to one global settings document.

## Values

- **Definitions:** each setting is a `SettingDefinitionGenericSO<T>` asset with a stable `SettingId`, a label and a default.
  - `SliderSettingSO` (float)
  - `BoolSettingSO` (bool)
  - `OptionSettingSO` (one of a list)
  - `KeyBindingOverridesSettingSO` (from `FakeMG.KeyBinding`)
- **Repository:** `SettingsStateRepository` holds the current values.
  - A setting needs no registration: its value is created the first time it is read or written, from the saved value or from its default.
  - `OnSettingChanged(SettingDefinitionSO)` fires for every change, including a restore from the save.
  - `OnSettingsEdited` fires only for `SetValue`, so loading never triggers a save.
- **Options:** an `OptionSettingSO` saves its option's `Id` and shows its `Label`.
  - Renaming or translating a label keeps players' choices.
  - An unknown id falls back to the first option (`IndexOfOptionOrDefault`).
- **Persistence:** `SettingsSaveLoadInstaller.Install` registers the repository, `SettingsGlobalSaveDocument` and `SettingsSaveSubscriber`.
  - Every value is saved as invariant-culture text (`SettingValueText`): bool, int, long, float, double, string and enums.
  - A value that no longer parses (e.g. a setting whose type changed) is reset to its default, with a warning.

## Reacting to a setting

Subscribe to `OnSettingChanged` and compare the setting, or keep a table of settings to actions, as `DisplaySettingsSubscriber` does.
Read the value with `GetValue` at boot as well: nothing is raised for a value that was already loaded.

## Settings Menu

A tabbed panel (`Prefabs/UI/Settings Panel`) with display, audio and controls pages. It works with mouse, keyboard and gamepad.
Its Controls page comes from `FakeMG.KeyBinding`; see that module's README.

- **Rows** (`Runtime/Presentation/Rows`): each row implements `ISettingRow`.
  - `SettingRowView` shows the label and description of the setting its serialized `SettingUIBinder` binds.
  - Each control kind (`Slider`, `Dropdown`, `ToggleSwitch`) has its own binder folder.
  - `SettingFocusTracker` highlights the focused row, shows it in `SettingDetailsView`, and raises `OnSelectedObjectChanged`.
- **Panel** (`Runtime/Presentation/Panel`): explicit navigation between tabs and rows (`SettingsNavigationGraph`), plus tab paging input.
- **Display** (`Runtime/Infrastructure/Display`): `DisplaySettingsApplier` applies window mode, resolution, VSync and frame rate;
  `DisplaySettingsSubscriber` applies them at boot and on change.
- **Pages:** each page is a `Settings Scroll View` instance.
  - Static pages lay out once with `OneTimeLayout`.
  - The Controls page keeps its layout, because its rows depend on the device.

### Setup

1. Create a `Display Settings Config SO` (Create > FakeMGFramework > Settings > Display) and call
   `SettingsInstaller.InstallDisplay(builder, displaySettingsConfigSO)` in the persistent scope.
2. Make a prefab variant of `Prefabs/UI/Settings Panel` in the game and assign the input the framework cannot own:
   - `SettingsTabInputSubscriber`: the previous and next tab actions, and the settings menu `ActionMapSO`.
   - The tab glyphs' `ActionGlyphView` actions.
   - On `Tab Contents/Controls Page`: the reset-all and reset-selected actions, in `KeyBindingListSubscriber` and in its
     `InputHintContext`.
3. Place the variant where the menu opens it, and give the instance an `InputContext` assigned to the subscriber's panel context.
4. The scope that opens the menu calls `InjectGameObject` on the panel before showing it. Its views only store their dependencies,
   so injecting again on every open is safe.

**Action map naming:** the settings menu's action map asset is named `<Map> Action Map`, e.g. `Settings Menu Action Map`, with no
`SO` suffix. `ActionMapSO.ValidateNamingPattern` requires that name, so it is the one exception to the SO suffix rule.

### New rows

Add a row prefab with a `SettingRowView`, a `SettingUIBinder` subclass, and the row's selectables listed on the row view.
Row prefabs (`Prefabs/UI/Setting Slider Row`, `Setting Dropdown Row`, `Setting Toggle Switch Row`) serve both touch and
focus-driven menus.
- `Icon` and `Background` ship disabled, and the slider's `Value` ships enabled.
- A game's variant turns on what it needs, and moves the label when it shows the icon.
- Rows are placed by anchors, not a layout group.

The toggle switch's look comes from `IToggleSwitchVisual` components: `ToggleSwitchRectVisual` slides the handle and fill, and
the sprite and GameObject visuals swap art. A variant restyles the switch by changing or adding visuals.
