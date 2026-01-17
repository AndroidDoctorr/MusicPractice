using UnityEngine;
using Valve.VR.InteractionSystem;

public class BassModule : SynthModule
{
    public float frequency = 440.0f; // Initial frequency
    public float modulationIndex = 5.0f; // Modulation index controls the depth of modulation
    public float modulationFrequency = 20.0f; // Modulation frequency
    public float duration = 0.1f; // Duration of the sample in seconds

    protected override void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        Interactable interactable = GetComponent<Interactable>();
        if (interactable != null) _interactable = interactable;
        _audioSource.clip = GenerateFMSynthSample();
    }

    public AudioClip GenerateFMSynthSample()
    {
        int sampleRate = 44100; // Adjust according to your needs
        int sampleLength = (int)(duration * sampleRate);
        float[] samples = new float[sampleLength];

        float phase = 0.0f;
        float phaseIncrement = frequency * 2 * Mathf.PI / sampleRate;
        float modulationPhase = 0.0f;
        float modulationPhaseIncrement = modulationFrequency * 2 * Mathf.PI / sampleRate;

        for (int i = 0; i < sampleLength; i++)
        {
            float modulator = Mathf.Sin(modulationPhase) * modulationIndex;
            float modulatedFrequency = frequency + modulator;

            samples[i] = Mathf.Sin(phase);

            phase += phaseIncrement;
            modulationPhase += modulationPhaseIncrement;

            // Keep phase within 0 to 2PI range
            if (phase > Mathf.PI * 2)
                phase -= Mathf.PI * 2;
            if (modulationPhase > Mathf.PI * 2)
                modulationPhase -= Mathf.PI * 2;
        }

        // Create an AudioClip from the generated samples
        AudioClip audioClip = AudioClip.Create("FMBassSample", sampleLength, 1, sampleRate, false);
        audioClip.SetData(samples, 0);

        return audioClip;
    }

    public static AudioClip GenerateTB303Bass(
        float frequency, float duration, int sampleRate,
        float cutoff, float resonance, float pitchModulation,
        float envelopeModulation, float accent)
    {
        int sampleLength = (int)(duration * sampleRate);
        float[] samples = new float[sampleLength];
        float phase = 0f;

        for (int i = 0; i < sampleLength; i++)
        {
            float t = i / (float)sampleRate;
            // Generate a sawtooth-like wave shape
            float sawtooth = 1f - 2f * (phase % 1f);
            // Apply an envelope to the sound
            float envelope = Mathf.Exp(-t * 5) * Mathf.Sin(2 * Mathf.PI * 5 * t);
            // Apply cutoff and resonance
            float filteredSawtooth = ApplyLowPassFilter(sawtooth, cutoff, resonance);
            // Apply pitch modulation
            float pitchMod = 1f + pitchModulation * Mathf.Sin(2 * Mathf.PI * 2 * t);
            // Apply envelope modulation
            float envelopeMod = 1f + envelopeModulation * envelope;
            // Apply accent
            float accentedSample = filteredSawtooth * envelope * pitchMod * envelopeMod * accent;
            samples[i] = accentedSample;
            // Update the phase
            phase += frequency / sampleRate;
            if (phase >= 1f) phase -= 1f;
        }

        AudioClip audioClip = AudioClip.Create("TB303Bass", sampleLength, 1, sampleRate, false);
        audioClip.SetData(samples, 0);

        return audioClip;
    }

    public override void PlaySound(Note note, int octave, float velocity)
    {
        PlaySample(GetPitch(note, octave), velocity);
    }
    protected void PlaySample(float pitch, float velocity)
    {
        Debug.Log($"Sample Module - Play Sample: {velocity}, {pitch}");
        _audioSource.pitch = pitch;
        PlaySample(velocity);
    }
    protected void PlaySample(float velocity)
    {
        _audioSource.PlayOneShot(_audioSource.clip, velocity);
    }
}