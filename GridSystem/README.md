# GRID SYSTEM

## Script organization

- `Composition`: assembly configuration, dependency installation, and lifecycle entry points.
- `Grid`: grid ownership, visualization, occupancy, placement access, and pointer projection.
- `Placement`: grouped by common types such as events, factories, interfaces, models, registries, services, and states.
- `Structures`: structure components, extension interfaces, and ScriptableObject definitions.
- `Tests/EditMode`: tests mirroring the production responsibility and type folders.

- can change grid size in the inspector
- can change cell size in the inspector 
- place mode, edit mode (destroy, move)
- use unity grid component
- Pivot of the object
   - From the center of a cell in that object
   - Close to the ground
- objects have different sizes
- objects will occupy the space of a rectangle
- can switch between 3d and 2d by changing component
- can rotate the object before placing and in edit mode
- abstract the input, only care about object placement
- abstract the grid visual (place, destroy, move)
   - top down
   - fps minecraft
