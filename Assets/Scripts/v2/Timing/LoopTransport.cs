using System;
using UnityEngine;

namespace MusicPractice.SpatialLoop
{
    public sealed class LoopTransport : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float _beatsPerMinute = 120f;
        [SerializeField, Min(1)] private int _bars = 1;
        [SerializeField] private TimeSignatureData _timeSignature = new TimeSignatureData();
        [SerializeField] private bool _playOnStart = true;
        [SerializeField, Min(0f)] private double _scheduleLeadTime = 0.1d;

        private bool _isPlaying;
        private double _dspEpoch;
        private double _absoluteTickAtEpoch;
        private int _revision;

        public float BeatsPerMinute => Mathf.Max(1f, _beatsPerMinute);
        public int Bars => Mathf.Max(1, _bars);
        public TimeSignatureData TimeSignature =>
            _timeSignature ?? (_timeSignature = new TimeSignatureData());
        public bool IsPlaying => _isPlaying;
        public int Revision => _revision;
        public long LoopLengthTicks => MusicalTime.GetLoopLengthTicks(TimeSignature, Bars);

        private void Start()
        {
            if (_playOnStart)
            {
                Play();
            }
        }

        public void Play()
        {
            if (_isPlaying)
            {
                return;
            }

            _dspEpoch = AudioSettings.dspTime + Math.Max(0d, _scheduleLeadTime);
            _isPlaying = true;
            _revision++;
        }

        public void Pause()
        {
            if (!_isPlaying)
            {
                return;
            }

            _absoluteTickAtEpoch = GetAbsoluteTick(AudioSettings.dspTime);
            _dspEpoch = AudioSettings.dspTime;
            _isPlaying = false;
            _revision++;
        }

        public void Stop()
        {
            _isPlaying = false;
            _absoluteTickAtEpoch = 0d;
            _dspEpoch = AudioSettings.dspTime;
            _revision++;
        }

        public void Restart()
        {
            _absoluteTickAtEpoch = 0d;
            _dspEpoch = AudioSettings.dspTime + Math.Max(0d, _scheduleLeadTime);
            _isPlaying = true;
            _revision++;
        }

        public void SetTempo(float beatsPerMinute)
        {
            if (beatsPerMinute < 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(beatsPerMinute));
            }

            double now = AudioSettings.dspTime;
            _absoluteTickAtEpoch = GetAbsoluteTick(now);
            _dspEpoch = now;
            _beatsPerMinute = beatsPerMinute;
            _revision++;
        }

        public void SetMeter(TimeSignatureData timeSignature, int bars)
        {
            if (timeSignature == null)
            {
                throw new ArgumentNullException(nameof(timeSignature));
            }

            if (bars < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(bars));
            }

            _timeSignature = timeSignature;
            _bars = bars;
            Restart();
        }

        public double GetAbsoluteTick(double dspTime)
        {
            if (!_isPlaying || dspTime <= _dspEpoch)
            {
                return _absoluteTickAtEpoch;
            }

            double elapsedSeconds = dspTime - _dspEpoch;
            return _absoluteTickAtEpoch + MusicalTime.SecondsToTicks(
                elapsedSeconds,
                BeatsPerMinute,
                TimeSignature.TempoBeatUnit);
        }

        public double GetLoopTick(double dspTime)
        {
            return MusicalTime.NormalizeTick(GetAbsoluteTick(dspTime), LoopLengthTicks);
        }

        public double GetLoopPhase(double dspTime)
        {
            return MusicalTime.TickToPhase(GetLoopTick(dspTime), LoopLengthTicks);
        }

        public double GetDspTimeForAbsoluteTick(double absoluteTick)
        {
            double deltaTicks = absoluteTick - _absoluteTickAtEpoch;
            return _dspEpoch + MusicalTime.TicksToSeconds(
                deltaTicks,
                BeatsPerMinute,
                TimeSignature.TempoBeatUnit);
        }

        private void OnValidate()
        {
            _beatsPerMinute = Mathf.Max(1f, _beatsPerMinute);
            _bars = Mathf.Max(1, _bars);
            _scheduleLeadTime = Math.Max(0d, _scheduleLeadTime);
        }
    }
}
