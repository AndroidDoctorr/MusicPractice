using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace MusicPractice.SpatialLoop.Tests
{
    public sealed class EffectAndPatternTests
    {
        [Test]
        public void EffectChainCloneIsIndependent()
        {
            var original = new EffectChainData();
            original.SetXY(EffectPairId.Space, new Vector2(0.4f, 0.7f));

            EffectChainData clone = original.Clone();
            clone.SetXY(EffectPairId.Space, new Vector2(0.1f, 0.2f));

            Assert.That(
                original.GetXY(EffectPairId.Space),
                Is.EqualTo(new Vector2(0.4f, 0.7f)));
            Assert.That(
                clone.GetXY(EffectPairId.Space),
                Is.EqualTo(new Vector2(0.1f, 0.2f)));
        }

        [Test]
        public void HousePatternExposesExpectedKickAndHatRoles()
        {
            PatternDefinition house = SpatialPatterns.CreateHouse();

            Assert.That(
                house.GetPoints(SampleRole.Kick).Select(point => point.Tick),
                Is.EqualTo(new long[] { 0, 960, 1920, 2880 }));
            Assert.That(
                house.GetPoints(SampleRole.OpenHat).Select(point => point.Tick),
                Is.EqualTo(new long[] { 480, 1440, 2400, 3360 }));
        }

        [Test]
        public void SampleInstanceCopiesAssetEffects()
        {
            var defaults = new EffectChainData();
            defaults.SetXY(EffectPairId.Filter, new Vector2(0.2f, 0.6f));

            var instance = new SampleInstanceData(
                "asset",
                0,
                240,
                0.8f,
                0f,
                defaults);
            defaults.SetXY(EffectPairId.Filter, new Vector2(0.8f, 0.1f));

            Assert.That(
                instance.Effects.GetXY(EffectPairId.Filter),
                Is.EqualTo(new Vector2(0.2f, 0.6f)));
        }
    }
}
