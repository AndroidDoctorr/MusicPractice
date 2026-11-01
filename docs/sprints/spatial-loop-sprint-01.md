# Spatial Loop Sprint 01: Clock, Meter, and Ring

## Sprint goal

Prove the foundation of the spatial loop before building sample recording,
sample-bank, effects, or polished interaction:

- Musical positions are stored as exact ticks rather than scene transforms.
- 4/4, 3/4, and grouped 6/8 produce correct bar lengths and marker positions.
- A DSP-based transport runs independently of frame rate.
- A known AudioClip plays four times per 4/4 bar through scheduled audio.
- A generated ring and playhead follow the same transport as the audio.
- The scene needs only one new root object; the existing space and Player remain
  unchanged.

## Included

- Musical-time and time-signature model
- Beat grouping and meter-aware snap candidates
- Basic loop and sample-instance data
- DSP transport
- Four-on-the-floor demo scheduler and reusable AudioSource pool
- Generated runtime ring, meter markers, and playhead
- EditMode tests for timing and snapping

## Not included

- Microphone recording
- Grabbing, cloning, or deleting sample objects
- `SampleInstance` prefab
- Waveform or arc mesh
- Sample bank
- Effects palette or effect presets
- Save/load

Those depend on this sprint's time/model boundary and start in later sprints.

## New runtime components

### `LoopTransport`

Owns BPM, meter, loop length, DSP epoch, and play/stop state. It is the source
of truth for both audio scheduling and visual phase.

### `DemoLoopScheduler`

Schedules one assigned AudioClip at each meter-group start. In 4/4 this produces
four-on-the-floor playback. It is intentionally a disposable vertical-slice
component, not the final sample-instance scheduler.

### `LoopRingView`

Generates the ring and meter markers at runtime and moves a playhead from the
transport phase. It also draws Scene-view gizmos while selected.

## Unity setup

The existing environment and Player rig do not need to be rebuilt.

### 1. Let Unity import the new scripts

1. Open the project in Unity 2021.3.8f1.
2. Wait for script compilation and `.meta` generation to finish.
3. Open **Window > General > Console**.
4. Resolve any compile errors before continuing. Warnings from older code can be
   handled separately unless they block Play mode.

### 2. Open the scene that contains the space and Player

Use the scene where the existing Player rig already works. Save a copy as a PoC
scene if the current layout must remain untouched:

1. Choose **File > Save As**.
2. Create `Assets/Scenes/v2` in the Project window if it does not exist.
3. Save as `SpatialLoopPoC.unity`.

Keeping a separate scene is recommended but not required for the first run.

### 3. Create the spatial-loop root

1. In the Hierarchy, choose **Create Empty**.
2. Rename it `Spatial Loop Root`.
3. Do **not** parent it to the headset or a controller.
4. Place it at the center of the playable area:
   - X and Z: the center used by the Player/tracking origin.
   - Y: comfortable ring height, initially about `1.0` to `1.2` metres for
     standing play.
5. Keep rotation at `(0, 0, 0)` and scale at `(1, 1, 1)`.

The root's forward direction is loop time zero. Rotate the root around Y if time
zero should face another direction.

### 4. Add the transport

1. Add the `Loop Transport` component to `Spatial Loop Root`.
2. Set:
   - **BPM:** `120`
   - **Bars:** `1`
   - **Play On Start:** enabled
   - **Schedule Lead Time:** `0.1`
3. Leave the time signature at its default 4/4 grouping.

The transport starts slightly in the future so the demo scheduler can queue the
first event at tick zero.

### 5. Create and assign ring and playhead materials

1. In `Assets/Materials/v2`, create a Material named `LoopRing`.
2. Use **Unlit/Color** in the built-in render pipeline.
3. Choose a bright color that is readable in the current environment.
4. Create a second opaque white material named `Playhead`.
5. Add the `Loop Ring View` component to `Spatial Loop Root`.
6. Drag `LoopRing` into **Line Material** and `Playhead` into
   **Playhead Material**.
7. Set:
   - **Radius:** `0.85`
   - **Ring Segments:** `128`
   - **Line Width:** `0.01`
   - **Marker Width:** `0.012`
   - **Minor Marker Length:** `0.06`
   - **Major Marker Length:** `0.12`
   - **Playhead Height:** `0.3`
   - **Playhead Radius:** `0.012`

No ring prefab is required. The component generates the line, markers, and
vertical playhead under a runtime-only child named `Generated Ring`.

### 6. Assign a test sound

1. Add the `Demo Loop Scheduler` component to `Spatial Loop Root`.
2. Drag a short kick, click, or closed hi-hat AudioClip into **Demo Clip**.
   Import a temporary WAV if no source AudioClip is currently available.
3. Set:
   - **Velocity:** `0.8`
   - **Voice Count:** `8`
   - **Look Ahead Seconds:** `0.1`
   - **Minimum Schedule Lead Seconds:** `0.02`
4. Leave **Play At Meter Group Starts** enabled.

Use a short one-shot for the first test. A long clip can overlap later events
and make scheduler behavior harder to hear.

### 7. Enter Play mode

Expected result:

- A ring appears around `Spatial Loop Root`.
- Time zero is forward from the root.
- Four strong meter markers appear in 4/4.
- A playhead moves clockwise.
- The assigned clip plays once per quarter note.
- Visual and audible phase return to zero together at each bar.

Move the root or change its radius if the ring intersects scenery or is outside
comfortable reach. Do not parent the root to the headset to solve alignment;
move or recenter the workspace anchor instead.

### 8. Check 3/4 and 6/8

Stop Play mode before editing serialized meter data.

For 3/4:

1. Set numerator `3`, denominator `4`.
2. Set beat groups to three entries: `1, 1, 1`.
3. Keep the tempo beat unit at **Quarter**.
4. Enter Play mode and confirm three grouped pulses per bar.

For 6/8:

1. Set numerator `6`, denominator `8`.
2. Set beat groups to two entries: `3, 3`.
3. Set the tempo beat unit to **Dotted Quarter**.
4. Enter Play mode and confirm two grouped pulses per bar with six minor
   denominator markers.

The displayed BPM follows the selected tempo beat unit. At 120 BPM in 6/8 with
a dotted-quarter beat, the two grouped pulses occur at 120 per minute.

### 9. Run the EditMode tests

1. Open **Window > General > Test Runner**.
2. Select **EditMode**.
3. Choose **Run All**.

The tests verify:

- 4/4, 3/4, and 6/8 bar lengths
- 6/8 beat-group starts
- Tick/phase/angle conversion and wraparound
- Meter snap resolution across the loop boundary

## Sprint acceptance checklist

- [ ] Project compiles after Unity imports the files.
- [ ] All new EditMode tests pass.
- [ ] Ring is room-anchored and comfortably positioned.
- [ ] 4/4 produces four grouped triggers per bar.
- [ ] 3/4 produces three grouped triggers per bar.
- [ ] 6/8 with `[3, 3]` produces two grouped triggers and six minor markers.
- [ ] Playhead and audio restart the bar together.
- [ ] Ten minutes of 4/4 playback produce no obvious drift, missed hit, or
      duplicate hit at wraparound.

## Next sprint

Once this checklist passes:

1. Create one reusable `SampleInstance.prefab`.
2. Add `SampleInstanceView` and a programmatic view pool.
3. Render four model instances at the scheduled quarter-note positions.
4. Add Vive grab-to-move and meter-aware snapping.
5. Add clone and delete through reversible edit commands.

Microphone recording should follow that interaction slice so a recording bug
cannot be mistaken for a timing or placement problem.
