using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SequencerChannel : MonoBehaviour
{
    protected List<SequencerBeat> _beats = new();
    protected int _prevIndex = 0;
    protected int _currentIndex = 0;
    protected bool[] _pattern;
    protected SampleRecorder _sampleRecorder;
    protected bool _isRecording = false;
    protected SequencerBeat _selectedBeat;
    protected XYPadController _xyPad;

    [Header("Channel Settings")]
    public string channelName;
    [Range(0f, 1f)]
    public float volume = 1.0f;
    [Range(0.5f, 2f)]
    public float pitch = 1.0f;
    public SoundModule SoundModule;
    public Button RecordButton;
    public Button XYPadButton;

    private void Awake()
    {
        _beats = GetComponentsInChildren<SequencerBeat>().ToList();
        _pattern = new bool[16];
        _sampleRecorder = FindObjectOfType<SampleRecorder>();
        _xyPad = FindObjectOfType<XYPadController>();

        if (RecordButton != null)
        {
            RecordButton.OnPress += StartRecording;
            RecordButton.OnRelease += StopRecording;
        }

        if (XYPadButton != null)
        {
            XYPadButton.OnPress += ToggleXYPad;
        }
    }

    private void ToggleXYPad()
    {
        if (_xyPad == null)
        {
            Debug.LogWarning("No XY Pad found in scene!");
            return;
        }

        // If the pad is already linked to this channel, unlink it
        if (_xyPad.IsLinkedToChannel(this))
        {
            _xyPad.UnlinkChannel();
            XYPadButton.GetComponent<Renderer>().material.color = Color.white;
        }
        else
        {
            _xyPad.LinkToChannel(this);
            XYPadButton.GetComponent<Renderer>().material.color = Color.green;
        }
    }

    private void StartRecording()
    {
        if (_isRecording) return;

        if (_selectedBeat == null)
        {
            Debug.Log("Please select a beat first");
            return;
        }

        _isRecording = true;
        _sampleRecorder?.StartRecording();
        
        // Visual feedback
        if (RecordButton != null)
        {
            RecordButton.GetComponent<Renderer>().material.color = Color.red;
        }
        if (_selectedBeat != null)
        {
            _selectedBeat.Lit.GetComponent<Renderer>().material.color = Color.red;
        }
    }

    private void StopRecording()
    {
        if (!_isRecording) return;

        _isRecording = false;
        _sampleRecorder?.StopRecording();
        
        // Visual feedback
        if (RecordButton != null)
        {
            RecordButton.GetComponent<Renderer>().material.color = Color.white;
        }
        if (_selectedBeat != null)
        {
            _selectedBeat.Lit.GetComponent<Renderer>().material.color = Color.yellow;
        }
    }

    public void SelectBeat(SequencerBeat beat)
    {
        // Deselect previous beat
        if (_selectedBeat != null)
        {
            _selectedBeat.DeselectBeat();
        }

        _selectedBeat = beat;
        beat.SelectBeat();
    }

    public void SetPattern(bool[] pattern)
    {
        if (pattern.Length != 16)
        {
            Debug.LogError($"Pattern length must be 16, got {pattern.Length}");
            return;
        }
        _pattern = pattern;

        // Sync visuals with pattern
        for (int i = 0; i < _beats.Count && i < _pattern.Length; i++)
        {
            _beats[i].SetActive(_pattern[i]);
        }
    }

    public void ToggleBeat(int beatIndex)
    {
        if (beatIndex < 0 || beatIndex >= _pattern.Length) return;

        _pattern[beatIndex] = !_pattern[beatIndex];
        if (beatIndex < _beats.Count)
        {
            _beats[beatIndex].SetActive(_pattern[beatIndex]);
        }
    }

    public bool IsBeatActive(int beatIndex)
    {
        return beatIndex >= 0 && beatIndex < _pattern.Length && _pattern[beatIndex];
    }

    public void PlayNextBeat()
    {
        SequencerBeat beat = _beats[_currentIndex];

        // Show playback progress regardless of whether this step is active
        beat.ShowPlaying(true);

        if (_pattern[_currentIndex])
        {
            // Try to get a recorded sample for this beat
            AudioClip sample = _sampleRecorder?.GetSample(transform.GetSiblingIndex(), _currentIndex);
            if (sample != null && SoundModule is SampleModule sampleModule)
            {
                // Play the recorded sample on this channel
                sampleModule.SetRecordedSample(sample);
                beat.SetActive(true);
                sampleModule.PlaySample(pitch, volume);
            }
            else
            {
                // Play the default beat sound
                beat.Play(SoundModule);
            }
        }

        if (_prevIndex != _currentIndex)
        {
            _beats[_prevIndex].TurnOff();
        }

        _prevIndex = _currentIndex;
        _currentIndex++;
        if (_currentIndex >= _beats.Count) _currentIndex = 0;
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
    }

    public void SetPitch(float newPitch)
    {
        pitch = Mathf.Clamp(newPitch, 0.5f, 2f);
    }
}