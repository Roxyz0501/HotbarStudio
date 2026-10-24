using System.Numerics;
using System.Text.Json;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Interface.Textures;
using Roxyz0501.Dalamud.CommonUi;

namespace HotbarStudio;

public sealed class Plugin : IDalamudPlugin
{
    private readonly IDalamudPluginInterface pi;
    private readonly ICommandManager commands;
    private readonly IFramework framework;
    private readonly IClientState client;
    private readonly ITextureProvider textures;
    private readonly IPluginLog log;
    private readonly IGameBars game;
    private readonly ConfigurationStore settings;
    private LocalizedFonts fonts;
    private readonly HashSet<string> fontNames = [];
    private bool fontsDirty;
    private readonly CancellationTokenRegistration shutdown, unloading;
    private readonly Editor editor = new();
    private readonly Dictionary<Slot, (string Name, uint Icon)> descriptions = [];
    private readonly Dictionary<Bar, string> readErrors = [];
    private readonly (int Id, string Name)[] jobs;
    private Bar source = new(false, 0, false, 1), target = new(false, 0, false, 2);
    private readonly HashSet<int> selected = [];
    private bool open, move, disposed, requestApply, uncertain;
    private ulong owner;
    private string status = "", transaction = "";
    private (Bar Bar, int Slot)? drag;
    private IReadOnlyList<Change>? pending;
    private long deadline;
    private string? journalPath;
    private string historyDay = DateTime.UtcNow.ToString("yyyy-MM-dd");
    private string[] historyFiles = [];
    private bool historyDirty = true;
    private int historyPage;

    public Plugin(IDalamudPluginInterface pi, ICommandManager commands, IFramework framework, IClientState client, ICondition condition, IDataManager data, ITextureProvider textures, IPluginLog log, IGameLifecycle lifecycle)
    {
        this.pi = pi; this.commands = commands; this.framework = framework; this.client = client; this.textures = textures; this.log = log;
        game = new GameBars(client, condition, data);
        settings = new(pi.ConfigFile.FullName, (key, ex) => log.Warning(ex, "Settings: {Status}", key));
        if (settings.Value.Language == null)
        {
            string? code = null;
            try { code = client.ClientLanguage.ToString() switch { "Japanese" => "ja", "German" => "de", "French" => "fr", "English" => "en", _ => null }; } catch { }
            code ??= Loc.Normalize(pi.UiLanguage) ?? "en";
            settings.Edit(c => c.Language = code);
        }
        jobs = data.GetExcelSheet<Lumina.Excel.Sheets.ClassJob>().Where(j => j.RowId is > 0 and <= 42 && !string.IsNullOrWhiteSpace(j.Abbreviation.ToString())).Select(j => ((int)j.RowId, j.Name.ToString() + " (" + j.Abbreviation.ToString() + ")")).ToArray();
        fonts = new(pi.UiBuilder.FontAtlas, jobs.Select(j => j.Name));
        if (!commands.AddHandler("/hotbarstudio", new CommandInfo((_, _) => Open()) { HelpMessage = "Hotbar Studio" })) throw new InvalidOperationException("Command collision");
        pi.UiBuilder.Draw += Draw; pi.UiBuilder.OpenMainUi += Open; pi.UiBuilder.OpenConfigUi += Open;
        framework.Update += Update; client.Logout += Logout;
        shutdown = lifecycle.GameShuttingDownToken.Register(settings.Flush);
        unloading = lifecycle.DalamudUnloadingToken.Register(settings.Flush);
    }
    private string T(string key) => Loc.Get(settings.Value.Language, key);
    private void Open() => open = true;
    private void Logout(int type, int code) { settings.Flush(); Reset(); owner = 0; }
    private void Reset() { editor.Reset(); descriptions.Clear(); readErrors.Clear(); selected.Clear(); drag = null; requestApply = false; pending = null; uncertain = false; }
    private void Fail(Exception ex)
    {
        status = ex is InvalidOperationException ? ex.Message : "Error";
        log.Warning(ex, "Hotbar Studio operation stopped");
    }
    private void Update(IFramework _)
    {
        settings.Tick();
        if (fontsDirty)
        {
            fontsDirty = false;
            var next = new LocalizedFonts(pi.UiBuilder.FontAtlas, jobs.Select(j => j.Name).Concat(fontNames));
            fonts.Dispose(); fonts = next;
        }
        if (owner != 0 && owner != game.Identity) { Reset(); owner = 0; status = "CharacterChanged"; }
        if (requestApply)
        {
            requestApply = false;
            try
            {
                var changes = editor.Changes;
                if (changes.Count == 0) return;
                game.Check(owner, editor.Guards);
                game.Check(owner, changes);
                string? command = changes.Count == 1 ? game.WholeCopyCommand(changes[0], editor.Bars) : null;
                transaction = Guid.NewGuid().ToString("N");
                journalPath = Path.Combine(pi.GetPluginConfigDirectory(), "history", DateTime.UtcNow.ToString("yyyy-MM-dd"), transaction + ".jsonl");
                Journal(new { Event = "prepared", Id = transaction, Character = owner, TimeUtc = DateTime.UtcNow, Changes = changes, Command = command });
                uncertain = true; // Once any game write starts, never automatically repeat a failed batch.
                if (command != null) { game.Send(command); pending = changes; deadline = Environment.TickCount64 + 2500; status = "Applying"; }
                else { game.ApplyLocal(owner, changes); Complete(); }
            }
            catch (Exception ex) { Fail(ex); }
        }
        if (pending != null)
        {
            try
            {
                if (pending.All(c => game.Read(c.Bar).SequenceEqual(c.After))) Complete();
                else if (Environment.TickCount64 >= deadline) { pending = null; status = "VerifyFailed"; }
            }
            catch (Exception ex) { pending = null; Fail(ex); }
        }
    }
    private void Journal(object record)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(journalPath!)!);
        using var stream = new FileStream(journalPath!, FileMode.Append, FileAccess.Write, FileShare.Read);
        var bytes = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(record) + "\n");
        stream.Write(bytes); stream.Flush(true);
    }
    private void Complete()
    {
        pending = null;
        Journal(new { Event = "verified-memory", Id = transaction, TimeUtc = DateTime.UtcNow });
        historyDirty = true;
        Reset(); status = "Done";
    }
    private void Ensure(Bar bar)
    {
        if (owner == 0) owner = game.Identity;
        if (readErrors.TryGetValue(bar, out string? error)) throw new InvalidOperationException(error);
        if (!editor.Bars.Contains(bar))
        {
            try { editor.Load(bar, game.Read(bar)); }
            catch (Exception ex) { readErrors[bar] = ex is InvalidOperationException ? ex.Message : "Error"; throw; }
        }
    }
    private void Transfer(Bar a, int ai, Bar b, int bi, bool moving)
    {
        try { Ensure(a); Ensure(b); editor.Transfer(a, ai, b, bi, moving); status = ""; }
        catch (Exception ex) { Fail(ex); }
    }
    private void Draw()
    {
        if (!open || disposed) return;
        using var font = fonts.Push(settings.Value.Language);
        ImGui.SetNextWindowSizeConstraints(new Vector2(1020, 500), new Vector2(float.MaxValue));
        ImGui.SetNextWindowSize(new Vector2(1100, 760), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Hotbar Studio###HotbarStudio", ref open))
        {
            if (ImGui.BeginTabBar("Tabs"))
            {
                if (ImGui.BeginTabItem(T("Editor"))) { DrawEditor(); ImGui.EndTabItem(); }
                if (ImGui.BeginTabItem(T("History"))) { DrawHistory(); ImGui.EndTabItem(); }
                StandardPluginTabs.DrawStandardTrailingTabs(T("Settings"), "Settings", () => {
                    ImGui.TextWrapped(T("Policy"));
                    if (settings.Notice.Length > 0) ImGui.TextWrapped(T(settings.Notice));
                    if (ImGui.Button(T("Retry"))) settings.Retry();
                }, T("Language"), () => {
                    var code = StandardPluginTabs.DrawLanguageSelector(T("Language"), "", settings.Value.Language!);
                    if (code != null) settings.Edit(c => c.Language = code);
                }, T("Support"), () => StandardPluginTabs.DrawSupportContent("Hotbar Studio", T("Donate"), "Roxyz0501", "Ko-fi"));
                ImGui.EndTabBar();
            }
        }
        ImGui.End();
    }
    private void DrawHistory()
    {
        ImGui.TextWrapped(T("HistoryHelp"));
        if (ImGui.InputText("UTC (yyyy-MM-dd)", ref historyDay, 10)) { historyDirty = true; historyPage = 0; }
        if (ImGui.Button("<")) { historyPage = Math.Max(0, historyPage - 1); historyDirty = true; }
        ImGui.SameLine(); if (ImGui.Button(">")) { historyPage++; historyDirty = true; }
        ImGui.SameLine(); if (ImGui.Button(T("Refresh"))) historyDirty = true;
        if (!DateTime.TryParseExact(historyDay, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out _)) return;
        try
        {
            if (historyDirty)
            {
                string dir = Path.Combine(pi.GetPluginConfigDirectory(), "history", historyDay);
                historyFiles = Directory.Exists(dir) ? Directory.EnumerateFiles(dir, "*.jsonl").Skip(historyPage * 20).Take(20).ToArray() : [];
                historyDirty = false;
            }
            ImGui.BeginDisabled(!game.Ready || pending != null || requestApply || uncertain || editor.Changes.Count > 0);
            foreach (string file in historyFiles)
            {
                ImGui.PushID(file); ImGui.TextUnformatted(Path.GetFileNameWithoutExtension(file)); ImGui.SameLine();
                if (ImGui.Button(T("Restore")))
                {
                    try
                    {
                    if (new FileInfo(file).Length > 512 * 1024) throw new InvalidOperationException("Limit");
                    using var reader = File.OpenText(file);
                    using var json = JsonDocument.Parse(reader.ReadLine() ?? "");
                    if (json.RootElement.GetProperty("Character").GetUInt64() != game.Identity) throw new InvalidOperationException("CharacterChanged");
                    var changes = json.RootElement.GetProperty("Changes").Deserialize<Change[]>() ?? throw new InvalidOperationException("Invalid");
                    if (changes.Length > 128) throw new InvalidOperationException("Limit");
                    var snapshots = new Dictionary<Bar, Slot[]>();
                    foreach (var c in changes)
                    {
                        c.Bar.Validate();
                        if (c.Before.Length != c.Bar.Count || c.Before.Any(s => !GameBars.Supported(s))) throw new InvalidOperationException("Invalid");
                        snapshots.Add(c.Bar, game.Read(c.Bar));
                    }
                    Reset();
                    owner = game.Identity;
                    foreach (var c in changes) { editor.Load(c.Bar, snapshots[c.Bar]); editor.Restore(c.Bar, c.Before); }
                    status = "RestoredPreview";
                    }
                    catch (Exception ex) { Fail(ex); }
                }
                ImGui.PopID();
            }
            ImGui.EndDisabled();
        }
        catch (Exception ex) { historyDirty = false; Fail(ex); }
        if (status.Length > 0) ImGui.TextWrapped(T(status));
    }
    private void DrawEditor()
    {
        ImGui.TextWrapped(T("Help"));
        if (!game.Ready) { ImGui.TextWrapped(T("NotReady")); return; }
        ImGui.BeginDisabled(pending != null || requestApply || uncertain);
        if (ImGui.RadioButton(T("Copy"), !move)) move = false; ImGui.SameLine();
        if (ImGui.RadioButton(T("Move"), move)) move = true;
        if (ImGui.BeginTable("Sides", 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV))
        {
            ImGui.TableNextColumn(); ImGui.PushID("source"); ImGui.TextUnformatted(T("Source")); Selector(ref source); Preview(source); ImGui.PopID();
            ImGui.TableNextColumn(); ImGui.PushID("target"); ImGui.TextUnformatted(T("Target"));
            var old = target; Selector(ref target); if (old.Pvp != target.Pvp || old.Job != target.Job || old.Cross != target.Cross) selected.Clear();
            Preview(target); ImGui.PopID(); ImGui.EndTable();
        }
        if (ImGui.Button(T(move ? "MoveBar" : "CopyBar"))) Transfer(source, -1, target, -1, move);
        ImGui.SameLine(); if (ImGui.Button(T("Undo"))) editor.Undo();
        for (int i = 1; i <= (target.Cross ? 8 : 10); i++)
        {
            bool value = selected.Contains(i);
            if (i > 1) ImGui.SameLine();
            if (ImGui.Checkbox(i.ToString(), ref value)) { if (value) selected.Add(i); else selected.Remove(i); }
        }
        if (ImGui.Button(T("Batch"))) foreach (int n in selected.Order()) Transfer(source, -1, target with { Number = n }, -1, false);
        foreach (int n in selected.Order())
        {
            ImGui.PushID(1000 + n); Preview(target with { Number = n }); ImGui.PopID();
        }
        int count = editor.Changes.Count;
        ImGui.Separator(); ImGui.TextUnformatted($"{T("Pending")}: {count}");
        ImGui.BeginDisabled(count == 0);
        if (ImGui.Button(T("Apply"))) requestApply = true;
        ImGui.EndDisabled(); ImGui.EndDisabled();
        ImGui.SameLine(); ImGui.BeginDisabled(pending != null || requestApply);
        if (ImGui.Button(T("Reset"))) { Reset(); status = ""; }
        ImGui.EndDisabled();
        if (status.Length > 0) ImGui.TextWrapped(T(status));
    }
    private void Selector(ref Bar b)
    {
        bool pvp = b.Pvp, cross = b.Cross;
        if (ImGui.Checkbox("PvP", ref pvp)) b = b with { Pvp = pvp };
        ImGui.SameLine(); if (ImGui.Checkbox(T("Cross"), ref cross)) b = b with { Cross = cross, Number = Math.Min(b.Number, cross ? 8 : 10) };
        int jobId = b.Job;
        string label = b.Job == 0 ? T("Shared") : jobs.FirstOrDefault(x => x.Id == jobId).Name ?? b.Job.ToString();
        ImGui.SetNextItemWidth(260);
        if (ImGui.BeginCombo("##job", label))
        {
            if (ImGui.Selectable(T("Shared"), b.Job == 0)) b = b with { Job = 0 };
            foreach (var job in jobs) if (ImGui.Selectable(job.Name, b.Job == job.Id)) b = b with { Job = job.Id };
            ImGui.EndCombo();
        }
        int number = b.Number; ImGui.SetNextItemWidth(160);
        if (ImGui.SliderInt(T("Bar"), ref number, 1, b.Cross ? 8 : 10)) b = b with { Number = number };
    }
    private void Preview(Bar bar)
    {
        try
        {
            Ensure(bar);
            ImGui.Button($"{(bar.Pvp ? "PvP" : "PvE")} / {T(bar.Cross ? "Cross" : "Normal")} {bar.Number}###bar", new Vector2(0, 28));
            DragDrop(bar, -1);
            var slots = editor.View(bar);
            if (bar.Cross)
            {
                var start = ImGui.GetCursorPos();
                // Native order: left face buttons, left D-pad, right face buttons, right D-pad.
                for (int i = 0; i < 16; i++)
                {
                    int group = i / 4, index = i % 4;
                    Vector2[] positions = [new(1, 2), new(2, 1), new(0, 1), new(1, 0)];
                    ImGui.SetCursorPos(start + new Vector2(group * 114, 0) + positions[index] * 34);
                    SlotButton(bar, i, slots[i]);
                }
                ImGui.SetCursorPos(start + new Vector2(0, 110)); ImGui.Dummy(new Vector2(450, 1));
            }
            else for (int i = 0; i < slots.Length; i++) { if (i % 6 != 0) ImGui.SameLine(); SlotButton(bar, i, slots[i]); }
        }
        catch (Exception ex) { ImGui.TextWrapped(T(ex is InvalidOperationException ? ex.Message : "Error")); }
    }
    private void SlotButton(Bar bar, int i, Slot slot)
    {
        if (!descriptions.TryGetValue(slot, out var info))
        {
            try { info = game.Describe(slot); }
            catch { info = ($"{slot.Type}:{slot.Id}", 0); }
            if (fontNames.Count < 4096 && fontNames.Add(info.Name)) fontsDirty = true;
            if (descriptions.Count < 4096) descriptions[slot] = info;
        }
        var texture = info.Icon == 0 ? null : textures.GetFromGameIcon(new GameIconLookup(info.Icon)).GetWrapOrDefault();
        ImGui.PushID(i);
        bool changed = editor.Changed(bar, i);
        if (changed) ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.15f, 0.48f, 0.52f, 1));
        if (texture != null) ImGui.ImageButton(texture.Handle, new Vector2(30));
        else ImGui.Button(slot.IsEmpty ? "-" : (i + 1).ToString(), new Vector2(36));
        if (changed) ImGui.PopStyleColor();
        if (ImGui.IsItemHovered()) ImGui.SetTooltip($"{i + 1}: {(slot.IsEmpty ? T("Empty") : info.Name)}");
        DragDrop(bar, i); ImGui.PopID();
    }
    private unsafe void DragDrop(Bar bar, int slot)
    {
        string kind = slot == -1 ? "HS_BAR" : "HS_SLOT";
        if (ImGui.BeginDragDropSource())
        {
            drag = (bar, slot); int value = 1; ImGui.SetDragDropPayload(kind, new ReadOnlySpan<byte>(&value, sizeof(int)));
            ImGui.TextUnformatted(T(move ? "Move" : "Copy")); ImGui.EndDragDropSource();
        }
        if (ImGui.BeginDragDropTarget())
        {
            var payload = ImGui.AcceptDragDropPayload(kind);
            if (payload.Handle != null && payload.IsDelivery() && drag is { } from) Transfer(from.Bar, from.Slot, bar, slot, move);
            ImGui.EndDragDropTarget();
        }
    }
    public void Dispose()
    {
        if (disposed) return; disposed = true;
        framework.Update -= Update; client.Logout -= Logout;
        pi.UiBuilder.Draw -= Draw; pi.UiBuilder.OpenMainUi -= Open; pi.UiBuilder.OpenConfigUi -= Open;
        commands.RemoveHandler("/hotbarstudio"); shutdown.Dispose(); unloading.Dispose();
        settings.Dispose(); fonts.Dispose(); Reset();
    }
}
