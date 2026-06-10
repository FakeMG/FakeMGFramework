# Temporary Message Notification System

## Definition, Visuals

A reusable UI system for showing short temporary non-blocking text messages to the player.

The system displays messages at a configurable screen anchor position. The center of the screen is one possible anchor, but the system should also support other positions such as top-center, bottom-center, or other UI-defined anchors.

### Message
- A message is currently text-only.
- Each message contains displayed text.
- Empty or whitespace-only text is allowed.
- Each message can choose its own text color.
- Duplicate messages are allowed.
- If the same message is triggered multiple times, each trigger creates a separate message entry.
- All messages use the same configured display duration.
- Messages should not block pointer, mouse, touch, gamepad, or other UI/game input.

### Message Type / Color
- The system supports predefined message types.
- Example predefined types:
  - Info
  - Success
  - Warning
  - Error
- Each predefined type maps to a default text color.
- If the caller provides only text, the system uses a default type/color.
- Callers can also pass a custom raw text color.
- A custom color overrides the predefined type color for that specific toast.
- Predefined types only affect text color for now.
- Predefined types should not change prefab, font, text size, animation, sound, or behavior.

### Text Layout
- Toast width is controlled by the toast prefab/view layout, not by the toast manager.
- The toast manager does not calculate text wrapping itself.
- The toast view applies the text and color.
- The toast view calculates its final visible height after wrapping, clamping, and truncation.
- The toast view reports its final visible height to the toast manager.
- The toast manager uses the reported height only for stack positioning.
- Toast text can wrap.
- Toast height is calculated from the final visible text height.
- Toast height is clamped to a configurable maximum height or maximum line count.
- If text exceeds the allowed height, the visible text is truncated with ellipsis.
- A toast should never expand so much that it blocks a large part of the screen.

### Text Alignment
- Text alignment can vary by anchor/layout.
- For example:
  - Center anchors may use center-aligned text.
  - Left-side anchors may use left-aligned text.
  - Right-side anchors may use right-aligned text.
- Alignment should be configurable through the toast prefab/view setup rather than hardcoded globally.

### Message Stack
- Multiple messages can be visible at the same time.
- Newer messages appear at the configured anchor/entry position.
- Existing visible messages are pushed away from the entry position to make room for newer messages.
- Existing messages must animate to their new stacked positions.
- Stack positions should account for each message’s actual visible text height.
- A configurable spacing value adds extra space between message entries.
- Messages with different visible heights may push older messages by different distances.
- Messages should never appear, disappear, or move instantly.

### Stack Direction
- The system should support configurable stack direction.
- Upward stacking is the current expected default.
- The design should allow other directions later, such as:
  - Up
  - Down
  - Left
  - Right
- Stack direction affects where older messages move when newer messages appear.
- The core logic should calculate stack offsets based on direction, message size, and spacing.

### Animation Components
- Messages have show, hide, and reposition animations.
- The default example behavior is:
  - Show: scale up and fade in.
  - Hide: scale down and fade out.
  - Reposition: animate to the new stack position.
- The core system must not hardcode specific animation types.
- Animation behavior should be controlled by replaceable Unity components attached to the message view/prefab.
- New message animation styles should be addable later by creating or swapping animation components, without changing the core message queue/stack logic.
- The core system should only request animation actions such as show, hide, and move/reposition.
- The animation component decides how those actions look.
- Different message prefabs may use different animation components in the future.
- Animations and display timing should use unscaled time, so they continue when the game is paused or `Time.timeScale == 0`.

### Pooling
- Message views should be pooled and reused.
- The system should not create and destroy message views every time a message is shown.
- When a message finishes hiding, its view is returned to the pool for later reuse.
- If the pool has no available views, it may expand automatically.
- The pool has a configured maximum size.
- If the pool reaches its maximum size and no view is available, the system should remove the oldest active toast using the normal hide flow to make room.

### Global Access
- The toast system should be globally accessible through a service.
- Gameplay systems and UI systems should be able to request a toast without directly referencing the toast UI object.
- The service should expose a simple API for showing messages.
- The UI implementation should remain behind the service.
- If a toast is requested before the toast UI/container is ready, this is treated as a setup error.
- In that setup-error case, the request is ignored and a development warning is logged.
- Toast requests should not be queued before the UI is ready.

### Container
- The system uses one global toast container.
- It does not support multiple named toast channels for now.
- There should not be separate gameplay, combat, UI, or system-specific toast containers in this feature.

### Scene Behavior
- Active toasts should be cleared when changing scenes.
- Hiding toasts should also be cleared when changing scenes.
- Scene-specific messages should not persist into the next scene.
- Clearing on scene change should use the same hide animation flow.
- Toasts should not instantly disappear on scene change unless the object is forcibly destroyed by scene unload.
- After hide animation finishes, cleared toast views are reset and returned to the pool.

### TextMeshPro / Localization Support
- The system should use TextMeshPro layout behavior for wrapping, preferred height calculation, truncation, and dynamic text sizing.
- It should support localization-friendly text.
- The system should not assume a fixed string length, fixed language, or fixed character width.

## Boundaries & Constraints

- **Scope Limits**:
  - This system only handles temporary non-blocking text messages.
  - It does not handle confirmation dialogs.
  - It does not handle blocking popups.
  - It does not handle tutorial overlays.
  - It does not handle permanent message logs.
  - It does not handle interactive message actions.
  - It does not include sound effects.
  - It does not include multiple toast channels/containers.

- **Hard Constraints**:
  - Text-only for now.
  - Messages must not block input.
  - Empty or whitespace-only text is allowed.
  - Maximum visible message count is a global setting.
  - Display duration is a global setting.
  - Default message type/color exists when caller only provides text.
  - Stack layout must account for visible text height.
  - Extra stack spacing is configurable.
  - Stack direction should be configurable or future-ready.
  - Toast height must be clamped by configurable maximum height or maximum line count.
  - Overflowing text must be truncated with ellipsis.
  - Toasts must not expand so much that they block a large part of the screen.
  - Every show, hide, and movement change must be animated.
  - Animations and display duration must continue during pause using unscaled time.
  - When the visible message limit is exceeded, the oldest active visible message is removed using its hide animation.
  - A new message may show immediately while the removed oldest message is still hiding.
  - Messages that are already hiding do not count toward the max visible limit.
  - Same-message repeats create multiple separate messages.
  - Animation behavior must be component-based, not hardcoded into the core system.
  - Do not use strategy objects for message animation behavior.
  - Message views should be pooled/reused.
  - Pool should expand automatically when needed, up to a configured maximum.
  - Text width should be defined by the toast prefab/view layout.
  - Toast view should report final visible height to the toast manager.
  - Toast system should be globally accessible through a service.
  - The system uses one global toast container.
  - Active and hiding toasts should clear on scene change using the same hide animation flow.
  - Predefined message types only control text color for now.
  - Custom raw text color can override predefined message type color.
  - Toast requested before UI/container readiness is a setup error, not a queued request.
  - When a toast is cleared early, its display timer is cancelled and only the hide animation continues.

- **Negative Space**:
  - A message is not a modal popup.
  - A message is not a selectable UI element.
  - A message is not a persistent notification.
  - A message is not merged with another message just because the text is the same.
  - The example scale/fade animation is only one implementation, not the fixed behavior.
  - The system should not depend on one specific visual style.
  - The system should not rely on creating/destroying UI objects per message.
  - Stack layout is not fixed-height only; different visible text heights should be handled.
  - Long text should not cause a toast to grow without limit.
  - The system should not depend on scaled game time.
  - Callers should not need direct references to the toast UI object.
  - Toasts should not carry over between scenes.
  - Message type does not imply different prefab, font, size, animation, or audio.
  - The system should not play sound effects for toast show/hide.
  - The system should not support multiple named containers yet.
  - Toast requests should not be saved and shown later if the UI was not ready.

## Execution

### Interaction 1: Show Message
1. A caller requests a temporary message to be shown through the global toast service.
2. The caller provides at least the message text.
3. The caller may provide a predefined message type.
4. The caller may provide a custom text color.
5. If a custom color is provided, it is used for that message.
6. If no custom color is provided, the predefined message type’s default color is used.
7. If no predefined type is provided, the default type/color is used.
8. The system gets an available message view from the pool.
9. If the pool has no available view and has not reached its max size, the pool expands automatically.
10. If the pool has reached its max size and no view is available, the oldest active visible toast is removed using the normal hide flow to make room.
11. The toast view applies the text and color to the TextMeshPro text component.
12. The toast prefab/view layout determines the message width.
13. The message text wraps if needed.
14. The toast view clamps visible height by maximum height or maximum line count.
15. If the text exceeds the allowed height, the visible text is truncated with ellipsis.
16. The toast view reports its final visible height to the manager.
17. The system adds the message to the active visible message list.
18. The new message appears at the configured anchor/entry position.
19. The new message plays its show animation through its animation component using unscaled time.
20. Existing active visible messages animate away from the entry position to make room for the new message.
21. Their stack positions are calculated from visible message heights, configured spacing, and stack direction.
22. The message remains visible for the globally configured display duration using unscaled time.

### Interaction 2: Hide Message After Duration
1. A visible message reaches the end of its display duration.
2. The message is removed from the active visible message list.
3. The message plays its hide animation through its animation component using unscaled time.
4. Since the message is now hiding, it no longer counts toward the max visible limit.
5. Remaining active visible messages recalculate their stack positions using visible height, configured spacing, and stack direction.
6. Remaining active visible messages animate into their updated stack positions.
7. After the hide animation finishes, the message view is returned to the pool.

### Interaction 3: Show Message While Others Are Visible
1. A new message is requested while one or more messages are already visible.
2. The system gets a separate message view from the pool.
3. The system creates a separate message entry, even if the text matches an existing message.
4. The text is wrapped, clamped, and truncated if needed.
5. Existing active visible messages animate away from the entry position.
6. Stack offsets are based on the active messages’ visible heights plus the configured spacing value.
7. Stack movement follows the configured stack direction.
8. The new message plays its show animation at the configured entry position.
9. All visible and hiding messages remain non-blocking to input.

### Interaction 4: Exceed Maximum Visible Messages
1. A new message is requested while the active visible message count is already at the global maximum.
2. The oldest active visible message is selected for removal.
3. The oldest message is removed from the active visible message list.
4. The oldest message starts its hide animation.
5. The new message is added immediately; it does not need to wait for the oldest message’s hide animation to finish.
6. The new message plays its show animation.
7. The remaining active visible messages recalculate their positions using visible height, configured spacing, and stack direction.
8. The remaining active visible messages animate into their updated positions.
9. When the removed message finishes hiding, its view is returned to the pool.
10. The system keeps the active visible message count within the configured maximum.

### Interaction 5: Game Paused
1. The game is paused or `Time.timeScale` becomes `0`.
2. Existing toast display timers continue using unscaled time.
3. Toast show, hide, and reposition animations continue using unscaled time.
4. Toasts continue their normal lifecycle even while gameplay time is paused.

### Interaction 6: Scene Change
1. A scene change begins or the toast system receives a scene-clear event.
2. Active visible toasts are cleared through the same hide animation flow.
3. Each cleared toast has its display timer cancelled.
4. Hiding toasts continue or restart their hide flow as needed.
5. Cleared toast views do not remain in the active visible list.
6. Cleared toast views no longer count toward the max visible limit.
7. After the hide animation finishes, cleared toast views are reset and returned to the pool.
8. No visible toast message persists into the next scene.

### Interaction 7: Toast Requested Before UI Is Ready
1. A caller requests a toast through the toast service.
2. The toast service detects that the toast UI/container is not ready.
3. The request is treated as a setup error.
4. The request is ignored.
5. A development warning is logged.
6. The toast is not queued for later display.

### Interaction 8: Toast Cleared Early
1. A toast is cleared before its display duration ends.
2. The toast’s display timer is cancelled immediately.
3. The toast is removed from the active visible list.
4. The toast plays its normal hide animation.
5. Remaining active visible toasts recalculate their positions.
6. Remaining active visible toasts animate into their updated positions.
7. After the cleared toast finishes hiding, it is reset and returned to the pool.