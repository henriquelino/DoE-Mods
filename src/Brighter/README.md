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
| `HeadlampIntensity` | A realtime light that follows your head. | Whatever the bake did. This is the lever to try first. |
| `CloneSceneLights` | A new realtime light on every torch and lamp in the scene. | Whatever the bake did, if the world shaders accept realtime lights at all. |
| `SceneLightBoost` | Raises the intensity of the lights already there. | Only for lights the level left realtime. Does nothing for baked ones. |

`PixelLightCount` decides how many lights the renderer draws per-pixel instead of folding
into vertex light. Added lights do not count for much below it. It costs frames; lower it
first if the headset drops below 90.

## The three lamp modes

`HeadlampMode` picks how the light behaves. A point light falls off with the square of the
distance, which is why a bright setting blows out the wall at arm's length and still leaves
the far wall dim.

- **`soft`** (the default) is the middle ground: a point light stood `HeadlampSetbackMeters`
  behind your head, with its range extended by the same amount. From 18 m away, a surface at
  19 m and one at 28 m get close to the same light, so the near field stops blowing out, and
  the reach still ends where `HeadlampRange` says. The knob is scaled back up so the setback
  does not cost you brightness.
- **`point`** is the lamp you carry, with the full squared falloff. Bright up close, dark
  across the room.
- **`flood`** is a directional light: no position, no falloff, no range. It lights the whole
  scene, walls included. `HeadlampRange` and `HeadlampSetbackMeters` do nothing here. The knob
  is scaled by `HeadlampFloodScale` (0.2), because a directional light needs far less.

`HeadlampPitchDegrees` (20) aims the lamp that many degrees below where you look, so a level
gaze still lights the floor ahead. Negative aims up. Flood mode only, because a point light is
omnidirectional and has no direction to aim.

`HeadlampHeightMeters` (2.5) lifts the lamp above your head, which puts more light on the
floor, like a lamp on the ceiling instead of one at your eyes. Point and soft modes only.

The setback and the height are measured along the direction you *face*, not the direction you
*look*, so tilting your head does not sling the light around.

## Why the distance can look darker than vanilla

The game tonemaps the frame. A bright pool of light around you raises the average brightness,
the exposure comes down to compensate, and everything outside the pool is crushed darker than
it was before the mod. Adding more local light makes the distance worse.

The cure is even light rather than more light:

- Raise `AmbientLevel`. Ambient is flat, so it lifts the far end of the room as much as your
  feet and costs the exposure nothing.
- Lower `HeadlampIntensity`, and raise `HeadlampSetbackMeters` so what remains is spread out.
- Keep `HeadlampRange` well past what you want to see. The light ends hard at that distance, so
  a short range is its own black wall.

The torch copies have the full squared falloff too. If they blow out up close, raise
`CloneRange` (9 m) or lower `CloneIntensity`.

## If none of them work

Then the world shaders are lightmap-only and ignore realtime lights. The remaining route is
a post-process exposure lift on the camera, which is a bigger job and not in this mod.

## It starts off

Every session begins with the mod switched off, so the game looks exactly as it shipped until
you ask for light. Tap your forehead to switch on. Set `StartOff` to false to have the light
on from the first frame.

## In the headset: the forehead switch

The lamp is worn, so it is switched where a real one would be.

- **Tap.** Put a hand to your forehead and pull the trigger. Everything goes off, and the room
  is exactly as the game shipped it: no headlamp, no torch copies, the game's own fog and
  ambient. Tap again to bring it back. This is the switch to reach for when you want to see
  what the mod is doing.
- **Knob.** Keep the trigger pulled and turn your wrist. The headlamp dims and brightens, from
  `ForeheadMinIntensity` to `ForeheadMaxIntensity` across `ForeheadTurnDegrees` of turn, 120
  degrees by default, with a buzz on each quarter step. The knob never reaches zero, because a
  tap is how you turn things off. Turning the knob while off switches back on.

The hand has to be in *front* of your head, not merely near it, because reaching over your
shoulder for an arrow puts a hand the same distance away. `ForeheadMinForwardMeters` (0.02) and
`ForeheadMinHeightMeters` (-0.08) set that box. Raise the first if a quiver reach still trips
it; lower it if the gesture has become hard to hit.

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
