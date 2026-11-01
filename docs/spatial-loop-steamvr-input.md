# Spatial loop — SteamVR input

Spatial loop controls do **not** use Unity's legacy Input Manager or Input System
assets in the Project window. They use the **SteamVR Input** action manifest and
per-controller binding JSON under StreamingAssets.

## Where the files live

| Purpose | Path |
|--------|------|
| Action definitions (what exists) | `Assets/StreamingAssets/SteamVR/actions.json` |
| Vive wand bindings (hardware mapping) | `Assets/StreamingAssets/SteamVR/bindings_vive_controller.json` |
| Runtime reader | `Assets/Scripts/v2/Interaction/SpatialHandInputRouter.cs` |

`SpatialHandInputRouter` loads actions by **name** at runtime:

- `TriggerLeft` / `TriggerRight`
- `TrackpadLeft` / `TrackpadRight` (vector2 position)
- `TrackpadTouchLeft` / `TrackpadTouchRight`
- `TrackpadClickLeft` / `TrackpadClickRight`
- `LDpadUp` / `LDPadDn` (left trackpad sector on click)
- `RDPadUp` / `RDPadDn` (right trackpad sector on click)
- `MenuLeft` / `MenuRight`

Clone/delete on a **held loop arc** use the sector actions (click toward the top
or bottom of the trackpad). Effect XY uses trackpad touch + position.

## Unity setup (after changing JSON)

1. Open **Window > SteamVR Input**.
2. If prompted, use `Assets/StreamingAssets/SteamVR/actions.json`.
3. Click **Save and Generate** (updates C# under `Assets/SteamVR`).
4. Open **Vive Controller** bindings and confirm trackpad + d-pad sector outputs.
5. Save bindings.

Missing actions log a warning from `SpatialHandInputRouter` at Play mode start.

## Why nothing shows on Spatial Loop Root

Trackpad actions are not Inspector fields. SteamVR resolves them globally via
`SteamVR_Input.GetAction<T>("ActionName")` after Save and Generate. The JSON
manifest is the source of truth.

## Clone / delete checklist

1. **Grip a loop arc** on the ring (not a bank cube).
2. HUD should show `INSTANCE` and `TP↑ CLONE ↓ DEL`.
3. While holding grip, **click the trackpad toward top** (clone) or **bottom**
   (delete).

If sector clicks fail, open the SteamVR binding UI and verify north/south map to
`LDpadUp` / `LDPadDn` (or right-hand equivalents).
