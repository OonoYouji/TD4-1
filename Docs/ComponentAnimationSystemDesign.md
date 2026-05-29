# Component Animation System Design Document

## 1. Overview
The Component Animation System allows for animating any property of an ECS Component or Script variable over time. This is inspired by Unity's Animator and AnimationClip system, providing a flexible way to create visual effects (e.g., UV scrolling, color fading, oscillating movement) and logic-driven animations.

## 2. Key Concepts

### 2.1 AnimationClip (.anim)
A resource file (JSON) that defines:
- **Duration**: Total length of the animation.
- **Looping**: Whether the animation repeats.
- **Tracks**: A collection of animation curves, each targeting a specific component and property.
  - **Target Path**: Identification of the target component (e.g., "Transform", "MeshRenderer", "Script:MyScript").
  - **Property Name**: The specific field to animate (e.g., "position.x", "material.uvTransform.offset.x", "myCustomFloat").
  - **Keyframes**: A list of `(time, value, interpolationType)` points.

### 2.2 AnimationPlayer (Component)
An ECS component attached to a GameEntity that:
- Manages the playback state (Play, Pause, Stop, CurrentTime, Speed).
- References one or more AnimationClips.
- **Binding**: At runtime, it resolves the "Target Path" and "Property Name" into direct memory pointers or setter calls to the actual components on the same entity.

## 3. Architecture

### 3.1 Data Structure (JSON Example)
```json
{
  "name": "FireballEffect",
  "duration": 2.0,
  "loop": true,
  "tracks": [
    {
      "component": "Transform",
      "property": "scale.x",
      "keyframes": [
        { "t": 0.0, "v": 1.0, "in": "Linear" },
        { "t": 1.0, "v": 1.5, "in": "Cubic" },
        { "t": 2.0, "v": 1.0 }
      ]
    },
    {
      "component": "MeshRenderer",
      "property": "material.uvTransform.position.x",
      "keyframes": [
        { "t": 0.0, "v": 0.0 },
        { "t": 2.0, "v": 1.0 }
      ]
    }
  ]
}
```

### 3.2 Property Binding System
To support animating *any* value, we will utilize:
1.  **Reflection/MetaData**: Leveraging the existing MetaData system used by the Inspector to find fields by string.
2.  **Variables Component Integration**: Direct integration with the `Variables` component to allow animating script-defined variables without manual C++ binding.

## 4. Implementation Plan

### Phase 1: Core Engine Support
- Create `AnimationClip` resource class and loader.
- Implement `AnimationPlayer` component.
- Implement the `AnimationSystem` to update `AnimationPlayer` states and apply values to targets.

### Phase 2: Property Accessor
- Build a robust property resolver that can navigate nested structures (e.g., `material.color.r`).
- Add support for interpolating basic types (float, Vector2/3/4, Color).

### Phase 3: Editor Integration
- Add "Animation" tab to the Editor.
- Create a simple Keyframe Timeline view for creating/editing `.anim` files.
- Add "Record" mode (optional/future) to capture property changes into keyframes.

## 5. Use Cases
- **UV Scroll**: Animate `MeshRenderer.material.uvTransform.position` for flowing water or energy effects.
- **Flickering Light**: Animate `Light.intensity` and `Light.color`.
- **UI Effects**: Animate `SpriteRenderer.color.a` for fading or `Transform.scale` for button pops.
- **Gameplay**: Animate custom script variables to control AI behavior or game state over time.
