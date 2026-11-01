using System;
using UnityEngine;

public class SamplePad : MonoBehaviour
{
    [Serializable]
    public class SampleSlot
    {
        public string DisplayName;
        public AudioClip Clip;

        public bool HasClip => Clip != null;
    }

    public static SamplePad Instance { get; private set; }

    [Header("Pad Setup")]
    [SerializeField] private SampleButton[] _buttons;
    [SerializeField] private Button _recordButton;
    [SerializeField] private int _slotCount = 16;

    [Header("Recording")]
    [SerializeField] private string _preferredMicrophoneName;
    [SerializeField] private int _maxRecordingSeconds = 30;
    [SerializeField] private bool _trimLeadingSilence = true;
    [SerializeField] private bool _trimTrailingSilence = true;
    [SerializeField] private float _silenceThreshold = 0.01f;
    [SerializeField] private int _minSilenceLength = 100;
    [SerializeField] private int _minSoundLength = 20;

    private SampleSlot[] _slots;
    private int _selectedSlotIndex = -1;
    private bool _isRecording;
    private string _microphoneName;
    private AudioClip _micClip;

    public event Action<int> OnSlotChanged;
    public event Action<int> OnSelectionChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple SamplePads found in scene. Using the latest one.");
        }
        Instance = this;

        _slots = new SampleSlot[Mathf.Max(1, _slotCount)];
        for (int i = 0; i < _slots.Length; i++)
        {
            _slots[i] = new SampleSlot { DisplayName = $"Slot {i + 1:00}" };
        }

        if (_buttons == null || _buttons.Length == 0)
        {
            _buttons = GetComponentsInChildren<SampleButton>(true);
        }

        for (int i = 0; i < _buttons.Length; i++)
        {
            if (_buttons[i] == null) continue;
            _buttons[i].Initialize(this, i);
        }

        if (_recordButton != null)
        {
            _recordButton.OnPress += StartRecording;
            _recordButton.OnRelease += StopRecording;
        }

        ChooseMicrophone();
        RefreshButtons();
    }

    private void OnDestroy()
    {
        if (_recordButton != null)
        {
            _recordButton.OnPress -= StartRecording;
            _recordButton.OnRelease -= StopRecording;
        }

        if (Instance == this) Instance = null;
    }

    public void ToggleSelection(int slotIndex)
    {
        if (!IsValidSlot(slotIndex)) return;

        _selectedSlotIndex = _selectedSlotIndex == slotIndex ? -1 : slotIndex;
        OnSelectionChanged?.Invoke(_selectedSlotIndex);
        RefreshButtons();
    }

    public int GetSelectedSlotIndex()
    {
        return _selectedSlotIndex;
    }

    public bool HasSelectedSlot()
    {
        return IsValidSlot(_selectedSlotIndex);
    }

    public bool TryGetSelectedSlot(out int slotIndex)
    {
        slotIndex = _selectedSlotIndex;
        return IsValidSlot(slotIndex);
    }

    public AudioClip GetClip(int slotIndex)
    {
        return IsValidSlot(slotIndex) ? _slots[slotIndex].Clip : null;
    }

    public string GetDisplayName(int slotIndex)
    {
        if (!IsValidSlot(slotIndex)) return string.Empty;
        return _slots[slotIndex].DisplayName;
    }

    public bool SlotHasClip(int slotIndex)
    {
        return IsValidSlot(slotIndex) && _slots[slotIndex].HasClip;
    }

    public void SetSlotClip(int slotIndex, AudioClip clip, string displayName = null)
    {
        if (!IsValidSlot(slotIndex)) return;

        _slots[slotIndex].Clip = clip;
        _slots[slotIndex].DisplayName = string.IsNullOrWhiteSpace(displayName)
            ? $"Slot {slotIndex + 1:00}"
            : displayName;

        OnSlotChanged?.Invoke(slotIndex);
        RefreshButtons();
    }

    public int GetNextEmptySlotIndex()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (!_slots[i].HasClip) return i;
        }
        return -1;
    }

    private void RefreshButtons()
    {
        for (int i = 0; i < _buttons.Length; i++)
        {
            if (_buttons[i] == null) continue;

            bool selected = i == _selectedSlotIndex;
            bool occupied = i < _slots.Length && _slots[i].HasClip;
            string name = i < _slots.Length ? _slots[i].DisplayName : $"Slot {i + 1:00}";
            _buttons[i].Refresh(selected, occupied, name);
        }
    }

    private void StartRecording()
    {
        if (_isRecording) return;

        ChooseMicrophone();
        if (string.IsNullOrEmpty(_microphoneName))
        {
            Debug.LogWarning("SamplePad: no microphone available for recording");
            return;
        }

        int frequency = AudioSettings.outputSampleRate;
        int lengthSec = Mathf.Max(1, _maxRecordingSeconds);
        _micClip = Microphone.Start(_microphoneName, false, lengthSec, frequency);
        _isRecording = true;
    }

    private void StopRecording()
    {
        if (!_isRecording) return;
        _isRecording = false;

        if (string.IsNullOrEmpty(_microphoneName))
        {
            Debug.LogWarning("SamplePad: stop recording called but no microphone was selected");
            return;
        }

        int position = Microphone.GetPosition(_microphoneName);
        Microphone.End(_microphoneName);

        if (_micClip == null || position <= 0)
        {
            Debug.LogWarning("SamplePad: recording produced no audio");
            return;
        }

        int channels = _micClip.channels;
        float[] data = new float[position * channels];
        _micClip.GetData(data, 0);

        AudioClip clip = AudioClip.Create("RecordedSample", position, channels, _micClip.frequency, false);
        clip.SetData(data, 0);
        clip = TrimSilence(clip);

        int targetSlot = _selectedSlotIndex;
        if (!IsValidSlot(targetSlot))
        {
            targetSlot = GetNextEmptySlotIndex();
        }

        if (!IsValidSlot(targetSlot))
        {
            Debug.LogWarning("SamplePad: no empty slots available and no slot selected");
            return;
        }

        string slotName = $"Sample {targetSlot + 1:00}";
        SetSlotClip(targetSlot, clip, slotName);
        _selectedSlotIndex = targetSlot;
        OnSelectionChanged?.Invoke(_selectedSlotIndex);
        RefreshButtons();
    }

    private void ChooseMicrophone()
    {
        if (Microphone.devices.Length == 0)
        {
            _microphoneName = null;
            return;
        }

        if (!string.IsNullOrEmpty(_preferredMicrophoneName) &&
            Array.Exists(Microphone.devices, d => d == _preferredMicrophoneName))
        {
            _microphoneName = _preferredMicrophoneName;
        }
        else
        {
            _microphoneName = Microphone.devices[0];
        }
    }

    private bool IsValidSlot(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < _slots.Length;
    }

    private AudioClip TrimSilence(AudioClip clip)
    {
        if (!_trimLeadingSilence && !_trimTrailingSilence) return clip;

        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        int startIndex = 0;
        int endIndexExclusive = samples.Length;

        if (_trimLeadingSilence)
        {
            startIndex = FindSoundStart(samples, clip.channels);
        }
        if (_trimTrailingSilence)
        {
            endIndexExclusive = FindSoundEndExclusive(samples, clip.channels);
        }

        if (endIndexExclusive <= startIndex) return clip;

        int newLength = endIndexExclusive - startIndex;
        float[] trimmedSamples = new float[newLength];
        Array.Copy(samples, startIndex, trimmedSamples, 0, newLength);

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
        int soundFrames = 0;
        int minSound = Mathf.Max(1, _minSoundLength);

        for (int i = 0; i < samples.Length; i += channels)
        {
            if (IsFrameSilent(samples, i, channels))
            {
                silentFrames++;
                soundFrames = 0;
                continue;
            }

            soundFrames++;
            if (soundFrames < minSound) continue;

            int start = i - ((soundFrames - 1) * channels);
            if (silentFrames > 0) return Mathf.Max(0, start);
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
}
