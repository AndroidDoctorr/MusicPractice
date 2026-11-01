using System;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

namespace MusicPractice.SpatialLoop
{
    [RequireComponent(typeof(LoopTransport), typeof(LoopRingView), typeof(DemoLoopScheduler))]
    public sealed class SpatialLoopController : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private LoopTransport _transport;
        [SerializeField] private LoopRingView _ringView;
        [SerializeField] private DemoLoopScheduler _demoScheduler;
        [SerializeField] private Material _sampleMaterial;
        [SerializeField] private SampleInstanceView _sampleViewPrefab;

        [Header("Initial Demo Pattern")]
        [SerializeField, Min(1)] private long _defaultDurationTicks = 240;

        [Header("Time Snapping")]
        [SerializeField, Min(1)] private int _snapSubdivisionsPerGroup = 4;
        [SerializeField, Min(0)] private long _maximumSnapDistanceTicks = 180;

        private readonly Dictionary<string, SampleInstanceView> _views =
            new Dictionary<string, SampleInstanceView>();
        private readonly Dictionary<string, SampleAssetData> _assets =
            new Dictionary<string, SampleAssetData>();
        private readonly LoopEditHistory _history = new LoopEditHistory();
        private readonly List<SnapCandidate> _snapCandidates = new List<SnapCandidate>();
        private LoopData _loop;
        private Transform _viewRoot;
        private SampleInstanceView _selectedView;
        private SpatialPerformanceController _performance;

        public float RingRadius => _ringView != null ? _ringView.Radius : 0.85f;
        public Material SampleMaterial => _sampleMaterial;
        public long LoopLengthTicks =>
            _loop != null ? _loop.LengthTicks : _transport.LoopLengthTicks;
        public TimeSignatureData LoopTimeSignature =>
            _loop != null ? _loop.TimeSignature : _transport.TimeSignature;

        private void Reset()
        {
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void Start()
        {
            if (_sampleMaterial == null)
            {
                Debug.LogError(
                    "SpatialLoopController needs the sample material assigned. Use the Unlit sample material, not the Ring material.",
                    this);
                enabled = false;
                return;
            }

            CreateViewRoot();
            if (_demoScheduler.DemoClip != null)
            {
                RegisterAsset(new SampleAssetData(
                    "Demo Sample",
                    _demoScheduler.DemoClip,
                    Color.red,
                    null,
                    "demo-sample"));
            }
            CreateInitialLoop();
            RebuildSnapCandidates();
            RebuildAllViews();
            SyncScheduler();

            _performance = GetComponent<SpatialPerformanceController>();
            if (_performance == null)
            {
                _performance = gameObject.AddComponent<SpatialPerformanceController>();
            }

            _performance.Initialize(this);
        }

        public void Select(SampleInstanceView view)
        {
            if (view != null && !view.IsPendingDeletion)
            {
                _selectedView = view;
            }
        }

        public void NotifyGrabbed(SampleInstanceView view, Hand hand)
        {
            Select(view);
            _performance?.NotifyInstanceGrabbed(hand.handType, view);
        }

        public void NotifyReleased(SampleInstanceView view, Hand hand)
        {
            _performance?.NotifyInstanceReleased(hand.handType, view);
        }

        public void CommitDrop(SampleInstanceView view, long previousStartTick)
        {
            if (view == null || view.Data == null || view.IsPendingDeletion)
            {
                return;
            }

            if (_performance != null && _performance.TryCommitInstanceDrop(view))
            {
                return;
            }

            long newStartTick = GetSnappedStartTick(view);
            if (newStartTick == previousStartTick)
            {
                ApplyMove(view.Data, newStartTick);
                return;
            }

            SampleInstanceData data = view.Data;
            _history.Execute(new DelegateLoopEditCommand(
                () => ApplyMove(data, newStartTick),
                () => ApplyMove(data, previousStartTick)));
        }

        [ContextMenu("Clone Selected Sample")]
        public void CloneSelected()
        {
            if (_selectedView != null)
            {
                Clone(_selectedView);
            }
        }

        public void CloneHeldInstance(SampleInstanceView view)
        {
            if (view == null || view.IsPendingDeletion)
            {
                return;
            }

            Select(view);
            Clone(view);
        }

        public void DeleteHeldInstance(SampleInstanceView view, Hand hand)
        {
            if (view == null || view.Data == null || view.IsPendingDeletion)
            {
                return;
            }

            view.PrepareForDeletion();
            if (hand != null && hand.ObjectIsAttached(view.gameObject))
            {
                hand.DetachObject(view.gameObject);
            }

            Delete(view.Data);
        }

        [ContextMenu("Delete Selected Sample")]
        public void DeleteSelected()
        {
            if (_selectedView != null)
            {
                Delete(_selectedView.Data);
            }
        }

        [ContextMenu("Undo")]
        public void Undo()
        {
            _history.TryUndo();
        }

        [ContextMenu("Redo")]
        public void Redo()
        {
            _history.TryRedo();
        }

        public void RegisterAsset(SampleAssetData asset)
        {
            if (asset != null)
            {
                _assets[asset.Id] = asset;
            }
        }

        public SampleAssetData GetAsset(string assetId)
        {
            return !string.IsNullOrWhiteSpace(assetId)
                && _assets.TryGetValue(assetId, out SampleAssetData asset)
                ? asset
                : null;
        }

        public void Audition(
            SampleAssetData asset,
            EffectChainData effects)
        {
            if (asset?.Clip != null)
            {
                _demoScheduler.PlayImmediate(
                    asset.Clip,
                    0.9f,
                    0f,
                    effects ?? asset.DefaultEffects);
            }
        }

        public void RefreshPlayback()
        {
            SyncScheduler();
        }

        public void AddSingleAssetAtWorld(
            SampleAssetData asset,
            Vector3 worldPosition)
        {
            if (asset == null)
            {
                return;
            }

            Vector3 local = transform.InverseTransformPoint(worldPosition);
            double angle = Math.Atan2(local.x, local.z) * Mathf.Rad2Deg;
            if (angle < 0d)
            {
                angle += 360d;
            }

            long tick = MusicalTime.ClockwiseAngleDegreesToTick(
                angle,
                LoopLengthTicks);
            if (SnapResolver.TryResolveNearest(
                tick,
                LoopLengthTicks,
                _snapCandidates,
                _maximumSnapDistanceTicks,
                out SnapResult snap))
            {
                tick = snap.Tick;
            }

            AddAssetPlacements(
                asset,
                new[] { new PlacementSpec(tick, 0.8f) });
        }

        public void AddAssetPlacements(
            SampleAssetData asset,
            IReadOnlyList<PlacementSpec> placements)
        {
            if (asset == null || placements == null || placements.Count == 0)
            {
                return;
            }

            RegisterAsset(asset);
            var created = new List<SampleInstanceData>(placements.Count);
            for (int i = 0; i < placements.Count; i++)
            {
                created.Add(CreateInstance(asset, placements[i]));
            }

            _history.Execute(new DelegateLoopEditCommand(
                () =>
                {
                    for (int i = 0; i < created.Count; i++)
                    {
                        AddInstance(created[i]);
                    }
                },
                () =>
                {
                    for (int i = 0; i < created.Count; i++)
                    {
                        RemoveInstance(created[i]);
                    }
                }));
        }

        public void ReplaceInstanceWithPlacements(
            SampleInstanceData source,
            IReadOnlyList<PlacementSpec> placements)
        {
            SampleAssetData asset = source != null
                ? GetAsset(source.SampleAssetId)
                : null;
            if (asset == null || placements == null || placements.Count == 0)
            {
                return;
            }

            var created = new List<SampleInstanceData>(placements.Count);
            for (int i = 0; i < placements.Count; i++)
            {
                created.Add(CreateInstance(asset, placements[i]));
            }

            _history.Execute(new DelegateLoopEditCommand(
                () =>
                {
                    RemoveInstance(source);
                    for (int i = 0; i < created.Count; i++)
                    {
                        AddInstance(created[i]);
                    }
                },
                () =>
                {
                    for (int i = 0; i < created.Count; i++)
                    {
                        RemoveInstance(created[i]);
                    }

                    AddInstance(source);
                }));
        }

        private SampleInstanceData CreateInstance(
            SampleAssetData asset,
            PlacementSpec placement)
        {
            long durationTicks = (long)Math.Round(
                MusicalTime.SecondsToTicks(
                    asset.DurationSeconds,
                    _transport.BeatsPerMinute,
                    _transport.TimeSignature.TempoBeatUnit));
            durationTicks = Math.Max(1L, Math.Min(durationTicks, LoopLengthTicks));
            return new SampleInstanceData(
                asset.Id,
                MusicalTime.NormalizeTick(placement.Tick, LoopLengthTicks),
                durationTicks,
                placement.Velocity,
                0f,
                asset.DefaultEffects);
        }

        private void Clone(SampleInstanceView sourceView)
        {
            SampleInstanceData source = sourceView.Data;
            if (source == null)
            {
                return;
            }

            var clone = new SampleInstanceData(
                source.SampleAssetId,
                source.StartTick,
                source.DurationTicks,
                source.Velocity,
                source.PitchSemitones,
                source.Effects);

            _history.Execute(new DelegateLoopEditCommand(
                () => AddInstance(clone),
                () => RemoveInstance(clone)));
        }

        private void Delete(SampleInstanceData data)
        {
            if (data == null)
            {
                return;
            }

            _history.Execute(new DelegateLoopEditCommand(
                () => RemoveInstance(data),
                () => AddInstance(data)));
        }

        private void ApplyMove(SampleInstanceData data, long startTick)
        {
            long normalizedTick = MusicalTime.NormalizeTick(
                startTick,
                LoopLengthTicks);
            data.SetTiming(normalizedTick, data.DurationTicks);

            if (_views.TryGetValue(data.Id, out SampleInstanceView view)
                && view != null)
            {
                view.transform.SetParent(_viewRoot, false);
                view.RefreshFromModel();
            }

            SyncScheduler();
        }

        private void AddInstance(SampleInstanceData data)
        {
            _loop.AddInstance(data);
            CreateView(data);
            SyncScheduler();
        }

        private void RemoveInstance(SampleInstanceData data)
        {
            _loop.RemoveInstance(data.Id);

            if (_views.TryGetValue(data.Id, out SampleInstanceView view))
            {
                _views.Remove(data.Id);
                if (_selectedView == view)
                {
                    _selectedView = null;
                }

                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }

            SyncScheduler();
        }

        private long GetSnappedStartTick(SampleInstanceView view)
        {
            Vector3 localPosition = transform.InverseTransformPoint(
                view.transform.position);
            double angle = Math.Atan2(localPosition.x, localPosition.z)
                * Mathf.Rad2Deg;
            if (angle < 0d)
            {
                angle += 360d;
            }

            long rawStartTick = MusicalTime.ClockwiseAngleDegreesToTick(
                angle,
                LoopLengthTicks);

            if (SnapResolver.TryResolveNearest(
                rawStartTick,
                LoopLengthTicks,
                _snapCandidates,
                _maximumSnapDistanceTicks,
                out SnapResult snap))
            {
                return snap.Tick;
            }

            return rawStartTick;
        }

        private void CreateInitialLoop()
        {
            _loop = new LoopData("Spatial Loop", _transport.Bars, _transport.TimeSignature);

            TimeSignatureData signature = _transport.TimeSignature;
            long ticksPerBar = MusicalTime.GetTicksPerBar(signature);
            long ticksPerUnit = MusicalTime.GetTicksPerDenominatorUnit(
                signature.Denominator);
            int[] groups = signature.GetBeatGroups();
            long duration = Math.Min(
                Math.Max(1L, _defaultDurationTicks),
                _loop.LengthTicks);

            for (int bar = 0; bar < _transport.Bars; bar++)
            {
                long groupStart = bar * ticksPerBar;
                for (int groupIndex = 0; groupIndex < groups.Length; groupIndex++)
                {
                    _loop.AddInstance(new SampleInstanceData(
                        "demo-sample",
                        groupStart,
                        duration,
                        0.8f));
                    groupStart += groups[groupIndex] * ticksPerUnit;
                }
            }
        }

        private void RebuildSnapCandidates()
        {
            _snapCandidates.Clear();
            _snapCandidates.AddRange(SnapResolver.BuildMeterCandidates(
                _transport.TimeSignature,
                _transport.Bars,
                Mathf.Max(1, _snapSubdivisionsPerGroup)));
        }

        private void RebuildAllViews()
        {
            foreach (SampleInstanceView view in _views.Values)
            {
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }

            _views.Clear();

            IReadOnlyList<SampleInstanceData> instances = _loop.Instances;
            for (int i = 0; i < instances.Count; i++)
            {
                CreateView(instances[i]);
            }
        }

        private void CreateView(SampleInstanceData data)
        {
            SampleInstanceView view;
            if (_sampleViewPrefab != null)
            {
                view = Instantiate(_sampleViewPrefab, _viewRoot);
            }
            else
            {
                var viewObject = new GameObject("Sample Instance");
                viewObject.transform.SetParent(_viewRoot, false);
                view = viewObject.AddComponent<SampleInstanceView>();
            }

            view.Initialize(this, data, _sampleMaterial);
            _views[data.Id] = view;
        }

        private void CreateViewRoot()
        {
            var viewRootObject = new GameObject("Sample Instance Views");
            _viewRoot = viewRootObject.transform;
            _viewRoot.SetParent(transform, false);
        }

        private void SyncScheduler()
        {
            if (_demoScheduler == null || _loop == null)
            {
                return;
            }

            var playbackEvents = new List<SamplePlaybackEvent>(
                _loop.Instances.Count);
            for (int i = 0; i < _loop.Instances.Count; i++)
            {
                SampleInstanceData instance = _loop.Instances[i];
                SampleAssetData asset = GetAsset(instance.SampleAssetId);
                if (asset?.Clip == null)
                {
                    continue;
                }

                playbackEvents.Add(new SamplePlaybackEvent(
                    instance.StartTick,
                    asset.Clip,
                    instance.Velocity,
                    instance.PitchSemitones,
                    instance.Effects));
            }

            _demoScheduler.SetEvents(playbackEvents);
        }

        private void ResolveReferences()
        {
            if (_transport == null)
            {
                _transport = GetComponent<LoopTransport>();
            }

            if (_ringView == null)
            {
                _ringView = GetComponent<LoopRingView>();
            }

            if (_demoScheduler == null)
            {
                _demoScheduler = GetComponent<DemoLoopScheduler>();
            }
        }

        private void OnValidate()
        {
            _defaultDurationTicks = Math.Max(1L, _defaultDurationTicks);
            _snapSubdivisionsPerGroup = Mathf.Max(1, _snapSubdivisionsPerGroup);
            _maximumSnapDistanceTicks = Math.Max(0L, _maximumSnapDistanceTicks);
        }
    }
}
