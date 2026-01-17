using UnityEngine;
using System.Collections.Generic;

public enum PatternType
{
    DrumNBass,
    House,
    HipHop,
    Techno,
    Custom
}

[System.Serializable]
public class BeatPattern
{
    public string name;
    public PatternType type;
    public float swing = 0.0f; // 0.0 to 1.0, where 0.5 is maximum swing
    public List<bool[]> channelPatterns = new List<bool[]>();

    public BeatPattern(string name, PatternType type, float swing, List<bool[]> patterns)
    {
        this.name = name;
        this.type = type;
        this.swing = Mathf.Clamp(swing, 0f, 1f);
        this.channelPatterns = patterns;
    }
}

public static class SequencerPatterns
{
    // Default patterns for 5 channels (Kick, Open HiHat, Clap, Closed HiHat, Snare)
    public static readonly BeatPattern DrumNBass = new BeatPattern(
        "Drum & Bass",
        PatternType.DrumNBass,
        0.3f,
        new List<bool[]>
        {
            // Kick (Channel 1)
            new bool[] { true, false, false, false, true, false, false, false, true, false, false, false, true, false, false, false },
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
            default:
                return DrumNBass;
        }
    }
} 