using NUnit.Framework;
using OperationHeist.Network;
using OperationHeist.Player;

namespace OperationHeist.Tests
{
    public class NetworkHealthRulesTests
    {
        [Test]
        public void ReviveRequiresDownedTargetHealthyMedicAndRange()
        {
            Assert.That(NetworkHealthRules.CanRevive(true, false, 2f), Is.True);
            Assert.That(NetworkHealthRules.CanRevive(true, false, 3f), Is.False);
            Assert.That(NetworkHealthRules.CanRevive(false, false, 1f), Is.False);
            Assert.That(NetworkHealthRules.CanRevive(true, true, 1f), Is.False);
        }
    }

    public class WirePatchMinigameTests
    {
        [Test]
        public void CorrectOrderSucceedsAndWrongExceedsMistakesFails()
        {
            var sequence = WirePatchRules.CreateShuffledSequence(3, new System.Random(7));
            Assert.That(sequence.Length, Is.EqualTo(3));
            Assert.That(WirePatchRules.IsValidPick(sequence, 0, sequence[0]), Is.True);
            Assert.That(WirePatchRules.IsValidPick(sequence, 0, sequence[0] == 0 ? 1 : 0), Is.False);

            var go = new UnityEngine.GameObject("WireTest");
            var mini = go.AddComponent<WirePatchMinigame>();
            mini.Configure(3, 0);
            mini.Begin(11);
            int expected = mini.PeekExpectedWire();
            Assert.That(mini.TrySelect(expected == 0 ? 1 : 0), Is.False);
            Assert.That(mini.Result, Is.EqualTo(MinigameResult.Failed));
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void FullCorrectSequenceSucceeds()
        {
            var go = new UnityEngine.GameObject("WireOk");
            var mini = go.AddComponent<WirePatchMinigame>();
            mini.Configure(4, 1);
            mini.Begin(3);
            while (mini.IsRunning)
                Assert.That(mini.TrySelect(mini.PeekExpectedWire()), Is.True);
            Assert.That(mini.Result, Is.EqualTo(MinigameResult.Success));
            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
