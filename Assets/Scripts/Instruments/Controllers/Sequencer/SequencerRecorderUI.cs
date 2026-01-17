using UnityEngine;
using Valve.VR;

public class SequencerRecorderUI : MonoBehaviour
{
    [SerializeField] private SampleRecorder _sampleRecorder;
    [SerializeField] private Button[] _channelButtons;
    [SerializeField] private Button[] _beatButtons;
    [SerializeField] private Button _recordButton;
    [SerializeField] private Button _clearButton;
    
    private int _selectedChannel = -1;
    private int _selectedBeat = -1;

    private void Start()
    {
        if (_sampleRecorder == null)
        {
            _sampleRecorder = FindObjectOfType<SampleRecorder>();
        }

        // Setup channel buttons
        for (int i = 0; i < _channelButtons.Length; i++)
        {
            int channel = i; // Capture for lambda
            _channelButtons[i].OnPress += () => SelectChannel(channel);
        }

        // Setup beat buttons
        for (int i = 0; i < _beatButtons.Length; i++)
        {
            int beat = i; // Capture for lambda
            _beatButtons[i].OnPress += () => SelectBeat(beat);
        }

        // Setup record button
        _recordButton.OnPress += () => {
            if (_selectedChannel != -1 && _selectedBeat != -1)
            {
                _sampleRecorder.StartRecording();
            }
        };

        // Setup clear button
        _clearButton.OnPress += () => {
            if (_selectedChannel != -1 && _selectedBeat != -1)
            {
                _sampleRecorder.ClearSample(_selectedChannel, _selectedBeat);
            }
        };
    }

    private void SelectChannel(int channel)
    {
        _selectedChannel = channel;
        _sampleRecorder.SelectChannel(channel);
        
        // Visual feedback
        for (int i = 0; i < _channelButtons.Length; i++)
        {
            _channelButtons[i].GetComponent<Renderer>().material.color = 
                (i == channel) ? Color.green : Color.white;
        }
    }

    private void SelectBeat(int beat)
    {
        _selectedBeat = beat;
        _sampleRecorder.SelectBeat(beat);
        
        // Visual feedback
        for (int i = 0; i < _beatButtons.Length; i++)
        {
            _beatButtons[i].GetComponent<Renderer>().material.color = 
                (i == beat) ? Color.yellow : Color.white;
        }
    }
} 