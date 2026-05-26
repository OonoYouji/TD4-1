# Engine Bug Tracker

This document tracks bugs and issues identified in the engine. Please add new bugs to the "Pending Bugs" section as bullet points, and I will format them into the tracker.

## Pending Bugs (Quick Entry)
- (Add your bug descriptions here as bullet points)

---

## Status Legend
- 🔴 **New**: Not yet investigated.
- 🟡 **In Progress**: Currently being fixed.
- 🟢 **Resolved**: Fixed and verified.
- ⚪ **Closed**: Won't fix or duplicate.

---

## Active Bugs

| ID | Title | Priority | Status | Description |
| :--- | :--- | :--- | :--- | :--- |
| BUG-001 | C# SerializeField enum support | Medium | 🟢 Resolved | SerializeField does not work for enum types in C# scripts. |
| BUG-002 | C# Runtime WindowSize access | Medium | 🟢 Resolved | Cannot retrieve the runtime window size from C# scripts. |
| BUG-003 | C# SerializeField List<> support | Medium | 🟢 Resolved | SerializeField does not work for List<> types in C# scripts. |

---

## Bug Details

### BUG-001 C# SerializeField enum support
- **Priority**: Medium
- **Status**: 🟢 Resolved
- **Description**: SerializeField does not work for enum types in C# scripts.
- **Reproduction Steps**: Create a C# script with an enum field marked with [SerializeField] and try to edit it in the editor or see it serialized.
- **Expected Behavior**: Enums marked with [SerializeField] should be visible and editable in the editor.
- **Actual Behavior**: Enums were ignored in `Variables` component and had no editor UI in some cases.
- **Root Cause**: `Variables` component didn't handle `MONO_TYPE_ENUM` and the editor UI was missing enum support for template fields.
- **Resolution**: Added `MONO_TYPE_ENUM` support to `Variables.cpp` and implemented Enum Combo UI in `ImGuiShowField.cpp`.

### BUG-002 C# Runtime WindowSize access
- **Priority**: Medium
- **Status**: 🟢 Resolved
- **Description**: Cannot retrieve the runtime window size from C# scripts.
- **Reproduction Steps**: Try to access window width/height from a C# script.
- **Expected Behavior**: Provide an API to get current window dimensions.
- **Actual Behavior**: No API was available.
- **Root Cause**: Missing C# API and internal call.
- **Resolution**: Created `Window.cs` in C# library and added `InternalGetWindowSize` internal call in C++.

### BUG-003 C# SerializeField List<> support
- **Priority**: Medium
- **Status**: 🟢 Resolved
- **Description**: SerializeField does not work for List<> types in C# scripts.
- **Reproduction Steps**: Create a C# script with a List<T> field marked with [SerializeField].
- **Expected Behavior**: Lists marked with [SerializeField] should be serialized and editable.
- **Actual Behavior**: Lists were ignored by the engine.
- **Root Cause**: `Variables` component and editor UI did not support generic instances or list types.
- **Resolution**: Expanded `Variables` to support `std::vector` of basic types and implemented `ListField` editor UI in `ImGuiShowField.cpp`.

---

## Fixed Bugs

| ID | Title | Resolved Date | Resolution Summary |
| :--- | :--- | :--- | :--- |
| BUG-001 | C# SerializeField enum support | 2026-05-26 | Added Enum support to Variables and ImGui. |
| BUG-002 | C# Runtime WindowSize access | 2026-05-26 | Added Window.Size API to C#. |
| BUG-003 | C# SerializeField List<> support | 2026-05-26 | Added List<T> support to Variables and ImGui. |
