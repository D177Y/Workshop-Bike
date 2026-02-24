# Drag and Drop Playbook (Blazor + nested lists)

Use this exact pattern for reliable drag/drop in `SuperDefaultCategories` and similar nested UIs.

## What made it work

1. Use explicit HTML draggable strings, not boolean binding:
   - `draggable="@(canDrag ? "true" : "false")"`

2. Use separate handle classes per drag level:
   - Root items: `.root-drag-handle`
   - Child/sub items: `.sub-drag-handle`

3. Initialize DnD with row + handle selectors:
   - Root: `init(rootContainer, ..., "ReorderRoot", ".root-dnd-row", ".root-drag-handle")`
   - Sub: `init(subContainer, ..., "ReorderSub", ".sub-dnd-row", ".sub-drag-handle")`

4. In JS DnD helper, gate handle matching from `pointerdown` origin, not `dragstart` target:
   - Store pointerdown element on container.
   - During `dragstart`, validate handle from that stored origin.
   - This avoids false negatives where `dragstart` target is the draggable row.

5. In nested draggable UIs, isolate root/sub handle selectors so parent drag does not steal child drag.

## Current reference implementation

- Page: `Workshop/Components/Pages/SuperDefaultCategories.razor`
- JS helper: `Workshop/wwwroot/status-dnd.js`

## Quick checklist before sign-off

1. Category 1 drag works from root handle.
2. Category 2 drag works from sub handle.
3. Reorder persists after drop/reload.
4. Filter-disabled drag behavior still correct.
5. Hard refresh (`Ctrl+F5`) verified on deployed IIS site.
