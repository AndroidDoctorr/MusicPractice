using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using System.Linq;

public class XYPadController : MonoBehaviour
{
    [SerializeField] private Transform _padSurface;
    [SerializeField] private Transform _cursor;
    [SerializeField] private float _padSize = 0.1f; // Size of the pad in world units
    [Header("Optional Buttons")]
    [SerializeField] private Button _modeButton;
    [SerializeField] private Button _stepMaskButton;
    [SerializeField] private Button _prevChannelButton;
    [SerializeField] private Button _nextChannelButton;
    
    private Vector2 _currentPosition;
    private bool _isActive = false;
    private Interactable _interactable;

    private enum EditMode { Channel, StepVelocity }
    [SerializeField] private EditMode _editMode = EditMode.Channel;

    [SerializeField] private SequencerChannel.StepMask _stepMask = SequencerChannel.StepMask.SelectedStep;

    private SequencerChannel[] _channels;
    
    // Property ranges
    [System.Serializable]
    public class PropertyRange
    {
        public float min = 0f;
        public float max = 1f;
    }
    
    [Header("Property Ranges")]
    public PropertyRange volumeRange = new PropertyRange { min = 0f, max = 1f };
    public PropertyRange pitchRange = new PropertyRange { min = 0.5f, max = 2f };
    
    // Current target
    private SequencerChannel _currentChannel;
    
    private void Start()
    {
        _interactable = GetComponent<Interactable>();
        if (_interactable == null)
        {
            Debug.LogError("XYPadController requires an Interactable component!");
        }

        _channels = FindObjectsOfType<SequencerChannel>()
            .OrderBy(c => c.transform.GetSiblingIndex())
            .ToArray();

        if (_modeButton != null) _modeButton.OnPress += ToggleMode;
        if (_stepMaskButton != null) _stepMaskButton.OnPress += CycleStepMask;
        if (_prevChannelButton != null) _prevChannelButton.OnPress += () => CycleChannel(-1);
        if (_nextChannelButton != null) _nextChannelButton.OnPress += () => CycleChannel(1);
    }
    
    private void Update()
    {
        if (!_isActive || _currentChannel == null) return;
        
        // Update cursor position based on hand position
        if (_interactable.attachedToHand != null)
        {
            Vector3 localHandPos = _padSurface.InverseTransformPoint(_interactable.attachedToHand.transform.position);
            _currentPosition = new Vector2(
                Mathf.Clamp(localHandPos.x / _padSize, -0.5f, 0.5f) + 0.5f,
                Mathf.Clamp(localHandPos.y / _padSize, -0.5f, 0.5f) + 0.5f
            );
            
            // Update cursor position
            _cursor.localPosition = new Vector3(
                (_currentPosition.x - 0.5f) * _padSize,
                (_currentPosition.y - 0.5f) * _padSize,
                _cursor.localPosition.z
            );
            
            // Update properties
            ApplyEdits();
        }
    }
    
    public void LinkToChannel(SequencerChannel channel)
    {
        if (_currentChannel != null && _currentChannel != channel)
        {
            _currentChannel.SetXYPadLinked(false);
        }

        _currentChannel = channel;
        _isActive = true;
        _currentChannel.SetXYPadLinked(true);
        
        SyncCursorToTarget();
        
        // Update cursor position
        _cursor.localPosition = new Vector3(
            (_currentPosition.x - 0.5f) * _padSize,
            (_currentPosition.y - 0.5f) * _padSize,
            _cursor.localPosition.z
        );
        
        Debug.Log($"XY Pad linked to channel: {channel.channelName}");
    }
    
    public void UnlinkChannel()
    {
        if (_currentChannel != null) _currentChannel.SetXYPadLinked(false);
        _currentChannel = null;
        _isActive = false;
        Debug.Log("XY Pad unlinked from channel");
    }
    
    public bool IsLinkedToChannel(SequencerChannel channel)
    {
        return _currentChannel == channel;
    }
    
    private void ApplyEdits()
    {
        if (_currentChannel == null) return;
        
        if (_editMode == EditMode.Channel)
        {
            // X controls pitch, Y controls volume
            float newPitch = Mathf.Lerp(pitchRange.min, pitchRange.max, _currentPosition.x);
            float newVolume = Mathf.Lerp(volumeRange.min, volumeRange.max, _currentPosition.y);

            _currentChannel.SetPitch(newPitch);
            _currentChannel.SetVolume(newVolume);
        }
        else
        {
            // StepVelocity mode: Y controls velocity for a mask of steps
            float newVelocity = Mathf.Clamp01(_currentPosition.y);
            _currentChannel.ApplyVelocityToMask(_stepMask, newVelocity);
        }
    }

    private void ToggleMode()
    {
        _editMode = _editMode == EditMode.Channel ? EditMode.StepVelocity : EditMode.Channel;
        SyncCursorToTarget();
    }

    private void CycleStepMask()
    {
        int next = ((int)_stepMask + 1) % System.Enum.GetValues(typeof(SequencerChannel.StepMask)).Length;
        _stepMask = (SequencerChannel.StepMask)next;
    }

    private void CycleChannel(int dir)
    {
        if (_channels == null || _channels.Length == 0) return;

        int idx = 0;
        if (_currentChannel != null)
        {
            idx = System.Array.IndexOf(_channels, _currentChannel);
            if (idx < 0) idx = 0;
        }

        idx = (idx + dir) % _channels.Length;
        if (idx < 0) idx += _channels.Length;
        LinkToChannel(_channels[idx]);
    }

    private void SyncCursorToTarget()
    {
        if (_currentChannel == null) return;

        if (_editMode == EditMode.Channel)
        {
            _currentPosition = new Vector2(
                Mathf.InverseLerp(pitchRange.min, pitchRange.max, _currentChannel.pitch),
                Mathf.InverseLerp(volumeRange.min, volumeRange.max, _currentChannel.volume)
            );
        }
        else
        {
            // Keep X as-is, set Y to selected-step velocity
            int step = _currentChannel.GetSelectedStepIndex();
            float v = _currentChannel.GetStepVelocity(step);
            _currentPosition = new Vector2(_currentPosition.x, Mathf.Clamp01(v));
        }
    }
} 