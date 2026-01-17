using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SampleRecorder : MonoBehaviour
{
    [SerializeField] private float _maxSampleLength = 0.5f; // Maximum length of a sample in seconds
    [SerializeField] private int _sampleRate = 44100;
    [SerializeField] private float _silenceThreshold = 0.01f; // Threshold for detecting silence
    [SerializeField] private int _minSilenceLength = 100; // Minimum number of samples to consider as silence
    [SerializeField] private string _preferredMicrophoneName;
    
    private AudioClip _currentRecording;
    private float _recordingStartTime;
    private bool _isRecording = false;
    private int _selectedChannel = -1;
    private int _selectedBeat = -1;
    private string _microphoneName;
    private AudioClip _micClip;
    
    private Dictionary<int, Dictionary<int, AudioClip>> _samples = new(); // channel -> (beat -> sample)
    
    public event System.Action<int, int> OnSampleRecorded; // channel, beat

    private void Start()
    {
        ChooseMicrophone();

        // Initialize sample storage for each channel
        for (int i = 0; i < 5; i++) // 5 channels
        {
            _samples[i] = new Dictionary<int, AudioClip>();
        }
    }

    public void SelectChannel(int channel)
    {
        _selectedChannel = channel;
        Debug.Log($"Selected channel {channel} for recording");
    }

    public void SelectBeat(int beat)
    {
        _selectedBeat = beat;
        Debug.Log($"Selected beat {beat} for recording");
    }

    public void StartRecording()
    {
        if (_selectedChannel == -1 || _selectedBeat == -1)
        {
            Debug.LogWarning("Please select a channel and beat before recording");
            return;
        }

        if (_isRecording) return;

        _isRecording = true;
        _recordingStartTime = Time.time;
        
        ChooseMicrophone();
        if (string.IsNullOrEmpty(_microphoneName))
        {
            Debug.LogWarning("No microphone available for recording");
            _isRecording = false;
            return;
        }

        int lengthSec = Mathf.Max(1, Mathf.CeilToInt(_maxSampleLength));
        _micClip = Microphone.Start(_microphoneName, false, lengthSec, _sampleRate);
        
        Debug.Log($"Started recording for channel {_selectedChannel}, beat {_selectedBeat}");
    }

    public void StopRecording()
    {
        if (!_isRecording) return;

        _isRecording = false;
        float recordingDuration = Time.time - _recordingStartTime;
        
        if (!string.IsNullOrEmpty(_microphoneName))
        {
            int position = Microphone.GetPosition(_microphoneName);
            Microphone.End(_microphoneName);

            if (_micClip != null && position > 0)
            {
                int channels = _micClip.channels;
                float[] data = new float[position * channels];
                _micClip.GetData(data, 0);

                _currentRecording = AudioClip.Create("RecordedSample", position, channels, _micClip.frequency, false);
                _currentRecording.SetData(data, 0);
            }
        }
        
        if (_currentRecording != null)
        {
            // Trim leading/trailing silence
            _currentRecording = TrimSilence(_currentRecording);
            
            // Store the sample
            _samples[_selectedChannel][_selectedBeat] = _currentRecording;
            OnSampleRecorded?.Invoke(_selectedChannel, _selectedBeat);
            Debug.Log($"Recorded sample for channel {_selectedChannel}, beat {_selectedBeat}");
        }
    }

    private void ChooseMicrophone()
    {
        if (Microphone.devices.Length == 0)
        {
            _microphoneName = null;
            return;
        }

        if (!string.IsNullOrEmpty(_preferredMicrophoneName) &&
            Microphone.devices.Contains(_preferredMicrophoneName))
        {
            _microphoneName = _preferredMicrophoneName;
        }
        else
        {
            _microphoneName = Microphone.devices[0];
        }
    }

    private AudioClip TrimSilence(AudioClip clip)
    {
        // Get the audio data
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        int startIndex = FindSoundStart(samples, clip.channels);
        int endIndexExclusive = FindSoundEndExclusive(samples, clip.channels);
        if (endIndexExclusive <= startIndex) return clip;

        // Create a new clip without the silence
        int newLength = endIndexExclusive - startIndex;
        float[] trimmedSamples = new float[newLength];
        System.Array.Copy(samples, startIndex, trimmedSamples, 0, newLength);

        AudioClip trimmedClip = AudioClip.Create(
            clip.name + "_trimmed",
            newLength / clip.channels,
            clip.channels,
            clip.frequency,
            false
        );
        
        trimmedClip.SetData(trimmedSamples, 0);
        return trimmedClip;
    }

    private int FindSoundStart(float[] samples, int channels)
    {
        int silentFrames = 0;
        for (int i = 0; i < samples.Length; i += channels)
        {
            if (IsFrameSilent(samples, i, channels))
            {
                silentFrames++;
                continue;
            }

            if (silentFrames >= _minSilenceLength) return i;
            return 0;
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

            if (silentFrames >= _minSilenceLength) return i + channels;
            return samples.Length;
        }

        return samples.Length;
    }

    private bool IsFrameSilent(float[] samples, int frameStart, int channels)
    {
        for (int channel = 0; channel < channels; channel++)
        {
            if (Mathf.Abs(samples[frameStart + channel]) > _silenceThreshold) return false;
        }
        return true;
    }

    public AudioClip GetSample(int channel, int beat)
    {
        if (_samples.TryGetValue(channel, out var channelSamples) &&
            channelSamples.TryGetValue(beat, out var sample))
        {
            return sample;
        }
        return null;
    }

    public bool HasSample(int channel, int beat)
    {
        return _samples.ContainsKey(channel) && 
               _samples[channel].ContainsKey(beat);
    }

    public void ClearSample(int channel, int beat)
    {
        if (_samples.ContainsKey(channel))
        {
            _samples[channel].Remove(beat);
        }
    }

    public void ClearChannel(int channel)
    {
        if (_samples.ContainsKey(channel))
        {
            _samples[channel].Clear();
        }
    }
} 