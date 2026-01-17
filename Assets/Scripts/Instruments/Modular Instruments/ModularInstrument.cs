using UnityEngine;

public class ModularInstrument : MonoBehaviour
{
    public Controller Controller;
    public SoundModule SoundModule;

    protected virtual void Awake()
    {
        if (!!SoundModule) Controller.SetSoundModule(SoundModule);
    }
}
