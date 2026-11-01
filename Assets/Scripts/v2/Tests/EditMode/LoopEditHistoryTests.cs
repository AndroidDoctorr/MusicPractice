using NUnit.Framework;

namespace MusicPractice.SpatialLoop.Tests
{
    public sealed class LoopEditHistoryTests
    {
        [Test]
        public void ExecuteUndoAndRedoApplyCommandInOrder()
        {
            int value = 0;
            var history = new LoopEditHistory();
            var command = new DelegateLoopEditCommand(
                () => value = 7,
                () => value = 0);

            history.Execute(command);
            Assert.That(value, Is.EqualTo(7));
            Assert.That(history.UndoCount, Is.EqualTo(1));

            Assert.That(history.TryUndo(), Is.True);
            Assert.That(value, Is.EqualTo(0));
            Assert.That(history.RedoCount, Is.EqualTo(1));

            Assert.That(history.TryRedo(), Is.True);
            Assert.That(value, Is.EqualTo(7));
            Assert.That(history.UndoCount, Is.EqualTo(1));
        }

        [Test]
        public void NewCommandClearsRedoHistory()
        {
            int value = 0;
            var history = new LoopEditHistory();

            history.Execute(new DelegateLoopEditCommand(
                () => value = 1,
                () => value = 0));
            history.TryUndo();
            history.Execute(new DelegateLoopEditCommand(
                () => value = 2,
                () => value = 0));

            Assert.That(value, Is.EqualTo(2));
            Assert.That(history.RedoCount, Is.EqualTo(0));
            Assert.That(history.TryRedo(), Is.False);
        }

        [Test]
        public void SixEightSnapCandidatesIncludeBothGroupStarts()
        {
            var signature = TimeSignatureData.SixEight();
            var candidates = SnapResolver.BuildMeterCandidates(signature, 1);

            Assert.That(
                candidates.Exists(candidate =>
                    candidate.Tick == 0
                    && candidate.Kind == SnapKind.BeatGroup),
                Is.True);
            Assert.That(
                candidates.Exists(candidate =>
                    candidate.Tick == 1440
                    && candidate.Kind == SnapKind.BeatGroup),
                Is.True);
        }
    }
}
