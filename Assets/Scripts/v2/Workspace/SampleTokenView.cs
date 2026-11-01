using System.Collections;
using UnityEngine;
using Valve.VR.InteractionSystem;

namespace MusicPractice.SpatialLoop
{
    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
    [RequireComponent(typeof(Interactable), typeof(SampleTokenGrab))]
    public sealed class SampleTokenView : MonoBehaviour
    {
        private SpatialWorkspaceController _workspace;
        private SpatialPerformanceController _performance;
        private SampleAssetData _asset;
        private Rigidbody _rigidbody;
        private bool _isShelfToken;
        private bool _suppressDropCommit;

        public SampleAssetData Asset => _asset;
        public bool IsShelfToken => _isShelfToken;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.useGravity = false;
            _rigidbody.isKinematic = true;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

            Interactable interactable = GetComponent<Interactable>();
            interactable.hideHandOnAttach = false;
            interactable.highlightOnHover = true;
            interactable.hideHighlight = new GameObject[0];
            interactable.useHandObjectAttachmentPoint = true;
        }

        public void Initialize(
            SpatialWorkspaceController workspace,
            SpatialPerformanceController performance,
            SampleAssetData asset,
            bool isShelfToken)
        {
            _workspace = workspace;
            _performance = performance;
            _asset = asset;
            _isShelfToken = isShelfToken;
            name = isShelfToken
                ? $"Bank {asset.DisplayName}"
                : $"Offered {asset.DisplayName}";

            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                properties.SetColor("_Color", asset.Color);
                renderer.SetPropertyBlock(properties);
            }
        }

        public bool IsCarried()
        {
            Interactable interactable = GetComponent<Interactable>();
            return !_isShelfToken
                && interactable != null
                && interactable.attachedToHand != null;
        }

        public void OfferToHand(Hand hand)
        {
            if (hand == null)
            {
                return;
            }

            transform.SetParent(null, true);
            _rigidbody.isKinematic = true;
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;

            Interactable interactable = GetComponent<Interactable>();
            SampleTokenGrab grab = GetComponent<SampleTokenGrab>();
            _suppressDropCommit = true;
            if (interactable.attachedToHand != null)
            {
                interactable.attachedToHand.DetachObject(gameObject);
            }

            grab.PrepareProgrammaticAttach();
            hand.AttachObject(
                gameObject,
                GrabTypes.Grip,
                grab.attachmentFlags);
            _suppressDropCommit = false;
        }

        private void OnAttachedToHand(Hand hand)
        {
            _performance?.NotifyAssetGrabbed(hand.handType, this);
        }

        private void OnDetachedFromHand(Hand hand)
        {
            _performance?.NotifyAssetReleased(hand.handType, this);
            if (_suppressDropCommit)
            {
                return;
            }

            StartCoroutine(CommitDropAtEndOfFrame());
        }

        private IEnumerator CommitDropAtEndOfFrame()
        {
            yield return new WaitForEndOfFrame();

            if (this == null)
            {
                yield break;
            }

            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.isKinematic = true;
            _workspace?.CommitTokenDrop(this);
        }
    }
}
