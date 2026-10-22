# Brighter: open investigation

## Black screen on entering a lobby

Reported 2026-10-03 on game 1.3, Brighter 0.9.0. The game shows a black screen and never
loads. Sometimes it crashes instead. Not every time.

### What the log proves

From `MelonLoader\Latest.log`, session starting 18:18:36:

- `1 Mod loaded.` Brighter is the only mod installed. The cause is Brighter, MelonLoader, or
  the game.
- The log ends clean at 18:18:46 with the keyboard warning and nothing after. Every path in
  this mod sits inside a try/catch that logs, so a managed exception would have printed a
  `[Brighter]` warning. There is none. This is a native crash or a GPU stall, not C# falling
  over.
- The log cannot say where the game got to, because `VerboseLogging` was false and the mod
  logs nothing on a scene change.

### Suspects, none confirmed

1. **Load, not logic.** The mod can create up to `MaxClonedLights` (150) realtime lights set
   to `ForcePixel`, and raises `PixelLightCount` to 8. In a VR lobby at 90 fps that is a lot
   of extra shading arriving at once. A GPU stalled long enough is reset by the driver, which
   looks like a black screen that never recovers.
2. **The sweep scans everything.** `FindObjectsOfType(Light, true)` includes inactive objects,
   every `SweepIntervalSeconds` (5). A lobby is the worst case: GAME-INTERNALS.md records that
   each player instantiates the whole cosmetic catalogue inactive, 462 renderers per person,
   so the scan grows with the party.

### A real defect found while reading the code, not yet fixed

`Lighting.ApplyEnvironment` runs every frame and writes `RenderSettings.ambientLight`,
`ambientMode`, `ambientIntensity` and `QualitySettings.pixelLightCount` unconditionally. It
does this even while the mod is switched off, because `Restore()` makes the same writes.
Assigning `ambientLight` makes Unity recompute the ambient probe, so that happens 90 times a
second for no reason.

This is waste for certain. Whether it causes the black screen is unknown. The fix is to write
each value only when it differs from what is already set, about fifteen lines in
/home/hlino/gits/DoE-Mods/src/Brighter/Lighting.cs.

### Next step

Set `VerboseLogging = true` in `UserData\MelonPreferences.cfg` with the game closed, reproduce
the black screen, and read the tail of the log. Every scene load then prints the scene name
and its lighting, and every sweep prints how many lights it touched. That names the last thing
that happened before the freeze.

### Settings that test the load theory without a code change

    CloneSceneLights = false
    PixelLightCount = 0
    SweepIntervalSeconds = 30

That keeps the headlamp, the fog and the ambient, and removes everything that scales with
scene size. If lobbies load with those three set, the cause is load.

## Desktop keys do nothing

Not a bug to fix. Game 1.3 reads input through the Input System package, so `UnityEngine.Input`
throws on the first read. The mod says so once and switches the keys off for the session. Every
other mod in this repo makes the same call (CustomAvatars Core.cs:230, VisualCues Core.cs:62,
PartyHealth Core.cs:50), so their F-keys are presumably dead on 1.3 too. The forehead gesture
reads `XRInput` instead and is unaffected.
