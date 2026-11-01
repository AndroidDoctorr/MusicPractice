using System;
using System.Collections.Generic;

namespace MusicPractice.SpatialLoop
{
    public enum SampleRole
    {
        Kick,
        OpenHat,
        Clap,
        ClosedHat,
        Snare
    }

    [Serializable]
    public sealed class PatternPoint
    {
        public SampleRole Role;
        public long Tick;
        public float Velocity;

        public PatternPoint(SampleRole role, long tick, float velocity)
        {
            Role = role;
            Tick = tick;
            Velocity = velocity;
        }
    }

    [Serializable]
    public sealed class PatternDefinition
    {
        public string Name;
        public TimeSignatureData TimeSignature;
        public List<PatternPoint> Points = new List<PatternPoint>();

        public IEnumerable<PatternPoint> GetPoints(SampleRole role)
        {
            for (int i = 0; i < Points.Count; i++)
            {
                if (Points[i].Role == role)
                {
                    yield return Points[i];
                }
            }
        }
    }

    public static class SpatialPatterns
    {
        public static PatternDefinition CreateHouse()
        {
            var pattern = new PatternDefinition
            {
                Name = "House",
                TimeSignature = TimeSignatureData.FourFour()
            };

            Add(pattern, SampleRole.Kick, 1f, 0, 4, 8, 12);
            Add(pattern, SampleRole.OpenHat, 0.65f, 2, 6, 10, 14);
            Add(pattern, SampleRole.Clap, 0.75f, 4, 12);
            Add(
                pattern,
                SampleRole.ClosedHat,
                0.4f,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15);
            Add(pattern, SampleRole.Snare, 0.9f, 4, 12);
            return pattern;
        }

        private static void Add(
            PatternDefinition pattern,
            SampleRole role,
            float velocity,
            params int[] steps)
        {
            const long ticksPerSixteenth = MusicalTime.PulsesPerQuarter / 4;
            for (int i = 0; i < steps.Length; i++)
            {
                pattern.Points.Add(new PatternPoint(
                    role,
                    steps[i] * ticksPerSixteenth,
                    velocity));
            }
        }
    }
}
