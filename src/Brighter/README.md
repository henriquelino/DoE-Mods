# Brighter

Lights the dark rooms of Dungeons of Eternity.

Client-side only. It sends nothing, it has no gate, and a friend who runs nothing sees no
change. It only changes what your own eyes see.

## The problem it works around

The game bakes its lighting. `docs/GAME-INTERNALS.md` records the shader keyword
`BAKERY_VOLUME` on the live character material, and Bakery is a baked lightmapper. A wall
carries its light in a texture that was painted before the game shipped, so no runtime
setting repaints it. Raising the ambient light, the obvious first try, lifts the players and
the monsters and leaves the walls exactly as dark as they were.

Every lever here is additive instead. A light this mod creates at runtime is realtime by
construction, whatever the level baked.

## The five levers

Each one is its own setting in `[Brighter]`, because which of them a room answers to depends
on its shaders and that is not known yet. Turn them on one at a time and keep what works.

| Setting | What it does | Works when |
|---|---|---|
| `FogDensityScale` | 0 removes fog. | Always. In a dark dungeon the haze often costs more sight than the missing light. |
| `AmbientLevel` | Flat ambient light everywhere. | Lifts players, monsters, dropped items. Baked walls probably ignore it. |
| `HeadlampIntensity` | A realtime point light that follows your head. | Whatever the bake did. This is the lever to try first. |
| `CloneSceneLights` | A new realtime light on every torch and lamp in the scene. | Whatever the bake did, if the world shaders accept realtime lights at all. |
| `SceneLightBoost` | Raises the intensity of the lights already there. | Only for lights the level left realtime. Does nothing for baked ones. |

`PixelLightCount` decides how many lights the renderer draws per-pixel instead of folding
into vertex light. Added lights do not count for much below it. It costs frames; lower it
first if the headset drops below 90.

## If none of them work

Then the world shaders are lightmap-only and ignore realtime lights. The remaining route is
a post-process exposure lift on the camera, which is a bigger job and not in this mod.

## In the headset: the forehead switch

The lamp is worn, so it is switched where a real one would be.

- **Tap.** Put a hand to your forehead and pull the trigger. The lamp goes on or off. Turning
  it off remembers the brightness, so the next tap brings it back at the same setting.
- **Knob.** Keep the trigger pulled and turn your wrist. The lamp dims and brightens with the
  turn, 90 degrees per unit by default, with a buzz on each quarter step.

By default the gesture is ignored while that hand grips something, because a weapon swing is
also a raised hand and a pulled trigger. Set `ForeheadNeedsEmptyHand` to false if you would
rather have it always live.

Input comes from the game's own `XRInput`, the same source VisualCues and StayPutVR read.

## Hotkeys at the desk

Game 1.3 reads input through the Input System package, so `UnityEngine.Input` throws the first
time a mod reads it. This mod says so once and then leaves the desktop keys alone for the
session. If a future game build brings them back, these are the bindings.

The game window must have focus, which in VR means clicking it once.

- `B` everything on or off, with the scene's own values handed back.
- `]` and `[` headlamp brighter or dimmer, in steps of 0.25.
- `L` sweep the scene lights again.
- `K` reload settings from `MelonPreferences.cfg`.

## Things that bite

- **MelonLoader keeps every preference once written.** Changing a default in code never
  reaches a machine that ran an older build. Diff `MelonPreferences.cfg` against the
  `CreateEntry` defaults before you believe a default.
- **The game rewrites `MelonPreferences.cfg` on quit.** Edit it with the game closed.
- **`EnableDisabledLights` is off by default.** A dark room may be dark on purpose. Switching
  its light on can undo a puzzle or a scripted moment.
- **The sweep runs on a timer**, four seconds after a scene loads and every five after that,
  because the dungeon generator builds rooms after the scene is up. A room you walk into
  gets its lights on the next sweep, not the moment you enter.
