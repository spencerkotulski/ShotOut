using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
namespace ShotOut {
public class RecoveryLayer {public Mark Data;public string Image;}
public class RecoveryTab {public string Name,Background;public Rectangle CanvasBase;public bool Selected;public List<RecoveryLayer> Layers=new List<RecoveryLayer>();}
public class RecoveryManifest {public int Version=1;public List<RecoveryTab> Tabs=new List<RecoveryTab>();}
sealed class RecoveryBlob {public string Hash;}
sealed class PendingRecoveryTab : IDisposable {public RecoveryTab Data;public Bitmap Background;public RecoveryBlob Cache;public List<byte[]> Images=new List<byte[]>();public void Dispose(){if(Background!=null)Background.Dispose();}}
sealed class RecoveredSession : IDisposable {public string Root;public FileStream Lease;public List<Tuple<RecoveryTab,Project>> Tabs=new List<Tuple<RecoveryTab,Project>>();public void Dispose(){if(Lease!=null){Lease.Dispose();Lease=null;}}}
sealed class RecoveryStore {
    public readonly string Root;
    readonly FileStream lease;
    readonly ConditionalWeakTable<Bitmap,RecoveryBlob> backgrounds=new ConditionalWeakTable<Bitmap,RecoveryBlob>();
    readonly ConditionalWeakTable<byte[],RecoveryBlob> images=new ConditionalWeakTable<byte[],RecoveryBlob>();
    public RecoveryStore(string parent){Directory.CreateDirectory(parent);Root=Path.Combine(Path.GetFullPath(parent),"session-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(Root);lease=new FileStream(Path.Combine(Root,"active.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}
    internal PendingRecoveryTab Capture(EditorDocument document,bool selected){
        RecoveryBlob cached=backgrounds.GetValue(document.Image,key=>new RecoveryBlob());var tab=new PendingRecoveryTab {Data=new RecoveryTab {Name=document.Name,CanvasBase=document.CanvasBase,Selected=selected},Cache=cached};
        if(cached.Hash==null||!File.Exists(Path.Combine(Root,cached.Hash+".png")))tab.Background=(Bitmap)document.Image.Clone();else tab.Data.Background=cached.Hash;
        try{foreach(var mark in document.Marks){var copy=mark.Clone();tab.Images.Add(copy.Image);copy.Image=null;tab.Data.Layers.Add(new RecoveryLayer {Data=copy});}return tab;}catch{tab.Dispose();throw;}
    }
    string PutBlob(byte[] bytes){var cached=images.GetValue(bytes,key=>new RecoveryBlob());if(cached.Hash==null)using(var hash=SHA256.Create())cached.Hash=BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();string target=Path.Combine(Root,cached.Hash+".png");if(!File.Exists(target)){string temp=target+".tmp";File.WriteAllBytes(temp,bytes);File.Move(temp,target);}return cached.Hash;}
    public void Write(List<PendingRecoveryTab> pending){
        try{var manifest=new RecoveryManifest();foreach(var tab in pending){if(tab.Background!=null){tab.Data.Background=PutBlob(Art.Bytes(tab.Background));tab.Cache.Hash=tab.Data.Background;}for(int i=0;i<tab.Data.Layers.Count;i++)if(tab.Images[i]!=null)tab.Data.Layers[i].Image=PutBlob(tab.Images[i]);manifest.Tabs.Add(tab.Data);}
            string file=Path.Combine(Root,"session.xml"),temp=file+".tmp";using(var stream=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)){new XmlSerializer(typeof(RecoveryManifest)).Serialize(stream,manifest);stream.Flush(true);}if(File.Exists(file))File.Replace(temp,file,file+".bak");else File.Move(temp,file);
            var keep=BlobReferences(manifest);try{if(File.Exists(file+".bak"))keep.UnionWith(BlobReferences(ReadManifest(file+".bak")));}catch{return;}foreach(string blob in Directory.GetFiles(Root,"*.png")){string id=Path.GetFileNameWithoutExtension(blob);if(Regex.IsMatch(id,"^[a-f0-9]{64}$")&&!keep.Contains(id))File.Delete(blob);}
        }finally{foreach(var tab in pending)tab.Dispose();}
    }
    static HashSet<string> BlobReferences(RecoveryManifest manifest){return new HashSet<string>(manifest.Tabs.SelectMany(tab=>new[]{tab.Background}.Concat(tab.Layers.Select(layer=>layer.Image))).Where(id=>id!=null));}
    static RecoveryManifest ReadManifest(string path){using(var reader=XmlReader.Create(path,new XmlReaderSettings {DtdProcessing=DtdProcessing.Prohibit,MaxCharactersInDocument=64000000})){var manifest=(RecoveryManifest)new XmlSerializer(typeof(RecoveryManifest)).Deserialize(reader);if(manifest.Version!=1||manifest.Tabs==null||manifest.Tabs.Count>256)throw new InvalidDataException("Unsupported recovery data.");return manifest;}}
    static byte[] ReadBlob(string root,string id){if(id==null||!Regex.IsMatch(id,"^[a-f0-9]{64}$"))throw new InvalidDataException("Invalid recovery image reference.");byte[] bytes=File.ReadAllBytes(Path.Combine(root,id+".png"));using(var hash=SHA256.Create())if(BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant()!=id)throw new InvalidDataException("Recovery image is incomplete.");return bytes;}
    static List<Tuple<RecoveryTab,Project>> ReadTabs(string root,string file){var manifest=ReadManifest(file);var tabs=new List<Tuple<RecoveryTab,Project>>();var blobs=new Dictionary<string,byte[]>();Func<string,byte[]> blob=id=>{byte[] bytes;if(!blobs.TryGetValue(id,out bytes)){bytes=ReadBlob(root,id);blobs.Add(id,bytes);}return bytes;};foreach(var tab in manifest.Tabs){var project=new Project {Background=blob(tab.Background),CanvasBase=tab.CanvasBase};using(var image=Art.FromBytes(project.Background))if(image.Width>16000||image.Height>16000||(long)image.Width*image.Height>40000000)throw new InvalidDataException("Recovery canvas exceeds image limits.");if(tab.Layers==null)throw new InvalidDataException("Missing recovery layers.");foreach(var layer in tab.Layers){if(layer.Data==null)throw new InvalidDataException("Missing recovery layer.");var mark=layer.Data.Clone();mark.Image=layer.Image==null?null:blob(layer.Image);project.Marks.Add(mark);}tabs.Add(Tuple.Create(tab,project));}return tabs;}
    public static List<RecoveredSession> Find(string parent,string exclude,out int skipped){
        skipped=0;var result=new List<RecoveredSession>();if(!Directory.Exists(parent))return result;foreach(string root in Directory.GetDirectories(parent,"session-*")){if(String.Equals(root,exclude,StringComparison.OrdinalIgnoreCase)||!Regex.IsMatch(Path.GetFileName(root),"^session-[a-f0-9]{32}$"))continue;var session=new RecoveredSession {Root=root};try{session.Lease=new FileStream(Path.Combine(root,"active.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}catch(IOException){continue;}catch(UnauthorizedAccessException){skipped++;continue;}if(File.Exists(Path.Combine(root,"clean.exit"))){session.Dispose();try{DeleteSession(root);}catch(IOException){}catch(UnauthorizedAccessException){}continue;}if(!File.Exists(Path.Combine(root,"session.xml"))&&!File.Exists(Path.Combine(root,"session.xml.bak"))){session.Dispose();continue;}try{try{session.Tabs=ReadTabs(root,Path.Combine(root,"session.xml"));}catch{session.Tabs=ReadTabs(root,Path.Combine(root,"session.xml.bak"));}result.Add(session);}catch{session.Dispose();skipped++;}}return result;
    }
    public static void DeleteSession(string root){string full=Path.GetFullPath(root);if(!Regex.IsMatch(Path.GetFileName(full),"^session-[a-f0-9]{32}$"))throw new InvalidOperationException("Refusing to remove an unowned recovery directory.");string parent=Path.GetFullPath(Path.GetDirectoryName(full));if(!full.StartsWith(parent+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Recovery directory is outside its parent.");if(Directory.Exists(full))Directory.Delete(full,true);}
    public static void MarkClean(string root){File.WriteAllText(Path.Combine(root,"clean.exit"),"clean");}
    public void Close(bool clean){try{if(clean)MarkClean(Root);}finally{lease.Dispose();}if(clean)DeleteSession(Root);}
}
partial class Editor {
    RecoveryStore recoveryStore;
    Timer recoveryTimer;
    Task recoveryWrite;
    long recoveryRevision,recoverySavedRevision;
    DateTime recoveryTouched,recoveryLastSaved;
    List<RecoveredSession> recoveredSessions=new List<RecoveredSession>();
    static string RecoveryFolder {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"ShotOut","Recovery");}}
    void TouchRecovery(){recoveryRevision++;recoveryTouched=DateTime.UtcNow;}
    void ApplyRecoveryPreference(){if(Preferences.Current.AutoRecover)InitializeRecovery();else FinishRecovery(true);}
    void InitializeRecovery(){InitializeRecoveryCore(RecoveryFolder,false);}
    void InitializeRecoveryCore(string folder,bool test){
        if((Preferences.Testing&&!test)||!Preferences.Current.AutoRecover||recoveryStore!=null)return;
        try{recoveryStore=new RecoveryStore(folder);int skipped;recoveredSessions=RecoveryStore.Find(folder,recoveryStore.Root,out skipped);int restored=0,selected=-1;foreach(var session in recoveredSessions)foreach(var pair in session.Tabs){var tab=pair.Item1;var project=pair.Item2;AddDocument(Art.FromBytes(project.Background),"Recovered · "+tab.Name,true);ReadCanvasBase(project.CanvasBase);Marks=project.Marks;dirty=true;step=Marks.Count(mark=>mark.Kind=="Step")+1;if(tab.Selected)selected=currentDocument;RefreshImage();StoreDocument();restored++;}if(selected>=0)ActivateDocument(selected);if(restored>0){captureSerial=Math.Max(captureSerial,documents.Count);UpdateStatus("Recovered "+restored+" unsaved tabs.");}if(skipped>0)UpdateStatus("Some recovery files could not be opened; they were kept for a future retry.");
            recoveryTimer=new Timer {Interval=3000};recoveryTimer.Tick+=delegate{QueueRecovery();};recoveryTimer.Start();Disposed+=delegate{FinishRecovery(false);};TouchRecovery();
        }catch(Exception ex){UpdateStatus("Recovery could not start: "+ex.Message);FinishRecovery(false);}
    }
    void ReleaseRecovered(bool remove){foreach(var session in recoveredSessions){if(remove)try{RecoveryStore.MarkClean(session.Root);}catch(IOException){}catch(UnauthorizedAccessException){}session.Dispose();if(remove)try{RecoveryStore.DeleteSession(session.Root);}catch(IOException){}catch(UnauthorizedAccessException){}}recoveredSessions.Clear();}
    void QueueRecovery(){
        if(recoveryStore==null||clonePainting||strokeAdjusting||moving||resizing||dragging||cropGesture||panning||marqueeDragging)return;
        if(recoveryWrite!=null){if(!recoveryWrite.IsCompleted)return;if(recoveryWrite.IsFaulted){UpdateStatus("Autosave could not finish. Your open tabs are still available; recovery will retry.");recoverySavedRevision=-1;var ignored=recoveryWrite.Exception;}else ReleaseRecovered(true);recoveryWrite=null;}
        if(recoveryRevision==recoverySavedRevision||DateTime.UtcNow-recoveryTouched<TimeSpan.FromSeconds(1.5)||DateTime.UtcNow-recoveryLastSaved<TimeSpan.FromSeconds(15))return;
        var pending=new List<PendingRecoveryTab>();try{StoreDocument();foreach(var document in documents)if(document.Dirty&&!document.Welcome)pending.Add(recoveryStore.Capture(document,Object.ReferenceEquals(document,documents[currentDocument])));var store=recoveryStore;recoverySavedRevision=recoveryRevision;recoveryLastSaved=DateTime.UtcNow;recoveryWrite=Task.Factory.StartNew(delegate{store.Write(pending);});}catch{foreach(var tab in pending)tab.Dispose();UpdateStatus("Autosave could not start; it will retry.");}
    }
    void FinishRecovery(bool clean){
        if(recoveryTimer!=null){recoveryTimer.Stop();recoveryTimer.Dispose();recoveryTimer=null;}if(recoveryStore==null)return;var store=recoveryStore;recoveryStore=null;var old=recoveredSessions;recoveredSessions=new List<RecoveredSession>();if(clean){try{RecoveryStore.MarkClean(store.Root);foreach(var session in old)RecoveryStore.MarkClean(session.Root);}catch(IOException){}catch(UnauthorizedAccessException){}}Action finish=delegate{try{store.Close(clean);}catch(IOException){}catch(UnauthorizedAccessException){}finally{foreach(var session in old){session.Dispose();if(clean)try{RecoveryStore.DeleteSession(session.Root);}catch(IOException){}catch(UnauthorizedAccessException){}}}};if(recoveryWrite!=null&&!recoveryWrite.IsCompleted)recoveryWrite.ContinueWith(task=>{var ignored=task.Exception;finish();});else{if(recoveryWrite!=null){var ignored=recoveryWrite.Exception;}finish();}recoveryWrite=null;
    }
    public void VerifyRecovery(List<string> report,string folder){
        Action<bool,string> check=delegate(bool ok,string label){if(!ok)throw new Exception(label);report.Add("PASS "+label);};string parent=Path.Combine(folder,"isolated-recovery");var store=new RecoveryStore(parent);var doc=new EditorDocument {Image=Blank(200,120),Name="Unsaved capture",Dirty=true,CanvasBase=new Rectangle(0,0,200,120)};using(var image=Blank(60,40)){var bytes=Art.Bytes(image);doc.Marks.Add(new Mark {Kind="Image",Image=bytes,W=60,H=40,Orientation=1});doc.Marks.Add(new Mark {Kind="Image",Image=bytes,X=70,W=60,H=40});}doc.Marks.Add(new Mark {Kind="Grayscale",Name="Regional adjustment",X=10,Y=10,W=50,H=30,FillOpacity=40});
        store.Write(new List<PendingRecoveryTab>{store.Capture(doc,true)});int blobs=Directory.GetFiles(store.Root,"*.png").Length;check(blobs==2,"Recovery deduplicates repeated image layers into shared PNG blobs");var pending=store.Capture(doc,true);check(pending.Background==null,"Unchanged canvas is not copied or encoded again for recovery");doc.Marks[2].X=20;pending.Data.Layers[2].Data.X=20;store.Write(new List<PendingRecoveryTab>{pending});check(Directory.GetFiles(store.Root,"*.png").Length==blobs,"Metadata edits reuse existing recovery image files");int skipped;var live=RecoveryStore.Find(parent,null,out skipped);check(live.Count==0,"Recovery ignores a running instance with an active session lock");string root=store.Root;store.Close(false);var sessions=RecoveryStore.Find(parent,null,out skipped);check(sessions.Count==1&&sessions[0].Tabs[0].Item2.Marks.Count==3,"Crash recovery restores screenshot and editable layers");var project=sessions[0].Tabs[0].Item2;check(project.Marks[0].Orientation==1&&project.Marks[2].Kind=="Grayscale"&&project.Marks[2].FillOpacity==40&&project.Marks[2].X==20,"Recovery preserves transforms and adjustment properties");sessions[0].Dispose();File.WriteAllText(Path.Combine(root,"session.xml"),"incomplete snapshot");sessions=RecoveryStore.Find(parent,null,out skipped);check(sessions.Count==1&&sessions[0].Tabs[0].Item2.Marks[2].X==10,"Incomplete latest snapshot falls back to previous atomic manifest");sessions[0].Dispose();RecoveryStore.DeleteSession(root);check(!Directory.Exists(root),"Clean recovery cleanup removes only its own session directory");var clean=new RecoveryStore(parent);clean.Close(true);check(!Directory.Exists(clean.Root),"Clean app exit leaves no session to recover");var interruptedExit=new RecoveryStore(parent);interruptedExit.Write(new List<PendingRecoveryTab>{interruptedExit.Capture(doc,true)});string cleanRoot=interruptedExit.Root;RecoveryStore.MarkClean(cleanRoot);interruptedExit.Close(false);var discarded=RecoveryStore.Find(parent,null,out skipped);check(discarded.Count==0&&!Directory.Exists(cleanRoot),"Clean-exit marker prevents unwanted recovery if shutdown interrupts cleanup");doc.Image.Dispose();
        using(var writer=new Editor(null)){writer.InitializeRecoveryCore(parent,true);writer.LoadImageDocument(Blank(180,120),"Autosaved tab");writer.Push();writer.Marks.Add(new Mark {Kind="Text",X=10,Y=10,W=80,H=40,Text="Recovered edit",Orientation=1});writer.AddDocument(Blank(90,70),"Second unsaved tab",true,true);writer.recoveryTouched=DateTime.UtcNow.AddSeconds(-3);writer.QueueRecovery();check(writer.recoveryWrite!=null&&writer.recoveryWrite.Wait(15000)&&!writer.recoveryWrite.IsFaulted,"Editor schedules recovery writing on a background task");writer.FinishRecovery(false);}
        using(var reader=new Editor(null)){reader.InitializeRecoveryCore(parent,true);check(reader.DocumentCount==2&&reader.documents.Any(tab=>tab.Marks.Any(mark=>mark.Text=="Recovered edit")),"Restart restores all unsaved tabs and editable text into the editor");check(reader.documents.All(tab=>tab.Dirty),"Recovered tabs remain marked unsaved");reader.FinishRecovery(true);}
        check(new Preferences().AutoRecover,"Automatic recovery is enabled by default");
    }
}
}
