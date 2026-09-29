using System;
using System.Collections.Generic;
using Il2Cpp;
using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Brighter
{
    /// <summary>
    /// Five independent levers on how dark a room looks. Which of them a room answers to
    /// depends on its shaders, so each one has its own setting and none depends on another.
    /// </summary>
    public static class Lighting
    {
        private const string Prefix = "Brighter_";

        /// <summary>Hotkey B. Restores every captured value while it is on.</summary>
        public static bool Suspended;

        // ---- the scene's own values, captured once per scene so the mod can hand them back --
        private static bool _captured;
        private static bool _fogWas;
        private static float _fogDensityWas;
        private static AmbientMode _ambientModeWas;
        private static Color _ambientLightWas;
        private static float _ambientIntensityWas;
        private static int _pixelLightsWas;

        // ---- headlamp ----------------------------------------------------------------------
        private static GameObject _lampObject;
        private static Light _lamp;
        private static bool _lampFailed;
        private static Transform _eyes;
        private static string _eyesSource = "none";
        private static float _eyesRetryAt;

        // ---- sweep -------------------------------------------------------------------------
        private static readonly HashSet<int> Seen = new HashSet<int>();

        /// <summary>A light the sweep changed, with the values it had before, so suspending can hand them back.</summary>
        private struct Touched
        {
            public Light Light;
            public float Intensity;
            public float Range;
            public bool WasInactive;
        }

        private static readonly List<Touched> Changed = new List<Touched>();
        private static readonly List<GameObject> Clones = new List<GameObject>();
        private static bool _leversApplied = true;
        private static float _nextSweepAt;
        private static int _boosted, _cloned, _enabled;
        private static string _scene = "?";

        public static void OnSceneChanged(string sceneName)
        {
            _scene = sceneName;
            _captured = false;          // the new scene brings its own fog and ambient
            Seen.Clear();
            Changed.Clear();
            Clones.Clear();          // the scene that held them is gone; the objects went with it
            _leversApplied = Active;   // a new scene starts in whatever state the mod is in
            _boosted = _cloned = _enabled = 0;
            _eyes = null;
            _nextSweepAt = Time.unscaledTime + Mathf.Max(0f, ModConfig.SweepDelaySeconds.Value);
        }

        /// <summary>
        /// Runs the next sweep now (hotkey L). It does not clear <see cref="Seen"/>: a light boosted
        /// twice would carry the multiplier twice.
        /// </summary>
        public static void SweepNow() => _nextSweepAt = 0f;

        public static void Tick()
        {
            try { ApplyEnvironment(); } catch (Exception e) { Warn("environment", e); }
            try { ApplyLevers(); } catch (Exception e) { Warn("levers", e); }
            try { if (Active) Sweep(); } catch (Exception e) { Warn("sweep", e); }
        }

        /// <summary>
        /// Switching the mod off has to reach the work already done: a copied light keeps burning
        /// and a raised intensity keeps its multiplier until they are handed back.
        /// </summary>
        private static void ApplyLevers()
        {
            if (Active == _leversApplied) return;
            _leversApplied = Active;

            for (var i = 0; i < Clones.Count; i++)
                if (Interop.Alive(Clones[i])) Clones[i].SetActive(Active);

            for (var i = 0; i < Changed.Count; i++)
            {
                var t = Changed[i];
                if (!Interop.Alive(t.Light)) continue;
                try
                {
                    if (Active)
                    {
                        t.Light.intensity = t.Intensity * ModConfig.SceneLightBoost.Value;
                        if (t.Light.type != LightType.Directional) t.Light.range = t.Range * ModConfig.SceneLightRangeScale.Value;
                        if (t.WasInactive) t.Light.gameObject.SetActive(true);
                    }
                    else
                    {
                        t.Light.intensity = t.Intensity;
                        if (t.Light.type != LightType.Directional) t.Light.range = t.Range;
                        if (t.WasInactive) t.Light.gameObject.SetActive(false);
                    }
                }
                catch { }
            }
        }

        public static void LateTick()
        {
            try { Headlamp(); } catch (Exception e) { Warn("headlamp", e); }
        }

        private static bool Active => ModConfig.Enabled.Value && !Suspended;

        // ---- lever 1 and 2: fog and ambient -------------------------------------------------

        private static void ApplyEnvironment()
        {
            if (!_captured)
            {
                _fogWas = RenderSettings.fog;
                _fogDensityWas = RenderSettings.fogDensity;
                _ambientModeWas = RenderSettings.ambientMode;
                _ambientLightWas = RenderSettings.ambientLight;
                _ambientIntensityWas = RenderSettings.ambientIntensity;
                _pixelLightsWas = QualitySettings.pixelLightCount;
                _captured = true;
                if (ModConfig.VerboseLogging.Value)
                    Core.Log.Msg($"[{_scene}] scene lighting: fog={_fogWas} density={_fogDensityWas:0.####}, ambient={_ambientModeWas} intensity={_ambientIntensityWas:0.##}, pixelLights={_pixelLightsWas}");
            }

            if (!Active) { Restore(); return; }

            var fogScale = ModConfig.FogDensityScale.Value;
            if (fogScale <= 0f) RenderSettings.fog = false;
            else { RenderSettings.fog = _fogWas; RenderSettings.fogDensity = _fogDensityWas * fogScale; }

            var ambient = ModConfig.AmbientLevel.Value;
            if (ambient > 0f)
            {
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(ambient, ambient, ambient, 1f);
                RenderSettings.ambientIntensity = 1f;
            }
            else
            {
                RenderSettings.ambientMode = _ambientModeWas;
                RenderSettings.ambientLight = _ambientLightWas;
                RenderSettings.ambientIntensity = _ambientIntensityWas;
            }

            var pixelLights = ModConfig.PixelLightCount.Value;
            QualitySettings.pixelLightCount = pixelLights > 0 ? pixelLights : _pixelLightsWas;
        }

        private static void Restore()
        {
            RenderSettings.fog = _fogWas;
            RenderSettings.fogDensity = _fogDensityWas;
            RenderSettings.ambientMode = _ambientModeWas;
            RenderSettings.ambientLight = _ambientLightWas;
            RenderSettings.ambientIntensity = _ambientIntensityWas;
            QualitySettings.pixelLightCount = _pixelLightsWas;
        }

        // ---- lever 3: the headlamp ----------------------------------------------------------

        private static void Headlamp()
        {
            var want = Active && ModConfig.HeadlampIntensity.Value > 0f;
            if (!want)
            {
                if (Interop.Alive(_lampObject)) _lampObject.SetActive(false);
                return;
            }

            if (_lampFailed) return;

            if (!Interop.Alive(_lampObject))
            {
                _lampObject = new GameObject(Prefix + "Headlamp");
                Object.DontDestroyOnLoad(_lampObject);
                _lamp = _lampObject.AddComponent(Il2CppType.Of<Light>())?.TryCast<Light>();
                if (_lamp == null)
                {
                    Core.Log.Error("AddComponent(Light) returned something that is not a Light; the headlamp stays off for this session.");
                    Object.Destroy(_lampObject); _lampObject = null; _lampFailed = true; return;
                }
                _lamp.renderMode = LightRenderMode.ForcePixel;
                _lamp.cullingMask = -1;
                Core.Log.Msg("Headlamp created.");
            }

            var eyes = Eyes();
            if (!Interop.Alive(eyes)) { _lampObject.SetActive(false); return; }

            _lampObject.SetActive(true);
            _lampObject.transform.position = eyes.position + eyes.forward * ModConfig.HeadlampForwardOffset.Value;
            // Tilting down lights the floor ahead while you look level. A point light is
            // omnidirectional, so this only changes anything in flood mode.
            _lampObject.transform.rotation = eyes.rotation * Quaternion.Euler(ModConfig.HeadlampPitchDegrees.Value, 0f, 0f);

            // A point light falls off with the square of the distance, so a wall at arm's length
            // blows out and trips the game's bloom while the far wall stays dim. A directional
            // light has no falloff at all: near and far read the same.
            var flood = string.Equals((ModConfig.HeadlampMode.Value ?? "point").Trim(), "flood", StringComparison.OrdinalIgnoreCase);
            _lamp.type = flood ? LightType.Directional : LightType.Point;
            _lamp.intensity = ModConfig.HeadlampIntensity.Value * (flood ? ModConfig.HeadlampFloodScale.Value : 1f);
            _lamp.range = ModConfig.HeadlampRange.Value;
            _lamp.shadows = ModConfig.HeadlampShadows.Value ? LightShadows.Soft : LightShadows.None;
            _lamp.color = ParseColor(ModConfig.HeadlampColor.Value);
        }

        private static Color ParseColor(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return Color.white;
            if (hex[0] != '#') hex = "#" + hex;
            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.white;
        }

        private static Transform Eyes()
        {
            if (Interop.Alive(_eyes)) return _eyes;
            if (Time.unscaledTime < _eyesRetryAt) return null;
            _eyesRetryAt = Time.unscaledTime + 1f;
            _eyes = null;

            try
            {
                var cam = Camera.main;
                if (Interop.Alive(cam) && cam.enabled) { _eyes = cam.transform; _eyesSource = "Camera.main"; }
            }
            catch { }

            if (_eyes == null)
            {
                try
                {
                    var local = AvatarPlayer.LocalAvatar;
                    if (Interop.Alive(local))
                    {
                        if (Interop.Alive(local.Eye)) { _eyes = local.Eye; _eyesSource = "AvatarPlayer.Eye"; }
                        else if (Interop.Alive(local.Head)) { _eyes = local.Head; _eyesSource = "AvatarPlayer.Head"; }
                    }
                }
                catch { }
            }

            if (_eyes != null && ModConfig.VerboseLogging.Value) Core.Log.Msg($"Headlamp follows {_eyesSource}.");
            return _eyes;
        }

        // ---- levers 4 and 5: the scene's own lights ------------------------------------------

        private static void Sweep()
        {
            var boost = ModConfig.SceneLightBoost.Value;
            var rangeScale = ModConfig.SceneLightRangeScale.Value;
            var clone = ModConfig.CloneSceneLights.Value;
            if (boost == 1f && rangeScale == 1f && !clone && !ModConfig.EnableDisabledLights.Value) return;

            var now = Time.unscaledTime;
            if (now < _nextSweepAt) return;
            _nextSweepAt = now + Mathf.Max(1f, ModConfig.SweepIntervalSeconds.Value);

            var found = FindLights();
            if (found == null) return;

            var wasBoosted = _boosted; var wasCloned = _cloned; var wasEnabled = _enabled;
            for (var i = 0; i < found.Count; i++)
            {
                var light = found[i];
                if (!Interop.Alive(light)) continue;

                int id;
                try { id = light.GetInstanceID(); } catch { continue; }
                if (!Seen.Add(id)) continue;

                try
                {
                    if (light.name.StartsWith(Prefix, StringComparison.Ordinal)) continue;

                    var go = light.gameObject;
                    var record = new Touched { Light = light, Intensity = light.intensity, Range = light.range, WasInactive = false };

                    if (ModConfig.EnableDisabledLights.Value && !go.activeSelf) { go.SetActive(true); record.WasInactive = true; _enabled++; }

                    if (boost != 1f) light.intensity *= boost;
                    if (rangeScale != 1f && light.type != LightType.Directional) light.range *= rangeScale;
                    Changed.Add(record);
                    _boosted++;

                    if (clone && _cloned < ModConfig.MaxClonedLights.Value) Clone(light);
                }
                catch (Exception e) { Warn("light", e); }
            }

            if (ModConfig.VerboseLogging.Value && (_boosted != wasBoosted || _cloned != wasCloned || _enabled != wasEnabled))
                Core.Log.Msg($"[{_scene}] swept {found.Count} lights: {_boosted} raised, {_cloned} copied, {_enabled} switched on.");
        }

        /// <summary>
        /// A light the level baked contributes nothing at runtime, and nothing in the runtime API
        /// says which ones those are. A light created here is realtime by construction, so the copy
        /// lights the room whatever the original was.
        /// </summary>
        private static void Clone(Light src)
        {
            if (src.type == LightType.Directional) return;          // a second sun blows out the scene
            if (!src.gameObject.activeInHierarchy) return;          // a child of a disabled object never renders

            var go = new GameObject(Prefix + "Fill");
            go.transform.SetParent(src.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            var fill = go.AddComponent(Il2CppType.Of<Light>())?.TryCast<Light>();
            if (fill == null) { Object.Destroy(go); return; }
            fill.type = src.type == LightType.Spot ? LightType.Spot : LightType.Point;
            if (fill.type == LightType.Spot) fill.spotAngle = src.spotAngle;
            fill.color = src.color;
            fill.intensity = ModConfig.CloneIntensity.Value;
            fill.range = Mathf.Max(src.range, ModConfig.CloneRange.Value);
            fill.shadows = LightShadows.None;
            fill.renderMode = LightRenderMode.ForcePixel;
            fill.cullingMask = -1;
            Clones.Add(go);
            _cloned++;
        }

        private static List<Light> FindLights()
        {
            var type = Il2CppType.Of<Light>();
            Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Object> raw;
            try { raw = Object.FindObjectsOfType(type, true); }
            catch { try { raw = Object.FindObjectsOfType(type); } catch (Exception e) { Warn("find", e); return null; } }
            if (raw == null) return null;

            var lights = new List<Light>();
            for (var i = 0; i < raw.Length; i++)
            {
                var light = raw[i]?.TryCast<Light>();
                if (light != null) lights.Add(light);
            }
            return lights;
        }

        // ---- reporting -----------------------------------------------------------------------

        public static string Describe() =>
            $"scene {_scene}: {_boosted} lights raised, {_cloned} copied, {_enabled} switched on; headlamp {(Interop.Alive(_lampObject) && _lampObject.activeSelf ? "on via " + _eyesSource : "off")}";

        private static void Warn(string what, Exception e) => Core.Log.Warning($"{what} threw: {e.GetType().Name}: {e.Message}");
    }
}
