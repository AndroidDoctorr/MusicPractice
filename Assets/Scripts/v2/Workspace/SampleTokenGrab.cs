using UnityEngine;
using Valve.VR.InteractionSystem;

namespace MusicPractice.SpatialLoop
{
    /// <summary>
    /// Grip grab for sample tokens. Unlike Valve's Throwable, does not detach when
    /// the grip button is simply not held (required for post-record OfferToHand).
    /// </summary>
    [RequireComponent(typeof(Interactable), typeof(Rigidbody))]
    public sealed class SampleTokenGrab : MonoBehaviour
    {
        [Tooltip("Attachment flags used for grip pickup and post-record offers.")]
        public Hand.AttachmentFlags attachmentFlags = Hand.defaultAttachmentFlags;

        private SampleTokenView _token;
        private bool _waitForGripPressBeforeRelease;

        private void Awake()
        {
            _token = GetComponent<SampleTokenView>();
        }

        public void PrepareProgrammaticAttach()
        {
            _waitForGripPressBeforeRelease = true;
        }

        private void HandHoverUpdate(Hand hand)
        {
            if (_token == null || !_token.IsShelfToken)
            {
                return;
            }

            Interactable interactable = GetComponent<Interactable>();
            if (interactable != null && interactable.attachedToHand != null)
            {
                return;
            }

            if (hand.GetGrabStarting() == GrabTypes.Grip)
            {
                hand.AttachObject(gameObject, GrabTypes.Grip, attachmentFlags);
            }
        }

        private void HandAttachedUpdate(Hand hand)
        {
            if (_waitForGripPressBeforeRelease)
            {
                if (hand.grabGripAction != null
                    && hand.grabGripAction.GetState(hand.handType))
                {
                    _waitForGripPressBeforeRelease = false;
                }

                return;
            }

            if (hand.GetGrabEnding(GrabTypes.Grip) == GrabTypes.Grip)
            {
                hand.DetachObject(gameObject);
            }
        }
    }
}
