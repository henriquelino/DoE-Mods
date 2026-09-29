using System;
using Il2Cpp;
using UnityEngine;

namespace Brighter.Controls
{
    /// <summary>
    /// The headlamp is worn, so it is switched where a real one would be. Put a hand to your
    /// forehead and pull the trigger: a tap turns the mod off and back on, and holding while
    /// you turn your wrist dims or brightens the lamp, like a knob.
    ///
    /// Off means off. The tap suspends every lever, not the lamp alone, so the room goes back
    /// to the lighting the game shipped with.
    ///
    /// Input comes from the game's own abstraction (<c>XRInput.Instance</c>), the same source
    /// VisualCues and StayPutVR read. Nothing here touches <c>UnityEngine.Input</c>, which
    /// throws on game 1.3: the game has switched to the Input System package.
    /// </summary>
    public static class ForeheadSwitch
    {
        private const float TriggerOn = 0.6f;
        private const float TriggerOff = 0.35f;     // hysteresis, so a resting finger cannot chatter

        private static bool _held;
        private static bool _twisted;               // the hold turned into a knob, so releasing is not a tap
        private static string _hand = "right";
        private static float _heldSince;
        private static float _startIntensity;
        private static float _turnedDegrees;
        private static Vector3 _lastUp;
        private static float _lastHapticAt;

        private static float _retryAt;
        private static bool _warned, _readyLogged;
        private static int _failures;

        public static void Tick()
        {
            if (!ModConfig.Enabled.Value || !ModConfig.ForeheadSwitchEnabled.Value) return;
            if (Time.unscaledTime < _retryAt) return;

            try
            {
                var input = XRInput.Instance;
                if (!Interop.Alive(input)) { _retryAt = Time.unscaledTime + 2f; return; }

                var player = AvatarPlayer.LocalAvatar;
                if (!Interop.Alive(player)) { _retryAt = Time.unscaledTime + 2f; return; }

                var head = Interop.Alive(player.Eye) ? player.Eye : player.Head;
                if (!Interop.Alive(head)) { _retryAt = Time.unscaledTime + 2f; return; }

                if (!_readyLogged) { _readyLogged = true; Core.Log.Msg("Forehead switch ready: hand to your head, pull the trigger to toggle, hold and turn your wrist to dim."); }

                if (_held) Continue(input, head);
                else Begin(input, player, head);
            }
            catch (Exception e)
            {
                _failures++;
                if (!_warned) { _warned = true; Core.Log.Warning($"Forehead switch not readable ({e.GetType().Name}: {e.Message}); retrying. Settings still work from MelonPreferences.cfg."); }
                _retryAt = Time.unscaledTime + (_failures > 20 ? 30f : 3f);
            }
        }

        // ---- start -----------------------------------------------------------------------------

        private static void Begin(XRInput input, AvatarPlayer player, Transform head)
        {
            var reach = ModConfig.ForeheadReachMeters.Value;

            if (Ready(input, head, player.RightHand, false, reach)) Grab(input, head, player.RightHand, "right");
            else if (Ready(input, head, player.LeftHand, true, reach)) Grab(input, head, player.LeftHand, "left");
        }

        /// <summary>
        /// The hand is at your forehead, the trigger is pulled, and (by default) it holds nothing.
        /// In front of the head, not merely near it: reaching over your shoulder for an arrow puts
        /// a hand the same distance away, behind you.
        /// </summary>
        private static bool Ready(XRInput input, Transform head, Transform hand, bool isLeft, float reach)
        {
            if (!Interop.Alive(hand)) return false;
            if (Vector3.Distance(hand.position, head.position) > reach) return false;

            var local = head.InverseTransformPoint(hand.position);
            if (local.z < ModConfig.ForeheadMinForwardMeters.Value) return false;
            if (local.y < ModConfig.ForeheadMinHeightMeters.Value) return false;

            var trigger = Mathf.Clamp01(isLeft ? input.leftIndexTrigger : input.rightIndexTrigger);
            if (trigger < TriggerOn) return false;

            // A weapon swing is a raised hand and a pulled trigger too. The grip tells the two apart.
            if (ModConfig.ForeheadNeedsEmptyHand.Value)
            {
                var grip = Mathf.Clamp01(isLeft ? input.leftHandTrigger : input.rightHandTrigger);
                if (grip > 0.2f) return false;
            }
            return true;
        }

        private static void Grab(XRInput input, Transform head, Transform hand, string which)
        {
            _held = true;
            _twisted = false;
            _hand = which;
            _heldSince = Time.unscaledTime;
            _turnedDegrees = 0f;
            _startIntensity = Mathf.Clamp(ModConfig.HeadlampIntensity.Value, ModConfig.ForeheadMinIntensity.Value, ModConfig.ForeheadMaxIntensity.Value);
            _lastUp = hand.up;
            _lastHapticAt = _startIntensity;
        }

        // ---- hold ------------------------------------------------------------------------------

        private static void Continue(XRInput input, Transform head)
        {
            var isLeft = _hand == "left";
            var player = AvatarPlayer.LocalAvatar;
            var hand = isLeft ? player.LeftHand : player.RightHand;
            var trigger = Interop.Alive(hand) ? Mathf.Clamp01(isLeft ? input.leftIndexTrigger : input.rightIndexTrigger) : 0f;

            if (trigger < TriggerOff || !Interop.Alive(hand)) { Release(input); return; }

            // Roll about the axis the hand points along, accumulated per frame so it never wraps.
            var step = Vector3.SignedAngle(_lastUp, hand.up, hand.forward);
            if (Mathf.Abs(step) < 45f) _turnedDegrees += step;     // a jump that large is tracking noise
            _lastUp = hand.up;

            var deadzone = ModConfig.ForeheadTwistDeadzoneDegrees.Value;
            if (!_twisted && Mathf.Abs(_turnedDegrees) < deadzone) return;

            if (!_twisted)
            {
                _twisted = true;
                // Reaching for the knob means you want light, so the turn also undoes a tap.
                if (Lighting.Suspended) { Lighting.Suspended = false; Core.Log.Msg("Brighter on."); }
                Core.Log.Msg("Headlamp knob: turn your wrist.");
            }

            var min = ModConfig.ForeheadMinIntensity.Value;
            var max = Mathf.Max(min + 0.1f, ModConfig.ForeheadMaxIntensity.Value);
            var sweep = Mathf.Max(30f, ModConfig.ForeheadTurnDegrees.Value);
            var wanted = Mathf.Clamp(_startIntensity + _turnedDegrees / sweep * (max - min), min, max);
            ModConfig.HeadlampIntensity.Value = wanted;

            // One tick per quarter step, so the knob has detents you can feel.
            if (Mathf.Abs(wanted - _lastHapticAt) >= 0.25f)
            {
                _lastHapticAt = wanted;
                Haptic(input, isLeft, 0.25f, 1f);
            }
        }

        // ---- release ---------------------------------------------------------------------------

        private static void Release(XRInput input)
        {
            var isLeft = _hand == "left";
            _held = false;

            if (_twisted)
            {
                Core.Log.Msg($"Headlamp {ModConfig.HeadlampIntensity.Value:0.##}.");
                return;
            }

            if (Time.unscaledTime - _heldSince > ModConfig.ForeheadTapSeconds.Value) return;   // a long press that never turned does nothing

            // Every lever, not the lamp alone: off has to mean the lighting the game shipped with.
            Lighting.Suspended = !Lighting.Suspended;
            Core.Log.Msg(Lighting.Suspended ? "Brighter off: the game's own lighting is back." : "Brighter on.");

            Haptic(input, isLeft, 0.6f, 2f);
        }

        private static void Haptic(XRInput input, bool isLeft, float amplitude, float duration)
        {
            if (!ModConfig.ForeheadHaptics.Value) return;
            try { input.PlayHaptics(isLeft ? Handedness.Left : Handedness.Right, amplitude, duration); }
            catch { }
        }
    }
}
