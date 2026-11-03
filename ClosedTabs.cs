using System;using System.Collections.Generic;using System.Drawing;using System.IO;using System.Linq;using System.Windows.Forms;using System.Xml.Serialization;
namespace ShotOut {
public class ClosedTabEntry {public string Name,Snapshot,ImagePath,ProjectPath;public bool Dirty;public DateTime ClosedUtc;}
partial class Editor {
    List<ClosedTabEntry> closedTabs;string closedTabsIndex;
    List<ClosedTabEntry> ClosedTabs(){
        string index=Path.Combine(Preferences.Current.ProjectFolder,"Recently closed tabs","History.xml");
        if(closedTabs!=null&&closedTabsIndex==index)return closedTabs;closedTabsIndex=index;closedTabs=new List<ClosedTabEntry>();
        if(!Preferences.Testing&&File.Exists(index))try{using(var stream=File.OpenRead(index))closedTabs=(List<ClosedTabEntry>)new XmlSerializer(typeof(List<ClosedTabEntry>)).Deserialize(stream);}catch(Exception ex){UpdateStatus("Could not read recently closed tabs: "+ex.Message);}
        closedTabs=closedTabs.Where(item=>File.Exists(item.Snapshot)).Take(20).ToList();return closedTabs;
    }
    void SaveClosedTabs(){if(Preferences.Testing)return;Directory.CreateDirectory(Path.GetDirectoryName(closedTabsIndex));WriteFileSafely(closedTabsIndex,stream=>new XmlSerializer(typeof(List<ClosedTabEntry>)).Serialize(stream,closedTabs));}
    bool RememberClosedTab(EditorDocument document,string archive,bool automatic=false){
        if(document.Welcome)return true;
        try{var history=ClosedTabs();string snapshot=archive??ArchiveIdleTab(document,!automatic);history.Insert(0,new ClosedTabEntry{Name=document.Name,Snapshot=snapshot,ImagePath=document.ImagePath,ProjectPath=document.ProjectPath,Dirty=TabNeedsArchive(document),ClosedUtc=DateTime.UtcNow});if(history.Count>20)history.RemoveRange(20,history.Count-20);try{SaveClosedTabs();}catch(Exception ex){UpdateStatus("Recovery copy saved, but could not save its history index: "+ex.Message);}return true;}
        catch(Exception ex){UpdateStatus("Tab kept open: could not save its recovery copy. "+ex.Message);return false;}
    }
    void ReopenClosedTab(ClosedTabEntry entry){
        var history=ClosedTabs();if(entry==null)entry=history.FirstOrDefault();if(entry==null){UpdateStatus("No recently closed tabs to reopen.");return;}
        if(!File.Exists(entry.Snapshot)){UpdateStatus("This tab's recovery copy is no longer available.");return;}
        FinishInteraction();OpenPath(entry.Snapshot);fileName=entry.Name;imageSavePath=entry.ImagePath;projectSavePath=entry.ProjectPath;dirty=entry.Dirty;StoreDocument();TouchTabActivity();history.Remove(entry);try{SaveClosedTabs();}catch(Exception ex){UpdateStatus("Tab restored; could not update closed-tab history: "+ex.Message);}UpdateDocumentTitles();UpdateStatus();View.Focus();
    }
    internal void ShowClosedTabsMenu(Control anchor,Point location){
        var menu=new ContextMenuStrip();var history=ClosedTabs();var reopen=menu.Items.Add("Reopen closed tab",null,delegate{PopupLifetime.AfterClick(this,delegate{Guard(delegate{ReopenClosedTab(null);});});});((ToolStripMenuItem)reopen).ShortcutKeyDisplayString="Ctrl+Shift+T";reopen.Enabled=history.Count>0;
        if(history.Count>0){menu.Items.Add(new ToolStripSeparator());foreach(var entry in history.ToArray()){var target=entry;menu.Items.Add(entry.Name,null,delegate{PopupLifetime.AfterClick(this,delegate{Guard(delegate{ReopenClosedTab(target);});});});}}
        menu.Closed+=delegate{menu.Dispose();};menu.Show(anchor,location);
    }
    public void VerifyToolsAndClosedTabs(List<string> report,string folder){
        Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);report.Add("PASS "+label);};
        var newTabMessage=Message.Create(Handle,0,IntPtr.Zero,IntPtr.Zero);int previousCount=DocumentCount;check(ProcessCmdKey(ref newTabMessage,Keys.Control|Keys.T)&&DocumentCount==previousCount+1,"Ctrl+T opens a new tab");check(ToolDisplayName("Balloon")=="Speech balloon"&&CaptureSearchEntries().Any(entry=>entry.Tool=="Balloon"&&entry.Title=="Speech balloon"),"Speech balloon display name appears in tool search");
        SetTool("Balloon");check(toolTitle.Text.Equals("Speech balloon",StringComparison.OrdinalIgnoreCase),"Speech balloon name appears in the inspector");
        string root=Preferences.Current.ProjectFolder;bool confirm=Preferences.Current.ConfirmUnsaved,labels=Preferences.Current.ShowToolLabels;int scale=Preferences.Current.UiSize;
        try{
            Preferences.Current.ProjectFolder=Path.Combine(folder,"recovery-projects");Preferences.Current.ConfirmUnsaved=false;
            while(DocumentCount>1)CloseDocumentCore(DocumentCount-1,false);LoadBitmap(Blank(420,280),"Recover this capture");SetWelcome(false);Marks.Add(new Mark{Kind="Rectangle",X=30,Y=40,W=80,H=60,Color=Color.Red.ToArgb()});dirty=true;RefreshImage();StoreDocument();
            check(CloseDocumentCore(currentDocument,true)&&ClosedTabs().Count==1,"Manual closure retains an editable recovery copy");
            var item=ClosedTabs()[0];check(File.Exists(item.Snapshot),"Recovery copy exists before the closed tab is disposed");
            check(CaptureSearchEntries().Any(entry=>entry.Closed==item),"Ctrl+K includes recently closed tabs");
            var message=Message.Create(Handle,0,IntPtr.Zero,IntPtr.Zero);check(ProcessCmdKey(ref message,Keys.Control|Keys.Shift|Keys.T),"Ctrl+Shift+T handles reopening");
            check(fileName=="Recover this capture"&&Background.Size==new Size(420,280)&&Marks.Count==1&&Marks[0].X==30&&dirty,"Reopening restores original pixels, editable marks, name and unsaved state");
            check(ClosedTabs().Count==0,"Reopening consumes the history entry");
            var toolEntry=QuickSearchDialog.Filter(CaptureSearchEntries(),"bucket").First(entry=>entry.Tool=="Bucket");OpenSearchEntry(toolEntry);check(tool=="Bucket","Ctrl+K tool result activates Bucket fill");
            check(QuickSearchDialog.Filter(CaptureSearchEntries(),"blur").Any(entry=>entry.Tool=="Redact"),"Tool search accepts related tool names");
            Preferences.Current.ShowToolLabels=true;ApplyUiSize();ReflowToolbar();check(tools["Bucket"].Text=="Bucket fill"&&!toolRail.WrapContents&&tools["Bucket"].Width==ToolPixels(150),"Optional tool names use the scrolling single-column layout");
            Preferences.Current.UiSize=150;ApplyUiSize();check(tools["Bucket"].Width==ToolPixels(150)&&tools["Bucket"].Text=="Bucket fill","Tool labels scale with Ctrl+plus UI sizing");
            Show();System.Windows.Forms.Application.DoEvents();using(var image=new Bitmap(Width,Height)){DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(folder,"tool-labels.png"));}Hide();
            Preferences.Current.ShowToolLabels=false;ApplyUiSize();check(tools["Bucket"].Text==""&&tools["Bucket"].Width==ToolPixels(40),"Disabling tool names restores compact icon buttons");
        }finally{Preferences.Current.ProjectFolder=root;Preferences.Current.ConfirmUnsaved=confirm;Preferences.Current.ShowToolLabels=labels;Preferences.Current.UiSize=scale;closedTabs=null;ApplyUiSize();LoadBitmap(Blank(100,100),"Test");}
    }

    public void VerifySpeechBalloonStability(List<string> report,string folder){
        Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);report.Add("PASS "+label);};
        LoadBitmap(Blank(1000,700),"Balloon stability");SetWelcome(false);
        var bubble=new Mark{Kind="Balloon",X=100,Y=100,W=280,H=160,Radius=20,Text="Speech balloon",Size=28,Fill=true,FillColor=Color.Blue.ToArgb(),Color=Color.Blue.ToArgb(),BubbleSized=true};
        var other=new Mark{Kind="Rectangle",X=620,Y=400,W=120,H=80,Fill=true,FillColor=Color.Red.ToArgb()};Marks.Add(bubble);Marks.Add(other);SetTool("Select");Active=other;Selected.Clear();Selected.Add(other);SyncStyle();RefreshImage();Show();Application.DoEvents();
        foreach(float zoom in new[]{.37f,.65f,1.25f}){
            View.Zoom=zoom;View.PanX=View.PanY=0;var before=bubble.Clone();
            using(var path=Art.BalloonPath(bubble))using(var scaledPath=Art.BalloonPath(Art.ScaleMarkup(bubble,zoom))){Func<PointF[],float,List<PointF>> normalize=(source,factor)=>{var points=new List<PointF>();foreach(var point in source){var scaled=new PointF(point.X*factor,point.Y*factor);if(points.Count==0||Math.Abs(points.Last().X-scaled.X)>.002||Math.Abs(points.Last().Y-scaled.Y)>.002)points.Add(scaled);}return points;};var native=normalize(path.PathPoints,zoom);var display=normalize(scaledPath.PathPoints,1);check(native.Count==display.Count&&native.Zip(display,(a,b)=>Math.Abs(a.X-b.X)<.002&&Math.Abs(a.Y-b.Y)<.002).All(ok=>ok),"Speech balloon body and tail keep proportional geometry at "+zoom+"x zoom");}
            Func<int> bodyBottom=delegate{using(var image=new Bitmap(View.Width,View.Height)){View.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));var origin=View.Origin;int x=(int)Math.Round(origin.X+(bubble.X+bubble.W/2)*zoom),bottom=-1;for(int y=Math.Max(0,(int)(origin.Y+bubble.Y*zoom));y<Math.Min(image.Height,(int)(origin.Y+(bubble.Y+bubble.H)*zoom));y++){if(x<0||x>=image.Width)continue;var color=image.GetPixel(x,y);if(color.B>color.R+70&&color.B>color.G+70)bottom=y;}return bottom;}};
            int idle=bodyBottom();moving=true;StartDragPreview();other.Move(12,6);TryQueueDragFrame();PrepareDragFrame();int live=bodyBottom();moving=false;RefreshImage();int settled=bodyBottom();
            check(idle>=0&&Math.Abs(idle-live)<=1&&idle==settled,"Moving another annotation preserves speech balloon body size during and after dragging at "+zoom+"x");
            check(bubble.X==before.X&&bubble.Y==before.Y&&bubble.W==before.W&&bubble.H==before.H&&bubble.Size==before.Size,"Unrelated dragging does not change saved speech balloon dimensions");
        }
        Hide();LoadBitmap(Blank(100,100),"Test");
    }

}
}

