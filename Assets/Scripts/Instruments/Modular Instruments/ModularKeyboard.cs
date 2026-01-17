using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valve.VR.InteractionSystem;

public class ModularKeyboard : ModularInstrument
{
    protected override void Awake()
    {
        base.Awake();
        SoundModule.SetInteractable(GetComponent<Interactable>());
    }
}
