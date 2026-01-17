using static Instrument;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.PackageManager.UI;

public class SynthModule : SoundModule
{
    protected List<AudioSource> _activeSources = new();
    protected float[] _currentWaveform;

    protected bool _hasWaveformChanged = true;
    protected float _baseFrequency = 261.63f;
    protected float _frequency = 440;
    protected float _color = 0.0f;
    protected float _phase = 0.0f;
    protected float _volume = 0.0f;
    protected float _amplitude = 0.5f;

    public float Squareness = 0.0f;
    public float Triangleness = 0.0f;
    public float Sawness = 0.0f;
    public float Whiteness = 0.0f;

    public AudioClip BaseClip;

    protected AudioLowPassFilter _lowPassFilter;
    protected AudioHighPassFilter _highPassFilter;

    public float SampleRate = 22050;
    public float OctaveRange = 12.0f;

    protected void Start()
    {
        if (!_audioSource) _audioSource = GetComponent<AudioSource>();
        if (!_audioSource) return;
        if (!_audioSource.clip && !BaseClip) return;
        if (!_audioSource.clip) _audioSource.clip = BaseClip;
        if (!_audioSource.clip) return;
        _audioSource.clip.SetData(GenerateWaveform(_audioSource), 0);
    }
    protected void FixedUpdate()
    {
        if (!_audioSource && _activeSources.Count == 0) return;
        if (_activeSources.Count > 0 && _hasWaveformChanged)
        {
            foreach (AudioSource source in _activeSources)
                source.clip.SetData(GenerateWaveform(source), 0);
            _hasWaveformChanged = false;
        }
        else if (!!_audioSource && _audioSource.isPlaying && _hasWaveformChanged)
        {
            _audioSource.clip.SetData(GenerateWaveform(_audioSource), 0);
            _hasWaveformChanged = false;
        }
    }


    public void StartSound(float pitch, float velocity, AudioSource audioSource)
    {
        audioSource.pitch = pitch;
        audioSource.volume = Mathf.Clamp01(velocity);
        audioSource.loop = true;

        audioSource.Play();
        if (audioSource != _audioSource)
            _activeSources.Add(audioSource);
    }
    public override void StartSound(Note note, int octave, float velocity, AudioSource audioSource)
    {
        if (!audioSource.clip)
        {
            audioSource.clip = BaseClip;
            audioSource.clip.SetData(GenerateWaveform(audioSource), 0);
        }

        float pitch = GetPitch(note, octave);
        StartSound(pitch, velocity, audioSource);
    }
    public override void StopSound(AudioSource audioSource)
    {
        if (audioSource != _audioSource)
            _activeSources.Remove(audioSource);
        audioSource.Stop();
    }
    public override void StartSound(Note note, int octave, float velocity)
    {
        StartSound(note, octave, velocity, _audioSource);
    }
    public void StartSound(float pitch, float velocity)
    {
        StartSound(pitch, velocity, _audioSource);
    }
    public override void StopSound()
    {
        StopSound(_audioSource);
    }




    public virtual void SetTriangleness(float amount)
    {
        Triangleness = amount;
        _hasWaveformChanged = true;
    }
    public virtual void SetSquareness(float amount)
    {
        Squareness = amount;
        _hasWaveformChanged = true;
    }
    public virtual void SetSawness(float amount)
    {
        Sawness = amount;
        _hasWaveformChanged = true;
    }
    public virtual void SetWhiteness(float amount)
    {
        Whiteness = amount;
        _hasWaveformChanged = true;
    }



    private float[] GenerateWaveform(AudioSource audioSource)
    {
        int numSamples = audioSource.clip.samples;
        float[] audioData = new float[numSamples];
        float increment = 2 * Mathf.PI * _frequency / SampleRate;

        for (int i = 0; i < numSamples; i++)
        {
            float sampleValue;

            // Interpolate between different waveforms based on parameters
            sampleValue = _amplitude * (
                (1 - Squareness) * Mathf.Sin(_phase) +
                Squareness * ((_phase < Mathf.PI) ? 1 : -1)
            );

            sampleValue = Mathf.Lerp(sampleValue,
                _amplitude * (
                    (1 - Triangleness) * ((2 * _amplitude / Mathf.PI) * Mathf.Asin(Mathf.Sin(_phase))) +
                    Triangleness * ((2 * _amplitude / Mathf.PI) * Mathf.Asin(Mathf.Sin(_phase)))
                ), Triangleness);

            sampleValue = Mathf.Lerp(sampleValue,
                _amplitude * (
                    (1 - Sawness) * (_amplitude * ((_phase / (2 * Mathf.PI)) - Mathf.Floor((_phase / (2 * Mathf.PI)))) * 2 - 1) +
                    Sawness * (_amplitude * ((_phase / (2 * Mathf.PI)) - Mathf.Floor((_phase / (2 * Mathf.PI)))) * 2 - 1)
                ), Sawness);

            sampleValue = Mathf.Lerp(sampleValue,
                2 * _amplitude * (Random.Range(0,1.0f) - 0.5f), Whiteness);

            audioData[i] = sampleValue;
            _phase += increment;

            if (_phase >= 2 * Mathf.PI)
            {
                _phase -= 2 * Mathf.PI;
            }
        }

        return audioData;
    }
    
    protected static float ApplyLowPassFilter(float input, float cutoff, float resonance)
    {
        float c = 1.0f / (1.0f + resonance * 10.0f);
        float filteredValue = input * (1.0f - c) + cutoff * c;
        return filteredValue;
    }
    private float DoThing()
    {
        return ((1 - Sawness) * (_amplitude * ((_phase / (2 * Mathf.PI)) -
            Mathf.Floor((_phase / (2 * Mathf.PI)))) * 2 - 1) +
            Sawness * (_amplitude * ((_phase / (2 * Mathf.PI)) -
            Mathf.Floor((_phase / (2 * Mathf.PI)))) * 2 - 1));
    }
}
