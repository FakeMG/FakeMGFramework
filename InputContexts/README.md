# Input Contexts

Decides who handles an input right now. Every screen, popup and gameplay mode that takes input is a context; only
the contexts on top handle it, so a dialog's Back closes the dialog and never also the screen beneath it.

## How it fits together

- **`InputContext`** (component): one screen, popup or mode.
  - It is shown while enabled, or turn off *Shown While Enabled* and call `Show()` / `Hide()` from code.
  - A *modal* context silences every context shown before it, e.g. a dialog over its screen.
  - Its owner handles actions with `AddHandler(actionReference, handler)`, paired with `RemoveHandler(...)`.
- **`InputContextStack`**: lists the active contexts, newest first, down to the newest modal one. An action belongs to the
  newest active context with a handler for it.
- **`InputContextRouter`**: when an owned action performs, it runs the owner's handler. It ignores:
  - keyboard presses into a focused text field, so typing "F" into a name never means Back;
  - actions another `performed` handler disabled first, e.g. by switching action maps.

## Setup

1. In the persistent scope, call `InputContextsInstaller.Install(builder)`.
2. Put an `InputContext` on every screen, popup and mode that takes input, and handle its actions:

```csharp
private void OnEnable()  { _inputContext.AddHandler(_backActionReference, Close); }
private void OnDisable() { _inputContext.RemoveHandler(_backActionReference, Close); }
```

On-screen buttons call the same method directly.

## Extension points

- **Hints.** `FakeMG.InputHints` reads the stack to show the hints of the active contexts. Any other presentation can do
  the same through `InputContextStack.ActiveContexts` and `OnActiveContextsChanged`.
- **Other context sources.** Anything implementing `IInputContext`, such as a plain class for a gameplay state, can be
  shown with `ShowInputContextEvent` / `HideInputContextEvent`.

## Pitfalls

- Contexts do not enable action maps; keep using `ActionMapManager` for that. A context only decides who handles input.
- A context that handles nothing still counts: a modal one silences everything beneath it.
- Actions that the EventSystem already delivers, such as Submit to the selected button, need no handler.
