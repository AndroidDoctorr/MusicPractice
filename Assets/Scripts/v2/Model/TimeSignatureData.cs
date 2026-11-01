using System;
using System.Collections.Generic;
using UnityEngine;

namespace MusicPractice.SpatialLoop
{
    public enum TempoBeatUnit
    {
        Eighth,
        Quarter,
        DottedQuarter,
        Half
    }

    [Serializable]
    public sealed class TimeSignatureData
    {
        [SerializeField, Min(1)] private int _numerator = 4;
        [SerializeField, Min(1)] private int _denominator = 4;
        [SerializeField] private int[] _beatGroups = { 1, 1, 1, 1 };
        [SerializeField] private TempoBeatUnit _tempoBeatUnit = TempoBeatUnit.Quarter;

        public int Numerator => Mathf.Max(1, _numerator);
        public int Denominator => IsPowerOfTwo(_denominator) ? _denominator : 4;
        public TempoBeatUnit TempoBeatUnit => _tempoBeatUnit;

        public TimeSignatureData()
        {
        }

        public TimeSignatureData(
            int numerator,
            int denominator,
            int[] beatGroups,
            TempoBeatUnit tempoBeatUnit)
        {
            if (numerator < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(numerator));
            }

            if (!IsPowerOfTwo(denominator))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(denominator),
                    "The denominator must be a positive power of two.");
            }

            _numerator = numerator;
            _denominator = denominator;
            _beatGroups = beatGroups != null ? (int[])beatGroups.Clone() : null;
            _tempoBeatUnit = tempoBeatUnit;

            if (!HasValidBeatGroups())
            {
                throw new ArgumentException(
                    "Beat groups must be positive and sum to the numerator.",
                    nameof(beatGroups));
            }
        }

        public int[] GetBeatGroups()
        {
            if (HasValidBeatGroups())
            {
                return (int[])_beatGroups.Clone();
            }

            return CreateDefaultBeatGroups(Numerator, Denominator);
        }

        public bool HasValidBeatGroups()
        {
            if (_beatGroups == null || _beatGroups.Length == 0)
            {
                return false;
            }

            int sum = 0;
            for (int i = 0; i < _beatGroups.Length; i++)
            {
                if (_beatGroups[i] < 1)
                {
                    return false;
                }

                sum += _beatGroups[i];
            }

            return sum == Numerator;
        }

        public IReadOnlyList<long> GetGroupStartTicks(
            int pulsesPerQuarter = MusicalTime.PulsesPerQuarter)
        {
            int[] groups = GetBeatGroups();
            long ticksPerUnit = MusicalTime.GetTicksPerDenominatorUnit(
                Denominator,
                pulsesPerQuarter);
            var starts = new List<long>(groups.Length);
            long currentTick = 0;

            for (int i = 0; i < groups.Length; i++)
            {
                starts.Add(currentTick);
                currentTick += groups[i] * ticksPerUnit;
            }

            return starts;
        }

        public static TimeSignatureData FourFour()
        {
            return new TimeSignatureData(
                4,
                4,
                new[] { 1, 1, 1, 1 },
                TempoBeatUnit.Quarter);
        }

        public static TimeSignatureData ThreeFour()
        {
            return new TimeSignatureData(
                3,
                4,
                new[] { 1, 1, 1 },
                TempoBeatUnit.Quarter);
        }

        public static TimeSignatureData SixEight()
        {
            return new TimeSignatureData(
                6,
                8,
                new[] { 3, 3 },
                TempoBeatUnit.DottedQuarter);
        }

        private static int[] CreateDefaultBeatGroups(int numerator, int denominator)
        {
            if (denominator == 8 && numerator > 3 && numerator % 3 == 0)
            {
                int[] compoundGroups = new int[numerator / 3];
                for (int i = 0; i < compoundGroups.Length; i++)
                {
                    compoundGroups[i] = 3;
                }

                return compoundGroups;
            }

            int[] simpleGroups = new int[numerator];
            for (int i = 0; i < simpleGroups.Length; i++)
            {
                simpleGroups[i] = 1;
            }

            return simpleGroups;
        }

        private static bool IsPowerOfTwo(int value)
        {
            return value > 0 && (value & (value - 1)) == 0;
        }
    }
}
