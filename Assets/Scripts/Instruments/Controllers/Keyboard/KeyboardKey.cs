using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class KeyboardKey : MonoBehaviour
{
    private List<GameObject> _contacts = new();
    private AudioSource _audioSource;

    public delegate void KeyPressDelegate(Note note, int octave, float velocity, AudioSource source);
    public event KeyPressDelegate OnPress;

    public delegate void KeyReleaseDelegate(AudioSource source);
    public event KeyReleaseDelegate OnRelease;

    public TMP_Text NoteName;
    public Note Note = Note.C;
    public int Octave = 4;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        SetName();
    }

    public virtual void SetName()
    {
        NoteName.text = $"{Note.ToString().Replace('s','#')}{Octave}";
    }
    protected virtual void OnCollisionEnter(Collision collision)
    {
        float intensity = collision.relativeVelocity.magnitude;
        Debug.Log($"Keyboard Key Pressed: {Note}{Octave} - {intensity}");

        _contacts.Add(collision.gameObject);
        OnPress(Note, Octave, intensity, _audioSource);
    }
    private void OnCollisionExit(Collision collision)
    {
        if (_contacts.Contains(collision.gameObject))
            _contacts.Remove(collision.gameObject);

        if (_contacts.Count == 0)
            OnRelease(_audioSource);
    }
}
