using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class PercussiveInstrument : Instrument
{
    private string[] _folderPaths;
    private List<List<AudioClip>> _samples = new();
    private int _currentFolderIndex = 0;
    private int _currentSampleIndex = 0;
    private AudioClip _currentSample;

    public string SampleFolderPath = "DrumSamples";

    public TMP_Text SampleNameText;
    public TMP_Text FolderNameText;

    public SteamVR_Action_Boolean RDPadUp;
    public SteamVR_Action_Boolean RDPadDn;
    public SteamVR_Action_Boolean RDPadLf;
    public SteamVR_Action_Boolean RDPadRt;


    protected override void Awake()
    {
        base.Awake();
        DiscoverFolderPaths();
        LoadSamples();
        UpdateDisplay();
        SetSample();

        RDPadUp.onStateDown += NextFolder;
        RDPadDn.onStateDown += PrevFolder;
        RDPadLf.onStateDown += NextSample;
        RDPadRt.onStateDown += PrevSample;
    }
    protected virtual void OnCollisionEnter(Collision collision)
    {
        float strikeIntensity = Mathf.Clamp01(collision.relativeVelocity.magnitude);
        float volume = Mathf.Lerp(0, 1, strikeIntensity);
        _audioSource.volume = volume;
        PlaySample();
    }

    protected void DiscoverFolderPaths()
    {
        string samplePath = $"{Application.dataPath}/Resources/{SampleFolderPath}/";
        Debug.Log($"Percussion - Discover Paths: {samplePath}");
        _folderPaths = Directory.GetDirectories(samplePath);
    }
    protected void LoadSamples()
    {
        Debug.Log("Percussion - Load Samples");
        Debug.Log($"Folder paths: {_folderPaths.Length}");
        _samples = new List<List<AudioClip>>();

        for (int i = 0; i < _folderPaths.Length; i++)
        {
            string samplePath = _folderPaths[i].Split("/Resources/")[1] + "/";
            Debug.Log($"Sample path: {samplePath}");
            AudioClip[] folderSamples = Resources.LoadAll<AudioClip>(samplePath);
            _samples.Add(new List<AudioClip>(folderSamples));
        }
    }
    protected void SwitchFolder(int direction)
    {
        Debug.Log($"Percussion - Switch folder {direction}");
        _currentFolderIndex += direction;
        if (_currentFolderIndex < 0)
            _currentFolderIndex = _samples.Count - 1;
        if (_currentFolderIndex >= _samples.Count)
            _currentFolderIndex = 0;
        _currentSampleIndex = 0;
        UpdateDisplay();
        SetSample();
    }
    protected void SwitchSample(int direction)
    {
        _currentSampleIndex += direction;
        if (_currentSampleIndex < 0)
            _currentSampleIndex = _samples[_currentFolderIndex].Count - 1;
        if (_currentSampleIndex >= _samples[_currentFolderIndex].Count)
            _currentSampleIndex = 0;
        UpdateDisplay();
        SetSample();
    }
    protected void UpdateDisplay()
    {
        Debug.Log("Percussion - Update Display");
        Debug.Log($"Sample folders: {_samples.Count}");
        Debug.Log($"Samples: {_samples[_currentFolderIndex].Count}");
        SampleNameText.text = _samples[_currentFolderIndex][_currentSampleIndex].name;
        FolderNameText.text = _folderPaths[_currentFolderIndex].Split($"{SampleFolderPath}/")[1];
    }

    protected void NextFolder(SteamVR_Action_Boolean action, SteamVR_Input_Sources source)
    {
        if (!_interactable.attachedToHand) return;
        SwitchFolder(1);
        PlaySample();
    }
    protected void PrevFolder(SteamVR_Action_Boolean action, SteamVR_Input_Sources source)
    {
        if (!_interactable.attachedToHand) return;
        SwitchFolder(-1);
        PlaySample();
    }
    protected void NextSample(SteamVR_Action_Boolean action, SteamVR_Input_Sources source)
    {
        if (!_interactable.attachedToHand) return;
        SwitchSample(1);
        PlaySample();
    }
    protected void PrevSample(SteamVR_Action_Boolean action, SteamVR_Input_Sources source)
    {
        if (!_interactable.attachedToHand) return;
        SwitchSample(-1);
        PlaySample();
    }
    protected void SetSample()
    {
        Debug.Log("Percussion - Set Sample");
        _currentSample = _samples[_currentFolderIndex][_currentSampleIndex];
    }
    protected void PlaySample()
    {
        if (_currentSample == null)
        {
            Debug.LogError("Audio Sample not found for Drum");
            return;
        };
        if (_audioSource == null)
        {
            Debug.LogError("Audio Source not found for Drum");
            return;
        };
        _audioSource.PlayOneShot(_currentSample);
    }
}