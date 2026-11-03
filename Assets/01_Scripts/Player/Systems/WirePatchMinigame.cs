using System;
using UnityEngine;

namespace OperationHeist.Player
{
    public enum MinigameResult { Running, Success, Failed, Cancelled }

    /// <summary>Abstract wire-order puzzle. No art packs; logic only.</summary>
    [DisallowMultipleComponent]
    public sealed class WirePatchMinigame : MonoBehaviour
    {
        [SerializeField] private int wireCount = 4;
        [SerializeField] private int maxMistakes = 1;

        private int[] _solution;
        private int _index;
        private int _mistakes;
        public MinigameResult Result { get; private set; } = MinigameResult.Cancelled;
        public int WireCount => _solution != null ? _solution.Length : 0;
        public int ProgressIndex => _index;
        public int Mistakes => _mistakes;
        public bool IsRunning => Result == MinigameResult.Running;
        public event Action Changed;

        public void Configure(int wires, int allowedMistakes)
        {
            wireCount = Mathf.Clamp(wires, 2, 6);
            maxMistakes = Mathf.Max(0, allowedMistakes);
        }

        public void Begin(int seed = 0)
        {
            var rng = seed == 0 ? new System.Random() : new System.Random(seed);
            _solution = WirePatchRules.CreateShuffledSequence(wireCount, rng);
            _index = 0;
            _mistakes = 0;
            Result = MinigameResult.Running;
            Changed?.Invoke();
        }

        public bool TrySelect(int wireId)
        {
            if (Result != MinigameResult.Running || _solution == null) return false;
            if (!WirePatchRules.IsValidPick(_solution, _index, wireId))
            {
                _mistakes++;
                if (_mistakes > maxMistakes)
                {
                    Result = MinigameResult.Failed;
                    Changed?.Invoke();
                    return false;
                }
                Changed?.Invoke();
                return false;
            }
            _index++;
            if (_index >= _solution.Length) Result = MinigameResult.Success;
            Changed?.Invoke();
            return true;
        }

        public void Cancel()
        {
            if (Result != MinigameResult.Running) return;
            Result = MinigameResult.Cancelled;
            Changed?.Invoke();
        }

        public int PeekExpectedWire() =>
            Result == MinigameResult.Running && _solution != null && _index < _solution.Length
                ? _solution[_index]
                : -1;
    }

    public static class WirePatchRules
    {
        public static int[] CreateShuffledSequence(int count, System.Random rng)
        {
            count = Mathf.Clamp(count, 2, 6);
            var sequence = new int[count];
            for (int i = 0; i < count; i++) sequence[i] = i;
            for (int i = count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (sequence[i], sequence[j]) = (sequence[j], sequence[i]);
            }
            return sequence;
        }

        public static bool IsValidPick(int[] solution, int index, int wireId)
        {
            if (solution == null || index < 0 || index >= solution.Length) return false;
            return solution[index] == wireId;
        }
    }
}
