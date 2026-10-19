using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
namespace ShotOut {
class EditorDocument {
    public Bitmap Image;
    public List<Mark> Marks=new List<Mark>(), Selected=new List<Mark>();
    public List<Project> Undo=new List<Project>(),Redo=new List<Project>();
    public Mark Active;
    public RectangleF? Crop;
    public Rectangle CanvasBase;
    public string Name="Untitled",Tool="Arrow";
    public int Step=1;
    public bool Dirty,Welcome;
    public float Zoom=1,PanX,PanY;
}
class DocumentStrip : Control {
    Editor editor;int offset;const int TabWidth=190;
    ToolTip tip=new ToolTip();
    public DocumentStrip(Editor owner) {editor=owner;Dock=DockStyle.Top;Height=42;BackColor=Color.FromArgb(26,28,33);DoubleBuffered=true;Cursor=Cursors.Hand;Font=owner.Font;AccessibleName="Open image tabs";}
    int ViewWidth {get{return Math.Max(100,Width-112);}}
    public void Reveal(int index) {int left=index*TabWidth;if(left<offset)offset=left;else if(left+TabWidth>ViewWidth+offset)offset=left+TabWidth-ViewWidth;Clamp();Invalidate();}
    Rectangle PlusBounds {get{return new Rectangle(Math.Min(editor.DocumentCount*TabWidth-offset+6,ViewWidth+4),4,32,34);}}
    void Clamp(){offset=Math.Max(0,Math.Min(offset,Math.Max(0,editor.DocumentCount*TabWidth-ViewWidth)));}
    protected override void OnPaint(PaintEventArgs e) {
        var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;var names=editor.DocumentTitles();var state=g.Save();g.SetClip(new Rectangle(0,0,ViewWidth,Height));
        for(int i=0;i<names.Length;i++){int x=i*TabWidth-offset+4;if(x+TabWidth<0||x>ViewWidth)continue;bool active=i==editor.CurrentDocument;using(var path=Art.Round(new RectangleF(x,4,TabWidth-7,34),7))using(var brush=new SolidBrush(active?Color.FromArgb(48,53,65):BackColor))g.FillPath(brush,path);TextRenderer.DrawText(g,names[i],Font,new Rectangle(x+12,5,TabWidth-48,31),active?Color.FromArgb(237,240,248):Color.FromArgb(158,168,189),TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);using(var pen=new Pen(active?Color.FromArgb(218,226,242):Color.FromArgb(139,150,173),1.4f)){g.DrawLine(pen,x+TabWidth-27,16,x+TabWidth-19,24);g.DrawLine(pen,x+TabWidth-19,16,x+TabWidth-27,24);}if(active)using(var b=new SolidBrush(Color.FromArgb(113,151,255)))g.FillRectangle(b,x+12,36,TabWidth-31,2);}
        g.Restore(state);var plus=PlusBounds;using(var pen=new Pen(Color.FromArgb(200,211,233),1.6f)){g.DrawLine(pen,plus.Left+9,21,plus.Left+23,21);g.DrawLine(pen,plus.Left+16,14,plus.Left+16,28);}if(editor.DocumentCount*TabWidth>ViewWidth){TextRenderer.DrawText(g,"‹",Font,new Rectangle(Width-70,2,32,36),Color.White,TextFormatFlags.VerticalCenter|TextFormatFlags.HorizontalCenter);TextRenderer.DrawText(g,"›",Font,new Rectangle(Width-36,2,32,36),Color.White,TextFormatFlags.VerticalCenter|TextFormatFlags.HorizontalCenter);}
    }
    protected override void OnMouseDown(MouseEventArgs e) {base.OnMouseDown(e);if(e.Button==MouseButtons.Left&&PlusBounds.Contains(e.Location)){editor.TabAction(editor.AddWelcomeDocument);return;}if(e.X>=ViewWidth){offset+=(e.X<Width-38?-1:1)*TabWidth;Clamp();Invalidate();return;}int index=(e.X+offset)/TabWidth;if(index<0||index>=editor.DocumentCount)return;int x=index*TabWidth-offset+4;editor.TabAction(delegate {if(e.Button==MouseButtons.Middle||(e.Button==MouseButtons.Left&&e.X>=x+TabWidth-34))editor.CloseDocument(index);else if(e.Button==MouseButtons.Left)editor.ActivateDocument(index);});}
    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);int index=(e.X+offset)/TabWidth;var names=editor.DocumentTitles();string label=PlusBounds.Contains(e.Location)?"New tab (Ctrl+N)":e.X<ViewWidth&&index<names.Length?names[index]+" · × or middle-click to close":"Scroll tabs";if(tip.GetToolTip(this)!=label)tip.SetToolTip(this,label);}
    protected override void OnMouseWheel(MouseEventArgs e){offset+=e.Delta>0?-TabWidth:TabWidth;Clamp();Invalidate();}
    protected override void Dispose(bool disposing){if(disposing)tip.Dispose();base.Dispose(disposing);}
}
partial class Editor {
    readonly List<EditorDocument> documents=new List<EditorDocument>();
    int currentDocument=-1,captureSerial;
    DocumentStrip documentStrip;
    public int DocumentCount {get{return documents.Count;}}
    public int CurrentDocument {get{return currentDocument;}}
    public void TabAction(Action action){Guard(action);}
    void InitializeDocuments(){documentStrip=new DocumentStrip(this);Controls.Add(documentStrip);documents.Add(new EditorDocument {Image=Background,Marks=Marks,Undo=undo,Redo=redo,Welcome=true});currentDocument=0;}
    public string[] DocumentTitles(){return documents.Select((d,i)=>i==currentDocument?fileName+(dirty?" *":""):d.Name+(d.Dirty?" *":"")).ToArray();}
    void UpdateDocumentTitles(){if(documentStrip!=null)documentStrip.Invalidate();}
    void FinishInteraction(){FinishCloneStroke();ResetCloneSource();FinishStrokeAdjustment();ClearImageSnapping();CloseInlineRename(true);if(dragging||moving||resizing||panning||marqueeDragging)CanvasUp(new MouseEventArgs(MouseButtons.Left,0,0,0,0));previewPending=false;if(previewClock!=null)previewClock.Stop();View.Capture=false;}
    void StoreDocument(){if(currentDocument<0||currentDocument>=documents.Count)return;var d=documents[currentDocument];d.Image=Background;d.CanvasBase=canvasBase;d.Marks=Marks;d.Undo=undo;d.Redo=redo;d.Active=Active;d.Selected=SelectedObjects();d.Crop=Crop;d.Name=fileName;d.Dirty=dirty;d.Tool=tool;d.Step=step;d.Zoom=View.Zoom;d.PanX=View.PanX;d.PanY=View.PanY;d.Welcome=welcomeRequested;}
    public void ActivateDocument(int index){if(index<0||index>=documents.Count||index==currentDocument)return;FinishInteraction();StoreDocument();currentDocument=index;ReadDocument();}
    void ReadDocument(){var d=documents[currentDocument];Background=d.Image;ReadCanvasBase(d.CanvasBase);Marks=d.Marks;undo=d.Undo;redo=d.Redo;Active=d.Active;Selected.Clear();foreach(var m in d.Selected)if(Marks.Contains(m))Selected.Add(m);Crop=d.Crop;fileName=d.Name;dirty=d.Dirty;tool=d.Tool;step=d.Step;View.Zoom=d.Zoom;View.PanX=d.PanX;View.PanY=d.PanY;RememberVariant(tool);View.Cursor=tool=="Hand"?Cursors.Hand:tool=="Select"?Cursors.Default:Cursors.Cross;SetWelcome(d.Welcome);SyncStyle();RefreshImage();documentStrip.Reveal(currentDocument);}
    public void AddDocument(Bitmap image,string name,bool unsaved,bool imageLayer=false){
        FinishInteraction();
        if(fileName=="Untitled"&&!dirty&&Marks.Count==0&&(documents.Count==1||welcomeRequested)){LoadBitmap(image,name);if(imageLayer)PromoteImageLayer();dirty=unsaved;if(welcome!=null)SetWelcome(false);UpdateStatus();documentStrip.Reveal(currentDocument);return;}
        StoreDocument();documents.Add(new EditorDocument {Image=image,Name=name,Dirty=unsaved,Tool=tool});currentDocument=documents.Count-1;ReadDocument();if(imageLayer)PromoteImageLayer();FitOnOpen();UpdateStatus();
    }
    public void CloseDocument(int index){CloseDocumentCore(index,true);}
    bool CloseDocumentCore(int index,bool prompt){if(index<0||index>=documents.Count)return false;ActivateDocument(index);FinishInteraction();if(prompt&&!ConfirmReplace())return false;StoreDocument();var doomed=documents[index];documents.RemoveAt(index);TouchRecovery();doomed.Image.Dispose();currentDocument=-1;
        if(documents.Count==0){var fresh=new EditorDocument {Image=Blank(1000,620),Welcome=true};documents.Add(fresh);}currentDocument=Math.Min(index,documents.Count-1);ReadDocument();return true;
    }
    void CycleDocument(int direction){if(documents.Count>1)ActivateDocument((currentDocument+direction+documents.Count)%documents.Count);}
    bool ConfirmAllDocuments(){FinishInteraction();StoreDocument();for(int i=0;i<documents.Count;i++){ActivateDocument(i);if(!ConfirmReplace())return false;StoreDocument();}return true;}
    void DisposeDocuments(){StoreDocument();foreach(var d in documents)if(d.Image!=null&&!Object.ReferenceEquals(d.Image,Background))d.Image.Dispose();}
    public void VerifyDocuments(List<string> report,string folder){
        Action<bool,string> check=delegate(bool ok,string label){if(!ok)throw new Exception(label);report.Add("PASS "+label);};
        LoadBitmap(Blank(200,150),"Untitled");AddDocument(Blank(210,160),"Capture 1",true);check(DocumentCount==1,"First capture replaces only the untouched welcome canvas");
        Push();Marks.Add(new Mark {Kind="Rectangle",Name="First mark",X=10,Y=10,W=50,H=30});RefreshImage();int firstUndo=undo.Count;float zoom=View.Zoom;var first=documents[0];
        for(int i=2;i<=5;i++)AddDocument(Blank(200+i,150+i),"Capture "+i,true);
        check(DocumentCount==5&&fileName=="Capture 5","Five captures create five tabs without discarding prior captures");
        ActivateDocument(0);check(Marks.Count==1&&Marks[0].Name=="First mark"&&undo.Count==firstUndo&&Math.Abs(View.Zoom-zoom)<.001,"Tab switch restores annotations undo history and zoom");Undo();check(Marks.Count==0,"Undo remains local to the selected tab");ActivateDocument(4);check(Background.Width==205&&Marks.Count==0&&dirty,"Other capture retains image and unsaved state");
        int before=DocumentCount;CloseDocumentCore(2,false);check(DocumentCount==before-1&&DocumentTitles().All(x=>!x.StartsWith("Capture 3")),"Closing a tab removes only that document");
        var original=Preferences.Current.ConfirmUnsaved;Preferences.Current.ConfirmUnsaved=false;try{check(ConfirmAllDocuments(),"Closing window visits every open document");}finally{Preferences.Current.ConfirmUnsaved=original;}
        ActivateDocument(0);Show();Application.DoEvents();using(var preview=new Bitmap(Width,Height)){DrawToBitmap(preview,new Rectangle(Point.Empty,preview.Size));preview.Save(System.IO.Path.Combine(folder,"tabs-interface.png"));}Hide();
        while(DocumentCount>1)CloseDocumentCore(DocumentCount-1,false);CloseDocumentCore(0,false);check(DocumentCount==1&&fileName=="Untitled"&&Marks.Count==0&&!dirty,"Closing final tab returns to an empty welcome canvas");
        LoadBitmap(Blank(100,100),"Test");
    }
}
}
