using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class KeyboardController : Controller
{
    public int StartOctave = 4;
    public float Sensitivity = 2;

    protected override void Awake()
    {
        SetUpOctaves();
    }
    protected void SetUpOctaves()
    {
        KeyboardOctave[] octaves = GetComponentsInChildren<KeyboardOctave>();

        foreach (KeyboardOctave octave in octaves)
        {
            foreach (KeyboardKey key in octave.Keys)
            {
                Debug.Log($"Set up Keyboard Key: {key.Note}{octave.Octave}");
                key.Octave = octave.Octave;
                key.SetName();
                key.OnPress += DoKeyPress;
                key.OnRelease += DoKeyRelease;
            }
        }
    }

    protected void DoKeyPress(Note note, int octave, float velocity, AudioSource audioSource)
    {
        Debug.Log($"Keyboard Controller - Detected key press: {note}{octave} - {velocity}");
        float volume = Mathf.Clamp(velocity * Sensitivity, 0f, 1f);
        _soundModule.StartSound(note, octave, volume, audioSource);
    }
    protected void DoKeyRelease(AudioSource audioSource)
    {
        _soundModule.StopSound(audioSource);
    }
}
