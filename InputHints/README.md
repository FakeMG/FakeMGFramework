# Input Hints

Shows the player which inputs do what right now, with glyphs for the device they are using. Hints follow the active
input contexts (screen, popup, gameplay mode, what is under the pointer). They are display-only: what an input does is
owned by `FakeMG.InputContexts`.

## How it fits together

- **`InputHint`**: a label plus the actions it shows, such as "Back" = Main Menu/Back. Owners can relabel or hide it at
  runtime (`SetLabel`, `SetShown`).
- **`InputHintContext`** (component): the hints of the `InputContext` on the same GameObject, shown while that context is
  active. `GetHintFor(actionReference)` returns the hint for an action, to relabel or hide it.
- **`ActiveInputHints`**: the hints to show. It follows `InputContextStack`, so it lists contexts newest first, down to
  the newest modal one. An action shows once, from the newest hint using it.
- **Glyphs**
  - `InputGlyphSetSO` maps control paths to glyphs for one device family.
  - `InputGlyphLibrarySO` lists the sets in priority order.
  - `ActiveGlyphSetTracker` follows the last device pressed.
  - `InputGlyphResolver` turns a hint into glyphs, falling back to the control's name when a glyph is missing.

## Setup

1. In the persistent scope, call `InputContextsInstaller.Install(builder)`, then
   `InputHintsInstaller.Install(builder, glyphLibrarySO)`.
2. Add a presentation and register it, e.g. a `ScreenBar/Input Hint Bar` prefab instance plus
   `InputHintsInstaller.InstallScreenBar(builder)`.
3. Next to each `InputContext` that should show prompts, add an `InputHintContext` and fill in its hints. Handle the
   actions through the `InputContext`, not the hints.

## Extension points

- **Presentation / placement.** Hints are drawn by presentation folders under `Runtime/Presentation`. `ScreenBar` is a
  strip along a screen edge, and its prefabs under `Prefabs/ScreenBar` give the structure.
  - To restyle it, make prefab variants in the game.
  - For a different placement, such as prompts floating over world objects for a game with a controllable character, add
    a sibling folder whose view reads `ActiveInputHints` and `InputGlyphResolver` the way `InputHintBar` does.
- **Glyph packs.** Runtime code only knows `InputGlyphSetSO` assets. `GlyphSets/Kenney` ships the Kenney Input Prompts
  (CC0) for these families:
  - keyboard and mouse;
  - Xbox, which also draws any other gamepad;
  - PlayStation 4 and 5;
  - Switch Pro.
  - To use another pack, create glyph sets for it, by hand or with an editor table like `Editor/Kenney`, and point the
    library at them.
  - Rebuild the Kenney sets: select `GlyphSets/Kenney/Textures`, then run *Assets > FakeMGFramework > Input Hints >
    Build Kenney Glyph Sets*.
- **More device families.** Add a glyph set whose *Device Layouts* name the Input System layout (e.g.
  `SteamDeckController`), and list it in the library before broader sets.

## Pitfalls

- An action shows its first binding the current device can perform, so list the preferred binding first.
- `InputHintContext.Find` throws when no hint uses the action. Add the hint in the Inspector.
- Hints ignore the pointer: the bar has no raycast targets or GraphicRaycaster. Keep it that way in variants, so the bar
  never blocks clicks on what is beneath it.
- Glyphs batch only when sprite packing is on (*Project Settings > Editor > Sprite Packer Mode*: Sprite Atlas V2). The
  Kenney build packs one atlas per texture folder under `GlyphSets/Kenney/SpriteAtlases`, holding only the glyphs the
  glyph sets use. For another pack, call `InputGlyphAtlasPacker.Pack` with its sprites.
