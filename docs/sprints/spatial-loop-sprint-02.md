# Spatial Loop Sprint 02: Tangible Sample Instances

> Control update: the directional-pad clone/delete/undo/redo bindings in this
> historical sprint were superseded by
> [`spatial-loop-controls-workspace.md`](../spatial-loop-controls-workspace.md).
> Grip is now grab-only, trigger is contextual record/audition, and trackpad
> touch/click controls effects and navigation.

## Sprint goal

Turn the Sprint 1 clock and ring into an editable loop:

- Four sample instances appear on the ring from one shared demo sample.
- Each sample is a procedural arc rendered with the sample material.
- Grabbing and releasing an arc moves it in musical time.
- Released samples snap to meter-derived subdivisions.
- Samples can be cloned and deleted with Vive actions.
- Move, clone, and delete operations can be undone and redone.
- The audible demo pattern follows the edited model.

## Material and mesh decision

Keep the two materials separate:

- `Ring` is assigned only to `LoopRingView`.
- `Unlit` is assigned to `SpatialLoopController.Sample Material`.

The Sprint 2 sample body is generated in code. Its arc length must change with
sample duration, so a fixed Blender mesh would work against the interaction.

Blender will be useful for reusable pieces that do not need to deform around the
timeline:

- Attack and release handles
- Selection rings and icons
- Effect satellites
- Sample-bank shelves and tokens
- Transport controls
- Palette and preset objects

The procedural body can later incorporate or attach those authored meshes.

## Included

- `SampleInstanceView` with a procedural, extruded arc mesh
- Runtime view creation; no per-sample prefab or copied AudioClip
- `SpatialLoopController` as the loop/model/view coordinator
- Meter-aware snapping on release
- SteamVR grab behavior through `Interactable` and `Throwable`
- Clone and delete actions while a sample is held
- Undo and redo actions
- Demo scheduler events sourced from editable sample-instance data

## Deferred

- Recording and the sample bank
- Pitch from vertical position
- Velocity editing
- Duration handles
- Attack and release handles
- Snap-preview ghost
- Per-instance colors and waveforms
- Effects
- Save/load

## Unity setup

The existing `Spatial Loop Root`, `Loop Transport`, `Loop Ring View`, and
`Demo Loop Scheduler` stay in place.

### 1. Import and compile

1. Return to Unity and wait for script compilation.
2. Open **Window > General > Console**.
3. Confirm that no new compile errors appear.

### 2. Add the controller

1. Select `Spatial Loop Root`.
2. Add the `Spatial Loop Controller` component.
3. Assign:
   - **Sample Material:** `Assets/Materials/v2/Unlit`
   - **Sample View Prefab:** leave empty
4. Keep the defaults:
   - **Default Duration Ticks:** `240`
   - **Snap Subdivisions Per Group:** `4`
   - **Maximum Snap Distance Ticks:** `180`

The controller automatically finds the transport, ring view, and demo scheduler
on the same GameObject.

Leaving **Sample View Prefab** empty is intentional. The controller creates a
runtime GameObject and adds the procedural view, collider, Rigidbody,
`Interactable`, and `Throwable`. A reusable prefab can be introduced when custom
Blender-authored handles or icons are ready.

### 3. Assign Vive actions

On `Spatial Loop Controller`, assign these existing actions:

- **Clone Left Action:** `/actions/default/in/MenuLeft`
- **Clone Right Action:** `/actions/default/in/MenuRight`
- **Delete Left Action:** `/actions/default/in/LDPadDn`
- **Delete Right Action:** `/actions/default/in/RDPadDn`
- **Undo Left Action:** `/actions/default/in/LDPadLf`
- **Undo Right Action:** `/actions/default/in/RDPadLf`
- **Redo Left Action:** `/actions/default/in/LDpadRt`
- **Redo Right Action:** `/actions/default/in/RDpadRt`

The separate left/right actions match the project's current SteamVR action
layout. They can be remapped later without changing the edit commands.

### 4. Keep the existing material assignments

Verify:

- `Loop Ring View > Line Material` is `Ring`.
- `Loop Ring View > Playhead Material` is an opaque white material.
- `Spatial Loop Controller > Sample Material` is `Unlit`.

The ring and markers use `Ring`, the vertical playback marker uses its white
material, and only the sample arcs use the solid red `Unlit` material.

### 5. Enter Play mode

Expected result:

- Four red sample arcs appear at the 4/4 meter-group starts.
- The kick pattern sounds the same as Sprint 1.
- The playhead remains driven by the DSP transport.
- A runtime child named `Sample Instance Views` appears under
  `Spatial Loop Root`.

### 6. Move and snap a sample

1. Grab a red sample arc with the normal SteamVR grab action.
2. Move it around the ring.
3. Release it near another rhythmic position.

Expected result:

- The sample returns exactly to the ring.
- It snaps to the nearest enabled meter subdivision.
- The audible kick moves to the new time.
- The next scheduled occurrence uses the new position.

The sample object's transform is its leading edge, matching the audible trigger
time. The procedural mesh extends clockwise from that point for its duration.

Sprint 2 derives time from the released object's angle. Height and radial
distance are ignored on release and reset to the ring. Pitch and lane behavior
come later.

### 7. Clone and delete

While holding a sample:

- Press the hand's menu action to clone it.
- Press the hand's down-pad action to delete it.

Cloning leaves a copy at the sample's previous musical position while the held
sample can be moved elsewhere.

### 8. Undo and redo

While in Play mode:

- Press the configured left-pad action to undo.
- Press the configured right-pad action to redo.

Undo and redo operate on move, clone, and delete commands. They recreate runtime
views from model data when needed.

### 9. Regression checks

1. Leave playback running across several loop boundaries.
2. Move a sample shortly before the playhead reaches it.
3. Clone samples until several trigger together.
4. Delete and undo a sample.
5. Confirm `Ring` never appears on a sample and `Unlit` never replaces the ring
   material.

## Sprint acceptance checklist

- [ ] Four red sample arcs appear at quarter-note positions in 4/4.
- [ ] Samples are grabbable with the existing Vive Player setup.
- [ ] Released samples return to the ring and snap predictably.
- [ ] Moving a sample changes its audible trigger position.
- [ ] Clone creates a new model instance without copying the AudioClip.
- [ ] Delete removes only the selected instance.
- [ ] Undo and redo work for move, clone, and delete.
- [ ] Ring and sample materials remain independent.
- [ ] No duplicate or missed trigger is introduced at loop wraparound.
- [ ] Frame rate remains comfortable with at least 16 sample views.

## Next sprint

Sprint 3 should replace the synthetic four-instance setup with recorded sample
assets:

1. Extract microphone capture and trimming from the old sample pad.
2. Create a `SampleAssetData` registry.
3. Spawn a physical sample object when recording finishes.
4. Add immediate audition.
5. Add a collapsible recent-samples bank.

Pitch, duration, envelope handles, and waveform visualization can then operate
on real recorded assets rather than the temporary shared demo clip.
