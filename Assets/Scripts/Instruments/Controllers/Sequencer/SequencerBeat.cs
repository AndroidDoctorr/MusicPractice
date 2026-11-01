using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SequencerBeat : MonoBehaviour
{
    public GameObject Lit;
    public GameObject Unlit;
    public GameObject Playing;
    public Button Button;

    public float Velocity = 0.75f;
    public Note Note = Note.A;
    public int Octave = 4;
    public bool IsAccented = false;

    private SequencerChannel _channel;
    private bool _isActive;
    private int _beatIndex;

    private void Awake()
    {
        _channel = GetComponentInParent<SequencerChannel>();
        _beatIndex = transform.GetSiblingIndex();
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
        _channel?.SelectStep(_beatIndex);
        // Update local state to match channel-driven value
        bool newState = _channel != null && _channel.IsBeatActive(_beatIndex);
        SetActive(newState);
    }
}
