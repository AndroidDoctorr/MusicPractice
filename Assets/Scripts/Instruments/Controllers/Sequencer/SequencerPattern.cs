using UnityEngine;
using System.Collections.Generic;

public enum PatternType
{
    DrumNBass,
    House,
    HipHop,
    Techno,
    Custom,
    Blank
}

[System.Serializable]
public class BeatPattern
{
    public string name;
    public PatternType type;
    public float swing = 0.0f; // 0.0 to 1.0, where 0.5 is maximum swing
    public List<bool[]> channelPatterns = new List<bool[]>();
    public List<float[]> channelVelocities = new List<float[]>();

    public BeatPattern(string name, PatternType type, float swing, List<bool[]> patterns, List<float[]> velocities = null)
    {
        this.name = name;
        this.type = type;
        this.swing = Mathf.Clamp(swing, 0f, 1f);
        this.channelPatterns = patterns;
        this.channelVelocities = velocities ?? new List<float[]>();
    }
}

public static class SequencerPatterns
{
    private static float[] VelFromBool(bool[] pattern, float onVelocity)
    {
        float[] v = new float[pattern.Length];
        for (int i = 0; i < pattern.Length; i++)
        {
            v[i] = pattern[i] ? Mathf.Clamp01(onVelocity) : 0f;
        }
        return v;
    }

    // Default patterns for 5 channels (Kick, Open HiHat, Clap, Closed HiHat, Snare)
    public static readonly BeatPattern DrumNBass = new BeatPattern(
        "Drum & Bass",
        PatternType.DrumNBass,
        0.3f,
        new List<bool[]>
        {
            // More breakbeat-ish DnB (16th grid)
            // Kick (Channel 1): 1, 7, 9, 11 (plus a small pickup on 16)
            new bool[] { true, false, false, false, false, false, true, false, true, false, true, false, false, false, false, true },
            // Open HiHat (Channel 2): occasional lifts
            new bool[] { false, false, false, false, false, false, false, true, false, false, false, true, false, false, false, true },
            // Clap (Channel 3): layered on snare hits
            new bool[] { false, false, false, false, true, false, false, false, false, false, false, false, true, false, false, false },
            // Closed HiHat (Channel 4): driving 16ths with some gaps
            new bool[] { true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true },
            // Snare (Channel 5): 2 and 4 backbeat (5, 13)
            new bool[] { false, false, false, false, true, false, false, false, false, false, false, false, true, false, false, false }
        },
        new List<float[]>
        {
            // Kick velocities (strong on 1/9, lighter on syncopations)
            new float[] { 1.0f, 0f, 0f, 0f, 0f, 0f, 0.7f, 0f, 0.95f, 0f, 0.7f, 0f, 0f, 0f, 0f, 0.5f },
            // Open hat velocities
            new float[] { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.55f, 0f, 0f, 0f, 0.5f, 0f, 0f, 0f, 0.55f },
            // Clap velocities
            new float[] { 0f, 0f, 0f, 0f, 0.75f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.75f, 0f, 0f, 0f },
            // Closed hat velocities (slight accents)
            new float[] { 0.35f, 0.25f, 0.32f, 0.25f, 0.35f, 0.25f, 0.32f, 0.25f, 0.35f, 0.25f, 0.32f, 0.25f, 0.35f, 0.25f, 0.32f, 0.25f },
            // Snare velocities
            new float[] { 0f, 0f, 0f, 0f, 0.9f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.9f, 0f, 0f, 0f },
        }
    );

    public static readonly BeatPattern House = new BeatPattern(
        "House",
        PatternType.House,
        0.2f,
        new List<bool[]>
        {
            // Kick (Channel 1)
            new bool[] { true, false, false, false, true, false, false, false, true, false, false, false, true, false, false, false },
            // Open HiHat (Channel 2)
            new bool[] { false, false, true, false, false, false, true, false, false, false, true, false, false, false, true, false },
            // Clap (Channel 3)
            new bool[] { false, false, false, false, true, false, false, false, false, false, false, false, true, false, false, false },
            // Closed HiHat (Channel 4)
            new bool[] { true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true },
            // Snare (Channel 5)
            new bool[] { false, false, false, false, true, false, false, false, false, false, false, false, true, false, false, false }
        }
    );

    public static readonly BeatPattern Blank = new BeatPattern(
        "Blank",
        PatternType.Blank,
        0f,
        new List<bool[]>
        {
            // Kick (Channel 1)
            new bool[] { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false },
            // Open HiHat (Channel 2)
            new bool[] { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false },
            // Clap (Channel 3)
            new bool[] { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false },
            // Closed HiHat (Channel 4)
            new bool[] { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false },
            // Snare (Channel 5)
            new bool[] { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false }
        }
    );

    public static readonly BeatPattern HipHop = new BeatPattern(
        "Hip Hop",
        PatternType.HipHop,
        0.4f,
        new List<bool[]>
        {
            // Kick (Channel 1)
            new bool[] { true, false, false, false, false, false, true, false, true, false, false, false, false, false, true, false },
            // Open HiHat (Channel 2)
            new bool[] { false, false, true, false, false, false, true, false, false, false, true, false, false, false, true, false },
            // Clap (Channel 3)
            new bool[] { false, false, false, false, true, false, false, false, false, false, false, false, true, false, false, false },
            // Closed HiHat (Channel 4)
            new bool[] { true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false },
            // Snare (Channel 5)
            new bool[] { false, false, false, false, true, false, false, false, false, false, false, false, true, false, false, false }
        }
    );

    public static readonly BeatPattern Techno = new BeatPattern(
        "Techno",
        PatternType.Techno,
        0.1f,
        new List<bool[]>
        {
            // Kick (Channel 1)
            new bool[] { true, false, false, false, true, false, false, false, true, false, false, false, true, false, false, false },
            // Open HiHat (Channel 2)
            new bool[] { false, false, true, false, false, false, true, false, false, false, true, false, false, false, true, false },
            // Clap (Channel 3)
            new bool[] { false, false, false, false, true, false, false, false, false, false, false, false, true, false, false, false },
            // Closed HiHat (Channel 4)
            new bool[] { true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true },
            // Snare (Channel 5)
            new bool[] { false, false, false, false, true, false, false, false, false, false, false, false, true, false, false, false }
        }
    );

    // Helper method to get a pattern by type
    public static BeatPattern GetPattern(PatternType type)
    {
        switch (type)
        {
            case PatternType.DrumNBass:
                return DrumNBass;
            case PatternType.House:
                return House;
            case PatternType.HipHop:
                return HipHop;
            case PatternType.Techno:
                return Techno;
            case PatternType.Blank:
                return Blank;
            default:
                return Blank;
        }
    }
} 