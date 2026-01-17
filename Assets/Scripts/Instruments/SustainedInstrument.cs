using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class SustainedInstrument : Instrument
{
    private float _shapeControlUp = 0.0f;
    private float _shapeControlDn = 0.0f;
    private float _shapeControlLf = 0.0f;
    private float _shapeControlRt = 0.0f;

    protected override void Awake()
    {
        base.Awake();
        _audioSource.loop = true;
        _interactable = GetComponent<Interactable>();

        TriggerRight.onChange += DoRightTrigger;
    }

    protected virtual void FixedUpdate()
    {
        if (!_interactable.attachedToHand) return;
        if (_volume > 0)
        {
            float instrumentHeight = transform.position.y;
            float noteOffset = (instrumentHeight - 0.5f) * octaveRange;
            float noteFrequency = baseFrequency * Mathf.Pow(2.0f, noteOffset / 12.0f);
            _frequency = noteFrequency;

            _whiteness = _shapeControlDn;
            // These vary depending on the control mode
            _triangleness = _shapeControlUp;
            _squareness = _shapeControlRt;
            _sawness = _shapeControlLf;

            if (_audioSource.clip == null)
            {
                Debug.Log("NO AUDIO CLIP");
                return;
            }

            // Generate and set audio data for the sine wave continuously
            _audioSource.clip.SetData(GenerateWaveform(), 0);
            if (!_audioSource.isPlaying)
                _audioSource.Play();

            // _lowPassFilter.cutoffFrequency = Mathf.Lerp(500, 20000, );
            // _highPassFilter.cutoffFrequency = Mathf.Lerp(20, 2000, filterControl.y);
        }
        else if (_audioSource.isPlaying)
        {
            _audioSource.Stop();
        }
    }

    private float[] GenerateWaveform()
    {
        int numSamples = _audioSource.clip.samples;
        float[] audioData = new float[numSamples];
        float increment = 2 * Mathf.PI * _frequency / SampleRate;

        for (int i = 0; i < numSamples; i++)
        {
            float sampleValue;

            // Interpolate between different waveforms based on parameters
            sampleValue = _amplitude * (
                (1 - _squareness) * Mathf.Sin(_phase) +
                _squareness * ((_phase < Mathf.PI) ? 1 : -1)
            );

            sampleValue = Mathf.Lerp(sampleValue,
                _amplitude * (
                    (1 - _triangleness) * ((2 * _amplitude / Mathf.PI) * Mathf.Asin(Mathf.Sin(_phase))) +
                    _triangleness * ((2 * _amplitude / Mathf.PI) * Mathf.Asin(Mathf.Sin(_phase)))
                ), _triangleness);

            sampleValue = Mathf.Lerp(sampleValue,
                _amplitude * (
                    (1 - _sawness) * (_amplitude * ((_phase / (2 * Mathf.PI)) - Mathf.Floor((_phase / (2 * Mathf.PI)))) * 2 - 1) +
                    _sawness * (_amplitude * ((_phase / (2 * Mathf.PI)) - Mathf.Floor((_phase / (2 * Mathf.PI)))) * 2 - 1)
                ), _sawness);

            sampleValue = Mathf.Lerp(sampleValue,
                2 * _amplitude * (Random.value - 0.5f), _whiteness);

            audioData[i] = sampleValue;
            _phase += increment;

            if (_phase >= 2 * Mathf.PI)
            {
                _phase -= 2 * Mathf.PI;
            }
        }

        return audioData;
    }

    public void SetWaveform(int waveform)
    {
        _currentWaveform = (WaveformType)waveform;
    }
    protected virtual void DoRightTrackpad(SteamVR_Action_Vector2 action, SteamVR_Input_Sources source, Vector2 axis, Vector2 delta)
    {
        if (axis.x > 0)
            _shapeControlRt = axis.x;
        else
            _shapeControlLf = -axis.x;
        if (axis.y > 0)
            _shapeControlUp = axis.y;
        else
            _shapeControlDn = -axis.y;
    }

    protected void DoRightTrigger(SteamVR_Action_Single action, SteamVR_Input_Sources source, float axis, float delta)
    {
        _volume = axis;
    }
}