using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class SampleModule : SoundModule
{
    public enum HeldSampleMode
    {
        // Plays from the start; stops immediately when the key is released
        PlayFullNoLoop,
        // Loops the entire clip while the key is held
        LoopWholeClip,
        // Attempts to auto-detect a seamless loop region and loops that while held
        AutoLoopRegion
    }

    [SerializeField] private bool _debugLogs = false;
    
    [Header("Recording (optional)")]
    [SerializeField] private Button _recordButton;
    [SerializeField] private string _preferredMicrophoneName;
    [SerializeField] private int _maxRecordingSeconds = 30;
    [SerializeField] private bool _trimLeadingSilence = true;
    [SerializeField] private bool _trimTrailingSilence = true;
    [SerializeField] private float _silenceThreshold = 0.01f;
    [SerializeField] private int _minSilenceLength = 100;
    [Tooltip("Consecutive non-silent frames required before we consider the audio to have 'started'. Helps ignore small noise spikes at the beginning.")]
    [SerializeField] private int _minSoundLength = 20;

    [Header("Auto Loop (for held notes)")]
    [SerializeField] private HeldSampleMode _heldSampleMode = HeldSampleMode.AutoLoopRegion;
    [SerializeField] private bool _autoLoopRecordedSamples = true;
    [Tooltip("If auto-loop can't find good points, loop the whole clip instead of not looping.")]
    [SerializeField] private bool _fallbackLoopWholeClip = true;
    [Tooltip("Window size used to compare loop boundaries, in milliseconds.")]
    [SerializeField] private int _loopMatchWindowMs = 40;
    [Tooltip("Minimum loop length, in milliseconds.")]
    [SerializeField] private int _minLoopLengthMs = 120;
    [Tooltip("Maximum loop length, in milliseconds.")]
    [SerializeField] private int _maxLoopLengthMs = 1200;
    [Tooltip("Higher = stricter (fewer loops found). Lower = more loops found but may click.")]
    [SerializeField] private float _maxLoopMatchMse = 0.0025f;

    [Header("Waveform Debug (optional)")]
    [SerializeField] private LineRenderer _waveformRenderer;
    [SerializeField] private Transform _loopStartMarker;
    [SerializeField] private Transform _loopEndMarker;
    [SerializeField] private float _waveformWidth = 0.2f;
    [SerializeField] private float _waveformHeight = 0.05f;
    [SerializeField] private int _waveformPoints = 256;

    private bool _isRecording;
    private string _microphoneName;
    private AudioClip _micClip;

    private int _loopStartSample = -1;
    private int _loopEndSample = -1;
    private readonly Dictionary<AudioSource, bool> _loopingSources = new();

    protected List<string> _folderPaths;
    protected List<List<AudioClip>> _samples = new();
    protected int _currentFolderIndex = 0;
    protected int _currentSampleIndex = 0;
    protected AudioClip _currentSample;

    public string SampleFolderPath = "";
    public string StartFolder;
    public string StartSample;

    public int StartOctave = 4;
    public Note StartNote = Note.A;

    public TMP_Text SampleNameText;
    public TMP_Text FolderNameText;

    public SteamVR_Action_Boolean RDPadUp;
    public SteamVR_Action_Boolean RDPadDn;
    public SteamVR_Action_Boolean RDPadLf;
    public SteamVR_Action_Boolean RDPadRt;

    protected override void Awake()
    {
        base.Awake();

        RDPadUp.onStateDown += NextFolder;
        RDPadDn.onStateDown += PrevFolder;
        RDPadLf.onStateDown += NextSample;
        RDPadRt.onStateDown += PrevSample;
        
        ChooseMicrophone();
    }

    protected virtual void Start()
    {
        DiscoverFolderPaths();
        LoadSamples();
        SetStartIndices();
        UpdateDisplay();
        SetSample();
    }

    private void Update()
    {
        if (_loopingSources.Count == 0) return;
        if (_currentSample == null) return;

        int loopStart = _loopStartSample;
        int loopEnd = _loopEndSample;
        if (loopStart < 0 || loopEnd <= loopStart) return;

        var sources = _loopingSources.Keys.ToArray();
        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource src = sources[i];
            if (src == null || !src.isPlaying)
            {
                _loopingSources.Remove(src);
                continue;
            }

            if (!_loopingSources.TryGetValue(src, out bool shouldLoop) || !shouldLoop) continue;

            if (src.timeSamples >= loopEnd)
            {
                src.timeSamples = loopStart;
            }
        }
    }

    private void OnEnable()
    {
        if (_recordButton != null)
        {
            _recordButton.OnPress += StartRecordingFromMic;
            _recordButton.OnRelease += StopRecordingFromMic;
        }
    }

    private void OnDisable()
    {
        if (_recordButton != null)
        {
            _recordButton.OnPress -= StartRecordingFromMic;
            _recordButton.OnRelease -= StopRecordingFromMic;
        }
    }

    public override void PlaySound(Note note, int octave, float velocity, AudioSource audioSource)
    {
        PlaySample(GetPitch(note, octave), velocity, audioSource);
    }
    public override void StartSound(Note note, int octave, float velocity, AudioSource audioSource)
    {
        if (audioSource == null) return;
        if (_currentSample == null) return;

        audioSource.Stop();
        audioSource.clip = _currentSample;
        audioSource.pitch = GetPitch(note, octave);
        audioSource.volume = Mathf.Clamp01(velocity);
        audioSource.timeSamples = 0;

        if (_heldSampleMode == HeldSampleMode.PlayFullNoLoop)
        {
            audioSource.loop = false;
            audioSource.Play();
            _loopingSources.Remove(audioSource);
            return;
        }

        if (_heldSampleMode == HeldSampleMode.LoopWholeClip)
        {
            audioSource.loop = true;
            audioSource.Play();
            _loopingSources.Remove(audioSource);
            return;
        }

        // AutoLoopRegion
        audioSource.loop = false; // custom looping (sub-region) via Update()
        audioSource.Play();

        bool canLoop = _loopStartSample >= 0 && _loopEndSample > _loopStartSample;
        if (!canLoop && _fallbackLoopWholeClip)
        {
            _loopStartSample = 0;
            _loopEndSample = _currentSample.samples;
            canLoop = true;
        }

        _loopingSources[audioSource] = canLoop;
    }

    public override void PlaySound(Note note, int octave, float velocity)
    {
        PlaySample(GetPitch(note, octave), velocity);
    }
    public override void StartSound(Note note, int octave, float velocity)
    {
        StartSound(note, octave, velocity, _audioSource);
    }

    public override void StopSound()
    {
        StopSound(_audioSource);
    }

    public override void StopSound(AudioSource audioSource)
    {
        if (audioSource == null) return;
        audioSource.loop = false;
        audioSource.Stop();
        _loopingSources.Remove(audioSource);
    }



    public void SetSampleFolderName(string name)
    {
        if (_debugLogs) Debug.Log("Sound Module - Set Sample Folder Name");
        SampleFolderPath = name;
    }
    protected void DiscoverFolderPaths()
    {
        string samplePath = GetSamplePath();
        if (_debugLogs) Debug.Log($"Sample Module - Discover Paths: {samplePath}");
        _folderPaths = Directory.GetDirectories(samplePath).ToList();
    }
    protected string GetSamplePath()
    {
        return $"{Application.dataPath}/Resources/{SampleFolderPath}/";
    }
    protected void LoadSamples()
    {
        if (_debugLogs) Debug.Log("Sample Module - Load Samples");
        _samples = new List<List<AudioClip>>();

        for (int i = 0; i < _folderPaths.Count; i++)
        {
            string samplePath = _folderPaths[i].Split("/Resources/")[1] + "/";
            AudioClip[] folderSamples = Resources.LoadAll<AudioClip>(samplePath);
            _samples.Add(new List<AudioClip>(folderSamples));
        }
    }
    protected void SetStartIndices()
    {
        if (_debugLogs) Debug.Log($"Sample Module - Set Start Indices");
        _currentFolderIndex = _folderPaths.IndexOf(GetSamplePath() + StartFolder);
        if (_currentFolderIndex == -1)
        {
            _currentFolderIndex = 0;
            return;
        }
        
        AudioClip clip = _samples[_currentFolderIndex].FirstOrDefault(c => c.name == StartSample);
        if (clip != null)
            _currentSampleIndex = _samples[_currentFolderIndex].IndexOf(clip);
    }
    protected void SwitchFolder(int direction)
    {
        if (_debugLogs) Debug.Log($"Sample Module - Switch folder {direction}");
        _currentFolderIndex += direction;
        if (_currentFolderIndex < 0)
            _currentFolderIndex = _samples.Count - 1;
        if (_currentFolderIndex >= _samples.Count)
            _currentFolderIndex = 0;
        _currentSampleIndex = 0;
        UpdateDisplay();
        SetSample();
    }
    protected void SwitchSample(int direction)
    {
        if (_debugLogs) Debug.Log($"Sample Module - Switch sample {direction}");
        _currentSampleIndex += direction;
        if (_currentSampleIndex < 0)
            _currentSampleIndex = _samples[_currentFolderIndex].Count - 1;
        if (_currentSampleIndex >= _samples[_currentFolderIndex].Count)
            _currentSampleIndex = 0;
        UpdateDisplay();
        SetSample();
    }
    protected void UpdateDisplay()
    {
        string folderName = _folderPaths[_currentFolderIndex].Split($"{SampleFolderPath}/")[1];
        string sampleName = _samples[_currentFolderIndex][_currentSampleIndex].name;
        if (_debugLogs) Debug.Log($"Sample Module - Update Display: {folderName}/{sampleName}");
        FolderNameText.text = folderName;
        SampleNameText.text = sampleName;
    }

    protected void NextFolder(SteamVR_Action_Boolean action, SteamVR_Input_Sources source)
    {
        if (!!_interactable && !_interactable.attachedToHand) return;
        SwitchFolder(1);
        PlaySample(GetPitch(StartNote, StartOctave), 0.5f);
    }
    protected void PrevFolder(SteamVR_Action_Boolean action, SteamVR_Input_Sources source)
    {
        if (!!_interactable && !_interactable.attachedToHand) return;
        SwitchFolder(-1);
        PlaySample(GetPitch(StartNote, StartOctave), 0.5f);
    }
    protected void NextSample(SteamVR_Action_Boolean action, SteamVR_Input_Sources source)
    {
        if (!!_interactable && !_interactable.attachedToHand) return;
        SwitchSample(1);
        PlaySample(GetPitch(StartNote, StartOctave), 0.5f);
    }
    protected void PrevSample(SteamVR_Action_Boolean action, SteamVR_Input_Sources source)
    {
        if (!!_interactable && !_interactable.attachedToHand) return;
        SwitchSample(-1);
        PlaySample(GetPitch(StartNote, StartOctave), 0.5f);
    }

    public virtual void SetSample()
    {
        if (_debugLogs) Debug.Log($"Sample Module - Set Sample: {_currentFolderIndex}, {_currentSampleIndex}");
        _currentSample = _samples[_currentFolderIndex][_currentSampleIndex];
        _loopStartSample = -1;
        _loopEndSample = -1;
        UpdateWaveformDebug(_currentSample);
    }

    // Override the current sample with a freshly recorded clip.
    public void SetRecordedSample(AudioClip clip)
    {
        _currentSample = clip;
        if (clip == null) return;

        if (SampleNameText != null)
        {
            SampleNameText.text = clip.name;
        }
        if (FolderNameText != null)
        {
            FolderNameText.text = "Recorded";
        }

        if (_autoLoopRecordedSamples && _heldSampleMode == HeldSampleMode.AutoLoopRegion)
        {
            AutoDetectLoopPoints(_currentSample);
        }
        else
        {
            _loopStartSample = -1;
            _loopEndSample = -1;
        }

        UpdateWaveformDebug(_currentSample);
    }

    public void StartRecordingFromMic()
    {
        if (_isRecording) return;

        ChooseMicrophone();
        if (string.IsNullOrEmpty(_microphoneName))
        {
            Debug.LogWarning("SampleModule: no microphone available for recording");
            return;
        }

        int frequency = AudioSettings.outputSampleRate;
        int lengthSec = Mathf.Max(1, _maxRecordingSeconds);
        _micClip = Microphone.Start(_microphoneName, false, lengthSec, frequency);
        _isRecording = true;
        if (_debugLogs) Debug.Log($"SampleModule: recording started on '{_microphoneName}'");
    }

    public void StopRecordingFromMic()
    {
        if (!_isRecording) return;
        _isRecording = false;

        if (string.IsNullOrEmpty(_microphoneName))
        {
            Debug.LogWarning("SampleModule: stop recording called but no microphone was selected");
            return;
        }

        int position = Microphone.GetPosition(_microphoneName);
        Microphone.End(_microphoneName);

        if (_micClip == null || position <= 0)
        {
            Debug.LogWarning("SampleModule: recording produced no audio");
            return;
        }

        int channels = _micClip.channels;
        float[] data = new float[position * channels];
        _micClip.GetData(data, 0);

        AudioClip clip = AudioClip.Create("RecordedSample", position, channels, _micClip.frequency, false);
        clip.SetData(data, 0);

        clip = TrimSilence(clip);

        SetRecordedSample(clip);
        if (_debugLogs) Debug.Log($"SampleModule: recording stopped, samples: {position}");
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

            // Only trim if we actually had a meaningful silence run at the end
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

    private void AutoDetectLoopPoints(AudioClip clip)
    {
        _loopStartSample = -1;
        _loopEndSample = -1;
        if (clip == null) return;

        int totalFrames = clip.samples;
        if (totalFrames < 2048) return;

        float[] mono = GetMonoFrames(clip);

        int window = Mathf.Max(256, Mathf.RoundToInt(clip.frequency * (_loopMatchWindowMs / 1000f)));
        int minLoop = Mathf.Max(window * 2, Mathf.RoundToInt(clip.frequency * (_minLoopLengthMs / 1000f)));
        int maxLoop = Mathf.Min(totalFrames - window * 2, Mathf.RoundToInt(clip.frequency * (_maxLoopLengthMs / 1000f)));
        if (maxLoop <= minLoop) return;

        int attackMargin = Mathf.Min(totalFrames / 5, clip.frequency / 10); // ~100ms or 20% of clip
        int tailMargin = Mathf.Min(totalFrames / 10, clip.frequency / 20);  // ~50ms or 10% of clip

        float bestScore = float.PositiveInfinity;
        int bestStart = -1;
        int bestEnd = -1;

        int endMin = Mathf.Clamp(attackMargin + minLoop, 0, totalFrames - window - 1);
        int endMax = Mathf.Clamp(totalFrames - tailMargin - window, endMin, totalFrames - window - 1);

        int step = Mathf.Max(64, window / 8);

        for (int end = endMin; end <= endMax; end += step)
        {
            int startMin = attackMargin;
            int startMax = end - minLoop;
            if (startMax <= startMin) continue;

            if (end - startMin > maxLoop) startMin = end - maxLoop;

            for (int start = startMin; start <= startMax; start += step)
            {
                int loopLength = end - start;
                if (loopLength < minLoop || loopLength > maxLoop) continue;

                float mse = BoundaryMse(mono, start, end, window);
                float boundaryPenalty = Mathf.Abs(mono[start]) + Mathf.Abs(mono[end]);
                float score = mse + boundaryPenalty * 0.01f;

                if (score < bestScore)
                {
                    bestScore = score;
                    bestStart = start;
                    bestEnd = end;
                }
            }
        }

        if (bestStart < 0 || bestEnd <= bestStart) return;
        if (bestScore > _maxLoopMatchMse) return;

        _loopStartSample = bestStart;
        _loopEndSample = bestEnd;
    }

    private float[] GetMonoFrames(AudioClip clip)
    {
        int frames = clip.samples;
        int channels = clip.channels;
        float[] data = new float[frames * channels];
        clip.GetData(data, 0);

        if (channels == 1) return data;

        float[] mono = new float[frames];
        for (int i = 0; i < frames; i++)
        {
            float sum = 0f;
            int baseIdx = i * channels;
            for (int c = 0; c < channels; c++) sum += data[baseIdx + c];
            mono[i] = sum / channels;
        }
        return mono;
    }

    private float BoundaryMse(float[] mono, int start, int end, int window)
    {
        int maxA = Mathf.Min(mono.Length, start + window);
        int maxB = Mathf.Min(mono.Length, end + window);
        int len = Mathf.Min(maxA - start, maxB - end);
        if (len <= 0) return float.PositiveInfinity;

        float acc = 0f;
        for (int i = 0; i < len; i++)
        {
            float d = mono[start + i] - mono[end + i];
            acc += d * d;
        }

        return acc / len;
    }

    private void UpdateWaveformDebug(AudioClip clip)
    {
        if (_waveformRenderer == null || clip == null) return;

        int points = Mathf.Clamp(_waveformPoints, 32, 2048);
        _waveformRenderer.positionCount = points;

        float[] mono = GetMonoFrames(clip);
        int frames = mono.Length;

        for (int i = 0; i < points; i++)
        {
            float t = points == 1 ? 0f : (float)i / (points - 1);
            int idx = Mathf.Clamp(Mathf.RoundToInt(t * (frames - 1)), 0, frames - 1);
            float x = (t - 0.5f) * _waveformWidth;
            float y = mono[idx] * _waveformHeight;
            _waveformRenderer.SetPosition(i, new Vector3(x, y, 0f));
        }

        if (_loopStartMarker != null && _loopStartSample >= 0)
        {
            float t = (float)_loopStartSample / Mathf.Max(1, clip.samples);
            _loopStartMarker.localPosition = new Vector3((t - 0.5f) * _waveformWidth, 0f, 0f);
        }
        if (_loopEndMarker != null && _loopEndSample >= 0)
        {
            float t = (float)_loopEndSample / Mathf.Max(1, clip.samples);
            _loopEndMarker.localPosition = new Vector3((t - 0.5f) * _waveformWidth, 0f, 0f);
        }
    }

    public void PlaySample(float pitch, float velocity, AudioSource audioSource)
    {
        if (_debugLogs) Debug.Log($"Sample Module - Play Sample: {velocity}, {pitch}");
        audioSource.pitch = pitch;
        PlaySample(velocity, audioSource);
    }
    public void PlaySample(float pitch, float velocity)
    {
        if (_debugLogs) Debug.Log($"Sample Module - Play Sample: {velocity}, {pitch}");
        _audioSource.pitch = pitch;
        PlaySample(velocity);
    }
    public void PlaySample(float velocity)
    {
        PlaySample(velocity, _audioSource);
    }
    public void PlaySample(float velocity, AudioSource audioSource)
    {
        if (_debugLogs) Debug.Log($"Sample Module - Play Sample: {velocity}");
        if (_currentSample == null)
        {
            Debug.LogError($"Audio Sample not found for Sampler in {_instrumentName}");
            return;
        };
        if (audioSource == null)
        {
            Debug.LogError($"Audio Source not found for Sampler in {_instrumentName}");
            return;
        };
        audioSource.PlayOneShot(_currentSample, velocity);
    }
}
