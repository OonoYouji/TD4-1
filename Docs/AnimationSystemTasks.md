# Animation System Implementation Tasks

## Phase 1: Resource & Component Foundation
- [ ] **Task 1: Define AnimationClip Data Class**
    - [ ] Create `AnimationClip` class in `Engine/Asset/Assets/Animation/`.
    - [ ] Implement `AnimationClipLoader` to parse `.anim` (JSON) files.
- [ ] **Task 2: Implement AnimationPlayer Component**
    - [ ] Create `AnimationPlayer` component in `Engine/ECS/Component/Components/ComputeComponents/Animation/`.
    - [ ] Add playback controls: `Play()`, `Pause()`, `Stop()`, `SetClip()`.
    - [ ] Add properties for `speed`, `isLooping`, `autoPlay`.
- [ ] **Task 3: Create AnimationSystem**
    - [ ] Create `AnimationSystem` in `Engine/ECS/System/AnimationSystem/`.
    - [ ] Implement the update loop to advance `currentTime` for all active `AnimationPlayer` components.

## Phase 2: Property Binding & Interpolation
- [ ] **Task 4: Property Path Resolver**
    - [ ] Implement logic to find components by name on an entity.
    - [ ] Implement a recursive property finder using MetaData (for C++ components) and `Variables::Get/Set` (for script variables).
- [ ] **Task 5: Interpolation Logic**
    - [ ] Implement Linear and Step interpolation for float values.
    - [ ] Add support for Vector2/3/4 and Color interpolation.

## Phase 3: Runtime Application
- [ ] **Task 6: Apply Animation to Components**
    - [ ] In `AnimationSystem`, calculate interpolated values based on `currentTime`.
    - [ ] Apply these values to the resolved property pointers.

## Phase 4: Editor & Polish
- [ ] **Task 7: Animation Tab in Editor**
    - [ ] Create a new Editor window for managing animations.
    - [ ] Add a timeline UI to visualize and edit keyframes.
- [ ] **Task 8: C# Scripting API**
    - [ ] Expose `AnimationPlayer` methods to C# via internal calls.
    - [ ] Add `OnAnimationEnd` callback support.

## Phase 9: Verification
- [ ] **Task 9: Sample Animations**
    - [ ] Create a "UV Scroll" sample for MeshRenderer.
    - [ ] Create a "Pulsing Light" sample.
    - [ ] Create a "Moving Platform" sample using Transform.
