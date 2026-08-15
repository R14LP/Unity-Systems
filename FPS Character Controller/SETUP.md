# FPS Character Controller — Setup Guide

A physics-based first-person character controller for Unity, featuring sprint, slope handling, coyote time, air strafing, 8-directional locomotion (via a nested Blend Tree), and an item pickup/throw system.

## Requirements

- Unity 6 (tested on 6000.3.6f1)
- A Humanoid-rigged character model (e.g. from Mixamo) with:
  - Idle
  - Walk: forward, backward, left strafe, right strafe, and the 4 diagonals (forward-left, forward-right, back-left, back-right)
  - Run/Sprint: same 8 directions as Walk
  - Jump Begin, Jump Fall (looping), Jump Land

## 1. Layers & Tags

Before setting anything up, create these first:

- **Layer:** `Player` — for the player's own colliders, so raycasts can exclude self-collision.
- **Layer:** `HeldItem` — used by the item camera's culling mask so held items render correctly.
- **Tag:** `Item` — applied to any object the player can pick up.

## 2. Player Hierarchy

Build the following hierarchy:

```
Player                  (empty GameObject, Layer: Player)
├── PlayerModel          (your character FBX, dragged in as a child)
├── Orientation           (empty)
└── CameraPos             (empty)

CameraHolder             (empty GameObject)
└── PlayerCam             (Camera)
    ├── HoldPoint          (empty)
    └── ItemCamera         (Camera)

Item                    (any pickupable object — Tag: Item)

Canvas
└── ItemNameText          (TextMeshPro - UGUI)
```

## 3. Player Object

**Rigidbody**
- Freeze Rotation: X, Y, Z all checked
- Freeze Position: none checked
- Collision Detection: Continuous

**Capsule Collider**
- Add one on `Player` (or on `PlayerModel` if that's where you've placed it — just make sure it lives somewhere in the hierarchy Rigidbody can act on). Size it to match your character's actual **animated idle pose**, not the T-pose bind pose — these can differ slightly after Humanoid retargeting. Pause the game while Idle is playing and adjust Height/Center visually against the mesh, then re-enter the same values in Edit mode (Play Mode changes don't persist).

**Animator**
- Controller: your Animator Controller (see Section 7)
- Avatar: your character model's Avatar
- Apply Root Motion: **unchecked**
- Animate Physics: unchecked
- Update Mode: Normal
- Culling Mode: Always Animate

**Scripts (all on the Player object):**

| Script | Field | Assign |
|---|---|---|
| PlayerMovement | Player Model | `PlayerModel` |
| | Orientation | `Orientation` |
| PlayerAnimator | Animator | Player's own Animator |
| | Player Movement | Player's own PlayerMovement |
| ItemInteraction | Hold Point | `HoldPoint` |
| | Highlight Material | a new Material (set its color/emission to whatever highlight style you want) |
| | Item Name Text | `ItemNameText` |
| | Player Cam | `PlayerCam`'s PlayerCam script |
| | Player Camera | `PlayerCam`'s Transform |

## 4. Camera Setup

**CameraHolder** — empty object at world origin.

**PlayerCam** (child of CameraHolder)
- `PlayerCam` script:
  - Orientation → `Orientation`
  - Player Body → `Player`
- Camera component:
  - Clipping Planes Near: increase from the default (try 0.1–0.3) until you no longer see inside the character's head
  - Stack: add `ItemCamera` as an Overlay camera

**HoldPoint** (child of PlayerCam) — position it in front of the camera to match where held items should sit.

**ItemCamera** (child of PlayerCam)
- Render Type: Overlay
- Culling Mask: `HeldItem` only
- Clear Depth: checked

**CameraPos** (child of Player)
- `HeadCameraFollow` script:
  - Animator → Player's Animator
  - Camera Holder → `CameraHolder`
  - Orientation → `Orientation`
  - Offset: tune Y (height) and Z (forward distance) until the camera sits at eye level without clipping

> This script positions the camera at the character's head bone every frame (for natural head-bob), while rotation is driven entirely by `PlayerCam`'s mouse look — the head bone's own animated rotation never affects the camera.

## 5. Item System

Any object the player can pick up needs:
- Tag: `Item`
- A Collider
- A Rigidbody

`ItemInteraction` handles raycasting to find/highlight items, picking up (E key), rotating while held (right-click + drag), and throwing (left-click).

## 6. Animation Import Settings

For **every** animation clip you use (Idle, all 8 Walk directions, all 8 Run directions, Jump Begin, Jump Fall, Jump Land):

1. Select the clip's FBX file in the Project window.
2. Open the **Rig** tab:
   - Animation Type: `Humanoid`
   - Avatar Definition: `Create From This Model`
   - Click **Apply**
3. Open the **Animation** tab:
   - Loop Time: checked — for anything meant to repeat (Idle, Walk, Run, Jump Fall). **Leave unchecked** for `Jump Begin` and `Jump Land` — those are one-shot clips.
   - Root Transform Rotation → Bake Into Pose: checked, Based Upon: `Original`
   - Root Transform Position (Y) → Bake Into Pose: checked, Based Upon (at Start): `Original`
   - Click **Apply**

    <img width="441" height="312" alt="rig-settings" src="https://github.com/user-attachments/assets/dc4451f0-bab8-41c8-b57c-fa075b01a8fd" />

    <img width="441" height="954" alt="animation-loop-settings" src="https://github.com/user-attachments/assets/261d46cf-0ca8-4104-99b4-c909420fa0f6" />

    you don't need one per clip, the settings are identical for all of them (aside from the Loop Time exception above).

## 7. Animator Controller Setup

Create an Animator Controller and add these **Parameters**:
- `MoveSpeed` (Float)
- `VelocityX` (Float)
- `VelocityZ` (Float)
- `Grounded` (Bool)
- `Jump` (Trigger)

<img width="148" height="179" alt="animator-parameters" src="https://github.com/user-attachments/assets/2f1b096b-d5b9-4c1b-96d9-b85ddb3a1b31" />

### 7.1 Locomotion Blend Tree

1. Right-click empty canvas → **Create State → From New Blend Tree**. Set it as the layer's default state.
2. Double-click to enter it. In the Inspector: Blend Type `1D`, Parameter `MoveSpeed`.
3. In the Motion list, add two entries via **+ → Blend Tree** (not a direct animation clip). Name one `walk`, the other `run`.
4. Set Threshold: `walk` = **3**, `run` = **7** (match these to your actual walk/sprint speeds in `PlayerMovement`).

<img width="441" height="300" alt="blend-tree-outer" src="https://github.com/user-attachments/assets/08684e47-389b-4a2e-892c-eaae109f6280" />

5. Double-click into `walk`. Set Blend Type `2D Freeform Directional`, Parameters X = `VelocityX`, Y = `VelocityZ`.
6. Add 9 motions (Idle + all 8 directions) with these exact positions:

| Clip | Pos X | Pos Y |
|---|---|---|
| Idle | 0 | 0 |
| Left | -1 | 0 |
| Right | 1 | 0 |
| Backward | 0 | -1 |
| Forward | 0 | 1 |
| Forward-Left (diagonal) | -0.7 | 0.7 |
| Back-Left (diagonal) | -0.7 | -0.7 |
| Forward-Right (diagonal) | 0.7 | 0.7 |
| Back-Right (diagonal) | 0.7 | -0.7 |

7. Go back up a level, double-click into `run`, and repeat the exact same setup using your run/sprint clips at the same 9 positions.

<img width="442" height="955" alt="blend-tree-run-2d" src="https://github.com/user-attachments/assets/1add9ec4-c932-4c11-b929-87ea1b256ebb" />

<img width="441" height="956" alt="blend-tree-walk-2d" src="https://github.com/user-attachments/assets/67a70284-13a0-4ccf-9e5e-e74fb535de9c" />

> Using real diagonal-capture clips (not just blended cardinals) at the ±0.7/±0.7 positions is what fixes foot-crossing on diagonal movement — if you only have 4 cardinal clips, the blend tree will interpolate between them instead, which is more prone to visible foot artifacts.

### 7.2 Jump / Fall / Landing States

Add three states alongside the Blend Tree state: `JumpUp` (Motion: your Jump-Begin clip), `Falling` (Motion: your looping Fall clip), `Landing` (Motion: your Jump-Land clip).

Set up these 6 transitions exactly:

| From → To | Has Exit Time | Exit Time | Condition |
|---|---|---|---|
| Any State → JumpUp | Off | — | `Jump` (trigger) |
| JumpUp → Landing | Off | — | `Grounded` = true |
| JumpUp → Falling | **On** | ~0.9 | *(none)* |
| Blend Tree → Falling | Off | — | `Grounded` = false |
| Falling → Landing | Off | — | `Grounded` = true |
| Landing → Blend Tree | **On** | ~0.85–0.9 | *(none)* |

**Why two ways out of JumpUp?** If you land quickly (a short hop), `JumpUp → Landing` cuts straight to the landing pose without waiting for the jump-start clip to finish. If you're still airborne once that clip nears its end, `JumpUp → Falling` (via Exit Time) takes over instead, so you get a proper looping fall animation rather than freezing on the last frame of the jump-start clip.

<img width="1325" height="658" alt="animator-state-graph" src="https://github.com/user-attachments/assets/766db619-7906-401d-80d6-6bd9c5dc8a3e" />

<img width="440" height="954" alt="jumpup-to-falling" src="https://github.com/user-attachments/assets/ab1798eb-30c7-456b-abfb-4fd4ca7f59bd" />

<img width="445" height="955" alt="jumpup-to-landing" src="https://github.com/user-attachments/assets/775f3bf2-141e-4419-92e1-6a312c1fa690" />

*These two transitions are the ones people mix up most — everything else follows the same "Exit Time off + Grounded condition" or "Exit Time on + no condition" pattern shown in the table above, so extra screenshots of the rest are optional.*

## 8. Testing Checklist

- [ ] Walk / sprint (Shift) transitions smoothly, matching actual physics speed
- [ ] All 8 directions blend without visible foot-crossing
- [ ] Jump works from a dead stop, mid-sprint, and just after walking off a ledge (coyote time)
- [ ] A short hop lands cleanly (JumpUp → Landing); a long fall shows the Falling loop before landing
- [ ] Character doesn't slide on flat ground; slides uncontrollably above the max slope angle; walks/climbs normally below it
- [ ] Camera doesn't clip into the character's head at any angle
- [ ] Item highlight, pickup, rotate, and throw all work as expected
