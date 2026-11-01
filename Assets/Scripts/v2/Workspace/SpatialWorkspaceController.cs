using System;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

namespace MusicPractice.SpatialLoop
{
    public enum WorkspacePage
    {
        Bank,
        Place,
        Arpeggiator
    }

    public readonly struct PlacementSpec
    {
        public long Tick { get; }
        public float Velocity { get; }

        public PlacementSpec(long tick, float velocity)
        {
            Tick = tick;
            Velocity = velocity;
        }
    }

    public sealed class SpatialWorkspaceController : MonoBehaviour
    {
        private readonly List<SampleAssetData> _assets = new List<SampleAssetData>();
        private readonly Dictionary<string, SampleTokenView> _shelfTokens =
            new Dictionary<string, SampleTokenView>();
        private readonly List<PlacementPad> _pads = new List<PlacementPad>();
        private readonly Dictionary<int, Transform> _distributionPads =
            new Dictionary<int, Transform>();
        private readonly List<GameObject> _ghosts = new List<GameObject>();

        private SpatialLoopController _loopController;
        private SpatialPerformanceController _performance;
        private Transform _workspaceRoot;
        private Transform _bankRoot;
        private Transform _placeRoot;
        private Transform _arpRoot;
        private Transform _tabRoot;
        private Transform _ghostRoot;
        private WorkspacePage _activePage = WorkspacePage.Place;
        private readonly List<WorkspacePageTab> _tabs = new List<WorkspacePageTab>();
        private PatternDefinition _activePattern;
        private Material _padMaterial;
        private Material _ghostMaterial;
        private PlacementPad _previewPad;

        public void Initialize(
            SpatialLoopController loopController,
            SpatialPerformanceController performance)
        {
            _loopController = loopController;
            _performance = performance;
            _activePattern = SpatialPatterns.CreateHouse();
            CreateMaterials();
            BuildWorkspace();
        }

        private void OnDestroy()
        {
            if (_padMaterial != null)
            {
                Destroy(_padMaterial);
            }

            if (_ghostMaterial != null)
            {
                Destroy(_ghostMaterial);
            }
        }

        public void RegisterAsset(
            SampleAssetData asset,
            SteamVR_Input_Sources offeredTo)
        {
            if (asset == null)
            {
                return;
            }

            if (!_assets.Exists(existing => existing.Id == asset.Id))
            {
                _assets.Add(asset);
                _loopController.RegisterAsset(asset);
            }

            EnsureShelfToken(asset);
            CreateOfferedToken(asset, offeredTo);
        }

        public void NotifyTokenGrabbed(SampleTokenView token)
        {
            if (token == null || !token.IsShelfToken)
            {
                return;
            }

            _shelfTokens.Remove(token.Asset.Id);
            EnsureShelfToken(token.Asset);
        }

        public void CommitTokenDrop(SampleTokenView token)
        {
            if (token == null || token.Asset == null)
            {
                return;
            }

            PlacementPad pad = FindPad(token.transform.position);
            if (pad != null)
            {
                _loopController.AddAssetPlacements(
                    token.Asset,
                    GetPlacements(pad));
                Destroy(token.gameObject);
                ClearPreview();
                EnsureShelfToken(token.Asset);
                return;
            }

            Vector3 local = _loopController.transform.InverseTransformPoint(
                token.transform.position);
            float radialDistance = new Vector2(local.x, local.z).magnitude;
            if (Mathf.Abs(radialDistance - _loopController.RingRadius) < 0.2f)
            {
                _loopController.AddSingleAssetAtWorld(
                    token.Asset,
                    token.transform.position);
                Destroy(token.gameObject);
                EnsureShelfToken(token.Asset);
                return;
            }

            ReturnTokenToBank(token);
        }

        private void ReturnTokenToBank(SampleTokenView token)
        {
            if (token == null || token.Asset == null)
            {
                return;
            }

            if (token.IsShelfToken)
            {
                token.transform.SetParent(_bankRoot, false);
                int index = _assets.FindIndex(item => item.Id == token.Asset.Id);
                token.transform.localPosition = new Vector3(
                    (index % 3) * 0.09f,
                    -(index / 3) * 0.09f,
                    0f);
                return;
            }

            Destroy(token.gameObject);
            EnsureShelfToken(token.Asset);
        }

        public bool TryCommitInstanceDrop(SampleInstanceView view)
        {
            if (view == null || view.Data == null)
            {
                return false;
            }

            PlacementPad pad = FindPad(view.transform.position);
            if (pad == null)
            {
                return false;
            }

            _loopController.ReplaceInstanceWithPlacements(
                view.Data,
                GetPlacements(pad));
            ClearPreview();
            return true;
        }

        public void BeginPreview(PlacementPad pad, SampleAssetData asset)
        {
            if (pad == null || asset == null)
            {
                return;
            }

            _previewPad = pad;
            ShowGhosts(GetPlacements(pad));
            HighlightRecommendation(asset);
        }

        public void BeginPreview(PlacementPad pad, SampleInstanceData instance)
        {
            BeginPreview(pad, _loopController.GetAsset(instance.SampleAssetId));
        }

        public void EndPreview(PlacementPad pad)
        {
            if (_previewPad == pad)
            {
                ClearPreview();
            }
        }

        public void SetPage(WorkspacePage page)
        {
            if (_activePage == page)
            {
                return;
            }

            _activePage = page;
            ApplyPageVisibility();
            RefreshTabHighlights();
            ClearPreview();
        }

        private void BuildWorkspace()
        {
            var workspace = new GameObject("Spatial Workspace");
            _workspaceRoot = workspace.transform;
            _workspaceRoot.SetParent(transform, false);
            _workspaceRoot.localPosition = new Vector3(0f, -0.1f, 0.48f);

            var tabs = new GameObject("Workspace Tabs");
            _tabRoot = tabs.transform;
            _tabRoot.SetParent(_workspaceRoot, false);
            _tabRoot.localPosition = new Vector3(0f, 0.24f, -0.02f);
            CreatePageTab(_tabRoot, WorkspacePage.Bank, "BANK", new Vector3(-0.2f, 0f, 0f));
            CreatePageTab(_tabRoot, WorkspacePage.Place, "PLACE", new Vector3(0f, 0f, 0f));
            CreatePageTab(
                _tabRoot,
                WorkspacePage.Arpeggiator,
                "ARP",
                new Vector3(0.2f, 0f, 0f));

            var bank = new GameObject("Sample Bank");
            _bankRoot = bank.transform;
            _bankRoot.SetParent(_workspaceRoot, false);
            CreateLabel(_bankRoot, "SAMPLES", new Vector3(0f, 0.18f, 0f));

            var place = new GameObject("Placement Pads");
            _placeRoot = place.transform;
            _placeRoot.SetParent(_workspaceRoot, false);

            SampleRole[] roles =
            {
                SampleRole.Kick,
                SampleRole.OpenHat,
                SampleRole.Clap,
                SampleRole.ClosedHat,
                SampleRole.Snare
            };
            int[] counts = { 1, 2, 4, 8, 16 };

            for (int i = 0; i < roles.Length; i++)
            {
                float x = (i - 2) * 0.13f;
                CreateRolePad(_placeRoot, roles[i], new Vector3(x, 0.07f, 0f));
                CreateDistributionPad(
                    _placeRoot,
                    counts[i],
                    new Vector3(x, -0.07f, 0f));
            }

            CreateLabel(
                _placeRoot,
                "PATTERN ROLES",
                new Vector3(0f, 0.2f, 0f));
            CreateLabel(
                _placeRoot,
                "DISTRIBUTION",
                new Vector3(0f, -0.2f, 0f));

            var arp = new GameObject("Arpeggiator Pads");
            _arpRoot = arp.transform;
            _arpRoot.SetParent(_workspaceRoot, false);
            IReadOnlyList<ArpeggiatorPattern> arpPatterns =
                ArpeggiatorPatterns.PrototypeSet;
            for (int i = 0; i < arpPatterns.Count; i++)
            {
                float x = (i - (arpPatterns.Count - 1) * 0.5f) * 0.13f;
                CreateArpeggiatorPad(
                    _arpRoot,
                    arpPatterns[i],
                    new Vector3(x, 0f, 0f));
            }

            CreateLabel(
                _arpRoot,
                "ARPEGGIATOR",
                new Vector3(0f, 0.2f, 0f));
            CreateLabel(
                _arpRoot,
                "DROP SAMPLE ON PATTERN",
                new Vector3(0f, -0.2f, 0f),
                0.85f);

            var ghosts = new GameObject("Placement Ghosts");
            _ghostRoot = ghosts.transform;
            _ghostRoot.SetParent(_loopController.transform, false);

            ApplyPageVisibility();
            RefreshTabHighlights();
        }

        private void CreatePageTab(
            Transform parent,
            WorkspacePage page,
            string label,
            Vector3 localPosition)
        {
            GameObject tabObject = CreatePadObject(parent, localPosition);
            tabObject.transform.localScale = new Vector3(0.17f, 0.04f, 0.07f);
            WorkspacePageTab tab = tabObject.AddComponent<WorkspacePageTab>();
            tab.Initialize(this, page);
            _tabs.Add(tab);
            CreateLabel(tabObject.transform, label, new Vector3(0f, 0.03f, 0f));
        }

        private void ApplyPageVisibility()
        {
            if (_bankRoot != null)
            {
                _bankRoot.gameObject.SetActive(_activePage == WorkspacePage.Bank);
                _bankRoot.localPosition = Vector3.zero;
            }

            if (_placeRoot != null)
            {
                _placeRoot.gameObject.SetActive(_activePage == WorkspacePage.Place);
                _placeRoot.localPosition = Vector3.zero;
            }

            if (_arpRoot != null)
            {
                _arpRoot.gameObject.SetActive(_activePage == WorkspacePage.Arpeggiator);
                _arpRoot.localPosition = Vector3.zero;
            }
        }

        private void RefreshTabHighlights()
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                WorkspacePageTab tab = _tabs[i];
                bool active = tab.Page == _activePage;
                SetPadColor(
                    tab.gameObject,
                    active
                        ? new Color(1f, 0.85f, 0.2f, 0.55f)
                        : new Color(0.35f, 0.4f, 0.5f, 0.28f));
            }
        }

        private void CreateArpeggiatorPad(
            Transform parent,
            ArpeggiatorPattern pattern,
            Vector3 localPosition)
        {
            GameObject padObject = CreatePadObject(parent, localPosition);
            SetPadColor(
                padObject,
                new Color(0.95f, 0.45f, 1f, 0.36f));
            PlacementPad pad = padObject.AddComponent<PlacementPad>();
            pad.InitializeArpeggiator(this, pattern);
            _pads.Add(pad);
            CreateLabel(
                padObject.transform,
                pattern.Name,
                new Vector3(0f, 0.045f, 0f));
        }

        private void CreateRolePad(
            Transform parent,
            SampleRole role,
            Vector3 localPosition)
        {
            GameObject padObject = CreatePadObject(parent, localPosition);
            SetPadColor(padObject, GetRoleColor(role));
            PlacementPad pad = padObject.AddComponent<PlacementPad>();
            pad.InitializeRole(this, role);
            _pads.Add(pad);
            CreateLabel(
                padObject.transform,
                GetShortRoleName(role),
                new Vector3(0f, 0.05f, 0f));
        }

        private void CreateDistributionPad(
            Transform parent,
            int count,
            Vector3 localPosition)
        {
            GameObject padObject = CreatePadObject(parent, localPosition);
            SetPadColor(
                padObject,
                new Color(0.25f, 0.75f, 1f, 0.32f));
            PlacementPad pad = padObject.AddComponent<PlacementPad>();
            pad.InitializeDistribution(this, count);
            _pads.Add(pad);
            _distributionPads[count] = padObject.transform;
            CreateLabel(
                padObject.transform,
                count == 1 ? "ONCE" : count.ToString(),
                new Vector3(0f, 0.05f, 0f));
        }

        private GameObject CreatePadObject(Transform parent, Vector3 localPosition)
        {
            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.transform.SetParent(parent, false);
            pad.transform.localPosition = localPosition;
            pad.transform.localScale = new Vector3(0.11f, 0.045f, 0.11f);
            pad.GetComponent<Renderer>().sharedMaterial = _padMaterial;
            return pad;
        }

        private void EnsureShelfToken(SampleAssetData asset)
        {
            if (_shelfTokens.TryGetValue(asset.Id, out SampleTokenView existing)
                && existing != null)
            {
                return;
            }

            SampleTokenView token = CreateToken(asset, true);
            token.transform.SetParent(_bankRoot, false);
            int index = _assets.FindIndex(item => item.Id == asset.Id);
            token.transform.localPosition = new Vector3(
                (index % 3) * 0.09f,
                -(index / 3) * 0.09f,
                0f);
            _shelfTokens[asset.Id] = token;
        }

        private void CreateOfferedToken(
            SampleAssetData asset,
            SteamVR_Input_Sources source)
        {
            Hand hand = _performance.GetHand(source);
            if (hand == null)
            {
                return;
            }

            SampleTokenView token = CreateToken(asset, false);
            token.OfferToHand(hand);
        }

        private SampleTokenView CreateToken(
            SampleAssetData asset,
            bool isShelfToken)
        {
            GameObject tokenObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tokenObject.transform.localScale = Vector3.one * 0.065f;
            tokenObject.GetComponent<Renderer>().sharedMaterial =
                _loopController.SampleMaterial;
            SampleTokenView token = tokenObject.AddComponent<SampleTokenView>();
            token.Initialize(this, _performance, asset, isShelfToken);
            return token;
        }

        private PlacementPad FindPad(Vector3 worldPosition)
        {
            for (int i = 0; i < _pads.Count; i++)
            {
                if (_pads[i].WorldBounds.Contains(worldPosition))
                {
                    return _pads[i];
                }
            }

            return null;
        }

        private List<PlacementSpec> GetPlacements(PlacementPad pad)
        {
            var placements = new List<PlacementSpec>();
            if (pad.Kind == PlacementPadKind.Distribution)
            {
                int count = Mathf.Max(1, pad.DistributionCount);
                for (int i = 0; i < count; i++)
                {
                    placements.Add(new PlacementSpec(
                        _loopController.LoopLengthTicks * i / count,
                        0.8f));
                }

                return placements;
            }

            if (pad.Kind == PlacementPadKind.Arpeggiator)
            {
                ArpeggiatorPattern pattern = pad.ArpeggiatorPattern;
                if (pattern?.SixteenthSteps == null
                    || pattern.SixteenthSteps.Length == 0)
                {
                    return placements;
                }

                TimeSignatureData signature = _loopController.LoopTimeSignature;
                long barLength = MusicalTime.GetTicksPerBar(signature);
                int loopBars = Mathf.Max(
                    1,
                    (int)(_loopController.LoopLengthTicks / barLength));
                long ticksPerSixteenth = MusicalTime.PulsesPerQuarter / 4;
                for (int bar = 0; bar < loopBars; bar++)
                {
                    for (int i = 0; i < pattern.SixteenthSteps.Length; i++)
                    {
                        placements.Add(new PlacementSpec(
                            bar * barLength
                                + pattern.SixteenthSteps[i] * ticksPerSixteenth,
                            pattern.Velocity));
                    }
                }

                return placements;
            }

            long patternLength = MusicalTime.GetTicksPerBar(
                _activePattern.TimeSignature);
            int bars = Mathf.Max(
                1,
                (int)(_loopController.LoopLengthTicks / patternLength));
            foreach (PatternPoint point in _activePattern.GetPoints(pad.Role))
            {
                for (int bar = 0; bar < bars; bar++)
                {
                    placements.Add(new PlacementSpec(
                        point.Tick + bar * patternLength,
                        point.Velocity));
                }
            }

            return placements;
        }

        private void ShowGhosts(IReadOnlyList<PlacementSpec> placements)
        {
            ClearGhosts();
            for (int i = 0; i < placements.Count; i++)
            {
                GameObject ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ghost.name = "Placement Ghost";
                ghost.transform.SetParent(_ghostRoot, false);
                double phase = MusicalTime.TickToPhase(
                    placements[i].Tick,
                    _loopController.LoopLengthTicks);
                double radians = phase * Mathf.PI * 2d;
                ghost.transform.localPosition = new Vector3(
                    (float)Math.Sin(radians) * _loopController.RingRadius,
                    0.06f,
                    (float)Math.Cos(radians) * _loopController.RingRadius);
                ghost.transform.localScale = new Vector3(0.025f, 0.08f, 0.025f);
                ghost.GetComponent<Renderer>().sharedMaterial = _ghostMaterial;
                Destroy(ghost.GetComponent<Collider>());
                _ghosts.Add(ghost);
            }
        }

        private void HighlightRecommendation(SampleAssetData asset)
        {
            int recommended = asset.DurationSeconds > 2f
                ? 1
                : asset.DurationSeconds > 1f
                    ? 2
                    : asset.DurationSeconds > 0.5f
                        ? 4
                        : asset.DurationSeconds > 0.25f
                            ? 8
                            : 16;

            foreach (KeyValuePair<int, Transform> pair in _distributionPads)
            {
                pair.Value.localScale = pair.Key == recommended
                    ? new Vector3(0.12f, 0.055f, 0.12f)
                    : new Vector3(0.11f, 0.045f, 0.11f);
            }
        }

        private void ClearPreview()
        {
            _previewPad = null;
            ClearGhosts();
            foreach (Transform pad in _distributionPads.Values)
            {
                pad.localScale = new Vector3(0.11f, 0.045f, 0.11f);
            }
        }

        private void ClearGhosts()
        {
            for (int i = 0; i < _ghosts.Count; i++)
            {
                if (_ghosts[i] != null)
                {
                    Destroy(_ghosts[i]);
                }
            }

            _ghosts.Clear();
        }

        private void CreateMaterials()
        {
            Shader shader = Shader.Find("Standard");
            _padMaterial = new Material(shader)
            {
                name = "Runtime Placement Pad",
                color = new Color(0.2f, 0.75f, 1f, 0.3f)
            };
            SetTransparent(_padMaterial);

            _ghostMaterial = new Material(shader)
            {
                name = "Runtime Placement Ghost",
                color = new Color(1f, 1f, 1f, 0.28f)
            };
            SetTransparent(_ghostMaterial);
        }

        private static void SetTransparent(Material material)
        {
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt(
                "_DstBlend",
                (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
        }

        private static void CreateLabel(
            Transform parent,
            string text,
            Vector3 localPosition,
            float scale = 1f)
        {
            var labelObject = new GameObject($"Label {text}");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = localPosition;
            labelObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            labelObject.transform.localScale = Vector3.one * (0.016f * scale);
            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.text = text;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 96;
            label.characterSize = 0.14f;
            label.color = Color.white;
        }

        private static string GetShortRoleName(SampleRole role)
        {
            switch (role)
            {
                case SampleRole.OpenHat:
                    return "OPEN";
                case SampleRole.ClosedHat:
                    return "CLOSED";
                default:
                    return role.ToString().ToUpperInvariant();
            }
        }

        private static Color GetRoleColor(SampleRole role)
        {
            switch (role)
            {
                case SampleRole.Kick:
                    return new Color(1f, 0.18f, 0.12f, 0.38f);
                case SampleRole.OpenHat:
                    return new Color(0.15f, 0.85f, 1f, 0.38f);
                case SampleRole.Clap:
                    return new Color(1f, 0.65f, 0.12f, 0.38f);
                case SampleRole.ClosedHat:
                    return new Color(0.2f, 1f, 0.45f, 0.38f);
                case SampleRole.Snare:
                    return new Color(0.75f, 0.25f, 1f, 0.38f);
                default:
                    return Color.white;
            }
        }

        private static void SetPadColor(GameObject pad, Color color)
        {
            Renderer renderer = pad.GetComponent<Renderer>();
            var properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            properties.SetColor("_Color", color);
            renderer.SetPropertyBlock(properties);
        }
    }
}
