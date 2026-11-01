using System;
using UnityEngine;

namespace MusicPractice.SpatialLoop
{
    [Serializable]
    public sealed class SampleInstanceData
    {
        [SerializeField] private string _id;
        [SerializeField] private string _sampleAssetId;
        [SerializeField] private long _startTick;
        [SerializeField, Min(1)] private long _durationTicks = MusicalTime.PulsesPerQuarter;
        [SerializeField, Range(0f, 1f)] private float _velocity = 0.8f;
        [SerializeField] private float _pitchSemitones;
        [SerializeField] private EffectChainData _effects = new EffectChainData();

        public string Id => _id;
        public string SampleAssetId => _sampleAssetId;
        public long StartTick => _startTick;
        public long DurationTicks => Math.Max(1, _durationTicks);
        public float Velocity => Mathf.Clamp01(_velocity);
        public float PitchSemitones => _pitchSemitones;
        public EffectChainData Effects =>
            _effects ?? (_effects = new EffectChainData());

        public SampleInstanceData()
        {
            _id = Guid.NewGuid().ToString("N");
        }

        public SampleInstanceData(
            string sampleAssetId,
            long startTick,
            long durationTicks,
            float velocity = 0.8f,
            float pitchSemitones = 0f,
            EffectChainData effects = null)
        {
            if (string.IsNullOrWhiteSpace(sampleAssetId))
            {
                throw new ArgumentException(
                    "A sample asset ID is required.",
                    nameof(sampleAssetId));
            }

            if (durationTicks < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(durationTicks));
            }

            _id = Guid.NewGuid().ToString("N");
            _sampleAssetId = sampleAssetId;
            _startTick = startTick;
            _durationTicks = durationTicks;
            _velocity = Mathf.Clamp01(velocity);
            _pitchSemitones = pitchSemitones;
            _effects = effects?.Clone() ?? new EffectChainData();
        }

        public void SetTiming(long startTick, long durationTicks)
        {
            if (durationTicks < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(durationTicks));
            }

            _startTick = startTick;
            _durationTicks = durationTicks;
        }

        public void SetPerformance(float velocity, float pitchSemitones)
        {
            _velocity = Mathf.Clamp01(velocity);
            _pitchSemitones = pitchSemitones;
        }
    }
}
