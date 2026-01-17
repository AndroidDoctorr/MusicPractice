using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SequencerController : Controller
{
    protected List<SequencerChannel> _channels = new();
    protected Metronome _metronome;
    protected BeatPattern _currentPattern;
    protected float _swingAmount = 0f;
    protected int _currentBeat = 0;
    protected SampleRecorder _sampleRecorder;

    protected override void Awake()
    {
        base.Awake();

        _metronome = FindObjectOfType<Metronome>();
        if (_metronome != null)
        {
            _metronome.OnTick += OnMetronomeTick;
            _metronome.StartMetronome(); // ensure playback begins when the scene loads
        }

        _channels = GetComponentsInChildren<SequencerChannel>().ToList();
        
        // Find or create the SampleRecorder
        _sampleRecorder = FindObjectOfType<SampleRecorder>();
        if (_sampleRecorder == null)
        {
            GameObject recorderObj = new GameObject("SampleRecorder");
            _sampleRecorder = recorderObj.AddComponent<SampleRecorder>();
        }
        
        // Set default pattern
        SetPattern(PatternType.DrumNBass);
    }

    public void SetPattern(PatternType patternType)
    {
        _currentPattern = SequencerPatterns.GetPattern(patternType);
        _swingAmount = _currentPattern.swing;
        
        // Apply pattern to channels
        for (int i = 0; i < _channels.Count && i < _currentPattern.channelPatterns.Count; i++)
        {
            _channels[i].SetPattern(_currentPattern.channelPatterns[i]);
        }
    }

    protected void OnMetronomeTick()
    {
        foreach (SequencerChannel channel in _channels)
            channel.PlayNextBeat();

        _currentBeat = (_currentBeat + 1) % 16;
    }
}

