using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

namespace MusicPractice.SpatialLoop
{
    [RequireComponent(typeof(SpatialHandInputRouter))]
    [RequireComponent(typeof(SampleCaptureService))]
    [RequireComponent(typeof(SpatialEffectsController))]
    [RequireComponent(typeof(SpatialWorkspaceController))]
    public sealed class SpatialPerformanceController : MonoBehaviour
    {
        private readonly Dictionary<SteamVR_Input_Sources, SampleInstanceView>
            _heldInstances =
                new Dictionary<SteamVR_Input_Sources, SampleInstanceView>();
        private readonly Dictionary<SteamVR_Input_Sources, SampleTokenView>
            _heldAssets =
                new Dictionary<SteamVR_Input_Sources, SampleTokenView>();

        private SpatialLoopController _loopController;
        private SpatialHandInputRouter _input;
        private SampleCaptureService _capture;
        private SpatialEffectsController _effects;
        private SpatialWorkspaceController _workspace;
        private bool _initialized;

        public void Initialize(SpatialLoopController loopController)
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            _loopController = loopController;
            _input = GetComponent<SpatialHandInputRouter>();
            _capture = GetComponent<SampleCaptureService>();
            _effects = GetComponent<SpatialEffectsController>();
            _workspace = GetComponent<SpatialWorkspaceController>();

            _input.TriggerPressed += HandleTriggerPressed;
            _input.TriggerReleased += HandleTriggerReleased;
            _input.TrackpadTouched += HandleTrackpadTouched;
            _input.TrackpadClicked += HandleTrackpadClicked;
            _input.TrackpadUpPressed += HandleTrackpadUpPressed;
            _input.TrackpadDownPressed += HandleTrackpadDownPressed;
            _input.MenuTapped += HandleMenuTapped;
            _input.MenuHeld += HandleMenuHeld;
            _capture.SampleCaptured += HandleSampleCaptured;
            _capture.RecordingStateChanged += HandleRecordingStateChanged;
            _workspace.Initialize(loopController, this);
        }

        private void OnDestroy()
        {
            if (!_initialized)
            {
                return;
            }

            _input.TriggerPressed -= HandleTriggerPressed;
            _input.TriggerReleased -= HandleTriggerReleased;
            _input.TrackpadTouched -= HandleTrackpadTouched;
            _input.TrackpadClicked -= HandleTrackpadClicked;
            _input.TrackpadUpPressed -= HandleTrackpadUpPressed;
            _input.TrackpadDownPressed -= HandleTrackpadDownPressed;
            _input.MenuTapped -= HandleMenuTapped;
            _input.MenuHeld -= HandleMenuHeld;
            _capture.SampleCaptured -= HandleSampleCaptured;
            _capture.RecordingStateChanged -= HandleRecordingStateChanged;
        }

        private void LateUpdate()
        {
            if (!_initialized)
            {
                return;
            }

            UpdateHud(SteamVR_Input_Sources.LeftHand);
            UpdateHud(SteamVR_Input_Sources.RightHand);
        }

        public void NotifyInstanceGrabbed(
            SteamVR_Input_Sources source,
            SampleInstanceView view)
        {
            _heldInstances[source] = view;
            _heldAssets.Remove(source);
        }

        public void NotifyInstanceReleased(
            SteamVR_Input_Sources source,
            SampleInstanceView view)
        {
            if (_heldInstances.TryGetValue(source, out SampleInstanceView held)
                && held == view)
            {
                _heldInstances.Remove(source);
            }
        }

        public void NotifyAssetGrabbed(
            SteamVR_Input_Sources source,
            SampleTokenView token)
        {
            _heldAssets[source] = token;
            _heldInstances.Remove(source);
            _workspace.NotifyTokenGrabbed(token);
        }

        public void NotifyAssetReleased(
            SteamVR_Input_Sources source,
            SampleTokenView token)
        {
            if (_heldAssets.TryGetValue(source, out SampleTokenView held)
                && held == token)
            {
                _heldAssets.Remove(source);
            }
        }

        public Transform GetHandTransform(SteamVR_Input_Sources source)
        {
            return _input.GetHandTransform(source);
        }

        public Hand GetHand(SteamVR_Input_Sources source)
        {
            return _input.GetHand(source);
        }

        public bool AnyHandCarryingPlacementToken()
        {
            if (_heldAssets.Count > 0 || _heldInstances.Count > 0)
            {
                return true;
            }

            SampleTokenView[] tokens = FindObjectsOfType<SampleTokenView>();
            for (int i = 0; i < tokens.Length; i++)
            {
                if (tokens[i].IsCarried())
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryCommitInstanceDrop(SampleInstanceView view)
        {
            return _workspace.TryCommitInstanceDrop(view);
        }

        private void HandleTriggerPressed(SteamVR_Input_Sources source)
        {
            if (TryGetTargetAsset(source, out SampleAssetData asset))
            {
                EffectChainData effects = GetTargetEffects(source);
                _loopController.Audition(asset, effects);
                return;
            }

            if (AnyHandCarryingPlacementToken())
            {
                return;
            }

            _capture.StartCapture(source, _effects.MicrophoneEffects);
        }

        private void HandleTriggerReleased(SteamVR_Input_Sources source)
        {
            if (_capture.IsRecording && _capture.RecordingSource == source)
            {
                _capture.StopCapture(source);
            }
        }

        private void HandleTrackpadTouched(
            SteamVR_Input_Sources source,
            Vector2 axis)
        {
            _effects.ApplyXY(source, GetTargetEffects(source), axis);
            if (!_heldInstances.ContainsKey(source)
                && !_heldAssets.ContainsKey(source))
            {
                _capture.UpdateMonitorEffects(_effects.MicrophoneEffects);
            }
            RefreshTargetVisual(source);
            _loopController.RefreshPlayback();
        }

        private void HandleTrackpadUpPressed(SteamVR_Input_Sources source)
        {
            TryEditHeldInstance(source, clone: true);
        }

        private void HandleTrackpadDownPressed(SteamVR_Input_Sources source)
        {
            TryEditHeldInstance(source, clone: false);
        }

        private void HandleTrackpadClicked(
            SteamVR_Input_Sources source,
            Vector2 axis)
        {
            if (axis.magnitude >= 0.25f
                && Mathf.Abs(axis.y) > Mathf.Abs(axis.x)
                && _heldInstances.TryGetValue(source, out SampleInstanceView heldOnClick)
                && heldOnClick != null)
            {
                TryEditHeldInstance(source, clone: axis.y > 0f);
                return;
            }

            EffectChainData target = GetTargetEffects(source);
            if (axis.magnitude < 0.35f)
            {
                _effects.ToggleBypass(source, target);
            }
            else if (Mathf.Abs(axis.x) >= Mathf.Abs(axis.y))
            {
                _effects.NavigatePair(source, axis.x >= 0f ? 1 : -1);
            }
            else
            {
                _effects.NavigatePreset(source, target, axis.y >= 0f ? 1 : -1);
            }

            if (!_heldInstances.ContainsKey(source)
                && !_heldAssets.ContainsKey(source))
            {
                _capture.UpdateMonitorEffects(_effects.MicrophoneEffects);
            }
            RefreshTargetVisual(source);
            _loopController.RefreshPlayback();
        }

        private void TryEditHeldInstance(SteamVR_Input_Sources source, bool clone)
        {
            if (!_heldInstances.TryGetValue(source, out SampleInstanceView heldInstance)
                || heldInstance == null)
            {
                return;
            }

            if (clone)
            {
                _loopController.CloneHeldInstance(heldInstance);
            }
            else
            {
                _loopController.DeleteHeldInstance(heldInstance, GetHand(source));
                _heldInstances.Remove(source);
            }

            UpdateHud(source);
            _loopController.RefreshPlayback();
        }

        private void HandleMenuTapped(SteamVR_Input_Sources source)
        {
            _input.ToggleHud(source);
        }

        private void HandleMenuHeld(SteamVR_Input_Sources source)
        {
            EffectPresetData preset = _effects.SaveCurrentPreset(
                source,
                GetTargetEffects(source));
            if (preset != null)
            {
                Debug.Log($"Saved effect preset: {preset.Name}", this);
            }
        }

        private void HandleSampleCaptured(
            SampleAssetData asset,
            SteamVR_Input_Sources source)
        {
            _workspace.RegisterAsset(asset, source);
        }

        private void HandleRecordingStateChanged(
            SteamVR_Input_Sources source,
            bool recording)
        {
            UpdateHud(source);
        }

        private EffectChainData GetTargetEffects(SteamVR_Input_Sources source)
        {
            if (_heldInstances.TryGetValue(source, out SampleInstanceView view)
                && view != null)
            {
                return view.Data.Effects;
            }

            if (_heldAssets.TryGetValue(source, out SampleTokenView token)
                && token != null)
            {
                return token.Asset.DefaultEffects;
            }

            return _effects.MicrophoneEffects;
        }

        private bool TryGetTargetAsset(
            SteamVR_Input_Sources source,
            out SampleAssetData asset)
        {
            if (_heldInstances.TryGetValue(source, out SampleInstanceView view)
                && view != null)
            {
                asset = _loopController.GetAsset(view.Data.SampleAssetId);
                return asset != null;
            }

            if (_heldAssets.TryGetValue(source, out SampleTokenView token)
                && token != null)
            {
                asset = token.Asset;
                return asset != null;
            }

            asset = null;
            return false;
        }

        private void UpdateHud(SteamVR_Input_Sources source)
        {
            string target = "MIC";
            if (_heldInstances.TryGetValue(source, out SampleInstanceView view)
                && view != null)
            {
                SampleAssetData asset = _loopController.GetAsset(
                    view.Data.SampleAssetId);
                target = asset != null
                    ? $"INSTANCE\n{asset.DisplayName}\nTP↑ CLONE ↓ DEL"
                    : "INSTANCE\nTP↑ CLONE ↓ DEL";
            }
            else if (_heldAssets.TryGetValue(source, out SampleTokenView token)
                && token != null)
            {
                target = $"ASSET\n{token.Asset.DisplayName}";
            }

            if (_capture.IsRecording && _capture.RecordingSource == source)
            {
                target = "RECORDING";
            }

            _input.SetHudText(
                source,
                $"{target}\n{_effects.GetStatus(source, GetTargetEffects(source))}");
        }

        private void RefreshTargetVisual(SteamVR_Input_Sources source)
        {
            if (_heldInstances.TryGetValue(source, out SampleInstanceView view)
                && view != null)
            {
                view.RefreshEffectVisual();
            }
        }
    }
}
