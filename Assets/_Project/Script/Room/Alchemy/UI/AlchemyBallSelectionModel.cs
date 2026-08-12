using System;
using System.Collections.Generic;

public sealed class AlchemyBallSelectionEntry
{
    public Ball Ball { get; }
    public BallDefinition Definition =>
        Ball != null ? Ball.Definition : null;
    public bool IsSelected { get; internal set; }

    public AlchemyBallSelectionEntry(Ball ball)
    {
        Ball = ball;
    }
}

public sealed class AlchemyBallSelectionModel
{
    private readonly List<AlchemyBallSelectionEntry> entries =
        new List<AlchemyBallSelectionEntry>();
    private readonly HashSet<Ball> selectedBalls =
        new HashSet<Ball>();

    public IReadOnlyList<AlchemyBallSelectionEntry> Entries => entries;
    public int SelectedCount => selectedBalls.Count;

    public event Action Changed;

    public void Refresh(BallCollection collection)
    {
        entries.Clear();

        if (collection == null)
        {
            selectedBalls.Clear();
            Changed?.Invoke();
            return;
        }

        List<Ball> snapshot = collection.CreateSnapshot();
        selectedBalls.RemoveWhere(ball =>
            ball == null || !snapshot.Contains(ball));

        for (int i = 0; i < snapshot.Count; i++)
        {
            Ball ball = snapshot[i];
            if (ball == null || ball.Definition == null)
            {
                continue;
            }

            AlchemyBallSelectionEntry entry =
                new AlchemyBallSelectionEntry(ball)
                {
                    IsSelected = selectedBalls.Contains(ball)
                };
            entries.Add(entry);
        }

        Changed?.Invoke();
    }

    public bool Toggle(Ball ball)
    {
        if (ball == null || ball.Definition == null)
        {
            return false;
        }

        if (!selectedBalls.Add(ball))
        {
            selectedBalls.Remove(ball);
        }

        SynchronizeSelectionFlags();
        Changed?.Invoke();
        return true;
    }

    public void ClearSelection(bool notifyChanged = true)
    {
        if (selectedBalls.Count == 0)
        {
            return;
        }

        selectedBalls.Clear();
        SynchronizeSelectionFlags();
        if (notifyChanged)
        {
            Changed?.Invoke();
        }
    }

    public void SelectRange(IEnumerable<Ball> balls, bool additive)
    {
        if (!additive)
        {
            selectedBalls.Clear();
        }

        if (balls != null)
        {
            foreach (Ball ball in balls)
            {
                if (ball != null && ball.Definition != null)
                {
                    selectedBalls.Add(ball);
                }
            }
        }

        SynchronizeSelectionFlags();
        Changed?.Invoke();
    }

    public List<Ball> CreateSelectedBallSnapshot()
    {
        List<Ball> result = new List<Ball>();

        for (int i = 0; i < entries.Count; i++)
        {
            Ball ball = entries[i].Ball;
            if (ball != null && selectedBalls.Contains(ball))
            {
                result.Add(ball);
            }
        }

        return result;
    }

    private void SynchronizeSelectionFlags()
    {
        for (int i = 0; i < entries.Count; i++)
        {
            entries[i].IsSelected =
                entries[i].Ball != null &&
                selectedBalls.Contains(entries[i].Ball);
        }
    }
}
