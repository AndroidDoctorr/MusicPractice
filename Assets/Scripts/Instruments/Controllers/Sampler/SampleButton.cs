using TMPro;
using UnityEngine;

public class SampleButton : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private TMP_Text _label;
    [SerializeField] private GameObject _selectedState;
    [SerializeField] private GameObject _unselectedState;
    [SerializeField] private Renderer _renderer;
    [SerializeField] private Color _emptyColor = Color.white;
    [SerializeField] private Color _occupiedColor = new Color(0.7f, 0.9f, 1f);
    [SerializeField] private Color _selectedColor = Color.green;

    private SamplePad _samplePad;
    private int _slotIndex;

    public void Initialize(SamplePad samplePad, int slotIndex)
    {
        _samplePad = samplePad;
        _slotIndex = slotIndex;

        if (_button == null) _button = GetComponent<Button>();
        if (_renderer == null) _renderer = GetComponent<Renderer>();
        if (_button != null)
        {
            _button.OnPress -= HandlePressed;
            _button.OnPress += HandlePressed;
        }
    }

    public void Refresh(bool selected, bool occupied, string displayName)
    {
        if (_label != null)
        {
            _label.text = displayName;
        }

        if (_selectedState != null) _selectedState.SetActive(selected);
        if (_unselectedState != null) _unselectedState.SetActive(!selected);

        if (_renderer != null)
        {
            _renderer.material.color = selected
                ? _selectedColor
                : (occupied ? _occupiedColor : _emptyColor);
        }
    }

    private void HandlePressed()
    {
        _samplePad?.ToggleSelection(_slotIndex);
    }
}
