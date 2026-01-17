using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SequencerBeat : MonoBehaviour
{
    public GameObject Lit;
    public GameObject Unlit;
    public GameObject Playing;

    public float Velocity = 0.75f;
    public Note Note = Note.A;
    public int Octave = 4;
    public bool IsAccented = false;

    private SampleRecorder _sampleRecorder;
    private SequencerChannel _channel;
    private bool _isActive;
    private int _beatIndex;

    private void Awake()
    {
        _channel = GetComponentInParent<SequencerChannel>();
        _beatIndex = transform.GetSiblingIndex();
        _sampleRecorder = FindObjectOfType<SampleRecorder>();
        SetActive(false);
        ShowPlaying(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        ToggleActive();
    }

    private void OnCollisionEnter(Collision collision)
    {
        ToggleActive();
    }

    public void SelectBeat()
    {
        if (_sampleRecorder == null) return;

        int channelIndex = transform.parent.GetSiblingIndex();
        _sampleRecorder.SelectChannel(channelIndex);
        _sampleRecorder.SelectBeat(_beatIndex);
    }

    public void DeselectBeat()
    {
        // Restore current active state visuals
        SetActive(_isActive);
    }

    public void Play(SoundModule soundModule)
    {
        if (Velocity == 0) return;

        SetActive(true);
        soundModule.PlaySound(Note, Octave, Velocity);
    }

    public void TurnOff()
    {
        ShowPlaying(false);
    }

    public void ShowPlaying(bool playing)
    {
        if (Playing != null)
        {
            Playing.SetActive(playing);
        }
    }

    public void SetActive(bool active)
    {
        _isActive = active;
        if (Lit != null) Lit.SetActive(active);
        if (Unlit != null) Unlit.SetActive(!active);
    }

    private void ToggleActive()
    {
        _channel?.ToggleBeat(_beatIndex);
        // Update local state to match channel-driven value
        bool newState = _channel != null && _channel.IsBeatActive(_beatIndex);
        SetActive(newState);
    }
}
