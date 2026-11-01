using System;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

namespace MusicPractice.SpatialLoop
{
    public sealed class SpatialHandInputRouter : MonoBehaviour
    {
        private sealed class HandState
        {
            public SteamVR_Input_Sources Source;
            public Hand Hand;
            public SteamVR_Action_Single Trigger;
            public SteamVR_Action_Vector2 Trackpad;
            public SteamVR_Action_Boolean TrackpadTouch;
            public SteamVR_Action_Boolean TrackpadClick;
            public SteamVR_Action_Boolean PadUp;
            public SteamVR_Action_Boolean PadDown;
            public SteamVR_Action_Boolean Menu;
            public TextMesh Hud;
            public bool TriggerWasDown;
            public bool MenuWasDown;
            public float MenuDownTime;
            public bool TouchWasDown;
            public bool TouchIsEffectGesture;
            public float TouchStartTime;
            public Vector2 TouchStartAxis;
        }

        [SerializeField, Range(0.1f, 1f)] private float _triggerThreshold = 0.7f;
        [SerializeField, Min(0.2f)] private float _menuHoldSeconds = 0.65f;
        [SerializeField] private bool _showDiagnostics = true;

        private readonly Dictionary<SteamVR_Input_Sources, HandState> _hands =
            new Dictionary<SteamVR_Input_Sources, HandState>();

        public event Action<SteamVR_Input_Sources> TriggerPressed;
        public event Action<SteamVR_Input_Sources> TriggerReleased;
        public event Action<SteamVR_Input_Sources, Vector2> TrackpadTouched;
        public event Action<SteamVR_Input_Sources, Vector2> TrackpadClicked;
        public event Action<SteamVR_Input_Sources> TrackpadUpPressed;
        public event Action<SteamVR_Input_Sources> TrackpadDownPressed;
        public event Action<SteamVR_Input_Sources> MenuTapped;
        public event Action<SteamVR_Input_Sources> MenuHeld;

        private void Start()
        {
            RegisterHand(
                SteamVR_Input_Sources.LeftHand,
                "TriggerLeft",
                "TrackpadLeft",
                "TrackpadTouchLeft",
                "TrackpadClickLeft",
                "LDpadUp",
                "LDPadDn",
                "MenuLeft");
            RegisterHand(
                SteamVR_Input_Sources.RightHand,
                "TriggerRight",
                "TrackpadRight",
                "TrackpadTouchRight",
                "TrackpadClickRight",
                "RDPadUp",
                "RDPadDn",
                "MenuRight");
        }

        private void Update()
        {
            foreach (HandState state in _hands.Values)
            {
                PollHand(state);
            }
        }

        public void SetHudText(SteamVR_Input_Sources source, string text)
        {
            if (_hands.TryGetValue(source, out HandState state)
                && state.Hud != null)
            {
                state.Hud.text = text;
            }
        }

        public void ToggleHud(SteamVR_Input_Sources source)
        {
            if (_hands.TryGetValue(source, out HandState state)
                && state.Hud != null)
            {
                state.Hud.gameObject.SetActive(!state.Hud.gameObject.activeSelf);
            }
        }

        public Transform GetHandTransform(SteamVR_Input_Sources source)
        {
            return GetHand(source)?.transform;
        }

        public Hand GetHand(SteamVR_Input_Sources source)
        {
            return _hands.TryGetValue(source, out HandState state)
                ? state.Hand
                : null;
        }

        private void RegisterHand(
            SteamVR_Input_Sources source,
            string triggerName,
            string trackpadName,
            string touchName,
            string clickName,
            string padUpName,
            string padDownName,
            string menuName)
        {
            Hand hand = FindHand(source);
            var state = new HandState
            {
                Source = source,
                Hand = hand,
                Trigger = SteamVR_Input.GetAction<SteamVR_Action_Single>(triggerName),
                Trackpad = SteamVR_Input.GetAction<SteamVR_Action_Vector2>(trackpadName),
                TrackpadTouch = SteamVR_Input.GetAction<SteamVR_Action_Boolean>(touchName),
                TrackpadClick = SteamVR_Input.GetAction<SteamVR_Action_Boolean>(clickName),
                PadUp = SteamVR_Input.GetAction<SteamVR_Action_Boolean>(padUpName),
                PadDown = SteamVR_Input.GetAction<SteamVR_Action_Boolean>(padDownName),
                Menu = SteamVR_Input.GetAction<SteamVR_Action_Boolean>(menuName)
            };

            LogMissingAction(triggerName, state.Trigger);
            LogMissingAction(trackpadName, state.Trackpad);
            LogMissingAction(touchName, state.TrackpadTouch);
            LogMissingAction(clickName, state.TrackpadClick);
            LogMissingAction(padUpName, state.PadUp);
            LogMissingAction(padDownName, state.PadDown);
            LogMissingAction(menuName, state.Menu);

            if (hand != null)
            {
                state.Hud = CreateHud(hand.transform, source);
            }

            _hands[source] = state;
        }

        private void PollHand(HandState state)
        {
            float triggerValue = state.Trigger != null
                ? state.Trigger.GetAxis(state.Source)
                : 0f;
            bool triggerDown = triggerValue >= _triggerThreshold;
            if (triggerDown && !state.TriggerWasDown)
            {
                TriggerPressed?.Invoke(state.Source);
            }
            else if (!triggerDown && state.TriggerWasDown)
            {
                TriggerReleased?.Invoke(state.Source);
            }

            state.TriggerWasDown = triggerDown;

            bool touching = state.TrackpadTouch != null
                && state.TrackpadTouch.GetState(state.Source);
            Vector2 axis = state.Trackpad != null
                ? state.Trackpad.GetAxis(state.Source)
                : Vector2.zero;
            bool clickDown = state.TrackpadClick != null
                && state.TrackpadClick.GetStateDown(state.Source);

            if (touching && !state.TouchWasDown)
            {
                state.TouchStartTime = Time.unscaledTime;
                state.TouchStartAxis = axis;
                state.TouchIsEffectGesture = false;
            }

            if (touching)
            {
                if (!state.TouchIsEffectGesture
                    && (Vector2.Distance(axis, state.TouchStartAxis) > 0.08f
                        || Time.unscaledTime - state.TouchStartTime > 0.18f))
                {
                    state.TouchIsEffectGesture = true;
                }

                if (state.TouchIsEffectGesture && !clickDown)
                {
                    TrackpadTouched?.Invoke(state.Source, axis);
                }
            }

            if (clickDown)
            {
                TrackpadClicked?.Invoke(state.Source, axis);
            }

            if (state.PadUp != null && state.PadUp.GetStateDown(state.Source))
            {
                TrackpadUpPressed?.Invoke(state.Source);
            }

            if (state.PadDown != null && state.PadDown.GetStateDown(state.Source))
            {
                TrackpadDownPressed?.Invoke(state.Source);
            }

            if (!touching)
            {
                state.TouchIsEffectGesture = false;
            }

            state.TouchWasDown = touching;

            bool menuDown = state.Menu != null && state.Menu.GetState(state.Source);
            if (menuDown && !state.MenuWasDown)
            {
                state.MenuDownTime = Time.unscaledTime;
            }
            else if (!menuDown && state.MenuWasDown)
            {
                if (Time.unscaledTime - state.MenuDownTime >= _menuHoldSeconds)
                {
                    MenuHeld?.Invoke(state.Source);
                }
                else
                {
                    MenuTapped?.Invoke(state.Source);
                }
            }

            state.MenuWasDown = menuDown;
        }

        private Hand FindHand(SteamVR_Input_Sources source)
        {
            Hand[] hands = FindObjectsOfType<Hand>();
            for (int i = 0; i < hands.Length; i++)
            {
                if (hands[i].handType == source)
                {
                    return hands[i];
                }
            }

            return null;
        }

        private static void LogMissingAction(string actionName, object action)
        {
            if (action != null)
            {
                return;
            }

            Debug.LogWarning(
                "SpatialHandInputRouter: SteamVR action '"
                + actionName
                + "' was not found. Open Window > SteamVR Input, load the action "
                + "manifest from Assets/StreamingAssets/SteamVR/actions.json, then "
                + "choose Save and Generate.");
        }

        private TextMesh CreateHud(
            Transform hand,
            SteamVR_Input_Sources source)
        {
            var hudObject = new GameObject($"{source} Spatial HUD");
            hudObject.transform.SetParent(hand, false);
            hudObject.transform.localPosition = new Vector3(0f, 0.08f, 0.14f);
            hudObject.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
            hudObject.transform.localScale = Vector3.one * 0.014f;

            TextMesh text = hudObject.AddComponent<TextMesh>();
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 72;
            text.characterSize = 0.16f;
            text.color = Color.white;
            text.text = source == SteamVR_Input_Sources.LeftHand
                ? "LEFT\nMIC"
                : "RIGHT\nMIC";
            hudObject.SetActive(_showDiagnostics);
            return text;
        }
    }
}
