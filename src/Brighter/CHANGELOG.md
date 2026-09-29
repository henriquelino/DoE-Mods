# Brighter changelog

## 0.4.0

A point light falls off with the square of the distance, so a high setting blew out the near
wall into the game's bloom while the far wall stayed dim. `HeadlampRange` goes from 14 m to
32 m, which flattens the curve: the near-to-far gap at 1 m and 10 m drops from about twelve
times to under three.

`HeadlampMode` is new. Set it to `flood` and the lamp becomes a directional light with no
falloff at all, so near and far read the same and nothing blows out. It reads as a lit room
rather than a carried torch.

## 0.3.0

Off now means off, in three places. The tap suspended the headlamp alone, so the fog removal
and the ambient stayed on. Suspending also only stopped new work: the torch copies already
made kept burning and the raised lights kept their multiplier. The sweep now records what each
light had before it touched it, and suspending hides the copies and hands the old values back.

The knob was geared far too long: 90 degrees per unit against a ceiling of 6 took one and a
half wrist rotations end to end. It now runs 0.4 to 3 across 120 degrees, one comfortable
turn. Turning the knob while the mod is off switches it back on.

## 0.2.0

The forehead switch. A hand at your head plus the trigger toggles the headlamp; holding and
turning your wrist works it like a knob, with haptic detents. Input comes from the game's own
`XRInput`, so it works in the headset with the desktop window unfocused.

Game 1.3 reads input through the Input System package, so `UnityEngine.Input` throws. The
desktop keys now report that once and switch themselves off instead of warning every frame.

Confirmed in game: the rooms do get brighter, and the headlamp reads like a carried torch.

## 0.1.0

First build. Five levers against the baked lighting, each its own setting: fog off, flat
ambient, a headlamp that follows your head, a realtime copy of every torch in the scene, and
an intensity boost on the lights already there. Hotkeys B, `[`, `]`, L and K.

Untested in game. Which levers a room answers to is the open question this build exists to
answer.
