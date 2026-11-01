using System.Collections.Generic;
using UnityEngine;

namespace MusicPractice.SpatialLoop
{
    [RequireComponent(typeof(LoopTransport))]
    public sealed class LoopRingView : MonoBehaviour
    {
        [SerializeField] private LoopTransport _transport;
        [SerializeField] private Material _lineMaterial;
        [SerializeField] private Material _playheadMaterial;
        [SerializeField, Min(0.1f)] private float _radius = 0.85f;
        [SerializeField, Range(32, 512)] private int _ringSegments = 128;
        [SerializeField, Min(0.001f)] private float _lineWidth = 0.01f;
        [SerializeField, Min(0.001f)] private float _markerWidth = 0.012f;
        [SerializeField, Min(0.01f)] private float _minorMarkerLength = 0.06f;
        [SerializeField, Min(0.01f)] private float _majorMarkerLength = 0.12f;
        [SerializeField, Min(0.01f)] private float _playheadHeight = 0.3f;
        [SerializeField, Min(0.002f)] private float _playheadRadius = 0.012f;

        private Transform _generatedRoot;
        private Transform _playhead;
        private Material _fallbackMaterial;

        public float Radius => Mathf.Max(0.1f, _radius);

        private void Reset()
        {
            _transport = GetComponent<LoopTransport>();
        }

        private void Awake()
        {
            if (_transport == null)
            {
                _transport = GetComponent<LoopTransport>();
            }

            Rebuild();
        }

        private void Update()
        {
            if (_transport == null || _playhead == null)
            {
                return;
            }

            double phase = _transport.GetLoopPhase(AudioSettings.dspTime);
            _playhead.localPosition = GetLocalRingPosition(phase, _radius);
        }

        private void OnDestroy()
        {
            if (_fallbackMaterial == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_fallbackMaterial);
            }
            else
            {
                DestroyImmediate(_fallbackMaterial);
            }
        }

        public void Rebuild()
        {
            DestroyGeneratedRoot();

            var rootObject = new GameObject("Generated Ring");
            _generatedRoot = rootObject.transform;
            _generatedRoot.SetParent(transform, false);

            BuildRingLine();
            BuildMeterMarkers();
            BuildPlayhead();
        }

        private void BuildRingLine()
        {
            var ringObject = new GameObject("Ring");
            ringObject.transform.SetParent(_generatedRoot, false);

            LineRenderer line = ringObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = Mathf.Max(32, _ringSegments);
            line.startWidth = _lineWidth;
            line.endWidth = _lineWidth;
            line.numCornerVertices = 2;
            line.sharedMaterial = GetLineMaterial();

            for (int i = 0; i < line.positionCount; i++)
            {
                double phase = i / (double)line.positionCount;
                line.SetPosition(i, GetLocalRingPosition(phase, _radius));
            }
        }

        private void BuildMeterMarkers()
        {
            if (_transport == null)
            {
                return;
            }

            var markersObject = new GameObject("Meter Markers");
            markersObject.transform.SetParent(_generatedRoot, false);

            TimeSignatureData signature = _transport.TimeSignature;
            long ticksPerBar = MusicalTime.GetTicksPerBar(signature);
            long ticksPerUnit = MusicalTime.GetTicksPerDenominatorUnit(
                signature.Denominator);
            long loopLengthTicks = _transport.LoopLengthTicks;
            int[] groups = signature.GetBeatGroups();

            var groupStarts = new HashSet<long>();
            for (int bar = 0; bar < _transport.Bars; bar++)
            {
                long groupStart = bar * ticksPerBar;
                for (int groupIndex = 0; groupIndex < groups.Length; groupIndex++)
                {
                    groupStarts.Add(groupStart);
                    groupStart += groups[groupIndex] * ticksPerUnit;
                }
            }

            for (int bar = 0; bar < _transport.Bars; bar++)
            {
                long barStart = bar * ticksPerBar;
                for (int unit = 0; unit < signature.Numerator; unit++)
                {
                    long tick = barStart + unit * ticksPerUnit;
                    bool isBarStart = unit == 0;
                    bool isGroupStart = groupStarts.Contains(tick);
                    float markerLength = isBarStart
                        ? _majorMarkerLength * 1.35f
                        : isGroupStart
                            ? _majorMarkerLength
                            : _minorMarkerLength;
                    string markerName = isBarStart
                        ? $"Bar {bar + 1}"
                        : isGroupStart
                            ? $"Group {tick}"
                            : $"Unit {tick}";

                    CreateMarker(
                        markersObject.transform,
                        markerName,
                        MusicalTime.TickToPhase(tick, loopLengthTicks),
                        markerLength);
                }
            }
        }

        private void CreateMarker(
            Transform parent,
            string markerName,
            double phase,
            float markerLength)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = markerName;
            marker.transform.SetParent(parent, false);

            float angle = (float)(phase * 360d);
            marker.transform.localPosition = GetLocalRingPosition(phase, _radius);
            marker.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
            marker.transform.localScale = new Vector3(
                _markerWidth,
                _markerWidth,
                markerLength);

            Collider markerCollider = marker.GetComponent<Collider>();
            if (markerCollider != null)
            {
                Destroy(markerCollider);
            }

            Renderer markerRenderer = marker.GetComponent<Renderer>();
            markerRenderer.sharedMaterial = GetLineMaterial();
        }

        private void BuildPlayhead()
        {
            GameObject playheadObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            playheadObject.name = "Playhead";
            _playhead = playheadObject.transform;
            _playhead.SetParent(_generatedRoot, false);
            _playhead.localScale = new Vector3(
                _playheadRadius * 2f,
                _playheadHeight * 0.5f,
                _playheadRadius * 2f);
            _playhead.localPosition = GetLocalRingPosition(0d, _radius);

            Collider playheadCollider = playheadObject.GetComponent<Collider>();
            if (playheadCollider != null)
            {
                Destroy(playheadCollider);
            }

            Renderer playheadRenderer = playheadObject.GetComponent<Renderer>();
            playheadRenderer.sharedMaterial = _playheadMaterial != null
                ? _playheadMaterial
                : GetLineMaterial();
        }

        private Material GetLineMaterial()
        {
            if (_lineMaterial != null)
            {
                return _lineMaterial;
            }

            if (_fallbackMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                _fallbackMaterial = new Material(shader)
                {
                    name = "Spatial Loop Fallback Material",
                    color = Color.cyan
                };
            }

            return _fallbackMaterial;
        }

        private Vector3 GetLocalRingPosition(double phase, float radius)
        {
            double angleRadians = phase * Mathf.PI * 2d;
            return new Vector3(
                (float)(System.Math.Sin(angleRadians) * radius),
                0f,
                (float)(System.Math.Cos(angleRadians) * radius));
        }

        private void DestroyGeneratedRoot()
        {
            if (_generatedRoot == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_generatedRoot.gameObject);
            }
            else
            {
                DestroyImmediate(_generatedRoot.gameObject);
            }

            _generatedRoot = null;
            _playhead = null;
        }

        private void OnDrawGizmosSelected()
        {
            float radius = Mathf.Max(0.1f, _radius);
            const int previewSegments = 64;
            Gizmos.color = Color.cyan;
            Gizmos.matrix = transform.localToWorldMatrix;

            Vector3 previous = GetLocalRingPosition(0d, radius);
            for (int i = 1; i <= previewSegments; i++)
            {
                double phase = i / (double)previewSegments;
                Vector3 next = GetLocalRingPosition(phase, radius);
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }

        private void OnValidate()
        {
            _radius = Mathf.Max(0.1f, _radius);
            _ringSegments = Mathf.Clamp(_ringSegments, 32, 512);
            _lineWidth = Mathf.Max(0.001f, _lineWidth);
            _markerWidth = Mathf.Max(0.001f, _markerWidth);
            _minorMarkerLength = Mathf.Max(0.01f, _minorMarkerLength);
            _majorMarkerLength = Mathf.Max(_minorMarkerLength, _majorMarkerLength);
            _playheadHeight = Mathf.Max(0.01f, _playheadHeight);
            _playheadRadius = Mathf.Max(0.002f, _playheadRadius);
        }
    }
}
