# Audio System
- A robust audio management solution for handling dynamic sound effects and music with per-cue customization.

## 🚀 Features

### ✅ Completed
- Randomization
  - Clip Randomization: Selects a random audio file from a list to ensure variety.
  - Volume Randomization: Applies slight volume shifts for a more natural feel.
  - Pitch Randomization: Modulates pitch to avoid repetitive "machine gun" sound artifacts.
  - Start Time Randomization: Initiates playback at different points within a clip for diversity.

- Timing & Playback
  - Delay Start: Postpone audio playback by a set duration.
  - Cooldowns: Set a minimum delay between triggers of the same Audio Cue ScriptableObject.
  - Fade In/Out: Smooth transitions for background music or ambient loops.
  - Looping: Toggleable looping support for any sound.

- Architecture
  - Individual Settings: Per-audio cue configuration on Audio Cue MonoBehaviour.
  - Global Settings: General configurations for all Audio Cue MonoBehaviours.
  - Dynamic Sources: Each sound played utilizes its own dedicated audio source.

- Mixer Controls
  - Independent volume control for Master, Music, and SFX channels.

### ⏳ Planned (To-Do)
- [ ] Audio Ducking: Automatically lower music/SFX volume during specific events (e.g., dialogue).
- [ ] Follow Target: Allow audio sources to attach to and follow moving GameObjects.
- [ ] Auto-Generator: Editor tool to automatically create Audio Cue ScriptableObjects from folders/selections.

### ❌ Out of Scope / Not Implemented
- In-Editor Preview: Currently, audio can only be previewed during Play Mode.