
using UnityEngine;

public class Controller : MonoBehaviour
{
    protected SoundModule _soundModule;

    protected virtual void Awake()
    {

    }

    public void SetSoundModule(SoundModule soundModule)
    {
        _soundModule = soundModule;
    }
}
