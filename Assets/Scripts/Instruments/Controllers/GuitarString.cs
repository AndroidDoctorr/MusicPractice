using UnityEngine;

public class GuitarString : MonoBehaviour
{
    private AudioSource _audioSource;
    private SoundModule _soundModule;

    public delegate void GuitarStringDelegate(
        int baseOctave, Note baseKey, float dist, float velocity, SoundModule soundModule
    );
    public event GuitarStringDelegate OnStrum;

    public int BaseOctave;
    public Note BaseKey;
    public Transform FretBase;


    private void Awake()
    {
        _soundModule = GetComponent<SoundModule>();
        _audioSource = GetComponent<AudioSource>();
        _soundModule.SetAudioSource(_audioSource);
    }
    private void OnCollisionEnter(Collision collision)
    {
        float velocity = collision.relativeVelocity.magnitude;
        float dist = Vector3.Distance(collision.transform.position, FretBase.position);
        Debug.Log($"STRING HIT: {BaseKey}{BaseOctave}, {dist}, {velocity}");
        OnStrum?.Invoke(BaseOctave, BaseKey, dist, velocity, _soundModule);
    }
}