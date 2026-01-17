using UnityEngine;

public class TB303SynthModule : SynthModule
{
    [Header("TB-303 Specific Properties")]
    public float cutoff = 0.7f;
    public float resonance = 0.5f;
    public float envelopeMod = 0.5f;
    public float decay = 0.5f;
    public float accent = 0.5f;

    private float _currentCutoff;
    private float _currentResonance;
    private float _currentEnvelopeMod;
    private float _currentDecay;
    private float _currentAccent;

    private new LowPassFilter _lowPassFilter;
    private float _baseCutoffFrequency = 3000f;

    protected override void Awake()
    {
        base.Awake();
        // Initialize the low-pass filter with desired cutoff frequency and sample rate
        _lowPassFilter = new LowPassFilter(3000f, AudioSettings.outputSampleRate);
    }

    public override void PlaySound(Note note, int octave, float velocity, AudioSource audioSource)
    {
        PlaySound(note, octave, velocity, audioSource, false);
    }

    public void PlaySound(Note note, int octave, float velocity, AudioSource audioSource, bool isAccented)
    {
        float frequency = GetFrequency(note, octave);
        float[] waveData = GenerateWaveform(frequency, audioSource.clip.samples, audioSource.clip.channels, audioSource.clip.frequency);

        // Apply envelope and filter modulation
        ApplyEnvelopeAndFilterModulation(waveData, velocity, accent, audioSource.clip.frequency, isAccented);

        audioSource.clip.SetData(waveData, 0);
        audioSource.Play();
    }

    private void ApplyEnvelopeAndFilterModulation(float[] waveData, float velocity, float accent, int sampleRate, bool isAccented)
    {
        float envelopeValue = velocity + (isAccented ? accent : 0);
        for (int i = 0; i < waveData.Length; i++)
        {
            // Modulate the cutoff frequency based on the envelope
            float modulatedCutoff = _baseCutoffFrequency * (1 + envelopeValue);
            _lowPassFilter.UpdateCutoffFrequency(modulatedCutoff, sampleRate);

            // Apply the low-pass filter with modulated cutoff
            waveData[i] = _lowPassFilter.ApplyFilter(waveData[i]);

            // Apply amplitude modulation
            waveData[i] *= envelopeValue;
        }
    }

    private float[] GenerateWaveform(float frequency, int length, int channels, int sampleRate)
    {
        float[] data = new float[length * channels];
        for (int i = 0; i < data.Length; i += channels)
        {
            // Simple sawtooth wave generation
            data[i] = ((i % (sampleRate / frequency)) / (sampleRate / frequency)) * 2 - 1;

            // Copy to all channels
            for (int j = 1; j < channels; j++)
            {
                data[i + j] = data[i];
            }
        }
        return data;
    }

    public class LowPassFilter
    {
        private float _previousValue = 0;
        private float _alpha;

        public LowPassFilter(float cutoffFrequency, float sampleRate)
        {
            _alpha = CalculateAlpha(cutoffFrequency, sampleRate);
        }

        private float CalculateAlpha(float cutoffFrequency, float sampleRate)
        {
            float rc = 1.0f / (cutoffFrequency * 2 * Mathf.PI);
            float dt = 1.0f / sampleRate;
            return dt / (rc + dt);
        }

        public float ApplyFilter(float sample)
        {
            _previousValue += _alpha * (sample - _previousValue);
            return _previousValue;
        }

        public void UpdateCutoffFrequency(float newCutoffFrequency, float sampleRate)
        {
            _alpha = CalculateAlpha(newCutoffFrequency, sampleRate);
        }
    }

    // Additional methods for setting and adjusting TB-303 parameters
    public void SetCutoff(float value)
    {
        cutoff = value;
        // Update the synthesis parameters
    }

    // Similarly, create methods for resonance, envelopeMod, decay, accent

    private float GetFrequency(Note note, int octave)
    {
        // A4 is the 9th semitone in the 4th octave (in 0-indexed octaves)
        int semitoneOffsetFromA4 = (octave - 4) * 12 + (int)note - (int)Note.A;

        // A4 is 440 Hz
        float frequencyOfA4 = 440f;

        // Calculate the frequency
        float frequency = frequencyOfA4 * Mathf.Pow(2, semitoneOffsetFromA4 / 12f);
        return frequency;
    }
}
