using MelonLoader;

namespace Brighter
{
    /// <summary>
    /// Settings in UserData/MelonPreferences.cfg: [Brighter] for what a player changes,
    /// [Brighter_Dev] for diagnostics. Everything is read live, so editing the file and
    /// pressing the reload key applies without a restart.
    ///
    /// The game bakes its lighting (the live shaders carry Bakery keywords), so no runtime
    /// setting repaints a wall. Every lever here is additive: new realtime light, or less fog.
    /// Each one is separate because which of them a given room answers to is not known yet.
    /// </summary>
    public static class ModConfig
    {
        public static MelonPreferences_Category Main;
        public static MelonPreferences_Category Dev;

        // ---- [Brighter] ------------------------------------------------------------------
        public static MelonPreferences_Entry<bool> Enabled;

        /// <summary>Multiplies the scene's own fog density. 0 turns fog off; 1 leaves it alone.</summary>
        public static MelonPreferences_Entry<float> FogDensityScale;

        /// <summary>Flat ambient light added everywhere, 0 to 1. 0 leaves the scene's own ambient alone. Lifts players, monsters and dropped items; baked walls may ignore it.</summary>
        public static MelonPreferences_Entry<float> AmbientLevel;

        /// <summary>Brightness of the light that follows your head. 0 turns it off. This is the lever that works whatever the bake did.</summary>
        public static MelonPreferences_Entry<float> HeadlampIntensity;
        /// <summary>How far the headlamp reaches, metres.</summary>
        public static MelonPreferences_Entry<float> HeadlampRange;
        /// <summary>Headlamp colour, hex RGB. Default is a warm white.</summary>
        public static MelonPreferences_Entry<string> HeadlampColor;
        /// <summary>Let the headlamp cast shadows. Costs frames, and a light at your eyes casts almost none you can see.</summary>
        public static MelonPreferences_Entry<bool> HeadlampShadows;
        /// <summary>Metres the headlamp sits ahead of your eyes, so it does not light the inside of your own head.</summary>
        public static MelonPreferences_Entry<float> HeadlampForwardOffset;

        /// <summary>Hand to your forehead plus the trigger switches the headlamp. Hold and turn your wrist to dim it.</summary>
        public static MelonPreferences_Entry<bool> ForeheadSwitchEnabled;
        /// <summary>How close your hand must come to your head for the gesture to count, metres.</summary>
        public static MelonPreferences_Entry<float> ForeheadReachMeters;
        /// <summary>Ignore the gesture while the hand grips something. A weapon swing is a raised hand and a pulled trigger too.</summary>
        public static MelonPreferences_Entry<bool> ForeheadNeedsEmptyHand;
        /// <summary>A trigger pull shorter than this, with no turn, is a tap: it switches the lamp on or off.</summary>
        public static MelonPreferences_Entry<float> ForeheadTapSeconds;
        /// <summary>Turn your wrist this far before the knob takes over, degrees. Stops a tap from nudging the brightness.</summary>
        public static MelonPreferences_Entry<float> ForeheadTwistDeadzoneDegrees;
        /// <summary>Degrees of wrist turn for one unit of brightness. Larger is a finer knob.</summary>
        public static MelonPreferences_Entry<float> ForeheadDegreesPerUnit;
        /// <summary>The knob never goes past this brightness.</summary>
        public static MelonPreferences_Entry<float> ForeheadMaxIntensity;
        /// <summary>Buzz the controller on each step of the knob and on each toggle.</summary>
        public static MelonPreferences_Entry<bool> ForeheadHaptics;

        /// <summary>Multiplies the intensity of every light already in the scene. 1 leaves them alone. A light the level baked contributes nothing at runtime, so this does nothing for those.</summary>
        public static MelonPreferences_Entry<float> SceneLightBoost;
        /// <summary>Multiplies the reach of every light already in the scene. 1 leaves them alone.</summary>
        public static MelonPreferences_Entry<float> SceneLightRangeScale;

        /// <summary>Put a new realtime light on top of every torch and lamp in the scene. This is how a baked room gets brighter: the copy is realtime whatever the original was.</summary>
        public static MelonPreferences_Entry<bool> CloneSceneLights;
        /// <summary>Brightness of each copied light.</summary>
        public static MelonPreferences_Entry<float> CloneIntensity;
        /// <summary>How far each copied light reaches, metres. It never reaches less than the light it copies.</summary>
        public static MelonPreferences_Entry<float> CloneRange;
        /// <summary>Stop after this many copies in one scene, so a big dungeon cannot cost all the frames.</summary>
        public static MelonPreferences_Entry<int> MaxClonedLights;

        /// <summary>How many lights the renderer draws per-pixel instead of folding into vertex light. 0 leaves the game's setting. Raising it makes added lights count, and costs frames.</summary>
        public static MelonPreferences_Entry<int> PixelLightCount;

        // ---- [Brighter_Dev] --------------------------------------------------------------
        public static MelonPreferences_Entry<bool> HotkeysEnabled;
        public static MelonPreferences_Entry<bool> VerboseLogging;
        /// <summary>Seconds after a scene loads before the first sweep, so the dungeon generator finishes first.</summary>
        public static MelonPreferences_Entry<float> SweepDelaySeconds;
        /// <summary>Seconds between sweeps. Rooms that spawn later get their lights on the next one.</summary>
        public static MelonPreferences_Entry<float> SweepIntervalSeconds;
        /// <summary>Switch on lights the level left switched off. A room may keep its light off on purpose, so this can undo a puzzle or a scripted moment.</summary>
        public static MelonPreferences_Entry<bool> EnableDisabledLights;

        public static void Load()
        {
            Main = MelonPreferences.CreateCategory("Brighter");
            Enabled = Main.CreateEntry("Enabled", true);

            FogDensityScale = Main.CreateEntry("FogDensityScale", 0f, description: "Multiplies the scene's fog density. 0 = no fog, 1 = untouched.");
            AmbientLevel = Main.CreateEntry("AmbientLevel", 0.25f, description: "Flat ambient light everywhere, 0 to 1. 0 = leave the scene alone.");

            HeadlampIntensity = Main.CreateEntry("HeadlampIntensity", 1.5f, description: "Brightness of the light that follows your head. 0 = off.");
            HeadlampRange = Main.CreateEntry("HeadlampRange", 14f, description: "How far the headlamp reaches, metres.");
            HeadlampColor = Main.CreateEntry("HeadlampColor", "FFF0D8", description: "Headlamp colour, hex RGB.");
            HeadlampShadows = Main.CreateEntry("HeadlampShadows", false, description: "Let the headlamp cast shadows. Costs frames.");
            HeadlampForwardOffset = Main.CreateEntry("HeadlampForwardOffset", 0.25f, description: "Metres ahead of your eyes the headlamp sits.");

            ForeheadSwitchEnabled = Main.CreateEntry("ForeheadSwitchEnabled", true,
                description: "Hand to your forehead plus the trigger switches the headlamp. Hold and turn your wrist to dim it.");
            ForeheadReachMeters = Main.CreateEntry("ForeheadReachMeters", 0.25f, description: "How close your hand must come to your head, metres.");
            ForeheadNeedsEmptyHand = Main.CreateEntry("ForeheadNeedsEmptyHand", true, description: "Ignore the gesture while that hand grips something, so a weapon swing cannot trip it.");
            ForeheadTapSeconds = Main.CreateEntry("ForeheadTapSeconds", 0.45f, description: "A pull shorter than this, with no turn, toggles the lamp.");
            ForeheadTwistDeadzoneDegrees = Main.CreateEntry("ForeheadTwistDeadzoneDegrees", 15f, description: "Turn this far before the knob takes over.");
            ForeheadDegreesPerUnit = Main.CreateEntry("ForeheadDegreesPerUnit", 90f, description: "Degrees of wrist turn per unit of brightness. Larger is finer.");
            ForeheadMaxIntensity = Main.CreateEntry("ForeheadMaxIntensity", 6f);
            ForeheadHaptics = Main.CreateEntry("ForeheadHaptics", true, description: "Buzz the controller on each step and on each toggle.");

            SceneLightBoost = Main.CreateEntry("SceneLightBoost", 1.6f, description: "Multiplies every existing light's intensity. 1 = untouched.");
            SceneLightRangeScale = Main.CreateEntry("SceneLightRangeScale", 1.3f, description: "Multiplies every existing light's reach. 1 = untouched.");

            CloneSceneLights = Main.CreateEntry("CloneSceneLights", true, description: "Put a new realtime light on every torch and lamp, so a baked room lights up.");
            CloneIntensity = Main.CreateEntry("CloneIntensity", 1.2f);
            CloneRange = Main.CreateEntry("CloneRange", 9f, description: "Metres. Never less than the light it copies.");
            MaxClonedLights = Main.CreateEntry("MaxClonedLights", 150, description: "Copies stop at this many per scene.");

            PixelLightCount = Main.CreateEntry("PixelLightCount", 8, description: "Lights drawn per-pixel. 0 = leave the game's setting alone. Lower this first if frames drop.");

            Dev = MelonPreferences.CreateCategory("Brighter_Dev");
            HotkeysEnabled = Dev.CreateEntry("HotkeysEnabled", true,
                description: "B = everything on or off; ] and [ = headlamp brighter or dimmer; L = sweep the scene lights again; K = reload settings.");
            VerboseLogging = Dev.CreateEntry("VerboseLogging", false, description: "Log every sweep and every setting the mod writes.");
            SweepDelaySeconds = Dev.CreateEntry("SweepDelaySeconds", 4f, description: "Seconds after a scene loads before the first sweep.");
            SweepIntervalSeconds = Dev.CreateEntry("SweepIntervalSeconds", 5f, description: "Seconds between sweeps.");
            EnableDisabledLights = Dev.CreateEntry("EnableDisabledLights", false,
                description: "Switch on lights the level left off. A dark room may be dark on purpose, so this can break a puzzle.");
        }
    }
}
