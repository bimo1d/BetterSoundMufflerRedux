using System.Collections.Generic;
using System.Reflection;
using KSP.Audio;
using KSP.Game;
using KSP.Sim.impl;
using Redux.ExtraModTypes;
using UnityEngine;

namespace BetterSoundMufflerRedux
{
    internal sealed class BetterSoundMufflerController
    {
        private const float AtmosphereReturnDensity = 0.02f;
        private const float VolumeSmoothSpeed = 2.5f;
        private const float GracePeriodSeconds = 1.0f;
        private const float StopAllThreshold = 0.05f;
        private const float KerbinSeaLevelDensity = 1.225f;
        private const float DebugLogInterval = 0.5f;
        private const float EmitterCacheRefreshInterval = 0.75f;

        private static BetterSoundMufflerController _active;

        private readonly KerbalMod _mod;
        private readonly BetterSoundMufflerConfig _config;
        private readonly HashSet<GameObject> _affected = new HashSet<GameObject>();
        private readonly List<KSPPartAudioBase> _partAudioBuffer = new List<KSPPartAudioBase>();
        private readonly List<KSPVFXAudio> _vfxAudioBuffer = new List<KSPVFXAudio>();
        private readonly List<KSPAudioVessel> _vesselAudioBuffer = new List<KSPAudioVessel>();
        private readonly List<AkWwiseEventPlayback> _eventPlaybackBuffer = new List<AkWwiseEventPlayback>();
        private readonly List<GameObject> _emitterCache = new List<GameObject>();
        private readonly HashSet<GameObject> _emitterCacheSet = new HashSet<GameObject>();
        private readonly Dictionary<string, float> _nextDebugLogTime = new Dictionary<string, float>();
        private GameObject _uiAudioObject;
        private VesselBehavior _cachedBehavior;
        private bool _uiAudioLookupDone;
        private float _currentVolume = 1.0f;
        private bool _muffleActive;
        private float _lastAtmDensity = KerbinSeaLevelDensity;
        private bool _hadVessel;
        private float _gateLostAt = -1.0f;
        private float _nextEmitterCacheRefreshTime;

        internal BetterSoundMufflerController(KerbalMod mod, BetterSoundMufflerConfig config)
        {
            _mod = mod;
            _config = config;
            _active = this;
        }

        internal static BetterSoundMufflerController Active
        {
            get { return _active; }
        }

        internal static void ClearActive(BetterSoundMufflerController controller)
        {
            if (_active == controller) _active = null;
        }

        internal void LateUpdate()
        {
            VesselComponent vessel;
            VesselBehavior behavior;
            if (CanMuffle(out vessel, out behavior))
            {
                UpdateMuffling(vessel, behavior);
            }
            else
            {
                HandleGateClosed();
            }
        }

        internal void ResetAll()
        {
            int count = _affected.Count;
            foreach (GameObject go in _affected)
            {
                if (go == null) continue;
                AkSoundEngine.SetGameObjectOutputBusVolume(go, null, 1.0f);
                AkSoundEngine.SetScalingFactor(go, 1.0f);
                AkSoundEngine.ResetRTPCValue(KSPAudioParams.k_part_atmos_density_rtpc, go);
                AkSoundEngine.ResetRTPCValue(KSPAudioParams.k_part_static_pressure_kPa_rtpc, go);
            }

            _affected.Clear();
            _emitterCache.Clear();
            _emitterCacheSet.Clear();
            _cachedBehavior = null;
            _muffleActive = false;
            _currentVolume = 1.0f;
            _nextEmitterCacheRefreshTime = 0.0f;
            LogDebug("reset", "count=" + count);
        }

        internal void ApplyToEmitter(GameObject go)
        {
            if (go == null || !IsEffectiveMuffleActive()) return;

            if (IsUiAudio(go))
            {
                LogDebug("skip-ui", go.name);
                return;
            }

            float vol = Mathf.Min(_currentVolume, ComputeVolumeFromDensity(_lastAtmDensity));
            SetEmitterVolume(go, vol);
        }

        internal void ApplyToHierarchy(GameObject root)
        {
            if (root == null || !IsEffectiveMuffleActive()) return;

            ApplyToEmitter(root);
            Transform t = root.transform;
            for (int i = 0; i < t.childCount; i++)
            {
                ApplyToHierarchy(t.GetChild(i).gameObject);
            }
        }

        internal void LogPatch(string patchName, GameObject target, string extra)
        {
            if (!IsDebugEnabled()) return;

            string targetName = target == null ? "null" : target.name;
            bool effective = IsEffectiveMuffleActive();
            string gateReason = !effective ? "gate-off" : (target != null && IsUiAudio(target) ? "ui-skip" : "apply");
            string suffix = string.IsNullOrEmpty(extra) ? "" : " " + extra;
            LogDebug("patch:" + patchName + ":" + targetName, "vol=" + _currentVolume.ToString("F2") + " result=" + gateReason + suffix);
        }

        internal void StopEmitterIfMuffled(GameObject go)
        {
            if (go == null || !IsEffectiveMuffleActive() || IsUiAudio(go)) return;

            float vol = Mathf.Min(_currentVolume, ComputeVolumeFromDensity(_lastAtmDensity));
            if (vol <= StopAllThreshold)
            {
                AkSoundEngine.StopAll(go);
            }
        }

        internal void RefreshEmitterCacheNow()
        {
            _nextEmitterCacheRefreshTime = 0.0f;
        }

        private void UpdateMuffling(VesselComponent vessel, VesselBehavior behavior)
        {
            _gateLostAt = -1.0f;
            _hadVessel = true;
            _lastAtmDensity = vessel == null ? KerbinSeaLevelDensity : Mathf.Max(0.0f, (float)vessel.AtmDensity);

            float target = ComputeVolumeFromDensity(_lastAtmDensity);
            _currentVolume = Mathf.MoveTowards(_currentVolume, target, Time.unscaledDeltaTime * VolumeSmoothSpeed);
            _muffleActive = _currentVolume < 0.999f;

            if (!_muffleActive && target >= 0.999f)
            {
                ResetAll();
                return;
            }

            ScanVessel(behavior);
            LogDebug("tick", "vol=" + _currentVolume.ToString("F2") + " target=" + target.ToString("F2") + " atm=" + _lastAtmDensity.ToString("F4") + " affected=" + _affected.Count);
        }

        private void HandleGateClosed()
        {
            float now = Time.unscaledTime;
            if (_hadVessel && _muffleActive)
            {
                if (_gateLostAt < 0.0f) _gateLostAt = now;
                if (now - _gateLostAt < GracePeriodSeconds)
                {
                    LogDebug("grace", "elapsed=" + (now - _gateLostAt).ToString("F2") + " vol=" + _currentVolume.ToString("F2") + " affected=" + _affected.Count);
                    return;
                }
            }

            if (_muffleActive || _affected.Count > 0)
            {
                LogDebug("gate-closed", "enabled=" + (_config.Enabled != null && _config.Enabled.Value) + " state=" + GetGameStateName());
            }

            ResetAll();
            _hadVessel = false;
            _gateLostAt = -1.0f;
        }

        private string GetGameStateName()
        {
            if (_mod.Game == null || _mod.Game.GlobalGameState == null) return "null";
            return _mod.Game.GlobalGameState.GetState().ToString();
        }

        private bool CanMuffle(out VesselComponent vessel, out VesselBehavior behavior)
        {
            vessel = null;
            behavior = null;

            if (_mod.Game == null || _config.Enabled == null || !_config.Enabled.Value) return false;
            if (_mod.Game.GlobalGameState == null) return false;

            GameState state = _mod.Game.GlobalGameState.GetState();
            if (state != GameState.FlightView && state != GameState.Map3DView) return false;
            if (_mod.Game.ViewController == null) return false;
            if (!_mod.Game.ViewController.TryGetActiveSimVessel(out vessel, true) || vessel == null) return false;

            behavior = _mod.Game.ViewController.GetBehaviorIfLoaded(vessel);
            return behavior != null;
        }

        private float ComputeVolumeFromDensity(float density)
        {
            float amount = Mathf.Clamp01(_config.GetMuffleAmount() / 100.0f);
            if (amount <= 0.0f) return 1.0f;

            float atmosphere = Mathf.Clamp01(density / AtmosphereReturnDensity);
            float vacuum = 1.0f - atmosphere;
            return Mathf.Clamp01(1.0f - (amount * vacuum));
        }

        private void ScanVessel(VesselBehavior behavior)
        {
            if (behavior == null) return;

            if (_cachedBehavior != behavior || Time.unscaledTime >= _nextEmitterCacheRefreshTime)
            {
                RebuildEmitterCache(behavior);
            }

            for (int i = 0; i < _emitterCache.Count; i++)
            {
                SetEmitterVolume(_emitterCache[i], _currentVolume);
            }
        }

        private void RebuildEmitterCache(VesselBehavior behavior)
        {
            _cachedBehavior = behavior;
            _nextEmitterCacheRefreshTime = Time.unscaledTime + EmitterCacheRefreshInterval;
            _emitterCache.Clear();
            _emitterCacheSet.Clear();

            AddEmitter(behavior.gameObject);
            ScanBufferToCache(behavior, _partAudioBuffer);
            ScanBufferToCache(behavior, _vfxAudioBuffer);
            ScanBufferToCache(behavior, _vesselAudioBuffer);
            ScanBufferToCache(behavior, _eventPlaybackBuffer);

            foreach (PartBehavior part in behavior.parts)
            {
                ScanPart(part);
            }
        }

        private void ScanPart(PartBehavior part)
        {
            if (part == null) return;

            AddEmitter(part.gameObject);
            ScanBufferToCache(part, _partAudioBuffer);
            ScanBufferToCache(part, _vfxAudioBuffer);
            ScanBufferToCache(part, _eventPlaybackBuffer);
        }

        private void ScanBufferToCache<T>(Component root, List<T> buffer) where T : Component
        {
            buffer.Clear();
            root.GetComponentsInChildren(true, buffer);
            for (int i = 0; i < buffer.Count; i++)
            {
                T component = buffer[i];
                if (component != null) AddEmitter(component.gameObject);
            }
        }

        private void AddEmitter(GameObject go)
        {
            if (go == null || !_emitterCacheSet.Add(go)) return;

            _emitterCache.Add(go);
        }

        private void SetEmitterVolume(GameObject go, float volume)
        {
            if (go == null) return;

            _affected.Add(go);
            AkSoundEngine.SetGameObjectOutputBusVolume(go, null, volume);
            AkSoundEngine.SetScalingFactor(go, volume);
            AkSoundEngine.SetRTPCValue(KSPAudioParams.k_part_atmos_density_rtpc, 0.0f, go);
            AkSoundEngine.SetRTPCValue(KSPAudioParams.k_part_static_pressure_kPa_rtpc, 0.0f, go);
        }

        private bool IsUiAudio(GameObject go)
        {
            EnsureUiAudioLookup();
            if (_uiAudioObject == null) return false;

            Transform t = go.transform;
            while (t != null)
            {
                if (t.gameObject == _uiAudioObject) return true;
                t = t.parent;
            }

            return false;
        }

        private void EnsureUiAudioLookup()
        {
            if (_uiAudioLookupDone) return;

            KSPAudioEventManager mgr = Object.FindAnyObjectByType<KSPAudioEventManager>();
            if (mgr == null) return;

            FieldInfo field = typeof(KSPAudioEventManager).GetField("_uiAudioGameObject", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) return;

            _uiAudioObject = field.GetValue(mgr) as GameObject;
            _uiAudioLookupDone = _uiAudioObject != null;
        }

        private bool IsEffectiveMuffleActive()
        {
            if (_muffleActive) return true;
            if (!_hadVessel || _config == null || _config.Enabled == null || !_config.Enabled.Value) return false;

            float amount = Mathf.Clamp01(_config.GetMuffleAmount() / 100.0f);
            if (amount <= 0.0f) return false;

            float atmosphere = Mathf.Clamp01(_lastAtmDensity / AtmosphereReturnDensity);
            return atmosphere < 0.999f;
        }

        private bool IsDebugEnabled()
        {
            return _config != null && _config.DebugLogging != null && _config.DebugLogging.Value;
        }

        private void LogDebug(string stage, string details)
        {
            if (!IsDebugEnabled()) return;

            float now = Time.unscaledTime;
            float next;
            if (_nextDebugLogTime.TryGetValue(stage, out next) && now < next) return;

            _nextDebugLogTime[stage] = now + DebugLogInterval;
            Debug.Log("[BetterSoundMufflerRedux] " + stage + " " + details);
        }
    }
}
