using System;
using Brighter.Controls;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(Brighter.Core), "Brighter", "0.9.0", "henriquelino")]
[assembly: MelonGame("Othergate LLC", "Dungeons of Eternity")]

namespace Brighter
{
    /// <summary>
    /// Brighter: lights the dark rooms.
    ///
    /// The game bakes its lighting, so no runtime setting repaints a wall. Every lever in
    /// <see cref="Lighting"/> is additive instead: a realtime light that follows your head,
    /// a realtime copy of every torch, more ambient, less fog. They are separate settings
    /// because which ones a room answers to depends on its shaders. Turn them on one at a
    /// time and keep the ones that work.
    ///
    /// Client-side only. Nothing is sent, nothing is gated, friends running nothing see no change.
    /// </summary>
    public class Core : MelonMod
    {
        public const string Version = "0.9.0";

        public static Core Instance { get; private set; }
        public static MelonLogger.Instance Log => Instance.LoggerInstance;

        /// <summary>
        /// Game 1.3 reads input through the Input System package, so <c>UnityEngine.Input</c> throws
        /// on the first read. One warning, then the desktop keys go quiet: the gesture and the
        /// settings file are the ways in.
        /// </summary>
        private bool _keyboardDead;

        public override void OnInitializeMelon()
        {
            Instance = this;
            ModConfig.Load();
            try { MelonPreferences.Save(); }
            catch (Exception e) { LoggerInstance.Warning($"Could not write MelonPreferences.cfg: {e.Message}"); }

            Lighting.Suspended = ModConfig.StartOff.Value;

            LoggerInstance.Msg($"Brighter {Version} — five ways to light a dark room, each its own setting in [Brighter].");
            if (Lighting.Suspended) LoggerInstance.Msg("Starting off: the game's own lighting. Tap your forehead to switch on (StartOff = false to start lit).");
            LoggerInstance.Msg("In the headset: hand to your forehead and pull the trigger to switch the mod on or off; hold and turn your wrist to dim the headlamp.");
            LoggerInstance.Msg("At the desk (window focused): B = all on/off, ] and [ = headlamp brighter/dimmer, L = sweep the lights again, K = reload settings.");
        }

        public override void OnUpdate()
        {
            Lighting.Tick();
            ForeheadSwitch.Tick();

            if (_keyboardDead || !ModConfig.HotkeysEnabled.Value) return;
            try
            {
                if (Input.GetKeyDown(KeyCode.B))
                {
                    Lighting.Suspended = !Lighting.Suspended;
                    LoggerInstance.Msg(Lighting.Suspended ? "Brighter off; the scene's own lighting is back." : "Brighter on.");
                }
                else if (Input.GetKeyDown(KeyCode.RightBracket))
                {
                    ModConfig.HeadlampIntensity.Value = Mathf.Min(10f, ModConfig.HeadlampIntensity.Value + 0.25f);
                    LoggerInstance.Msg($"Headlamp {ModConfig.HeadlampIntensity.Value:0.##}.");
                }
                else if (Input.GetKeyDown(KeyCode.LeftBracket))
                {
                    ModConfig.HeadlampIntensity.Value = Mathf.Max(0f, ModConfig.HeadlampIntensity.Value - 0.25f);
                    LoggerInstance.Msg($"Headlamp {ModConfig.HeadlampIntensity.Value:0.##}" + (ModConfig.HeadlampIntensity.Value <= 0f ? " (off)." : "."));
                }
                else if (Input.GetKeyDown(KeyCode.L))
                {
                    Lighting.SweepNow();
                    LoggerInstance.Msg("Sweeping the scene lights again.");
                }
                else if (Input.GetKeyDown(KeyCode.K))
                {
                    MelonPreferences.Load();
                    LoggerInstance.Msg("Settings reloaded from MelonPreferences.cfg.");
                }
            }
            catch (Exception e)
            {
                _keyboardDead = true;
                LoggerInstance.Warning($"Desktop keys are off for this session ({e.GetType().Name}: {e.Message}). Use the forehead gesture, or edit MelonPreferences.cfg with the game closed.");
            }
        }

        public override void OnLateUpdate()
        {
            Lighting.LateTick();
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            Lighting.OnSceneChanged(sceneName);
        }

        public override void OnApplicationQuit()
        {
            LoggerInstance.Msg($"Quit. {Lighting.Describe()}.");
        }
    }
}
