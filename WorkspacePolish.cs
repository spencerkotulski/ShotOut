using System;using System.Collections.Generic;using System.Drawing;using System.IO;using System.Linq;using System.Windows.Forms;
namespace ShotOut {partial class Editor {
Control editingDocumentBar;
[System.Runtime.InteropServices.DllImport("dwmapi.dll",EntryPoint="DwmGetWindowAttribute")]
static extern int ReadCornerPreference(IntPtr window,int attribute,out int preference,int size);
internal bool IsWelcomeScreen {get{return welcomeRequested;}}
void ApplyWorkspaceVisibility(){
    SuspendLayout();try{
        if(toolRail!=null)toolRail.Visible=!welcomeRequested;
        if(inspector!=null&&inspector.Parent!=null)inspector.Parent.Visible=!welcomeRequested;
        if(effectsResizeGrip!=null)effectsResizeGrip.Visible=!welcomeRequested;
        if(editingDocumentBar!=null)editingDocumentBar.Visible=!welcomeRequested;
        if(welcome!=null){welcome.Cursor=Cursors.Default;welcome.Visible=welcomeRequested;}
        if(View!=null)View.Cursor=welcomeRequested||tool=="Select"?Cursors.Default:tool=="Hand"?Cursors.Hand:Cursors.Cross;
    }finally{ResumeLayout(true);}
}
public void VerifyWorkspacePolish(List<string> report,string folder){
Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);report.Add("PASS "+label);};
Show();Application.DoEvents();AddWelcomeDocument();Application.DoEvents();
check(welcome.Visible&&!toolRail.Visible&&!inspector.Parent.Visible&&!editingDocumentBar.Visible,"New tabs show the library without editing sidebars or image controls");
check(View.Cursor==Cursors.Default&&welcome.Cursor==Cursors.Default&&screenshotFiles.Cursor==Cursors.Default,"New-tab browser uses the standard pointer");
CanvasMove(new MouseEventArgs(MouseButtons.None,0,100,100,0));check(View.Cursor==Cursors.Default,"Hovering an empty tab never switches to a crosshair");
using(var screenshot=new Bitmap(Width,Height)){DrawToBitmap(screenshot,new Rectangle(Point.Empty,screenshot.Size));screenshot.Save(Path.Combine(folder,"clean-new-tab.png"));}
int browserTab=currentDocument;AddDocument(Blank(350,240),"Opened image",false,true);check(View.Cursor==Cursors.Cross,"Loading a photo from a new tab restores the drawing crosshair");SetTool("Rectangle");check(!welcomeRequested&&toolRail.Visible&&inspector.Parent.Visible&&editingDocumentBar.Visible&&View.Cursor==Cursors.Cross,"Opening an image restores editing panes and the chosen tool cursor");
AddWelcomeDocument();check(welcomeRequested&&!toolRail.Visible&&!inspector.Parent.Visible,"A second new tab hides editing controls again");ActivateDocument(browserTab);check(!welcomeRequested&&toolRail.Visible&&inspector.Parent.Visible,"Returning to an edited tab restores the editing workspace");
int corners;int result=ReadCornerPreference(Handle,33,out corners,4);if(result==0)check(corners==2,"Windows rounded-corner preference is active without a custom window region");else report.Add("SKIP Native rounded corners are unavailable on this Windows version");check(Region==null,"Native rounded corners preserve the normal resize frame");
LoadBitmap(Blank(1000,620),"Untitled");SetWelcome(false);SetTool("Rectangle");Show();Application.DoEvents();View.Zoom=1;View.PanX=View.PanY=0;
Func<float,float,MouseEventArgs> mouse=(x,y)=>{var origin=View.Origin;return new MouseEventArgs(MouseButtons.Left,1,(int)(origin.X+x*View.Zoom),(int)(origin.Y+y*View.Zoom),0);};
CanvasDownWithModifiers(mouse(80,80),Keys.None);CanvasMove(mouse(200,180));CanvasUp(mouse(200,180));check(Marks.Count==1,"Blank canvas accepts a rectangle");SetTool("Select");
for(int attempt=0;attempt<4;attempt++) {var mark=Marks.Last();var center=new PointF(mark.X+mark.W/2,mark.Y+mark.H/2);CanvasDownWithModifiers(mouse(center.X,center.Y),Keys.None);foreach(int offset in new[]{150,700,948,948,1000,1200}){CanvasMove(mouse(center.X+(attempt%2==0?offset:-offset),center.Y));using(var frame=new Bitmap(View.Width,View.Height))View.DrawToBitmap(frame,new Rectangle(Point.Empty,frame.Size));}CanvasUp(mouse(center.X+(attempt%2==0?1200:-1200),center.Y));using(var frame=new Bitmap(View.Width,View.Height))View.DrawToBitmap(frame,new Rectangle(Point.Empty,frame.Size));check(Preview.Size==Background.Size,"Off-canvas rectangle drag remains paintable "+attempt);}
int edited=currentDocument;StoreDocument();AddWelcomeDocument();ActivateDocument(edited);using(var frame=new Bitmap(View.Width,View.Height))View.DrawToBitmap(frame,new Rectangle(Point.Empty,frame.Size));check(Background.Width>0&&Marks.Count==1,"Expanded blank canvas survives switching tabs");
using(var source=Blank(80,60))using(var target=Blank(80,60)){foreach(var area in new[]{new Rectangle(80,0,10,20),new Rectangle(0,60,20,10),new Rectangle(-10,0,10,20),new Rectangle(0,-10,20,10),new Rectangle(0,0,0,20)})Art.CopyPixelRegion(source,target,area);check(true,"Zero-area intersections at every canvas edge are safe");}
Undo();Redo();check(Preview.Size==Background.Size,"Undo and redo retain a valid canvas after moving outside its edges");
SetTool("Rectangle");CanvasDownWithModifiers(mouse(20,240),Keys.Control);CanvasMove(mouse(100,300));CanvasUp(mouse(100,300));check(Marks.Count==2,"Drawing still works after off-canvas moves, tab switches and undo");
var preserved=Background;check(!ResizeCanvas(0,10,0,0)&&Object.ReferenceEquals(preserved,Background),"Invalid canvas dimensions leave the current image intact");
Hide();LoadBitmap(Blank(100,100),"Test");
}
}}