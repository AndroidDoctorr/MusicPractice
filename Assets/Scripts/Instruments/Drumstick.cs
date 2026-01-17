using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class Drumstick : MonoBehaviour
{
    private Interactable _interactable;

    public Transform AttachmentPoint;
    private void Awake()
    {
        _interactable = GetComponent<Interactable>();
        _interactable.onAttachedToHand += PickUpStick;
    }

    private void PickUpStick(Hand hand)
    {
        if (hand.handType == Valve.VR.SteamVR_Input_Sources.LeftHand)
        {
            AttachmentPoint.rotation = Quaternion.Euler(new Vector3(90, 90, 0));
        } else
        {
            AttachmentPoint.rotation = Quaternion.Euler(new Vector3(0, 0, 90));
        }
    }
}
