using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
namespace ShotOut {
partial class Editor {
    readonly HashSet<Mark> Selected=new HashSet<Mark>();
    public RectangleF? Marquee { get; set; }
    bool marqueeDragging;
    public List<Mark> SelectedObjects() { Selected.RemoveWhere(m=>!Marks.Contains(m)); if(Active!=null&&!Selected.Contains(Active))return new List<Mark>{Active}; return Marks.Where(m=>Selected.Contains(m)).ToList(); }
    Mark HitObject(PointF p) { float tolerance=5/View.Zoom;return Marks.LastOrDefault(m=>m.Visible&&HitMarkup(m,p,tolerance)); }
    System.Windows.Forms.Timer previewClock;
    bool previewPending;
    int resizeHandle=-1;
    Mark resizeOriginal, layerDragging;
    Point layerMouse;
    CheckBox tailBox;
    FlowLayoutPanel tailRow;
    public static PointF[] Handles(RectangleF r) { return new[]{r.Location,new PointF(r.Left+r.Width/2,r.Top),new PointF(r.Right,r.Top),new PointF(r.Right,r.Top+r.Height/2),new PointF(r.Right,r.Bottom),new PointF(r.Left+r.Width/2,r.Bottom),new PointF(r.Left,r.Bottom),new PointF(r.Left,r.Top+r.Height/2)}; }
    int HitHandle(PointF p,RectangleF r) { if(Active==null||!Active.Visible)return -1;if(Active.Kind=="Magnify"){if(Active.MagnifyLinked&&Art.MagnifySource(Active,Background.Size).Contains(p)&&!Active.BodyBounds.Contains(p))return 300;r=Active.BodyBounds;}if(Active.Kind=="Balloon") {if(Active.Tail){var tip=Art.TailPoint(Active);if(Math.Abs(p.X-tip.X)<9/View.Zoom&&Math.Abs(p.Y-tip.Y)<9/View.Zoom)return 200;}r=Active.BodyBounds;} if(Art.IsArrow(Active.Kind)) { var points=ArrowHandles(Active);for(int i=0;i<points.Length;i++)if(Math.Abs(p.X-points[i].X)<8/View.Zoom&&Math.Abs(p.Y-points[i].Y)<8/View.Zoom)return 100+i;return -1; } var handles=Handles(r); float t=8/View.Zoom; for(int i=0;i<handles.Length;i++)if(Math.Abs(p.X-handles[i].X)<t&&Math.Abs(p.Y-handles[i].Y)<t)return i; return -1; }
    void ResizeActive(PointF p) {
        if(resizeHandle==300&&Active.Kind=="Magnify"){Active.SourceX=resizeOriginal.SourceX+p.X-start.X;Active.SourceY=resizeOriginal.SourceY+p.Y-start.Y;Active.SourceInitialized=true;return;}
        if(resizeHandle==200&&Active.Kind=="Balloon"){var body=resizeOriginal.BodyBounds;Active.TailTipX=(p.X-body.Left)/Math.Max(1,body.Width);Active.TailTipY=(p.Y-body.Top)/Math.Max(1,body.Height);return;}
        if(Art.IsArrow(resizeOriginal.Kind)&&resizeHandle>=100) { EditArrow(p);return; }
        var old=resizeOriginal.Kind=="Balloon"||resizeOriginal.Kind=="Magnify"?resizeOriginal.BodyBounds:resizeOriginal.Bounds; float l=old.Left,t=old.Top,r=old.Right,b=old.Bottom;
        if(resizeHandle==0||resizeHandle==6||resizeHandle==7)l=Math.Min(p.X,r-4);
        if(resizeHandle==0||resizeHandle==1||resizeHandle==2)t=Math.Min(p.Y,b-4);
        if(resizeHandle==2||resizeHandle==3||resizeHandle==4)r=Math.Max(p.X,l+4);
        if(resizeHandle==4||resizeHandle==5||resizeHandle==6)b=Math.Max(p.Y,t+4);
        Active.X=l;Active.Y=t;Active.W=r-l;Active.H=b-t;
        if(resizeOriginal.Points.Count>0)Active.Points=resizeOriginal.Points.Select(q=>new PointF(l+(q.X-old.Left)*(r-l)/Math.Max(1,old.Width),t+(q.Y-old.Top)*(b-t)/Math.Max(1,old.Height))).ToList();
    }
    void QueuePreview() {
        if((moving||resizing||dragging)&&TryQueueDragFrame())return;
        if(!moving&&!resizing&&!dragging)EndDragPreview();
        if(dragging||moving||resizing) {
            previewPending=true;
            if(previewClock==null) { previewClock=new System.Windows.Forms.Timer { Interval=16 }; previewClock.Tick+=delegate { if(previewPending) { previewPending=false; RenderPreview(false); } else previewClock.Stop(); }; Disposed+=delegate { previewClock.Dispose(); }; }
            previewClock.Start(); return;
        }
        previewPending=false; if(previewClock!=null)previewClock.Stop(); RenderPreview(true);
    }
    void RenderPreview(bool quality) {
        if(welcome!=null&&(fileName!="Untitled"||dirty||Marks.Count>0))welcome.Hide();
        var next=Art.Render(Background,Marks,quality); var old=Preview;Preview=next;if(old!=null)old.Dispose();
        if(!dragging&&!moving&&!resizing) { SyncLayers(); UpdateStatus(); } View.Invalidate();
    }
    static void DrawEye(Graphics g,Rectangle r,bool visible) {var state=g.Save();g.TranslateTransform(r.X,r.Y);g.ScaleTransform(r.Width/20f,r.Height/18f);using(var p=new Pen(Color.FromArgb(151,165,188),1.4f)){g.DrawArc(p,0,2,20,14,10,160);g.DrawArc(p,0,2,20,14,190,160);if(visible)g.DrawEllipse(p,7,6,6,6);else g.DrawLine(p,1,17,19,1);}g.Restore(state);}
    void RenameLayer() { BeginInlineRename(); }
    void MoveLayer(Mark mark,int target) { if(mark==null||!Marks.Contains(mark))return; int index=Math.Max(0,Math.Min(Marks.Count-1,target)); if(Marks.IndexOf(mark)==index)return; Push();Marks.Remove(mark);Marks.Insert(index,mark);Active=mark;RefreshImage(); }
    void WireLayers() {WireModernLayers();}
    public void VerifyCtrlDraw(List<string> report) {
        LoadBitmap(Blank(500,400),"Ctrl drawing test");View.Fit();SetTool("Rectangle");
        var original=new Mark {Kind="Rectangle",X=40,Y=40,W=100,H=65};Marks.Add(original);Active=original;SyncStyle();RefreshImage();
        Func<float,float,MouseEventArgs> mouse=delegate(float x,float y) { var o=View.Origin;return new MouseEventArgs(MouseButtons.Left,1,(int)Math.Round(o.X+x*View.Zoom),(int)Math.Round(o.Y+y*View.Zoom),0); };
        CanvasDownWithModifiers(mouse(140,105),Keys.Control);CanvasMove(mouse(180,135));CanvasUp(mouse(180,135));
        if(Marks.Count!=2||original.W!=100||original.H!=65)throw new Exception("Ctrl must draw over an existing resize handle");report.Add("PASS Ctrl draws over existing resize handles without resizing");
        Active=original;CanvasDownWithModifiers(mouse(70,70),Keys.Control);CanvasMove(mouse(110,95));CanvasUp(mouse(110,95));
        if(Marks.Count!=3||original.X!=40||original.Y!=40)throw new Exception("Ctrl must draw over existing shape bodies");report.Add("PASS Ctrl draws over shape bodies without moving them");
        SetTool("Select");CanvasDownWithModifiers(mouse(65,60),Keys.Control);CanvasUp(mouse(65,60));if(Marks.Count!=3||Active!=original)throw new Exception("Select tool must remain a selection tool with Ctrl");report.Add("PASS Ctrl preserves Select tool behavior");
        LoadBitmap(Blank(100,100),"Test");
    }
    public void VerifyFixes(List<string> report) {
        Action<bool,string> check=delegate(bool ok,string label) { if(!ok)throw new Exception(label);report.Add("PASS "+label); };
        using(var pattern=Blank(110,70)) {
            for(int y=0;y<70;y++)for(int x=0;x<110;x++)pattern.SetPixel(x,y,Color.FromArgb(x*2,y*3,x%2==0?255:0));
            foreach(int intensity in new[]{1,3,12,40,80}) {
                var mask=new Mark { Kind="Pixelate",X=10.4f,Y=9.6f,W=85.3f,H=41.3f,Width=intensity };
                using(var image=Art.Render(pattern,new List<Mark>{mask})) {
                    check(image.GetPixel(9,9)==pattern.GetPixel(9,9)&&image.GetPixel(96,51)==pattern.GetPixel(96,51),"Pixelation leaves outside pixels intact at strength "+intensity);
                    bool covered=true;for(int y=9;y<51;y++)if(image.GetPixel(95,y).R>=190)covered=false;for(int x=10;x<96;x++)if(image.GetPixel(x,50).G>=150)covered=false;
                    check(covered,"Right/bottom edges fully averaged at strength "+intensity);
                    if(intensity==1)check(image.GetPixel(10,9)==Color.FromArgb(27,37,127)&&image.GetPixel(17,16)==Color.FromArgb(27,37,127),"Pixel blocks contain the actual selected image average");
                    if(intensity==80)check(image.GetPixel(10,9)==Color.FromArgb(105,88,127)&&image.GetPixel(95,50)==Color.FromArgb(105,88,127),"Maximum strength averages the complete selection without fringes");
                }
            }
        }
        LoadBitmap(Blank(500,400),"Selection test");View.Fit();SetTool("Select");
        var first=new Mark { Kind="Rectangle",X=40,Y=40,W=100,H=65 };var second=new Mark { Kind="Ellipse",X=220,Y=140,W=90,H=70 };Marks.Add(first);Marks.Add(second);RefreshImage();
        Func<float,float,Point> screen=delegate(float x,float y) { var o=View.Origin;return new Point((int)Math.Round(o.X+x*View.Zoom),(int)Math.Round(o.Y+y*View.Zoom)); };
        Action<Point,bool> mouse=delegate(Point p,bool down) { var e=new MouseEventArgs(MouseButtons.Left,1,p.X,p.Y,0);if(down)CanvasDownWithModifiers(e,Keys.None);else CanvasUp(e); };
        var start=screen(5,5);mouse(start,true);var end=screen(450,330);CanvasMove(new MouseEventArgs(MouseButtons.Left,0,end.X,end.Y,0));mouse(end,false);check(SelectedObjects().Count==2,"Mouse marquee selects multiple markup objects");
        start=screen(85,65);mouse(start,true);end=screen(105,85);CanvasMove(new MouseEventArgs(MouseButtons.Left,0,end.X,end.Y,0));mouse(end,false);check(Math.Abs(first.X-60)<2&&Math.Abs(second.X-240)<2,"Dragging any selected object moves the group");
        Duplicate();check(Marks.Count==4&&SelectedObjects().Count==2,"Duplicate selected markup group");Delete();check(Marks.Count==2&&SelectedObjects().Count==0,"Delete selected markup group");
        SetTool("Pen");start=screen(first.X+30,first.Y+30);mouse(start,true);mouse(start,false);check(Active==first,"Click existing markup selects it while a drawing tool is active");
        LoadBitmap(Blank(100,100),"Test");
    }
    public void VerifyRefinements(List<string> report) {
        Show(); Application.DoEvents();
        Action<bool,string> check=delegate(bool ok,string name) { if(!ok)throw new Exception(name);report.Add("PASS "+name); };
        check(tools.ContainsKey("Text")&&tools.ContainsKey("Balloon"),"Separate text and speech bubble tools");
        SetTool("Balloon");check(tailRow.Visible&&radiusRow.Visible,"Bubble tail and radius controls");
        using(var bg=Blank(120,100)) { var bubble=new Mark { Kind="Balloon",X=10,Y=10,W=90,H=70,Radius=12,FillColor=Color.Gold.ToArgb() }; using(var a=Art.Render(bg,new List<Mark>{bubble})) { bubble.Tail=false; using(var b=Art.Render(bg,new List<Mark>{bubble}))check(a.GetPixel(80,74).ToArgb()!=b.GetPixel(80,74).ToArgb(),"Tail toggle changes exported bubble geometry"); } }
        Marks.Add(new Mark {Kind="Ellipse",X=5,Y=5,W=15,H=15}); var mark=new Mark {Kind="Rectangle",X=40,Y=40,W=100,H=70};Marks.Add(mark);Active=mark;resizeOriginal=mark.Clone();resizeHandle=0;ResizeActive(new PointF(20,25));check(mark.X==20&&mark.Y==25&&mark.W==120&&mark.H==85,"Resize from top-left handle");
        resizeOriginal=mark.Clone();resizeHandle=3;ResizeActive(new PointF(200,60));check(mark.W==180&&mark.H==85,"Resize from edge handle");
        MoveLayer(mark,0);check(Marks[0]==mark,"Drag reorder moves the actual layer");Undo();
        Active=Marks.Last();Active.Name="Renamed layer";Active.Visible=false;var snap=Snapshot();check(snap.Marks.Last().Name=="Renamed layer"&&!snap.Marks.Last().Visible,"Layer name and visibility survive snapshots");
        using(var palette=new PaletteDialog(Color.CornflowerBlue))check(palette.Chosen.ToArgb()==Color.CornflowerBlue.ToArgb(),"Modern picker preserves starting color");
        LoadBitmap(Blank(100,100),"Test"); Hide();
    }
}
class Spectrum : Control {
    public double Hue,Saturation,Value;
    public event EventHandler Changed;
    bool hueDrag,svDrag;
    public Color Selected { get { return HSV(Hue,Saturation,Value); } }
    public Spectrum(Color initial) { DoubleBuffered=true;Size=new Size(316,214);Hue=initial.GetHue();Value=Math.Max(initial.R,Math.Max(initial.G,initial.B))/255d;Saturation=Value==0?0:1-Math.Min(initial.R,Math.Min(initial.G,initial.B))/(Value*255); }
    public static Color HSV(double h,double s,double v) { double c=v*s,x=c*(1-Math.Abs((h/60)%2-1)),m=v-c; double r=0,g=0,b=0;int segment=(int)(h/60)%6;if(segment==0){r=c;g=x;}else if(segment==1){r=x;g=c;}else if(segment==2){g=c;b=x;}else if(segment==3){g=x;b=c;}else if(segment==4){r=x;b=c;}else{r=c;b=x;}return Color.FromArgb((int)Math.Round((r+m)*255),(int)Math.Round((g+m)*255),(int)Math.Round((b+m)*255)); }
    protected override void OnPaint(PaintEventArgs e) {
        var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;var square=new Rectangle(0,0,Width-1,168);
        using(var brush=new LinearGradientBrush(square,Color.White,HSV(Hue,1,1),0f))g.FillRectangle(brush,square);
        using(var brush=new LinearGradientBrush(square,Color.Transparent,Color.Black,90f))g.FillRectangle(brush,square);
        for(int i=0;i<6;i++){var r=new RectangleF(i*Width/6f,187,Width/6f+1,16);using(var brush=new LinearGradientBrush(r,HSV(i*60,1,1),HSV((i+1)*60,1,1),0f))g.FillRectangle(brush,r);}
        float px=(float)Saturation*(Width-1),py=(float)(1-Value)*168;using(var p=new Pen(Color.White,2))g.DrawEllipse(p,px-5,py-5,10,10);using(var p=new Pen(Color.Black,1))g.DrawEllipse(p,px-6,py-6,12,12);
        using(var p=new Pen(Color.White,2))g.DrawRectangle(p,(float)Hue/360*(Width-1)-3,184,6,22);
    }
    void UpdateAt(Point p) { if(hueDrag)Hue=Math.Max(0,Math.Min(359.99,p.X*360d/(Width-1)));if(svDrag){Saturation=Math.Max(0,Math.Min(1,p.X/(double)(Width-1)));Value=1-Math.Max(0,Math.Min(1,p.Y/168d));}Invalidate();if(Changed!=null)Changed(this,EventArgs.Empty); }
    protected override void OnMouseDown(MouseEventArgs e) { hueDrag=e.Y>=180;svDrag=!hueDrag;Capture=true;UpdateAt(e.Location); }
    protected override void OnMouseMove(MouseEventArgs e) { if(hueDrag||svDrag)UpdateAt(e.Location); }
    protected override void OnMouseUp(MouseEventArgs e) { hueDrag=svDrag=false;Capture=false; }
}
class PaletteDialog : Form {
    public Color Chosen { get; set; }
    public PaletteDialog(Color initial) {
        Chosen=initial;Text="Choose color";ClientSize=new Size(356,388);BackColor=Color.FromArgb(30,32,38);ForeColor=Color.FromArgb(233,236,243);Font=new Font("Segoe UI",13,FontStyle.Regular,GraphicsUnit.Pixel);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=MinimizeBox=false;StartPosition=FormStartPosition.CenterParent;
        var spectrum=new Spectrum(initial){Location=new Point(20,18)};Controls.Add(spectrum);
        var palette=new FlowLayoutPanel{Location=new Point(17,235),Size=new Size(324,38)};
        foreach(var color in new[]{Color.FromArgb(255,100,100),Color.FromArgb(255,185,70),Color.FromArgb(255,220,60),Color.FromArgb(65,200,148),Color.FromArgb(90,150,255),Color.FromArgb(177,130,255),Color.White,Color.Black}) { Color c=color;var b=new ModernButton{Swatch=true,BackColor=c,Size=new Size(34,32),Margin=new Padding(3),AccessibleName=c.Name};b.Click+=delegate{Chosen=c;spectrum.Hue=c.GetHue();spectrum.Value=Math.Max(c.R,Math.Max(c.G,c.B))/255d;spectrum.Saturation=spectrum.Value==0?0:1-Math.Min(c.R,Math.Min(c.G,c.B))/(spectrum.Value*255);spectrum.Invalidate();UpdateHex();};palette.Controls.Add(b); }Controls.Add(palette);
        hex=new TextBox{Location=new Point(20,284),Width=192,BackColor=Color.FromArgb(43,46,54),ForeColor=ForeColor,BorderStyle=BorderStyle.FixedSingle,Font=Font};Controls.Add(hex);swatch=new Panel{Location=new Point(228,282),Size=new Size(108,29)};Controls.Add(swatch);UpdateHex();spectrum.Changed+=delegate{Chosen=spectrum.Selected;UpdateHex();};
        var apply=new ModernButton{Text="Apply color",Location=new Point(182,334),Size=new Size(154,36),BackColor=Color.FromArgb(84,119,239),ForeColor=ForeColor,Font=Font};apply.Click+=delegate { int rgb;string value=hex.Text.Trim().TrimStart('#');if(value.Length!=6||!int.TryParse(value,NumberStyles.HexNumber,CultureInfo.InvariantCulture,out rgb)){hex.Focus();hex.SelectAll();return;}Chosen=Color.FromArgb(255,(rgb>>16)&255,(rgb>>8)&255,rgb&255);DialogResult=DialogResult.OK;Close();};Controls.Add(apply);
        var cancel=new ModernButton{Text="Cancel",Location=new Point(20,334),Size=new Size(148,36),BackColor=Color.FromArgb(43,46,54),ForeColor=ForeColor,Font=Font,DialogResult=DialogResult.Cancel};Controls.Add(cancel);AcceptButton=apply;CancelButton=cancel;
    }
    TextBox hex;Panel swatch;
    void UpdateHex(){if(hex==null)return;hex.Text="#"+(Chosen.ToArgb()&0xffffff).ToString("X6");swatch.BackColor=Chosen;}
}
}
