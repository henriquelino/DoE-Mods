# Brighter changelog

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
