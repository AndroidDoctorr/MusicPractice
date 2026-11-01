using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SequencerChannel : MonoBehaviour
{
    protected List<SequencerBeat> _beats = new();
    protected int _prevIndex = 0;
    protected int _currentIndex = 0;
    protected bool[] _pattern;
    protected float[] _stepVelocities;
    protected XYPadController _xyPad;
    protected int _selectedStepIndex = 0;

    [Header("Channel Settings")]
    public string channelName;
    [Range(0f, 1f)]
    public float volume = 1.0f;
    [Range(0.5f, 2f)]
    public float pitch = 1.0f;
    [Tooltip("Velocity used when toggling a step on (if pattern doesn't supply a velocity).")]
    [Range(0f, 1f)]
    public float defaultOnVelocity = 0.75f;
    public SoundModule SoundModule;
    public Button XYPadButton;

    private void Awake()
    {
        _beats = GetComponentsInChildren<SequencerBeat>().ToList();
        _pattern = new bool[16];
        _stepVelocities = new float[16];
        _xyPad = FindObjectOfType<XYPadController>();

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
            SetXYPadLinked(false);
        }
        else
        {
            _xyPad.LinkToChannel(this);
            SetXYPadLinked(true);
        }
    }

    public void SetXYPadLinked(bool linked)
    {
        if (XYPadButton == null) return;
        Renderer r = XYPadButton.GetComponent<Renderer>();
        if (r != null) r.material.color = linked ? Color.green : Color.white;
    }

    public void SelectStep(int stepIndex)
    {
        _selectedStepIndex = Mathf.Clamp(stepIndex, 0, 15);
    }

    public int GetSelectedStepIndex()
    {
        return _selectedStepIndex;
    }

    public void SetPattern(bool[] pattern)
    {
        SetPattern(pattern, null);
    }

    public void SetPattern(bool[] pattern, float[] velocities)
    {
        if (pattern.Length != 16)
        {
            Debug.LogError($"Pattern length must be 16, got {pattern.Length}");
            return;
        }
        _pattern = (bool[])pattern.Clone();

        if (velocities != null && velocities.Length == 16)
        {
            _stepVelocities = (float[])velocities.Clone();
        }
        else
        {
            // Derive velocities from on/off pattern
            for (int i = 0; i < 16; i++)
            {
                _stepVelocities[i] = _pattern[i] ? defaultOnVelocity : 0f;
            }
        }

        // Sync visuals with pattern
        for (int i = 0; i < _beats.Count && i < _pattern.Length; i++)
        {
            _beats[i].Velocity = _stepVelocities[i];
            _beats[i].SetActive(_pattern[i]);
        }
    }

    public void ToggleBeat(int beatIndex)
    {
        if (beatIndex < 0 || beatIndex >= _pattern.Length) return;

        _pattern[beatIndex] = !_pattern[beatIndex];
        if (beatIndex < _beats.Count)
        {
            _stepVelocities[beatIndex] = _pattern[beatIndex] ? Mathf.Max(defaultOnVelocity, 0.01f) : 0f;
            _beats[beatIndex].Velocity = _stepVelocities[beatIndex];
            _beats[beatIndex].SetActive(_pattern[beatIndex]);
        }
    }

    public void SetStepVelocity(int beatIndex, float velocity01)
    {
        if (beatIndex < 0 || beatIndex >= _pattern.Length) return;

        float v = Mathf.Clamp01(velocity01);
        _stepVelocities[beatIndex] = v;
        _pattern[beatIndex] = v > 0f;

        if (beatIndex < _beats.Count)
        {
            _beats[beatIndex].Velocity = v;
            _beats[beatIndex].SetActive(_pattern[beatIndex]);
        }
    }

    public enum StepMask
    {
        SelectedStep,
        All,
        Odds,
        Evens,
        Quarters,
        Offbeats
    }

    public void ApplyVelocityToMask(StepMask mask, float velocity01)
    {
        int selected = GetSelectedStepIndex();
        float v = Mathf.Clamp01(velocity01);

        for (int i = 0; i < _pattern.Length; i++)
        {
            bool apply = mask switch
            {
                StepMask.SelectedStep => i == selected,
                StepMask.All => true,
                StepMask.Odds => (i % 2) == 0, // 1,3,5... (0-based even indices)
                StepMask.Evens => (i % 2) == 1, // 2,4,6... (0-based odd indices)
                StepMask.Quarters => i == 0 || i == 4 || i == 8 || i == 12,
                StepMask.Offbeats => i == 2 || i == 6 || i == 10 || i == 14,
                _ => false
            };

            if (!apply) continue;
            SetStepVelocity(i, v);
        }
    }

    public bool IsBeatActive(int beatIndex)
    {
        return beatIndex >= 0 && beatIndex < _pattern.Length && _pattern[beatIndex];
    }

    public float GetStepVelocity(int beatIndex)
    {
        if (beatIndex < 0 || beatIndex >= _stepVelocities.Length) return 0f;
        return _stepVelocities[beatIndex];
    }

    public void PlayNextBeat()
    {
        SequencerBeat beat = _beats[_currentIndex];

        // Show playback progress regardless of whether this step is active
        beat.ShowPlaying(true);

        if (_pattern[_currentIndex] && beat.Velocity > 0f)
        {
            float v = Mathf.Clamp01(volume * beat.Velocity);

            // If this channel is a sample module, use channel pitch/volume directly.
            if (SoundModule is SampleModule sampleModule)
            {
                sampleModule.PlaySample(pitch, v);
            }
            else if (SoundModule != null)
            {
                // Otherwise defer to the module's note handling.
                SoundModule.PlaySound(beat.Note, beat.Octave, v);
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