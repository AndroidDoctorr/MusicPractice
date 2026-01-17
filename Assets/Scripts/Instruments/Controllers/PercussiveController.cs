using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class PercussiveController : Controller
{
    public Note Note = Note.A;
    public int Octave = 4;
    public float Sensitivity = 0.5f;
    public string SampleFolderName = "DrumSamples";

    protected override void Awake()
    {
        if (_soundModule is SampleModule)
            ((SampleModule) _soundModule).SetSampleFolderName(SampleFolderName);
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        float strikeIntensity = Mathf.Clamp01(collision.relativeVelocity.magnitude * Sensitivity);
        float volume = Mathf.Lerp(0, 1, strikeIntensity);
        _soundModule.PlaySound(Note, Octave, volume);
    }
}
