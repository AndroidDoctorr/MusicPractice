using System;
using System.Collections.Generic;
using UnityEngine;

namespace MusicPractice.SpatialLoop
{
    public readonly struct SamplePlaybackEvent
    {
        public long Tick { get; }
        public AudioClip Clip { get; }
        public float Velocity { get; }
        public float PitchSemitones { get; }
        public EffectChainData Effects { get; }

        public SamplePlaybackEvent(
            long tick,
            AudioClip clip,
            float velocity,
            float pitchSemitones,
            EffectChainData effects)
        {
            Tick = tick;
            Clip = clip;
            Velocity = velocity;
            PitchSemitones = pitchSemitones;
            Effects = effects;
        }
    }

    [RequireComponent(typeof(LoopTransport))]
    public sealed class DemoLoopScheduler : MonoBehaviour
    {
        [SerializeField] private LoopTransport _transport;
        [SerializeField] private AudioClip _demoClip;
        [SerializeField, Range(0f, 1f)] private float _velocity = 0.8f;
        [SerializeField, Min(1)] private int _voiceCount = 8;
        [SerializeField, Min(0.02f)] private double _lookAheadSeconds = 0.1d;
        [SerializeField, Min(0.005f)] private double _minimumScheduleLeadSeconds = 0.02d;
        [SerializeField] private bool _playAtMeterGroupStarts = true;

        private readonly List<AudioSource> _voices = new List<AudioSource>();
        private readonly List<double> _voiceAvailableTimes = new List<double>();
        private readonly List<SamplePlaybackEvent> _events =
            new List<SamplePlaybackEvent>();
        private readonly List<SamplePlaybackEvent> _customEvents =
            new List<SamplePlaybackEvent>();
        private bool _useCustomEvents;
        private int _lastTransportRevision = -1;
        private int _nextEventIndex;
        private long _nextLoopIndex;
        private bool _warnedMissingClip;

        public AudioClip DemoClip => _demoClip;

        private void Reset()
        {
            _transport = GetComponent<LoopTransport>();
        }

        private void Awake()
        {
            if (_transport == null)
            {
                _transport = GetComponent<LoopTransport>();
            }

            CreateVoicePool();
        }

        private void Update()
        {
            if (_transport == null || !_transport.IsPlaying)
            {
                return;
            }

            if (!_useCustomEvents && _demoClip == null)
            {
                if (!_warnedMissingClip)
                {
                    Debug.LogWarning(
                        "DemoLoopScheduler needs a short AudioClip before it can schedule the demo pattern.",
                        this);
                    _warnedMissingClip = true;
                }

                return;
            }

            _warnedMissingClip = false;

            if (_lastTransportRevision != _transport.Revision)
            {
                ResetScheduleCursor();
            }

            double now = AudioSettings.dspTime;
            double scheduleHorizon = now + Math.Max(
                _lookAheadSeconds,
                _minimumScheduleLeadSeconds);
            double horizonTick = _transport.GetAbsoluteTick(scheduleHorizon);
            long loopLengthTicks = _transport.LoopLengthTicks;

            while (GetNextAbsoluteEventTick(loopLengthTicks) <= horizonTick)
            {
                long absoluteEventTick = GetNextAbsoluteEventTick(loopLengthTicks);
                SamplePlaybackEvent playbackEvent = _events[_nextEventIndex];
                double eventDspTime = _transport.GetDspTimeForAbsoluteTick(
                    absoluteEventTick);

                if (playbackEvent.Clip != null
                    && eventDspTime >= now + _minimumScheduleLeadSeconds)
                {
                    ScheduleClip(eventDspTime, playbackEvent);
                }

                AdvanceCursor();
            }
        }

        private void OnDisable()
        {
            StopVoices();
            _lastTransportRevision = -1;
        }

        public void RebuildPattern()
        {
            _lastTransportRevision = -1;
        }

        public void SetEventTicks(IEnumerable<long> eventTicks)
        {
            _customEvents.Clear();
            _useCustomEvents = eventTicks != null;

            if (eventTicks != null)
            {
                long loopLengthTicks = _transport.LoopLengthTicks;
                foreach (long tick in eventTicks)
                {
                    _customEvents.Add(new SamplePlaybackEvent(
                        MusicalTime.NormalizeTick(tick, loopLengthTicks),
                        _demoClip,
                        _velocity,
                        0f,
                        null));
                }

                _customEvents.Sort((a, b) => a.Tick.CompareTo(b.Tick));
            }

            RebuildPattern();
        }

        public void SetEvents(IEnumerable<SamplePlaybackEvent> playbackEvents)
        {
            _customEvents.Clear();
            _useCustomEvents = playbackEvents != null;
            if (playbackEvents != null)
            {
                long loopLengthTicks = _transport.LoopLengthTicks;
                foreach (SamplePlaybackEvent playbackEvent in playbackEvents)
                {
                    _customEvents.Add(new SamplePlaybackEvent(
                        MusicalTime.NormalizeTick(
                            playbackEvent.Tick,
                            loopLengthTicks),
                        playbackEvent.Clip,
                        playbackEvent.Velocity,
                        playbackEvent.PitchSemitones,
                        playbackEvent.Effects));
                }

                _customEvents.Sort((a, b) => a.Tick.CompareTo(b.Tick));
            }

            RebuildPattern();
        }

        public void PlayImmediate(
            AudioClip clip,
            float velocity,
            float pitchSemitones,
            EffectChainData effects)
        {
            if (clip == null)
            {
                return;
            }

            ScheduleClip(
                AudioSettings.dspTime + _minimumScheduleLeadSeconds,
                new SamplePlaybackEvent(
                    0,
                    clip,
                    velocity,
                    pitchSemitones,
                    effects));
        }

        public void UseMeterPattern()
        {
            _customEvents.Clear();
            _useCustomEvents = false;
            RebuildPattern();
        }

        private void ResetScheduleCursor()
        {
            StopVoices();
            BuildEventTicks();
            _lastTransportRevision = _transport.Revision;
            _nextEventIndex = 0;
            _nextLoopIndex = 0;

            if (_events.Count == 0)
            {
                return;
            }

            double currentAbsoluteTick = _transport.GetAbsoluteTick(AudioSettings.dspTime);
            long loopLengthTicks = _transport.LoopLengthTicks;
            _nextLoopIndex = Math.Max(
                0L,
                (long)Math.Floor(currentAbsoluteTick / loopLengthTicks));

            while (GetNextAbsoluteEventTick(loopLengthTicks) < currentAbsoluteTick)
            {
                AdvanceCursor();
            }
        }

        private void BuildEventTicks()
        {
            _events.Clear();

            if (_useCustomEvents)
            {
                _events.AddRange(_customEvents);
                return;
            }

            TimeSignatureData signature = _transport.TimeSignature;
            long ticksPerBar = MusicalTime.GetTicksPerBar(signature);
            long ticksPerUnit = MusicalTime.GetTicksPerDenominatorUnit(
                signature.Denominator);
            int[] groups = signature.GetBeatGroups();

            for (int bar = 0; bar < _transport.Bars; bar++)
            {
                long barStart = bar * ticksPerBar;

                if (!_playAtMeterGroupStarts)
                {
                    for (int unit = 0; unit < signature.Numerator; unit++)
                    {
                        _events.Add(CreateDemoEvent(
                            barStart + unit * ticksPerUnit));
                    }

                    continue;
                }

                long groupStart = barStart;
                for (int groupIndex = 0; groupIndex < groups.Length; groupIndex++)
                {
                    _events.Add(CreateDemoEvent(groupStart));
                    groupStart += groups[groupIndex] * ticksPerUnit;
                }
            }
        }

        private SamplePlaybackEvent CreateDemoEvent(long tick)
        {
            return new SamplePlaybackEvent(
                tick,
                _demoClip,
                _velocity,
                0f,
                null);
        }

        private long GetNextAbsoluteEventTick(long loopLengthTicks)
        {
            if (_events.Count == 0)
            {
                return long.MaxValue;
            }

            return _nextLoopIndex * loopLengthTicks + _events[_nextEventIndex].Tick;
        }

        private void AdvanceCursor()
        {
            if (_events.Count == 0)
            {
                return;
            }

            _nextEventIndex++;
            if (_nextEventIndex < _events.Count)
            {
                return;
            }

            _nextEventIndex = 0;
            _nextLoopIndex++;
        }

        private void CreateVoicePool()
        {
            StopVoices();
            _voices.Clear();
            _voiceAvailableTimes.Clear();

            var voiceRoot = new GameObject("Demo Voices");
            voiceRoot.transform.SetParent(transform, false);

            int count = Mathf.Max(1, _voiceCount);
            for (int i = 0; i < count; i++)
            {
                var voiceObject = new GameObject($"Voice {i + 1:00}");
                voiceObject.transform.SetParent(voiceRoot.transform, false);

                AudioSource source = voiceObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;

                _voices.Add(source);
                _voiceAvailableTimes.Add(0d);
            }
        }

        private void ScheduleClip(
            double eventDspTime,
            SamplePlaybackEvent playbackEvent)
        {
            if (_voices.Count == 0)
            {
                CreateVoicePool();
            }

            int voiceIndex = FindVoice(eventDspTime);
            AudioSource voice = _voices[voiceIndex];
            voice.Stop();
            voice.clip = playbackEvent.Clip;
            voice.pitch = Mathf.Pow(2f, playbackEvent.PitchSemitones / 12f);
            voice.volume = Mathf.Clamp01(playbackEvent.Velocity);
            SpatialEffectsController.ApplyToAudioSource(
                voice,
                playbackEvent.Effects);
            voice.PlayScheduled(eventDspTime);

            _voiceAvailableTimes[voiceIndex] = eventDspTime
                + playbackEvent.Clip.length / Mathf.Max(0.01f, Mathf.Abs(voice.pitch));
        }

        private int FindVoice(double eventDspTime)
        {
            int earliestIndex = 0;
            double earliestAvailableTime = double.MaxValue;

            for (int i = 0; i < _voices.Count; i++)
            {
                if (_voiceAvailableTimes[i] <= eventDspTime)
                {
                    return i;
                }

                if (_voiceAvailableTimes[i] < earliestAvailableTime)
                {
                    earliestAvailableTime = _voiceAvailableTimes[i];
                    earliestIndex = i;
                }
            }

            return earliestIndex;
        }

        private void StopVoices()
        {
            for (int i = 0; i < _voices.Count; i++)
            {
                if (_voices[i] != null)
                {
                    _voices[i].Stop();
                }
            }
        }

        private void OnValidate()
        {
            _voiceCount = Mathf.Max(1, _voiceCount);
            _lookAheadSeconds = Math.Max(0.02d, _lookAheadSeconds);
            _minimumScheduleLeadSeconds = Math.Max(
                0.005d,
                _minimumScheduleLeadSeconds);
        }
    }
}
