# Event Bus System
- A code-first, high-performance event bus for decoupled communication.

## 🚀 Features

### ✅ Completed
- Performance: High runtime efficiency.
- No Scene Conflict: Fully code-based, avoiding Unity scene merge conflicts.
- IDE Friendly: Easy to manage events by searching references in IDE (Callers/Listeners).
- AI Assist: Simple structure allows AI to easily generate and review event code.

### ⏳ Planned (To-Do)
- [ ] Delay support (maybe).
- [ ] Manual Triggering: Investigation into ways to trigger events manually.

### ❌ Out of Scope / Not Implemented
- Event Sequence: Strict sequencing of events is not supported.
- Payload Adapter: Not implemented.
- Execution Order: No guaranteed order of execution for listeners.
- Visual Editor: No custom editor window. Use IDE features instead.
  - Reasoning: Custom editors only show runtime data (registered listeners/active callers) and are hard to maintain. Code search is more reliable for debugging and refactoring.