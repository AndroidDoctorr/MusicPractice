using System.Collections.Generic;

namespace MusicPractice.SpatialLoop
{
    public sealed class ArpeggiatorPattern
    {
        public string Name;
        public int[] SixteenthSteps;
        public float Velocity;

        public ArpeggiatorPattern(
            string name,
            float velocity,
            params int[] sixteenthSteps)
        {
            Name = name;
            Velocity = velocity;
            SixteenthSteps = sixteenthSteps;
        }
    }

    public static class ArpeggiatorPatterns
    {
        public static IReadOnlyList<ArpeggiatorPattern> PrototypeSet { get; } =
            new[]
            {
                new ArpeggiatorPattern("UP", 0.85f, 0, 1, 2, 3, 4, 5, 6, 7),
                new ArpeggiatorPattern("DOWN", 0.85f, 7, 6, 5, 4, 3, 2, 1, 0),
                new ArpeggiatorPattern("UP-DN", 0.8f, 0, 1, 2, 3, 2, 1),
                new ArpeggiatorPattern("BOUNCE", 0.75f, 0, 2, 1, 3, 2, 4, 3, 5),
                new ArpeggiatorPattern("TRIP", 0.8f, 0, 2, 4, 6, 8, 10),
                new ArpeggiatorPattern("OFFBEAT", 0.7f, 2, 6, 10, 14)
            };
    }
}
