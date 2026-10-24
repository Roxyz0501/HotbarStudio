using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Shell;
using System.Text;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureHotbarModule;

namespace HotbarStudio;

internal interface IGameBars
{
    ulong Identity { get; }
    bool Ready { get; }
    Slot[] Read(Bar bar);
    void Check(ulong owner, IReadOnlyList<Change> changes);
    void ApplyLocal(ulong owner, IReadOnlyList<Change> changes);
    string? WholeCopyCommand(Change target, IEnumerable<Bar> sources);
    void Send(string command);
    (string Name, uint Icon) Describe(Slot slot);
}
internal sealed unsafe class GameBars(IClientState client, ICondition condition, IDataManager data) : IGameBars
{
    public ulong Identity => Ready ? Module()->CharacterContentId : 0;
    public bool Ready => client.IsLoggedIn && RaptureHotbarModule.Instance() != null && RaptureHotbarModule.Instance()->ModuleReady;
    private RaptureHotbarModule* Module()
    {
        if (!Ready) throw new InvalidOperationException("NotReady");
        return RaptureHotbarModule.Instance();
    }
    private int Group(Bar b)
    {
        b.Validate();
        var m = Module();
        int index = b.Pvp ? m->GetPvPSavedHotbarIndexForClassJobId((uint)b.Job) : b.Job;
        if (index < 0 || index >= m->SavedHotbars.Length) throw new InvalidOperationException("InvalidJob");
        return index;
    }
    private bool Live(Bar b) => b.Pvp == Module()->PvPHotbarsActive && (Module()->IsHotbarShared((uint)b.Index) ? b.Job == 0 : b.Job == Module()->ActiveHotbarClassJobId);
    public Slot[] Read(Bar b)
    {
        var m = Module(); int group = Group(b); bool live = Live(b);
        var result = new Slot[b.Count];
        for (int i = 0; i < result.Length; i++)
        {
            if (live) { var s = m->Hotbars[b.Index].Slots[i]; result[i] = Normalize((byte)s.CommandType, s.CommandId); }
            else { var s = m->SavedHotbars[group].Hotbars[b.Index].Slots[i]; result[i] = Normalize((byte)s.CommandType, s.CommandId); }
        }
        return result;
    }
    private static Slot Normalize(byte type, uint id) => type == 0 ? Slot.Empty : new(type, id);
    public void Check(ulong owner, IReadOnlyList<Change> changes)
    {
        var m = Module();
        if (owner == 0 || owner != Identity) throw new InvalidOperationException("CharacterChanged");
        if (condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat] || condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas] || condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas51]) throw new InvalidOperationException("Busy");
        if (!m->DatFileLoadedSuccessfully) throw new InvalidOperationException("NotReady");
        foreach (var c in changes)
        {
            Group(c.Bar);
            if (c.Before.Length != c.Bar.Count || c.After.Length != c.Bar.Count) throw new InvalidOperationException("Invalid");
            if (!Read(c.Bar).SequenceEqual(c.Before)) throw new InvalidOperationException("Conflict");
            foreach (var s in c.After) if (!Supported(s)) throw new InvalidOperationException("Unsupported");
        }
    }
    // Persisted slot kinds only. Transient inventory drag payloads and unknown types are never installed.
    internal static bool Supported(Slot s) => s.Type is 0 or 1 or 2 or 4 or 6 or 7 or 8 or 9 or 10 or 11 or 12 or 13 or 15 or 16 or 17 or 18 or 20 or 21 or 22 or 24 or 25 or 26 or 27 or 29 or 30 or 31 or 32 or 33 or 34 or 35;
    public void ApplyLocal(ulong owner, IReadOnlyList<Change> changes)
    {
        Check(owner, changes);
        var m = Module();
        // A framework-thread batch. Write destinations and cleared sources in one callback; never execute a slot.
        BatchApplier.Apply(changes, Read, (bar, i, s) =>
            {
                int group = Group(bar); bool live = Live(bar);
                ref var saved = ref m->SavedHotbars[group].Hotbars[bar.Index].Slots[i];
                saved.CommandType = (HotbarSlotType)s.Type; saved.CommandId = s.Id;
                if (live) m->Hotbars[bar.Index].Slots[i].Set((HotbarSlotType)s.Type, s.Id);
            }, () => { m->HasChanges = true; m->SaveFile(false); });
    }
    public string? WholeCopyCommand(Change target, IEnumerable<Bar> sources)
    {
        if (target.Bar.Pvp != Module()->PvPHotbarsActive) return null;
        foreach (var source in sources)
        {
            if (source == target.Bar || source.Cross != target.Bar.Cross || source.Pvp != target.Bar.Pvp) continue;
            if (!Read(source).SequenceEqual(target.After)) continue;
            string? a = JobToken(source.Job), b = JobToken(target.Bar.Job);
            if (a != null && b != null) return $"/{(source.Pvp ? "pvp" : "")}{(source.Cross ? "crosshotbar" : "hotbar")} copy {a} {source.Number} {b} {target.Bar.Number}";
        }
        var different = Enumerable.Range(0, target.After.Length).Where(i => target.Before[i] != target.After[i]).ToArray();
        if (different.Length == 1 && !target.Bar.Cross && Live(target.Bar))
        {
            int index = different[0]; var slot = target.After[index];
            string prefix = target.Bar.Pvp ? "/pvphotbar" : "/hotbar";
            string? name = slot.Type switch {
                1 => data.GetExcelSheet<Lumina.Excel.Sheets.Action>().GetRow(slot.Id).Name.ToString(),
                9 => data.GetExcelSheet<Lumina.Excel.Sheets.CraftAction>().GetRow(slot.Id).Name.ToString(),
                10 => data.GetExcelSheet<Lumina.Excel.Sheets.GeneralAction>().GetRow(slot.Id).Name.ToString(),
                _ => null
            };
            if (name != null && name.Length > 0 && !name.Any(c => char.IsControl(c) || c == '"') && Encoding.UTF8.GetByteCount(name) < 300)
                return $"{prefix} {(slot.Type == 10 ? "general" : "set")} \"{name}\" {target.Bar.Number} {index + 1}";
        }
        return null;
    }
    private string? JobToken(int job)
    {
        if (job == 0) return "share";
        var value = data.GetExcelSheet<Lumina.Excel.Sheets.ClassJob>().GetRow((uint)job).Abbreviation.ToString();
        return value.Length is >= 3 and <= 4 && value.All(char.IsAsciiLetter) ? value : null;
    }
    public void Send(string command)
    {
        Module();
        var shell = RaptureShellModule.Instance();
        if (shell == null || shell->IsTextCommandUnavailable) throw new InvalidOperationException("Busy");
        var text = Utf8String.FromSequence(Encoding.UTF8.GetBytes(command));
        try { UIModule.Instance()->ProcessChatBoxEntry(text); } finally { text->Dtor(true); }
    }
    public (string Name, uint Icon) Describe(Slot s)
    {
        if (s.IsEmpty) return ("", 0);
        if (!Supported(s)) return ($"{s.Type}:{s.Id}", 0);
        Module(); HotbarSlot scratch = default;
        string name = scratch.GetDisplayNameForSlot((HotbarSlotType)s.Type, s.Id).ToString();
        int icon = scratch.GetIconIdForSlot((HotbarSlotType)s.Type, s.Id);
        return (name, (uint)Math.Max(0, icon));
    }
}
