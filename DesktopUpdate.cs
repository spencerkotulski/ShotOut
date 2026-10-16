using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
namespace ShotOut {
static class TrayActivation {
    public static string Name(int id){return "Local\\ShotOut.Activate."+id;}
    public static bool Signal(int id){try{using(var signal=EventWaitHandle.OpenExisting(Name(id)))return signal.Set();}catch(WaitHandleCannotBeOpenedException){return false;}catch(UnauthorizedAccessException){return false;}}
}
partial class Editor {
    bool welcomeRequested=true, startupHidePending, exitRequested;
    NotifyIcon trayIcon;
    ContextMenuStrip trayMenu;
    EventWaitHandle activationSignal;
    System.Windows.Forms.Timer activationTimer;
    void SetWelcome(bool show){welcomeRequested=show;if(welcome!=null)welcome.Visible=show;}
    public void AddWelcomeDocument(){
        FinishInteraction();StoreDocument();documents.Add(new EditorDocument {Image=Blank(1000,620),Welcome=true,Tool="Select"});
        currentDocument=documents.Count-1;ReadDocument();FitOnOpen();UpdateStatus();
    }
    void InitializeTray(bool startHidden){
        startupHidePending=startHidden;trayMenu=DarkMenu();
        trayMenu.Items.Add("Open ShotOut",null,delegate{RestoreFromTray();});
        trayMenu.Items.Add(new ToolStripSeparator());
        foreach(string mode in new[]{"Region","Screen","Window"}){string action=mode;trayMenu.Items.Add("Capture "+mode.ToLowerInvariant(),null,delegate{Guard(delegate{Capture(action);});});}
        trayMenu.Items.Add(new ToolStripSeparator());trayMenu.Items.Add("Settings",null,delegate{RestoreFromTray();ShowSettings();});
        trayMenu.Items.Add("Exit ShotOut",null,delegate{ExitFromTray();});
        trayIcon=new NotifyIcon {Icon=Icon,Text="ShotOut · screenshot capture",ContextMenuStrip=trayMenu,Visible=!Preferences.Testing};
        trayIcon.DoubleClick+=delegate{RestoreFromTray();};
        activationSignal=new EventWaitHandle(false,EventResetMode.AutoReset,TrayActivation.Name(Process.GetCurrentProcess().Id));
        activationTimer=new System.Windows.Forms.Timer {Interval=350,Enabled=!Preferences.Testing};activationTimer.Tick+=delegate{if(activationSignal.WaitOne(0))RestoreFromTray();};
        Disposed+=delegate{activationTimer.Dispose();activationSignal.Dispose();trayIcon.Visible=false;trayIcon.Dispose();trayMenu.Dispose();};
    }
    protected override void SetVisibleCore(bool value){
        if(value&&startupHidePending){startupHidePending=false;if(!IsHandleCreated)CreateHandle();value=false;}
        base.SetVisibleCore(value);
    }
    void RestoreFromTray(){startupHidePending=false;Show();if(WindowState==FormWindowState.Minimized)WindowState=FormWindowState.Normal;Activate();BringToFront();}
    bool HandleTrayClose(FormClosingEventArgs e){
        if(exitRequested||e.CloseReason!=CloseReason.UserClosing)return false;
        FinishInteraction();StoreDocument();SaveSessionPreferences();e.Cancel=true;Hide();return true;
    }
    void ExitFromTray(){RestoreFromTray();exitRequested=true;Close();if(!IsDisposed)exitRequested=false;}
    void GrowTextBox(Mark mark,float previousSize){
        if(mark==null||(mark.Kind!="Text"&&mark.Kind!="Balloon")||String.IsNullOrEmpty(mark.Text))return;
        var bounds=mark.BodyBounds;float horizontal=mark.Kind=="Balloon"?20:4,vertical=mark.Kind=="Balloon"?16+(mark.Tail?14:0):4;
        using(var bitmap=new Bitmap(1,1))using(var graphics=Graphics.FromImage(bitmap)){
            mark.TextInsetLeft=mark.TextInsetRight=mark.TextInsetTop=mark.TextInsetBottom=0;
            var size=RichLayout.NaturalSize(graphics,mark);
            mark.X=bounds.X;mark.Y=bounds.Y;mark.W=(float)Math.Ceiling(size.Width+horizontal+2);mark.H=(float)Math.Ceiling(size.Height+vertical+2);if(mark.Kind=="Text")TightenTextSelection(mark);
        }
    }

    public void VerifyDesktopUpdate(List<string> report,string folder){
        Action<bool,string> check=delegate(bool ok,string label){if(!ok)throw new Exception(label);report.Add("PASS "+label);};
        foreach(string kind in new[]{"Text","Balloon"}){
            LoadBitmap(Blank(500,300),"Text growth");var mark=new Mark {Kind=kind,X=20,Y=20,W=140,H=45,Text="Larger text\nstays visible",Size=16};mark.Runs.Add(new TextRun {Text=mark.Text,Style=1});Marks.Add(mark);Active=mark;SyncStyle();
            float previousHeight=mark.H;foreach(int size in new[]{24,36,60,100})fontSize.Value=size;
            check(mark.W>140&&mark.H>previousHeight&&mark.Size==100,"Rapid font growth expands "+kind+" bounds");check(Background.Width>=mark.Bounds.Right&&Background.Height>=mark.Bounds.Bottom,"Text growth expands canvas when needed for "+kind);
            float savedHeight=mark.H;Undo();check(Marks[0].Size==60&&Marks[0].H<savedHeight,"Undo restores font size and text bounds together for "+kind);
        }
        LoadBitmap(Blank(300,200),"Keep this capture");Push();Marks.Add(new Mark {Kind="Rectangle",X=20,Y=20,W=50,H=40});RefreshImage();int original=currentDocument,count=DocumentCount;var prior=Background;
        AddWelcomeDocument();check(DocumentCount==count+1&&welcomeRequested&&Marks.Count==0&&!dirty,"Plus creates a separate welcome tab");
        AddDocument(Blank(170,110),"Opened in new tab",false);check(DocumentCount==count+1&&Background.Width==170&&!welcomeRequested,"Opening image fills new welcome tab without adding another tab");
        ActivateDocument(original);check(Object.ReferenceEquals(prior,Background)&&Marks.Count==1&&dirty,"Adding and filling tab preserves previous capture edits");
        AddWelcomeDocument();Show();Application.DoEvents();int newTab=currentDocument;var closing=new FormClosingEventArgs(CloseReason.UserClosing,false);HandleTrayClose(closing);
        check(closing.Cancel&&!Visible&&!IsDisposed&&welcomeRequested,"Window close hides editor and preserves welcome tab");
        check(TrayActivation.Signal(Process.GetCurrentProcess().Id)&&activationSignal.WaitOne(0),"Relaunch can signal hidden editor for restoration");RestoreFromTray();check(Visible&&currentDocument==newTab&&welcomeRequested,"Tray restore keeps current tab");
        Close();check(!IsDisposed&&!Visible,"Actual window Close keeps the application alive");RestoreFromTray();
        check(trayMenu.Items.Cast<ToolStripItem>().Any(item=>item.Text=="Exit ShotOut"),"Tray offers explicit exit action");
        var systemClose=new FormClosingEventArgs(CloseReason.WindowsShutDown,false);check(!HandleTrayClose(systemClose),"Windows shutdown can close app instead of hiding it");
        using(var startup=new Editor(null,true)){startup.Show();check(!startup.Visible&&startup.IsHandleCreated,"Startup mode remains hidden with a window handle for capture shortcuts");startup.RestoreFromTray();check(startup.Visible,"Startup tray instance can open its editor");startup.ExitFromTray();check(startup.IsDisposed,"Tray Exit actually closes the application window");}
        using(var preview=new Bitmap(Width,Height)){DrawToBitmap(preview,new Rectangle(Point.Empty,preview.Size));preview.Save(System.IO.Path.Combine(folder,"desktop-interface.png"));}
        Hide();while(DocumentCount>1)CloseDocumentCore(DocumentCount-1,false);LoadBitmap(Blank(100,100),"Test");
    }
}
}
