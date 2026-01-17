using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class XYPadController : MonoBehaviour
{
    [SerializeField] private Transform _padSurface;
    [SerializeField] private Transform _cursor;
    [SerializeField] private float _padSize = 0.1f; // Size of the pad in world units
    
    private Vector2 _currentPosition;
    private bool _isActive = false;
    private Interactable _interactable;
    
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
            
            // Update channel properties
            UpdateChannelProperties();
        }
    }
    
    public void LinkToChannel(SequencerChannel channel)
    {
        _currentChannel = channel;
        _isActive = true;
        
        // Set initial cursor position based on current channel values
        _currentPosition = new Vector2(
            Mathf.InverseLerp(pitchRange.min, pitchRange.max, channel.pitch),
            Mathf.InverseLerp(volumeRange.min, volumeRange.max, channel.volume)
        );
        
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
        _currentChannel = null;
        _isActive = false;
        Debug.Log("XY Pad unlinked from channel");
    }
    
    public bool IsLinkedToChannel(SequencerChannel channel)
    {
        return _currentChannel == channel;
    }
    
    private void UpdateChannelProperties()
    {
        if (_currentChannel == null) return;
        
        // X controls pitch, Y controls volume
        float newPitch = Mathf.Lerp(pitchRange.min, pitchRange.max, _currentPosition.x);
        float newVolume = Mathf.Lerp(volumeRange.min, volumeRange.max, _currentPosition.y);
        
        _currentChannel.SetPitch(newPitch);
        _currentChannel.SetVolume(newVolume);
    }
} 