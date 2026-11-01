using System;
using UnityEngine;
using Valve.VR;

namespace MusicPractice.SpatialLoop
{
    public sealed class SampleCaptureService : MonoBehaviour
    {
        [SerializeField] private string _preferredMicrophoneName;
        [SerializeField, Min(1)] private int _maximumRecordingSeconds = 30;
        [SerializeField] private bool _trimLeadingSilence = true;
        [SerializeField] private bool _trimTrailingSilence = true;
        [SerializeField, Min(0f)] private float _silenceThreshold = 0.01f;
        [SerializeField, Min(1)] private int _minimumSilenceFrames = 100;
        [SerializeField, Min(1)] private int _minimumSoundFrames = 20;
        [SerializeField] private bool _monitorWhileRecording = true;

        private string _microphoneName;
        private AudioClip _microphoneClip;
        private SteamVR_Input_Sources _recordingSource;
        private EffectChainData _effectSnapshot;
        private bool _isRecording;
        private int _sampleNumber;
        private AudioSource _monitorSource;
        private bool _monitorPending;

        public bool IsRecording => _isRecording;
        public SteamVR_Input_Sources RecordingSource => _recordingSource;
        public event Action<SampleAssetData, SteamVR_Input_Sources> SampleCaptured;
        public event Action<SteamVR_Input_Sources, bool> RecordingStateChanged;

        private void Update()
        {
            if (_monitorPending
                && !string.IsNullOrEmpty(_microphoneName)
                && Microphone.GetPosition(_microphoneName) > 0)
            {
                _monitorSource.Play();
                _monitorPending = false;
            }
        }

        public bool StartCapture(
            SteamVR_Input_Sources source,
            EffectChainData microphoneEffects)
        {
            if (_isRecording)
            {
                return false;
            }

            ChooseMicrophone();
            if (string.IsNullOrEmpty(_microphoneName))
            {
                Debug.LogWarning("SampleCaptureService: no microphone is available.", this);
                return false;
            }

            int frequency = AudioSettings.outputSampleRate;
            _microphoneClip = Microphone.Start(
                _microphoneName,
                false,
                Mathf.Max(1, _maximumRecordingSeconds),
                frequency);
            _recordingSource = source;
            _effectSnapshot = microphoneEffects?.Clone() ?? new EffectChainData();
            _isRecording = true;
            StartMonitoring();
            RecordingStateChanged?.Invoke(source, true);
            return true;
        }

        public SampleAssetData StopCapture(SteamVR_Input_Sources source)
        {
            if (!_isRecording || source != _recordingSource)
            {
                return null;
            }

            _isRecording = false;
            int position = Microphone.GetPosition(_microphoneName);
            Microphone.End(_microphoneName);
            StopMonitoring();
            RecordingStateChanged?.Invoke(source, false);

            if (_microphoneClip == null || position <= 0)
            {
                Debug.LogWarning("SampleCaptureService: recording produced no audio.", this);
                return null;
            }

            int channels = _microphoneClip.channels;
            var data = new float[position * channels];
            _microphoneClip.GetData(data, 0);

            AudioClip clip = AudioClip.Create(
                $"Recorded Sample {_sampleNumber + 1:00}",
                position,
                channels,
                _microphoneClip.frequency,
                false);
            clip.SetData(data, 0);
            clip = TrimSilence(clip);

            _sampleNumber++;
            Color color = Color.HSVToRGB((_sampleNumber * 0.173f) % 1f, 0.8f, 1f);
            var asset = new SampleAssetData(
                $"Sample {_sampleNumber:00}",
                clip,
                color,
                _effectSnapshot);
            SampleCaptured?.Invoke(asset, source);
            return asset;
        }

        public void UpdateMonitorEffects(EffectChainData effects)
        {
            if (_monitorSource != null && _monitorSource.isPlaying)
            {
                SpatialEffectsController.ApplyToAudioSource(
                    _monitorSource,
                    effects);
            }
        }

        private void StartMonitoring()
        {
            if (!_monitorWhileRecording)
            {
                return;
            }

            if (_monitorSource == null)
            {
                _monitorSource = gameObject.AddComponent<AudioSource>();
                _monitorSource.playOnAwake = false;
                _monitorSource.loop = true;
                _monitorSource.spatialBlend = 0f;
            }

            _monitorSource.clip = _microphoneClip;
            SpatialEffectsController.ApplyToAudioSource(
                _monitorSource,
                _effectSnapshot);
            _monitorPending = true;
        }

        private void StopMonitoring()
        {
            _monitorPending = false;
            if (_monitorSource != null)
            {
                _monitorSource.Stop();
                _monitorSource.clip = null;
            }
        }

        private void ChooseMicrophone()
        {
            if (Microphone.devices.Length == 0)
            {
                _microphoneName = null;
                return;
            }

            if (!string.IsNullOrEmpty(_preferredMicrophoneName)
                && Array.Exists(
                    Microphone.devices,
                    device => device == _preferredMicrophoneName))
            {
                _microphoneName = _preferredMicrophoneName;
                return;
            }

            _microphoneName = Microphone.devices[0];
        }

        private AudioClip TrimSilence(AudioClip clip)
        {
            if (!_trimLeadingSilence && !_trimTrailingSilence)
            {
                return clip;
            }

            var samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            int start = _trimLeadingSilence
                ? FindSoundStart(samples, clip.channels)
                : 0;
            int end = _trimTrailingSilence
                ? FindSoundEndExclusive(samples, clip.channels)
                : samples.Length;

            if (end <= start)
            {
                return clip;
            }

            int length = end - start;
            var trimmed = new float[length];
            Array.Copy(samples, start, trimmed, 0, length);

            AudioClip result = AudioClip.Create(
                clip.name + " Trimmed",
                length / clip.channels,
                clip.channels,
                clip.frequency,
                false);
            result.SetData(trimmed, 0);
            return result;
        }

        private int FindSoundStart(float[] samples, int channels)
        {
            int silentFrames = 0;
            int soundFrames = 0;

            for (int i = 0; i < samples.Length; i += channels)
            {
                if (IsFrameSilent(samples, i, channels))
                {
                    silentFrames++;
                    soundFrames = 0;
                    continue;
                }

                soundFrames++;
                if (soundFrames < _minimumSoundFrames)
                {
                    continue;
                }

                int start = i - (soundFrames - 1) * channels;
                return silentFrames > 0 ? Mathf.Max(0, start) : 0;
            }

            return 0;
        }

        private int FindSoundEndExclusive(float[] samples, int channels)
        {
            int silentFrames = 0;
            for (int i = samples.Length - channels; i >= 0; i -= channels)
            {
                if (IsFrameSilent(samples, i, channels))
                {
                    silentFrames++;
                    continue;
                }

                return silentFrames >= _minimumSilenceFrames
                    ? i + channels
                    : samples.Length;
            }

            return samples.Length;
        }

        private bool IsFrameSilent(float[] samples, int frameStart, int channels)
        {
            for (int channel = 0; channel < channels; channel++)
            {
                if (Mathf.Abs(samples[frameStart + channel]) > _silenceThreshold)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
