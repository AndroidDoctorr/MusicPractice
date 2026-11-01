using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

namespace MusicPractice.SpatialLoop
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider))]
    [RequireComponent(typeof(Rigidbody), typeof(Interactable), typeof(Throwable))]
    public sealed class SampleInstanceView : MonoBehaviour
    {
        [SerializeField, Range(2, 64)] private int _maximumArcSegments = 32;
        [SerializeField, Min(0.01f)] private float _minimumRadialWidth = 0.07f;
        [SerializeField, Min(0.01f)] private float _maximumRadialWidth = 0.15f;
        [SerializeField, Min(0.005f)] private float _minimumHeight = 0.035f;
        [SerializeField, Min(0.005f)] private float _maximumHeight = 0.085f;

        private SpatialLoopController _controller;
        private SampleInstanceData _data;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private BoxCollider _boxCollider;
        private Rigidbody _rigidbody;
        private Mesh _runtimeMesh;
        private long _startTickBeforeGrab;
        private bool _pendingDeletion;

        public SampleInstanceData Data => _data;
        public long StartTickBeforeGrab => _startTickBeforeGrab;
        public bool IsPendingDeletion => _pendingDeletion;

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _boxCollider = GetComponent<BoxCollider>();
            _rigidbody = GetComponent<Rigidbody>();

            _rigidbody.useGravity = false;
            _rigidbody.isKinematic = true;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

            Interactable interactable = GetComponent<Interactable>();
            interactable.hideHandOnAttach = false;
            interactable.highlightOnHover = true;
            interactable.hideHighlight = new GameObject[0];

            SpatialGrabbableSetup.ConfigureThrowable(GetComponent<Throwable>());
        }

        private void OnDestroy()
        {
            if (_runtimeMesh == null)
            {
                return;
            }

            Destroy(_runtimeMesh);
        }

        public void Initialize(
            SpatialLoopController controller,
            SampleInstanceData data,
            Material sampleMaterial)
        {
            _controller = controller;
            _data = data;
            _meshRenderer.sharedMaterial = sampleMaterial;
            if (_meshRenderer.sharedMaterial != null)
            {
                _meshRenderer.sharedMaterial.EnableKeyword("_EMISSION");
            }
            SampleAssetData asset = controller.GetAsset(data.SampleAssetId);
            if (asset != null)
            {
                var properties = new MaterialPropertyBlock();
                _meshRenderer.GetPropertyBlock(properties);
                properties.SetColor("_Color", asset.Color);
                _meshRenderer.SetPropertyBlock(properties);
            }
            RefreshEffectVisual();
            name = $"Sample {data.StartTick}";
            RefreshFromModel();
        }

        public void RefreshFromModel()
        {
            if (_controller == null || _data == null)
            {
                return;
            }

            long loopLengthTicks = _controller.LoopLengthTicks;
            double startPhase = MusicalTime.TickToPhase(
                _data.StartTick,
                loopLengthTicks);
            float startAngle = (float)(startPhase * 360d);

            transform.localPosition = GetRingPosition(startPhase, _controller.RingRadius);
            transform.localRotation = Quaternion.Euler(0f, startAngle, 0f);
            transform.localScale = Vector3.one;
            name = $"Sample {_data.StartTick}";

            BuildArcMesh(
                _controller.RingRadius,
                _data.DurationTicks / (double)loopLengthTicks,
                _data.Velocity);

            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.isKinematic = true;
        }

        public void RefreshEffectVisual()
        {
            if (_data == null || _meshRenderer == null)
            {
                return;
            }

            EffectChainData effects = _data.Effects;
            Vector2 filter = effects.GetXY(EffectPairId.Filter);
            Vector2 space = effects.GetXY(EffectPairId.Space);
            float filterAmount = effects.IsBypassed(EffectPairId.Filter)
                ? 0f
                : Mathf.Max(Mathf.Abs(filter.x - 0.5f) * 2f, filter.y);
            float spaceAmount = effects.IsBypassed(EffectPairId.Space)
                ? 0f
                : Mathf.Max(space.x, space.y);
            Color effectColor = spaceAmount > filterAmount
                ? new Color(1f, 0.1f, 0.8f)
                : new Color(0.1f, 0.8f, 1f);
            float intensity = Mathf.Max(filterAmount, spaceAmount);

            var properties = new MaterialPropertyBlock();
            _meshRenderer.GetPropertyBlock(properties);
            properties.SetColor("_EmissionColor", effectColor * intensity * 1.5f);
            _meshRenderer.SetPropertyBlock(properties);
        }

        public void PrepareForDeletion()
        {
            _pendingDeletion = true;
        }

        private void OnAttachedToHand(Hand hand)
        {
            if (_data == null)
            {
                return;
            }

            _pendingDeletion = false;
            _startTickBeforeGrab = _data.StartTick;
            _controller?.NotifyGrabbed(this, hand);
        }

        private void OnDetachedFromHand(Hand hand)
        {
            _controller?.NotifyReleased(this, hand);
            if (!_pendingDeletion)
            {
                StartCoroutine(CommitDropAtEndOfFrame());
            }
        }

        private IEnumerator CommitDropAtEndOfFrame()
        {
            yield return new WaitForEndOfFrame();

            if (_pendingDeletion || this == null)
            {
                yield break;
            }

            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.isKinematic = true;
            _controller?.CommitDrop(this, _startTickBeforeGrab);
        }

        private void BuildArcMesh(
            float centerRadius,
            double durationPhase,
            float velocity)
        {
            if (_runtimeMesh == null)
            {
                _runtimeMesh = new Mesh
                {
                    name = "Procedural Sample Arc"
                };
                _meshFilter.sharedMesh = _runtimeMesh;
            }
            else
            {
                _runtimeMesh.Clear();
            }

            float clampedVelocity = Mathf.Clamp01(velocity);
            float radialWidth = Mathf.Lerp(
                _minimumRadialWidth,
                _maximumRadialWidth,
                clampedVelocity);
            float height = Mathf.Lerp(
                _minimumHeight,
                _maximumHeight,
                clampedVelocity);
            double arcRadians = Mathf.Clamp(
                (float)(durationPhase * Mathf.PI * 2d),
                Mathf.Deg2Rad * 3f,
                Mathf.PI * 1.9f);
            int segmentCount = Mathf.Clamp(
                Mathf.CeilToInt((float)(arcRadians * Mathf.Rad2Deg / 5d)),
                2,
                _maximumArcSegments);

            float innerRadius = Mathf.Max(0.01f, centerRadius - radialWidth * 0.5f);
            float outerRadius = centerRadius + radialWidth * 0.5f;
            float halfHeight = height * 0.5f;
            var vertices = new Vector3[(segmentCount + 1) * 4];
            var triangles = new List<int>(segmentCount * 24 + 12);

            for (int i = 0; i <= segmentCount; i++)
            {
                double t = i / (double)segmentCount;
                double relativeAngle = Mathf.Lerp(
                    0f,
                    (float)arcRadians,
                    (float)t);
                float sin = (float)System.Math.Sin(relativeAngle);
                float cos = (float)System.Math.Cos(relativeAngle);
                int vertex = i * 4;

                vertices[vertex] = new Vector3(
                    sin * innerRadius,
                    -halfHeight,
                    cos * innerRadius - centerRadius);
                vertices[vertex + 1] = new Vector3(
                    sin * outerRadius,
                    -halfHeight,
                    cos * outerRadius - centerRadius);
                vertices[vertex + 2] = new Vector3(
                    sin * innerRadius,
                    halfHeight,
                    cos * innerRadius - centerRadius);
                vertices[vertex + 3] = new Vector3(
                    sin * outerRadius,
                    halfHeight,
                    cos * outerRadius - centerRadius);
            }

            for (int i = 0; i < segmentCount; i++)
            {
                int current = i * 4;
                int next = (i + 1) * 4;

                AddQuad(triangles, current + 2, current + 3, next + 3, next + 2);
                AddQuad(triangles, current + 1, current, next, next + 1);
                AddQuad(triangles, current, current + 2, next + 2, next);
                AddQuad(triangles, current + 3, current + 1, next + 1, next + 3);
            }

            int last = segmentCount * 4;
            AddQuad(triangles, 0, 1, 3, 2);
            AddQuad(triangles, last, last + 2, last + 3, last + 1);

            _runtimeMesh.SetVertices(vertices);
            _runtimeMesh.SetTriangles(triangles, 0);
            _runtimeMesh.RecalculateNormals();
            _runtimeMesh.RecalculateBounds();

            Bounds bounds = _runtimeMesh.bounds;
            _boxCollider.center = bounds.center;
            _boxCollider.size = new Vector3(
                Mathf.Max(0.03f, bounds.size.x),
                Mathf.Max(0.03f, bounds.size.y),
                Mathf.Max(0.03f, bounds.size.z));
        }

        private static void AddQuad(
            List<int> triangles,
            int bottomLeft,
            int bottomRight,
            int topRight,
            int topLeft)
        {
            triangles.Add(bottomLeft);
            triangles.Add(bottomRight);
            triangles.Add(topRight);
            triangles.Add(bottomLeft);
            triangles.Add(topRight);
            triangles.Add(topLeft);
        }

        private static Vector3 GetRingPosition(double phase, float radius)
        {
            double angleRadians = phase * Mathf.PI * 2d;
            return new Vector3(
                (float)(System.Math.Sin(angleRadians) * radius),
                0f,
                (float)(System.Math.Cos(angleRadians) * radius));
        }
    }
}
