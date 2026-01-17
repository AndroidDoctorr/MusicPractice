using UnityEngine;

public class Button : MonoBehaviour
{
    public delegate void StartDelegate();
    public event StartDelegate OnPress;
    public event StartDelegate OnRelease;

    [Header("Visual States (optional)")]
    [Tooltip("Shown when the button is not pressed/recording")]
    public GameObject OffState;
    [Tooltip("Shown when the button is pressed/recording")]
    public GameObject OnState;
    [Tooltip("Set true if the button should start lit/pressed")]
    public bool StartOn;

    private bool _isPressed;

    private void Awake()
    {
        ApplyVisualState(StartOn);
        _isPressed = StartOn;
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandlePress();
    }

    private void OnCollisionExit(Collision collision)
    {
        HandleRelease();
    }

    private void OnTriggerEnter(Collider other)
    {
        HandlePress();
    }

    private void OnTriggerExit(Collider other)
    {
        HandleRelease();
    }

    private void HandlePress()
    {
        if (_isPressed) return;
        _isPressed = true;
        ApplyVisualState(true);
        OnPress?.Invoke();
    }

    private void HandleRelease()
    {
        if (!_isPressed) return;
        _isPressed = false;
        ApplyVisualState(false);
        OnRelease?.Invoke();
    }

    private void ApplyVisualState(bool on)
    {
        if (OnState != null) OnState.SetActive(on);
        if (OffState != null) OffState.SetActive(!on);
    }
}