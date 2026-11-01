using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Valve.VR;

namespace MusicPractice.SpatialLoop
{
    public sealed class SpatialEffectsController : MonoBehaviour
    {
        [Serializable]
        private sealed class PresetCollection
        {
            public List<EffectPresetData> Presets = new List<EffectPresetData>();
        }

        private readonly Dictionary<SteamVR_Input_Sources, EffectPairId> _selectedPairs =
            new Dictionary<SteamVR_Input_Sources, EffectPairId>();
        private readonly Dictionary<SteamVR_Input_Sources, int> _presetIndexes =
            new Dictionary<SteamVR_Input_Sources, int>();
        private readonly List<EffectPresetData> _factoryPresets =
            new List<EffectPresetData>();
        private readonly List<EffectPresetData> _userPresets =
            new List<EffectPresetData>();
        private readonly EffectChainData _microphoneEffects = new EffectChainData();

        public EffectChainData MicrophoneEffects => _microphoneEffects;
        public event Action EffectsChanged;

        private string PresetPath =>
            Path.Combine(Application.persistentDataPath, "spatial-effect-presets.json");

        private void Awake()
        {
            BuildFactoryPresets();
            LoadUserPresets();
        }

        public EffectPairId GetSelectedPair(SteamVR_Input_Sources source)
        {
            if (!_selectedPairs.TryGetValue(source, out EffectPairId pair))
            {
                pair = EffectPairId.Filter;
                _selectedPairs[source] = pair;
            }

            return pair;
        }

        public void ApplyXY(
            SteamVR_Input_Sources source,
            EffectChainData target,
            Vector2 trackpadAxis)
        {
            if (target == null)
            {
                return;
            }

            Vector2 normalized = new Vector2(
                Mathf.Clamp01(trackpadAxis.x * 0.5f + 0.5f),
                Mathf.Clamp01(trackpadAxis.y * 0.5f + 0.5f));
            target.SetXY(GetSelectedPair(source), normalized);
            EffectsChanged?.Invoke();
        }

        public void NavigatePair(SteamVR_Input_Sources source, int direction)
        {
            int count = Enum.GetValues(typeof(EffectPairId)).Length;
            int current = (int)GetSelectedPair(source);
            int next = (current + direction) % count;
            if (next < 0)
            {
                next += count;
            }

            _selectedPairs[source] = (EffectPairId)next;
            _presetIndexes[source] = 0;
            EffectsChanged?.Invoke();
        }

        public void NavigatePreset(
            SteamVR_Input_Sources source,
            EffectChainData target,
            int direction)
        {
            if (target == null)
            {
                return;
            }

            EffectPairId pair = GetSelectedPair(source);
            List<EffectPresetData> presets = GetPresets(pair);
            if (presets.Count == 0)
            {
                return;
            }

            int current = _presetIndexes.TryGetValue(source, out int index)
                ? index
                : 0;
            current = (current + direction) % presets.Count;
            if (current < 0)
            {
                current += presets.Count;
            }

            _presetIndexes[source] = current;
            EffectPresetData preset = presets[current];
            target.SetXY(pair, preset.XY);
            target.SetBypassed(pair, preset.Bypassed);
            EffectsChanged?.Invoke();
        }

        public void ToggleBypass(
            SteamVR_Input_Sources source,
            EffectChainData target)
        {
            if (target == null)
            {
                return;
            }

            EffectPairId pair = GetSelectedPair(source);
            target.SetBypassed(pair, !target.IsBypassed(pair));
            EffectsChanged?.Invoke();
        }

        public EffectPresetData SaveCurrentPreset(
            SteamVR_Input_Sources source,
            EffectChainData target)
        {
            if (target == null)
            {
                return null;
            }

            EffectPairId pair = GetSelectedPair(source);
            var preset = new EffectPresetData(
                $"{pair} User {_userPresets.Count + 1:00}",
                pair,
                target.GetXY(pair),
                target.IsBypassed(pair));
            _userPresets.Add(preset);
            SaveUserPresets();
            EffectsChanged?.Invoke();
            return preset;
        }

        public string GetStatus(
            SteamVR_Input_Sources source,
            EffectChainData target)
        {
            EffectPairId pair = GetSelectedPair(source);
            Vector2 xy = target != null ? target.GetXY(pair) : Vector2.zero;
            string bypass = target != null && target.IsBypassed(pair)
                ? " BYPASS"
                : string.Empty;
            List<EffectPresetData> presets = GetPresets(pair);
            int presetIndex = _presetIndexes.TryGetValue(source, out int index)
                ? Mathf.Clamp(index, 0, Mathf.Max(0, presets.Count - 1))
                : 0;
            string presetName = presets.Count > 0
                ? presets[presetIndex].Name
                : "Custom";
            return $"{pair}{bypass} · {presetName}\nX {xy.x:0.00}  Y {xy.y:0.00}";
        }

        public static void ApplyToAudioSource(
            AudioSource source,
            EffectChainData chain)
        {
            if (source == null)
            {
                return;
            }

            chain = chain ?? new EffectChainData();
            AudioLowPassFilter lowPass = GetOrAdd<AudioLowPassFilter>(source.gameObject);
            AudioHighPassFilter highPass = GetOrAdd<AudioHighPassFilter>(source.gameObject);
            AudioEchoFilter echo = GetOrAdd<AudioEchoFilter>(source.gameObject);
            AudioReverbFilter reverb = GetOrAdd<AudioReverbFilter>(source.gameObject);

            Vector2 filter = chain.GetXY(EffectPairId.Filter);
            bool filterEnabled = !chain.IsBypassed(EffectPairId.Filter);
            lowPass.enabled = filterEnabled && filter.x < 0.495f;
            highPass.enabled = filterEnabled && filter.x > 0.505f;

            if (lowPass.enabled)
            {
                float amount = 1f - filter.x * 2f;
                lowPass.cutoffFrequency = Mathf.Lerp(22000f, 350f, amount);
                lowPass.lowpassResonanceQ = Mathf.Lerp(1f, 10f, filter.y);
            }

            if (highPass.enabled)
            {
                float amount = (filter.x - 0.5f) * 2f;
                highPass.cutoffFrequency = Mathf.Lerp(10f, 6000f, amount);
                highPass.highpassResonanceQ = Mathf.Lerp(1f, 10f, filter.y);
            }

            Vector2 space = chain.GetXY(EffectPairId.Space);
            bool spaceEnabled = !chain.IsBypassed(EffectPairId.Space);
            echo.enabled = spaceEnabled && space.x > 0.01f;
            echo.delay = 250f;
            echo.decayRatio = Mathf.Lerp(0.1f, 0.85f, space.x);
            echo.dryMix = 1f;
            echo.wetMix = space.x;

            reverb.enabled = spaceEnabled && space.y > 0.01f;
            reverb.reverbPreset = AudioReverbPreset.User;
            reverb.dryLevel = 0f;
            reverb.room = Mathf.Lerp(-10000f, -1000f, space.y);
            reverb.reverbLevel = Mathf.Lerp(-10000f, 0f, space.y);
            reverb.decayTime = Mathf.Lerp(0.2f, 6f, space.y);
        }

        private void BuildFactoryPresets()
        {
            _factoryPresets.Clear();
            _factoryPresets.Add(new EffectPresetData(
                "Neutral",
                EffectPairId.Filter,
                new Vector2(0.5f, 0f)));
            _factoryPresets.Add(new EffectPresetData(
                "Warm",
                EffectPairId.Filter,
                new Vector2(0.25f, 0.25f)));
            _factoryPresets.Add(new EffectPresetData(
                "Telephone",
                EffectPairId.Filter,
                new Vector2(0.78f, 0.45f)));
            _factoryPresets.Add(new EffectPresetData(
                "Dry",
                EffectPairId.Space,
                Vector2.zero));
            _factoryPresets.Add(new EffectPresetData(
                "Echo",
                EffectPairId.Space,
                new Vector2(0.65f, 0.1f)));
            _factoryPresets.Add(new EffectPresetData(
                "Hall",
                EffectPairId.Space,
                new Vector2(0.15f, 0.8f)));
        }

        private List<EffectPresetData> GetPresets(EffectPairId pair)
        {
            var presets = new List<EffectPresetData>();
            for (int i = 0; i < _factoryPresets.Count; i++)
            {
                if (_factoryPresets[i].Pair == pair)
                {
                    presets.Add(_factoryPresets[i]);
                }
            }

            for (int i = 0; i < _userPresets.Count; i++)
            {
                if (_userPresets[i].Pair == pair)
                {
                    presets.Add(_userPresets[i]);
                }
            }

            return presets;
        }

        private void SaveUserPresets()
        {
            var collection = new PresetCollection();
            collection.Presets.AddRange(_userPresets);
            File.WriteAllText(PresetPath, JsonUtility.ToJson(collection, true));
        }

        private void LoadUserPresets()
        {
            if (!File.Exists(PresetPath))
            {
                return;
            }

            PresetCollection collection = JsonUtility.FromJson<PresetCollection>(
                File.ReadAllText(PresetPath));
            if (collection?.Presets != null)
            {
                _userPresets.AddRange(collection.Presets);
            }
        }

        private static T GetOrAdd<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }
    }
}
