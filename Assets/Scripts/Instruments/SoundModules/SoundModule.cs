using UnityEngine;
using Valve.VR.InteractionSystem;

public enum Note { C, Cs, D, Ds, E, F, Fs, G, Gs, A, As, B }
public enum WaveformType
{
    Sine,
    Square,
    Sawtooth,
    Triangle,
    WhiteNoise
}

public class SoundModule : MonoBehaviour
{
    protected Interactable _interactable;
    protected string _instrumentName;
    protected Rigidbody _rigidbody;
    protected AudioSource _audioSource;

    protected virtual void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        Interactable interactable = GetComponent<Interactable>();
        if (interactable != null) _interactable = interactable;
    }

    public void SetAudioSource(AudioSource audioSource)
    {
        Debug.Log("Sound Module - Set AudioSource");
        _audioSource = audioSource;
    }
    public void SetRigidbody(Rigidbody rigidbody)
    {
        Debug.Log("Sound Module - Set Rigidbody");
        _rigidbody = rigidbody;
    }
    public void SetInteractable (Interactable interactable)
    {
        Debug.Log("Sound Module - Set Interactable");
        _interactable = interactable;
    }
    public void SetInstrumentName(string name)
    {
        Debug.Log("Sound Module - Set Instrument Name");
        _instrumentName = name;
    }

    public virtual void PlaySound(Note note, int octave, float velocity, AudioSource audioSource) {
        // NO DEFAULT IMPLEMENTATION
    }
    public virtual void PlaySound(Note note, int octave, float velocity)
    {
        PlaySound(note, octave, velocity, _audioSource);
    }
    public virtual void StartSound(Note note, int octave, float velocity, AudioSource audioSource)
    {
        // NO DEFAULT IMPLEMENTATION
    }
    public virtual void StartSound(Note note, int octave, float velocity)
    {
        StartSound(note, octave, velocity, _audioSource);
    }
    public virtual void StopSound()
    {
        StopSound(_audioSource);
    }
    public virtual void StopSound(AudioSource audioSource)
    {
        // NO DEFAULT IMPLEMENTATION
    }

    public static float GetPitch(Note note, int octave)
    {
        float referencePitch = 523.25f; // C5
        int semitoneOffset = (int) note - (int) Note.C + (octave - 5) * 12;
        float pitch = referencePitch * Mathf.Pow(2.0f, semitoneOffset / 12.0f);
        return pitch / referencePitch;
    }
}
