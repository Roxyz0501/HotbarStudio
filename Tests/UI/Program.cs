using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using HotbarStudio;

unsafe class Program
{
 static void Main(string[] args)
 {
    string output = Path.GetFullPath(args[0]); Directory.CreateDirectory(output); Dummy.DirectoryPath = Path.Combine(output, "config-" + Guid.NewGuid().ToString("N"));
    ImGui.CreateContext(); var io = ImGui.GetIO(); io.DisplaySize = new(1440,900); io.DeltaTime = 1f/60; io.ConfigInputTrickleEventQueue = false;
    var pi = DispatchProxy.Create<IDalamudPluginInterface,Dummy>();
    var plugin = (Plugin)RuntimeHelpers.GetUninitializedObject(typeof(Plugin));
    var type = typeof(Plugin);
    void Set(string name, object? value) => type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(plugin,value);
    object? Invoke(string name, params object?[] parameters) => type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(plugin,parameters);
    var editor = new Editor(); var game = new FakeGame();
    var settings = new ConfigurationStore(Path.Combine(output,"settings.json")); settings.Edit(c=>c.Language="ja");
    var fonts = new LocalizedFonts(DispatchProxy.Create<IFontAtlas,Dummy>(), ["Paladin", "ナイト"]);
    Set("pi",pi); Set("editor",editor); Set("game",game); Set("settings",settings); Set("fonts",fonts);
    Set("textures",DispatchProxy.Create<ITextureProvider,Dummy>()); Set("log",DispatchProxy.Create<IPluginLog,Dummy>());
    Set("jobs", new (int Id,string Name)[] { (19,"ナイト (PLD)"),(24,"白魔道士 (WHM)") });
    Set("fontNames", new HashSet<string>()); Set("readErrors", new Dictionary<Bar,string>()); Set("descriptions",new Dictionary<Slot,(string Name,uint Icon)>()); Set("selected",new HashSet<int>());
    Set("source",new Bar(false,19,false,1)); Set("target",new Bar(false,0,false,2));
    Set("status",""); Set("transaction",""); Set("open",true); Set("historyDay","2026-10-24"); Set("historyFiles",Array.Empty<string>());
    io.Fonts.Build();
    int checks=0;
    void Check(bool condition,string label) { checks++; if(!condition) throw new Exception(label); }
    foreach(var f in Dummy.Fonts) foreach(char c in Loc.FontText.Distinct().Where(c=>!char.IsControl(c))) Check(f.FindGlyphNoFallback(c)!=null,"Glyph "+c);
    var textures = new Dictionary<ulong,(nint,int,int)>();
    for(int i=0;i<io.Fonts.Textures.Size;i++) { byte* pixels; int w=0,h=0,bpp=0; io.Fonts.GetTexDataAsRGBA32(i,&pixels,&w,&h,&bpp); io.Fonts.SetTexID(i,new ImTextureID((ulong)i+1)); textures[(ulong)i+1]=((nint)pixels,w,h); }
    void Frame() { ImGui.NewFrame(); Invoke("Draw"); ImGui.Render(); }
    Frame(); Frame(); int reads=game.Reads;
    foreach(var code in Loc.Codes) { settings.Edit(c=>c.Language=code); Frame(); Frame(); Renderer.Render(ImGui.GetDrawData(),textures,Path.Combine(output,code+".bmp")); Check(ImGui.GetDrawData().TotalVtxCount>0,"Render "+code); }
    Check(game.Reads==reads,"Unchanged preview does not reread hotbars");
    settings.Edit(c=>c.Language="ja"); Frame();
    // Actual mouse drag from source slot 1 into destination slot 1 in the rendered production window.
    io.AddMousePosEvent(86,320); Frame(); io.AddMouseButtonEvent(0,true); Frame(); io.AddMousePosEvent(110,320); Frame(); io.AddMousePosEvent(630,320); Frame(); Frame(); io.AddMouseButtonEvent(0,false); Frame(); Frame();
    Check(editor.Changes.Count==1,"Native ImGui drag delivered a slot copy");
    editor.Reset(); Frame();
    // Also exercise the production transfer handler independently of fixed pixel coordinates.
    Invoke("Transfer",new Bar(false,19,false,1),0,new Bar(false,0,false,2),0,true);
    Check(editor.Changes.Count==2,"Production slot move stages two bars");
    Frame(); Renderer.Render(ImGui.GetDrawData(),textures,Path.Combine(output,"staged.bmp"));
    Set("fontsDirty",false); Set("requestApply",true); Invoke("Update",new object?[]{null});
    Check(game.Writes==2 && editor.Changes.Count==0,"Apply verified and staging reset");
    Check(Directory.EnumerateFiles(Dummy.DirectoryPath,"*.jsonl",SearchOption.AllDirectories).Count()==1,"Journal persisted once per operation");
    Set("source",new Bar(false,19,true,1)); Set("target",new Bar(false,0,true,2)); Frame(); Frame();
    Renderer.Render(ImGui.GetDrawData(),textures,Path.Combine(output,"cross.bmp"));
    Check(game.Sent==0,"Local move does not send commands or packets");
    Invoke("Transfer",new Bar(false,19,true,1),-1,new Bar(false,0,true,2),-1,false);
    game.CommandMode = true; game.CommandSuccess = true; Set("fontsDirty",false); Set("requestApply",true); Invoke("Update",new object?[]{null});
    Check(game.Sent==1 && editor.Changes.Count==0,"Command copy verified before completion");
    Invoke("Transfer",new Bar(false,19,true,1),-1,new Bar(false,0,true,3),-1,false);
    game.CommandSuccess = false; Set("fontsDirty",false); Set("requestApply",true); Invoke("Update",new object?[]{null});
    Set("deadline",0L); Invoke("Update",new object?[]{null});
    Check(game.Sent==2 && editor.Changes.Count==1,"Failed command retains preview without fallback or retry");
    for(int i=0;i<10;i++) Invoke("Update",new object?[]{null});
    Check(game.Sent==2,"Uncertain command never retried");
    settings.Dispose(); fonts.Dispose(); ImGui.DestroyContext(); foreach(var pin in Dummy.Pins) pin.Free();
    Console.WriteLine($"PASS {checks} glyph, rendering and production-flow checks.");
 }
}
internal sealed class FakeGame : IGameBars
{
 public ulong Identity => 123;
 public bool Ready => true;
 public int Reads,Writes,Sent;
 public bool CommandMode,CommandSuccess;
 private Change? commandChange;
 private readonly Dictionary<Bar,Slot[]> bars=[];
 public Slot[] Read(Bar b) { Reads++; if(!bars.TryGetValue(b,out var s)) bars[b]=s=Enumerable.Range(0,b.Count).Select(i=>new Slot(1,(uint)(100+b.Job*10+i))).ToArray(); return s.ToArray(); }
 public void Check(ulong owner,IReadOnlyList<Change> changes) { foreach(var c in changes) if(!Read(c.Bar).SequenceEqual(c.Before)) throw new InvalidOperationException("Conflict"); }
 public void ApplyLocal(ulong owner,IReadOnlyList<Change> changes) => BatchApplier.Apply(changes,Read,(b,i,s)=>{bars[b][i]=s;Writes++;},()=>{});
 public string? WholeCopyCommand(Change target,IEnumerable<Bar> sources) { commandChange=target; return CommandMode?"/crosshotbar copy PLD 1 share 2":null; }
 public void Send(string command) { Sent++; if(CommandSuccess && commandChange is {} c) bars[c.Bar]=c.After.ToArray(); }
 public (string Name,uint Icon) Describe(Slot slot)=>("Action "+slot.Id,0);
}
public unsafe class Dummy : DispatchProxy
{
 public static string DirectoryPath="";
 public static List<ImFontPtr> Fonts=[];
 public static List<GCHandle> Pins=[];
 public ImFontPtr Font;
 protected override object? Invoke(MethodInfo? method,object?[]? args)
 {
  switch(method!.Name)
  {
   case "GetPluginConfigDirectory": return DirectoryPath;
   case "get_BuildStep": return FontAtlasBuildStep.PreBuild;
   case "NewDelegateFontHandle":
    var toolkit=DispatchProxy.Create<IFontAtlasBuildToolkitPreBuild,Dummy>(); ((FontAtlasBuildStepDelegate)args![0]!)(toolkit);
    var handle=DispatchProxy.Create<IFontHandle,Dummy>(); ((Dummy)(object)handle).Font=Fonts.Last(); return handle;
   case "AddDalamudAssetFont":
    var safe=(SafeFontConfig)args![1]!; var pin=GCHandle.Alloc(safe.GlyphRanges,GCHandleType.Pinned); Pins.Add(pin);
    var native=ImGui.ImFontConfig(); native.FontDataOwnedByAtlas=true; native.OversampleH=2; native.OversampleV=1; native.RasterizerMultiply=1; native.GlyphMaxAdvanceX=float.MaxValue; native.FontNo=safe.FontNo;
    var path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"XIVLauncher/dalamudAssets/dev/UIRes/NotoSansCJK-Regular.ttc");
    var font=ImGui.GetIO().Fonts.AddFontFromFileTTF(path,safe.SizePx,native,(ushort*)pin.AddrOfPinnedObject()); native.Destroy(); Fonts.Add(font); return font;
   case "Push": ImGui.PushFont(Font); return new Pop();
  }
  return method.ReturnType==typeof(void)?null:method.ReturnType.IsValueType?Activator.CreateInstance(method.ReturnType):null;
 }
 private sealed class Pop : IDisposable { public void Dispose()=>ImGui.PopFont(); }
}




