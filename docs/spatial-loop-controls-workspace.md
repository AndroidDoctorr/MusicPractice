# Spatial Loop Controls and Workspace

## Control rule

Controls are hand-local and keep the same meaning across targets:

- A hand gripping a loop sample targets that instance.
- A hand gripping a bank token targets that sample asset.
- A hand gripping nothing targets the microphone.
- Physical workspace objects handle their own direct interactions.

This permits one hand to edit a held sample while the other records or processes
the microphone.

## Controller grammar

### Grip

Grip is exclusively for grabbing and releasing physical objects. Trigger must
not also invoke SteamVR `GrabPinch`.

### Trigger

- Empty hand: hold to record the microphone; release to finish.
- Held sample: press to audition; hold to gate longer material.
- Completed recording: an offered token follows the recording hand until grip
  claims it.

The source recording remains dry. The microphone effect state is monitored wet
and copied as non-destructive defaults onto the new asset.

### Trackpad touch

Touch/movement controls the selected XY effect pair for that hand's target.
Effects latch at the last position when touch ends.

### Trackpad click

- Left/right: previous/next effect pair.
- Up/down: previous/next preset for that pair.
- Center: bypass or enable the pair.

While **gripping a loop instance**, up/down on a trackpad click clone or delete
that instance instead (up = clone, down = delete). Left/right and center still
control effects on the held instance.

A quick click with little movement navigates. A touch-drag becomes an effect
gesture. Haptics and the hand HUD confirm pair and preset changes.

### Menu

- Tap: show or hide the hand HUD.
- Hold: open contextual commands such as save preset, clear effects,
  duplicate/delete sample, keyboard, undo, and redo.

Undo/redo remains available through history, but no longer occupies primary
trackpad controls.

## Hand HUD

Each hand can display a compact diagnostic above the controller:

- Current target: `MIC`, sample asset, or loop instance
- Current effect pair and preset
- XY values
- Recording state
- Pair bypass state

The first implementation uses world-space text. It is intentionally diagnostic
and can later be replaced by authored geometry.

## Workspace

The workspace is room-anchored inside the loop, roughly 0.5–0.65 m in front of
the calibrated center and 0.9–1.1 m above the floor.

### Pages (deck)

To avoid crowding one waist panel, the prototype uses three pages switched by
**BANK**, **PLACE**, and **ARP** tabs along the top edge. Poke a tab with a
controller or held token to change pages; only one page is visible at a time.
The active tab is highlighted.

Later options if this still feels busy:

- **Rotating carousel**: the whole deck spins on a vertical hinge so inactive
  pages sit off to the side instead of stacking in depth.
- **Fold-out wings**: bank stays docked left while place/arp panels hinge out
  only when grabbed.
- **Context pages**: empty hand opens effects/recording; holding a token opens
  placement; holding a loop arc opens edit tools.

### Left: sample bank

- Every successful recording is added automatically.
- Each token has stable sample color and a short name.
- Trigger auditions a token.
- Grip creates or carries a placeable sample.
- Removing all loop instances does not delete the asset.

### Center: capture and active pattern

- Displays recording state and the most recent sample.
- Holds the active pattern selector.
- House is the initial pattern for the prototype.

### Right: placement pads

The upper row contains pattern roles:

- Kick
- Open Hat
- Clap
- Closed Hat
- Snare

The lower row contains distributions:

- Once
- 2
- 4
- 8
- 16

Hovering a held token previews translucent ghosts on the ring. Releasing commits
the preview as one edit. Direct placement on the ring remains available.

Sample duration recommends a distribution without selecting it automatically:
long samples emphasize Once or 2, while short transients emphasize 4, 8, or 16.

### Arpeggiator page

The **ARP** page exposes rhythmic patterns (up, down, up-down, bounce, triplet
feel, offbeat). Dropping a sample on a pad commits the same preview/commit flow
as role/distribution pads: ghosts on the ring, one undo step on release.

**Data model recommendation:** treat an arpeggiated placement as one logical
**pattern group** in the document (`ArpGroupData`: source asset, pattern id,
start tick, per-step velocity/pitch offsets) while the scheduler still expands
it into individual loop instances for playback and ring meshes. That keeps grab,
delete, and undo sane (one object on the ring or one grouped arc) without
fighting the existing per-hit voice pool. The current prototype expands to
multiple instances immediately for speed; grouping is the next refinement.

### Effects and keyboard

Saved effect-preset tokens live above or to the right of the placement pads. A
contextual keyboard folds from the lower workspace edge when a tonal sample is
targeted.

## Pattern representation

Legacy pattern arrays are imported into role points:

```text
PatternDefinition
  name
  timeSignature
  points[]

PatternPoint
  role
  tick
  velocity
```

The first conversion uses the House preset from
`SequencerPattern.cs`. Roles are no longer tied permanently to channel indexes
or a 16-element loop.

## Effect pairs

An effect pair maps normalized trackpad XY to two related controls:

- Filter: X low-pass ↔ neutral ↔ high-pass; Y resonance
- Space: X delay; Y reverb
- Modulation: X flange/chorus; Y phase
- Texture: X distortion; Y bit/sample-rate reduction
- Rhythm: X repeat division; Y gate/feedback

The prototype implements Filter and Space. Pair definitions carry parameter
ranges and a two-color visual identity. Sample hue remains source identity; a
secondary rim or HUD indicates effects.

## Presets

Factory presets are authored data. User presets are named snapshots containing:

- Pair ID
- X and Y
- Bypass
- Secondary effect parameters

Applying a preset copies its values. Existing songs do not change if a preset is
later edited.

## Prototype sequence

1. Semantic trigger, trackpad touch/click, menu, and hand target diagnostics
2. Empty-hand microphone capture and offered sample
3. Runtime sample bank and placement workspace
4. Pattern/distribution hover previews and commits
5. Filter and Space effects with preset navigation and saving
6. Contextual keyboard and authored Blender detail meshes

## Unity setup for the prototype

See **`docs/spatial-loop-steamvr-input.md`** for where trackpad actions live and
how to regenerate SteamVR Input after editing JSON.

1. Let Unity import the new v2 scripts.
2. Open **Window > SteamVR Input**.
3. Load the action manifest if prompted, then choose **Save and Generate** so
   `TrackpadTouchLeft/Right`, `TrackpadClickLeft/Right`, and pad sector actions
   (`LDpadUp`, `LDPadDn`, `RDPadUp`, `RDPadDn`) are generated.
4. Open the Vive controller binding UI and confirm:
   - Grip → `GrabGrip`
   - Trigger pull → `TriggerLeft` or `TriggerRight`
   - Trackpad position/touch/click → the matching hand actions
   - Application menu → `MenuLeft` or `MenuRight`
   - Trigger is not bound to `GrabPinch`
5. Keep `SpatialLoopController` on `Spatial Loop Root`. It adds the performance,
   capture, effects, input, and workspace prototype components at runtime.
6. Enter Play mode. The workspace and two hand HUDs are generated at runtime.
7. If microphone monitoring causes feedback, disable **Monitor While Recording**
   on `SampleCaptureService`; the dry recording and effect snapshot still work.
