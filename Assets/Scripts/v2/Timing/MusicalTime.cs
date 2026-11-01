using System;

namespace MusicPractice.SpatialLoop
{
    public static class MusicalTime
    {
        public const int PulsesPerQuarter = 960;

        public static long GetTicksPerDenominatorUnit(
            int denominator,
            int pulsesPerQuarter = PulsesPerQuarter)
        {
            if (denominator < 1 || (denominator & (denominator - 1)) != 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(denominator),
                    "The denominator must be a positive power of two.");
            }

            if (pulsesPerQuarter < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pulsesPerQuarter));
            }

            long wholeNoteTicks = pulsesPerQuarter * 4L;
            if (wholeNoteTicks % denominator != 0)
            {
                throw new ArgumentException(
                    "The pulses-per-quarter resolution cannot exactly represent this denominator.");
            }

            return wholeNoteTicks / denominator;
        }

        public static long GetTicksPerBar(
            TimeSignatureData timeSignature,
            int pulsesPerQuarter = PulsesPerQuarter)
        {
            if (timeSignature == null)
            {
                throw new ArgumentNullException(nameof(timeSignature));
            }

            return timeSignature.Numerator
                * GetTicksPerDenominatorUnit(
                    timeSignature.Denominator,
                    pulsesPerQuarter);
        }

        public static long GetLoopLengthTicks(
            TimeSignatureData timeSignature,
            int bars,
            int pulsesPerQuarter = PulsesPerQuarter)
        {
            if (bars < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(bars));
            }

            return GetTicksPerBar(timeSignature, pulsesPerQuarter) * bars;
        }

        public static long GetTicksPerTempoBeat(
            TempoBeatUnit tempoBeatUnit,
            int pulsesPerQuarter = PulsesPerQuarter)
        {
            if (pulsesPerQuarter < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pulsesPerQuarter));
            }

            switch (tempoBeatUnit)
            {
                case TempoBeatUnit.Eighth:
                    return pulsesPerQuarter / 2L;
                case TempoBeatUnit.Quarter:
                    return pulsesPerQuarter;
                case TempoBeatUnit.DottedQuarter:
                    return pulsesPerQuarter * 3L / 2L;
                case TempoBeatUnit.Half:
                    return pulsesPerQuarter * 2L;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tempoBeatUnit));
            }
        }

        public static double TicksToSeconds(
            double ticks,
            double beatsPerMinute,
            TempoBeatUnit tempoBeatUnit,
            int pulsesPerQuarter = PulsesPerQuarter)
        {
            if (beatsPerMinute <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(beatsPerMinute));
            }

            double ticksPerBeat = GetTicksPerTempoBeat(
                tempoBeatUnit,
                pulsesPerQuarter);
            return ticks / ticksPerBeat * (60d / beatsPerMinute);
        }

        public static double SecondsToTicks(
            double seconds,
            double beatsPerMinute,
            TempoBeatUnit tempoBeatUnit,
            int pulsesPerQuarter = PulsesPerQuarter)
        {
            if (seconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(seconds));
            }

            if (beatsPerMinute <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(beatsPerMinute));
            }

            double ticksPerBeat = GetTicksPerTempoBeat(
                tempoBeatUnit,
                pulsesPerQuarter);
            return seconds * (beatsPerMinute / 60d) * ticksPerBeat;
        }

        public static long NormalizeTick(long tick, long loopLengthTicks)
        {
            if (loopLengthTicks < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(loopLengthTicks));
            }

            long normalized = tick % loopLengthTicks;
            return normalized < 0 ? normalized + loopLengthTicks : normalized;
        }

        public static double NormalizeTick(double tick, long loopLengthTicks)
        {
            if (loopLengthTicks < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(loopLengthTicks));
            }

            double normalized = tick % loopLengthTicks;
            return normalized < 0 ? normalized + loopLengthTicks : normalized;
        }

        public static double TickToPhase(double tick, long loopLengthTicks)
        {
            return NormalizeTick(tick, loopLengthTicks) / loopLengthTicks;
        }

        public static long PhaseToTick(double phase, long loopLengthTicks)
        {
            if (double.IsNaN(phase) || double.IsInfinity(phase))
            {
                throw new ArgumentOutOfRangeException(nameof(phase));
            }

            double normalizedPhase = phase - Math.Floor(phase);
            long tick = (long)Math.Round(
                normalizedPhase * loopLengthTicks,
                MidpointRounding.AwayFromZero);
            return NormalizeTick(tick, loopLengthTicks);
        }

        public static double TickToClockwiseAngleDegrees(
            double tick,
            long loopLengthTicks)
        {
            return TickToPhase(tick, loopLengthTicks) * 360d;
        }

        public static long ClockwiseAngleDegreesToTick(
            double angleDegrees,
            long loopLengthTicks)
        {
            if (double.IsNaN(angleDegrees) || double.IsInfinity(angleDegrees))
            {
                throw new ArgumentOutOfRangeException(nameof(angleDegrees));
            }

            double normalizedAngle = angleDegrees % 360d;
            if (normalizedAngle < 0)
            {
                normalizedAngle += 360d;
            }

            return PhaseToTick(normalizedAngle / 360d, loopLengthTicks);
        }
    }
}
