# Time-of-Cycle System

## Definition, Visuals

The Time-of-Cycle System manages a configurable repeating timeline and exposes its current state to other systems.

A day–night cycle is the initial use case, but the system is not limited to literal days, environmental visuals, fixed cycle lengths, or a specific collection of target objects.

### Cycle

* The cycle has a configurable length.
* The cycle may represent a day or any other repeating progression.
* Time wraps from the end of the cycle back to its beginning.
* The system reports cycle completion but does not manage calendar concepts such as dates, day numbers, weekdays, or seasons.

### Time State

The system distinguishes between:

* **Authoritative time:** The time used by gameplay logic and exposed as the current cycle time.
* **Presentation time:** The time used by outputs when presentation is transitioning separately from gameplay time.

These values are usually identical but may temporarily differ during a time jump with smooth presentation.

### Named Periods

* The cycle is divided into any number of configurable named periods.
* Periods cover the complete cycle.
* Periods cannot overlap.
* Periods cannot leave gaps.
* Exactly one period is active at every moment.
* At an exact boundary, the new period owns that time.
* A period is defined by an identifier and start time.
* A period’s end is inferred from the next period’s start.
* Periods do not require custom metadata for the initial scope.

### Time-Driven Outputs

The system supports arbitrary values and states that change according to cycle time.

Environmental lighting, sky presentation, fog, and ambient settings are example uses, not fixed responsibilities.

Each output definition contains:

* A stable identifier.
* The type of value or state it produces.
* Continuous or discrete evaluation behavior.
* Timeline points, period conditions, or both.
* Configured values associated with those points or periods.
* Its interpolation behavior, when continuous.
* Its own transition duration for profile changes.

#### Continuous Outputs

* Continuous outputs define configurable points along the cycle.
* Each point contains a value.
* The output evaluates between neighboring points.
* Each output owns its interpolation behavior.
* Different continuous outputs may use different interpolation rules.

#### Discrete Outputs

* Discrete outputs may react to named periods.
* Discrete outputs may also react to independent timeline points.
* Discrete points do not need to align with period boundaries.
* During normal progression, a discrete output changes when its configured condition is reached.
* During a time skip, only the destination state is applied.

### Evaluation and Application

Output evaluation and target application are separate responsibilities.

* An output definition determines the value or state associated with a given presentation time.
* A binding or applicator connects an evaluated output to an actual consumer.
* The profile does not directly reference particular scene objects.
* Different scenes or systems may bind the same output definitions to different consumers.
* Custom value types may be supported through appropriate evaluators and applicators.

### Configuration Profiles

A profile contains reusable configuration rather than current runtime state.

A profile may contain:

#### Cycle Configuration

* Cycle length.
* Default starting time.
* Default automatic advancement rate.
* Whether automatic advancement begins enabled.
* Default command-transition settings where applicable.

#### Period Configuration

* Period identifiers.
* Period start times.

#### Output Definitions

* Output identifiers.
* Value types.
* Continuous or discrete behavior.
* Timeline points and values.
* Period-based values or conditions.
* Interpolation behavior.
* Profile-change transition duration.

### Configuration Layers

Configuration is resolved through layers:

1. A shared profile supplies reusable defaults.
2. A scene, location, environment, or other context may override selected configuration.
3. Runtime overrides may temporarily replace selected values.

Runtime overrides have higher precedence than contextual overrides. Contextual overrides have higher precedence than shared defaults.

The core system receives the applicable configuration without directly depending on a particular scene-management implementation.

When the effective profile changes:

* Outputs transition smoothly from their previous values to their new values.
* Each output uses its own configured transition duration.
* Outputs do not need to share one global transition duration.

### Public State and Notifications

The system exposes:

* Current authoritative time.
* Current presentation time when it differs.
* Normalized cycle progress.
* Current named period.
* Whether automatic advancement is active.
* Current advancement rate.
* Period-change notifications.
* Cycle-completion notifications.
* Time-command completion notifications.

The active external controller does not need to be part of the public state.

## Boundaries & Constraints

* **Scope Limits:** The system does not own dates, day numbers, weekdays, seasons, or calendar progression.
* **Scope Limits:** The system does not decide what gameplay consequences periods or outputs produce.
* **Scope Limits:** Save and load systems are responsible for persistence and may restore time through the public interface.
* **Scope Limits:** Period metadata beyond an identifier and start time is not required initially.
* **Hard Constraints:** Periods must cover the complete cycle without gaps or overlaps.
* **Hard Constraints:** Exactly one named period must be active at every time.
* **Hard Constraints:** Each output independently defines continuous or discrete behavior.
* **Hard Constraints:** Each output defines its own profile-change transition duration.
* **Hard Constraints:** Automatic advancement, manual commands, and external control must all be supported.
* **Hard Constraints:** Outputs update automatically when their evaluated time changes.
* **Negative Space:** The feature is not limited to environmental visuals.
* **Negative Space:** Profiles do not store current runtime time, active commands, active controllers, or transition progress.
* **Negative Space:** Output definitions do not directly identify scene objects or other concrete consumers.
* **Negative Space:** A smooth visual transition does not necessarily mean gameplay time is passing gradually.
* **Negative Space:** Time skips do not simulate every intermediate period as gameplay state.
* **Negative Space:** Configurable output update frequencies are not exposed.

## Execution

### Interaction 1: Automatic Advancement

1. The system starts from its configured, restored, or externally assigned time.
2. Authoritative time advances according to the active rate.
3. Presentation time follows authoritative time.
4. Outputs evaluate the updated presentation time.
5. The active named period is resolved.
6. Crossing a period boundary publishes a period-change notification.
7. Reaching the end of the cycle wraps time to the beginning.
8. A cycle-completion notification is published.
9. Automatic advancement continues unless controlled externally.

### Interaction 2: Immediate Time Change

1. A caller requests a target time.
2. The caller chooses forward or backward direction when direction matters.
3. Authoritative and presentation time move immediately to the target.
4. Outputs evaluate only the target state.
5. Intermediate timeline points and periods are not processed.
6. If the final period differs from the previous period, exactly one period-change notification is published.
7. The command completes immediately.

### Interaction 3: Simulated Time Advance

1. A caller requests a target time and transition settings.
2. The caller explicitly chooses forward or backward movement.
3. Authoritative time gradually moves toward the target.
4. Presentation time follows authoritative time.
5. Gameplay systems observe intermediate times.
6. Outputs update throughout the progression.
7. Period-change notifications occur normally for every boundary crossed.
8. The command completes when the target is reached.

### Interaction 4: Time Jump with Smooth Presentation

1. A caller requests a target time and presentation transition settings.
2. The caller explicitly chooses forward or backward presentation movement.
3. Authoritative time changes immediately to the destination.
4. The final named period becomes the gameplay state immediately.
5. If the final period differs from the previous period, exactly one period-change notification is published.
6. Presentation time moves smoothly toward the destination.
7. Outputs evaluate the changing presentation time.
8. Intermediate periods do not publish gameplay period-change notifications.
9. The command completes when presentation reaches the target.

### Interaction 5: Replacing a Time Command

1. A one-time time command begins.
2. A new one-time time command is requested before the previous command finishes.
3. The previous command is cancelled.
4. The newest command begins from the system’s current state.
5. Only the newest command continues toward completion.

### Interaction 6: Persistent External Control

Persistent external controllers may request one of three behaviors:

* Pause automatic advancement.
* Override the automatic advancement rate.
* Provide authoritative time directly.

Each controller request:

* Represents exactly one behavior.
* Has a priority.
* Remains active until released or invalidated.
* Either allows all one-time commands or blocks all one-time commands.

Control arbitration works as follows:

1. External systems register control requests.
2. The request with the highest priority becomes active.
3. If multiple requests share the highest priority, the newest request becomes active.
4. Other requests remain registered but inactive.
5. If the active request is released or becomes invalid, the next eligible request automatically takes control.
6. A one-time command may begin only if the active controller allows commands.
7. Automatic advancement resumes when no persistent controller remains.

### Interaction 7: Profile Change

1. A new effective configuration is resolved from shared, contextual, and runtime layers.
2. Each output evaluates its destination value under the new profile.
3. Each output transitions from its current applied value toward its destination value.
4. Each output uses its own transition duration.
5. Outputs finish independently.
6. The profile change does not require one shared global transition duration.
