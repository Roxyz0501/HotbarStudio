using HotbarStudio;
int checks = 0;
void Check(bool value, string label) { checks++; if (!value) throw new Exception(label); }
void Throws(Action action, string key) { try { action(); throw new Exception("No exception: " + key); } catch (InvalidOperationException ex) { Check(ex.Message == key, key); } }
Slot[] Values(int n, int start) => Enumerable.Range(start, n).Select(i => new Slot(1, (uint)i)).ToArray();
foreach (bool pvp in new[] { false, true }) foreach (bool cross in new[] { false, true })
{
    int count = cross ? 16 : 12;
    var a = new Bar(pvp, 19, cross, 1); var b = a with { Job = 0, Number = 2 };
    var x = Values(count, 100); var y = Values(count, 500);
    var e = new Editor(); e.Load(a, x); e.Load(b, y);
    x[0] = default; Check(e.View(a)[0].Id == 100, "Snapshots are isolated");
    e.Transfer(a, -1, b, -1, false); Check(e.View(a).SequenceEqual(e.View(b)), "Whole copy");
    e.Undo(); Check(e.View(b).SequenceEqual(y), "Whole undo");
    e.Transfer(a, 0, b, count - 1, true);
    Check(e.View(a)[0].IsEmpty && e.View(b)[count - 1].Id == 100, "Slot move");
    var changes = e.Changes;
    var mem = new Dictionary<Bar, Slot[]> { [a] = Values(count, 100), [b] = y.ToArray() };
    int saves = 0, writes = 0;
    BatchApplier.Apply(changes, bar => mem[bar].ToArray(), (bar, i, s) => { mem[bar][i] = s; writes++; }, () => saves++);
    Check(saves == 1 && writes == 2, "Move has two changed slots and one save");
    Check(mem[a][0].IsEmpty && mem[b][count - 1].Id == 100, "Batch applied");
    mem[a] = Values(count, 100); mem[b] = y.ToArray(); saves = 0;
    Throws(() => BatchApplier.Apply(changes, bar => mem[bar].ToArray(), (bar, i, s) => { /* dropped destination write */ }, () => saves++), "VerifyFailed");
    Check(mem[a][0].Id == 100, "Failed destination never clears source");
    Check(saves == 1, "Uncertain write saved once without retries");
    mem[b][0] = new(1, 900); writes = 0; saves = 0;
    Throws(() => BatchApplier.Apply(changes, bar => mem[bar].ToArray(), (bar, i, s) => writes++, () => saves++), "Conflict");
    Check(writes == 0 && saves == 0, "Stale target blocked before writes");
    e.Undo(); e.Transfer(a, -1, b, -1, true);
    Check(e.View(a).All(s => s.IsEmpty) && e.View(b)[0].Id == 100, "Whole move");
    e.Undo(); e.Transfer(a, 0, a, 1, true); Check(e.View(a)[0].IsEmpty && e.View(a)[1].Id == 100, "Same bar overlapping slot move");
    e.Undo(); e.Transfer(a, -1, a, -1, true); Check(e.Changes.Count == 0, "Self move is no-op");
    var other = a with { Pvp = !pvp }; e.Load(other, Values(count, 600));
    Throws(() => e.Transfer(a, 0, other, 0, false), "ModeMismatch");
    var shape = a with { Cross = !cross }; e.Load(shape, Values(shape.Count, 700));
    Throws(() => e.Transfer(a, -1, shape, -1, false), "SizeMismatch");
    e.Transfer(a, 0, shape, shape.Count - 1, false); Check(e.View(shape)[^1].Id == 100, "Cross-kind single slot");
    Throws(() => e.Transfer(a, count, b, 0, false), "Invalid");
    e.Reset(); Check(e.Changes.Count == 0 && !e.Bars.Any(), "Reset clears staged data");
}
var root = Path.Combine(AppContext.BaseDirectory, "fixtures", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
string path = Path.Combine(root, "config.json");
using (var store = new ConfigurationStore(path))
{
    store.Edit(c => c.Language = "ja"); store.Flush();
    long before = store.Writes;
    for (int i = 0; i < 600; i++) store.Edit(c => c.Language = i % 2 == 0 ? "de" : "en");
    Check(store.Writes == before, "600 edits cause no immediate writes");
    store.Tick(long.MaxValue - 100000); Check(store.Writes == before + 1, "One changed autosave");
    for (int i = 0; i < 8; i++) store.Flush(); Check(store.Writes == before + 1, "Unchanged saves zero writes");
    foreach (var code in Loc.Codes) { store.Edit(c => c.Language = code); store.Flush(); }
    Check(Directory.GetFiles(root, "config.json.bak.*").Length == 3, "Three backup generations");
    Console.WriteLine($"Settings metrics: {store.Writes} writes, {store.LogicalBytes} logical bytes");
}
File.WriteAllText(path, "broken");
using (var recovered = new ConfigurationStore(path)) Check(recovered.Value.Language == "zh-Hans", "Recover newest valid backup");
int attempts = 0;
using (var bad = new ConfigurationStore(Path.Combine(root, "failed.json"), fault: step => { if (step == "write") { attempts++; throw new IOException("injected"); } }))
{
    for (int i = 0; i < 10; i++) bad.Flush(); Check(attempts == 3, "Bounded failure retries");
}
Check(attempts == 3, "Dispose does not retry exhausted writes");
var disposed = new ConfigurationStore(Path.Combine(root, "disposed.json")); disposed.Edit(c => c.Language = "ko"); disposed.Dispose();
long disposedWrites = disposed.Writes; disposed.Edit(c => c.Language = "de"); disposed.Flush(); Check(disposed.Writes == disposedWrites && disposed.Value.Language == "ko", "No changes after dispose");
var dict = (Dictionary<string, string[]>)typeof(Loc).GetField("Text", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!.GetValue(null)!;
foreach (var row in dict) { Check(row.Value.Length == 7, "Seven languages: " + row.Key); foreach (var code in Loc.Codes) Check(!string.IsNullOrWhiteSpace(Loc.Get(code, row.Key)), "Translation " + code + row.Key); }
Console.WriteLine($"PASS: {checks} checks. Fixtures: {root}");
