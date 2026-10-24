namespace HotbarStudio;
public static class BatchApplier
{
    // Verify every non-empty destination before clearing ANY source. No retry of an uncertain write.
    public static void Apply(IReadOnlyList<Change> changes, Func<Bar, Slot[]> read, Action<Bar, int, Slot> write, Action save)
    {
        foreach (var c in changes)
            if (!read(c.Bar).SequenceEqual(c.Before)) throw new InvalidOperationException("Conflict");
        bool dirty = false;
        try
        {
            foreach (var c in changes)
                for (int i = 0; i < c.After.Length; i++)
                    if (c.Before[i] != c.After[i] && !c.After[i].IsEmpty) { dirty = true; write(c.Bar, i, c.After[i]); }
            foreach (var c in changes)
            {
                var actual = read(c.Bar);
                for (int i = 0; i < c.After.Length; i++)
                    if (!c.After[i].IsEmpty && actual[i] != c.After[i]) throw new InvalidOperationException("VerifyFailed");
            }
            foreach (var c in changes)
                for (int i = 0; i < c.After.Length; i++)
                    if (c.Before[i] != c.After[i] && c.After[i].IsEmpty) { dirty = true; write(c.Bar, i, c.After[i]); }
            foreach (var c in changes)
                if (!read(c.Bar).SequenceEqual(c.After)) throw new InvalidOperationException("VerifyFailed");
        }
        finally { if (dirty) save(); }
    }
}
