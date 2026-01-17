using UnityEngine;
using System;
using Valve.VR;
using Valve.VR.InteractionSystem;
using TMPro;
using System.Security.Cryptography;

public class Metronome : MonoBehaviour
{
    private bool _isRunning = false;
    private bool _changingTempo = false;
    private float _interval;
    private Interactable _interactable;
    private Coroutine _tickRoutine;

    public Button StartButton;
    public GameObject OnButton;
    public GameObject OffButton;
    public event Action OnTick;
    public bool StartOn = false;

    public float Tempo = 120.0f;
    [Tooltip("How many ticks per quarter note. 4 = 16th notes for a 16-step sequencer.")]
    public int TicksPerBeat = 4;

    public SteamVR_Action_Boolean RDPadUp;
    public SteamVR_Action_Boolean RDPadDown;
    public TMP_Text TempoDisplay;


    private void Start()
    {
        CalculateInterval();

        _interactable = GetComponent<Interactable>();
        
        StartButton.OnPress += ToggleMetronome;

        RDPadUp.onStateDown += ChangeTempoUp;
        RDPadDown.onStateDown += ChangeTempoDown;
        RDPadUp.onStateUp += StopChangingTempo;
        RDPadDown.onStateUp += StopChangingTempo;

        SetDisplay();
        if (StartOn) StartMetronome();
    }

    private void CalculateInterval()
    {
        // Base quarter-note interval: 60 seconds per minute divided by BPM
        // Divide further for subdivisions (ex: 4 => 16th notes)
        int ticksPerBeat = Mathf.Max(1, TicksPerBeat);
        _interval = (60.0f / Tempo) / ticksPerBeat;
    }
    public void ToggleMetronome()
    {
        Debug.Log("Metronome - Toggle");
        if (_isRunning) StopMetronome();
        else StartMetronome();
    }
    public void StartMetronome()
    {
        if (_isRunning || _tickRoutine != null) return;
        Debug.Log($"Metronome - Start at {Tempo} BPM");
        _isRunning = true;
        _tickRoutine = StartCoroutine(TickCoroutine());
        OnButton.SetActive(true);
        OffButton.SetActive(false);
    }
    public void StopMetronome()
    {
        if (!_isRunning) return;
        Debug.Log("Metronome - Stop");
        _isRunning = false;
        if (_tickRoutine != null)
        {
            StopCoroutine(_tickRoutine);
            _tickRoutine = null;
        }
        OnButton.SetActive(false);
        OffButton.SetActive(true);
    }
    private System.Collections.IEnumerator TickCoroutine()
    {
        while (_isRunning)
        {
            OnTick?.Invoke();
            yield return new WaitForSeconds(_interval);
        }
    }
    private void ChangeTempoUp(SteamVR_Action_Boolean action, SteamVR_Input_Sources source)
    {
        if (!_interactable.attachedToHand) return;
        IncreaseTempo();
        StartCoroutine(ContinuousTempoIncrease(1.0f));
    }

    private void ChangeTempoDown(SteamVR_Action_Boolean action, SteamVR_Input_Sources source)
    {
        if (!_interactable.attachedToHand) return;
        DecreaseTempo();
        StartCoroutine(ContinuousTempoDecrease(1.0f));
    }
    private void StopChangingTempo(SteamVR_Action_Boolean action, SteamVR_Input_Sources source)
    {
        StopAllCoroutines();
        _changingTempo = false;
    }
    private void IncreaseTempo()
    {
        Tempo++;
        CalculateInterval();
        SetDisplay();
    }
    private void DecreaseTempo()
    {
        Tempo--;
        CalculateInterval();
        SetDisplay();
    }
    private void SetDisplay()
    {
        TempoDisplay.text = Tempo.ToString();
    }
    private System.Collections.IEnumerator ContinuousTempoIncrease(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (_changingTempo)
        {
            IncreaseTempo();
            StartCoroutine(ContinuousTempoIncrease(0.1f));
        }
    }
    private System.Collections.IEnumerator ContinuousTempoDecrease(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (_changingTempo)
        {
            DecreaseTempo();
            StartCoroutine(ContinuousTempoDecrease(0.1f));
        }
    }
}

