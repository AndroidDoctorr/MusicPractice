using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StringController : Controller
{
    public List<GuitarString> Strings;

    protected override void Awake()
    {
        foreach (GuitarString str in Strings)
            str.OnStrum += StrumString;
    }

    protected virtual void StrumString(
        int baseOctave, Note baseKey, float dist, float velocity, SoundModule soundModule
    )
    {
        float octaveRange = 22.0f;
        float normalizedDist = Mathf.Clamp01(dist / octaveRange);

        int octave = baseOctave + Mathf.FloorToInt(normalizedDist * octaveRange);
        Note note = baseKey;

        float normalizedVelocity = Mathf.Clamp01(velocity / 5.0f);
        soundModule.StartSound(note, octave, normalizedVelocity);
    }
}
