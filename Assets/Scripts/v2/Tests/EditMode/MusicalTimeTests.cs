using System.Collections.Generic;
using NUnit.Framework;

namespace MusicPractice.SpatialLoop.Tests
{
    public sealed class MusicalTimeTests
    {
        [Test]
        public void CommonMetersHaveExactBarLengths()
        {
            Assert.That(
                MusicalTime.GetTicksPerBar(TimeSignatureData.FourFour()),
                Is.EqualTo(3840));
            Assert.That(
                MusicalTime.GetTicksPerBar(TimeSignatureData.ThreeFour()),
                Is.EqualTo(2880));
            Assert.That(
                MusicalTime.GetTicksPerBar(TimeSignatureData.SixEight()),
                Is.EqualTo(2880));
        }

        [Test]
        public void SixEightUsesTwoDottedQuarterGroups()
        {
            TimeSignatureData signature = TimeSignatureData.SixEight();
            IReadOnlyList<long> starts = signature.GetGroupStartTicks();

            Assert.That(starts, Is.EqualTo(new long[] { 0, 1440 }));
            Assert.That(
                MusicalTime.GetTicksPerTempoBeat(signature.TempoBeatUnit),
                Is.EqualTo(1440));
        }

        [Test]
        public void TickPhaseAndAngleRoundTripAtQuarterNotes()
        {
            const long loopLength = 3840;

            Assert.That(
                MusicalTime.TickToPhase(960, loopLength),
                Is.EqualTo(0.25d).Within(0.000001d));
            Assert.That(
                MusicalTime.TickToClockwiseAngleDegrees(960, loopLength),
                Is.EqualTo(90d).Within(0.000001d));
            Assert.That(
                MusicalTime.ClockwiseAngleDegreesToTick(90d, loopLength),
                Is.EqualTo(960));
        }

        [Test]
        public void TickNormalizationWrapsInBothDirections()
        {
            const long loopLength = 3840;

            Assert.That(MusicalTime.NormalizeTick(3840, loopLength), Is.EqualTo(0));
            Assert.That(MusicalTime.NormalizeTick(-1, loopLength), Is.EqualTo(3839));
            Assert.That(MusicalTime.PhaseToTick(1d, loopLength), Is.EqualTo(0));
        }

        [Test]
        public void MeterSnapWrapsAcrossLoopBoundary()
        {
            TimeSignatureData signature = TimeSignatureData.FourFour();
            List<SnapCandidate> candidates = SnapResolver.BuildMeterCandidates(
                signature,
                1);

            bool found = SnapResolver.TryResolveNearest(
                3800,
                MusicalTime.GetTicksPerBar(signature),
                candidates,
                100,
                out SnapResult result);

            Assert.That(found, Is.True);
            Assert.That(result.Tick, Is.EqualTo(0));
            Assert.That(result.Kind, Is.EqualTo(SnapKind.LoopBoundary));
            Assert.That(result.DistanceTicks, Is.EqualTo(40));
        }

        [Test]
        public void PatternPointCanBeatMoreDistantGridPoint()
        {
            TimeSignatureData signature = TimeSignatureData.FourFour();
            List<SnapCandidate> candidates = SnapResolver.BuildMeterCandidates(
                signature,
                1);
            candidates.Add(SnapResolver.CreatePatternCandidate(450));

            bool found = SnapResolver.TryResolveNearest(
                460,
                MusicalTime.GetTicksPerBar(signature),
                candidates,
                100,
                out SnapResult result);

            Assert.That(found, Is.True);
            Assert.That(result.Tick, Is.EqualTo(450));
            Assert.That(result.Kind, Is.EqualTo(SnapKind.Pattern));
        }
    }
}
