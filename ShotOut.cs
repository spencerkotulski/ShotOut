using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace ShotOut {
static class Program {
    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
    [STAThread] static void Main(string[] args) {
        DesktopDpi.Initialize(); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        if (args.Contains("--self-test")) { Tests.Run(args.Last()); return; }
        Application.ThreadException += delegate(object s, System.Threading.ThreadExceptionEventArgs e) { MessageBox.Show(e.Exception.Message, "ShotOut"); };
        Preferences.LoadCurrent(); if(!AppLaunch.AllowLaunch(Preferences.Current.AllowMultiple))return; Application.Run(new Editor(args.Length > 0 && !args[0].StartsWith("--") ? args[0] : null,args.Contains("--startup")));
    }
}
public class Mark {
    public bool IsCaptureBase;
    public float FillOpacity=100;
    public int TextAlignment=-1;
    public float TextInsetLeft,TextInsetTop,TextInsetRight,TextInsetBottom;
    public string Kind = "Rectangle", Text = "", Name = "";
    public float X, Y, W, H, Radius, CurveX, CurveY = -40, Width = 3, Size = 22;
    public int Color = System.Drawing.Color.FromArgb(255,100,100).ToArgb(), FillColor = System.Drawing.Color.White.ToArgb();
    public bool Fill, Visible = true, Bold = true, Italic, Underline, Shadow, Tail = true;
    public float Magnification=2,SourceX,SourceY;
    public bool MagnifyLinked,SourceInitialized;
    public float TailTipX=.15f,TailTipY=1,ShadowSize=8,ShadowIntensity=40;
    public RectangleF BodyBounds { get {return new RectangleF(Math.Min(X,X+W),Math.Min(Y,Y+H),Math.Abs(W),Math.Abs(H));} }
    public List<TextRun> Runs = new List<TextRun>();
    public List<PointF> Points = new List<PointF>();
    public byte[] Image;
    public RectangleF Bounds { get { var r=new RectangleF(Math.Min(X,X+W),Math.Min(Y,Y+H),Math.Abs(W),Math.Abs(H)); if(Kind.IndexOf("Curved",StringComparison.OrdinalIgnoreCase)>=0) { float cx=X+W/2+CurveX*2,cy=Y+H/2+CurveY*2; r=RectangleF.FromLTRB(Math.Min(r.Left,cx),Math.Min(r.Top,cy),Math.Max(r.Right,cx),Math.Max(r.Bottom,cy)); } if(Kind=="Balloon"&&Tail) {float tx=r.Left+TailTipX*r.Width,ty=r.Top+TailTipY*r.Height;r=RectangleF.FromLTRB(Math.Min(r.Left,tx),Math.Min(r.Top,ty),Math.Max(r.Right,tx),Math.Max(r.Bottom,ty));} if(Kind=="Magnify"&&MagnifyLinked){float factor=Math.Max(1,Magnification);float cx=SourceInitialized?SourceX:r.Left+r.Width/2,cy=SourceInitialized?SourceY:r.Top+r.Height/2;var source=new RectangleF(cx-r.Width/factor/2,cy-r.Height/factor/2,r.Width/factor,r.Height/factor);r=RectangleF.Union(r,source);} return r; } }
    public Mark Clone() { var m = (Mark)MemberwiseClone(); m.Points = new List<PointF>(Points); m.Runs=Runs==null?new List<TextRun>():Runs.Select(r=>new TextRun { Text=r.Text,Style=r.Style }).ToList(); return m; }
    public void Move(float x, float y) { X+=x; Y+=y; for(int i=0;i<Points.Count;i++) Points[i]=new PointF(Points[i].X+x,Points[i].Y+y); }
}
public class Project {
    public byte[] Background;
    public Rectangle CanvasBase;
    public List<Mark> Marks = new List<Mark>();
}
static partial class Art {
    public static bool IsArrow(string kind) { return kind=="Line"||kind=="Arrow"||kind=="Double arrow"||kind=="Dashed arrow"||kind=="Curved arrow"||kind=="Dashed curved arrow"; }
    public static GraphicsPath ArrowPath(Mark m) { var path=new GraphicsPath();var a=new PointF(m.X,m.Y);var b=new PointF(m.X+m.W,m.Y+m.H);if(m.Kind.IndexOf("Curved",StringComparison.OrdinalIgnoreCase)>=0) { var c=new PointF(m.X+m.W/2+2*m.CurveX,m.Y+m.H/2+2*m.CurveY);path.AddBezier(a,new PointF(a.X+2*(c.X-a.X)/3,a.Y+2*(c.Y-a.Y)/3),new PointF(b.X+2*(c.X-b.X)/3,b.Y+2*(c.Y-b.Y)/3),b); } else path.AddLine(a,b);return path; }
    public static Rectangle DrawingBounds(Mark m,Size imageSize) {if(m.Kind=="Spotlight")return new Rectangle(Point.Empty,imageSize);var r=m.Bounds;float pad=Math.Max(6,m.Width*6+4)+(m.Shadow?m.ShadowSize*2+8:0);r.Inflate(pad,pad);return Rectangle.Intersect(Rectangle.FromLTRB((int)Math.Floor(r.Left),(int)Math.Floor(r.Top),(int)Math.Ceiling(r.Right),(int)Math.Ceiling(r.Bottom)),new Rectangle(Point.Empty,imageSize));}
    public static byte[] Bytes(Bitmap b) { using(var s=new MemoryStream()) { b.Save(s,ImageFormat.Png); return s.ToArray(); } }
    public static Bitmap FromBytes(byte[] b) { using(var s=new MemoryStream(b)) using(var im=System.Drawing.Image.FromStream(s)) return new Bitmap(im); }
    public static GraphicsPath Round(RectangleF r, float radius) {
        var p=new GraphicsPath(); float d=Math.Min(radius*2,Math.Min(r.Width,r.Height));
        if(d<1) { p.AddRectangle(r); return p; }
        p.AddArc(r.X,r.Y,d,d,180,90); p.AddArc(r.Right-d,r.Y,d,d,270,90); p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90); p.AddArc(r.X,r.Bottom-d,d,d,90,90); p.CloseFigure(); return p;
    }
    public static Bitmap Render(Bitmap background, List<Mark> marks, bool quality=true) {
        var b=new Bitmap(background.Width,background.Height,PixelFormat.Format32bppArgb);
        using(var g=Graphics.FromImage(b)) g.DrawImageUnscaled(background,0,0);
        foreach(var m in marks) if(m.Visible) Draw(b,m,quality);
        return b;
    }
    public static void Draw(Bitmap b, Mark m, bool quality=true,Rectangle? updateRegion=null) {
        if(updateRegion.HasValue&&!DrawingBounds(m,b.Size).IntersectsWith(updateRegion.Value))return;
        var r=m.Bounds; if((r.Width<1||r.Height<1)&&!IsArrow(m.Kind)) return;
        Rectangle clip=Rectangle.Intersect(Rectangle.FromLTRB((int)Math.Floor(r.Left),(int)Math.Floor(r.Top),(int)Math.Ceiling(r.Right),(int)Math.Ceiling(r.Bottom)),new Rectangle(0,0,b.Width,b.Height));
        if(m.Kind=="Magnify") {DrawMagnifier(b,m,updateRegion);return;}
        if(m.Kind=="Pixelate"||m.Kind=="Blur") {
            if(clip.Width<1||clip.Height<1) return;
            using(var source=b.Clone(clip,PixelFormat.Format32bppArgb)) {
                if(m.Shadow)DrawShadow(b,m,updateRegion);
                if(m.Kind=="Blur") { using(var blurred=Blur(source,Math.Max(2,(int)m.Width*2))) using(var g=Graphics.FromImage(b)) g.DrawImageUnscaled(blurred,clip.Location); return; }
                // Average only the selection. Integer fills cover every pixel, including edge tiles.
                int block=Math.Max(8,(int)m.Width*3),columns=Math.Max(1,source.Width/block),rows=Math.Max(1,source.Height/block);
                var data=source.LockBits(new Rectangle(0,0,source.Width,source.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
                try {
                    int stride=data.Stride;var pixels=new byte[stride*source.Height];Marshal.Copy(data.Scan0,pixels,0,pixels.Length);
                    using(var g=Graphics.FromImage(b)) {
                        g.CompositingMode=CompositingMode.SourceCopy;g.SmoothingMode=SmoothingMode.None;
                        for(int gy=0;gy<rows;gy++)for(int gx=0;gx<columns;gx++) {
                            int left=gx*source.Width/columns,right=(gx+1)*source.Width/columns,top=gy*source.Height/rows,bottom=(gy+1)*source.Height/rows;
                            long red=0,green=0,blue=0,alpha=0;int count=(right-left)*(bottom-top);
                            for(int y=top;y<bottom;y++)for(int x=left;x<right;x++) { int i=y*stride+x*4;blue+=pixels[i];green+=pixels[i+1];red+=pixels[i+2];alpha+=pixels[i+3]; }
                            using(var brush=new SolidBrush(System.Drawing.Color.FromArgb((int)(alpha/count),(int)(red/count),(int)(green/count),(int)(blue/count))))g.FillRectangle(brush,clip.Left+left,clip.Top+top,right-left,bottom-top);
                        }
                    }
                } finally { source.UnlockBits(data); }

            } return;
        }
        if(m.Shadow)DrawShadow(b,m,updateRegion);
        if(quality&&m.Kind!="Bucket"&&m.Kind!="Spotlight"&&m.Kind!="Image"&&m.Kind!="Redact"&&m.Kind!="Eraser") {
            Rectangle box=Rectangle.Intersect(Rectangle.Ceiling(Inflated(r,m.Width*6+4)),new Rectangle(0,0,b.Width,b.Height));
            if(box.Width>0&&box.Height>0&&(long)box.Width*box.Height<8000000) {
                using(var overlay=new Bitmap(box.Width*2,box.Height*2)) { using(var g=Graphics.FromImage(overlay)) { g.ScaleTransform(2,2); g.TranslateTransform(-box.X,-box.Y); PaintVector(g,b,m); } using(var g=Graphics.FromImage(b)) { g.InterpolationMode=InterpolationMode.HighQualityBicubic; g.PixelOffsetMode=PixelOffsetMode.HighQuality; g.DrawImage(overlay,box); } } return;
            }
        }
        using(var g=Graphics.FromImage(b)) {if(updateRegion.HasValue)g.SetClip(updateRegion.Value);PaintVector(g,b,m);}
    }
    static RectangleF Inflated(RectangleF r,float n) { r.Inflate(n,n);return r; }
    static void PaintVector(Graphics g,Bitmap b,Mark m) { var r=m.Kind=="Balloon"?m.BodyBounds:m.Bounds;
        using(var pen=new Pen(System.Drawing.Color.FromArgb(m.Color),m.Width)) using(var ink=new SolidBrush(FillAlpha(m,System.Drawing.Color.FromArgb(m.Color)))) using(var fill=new SolidBrush(FillAlpha(m,System.Drawing.Color.FromArgb(m.FillColor)))) {
            g.SmoothingMode=SmoothingMode.AntiAlias; g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAlias; pen.LineJoin=LineJoin.Round;
            switch(m.Kind) {
                case "Rectangle": using(var path=Round(r,m.Radius)) { if(m.Fill) g.FillPath(fill,path); if(m.Width>0)g.DrawPath(pen,path); } break;
                case "Rounded": using(var path=Round(r,12)) { if(m.Fill) g.FillPath(fill,path); if(m.Width>0)g.DrawPath(pen,path); } break;
                case "Ellipse": if(m.Fill) g.FillEllipse(fill,r); if(m.Width>0)g.DrawEllipse(pen,r); break;
                case "Line": case "Arrow": case "Double arrow": case "Dashed arrow": case "Curved arrow": case "Dashed curved arrow":
                    if(m.Kind!="Line") pen.CustomEndCap=new AdjustableArrowCap(4,5,true);
                    if(m.Kind=="Double arrow") pen.CustomStartCap=new AdjustableArrowCap(4,5,true);
                    if(m.Kind.IndexOf("Dashed",StringComparison.OrdinalIgnoreCase)>=0)pen.DashStyle=DashStyle.Dash; using(var path=ArrowPath(m))g.DrawPath(pen,path); break;
                case "Pen": if(m.Points.Count>1) { pen.StartCap=pen.EndCap=LineCap.Round; g.DrawLines(pen,m.Points.ToArray()); } break;
                case "Highlight": using(var translucent=new SolidBrush(FillAlpha(m,System.Drawing.Color.FromArgb(85,System.Drawing.Color.FromArgb(m.Color))))) g.FillRectangle(translucent,r); break;
                case "Redact": using(var brush=new SolidBrush(FillAlpha(m,System.Drawing.Color.Black)))g.FillRectangle(brush,r); break;
                case "Eraser": g.FillRectangle(ink,r); break;
                case "Spotlight": using(var region=new Region(new Rectangle(0,0,b.Width,b.Height))) { region.Exclude(r); using(var shade=new SolidBrush(FillAlpha(m,System.Drawing.Color.FromArgb(160,0,0,0)))) g.FillRegion(shade,region); } break;
                case "Step": g.FillEllipse(ink,r); using(var f=new Font("Segoe UI",Math.Max(8,Math.Min(r.Width,r.Height)*.46f),FontStyle.Bold,GraphicsUnit.Pixel)) using(var sf=new StringFormat { Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center }) g.DrawString(m.Text,f,Brushes.White,r,sf); break;
                case "Balloon": using(var path=BalloonPath(m)) {g.FillPath(fill,path);if(m.Width>0)g.DrawPath(pen,path);} DrawText(g,m,new RectangleF(r.X+10,r.Y+8,r.Width-20,Math.Max(1,r.Height-(m.Tail?14:0)-16))); break;
                case "Text": case "Emoji": DrawText(g,m,r); break;
                case "Cursor": var pts=new PointF[]{ new PointF(r.X,r.Y),new PointF(r.X,r.Bottom),new PointF(r.X+r.Width*.3f,r.Y+r.Height*.72f),new PointF(r.X+r.Width*.6f,r.Bottom),new PointF(r.X+r.Width*.8f,r.Y+r.Height*.88f),new PointF(r.X+r.Width*.5f,r.Y+r.Height*.6f),new PointF(r.Right,r.Y+r.Height*.6f)}; using(var brush=new SolidBrush(FillAlpha(m,System.Drawing.Color.White)))g.FillPolygon(brush,pts); g.DrawPolygon(Pens.Black,pts); break;
                case "Bucket": PaintBucket(g,m,r);break;
                case "Image": if(m.Image!=null) using(var im=FromBytes(m.Image)) g.DrawImage(im,r); break;
            }
        }
    }
    static void DrawText(Graphics g,Mark m,RectangleF r) { RichLayout.Draw(g,m,r); }

    static Bitmap Blur(Bitmap input,int radius) {
        var b=new Bitmap(input.Width,input.Height,PixelFormat.Format32bppArgb); using(var g=Graphics.FromImage(b))g.DrawImageUnscaled(input,0,0);
        int w=b.Width,h=b.Height; var bits=b.LockBits(new Rectangle(0,0,w,h),ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb); int stride=bits.Stride; var src=new byte[stride*h]; var tmp=new byte[src.Length]; var dst=new byte[src.Length]; Marshal.Copy(bits.Scan0,src,0,src.Length);
        for(int y=0;y<h;y++) for(int c=0;c<4;c++) { long sum=0; int left=0,right=-1; for(int x=0;x<w;x++) { int l=Math.Max(0,x-radius),r=Math.Min(w-1,x+radius); while(right<r)sum+=src[y*stride+(++right)*4+c]; while(left<l)sum-=src[y*stride+(left++)*4+c]; tmp[y*stride+x*4+c]=(byte)(sum/(r-l+1)); } }
        for(int x=0;x<w;x++) for(int c=0;c<4;c++) { long sum=0; int top=0,bottom=-1; for(int y=0;y<h;y++) { int t=Math.Max(0,y-radius),d=Math.Min(h-1,y+radius); while(bottom<d)sum+=tmp[(++bottom)*stride+x*4+c]; while(top<t)sum-=tmp[(top++)*stride+x*4+c]; dst[y*stride+x*4+c]=(byte)(sum/(d-t+1)); } }
        Marshal.Copy(dst,0,bits.Scan0,dst.Length); b.UnlockBits(bits); return b;
    }
}
class Canvas : Control {
    public Editor OwnerEditor;
    public float Zoom=1, PanX, PanY;
    public PointF Origin { get { return new PointF((Width-OwnerEditor.Background.Width*Zoom)/2+PanX,(Height-OwnerEditor.Background.Height*Zoom)/2+PanY); } }
    public Canvas(Editor e) { OwnerEditor=e; DoubleBuffered=true;SetStyle(ControlStyles.ResizeRedraw,true); Dock=DockStyle.Fill; BackColor=Color.FromArgb(25,27,32); TabStop=true; }
    public PointF UnboundedPoint(Point p) {var o=Origin;return new PointF((p.X-o.X)/Zoom,(p.Y-o.Y)/Zoom);}
    public PointF ImagePoint(Point p) { var o=Origin; return new PointF(Math.Max(0,Math.Min(OwnerEditor.Background.Width,(p.X-o.X)/Zoom)),Math.Max(0,Math.Min(OwnerEditor.Background.Height,(p.Y-o.Y)/Zoom))); }
    public void Fit() { Zoom=Math.Min(1,Math.Min((Width-80f)/OwnerEditor.Background.Width,(Height-80f)/OwnerEditor.Background.Height)); Zoom=Math.Max(.02f,Zoom); PanX=PanY=0; Invalidate(); }
    protected override void OnPaint(PaintEventArgs e) {
        base.OnPaint(e);e.Graphics.Clear(BackColor); var ed=OwnerEditor; ed.PrepareDragFrame(); var o=Origin; var g=e.Graphics;
        if(ed.Background==null) return;
        g.TranslateTransform(o.X,o.Y); g.ScaleTransform(Zoom,Zoom);
        using(var checker=new Bitmap(20,20)) { using(var c=Graphics.FromImage(checker)) { c.Clear(Color.White); c.FillRectangle(Brushes.Gainsboro,0,0,10,10); c.FillRectangle(Brushes.Gainsboro,10,10,10,10); } using(var brush=new TextureBrush(checker)) g.FillRectangle(brush,0,0,ed.Background.Width,ed.Background.Height); }
        g.InterpolationMode=InterpolationMode.HighQualityBicubic; g.PixelOffsetMode=PixelOffsetMode.HighQuality;
        if(ed.Preview!=null) g.DrawImage(ed.Preview,0,0,ed.Background.Width,ed.Background.Height);
        ed.DrawHoveredLayer(g);
        foreach(var selected in ed.SelectedObjects()) if(selected!=ed.Active) { var sr=selected.Bounds; using(var sp=new Pen(Color.FromArgb(110,168,255),1/Zoom)) { sp.DashStyle=DashStyle.Dash; g.DrawRectangle(sp,sr.X,sr.Y,sr.Width,sr.Height); } }
        if(ed.Marquee.HasValue) { var mr=ed.Marquee.Value; using(var mb=new SolidBrush(Color.FromArgb(35,110,168,255)))g.FillRectangle(mb,mr);using(var mp=new Pen(Color.FromArgb(110,168,255),1/Zoom))g.DrawRectangle(mp,mr.X,mr.Y,mr.Width,mr.Height); }
        Mark sel=ed.Active;
        if(sel!=null&&sel.Kind=="Magnify")ed.DrawMagnifierHandles(g,sel);
        else if(sel!=null&&sel.Kind=="Balloon")ed.DrawBubbleHandles(g,sel);
        else if(sel!=null&&Art.IsArrow(sel.Kind))ed.DrawArrowHandles(g,sel);
        else if(sel!=null) { var r=sel.Bounds; using(var pen=new Pen(Color.FromArgb(110,168,255),1/Zoom)) { pen.DashStyle=DashStyle.Dash; g.DrawRectangle(pen,r.X,r.Y,r.Width,r.Height); } float d=7/Zoom; foreach(var p in Editor.Handles(r)) { g.FillRectangle(Brushes.White,p.X-d/2,p.Y-d/2,d,d); } }
        if(ed.Crop.HasValue&&ed.IsCutTool) {ed.DrawCutStrip(g);}
        else if(ed.Crop.HasValue) { var r=ed.Crop.Value; using(var reg=new Region(new Rectangle(0,0,ed.Background.Width,ed.Background.Height))) { reg.Exclude(r); using(var br=new SolidBrush(Color.FromArgb(145,0,0,0))) g.FillRegion(br,reg); } using(var pen=new Pen(Color.White,1/Zoom)) g.DrawRectangle(pen,r.X,r.Y,r.Width,r.Height); }
        g.ResetTransform();
    }
    protected override void OnResize(EventArgs e){base.OnResize(e);Invalidate();}
    internal bool RedrawsOnResize {get{return GetStyle(ControlStyles.ResizeRedraw);}}
    protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); Focus(); OwnerEditor.CanvasDown(e); }
    protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); OwnerEditor.CanvasMove(e); }
    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); OwnerEditor.CanvasUp(e); }
    protected override void OnMouseWheel(MouseEventArgs e) { base.OnMouseWheel(e); OwnerEditor.ZoomAt(e.Location,e.Delta>0?1.15f:1/1.15f); }
    protected override void OnMouseDoubleClick(MouseEventArgs e) { base.OnMouseDoubleClick(e); if(OwnerEditor.Crop.HasValue) OwnerEditor.CommitCrop(); else OwnerEditor.EditText(); }
}
partial class Editor : Form {
    public Bitmap Background, Preview;
    public List<Mark> Marks=new List<Mark>();
    public Mark Active;
    public RectangleF? Crop { get; set; }
    public Canvas View;
    string tool="Arrow";
    Color ink=Color.FromArgb(255,100,100), fill=Color.White;
    Color panel=Color.FromArgb(34,37,43), text=Color.FromArgb(224,227,236), muted=Color.FromArgb(151,159,175);
    NumberField thickness, fontSize, delay;
    CheckBox fillBox, boldBox, italicBox, cursorBox;
    ListBox layers;
    Label status, toolTitle, documentLabel;
    Button colorButton, fillButton;
    Panel welcome;
    Dictionary<string,Button> tools=new Dictionary<string,Button>();
    List<Project> undo=new List<Project>(), redo=new List<Project>();
    bool syncing, dragging, moving, resizing, panning, dirty;
    PointF start, last;
    Point lastScreen;
    Mark draft;
    int step=1;
    string fileName="Untitled";
    System.Windows.Forms.Timer captureTimer;
    [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr h,int id,uint modifiers,uint key);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr h,int id);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h,out NativeRect r);
    [DllImport("dwmapi.dll")] static extern int DwmGetWindowAttribute(IntPtr h,int a,out NativeRect r,int n);
    [StructLayout(LayoutKind.Sequential)] struct NativeRect { public int L,T,R,B; }
    public Editor(string path,bool startInTray=false) {
        Text="ShotOut — Screenshot studio"; AutoScaleMode=AutoScaleMode.None; Font=new Font("Segoe UI",13,FontStyle.Regular,GraphicsUnit.Pixel); BackColor=panel; ForeColor=text; MinimumSize=new Size(1100,740); Size=new Size(1320,900); StartPosition=FormStartPosition.CenterScreen; KeyPreview=true; AllowDrop=true;
        Background=Blank(1000,620);ResetCanvasBase(); BuildUI(); Disposed+=delegate {EndDragPreview();}; InstallLogo(); InitializeTray(startInTray); LoadPreferences(); RefreshImage();
        Shown+=delegate { if(!String.IsNullOrEmpty(path)&&File.Exists(path)) OpenPath(path); FitOnOpen(); };
        DragEnter+=delegate(object s,DragEventArgs e) { if(e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect=DragDropEffects.Copy; };
        DragDrop+=delegate(object s,DragEventArgs e) { var paths=(string[])e.Data.GetData(DataFormats.FileDrop); if(paths.Length>0) OpenPath(paths[0]); };
        FormClosing+=delegate(object s,FormClosingEventArgs e) { if(HandleTrayClose(e))return; if(!ConfirmAllDocuments()) e.Cancel=true; else SaveSessionPreferences(); };
        FormClosed+=delegate { DisposeDocuments(); for(int i=1;i<=3;i++) UnregisterHotKey(Handle,i); if(captureTimer!=null) captureTimer.Dispose(); if(Preview!=null) Preview.Dispose(); Background.Dispose(); };
    }
    public static Bitmap Blank(int w,int h) { var b=new Bitmap(w,h); using(var g=Graphics.FromImage(b)) g.Clear(Color.White); return b; }
    Button Button(string label,Action action,int width) { var b=new Button { Text=label,Width=width,Height=34,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(47,51,59),ForeColor=text,Margin=new Padding(4),Cursor=Cursors.Hand,TabStop=false }; b.FlatAppearance.BorderSize=0; b.Click+=delegate { Guard(action); }; return b; }
    void Guard(Action a) { try { a(); } catch(Exception ex) { MessageBox.Show(this,ex.Message,"ShotOut",MessageBoxButtons.OK,MessageBoxIcon.Error); } }
    Label Label(string value,int height) { return new Label { Text=value,Height=height,ForeColor=muted,Dock=DockStyle.Top,Padding=new Padding(14,9,4,0) }; }
    void BuildUI() { BuildModernUI(); }
    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); string problem; if(!RegisterCaptureKeys(Preferences.Current,out problem))BeginInvoke((Action)delegate { UpdateStatus(problem+" Open Settings to change shortcuts."); }); }
    protected override void WndProc(ref Message m) {if(HandleIntegratedChrome(ref m))return; if(m.Msg==0x312) { int n=m.WParam.ToInt32(); Capture(n==1?"Region":n==2?"Screen":"Window"); } base.WndProc(ref m); }
    public void SetTool(string name) {
        if(welcome!=null)SetWelcome(false);
        if(name=="Image") { InsertImage(); return; }
        Selected.Clear(); tool=name; RememberVariant(name); if(alignmentChoice!=null&&(name=="Text"||name=="Balloon")){bool previousSync=syncing;syncing=true;alignmentChoiceIndex=name=="Balloon"?1:0;alignmentChoice.SelectedItem=name=="Balloon"?"Center":"Left";syncing=previousSync;}
        if(toolTitle!=null) toolTitle.Text=tool.ToUpperInvariant(); Crop=null; Active=null; View.Cursor=tool=="Select"?Cursors.Default:Cursors.Cross; SyncLayers(); View.Invalidate(); UpdateStatus();
    }
    void PickColor(bool isFill) { using(var dialog=new PaletteDialog(isFill?fill:ink)) if(dialog.ShowDialog(this)==DialogResult.OK)ApplyPickedColor(isFill,dialog.Chosen); }
    void ApplyPickedColor(bool isFill,Color color) {bool previous=syncing;syncing=true;if(isFill){fill=color;fillBox.Checked=true;}else ink=color;colorButton.BackColor=ink;fillButton.BackColor=fill;syncing=previous;ChangeStyle();UpdateInspector();}
    void ChangeStyle() { if(syncing||thickness==null||fontSize==null||fillBox==null||boldBox==null||italicBox==null) return; if(Active!=null) { Push(); bool textLayoutChanged=Active.Size!=(float)fontSize.Value||Active.Bold!=boldBox.Checked||Active.Italic!=italicBox.Checked; Active.Color=ink.ToArgb(); Active.FillColor=fill.ToArgb(); Active.Width=(float)thickness.Value; float previousTextSize=Active.Size; Active.Size=(float)fontSize.Value; Active.Fill=fillBox.Checked;Active.FillOpacity=(float)fillOpacity.Value; ApplyAlignment(Active); RichLayout.ApplyStyle(Active,boldBox.Checked,italicBox.Checked,underlineBox.Checked); if(Active.Kind=="Rounded") { Active.Kind="Rectangle"; } Active.Radius=(float)cornerRadius.Value; Active.Tail=tailBox.Checked;Active.Shadow=shadowBox.Checked;Active.ShadowSize=(float)shadowSize.Value;Active.ShadowIntensity=(float)shadowIntensity.Value;ApplyMagnifySettings(Active);if(textLayoutChanged)GrowTextBox(Active,previousTextSize);EnsureCanvasFitsMarks(); RefreshImage(); } }
    void SyncStyle() { if(Active==null)return; if(!Selected.Contains(Active)) { Selected.Clear();Selected.Add(Active); } syncing=true; ink=Color.FromArgb(Active.Color); fill=Color.FromArgb(Active.FillColor); colorButton.BackColor=ink; fillButton.BackColor=fill; thickness.Minimum=OptionalOutline(Active.Kind)?0:1; thickness.Value=Math.Max(thickness.Minimum,Math.Min(80,(decimal)Active.Width)); fontSize.Value=Math.Max(8,Math.Min(240,(decimal)Active.Size)); fillBox.Checked=Active.Fill;fillOpacity.Value=(decimal)Active.FillOpacity; boldBox.Checked=Active.Bold; italicBox.Checked=Active.Italic; underlineBox.Checked=Active.Underline; cornerRadius.Value=Math.Min(500,Math.Max(0,(decimal)(Active.Kind=="Rounded"?12:Active.Radius))); tailBox.Checked=Active.Tail;shadowBox.Checked=Active.Shadow;shadowSize.Value=(decimal)Active.ShadowSize;shadowIntensity.Value=(decimal)Active.ShadowIntensity;SyncMagnifySettings(Active);SyncAlignment(Active); syncing=false; UpdateInspector(); }
    public void RefreshImage() { QueuePreview(); }
    void SyncLayers() { ClearLayerHover(); syncing=true; layers.BeginUpdate(); layers.Items.Clear(); for(int i=Marks.Count-1;i>=0;i--) layers.Items.Add(Marks[i]); int n=Marks.IndexOf(Active); layers.SelectedIndex=n<0?-1:Marks.Count-1-n; layers.EndUpdate(); syncing=false; UpdateInspector(); }
    void UpdateStatus(string message=null) { documentLabel.Text=fileName+"  ·  "+Background.Width+" × "+Background.Height; status.Text=message??(tool+"  ·  "+Math.Round(View.Zoom*100)+"%  ·  "+Marks.Count+" annotations     |     Ctrl+C copy image  ·  Ctrl+V paste  ·  Ctrl+Z undo  ·  ? shortcuts"); Text="ShotOut — "+fileName+(dirty?" *":""); if(zoomLabel!=null)zoomLabel.Text=Math.Round(View.Zoom*100)+"%"; UpdateDocumentTitles(); }
    public Project Snapshot() { return new Project { Background=SnapshotImageBytes(),CanvasBase=canvasBase,Marks=Marks.Select(m=>m.Clone()).ToList() }; }
    public void Push() { undo.Add(Snapshot()); if(undo.Count>Preferences.Current.UndoLimit) undo.RemoveAt(0); redo.Clear(); dirty=true; }
    void Restore(Project p) { Background.Dispose(); Background=Art.FromBytes(p.Background);ReadCanvasBase(p.CanvasBase); Marks=p.Marks.Select(m=>m.Clone()).ToList(); Active=null; Crop=null; step=Marks.Count(m=>m.Kind=="Step")+1; dirty=true; RefreshImage(); }
    public void Undo() { if(undo.Count==0)return; redo.Add(Snapshot()); var p=undo[undo.Count-1]; undo.RemoveAt(undo.Count-1); Restore(p); }
    public void Redo() { if(redo.Count==0)return; undo.Add(Snapshot()); var p=redo[redo.Count-1]; redo.RemoveAt(redo.Count-1); Restore(p); }
    public bool ConfirmReplace() { if(!Preferences.Current.ConfirmUnsaved||!dirty)return true; var result=MessageBox.Show(this,"Save your edits before continuing?","Unsaved image",MessageBoxButtons.YesNoCancel,MessageBoxIcon.Question); if(result==DialogResult.Cancel)return false; if(result==DialogResult.Yes)return SaveImage(); return true; }
    public void LoadBitmap(Bitmap b,string name) { Background.Dispose(); Background=b;ResetCanvasBase(); Marks.Clear(); undo.Clear(); redo.Clear(); Active=null; Crop=null; step=1; fileName=name; dirty=false;SetWelcome(false); RefreshImage(); FitOnOpen(); }
    void Open() { using(var d=new OpenFileDialog { Filter="Images and projects|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff;*.shotout;*.snapcanvas|All files|*.*" }) if(d.ShowDialog(this)==DialogResult.OK) OpenPath(d.FileName); }
    void OpenPath(string path) {
        if(Path.GetExtension(path).Equals(".shotout",StringComparison.OrdinalIgnoreCase)||Path.GetExtension(path).Equals(".snapcanvas",StringComparison.OrdinalIgnoreCase)) { Project p; using(var stream=File.OpenRead(path))p=(Project)new XmlSerializer(typeof(Project)).Deserialize(stream);if(p.Background==null||p.Marks==null)throw new InvalidDataException("Invalid project.");var image=Art.FromBytes(p.Background);AddDocument(image,Path.GetFileName(path),false);ReadCanvasBase(p.CanvasBase);Marks=p.Marks.Select(m=>m.Clone()).ToList();step=Marks.Count(m=>m.Kind=="Step")+1;RefreshImage(); }
        else using(var im=Image.FromFile(path))AddDocument(new Bitmap(im),Path.GetFileName(path),false,true);
    }
    public void CopyImage() { using(var b=Art.Render(Background,Marks)) { var data=new DataObject(); data.SetData(DataFormats.Bitmap,true,b); Clipboard.SetDataObject(data,true,10,80); } UpdateStatus("Copied image — switch to Slack and press Ctrl+V."); }
    void Paste() { if(Clipboard.ContainsImage()) { using(var im=Clipboard.GetImage()) { if(Marks.Count==0&&fileName=="Untitled"&&!dirty) LoadImageDocument(new Bitmap(im),"Clipboard"); else AddImage(new Bitmap(im)); } } else if(Clipboard.ContainsText()) { string t=Clipboard.GetText(); var m=NewMark("Text",new PointF(30,30)); m.Text=t; m.W=Math.Min(600,Background.Width-30); m.H=Math.Min(250,Background.Height-30);GrowTextBox(m,m.Size); Push(); Marks.Add(m); Active=m;EnsureCanvasFitsMarks(); RefreshImage(); } else UpdateStatus("Clipboard contains no image or text."); }
    bool SaveImage() { using(var d=new SaveFileDialog { Filter="PNG image|*.png|JPEG image|*.jpg|Bitmap|*.bmp",FileName=Path.GetFileNameWithoutExtension(fileName)+".png",AddExtension=true }) { if(d.ShowDialog(this)!=DialogResult.OK)return false; using(var b=Art.Render(Background,Marks)) { if(d.FilterIndex==2) { using(var flat=Blank(b.Width,b.Height)) { using(var g=Graphics.FromImage(flat))g.DrawImageUnscaled(b,0,0); flat.Save(d.FileName,ImageFormat.Jpeg); } } else b.Save(d.FileName,d.FilterIndex==3?ImageFormat.Bmp:ImageFormat.Png); } fileName=Path.GetFileName(d.FileName); dirty=false; UpdateStatus("Image saved."); return true; } }
    void SaveProject() { using(var d=new SaveFileDialog { Filter="Editable ShotOut project|*.shotout",FileName=Path.GetFileNameWithoutExtension(fileName)+".shotout" }) if(d.ShowDialog(this)==DialogResult.OK) { using(var s=File.Create(d.FileName)) new XmlSerializer(typeof(Project)).Serialize(s,Snapshot()); dirty=false; fileName=Path.GetFileName(d.FileName); UpdateStatus("Editable project saved."); } }
    void SaveMenu() { var menu=DarkMenu(); menu.Items.Add("Export image…    Ctrl+S",null,delegate { Guard(delegate { SaveImage(); }); }); menu.Items.Add("Save editable project…",null,delegate { Guard(SaveProject); }); menu.Show(Cursor.Position); }
    void InsertImage() { using(var d=new OpenFileDialog { Filter="Image|*.png;*.jpg;*.jpeg;*.gif;*.bmp" }) if(d.ShowDialog(this)==DialogResult.OK) using(var im=Image.FromFile(d.FileName)) AddImage(new Bitmap(im)); }
    void AddImage(Bitmap b) { using(b) { Push(); var m=NewMark("Image",new PointF(20,20)); m.Image=Art.Bytes(b); m.W=b.Width; m.H=b.Height; Marks.Add(m); Active=m; tool="Select";EnsureCanvasFitsMarks(); RefreshImage(); } }
    Mark NewMark(string kind,PointF p) { return new Mark { Kind=kind,X=p.X,Y=p.Y,Color=ink.ToArgb(),FillColor=fill.ToArgb(),TextAlignment=kind=="Balloon"?1:kind=="Text"?alignmentChoiceIndex:0,Width=(float)thickness.Value,Size=(float)fontSize.Value,Fill=fillBox.Checked,FillOpacity=(float)fillOpacity.Value,Bold=boldBox.Checked,Italic=italicBox.Checked,Underline=underlineBox.Checked,Radius=(float)cornerRadius.Value,Tail=tailBox.Checked,Shadow=shadowBox.Checked,ShadowSize=(float)shadowSize.Value,ShadowIntensity=(float)shadowIntensity.Value,Magnification=(float)magnifyPercent.Value/100,MagnifyLinked=magnifyLinked.Checked }; }
    public void CanvasDown(MouseEventArgs e) { CanvasDownWithModifiers(e,ModifierKeys); }
    void CanvasDownWithModifiers(MouseEventArgs e,Keys modifiers) {
        if(e.Button==MouseButtons.Middle) { panning=true; lastScreen=e.Location; View.Capture=true; return; }
        if(e.Button==MouseButtons.Right) { if(dragging) CancelDrag(); else { Crop=null; Context(); } return; }
        if(e.Button!=MouseButtons.Left)return;
        start=last=View.ImagePoint(e.Location); View.Capture=true;
        if(tool=="Bucket"){View.Capture=false;var point=View.UnboundedPoint(e.Location);if(new RectangleF(PointF.Empty,Background.Size).Contains(point))Guard(delegate{BucketFill(point);});return;}
        bool forceDraw=tool!="Select"&&tool!="Crop"&&!tool.StartsWith("Cut ")&&(modifiers&(Keys.Control|Keys.Alt))!=0;
        int hit=Active==null?-1:HitHandle(start,Active.Bounds);
        Mark clicked=HitObject(start);if(clicked!=null&&clicked.IsCaptureBase&&tool!="Select")clicked=null;
        if(!forceDraw&&(tool=="Select"||hit>=0||(clicked!=null&&!tool.StartsWith("Cut ")&&tool!="Crop"))) {
            resizeHandle=hit; resizing=hit>=0&&SelectedObjects().Count<=1;
            if(resizing)resizeOriginal=Active.Clone();
            else if(clicked!=null) {
                if((modifiers&Keys.Shift)!=0) { if(!Selected.Add(clicked))Selected.Remove(clicked); Active=Selected.LastOrDefault(); SyncLayers();View.Invalidate();View.Capture=false;return; }
                if(!Selected.Contains(clicked)) { Selected.Clear();Selected.Add(clicked); } Active=clicked;
            } else { if((modifiers&Keys.Shift)==0)Selected.Clear(); Active=null; Marquee=new RectangleF(start,SizeF.Empty); marqueeDragging=true; SyncLayers();View.Invalidate();return; }
            moving=Active!=null&&!resizing; if(Active!=null) { Push(); SyncStyle(); } SyncLayers(); StartDragPreview(); View.Invalidate(); return;
        }
        if(tool=="Crop"||tool.StartsWith("Cut ")) { cutDirection=0; Crop=new RectangleF(start,SizeF.Empty); dragging=true; return; }
        draft=NewMark(tool,start);
        if(tool=="Text"||tool=="Emoji"||tool=="Balloon") { string value; if(tool=="Emoji")value=Prompt.Ask(this,"Paste an emoji (Win + . opens the Windows picker)","✓",true); else value=RichTextEntry.Ask(this,draft); if(value==null) { draft=null; View.Capture=false; return; } draft.Text=value; draft.W=tool=="Emoji"?60:Math.Min(360,Background.Width-start.X); draft.H=tool=="Emoji"?60:Math.Min(150,Background.Height-start.Y); if(tool=="Emoji")draft.Size=46;GrowTextBox(draft,draft.Size); Push(); Marks.Add(draft); Active=draft; draft=null; View.Capture=false;EnsureCanvasFitsMarks(); RefreshImage(); return; }
        if(tool=="Step") { draft.Text=(step++).ToString(); draft.X-=20; draft.Y-=20; draft.W=draft.H=40; Push(); Marks.Add(draft); Active=draft; draft=null; View.Capture=false; RefreshImage(); return; }
        if(tool=="Cursor") { draft.W=32; draft.H=44; Push(); Marks.Add(draft); Active=draft; draft=null; View.Capture=false; RefreshImage(); return; }
        if(tool=="Eraser") { int x=Math.Min(Background.Width-1,Math.Max(0,(int)start.X-2)),y=Math.Min(Background.Height-1,Math.Max(0,(int)start.Y-2)); draft.Color=Preview.GetPixel(x,y).ToArgb(); }
        Push(); Marks.Add(draft); Active=draft; draft.Points.Add(start); dragging=true; StartDragPreview();
    }
    static RectangleF Inflate(RectangleF r,float d) { r.Inflate(d,d); return r; }
    public void CanvasMove(MouseEventArgs e) {
        if(panning) { View.PanX+=e.X-lastScreen.X; View.PanY+=e.Y-lastScreen.Y; lastScreen=e.Location; View.Invalidate(); return; }
        var p=moving||resizing?View.UnboundedPoint(e.Location):View.ImagePoint(e.Location);
        if(marqueeDragging) { Marquee=RectangleF.FromLTRB(Math.Min(start.X,p.X),Math.Min(start.Y,p.Y),Math.Max(start.X,p.X),Math.Max(start.Y,p.Y));View.Invalidate();return; }
        if(moving&&Active!=null) { foreach(var selected in SelectedObjects())selected.Move(p.X-last.X,p.Y-last.Y); last=p; RefreshImage(); return; }
        if(resizing&&Active!=null) { ResizeActive(p); RefreshImage(); return; }
        if(!dragging) { int handle=Active==null?-1:HitHandle(p,Active.Bounds); View.Cursor=handle>=100?(handle==102?Cursors.SizeAll:Cursors.Hand):handle<0?(Active!=null&&Active.Bounds.Contains(p)?Cursors.SizeAll:tool=="Select"?Cursors.Default:Cursors.Cross):handle==1||handle==5?Cursors.SizeNS:handle==3||handle==7?Cursors.SizeWE:handle==0||handle==4?Cursors.SizeNWSE:Cursors.SizeNESW; return; }
        if(Crop.HasValue&&tool.StartsWith("Cut ")) {UpdateCutStrip(p);View.Invalidate();return;}
        if(Crop.HasValue) { Crop=RectangleF.FromLTRB(Math.Min(start.X,p.X),Math.Min(start.Y,p.Y),Math.Max(start.X,p.X),Math.Max(start.Y,p.Y)); View.Invalidate(); return; }
        if(draft==null)return;
        if((ModifierKeys&Keys.Shift)!=0) { float dx=p.X-start.X,dy=p.Y-start.Y; if(Art.IsArrow(tool)) { double angle=Math.Round(Math.Atan2(dy,dx)/(Math.PI/4))*(Math.PI/4),len=Math.Sqrt(dx*dx+dy*dy); p=new PointF(start.X+(float)(Math.Cos(angle)*len),start.Y+(float)(Math.Sin(angle)*len)); } else { float side=Math.Max(Math.Abs(dx),Math.Abs(dy)); p=new PointF(start.X+side*(dx<0?-1:1),start.Y+side*(dy<0?-1:1)); } }
        draft.W=p.X-start.X; draft.H=p.Y-start.Y; if(draft.Kind=="Magnify") {var area=draft.BodyBounds;draft.SourceX=area.Left+area.Width/2;draft.SourceY=area.Top+area.Height/2;draft.SourceInitialized=true;} if(IsCurved(draft))InitCurve(draft);
        if(tool=="Pen") { draft.Points.Add(p); float minX=draft.Points.Min(a=>a.X),minY=draft.Points.Min(a=>a.Y); draft.X=minX; draft.Y=minY; draft.W=Math.Max(1,draft.Points.Max(a=>a.X)-minX); draft.H=Math.Max(1,draft.Points.Max(a=>a.Y)-minY); }
        RefreshImage();
    }
    public void CanvasUp(MouseEventArgs e) { if(e.Button==MouseButtons.Right)return; if(marqueeDragging) { marqueeDragging=false;foreach(var m in Marks)if(m.Visible&&Marquee.Value.IntersectsWith(m.Bounds))Selected.Add(m);Active=Marks.LastOrDefault(m=>Selected.Contains(m));Marquee=null;View.Capture=false;SyncStyle();RefreshImage();return; } if(draft!=null&&(draft.Bounds.Width<1||draft.Bounds.Height<1)) { if(Art.IsArrow(draft.Kind)) { if(draft.W==0) draft.W=.01f; if(draft.H==0) draft.H=.01f; } else { Marks.Remove(draft); Active=null; } } if(draft!=null&&draft.Kind=="Magnify"&&draft.MagnifyLinked)PlaceMagnifier(draft); bool finishCut=dragging&&tool.StartsWith("Cut ")&&Crop.HasValue; bool expand=moving||resizing||draft!=null; dragging=moving=resizing=panning=false; draft=null; View.Capture=false; if(expand)EnsureCanvasFitsMarks(); if(finishCut){CommitCrop();return;} RefreshImage(); if(Crop.HasValue)UpdateStatus("Selection ready — Enter / Apply crop to confirm. Esc cancels."); }
    void CancelDrag() { marqueeDragging=false;Marquee=null; if(draft!=null) { Marks.Remove(draft); draft=null; if(undo.Count>0)undo.RemoveAt(undo.Count-1); } dragging=moving=resizing=panning=false; Crop=null; View.Capture=false; RefreshImage(); }
    public void ZoomAt(Point p,float factor) { var o=View.Origin; var image=new PointF((p.X-o.X)/View.Zoom,(p.Y-o.Y)/View.Zoom); float z=Math.Max(.02f,Math.Min(8,View.Zoom*factor)); View.Zoom=z; View.PanX=p.X-image.X*z-(View.Width-Background.Width*z)/2; View.PanY=p.Y-image.Y*z-(View.Height-Background.Height*z)/2; View.Invalidate(); UpdateStatus(); }
    public void EditText() { if(Active==null||!(Active.Kind=="Text"||Active.Kind=="Balloon"||Active.Kind=="Step"||Active.Kind=="Emoji"))return; var edited=Active.Clone(); string t=Active.Kind=="Text"||Active.Kind=="Balloon"?RichTextEntry.Ask(this,edited):Prompt.Ask(this,"Edit text",edited.Text,true); if(t!=null) { Push(); Active.Text=t;Active.Runs=edited.Runs;GrowTextBox(Active,Active.Size);EnsureCanvasFitsMarks(); RefreshImage(); } }
    void Delete() { var chosen=SelectedObjects();if(chosen.Count==0)return;Push();foreach(var m in chosen)Marks.Remove(m);Selected.Clear();Active=null;EnsureCanvasFitsMarks();RefreshImage(); }
    void Duplicate() { var chosen=SelectedObjects();if(chosen.Count==0)return;Push();Selected.Clear();foreach(var source in chosen) { var m=source.Clone();m.Move(15,15);Marks.Add(m);Selected.Add(m);Active=m; } EnsureCanvasFitsMarks(); RefreshImage(); }
    void Reorder(int delta) { if(Active==null)return; int old=Marks.IndexOf(Active),n=Math.Max(0,Math.Min(Marks.Count-1,old+delta)); if(old==n)return; Push(); Marks.RemoveAt(old); Marks.Insert(n,Active); RefreshImage(); }
    void Context() { var menu=DarkMenu(); menu.Items.Add("Copy image",null,delegate { Guard(CopyImage); }); menu.Items.Add("Undo",null,delegate { Undo(); }); menu.Items.Add("Redo",null,delegate { Redo(); }); menu.Items.Add("Rename layer (F2)",null,delegate { RenameLayer(); }); menu.Items.Add("Duplicate annotation",null,delegate { Duplicate(); }); menu.Items.Add("Delete annotation",null,delegate { Delete(); }); menu.Items.Add("Show / hide annotation",null,delegate { if(Active!=null) { Push(); Active.Visible=!Active.Visible; RefreshImage(); } }); menu.Items.Add("Flatten annotations",null,delegate { Transform(b=>new Bitmap(b)); }); menu.Show(Cursor.Position); }
    public void CommitCrop() {
        if(!Crop.HasValue)return; Rectangle r=Rectangle.Intersect(Rectangle.Round(Crop.Value),new Rectangle(0,0,Background.Width,Background.Height)); if(r.Width<1||r.Height<1)return;
        if(tool=="Cut rows") { if(r.Height>=Background.Height)return; Transform(delegate(Bitmap b) { var n=new Bitmap(b.Width,b.Height-r.Height); using(var g=Graphics.FromImage(n)) { if(r.Top>0)g.DrawImage(b,new Rectangle(0,0,b.Width,r.Top),new Rectangle(0,0,b.Width,r.Top),GraphicsUnit.Pixel); if(r.Bottom<b.Height)g.DrawImage(b,new Rectangle(0,r.Top,b.Width,b.Height-r.Bottom),new Rectangle(0,r.Bottom,b.Width,b.Height-r.Bottom),GraphicsUnit.Pixel); } return n; }); }
        else if(tool=="Cut columns") { if(r.Width>=Background.Width)return; Transform(delegate(Bitmap b) { var n=new Bitmap(b.Width-r.Width,b.Height); using(var g=Graphics.FromImage(n)) { if(r.Left>0)g.DrawImage(b,new Rectangle(0,0,r.Left,b.Height),new Rectangle(0,0,r.Left,b.Height),GraphicsUnit.Pixel); if(r.Right<b.Width)g.DrawImage(b,new Rectangle(r.Left,0,b.Width-r.Right,b.Height),new Rectangle(r.Right,0,b.Width-r.Right,b.Height),GraphicsUnit.Pixel); } return n; }); }
        else Transform(b=>b.Clone(r,PixelFormat.Format32bppArgb));
    }
    public void Transform(Func<Bitmap,Bitmap> operation) { using(var b=Art.Render(Background,Marks)) { Bitmap next=operation(b); if(next==null)return; Push(); Background.Dispose(); Background=next;ResetCanvasBase(); Marks.Clear(); Active=null; Crop=null; RefreshImage(); View.Fit(); } }
    void ImageMenu() {
        var menu=new ContextMenuStrip();
        menu.Items.Add("Expand canvas…",null,delegate {Guard(ExpandCanvasDialog);});
        menu.Items.Add("Place original image beside canvas",null,delegate {Guard(delegate{PlaceImageBeside(false);});});
        menu.Items.Add("Place current edited view beside canvas",null,delegate {Guard(delegate{PlaceImageBeside(true);});});
        menu.Items.Add("New blank canvas…",null,delegate { Guard(delegate { var s=Prompt.Ask(this,"Canvas width × height","1200 x 800",false); var size=ParseSize(s); if(size.HasValue) { AddDocument(Blank(size.Value.Width,size.Value.Height),"Untitled",true); } }); });
        menu.Items.Add("Resize…",null,delegate { Guard(delegate { var size=ParseSize(Prompt.Ask(this,"Resize width × height",Background.Width+" x "+Background.Height,false)); if(size.HasValue) Transform(delegate(Bitmap b) { var n=new Bitmap(size.Value.Width,size.Value.Height); using(var g=Graphics.FromImage(n)) { g.InterpolationMode=InterpolationMode.HighQualityBicubic; g.DrawImage(b,0,0,n.Width,n.Height); } return n; }); }); });
        menu.Items.Add("Rotate 90° clockwise",null,delegate { Transform(delegate(Bitmap b) { var n=new Bitmap(b); n.RotateFlip(RotateFlipType.Rotate90FlipNone); return n; }); });
        menu.Items.Add("Rotate 90° counterclockwise",null,delegate { Transform(delegate(Bitmap b) { var n=new Bitmap(b); n.RotateFlip(RotateFlipType.Rotate270FlipNone); return n; }); });
        menu.Items.Add("Flip horizontal",null,delegate { Transform(delegate(Bitmap b) { var n=new Bitmap(b); n.RotateFlip(RotateFlipType.RotateNoneFlipX); return n; }); });
        menu.Items.Add("Flip vertical",null,delegate { Transform(delegate(Bitmap b) { var n=new Bitmap(b); n.RotateFlip(RotateFlipType.RotateNoneFlipY); return n; }); });
        menu.Items.Add("Grayscale",null,delegate { Transform(delegate(Bitmap b) { var n=new Bitmap(b.Width,b.Height); using(var g=Graphics.FromImage(n)) using(var a=new ImageAttributes()) { a.SetColorMatrix(new ColorMatrix(new float[][] {new float[]{.299f,.299f,.299f,0,0},new float[]{.587f,.587f,.587f,0,0},new float[]{.114f,.114f,.114f,0,0},new float[]{0,0,0,1,0},new float[]{0,0,0,0,1}})); g.DrawImage(b,new Rectangle(0,0,n.Width,n.Height),0,0,b.Width,b.Height,GraphicsUnit.Pixel,a); } return n; }); });
        menu.Items.Add("Add presentation background…",null,delegate { Guard(delegate { string value=Prompt.Ask(this,"Padding in pixels (0–500)","48",false); if(value==null)return; int pad; if(!int.TryParse(value,out pad)||pad<0||pad>500)throw new ArgumentException("Enter a number from 0 to 500."); Transform(delegate(Bitmap b) { var n=new Bitmap(b.Width+pad*2,b.Height+pad*2); using(var g=Graphics.FromImage(n)) { using(var brush=new LinearGradientBrush(new Rectangle(0,0,n.Width,n.Height),Color.FromArgb(65,87,150),Color.FromArgb(148,113,186),45f))g.FillRectangle(brush,0,0,n.Width,n.Height); using(var shadow=new SolidBrush(Color.FromArgb(65,0,0,0)))g.FillRectangle(shadow,pad+7,pad+9,b.Width,b.Height); g.DrawImageUnscaled(b,pad,pad); } return n; }); }); });
        menu.Items.Add("Append image below (manual stitch)…",null,delegate { Guard(delegate { using(var d=new OpenFileDialog { Filter="Image|*.png;*.jpg;*.jpeg;*.bmp" }) if(d.ShowDialog(this)==DialogResult.OK) using(var im=Image.FromFile(d.FileName)) Transform(delegate(Bitmap b) { var n=Blank(Math.Max(b.Width,im.Width),b.Height+im.Height); using(var g=Graphics.FromImage(n)) { g.DrawImageUnscaled(b,0,0); g.DrawImageUnscaled(im,0,b.Height); } return n; }); }); });
        menu.Items.Add("Flatten annotations",null,delegate { Transform(b=>new Bitmap(b)); });
        menu.Items.Add("Keyboard shortcuts",null,delegate { Help(); }); menu.Show(Cursor.Position);
    }
    static Size? ParseSize(string value) { if(value==null)return null; string[] p=value.ToLowerInvariant().Replace('×','x').Split('x'); int w,h; if(p.Length!=2||!int.TryParse(p[0].Trim(),out w)||!int.TryParse(p[1].Trim(),out h)||w<1||h<1||w>16000||h>16000||(long)w*h>40000000)throw new ArgumentException("Use width x height, up to 40 million pixels and 16,000 pixels per side."); return new Size(w,h); }
    void Help() { MessageBox.Show(this,"Capture region: Ctrl+Shift+2\nCapture all screens: Ctrl+Shift+3\nCapture window: Ctrl+Shift+4 (hover a window and click)\n\nCtrl+C / Ctrl+Shift+C: copy final image\nCtrl+V: paste image or text\nCtrl+S: export image · Ctrl+O: open\nCtrl+Z: undo · Ctrl+Y: redo\nCtrl+D: duplicate annotation · Delete: remove\nArrow keys: move (Shift: 10 px)\nPage Up / Down: reorder\nV select · A arrow · R rectangle · E ellipse\nF pen · T text · N step · H highlight\nB blur · P pixelate · C crop\n\nSelect a mark to move it; drag its bottom-right handle to resize.\nShift: square/circle or snap lines to 45°.\nWheel: zoom · Middle drag: pan · 0: fit\nEnter: apply crop / cut · Esc: cancel\n\nImage transforms flatten annotations; Undo restores them.\nSave an editable project to preserve annotations.","ShotOut shortcuts"); }
    protected override bool ProcessCmdKey(ref Message msg,Keys key) {
        if(ActiveControl is NumberField) { if(key==Keys.Enter||key==Keys.Escape) { View.Focus(); return true; } if(key>=Keys.A&&key<=Keys.Z)View.Focus(); else return base.ProcessCmdKey(ref msg,key); }
        else if(ActiveControl is TextBoxBase)return base.ProcessCmdKey(ref msg,key);
        if(key==(Keys.Control|Keys.N)) {AddWelcomeDocument();return true;}
        if(key==(Keys.Control|Keys.W)) { CloseDocument(currentDocument); return true; }
        if(key==(Keys.Control|Keys.Tab)||key==(Keys.Control|Keys.Shift|Keys.Tab)) { CycleDocument((key&Keys.Shift)!=0?-1:1);return true; }
        if(key==(Keys.Control|Keys.C)||key==(Keys.Control|Keys.Shift|Keys.C)) { Guard(CopyImage); return true; }
        if(key==(Keys.Control|Keys.V)) { Guard(Paste); return true; }
        if(key==(Keys.Control|Keys.S)) { Guard(delegate { SaveImage(); }); return true; }
        if(key==(Keys.Control|Keys.O)) { Guard(Open); return true; }
        if(key==(Keys.Control|Keys.Z)) { Undo(); return true; }
        if(key==(Keys.Control|Keys.Y)||key==(Keys.Control|Keys.Shift|Keys.Z)) { Redo(); return true; }
        if(key==(Keys.Control|Keys.D)) { Duplicate(); return true; }
        if(key==Keys.F2) { RenameLayer(); return true; }
        if(key==Keys.Delete) { Delete(); return true; }
        if(key==Keys.Escape) { if(dragging||marqueeDragging) CancelDrag(); else { Crop=null; Selected.Clear(); Active=null; SyncLayers(); View.Invalidate(); } return true; }
        if(key==Keys.Enter&&Crop.HasValue) { Guard(CommitCrop); return true; }
        if(key==Keys.PageUp||key==Keys.PageDown) { Reorder(key==Keys.PageUp?1:-1); return true; }
        Keys code=key&Keys.KeyCode;
        if(Active!=null&&(code==Keys.Left||code==Keys.Right||code==Keys.Up||code==Keys.Down)) { Push(); float d=(key&Keys.Shift)!=0?10:1; foreach(var selected in SelectedObjects())selected.Move(code==Keys.Left?-d:code==Keys.Right?d:0,code==Keys.Up?-d:code==Keys.Down?d:0);EnsureCanvasFitsMarks(); RefreshImage(); return true; }
        if(key==Keys.OemQuestion||key==Keys.F1) { Help(); return true; }
        if(key==Keys.D0) { View.Fit(); return true; }
        var map=Preferences.Current.ToolMap();
        if(map.ContainsKey(key)) { Guard(delegate { SetTool(map[key]); }); return true; }
        return base.ProcessCmdKey(ref msg,key);
    }
    new void Capture(string mode) {
        if(captureTimer!=null&&captureTimer.Enabled)return;
        Hide(); string captureResult=null;int ms=Math.Max(mode=="Window"?800:250,(int)delay.Value*1000); captureTimer=new System.Windows.Forms.Timer { Interval=ms };
        captureTimer.Tick+=delegate { captureTimer.Stop(); try {
            Rectangle bounds=DesktopDpi.ScreenBounds();
            using(var shot=new Bitmap(bounds.Width,bounds.Height)) {
                using(var g=Graphics.FromImage(shot)) { g.CopyFromScreen(bounds.Location,Point.Empty,bounds.Size,CopyPixelOperation.SourceCopy); if(cursorBox.Checked) DrawCursor(g,bounds.Location); }
                if(mode=="Screen") { captureResult=CompleteCapture(new Bitmap(shot)); }
                else using(var selector=new CaptureOverlay(shot,bounds,mode=="Window")) { if(selector.ShowDialog()==DialogResult.OK) { var area=selector.Selection; captureResult=CompleteCapture(shot.Clone(area,PixelFormat.Format32bppArgb)); } }
            }
        } catch(Exception ex) { MessageBox.Show(ex.Message,"Capture failed"); } finally { RestoreFromTray(); UpdateStatus(captureResult); captureTimer.Dispose(); captureTimer=null; } };
        captureTimer.Start();
    }
    [StructLayout(LayoutKind.Sequential)] struct CursorInfo { public int cbSize,flags; public IntPtr hCursor; public Point pos; }
    [StructLayout(LayoutKind.Sequential)] struct IconInfo { public bool icon; public int x,y; public IntPtr mask,color; }
    [DllImport("user32.dll")] static extern bool GetCursorInfo(ref CursorInfo ci);
    [DllImport("user32.dll")] static extern bool GetIconInfo(IntPtr h,out IconInfo info);
    [DllImport("user32.dll")] static extern bool DrawIconEx(IntPtr h,int x,int y,IntPtr icon,int w,int hgt,int step,IntPtr brush,int flags);
    [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr obj);
    static void DrawCursor(Graphics g,Point origin) { var ci=new CursorInfo(); ci.cbSize=Marshal.SizeOf(ci); if(!GetCursorInfo(ref ci)||ci.flags!=1)return; IconInfo ii; if(!GetIconInfo(ci.hCursor,out ii))return; IntPtr dc=g.GetHdc(); try { DrawIconEx(dc,ci.pos.X-origin.X-ii.x,ci.pos.Y-origin.Y-ii.y,ci.hCursor,0,0,0,IntPtr.Zero,3); } finally { g.ReleaseHdc(dc); if(ii.mask!=IntPtr.Zero)DeleteObject(ii.mask); if(ii.color!=IntPtr.Zero)DeleteObject(ii.color); } }
}
partial class CaptureOverlay : Form {
    Bitmap shot; Point origin,start; bool dragging,window; public Rectangle Selection {get;set;} IntPtr self;
    [StructLayout(LayoutKind.Sequential)] struct NativeRect { public int L,T,R,B; }
    [DllImport("user32.dll")] static extern IntPtr GetWindow(IntPtr hwnd,uint cmd);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h,out NativeRect r);
    [DllImport("dwmapi.dll")] static extern int DwmGetWindowAttribute(IntPtr h,int a,out NativeRect r,int n);
    public CaptureOverlay(Bitmap image,Rectangle bounds,bool pickWindow,Func<Point,Rectangle> picker=null) { smart=Preferences.Current.SmartRegion;selectionPicker=picker;shot=image; origin=bounds.Location; window=pickWindow;AutoScaleMode=AutoScaleMode.None;captureBounds=bounds; FormBorderStyle=FormBorderStyle.None; StartPosition=FormStartPosition.Manual; Bounds=bounds; TopMost=true; ShowInTaskbar=false; DoubleBuffered=true; Cursor=Cursors.Cross; KeyPreview=true; self=Handle;Shown+=delegate{if(window||smart)PickWindow(PointToClient(Cursor.Position));}; KeyDown+=delegate(object s,KeyEventArgs e) { if(e.KeyCode==Keys.Escape) { DialogResult=DialogResult.Cancel; Close(); } }; }
    protected override void OnPaint(PaintEventArgs e) { e.Graphics.DrawImageUnscaled(shot,0,0); using(var reg=new Region(ClientRectangle)) { if(!Selection.IsEmpty)reg.Exclude(Selection); using(var shade=new SolidBrush(Color.FromArgb(110,0,0,0)))e.Graphics.FillRegion(shade,reg); } if(!Selection.IsEmpty) { using(var pen=new Pen(Color.FromArgb(111,173,255),2))e.Graphics.DrawRectangle(pen,Selection); } using(var f=new Font("Segoe UI",14)) { string t=CaptureHint(); var s=e.Graphics.MeasureString(t,f); e.Graphics.FillRectangle(Brushes.Black,25,25,s.Width+24,s.Height+18); e.Graphics.DrawString(t,f,Brushes.White,37,34); } }
    protected override void OnMouseDown(MouseEventArgs e) { if(e.Button==MouseButtons.Right){DialogResult=DialogResult.Cancel;Close();return;}if(e.Button!=MouseButtons.Left)return;BeginSelection(e.Location,(ModifierKeys&Keys.Alt)==0); }

    void PickWindow(Point p) { if(selectionPicker!=null){Selection=Rectangle.Intersect(ClientRectangle,selectionPicker(p));return;} Selection=Rectangle.Empty; Point screen=new Point(p.X+origin.X,p.Y+origin.Y); IntPtr h=GetWindow(self,2); int count=0; while(h!=IntPtr.Zero&&count++<1000) { NativeRect r; if(IsWindowVisible(h)&&GetWindowRect(h,out r)) { NativeRect frame; if(DwmGetWindowAttribute(h,9,out frame,Marshal.SizeOf(typeof(NativeRect)))==0)r=frame; var rect=Rectangle.FromLTRB(r.L,r.T,r.R,r.B); if(rect.Contains(screen)) { rect.Offset(-origin.X,-origin.Y); Selection=Rectangle.Intersect(ClientRectangle,rect); break; } } h=GetWindow(h,2); } }
    protected override void OnMouseMove(MouseEventArgs e) {MoveSelection(e.Location,(ModifierKeys&Keys.Alt)==0);}
    protected override void OnMouseUp(MouseEventArgs e) {if(e.Button==MouseButtons.Left)EndSelection(e.Location);}

}
static class Prompt {
    public static string Ask(IWin32Window owner,string title,string value,bool multiline) {
        using(var f=new ApplyPrompt { Text=title,Size=new Size(450,multiline?260:160),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,BackColor=Color.FromArgb(34,37,43),ForeColor=Color.White,Font=new Font("Segoe UI",10) }) {
            var input=new TextBox { Text=value,Multiline=multiline,AcceptsReturn=multiline,ScrollBars=multiline?ScrollBars.Vertical:ScrollBars.None,Location=new Point(16,18),Size=new Size(400,multiline?140:30),BackColor=Color.FromArgb(47,51,59),ForeColor=Color.White,BorderStyle=BorderStyle.FixedSingle };
            var ok=new Button { Text="Apply",DialogResult=DialogResult.OK,Location=new Point(238,multiline?170:65),Size=new Size(85,32) }; var cancel=new Button { Text="Cancel",DialogResult=DialogResult.Cancel,Location=new Point(331,multiline?170:65),Size=new Size(85,32) }; if(multiline)f.Controls.Add(new Label { Text="Ctrl+Enter to apply",ForeColor=Color.FromArgb(160,170,190),Location=new Point(16,177),Size=new Size(200,24),Font=new Font("Segoe UI",12,FontStyle.Regular,GraphicsUnit.Pixel) }); input.KeyDown+=delegate(object sender,KeyEventArgs e) { if(e.Control&&e.KeyCode==Keys.Enter) { e.SuppressKeyPress=e.Handled=true;ok.PerformClick(); } }; f.Controls.Add(input); f.Controls.Add(ok); f.Controls.Add(cancel); f.AcceptButton=ok; f.CancelButton=cancel; f.Shown+=delegate { input.Focus(); input.SelectAll(); }; return f.ShowDialog(owner)==DialogResult.OK?input.Text:null;
        }
    }
}
static class Tests {
    static void Check(bool ok,string name,List<string> report) { if(!ok)throw new Exception("FAILED: "+name); report.Add("PASS "+name); }
    public static void Run(string folder) {
        Preferences.Testing=true;Directory.CreateDirectory(folder); var report=new List<string>();
        try {
            using(var ed=new Editor(null)) {
                ed.CreateControl(); ed.LoadBitmap(Editor.Blank(900,560),"Example screenshot");
                using(var g=Graphics.FromImage(ed.Background)) { g.Clear(Color.FromArgb(246,248,252)); g.FillRectangle(Brushes.White,30,30,840,500); using(var f=new Font("Segoe UI",26,FontStyle.Bold))g.DrawString("A clearer way to share feedback",f,Brushes.MidnightBlue,60,60); using(var f=new Font("Segoe UI",14)) { g.DrawString("Capture. Annotate. Copy. Keep the conversation moving.",f,Brushes.SlateGray,60,125); g.DrawString("Project overview",f,Brushes.Black,70,220); g.DrawString("Private account: confidential@example.com",f,Brushes.Black,70,280); g.DrawString("Ready to share",f,Brushes.Black,70,410); } g.FillRectangle(Brushes.RoyalBlue,650,395,160,52); using(var f=new Font("Segoe UI",12,FontStyle.Bold))g.DrawString("Send update",f,Brushes.White,670,410); }
                ed.Marks.Add(new Mark { Kind="Rounded",X=50,Y=200,W=310,H=55,Color=Color.RoyalBlue.ToArgb(),Width=3 });
                ed.Marks.Add(new Mark { Kind="Arrow",X=480,Y=340,W=145,H=70,Color=Color.FromArgb(255,100,100).ToArgb(),Width=4 });
                ed.Marks.Add(new Mark { Kind="Text",X=440,Y=320,W=380,H=60,Text="One click. Then paste into Slack.",Color=Color.FromArgb(219,70,80).ToArgb(),Size=21 });
                ed.Marks.Add(new Mark { Kind="Step",X=22,Y=207,W=40,H=40,Text="1",Color=Color.RoyalBlue.ToArgb() });
                ed.Marks.Add(new Mark { Kind="Highlight",X=65,Y=406,W=160,H=32,Color=Color.Gold.ToArgb() });
                ed.Marks.Add(new Mark { Kind="Redact",X=220,Y=275,W=520,H=40 });
                ed.RefreshImage(); using(var image=Art.Render(ed.Background,ed.Marks)) { Check(image.GetPixel(300,290).ToArgb()==Color.Black.ToArgb(),"Solid redaction exported",report); image.Save(Path.Combine(folder,"example.png")); }
                int count=ed.Marks.Count; ed.Push(); ed.Marks.Clear(); ed.Undo(); Check(ed.Marks.Count==count,"Undo restores annotation objects",report); ed.Redo(); Check(ed.Marks.Count==0,"Redo restores edits",report); ed.Undo();
                var serializer=new XmlSerializer(typeof(Project)); using(var s=new MemoryStream()) { serializer.Serialize(s,ed.Snapshot()); s.Position=0; var p=(Project)serializer.Deserialize(s); Check(p.Marks.Count==count&&p.Background.Length>100,"Editable project round trip",report); }
                report.Add("NOT RUN: Desktop clipboard and screen capture require interactive validation. Test does not replace the user's clipboard.");
                foreach(string kind in new string[]{"Rectangle","Rounded","Ellipse","Line","Arrow","Double arrow","Pen","Highlight","Redact","Eraser","Spotlight","Step","Balloon","Text","Emoji","Cursor","Blur","Pixelate","Magnify","Image"}) {
                    var m=new Mark { Kind=kind,X=70,Y=70,W=140,H=80,Text="Test",Fill=true }; m.Points.Add(new PointF(70,70));m.Points.Add(new PointF(210,150)); if(kind=="Image") using(var b=Editor.Blank(20,20))m.Image=Art.Bytes(b);
                    using(var b=Art.Render(ed.Background,new List<Mark>{m})) { bool changed=false; for(int y=0;y<b.Height&&!changed;y+=3)for(int x=0;x<b.Width;x+=3)if(b.GetPixel(x,y).ToArgb()!=ed.Background.GetPixel(x,y).ToArgb()) { changed=true;break; } Check(changed,"Render "+kind+" changes exported pixels",report); }
                }
                using(var white=Editor.Blank(120,120)) foreach(var line in new Mark[]{new Mark { Kind="Arrow",X=10,Y=60,W=100,H=0,Color=Color.Red.ToArgb(),Width=4 },new Mark { Kind="Line",X=60,Y=10,W=0,H=100,Color=Color.Blue.ToArgb(),Width=4 }}) using(var drawn=Art.Render(white,new List<Mark>{line}))Check(drawn.GetPixel(60,60).ToArgb()!=Color.White.ToArgb(),"Axis-aligned "+line.Kind,report);
                using(var edge=Editor.Blank(100,100)) { using(var g=Graphics.FromImage(edge))g.FillRectangle(Brushes.Black,0,0,50,100); using(var blurred=Art.Render(edge,new List<Mark>{new Mark { Kind="Blur",X=0,Y=0,W=100,H=100,Width=5 }}))Check(blurred.GetPixel(49,50).R>0&&blurred.GetPixel(50,50).R<255,"Blur blends both sides of an edge",report); }
                ed.Show(); Application.DoEvents(); ed.View.Fit(); ed.RefreshImage(); Application.DoEvents(); using(var preview=new Bitmap(ed.Width,ed.Height)) { ed.DrawToBitmap(preview,new Rectangle(Point.Empty,preview.Size)); preview.Save(Path.Combine(folder,"interface.png")); } ed.VerifyModernUI(report,folder); ed.Hide();
                ed.Crop=new RectangleF(20,20,400,300); ed.CommitCrop(); Check(ed.Background.Width==400&&ed.Background.Height==300&&ed.Marks.Count==0,"Crop flattens to correct dimensions",report); ed.Undo(); Check(ed.Background.Width==900&&ed.Marks.Count==count,"Undo crop restores image and annotations",report);
                ed.LoadBitmap(Editor.Blank(100,100),"Test"); ed.SetTool("Cut rows"); ed.Crop=new RectangleF(0,20,100,30); ed.CommitCrop(); Check(ed.Background.Height==70,"Cut rows",report); ed.Undo(); ed.SetTool("Cut columns"); ed.Crop=new RectangleF(20,0,30,100); ed.CommitCrop(); Check(ed.Background.Width==70,"Cut columns",report);
                ed.VerifyRefinements(report); ed.VerifyFixes(report); ed.VerifyCtrlDraw(report); ed.VerifyArrows(report); ed.VerifySettingsAndText(report,folder); ed.VerifyDocuments(report,folder); ed.VerifyLogoChanges(report,folder); ed.VerifyDragPerformance(report,folder); ed.VerifyShadowUpdate(report,folder); ed.VerifyShotOut(report,folder); ed.VerifyPanelAndOutlines(report,folder);ed.VerifyWorkspaceUpdate(report,folder);ed.VerifyInteractionPolish(report,folder);ed.VerifyDesktopUpdate(report,folder);ed.VerifySizingUpdate(report,folder);ed.VerifyCaptureConvenience(report,folder);ed.VerifyDpiAndBucket(report,folder);ed.VerifyImageLayerResize(report,folder);ed.VerifyChromeOpacity(report,folder);ed.VerifyModalFrameFix(report,folder); ed.LoadBitmap(Editor.Blank(100,100),"Test");
            }
            report.Add("All automated checks passed; interactive validation remains outstanding."); File.WriteAllLines(Path.Combine(folder,"test-results.txt"),report);
        } catch(Exception ex) { report.Add(ex.ToString()); File.WriteAllLines(Path.Combine(folder,"test-results.txt"),report); Environment.ExitCode=1; }
    }
}
}
