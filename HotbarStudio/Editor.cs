namespace HotbarStudio;

public readonly record struct Bar(bool Pvp, int Job, bool Cross, int Number)
{
    public int Index => (Cross ? 10 : 0) + Number - 1;
    public int Count => Cross ? 16 : 12;
    public void Validate() { if (Job < 0 || Job > 42 || Number < 1 || Number > (Cross ? 8 : 10)) throw new InvalidOperationException("Invalid"); }
}
public readonly record struct Slot(byte Type, uint Id)
{
    public static Slot Empty => default;
    public bool IsEmpty => Type == 0;
}
public sealed record Change(Bar Bar, Slot[] Before, Slot[] After);

// Managed staging only. No native pointers survive a read. Before values are optimistic concurrency guards.
public sealed class Editor
{
    private readonly Dictionary<Bar, Slot[]> original = [];
    private readonly Dictionary<Bar, Slot[]> staged = [];
    private readonly Stack<Dictionary<Bar, Slot[]>> undo = new();
    // Conservative for the current editing session, including undo. Cross-mode IDs must not be reinterpreted by commands.
    public bool RequiresLocalWrites { get; private set; }
    public IEnumerable<Bar> Bars => original.Keys;
    public IReadOnlyList<Change> Guards => original.Select(p => new Change(p.Key, (Slot[])p.Value.Clone(), (Slot[])p.Value.Clone())).ToArray();
    public IReadOnlyList<Change> Changes => staged.Where(p => !p.Value.SequenceEqual(original[p.Key])).Select(p => new Change(p.Key, (Slot[])original[p.Key].Clone(), (Slot[])p.Value.Clone())).ToArray();
    public void Load(Bar bar, Slot[] slots)
    {
        bar.Validate();
        if (slots.Length != bar.Count) throw new InvalidOperationException("Invalid");
        if (original.ContainsKey(bar)) return;
        if (original.Count >= 128) throw new InvalidOperationException("Limit");
        original[bar] = (Slot[])slots.Clone(); staged[bar] = (Slot[])slots.Clone();
    }
    public Slot[] View(Bar bar) => (Slot[])staged[bar].Clone();
    public bool Changed(Bar bar, int slot) => staged[bar][slot] != original[bar][slot];
    public void Restore(Bar bar, Slot[] value)
    {
        if (!original.ContainsKey(bar) || value.Length != bar.Count) throw new InvalidOperationException("Invalid");
        staged[bar] = (Slot[])value.Clone();
        RequiresLocalWrites = true;
    }
    public void Transfer(Bar source, int sourceSlot, Bar target, int targetSlot, bool move)
    {
        source.Validate(); target.Validate();
        if (!staged.ContainsKey(source) || !staged.ContainsKey(target)) throw new InvalidOperationException("Invalid");
        bool whole = sourceSlot == -1 && targetSlot == -1;
        if (whole && source.Cross != target.Cross) throw new InvalidOperationException("SizeMismatch");
        if (!whole && (sourceSlot < 0 || sourceSlot >= source.Count || targetSlot < 0 || targetSlot >= target.Count)) throw new InvalidOperationException("Invalid");
        if (source == target && (whole || sourceSlot == targetSlot)) return;
        if (undo.Count >= 100) throw new InvalidOperationException("Limit");
        undo.Push(staged.ToDictionary(p => p.Key, p => (Slot[])p.Value.Clone()));
        if (source.Pvp != target.Pvp) RequiresLocalWrites = true;
        if (whole)
        {
            staged[target] = (Slot[])staged[source].Clone();
            if (move) Array.Clear(staged[source]);
        }
        else
        {
            staged[target][targetSlot] = staged[source][sourceSlot];
            if (move) staged[source][sourceSlot] = Slot.Empty;
        }
    }
    public void Undo() { if (undo.TryPop(out var snapshot)) foreach (var p in snapshot) staged[p.Key] = p.Value; }
    public void Reset() { original.Clear(); staged.Clear(); undo.Clear(); RequiresLocalWrites = false; }
}
