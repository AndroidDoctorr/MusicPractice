using System;
using System.Collections.Generic;

namespace MusicPractice.SpatialLoop
{
    public enum SnapKind
    {
        LoopBoundary,
        Bar,
        BeatGroup,
        Pattern,
        DenominatorUnit,
        Subdivision,
        ExistingEvent
    }

    public readonly struct SnapCandidate
    {
        public long Tick { get; }
        public SnapKind Kind { get; }
        public int Priority { get; }

        public SnapCandidate(long tick, SnapKind kind, int priority)
        {
            Tick = tick;
            Kind = kind;
            Priority = priority;
        }
    }

    public readonly struct SnapResult
    {
        public long Tick { get; }
        public SnapKind Kind { get; }
        public long DistanceTicks { get; }

        public SnapResult(long tick, SnapKind kind, long distanceTicks)
        {
            Tick = tick;
            Kind = kind;
            DistanceTicks = distanceTicks;
        }
    }

    public static class SnapResolver
    {
        public static List<SnapCandidate> BuildMeterCandidates(
            TimeSignatureData timeSignature,
            int bars,
            int subdivisionsPerGroup = 4)
        {
            if (timeSignature == null)
            {
                throw new ArgumentNullException(nameof(timeSignature));
            }

            if (bars < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(bars));
            }

            if (subdivisionsPerGroup < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(subdivisionsPerGroup));
            }

            long ticksPerBar = MusicalTime.GetTicksPerBar(timeSignature);
            long ticksPerUnit = MusicalTime.GetTicksPerDenominatorUnit(
                timeSignature.Denominator);
            int[] groups = timeSignature.GetBeatGroups();
            var candidates = new List<SnapCandidate>();

            candidates.Add(new SnapCandidate(0, SnapKind.LoopBoundary, 0));

            for (int bar = 0; bar < bars; bar++)
            {
                long barStart = bar * ticksPerBar;
                if (bar > 0)
                {
                    candidates.Add(new SnapCandidate(barStart, SnapKind.Bar, 1));
                }

                for (int unit = 0; unit < timeSignature.Numerator; unit++)
                {
                    candidates.Add(new SnapCandidate(
                        barStart + unit * ticksPerUnit,
                        SnapKind.DenominatorUnit,
                        4));
                }

                long groupStart = barStart;
                for (int groupIndex = 0; groupIndex < groups.Length; groupIndex++)
                {
                    candidates.Add(new SnapCandidate(
                        groupStart,
                        SnapKind.BeatGroup,
                        2));

                    long groupLength = groups[groupIndex] * ticksPerUnit;
                    for (int division = 1; division < subdivisionsPerGroup; division++)
                    {
                        long subdivisionTick = groupStart + (long)Math.Round(
                            groupLength * (division / (double)subdivisionsPerGroup),
                            MidpointRounding.AwayFromZero);
                        candidates.Add(new SnapCandidate(
                            subdivisionTick,
                            SnapKind.Subdivision,
                            5));
                    }

                    groupStart += groupLength;
                }
            }

            return candidates;
        }

        public static SnapCandidate CreatePatternCandidate(long tick)
        {
            return new SnapCandidate(tick, SnapKind.Pattern, 3);
        }

        public static SnapCandidate CreateExistingEventCandidate(long tick)
        {
            return new SnapCandidate(tick, SnapKind.ExistingEvent, 6);
        }

        public static bool TryResolveNearest(
            long rawTick,
            long loopLengthTicks,
            IEnumerable<SnapCandidate> candidates,
            long maximumDistanceTicks,
            out SnapResult result)
        {
            if (loopLengthTicks < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(loopLengthTicks));
            }

            if (candidates == null)
            {
                throw new ArgumentNullException(nameof(candidates));
            }

            if (maximumDistanceTicks < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumDistanceTicks));
            }

            long normalizedRawTick = MusicalTime.NormalizeTick(rawTick, loopLengthTicks);
            bool found = false;
            long bestDistance = long.MaxValue;
            int bestPriority = int.MaxValue;
            SnapCandidate bestCandidate = default;

            foreach (SnapCandidate candidate in candidates)
            {
                long normalizedCandidate = MusicalTime.NormalizeTick(
                    candidate.Tick,
                    loopLengthTicks);
                long directDistance = Math.Abs(normalizedRawTick - normalizedCandidate);
                long wrappedDistance = loopLengthTicks - directDistance;
                long distance = Math.Min(directDistance, wrappedDistance);

                bool isBetter = distance < bestDistance
                    || (distance == bestDistance && candidate.Priority < bestPriority);
                if (!isBetter)
                {
                    continue;
                }

                found = true;
                bestDistance = distance;
                bestPriority = candidate.Priority;
                bestCandidate = new SnapCandidate(
                    normalizedCandidate,
                    candidate.Kind,
                    candidate.Priority);
            }

            if (!found || bestDistance > maximumDistanceTicks)
            {
                result = default;
                return false;
            }

            result = new SnapResult(
                bestCandidate.Tick,
                bestCandidate.Kind,
                bestDistance);
            return true;
        }
    }
}
