using System;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(Brighter.Core), "Brighter", "0.1.0", "henriquelino")]
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
        public const string Version = "0.1.0";

        public static Core Instance { get; private set; }
        public static MelonLogger.Instance Log => Instance.LoggerInstance;

        public override void OnInitializeMelon()
        {
            Instance = this;
            ModConfig.Load();
            try { MelonPreferences.Save(); }
            catch (Exception e) { LoggerInstance.Warning($"Could not write MelonPreferences.cfg: {e.Message}"); }

            LoggerInstance.Msg($"Brighter {Version} — five ways to light a dark room, each its own setting in [Brighter].");
            LoggerInstance.Msg("Hotkeys (window focused): B = all on/off, ] and [ = headlamp brighter/dimmer, L = sweep the lights again, K = reload settings.");
        }

        public override void OnUpdate()
        {
            Lighting.Tick();

            if (!ModConfig.HotkeysEnabled.Value) return;
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
            catch (Exception e) { LoggerInstance.Warning($"Hotkey threw: {e.GetType().Name}: {e.Message}"); }
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
