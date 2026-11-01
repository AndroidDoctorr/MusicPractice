using UnityEngine.Events;
using Valve.VR.InteractionSystem;

namespace MusicPractice.SpatialLoop
{
    internal static class SpatialGrabbableSetup
    {
        public static void ConfigureThrowable(Throwable throwable)
        {
            if (throwable == null)
            {
                return;
            }

            throwable.restoreOriginalParent = false;
            throwable.catchingSpeedThreshold = -1f;
            throwable.releaseVelocityStyle = ReleaseStyle.NoChange;
            throwable.attachmentFlags = Hand.defaultAttachmentFlags;

            if (throwable.onPickUp == null)
            {
                throwable.onPickUp = new UnityEvent();
            }

            if (throwable.onDetachFromHand == null)
            {
                throwable.onDetachFromHand = new UnityEvent();
            }
        }
    }
}
