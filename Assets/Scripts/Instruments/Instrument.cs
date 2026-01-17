using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class Instrument : MonoBehaviour
{
    protected Interactable _interactable;
    protected Rigidbody _rigidbody;

    protected float octaveRange = 12.0f;
    protected float baseFrequency = 261.63f;

    protected float _frequency = 440;
    protected float _pitch = 0.0f;
    protected float _color = 0.0f;
    protected float _phase = 0.0f;
    protected float _volume = 0.0f;
    protected float _amplitude = 0.5f;

    protected float _squareness = 0.0f;
    protected float _triangleness = 0.0f;
    protected float _sawness = 0.0f;
    protected float _whiteness = 0.0f;

    public AudioSource _audioSource;
    protected AudioLowPassFilter _lowPassFilter;
    protected AudioHighPassFilter _highPassFilter;
    protected WaveformType _currentWaveform = WaveformType.Sine;

    public Transform LeftHand;
    public Transform RightHand;
    public SteamVR_Action_Boolean MenuLeft;
    public SteamVR_Action_Boolean MenuRight;
    public SteamVR_Action_Single TriggerLeft;
    public SteamVR_Action_Single TriggerRight;
    public SteamVR_Action_Vector2 TrackpadLeft;
    public SteamVR_Action_Vector2 TrackpadRight;

    public float SampleRate = 22050;

    protected virtual void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _audioSource = GetComponent<AudioSource>();
        _highPassFilter = gameObject.AddComponent<AudioHighPassFilter>();
        _lowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();
        _interactable = GetComponent<Interactable>();

        var hands = FindObjectsOfType<Hand>();
        LeftHand = hands.FirstOrDefault(h => h.name == "LeftHand").transform;
        RightHand = hands.FirstOrDefault(h => h.name == "RightHand").transform;
    }
}
