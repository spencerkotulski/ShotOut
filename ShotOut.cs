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

[assembly: System.Reflection.AssemblyTitle("ShotOut")]
[assembly: System.Reflection.AssemblyProduct("ShotOut")]
[assembly: System.Reflection.AssemblyVersion("1.4.3.0")]
[assembly: System.Reflection.AssemblyFileVersion("1.4.3.0")]

namespace ShotOut {
static class Program {
    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
    [STAThread] static void Main(string[] args) {
        DesktopDpi.Initialize(); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        if (args.Contains("--self-test")) { Tests.Run(args.Last()); return; }
        Application.ThreadException += delegate(object s, System.Threading.ThreadExceptionEventArgs e) { ErrorDetail.Record(e.Exception);MessageBox.Show(e.Exception.Message, "ShotOut"); };
        Preferences.LoadCurrent(); if(!AppLaunch.AllowLaunch(Preferences.Current.AllowMultiple))return; Application.Run(new Editor(args.Length > 0 && !args[0].StartsWith("--") ? args[0] : null,args.Contains("--startup")));
    }
}
public class Mark {
    public List<string> ImageEffectOrder=new List<string>(); public bool ImageFilterEnabled=true; public int ImageFilter; public float ImageFilterStrength=100; public BackgroundLayout FrameLayout; public bool FrameRoundedImage; public List<ImageAdjustmentZone> ImageZones=new List<ImageAdjustmentZone>(); public bool ImageAdjustmentsEnabled=true; public float Exposure,Contrast,Brightness,Saturation,Temperature; public float AdjustmentStrength=100; public string ImageAdjustmentId; public bool IsCaptureBase; public bool? ImageMovementLocked;
    public bool IsCloneLayer;
    public string LinkId;
    public string FontFamily="Segoe UI";
    public int Orientation;
    public bool AdjustmentWholeCanvas;
    public bool HasCropClip;
    public RectangleF CropClip;
    public RectangleF VisibleBounds {get{return HasCropClip?RectangleF.Intersect(Bounds,CropClip):Bounds;}}
    public float FillOpacity=100;
    public int TextAlignment=-1;
    public float TextInsetLeft,TextInsetTop,TextInsetRight,TextInsetBottom;
    [XmlIgnore] public float RenderScale=1;
    public string Kind = "Rectangle", Text = "", Name = "";
    public bool ArrowCurved, IsPresentation, IsPresentationCard, BubbleSized; public string ArrowStyle="Modern"; public float X, Y, W, H, Radius, CurveX, CurveY = -40, Width = 3, Size = 22;
    public int Color = System.Drawing.Color.FromArgb(255,100,100).ToArgb(), FillColor = System.Drawing.Color.White.ToArgb();
    public bool Fill, Visible = true, Bold = true, Italic, Underline, Shadow, Tail = true;
    public float Magnification=2,SourceX,SourceY;
    public bool MagnifyLinked,SourceInitialized;
    public float TailTipX=.15f,TailTipY=1,ShadowSize=8,ShadowIntensity=40;
    public RectangleF BodyBounds { get {return new RectangleF(Math.Min(X,X+W),Math.Min(Y,Y+H),Math.Abs(W),Math.Abs(H));} }
    public List<TextRun> Runs = new List<TextRun>();
    public List<PointF> Points = new List<PointF>();
    public byte[] Image;
    public RectangleF Bounds { get { var r=new RectangleF(Math.Min(X,X+W),Math.Min(Y,Y+H),Math.Abs(W),Math.Abs(H)); if(ArrowCurved||Kind.IndexOf("Curved",StringComparison.OrdinalIgnoreCase)>=0) { float cx=X+W/2+CurveX*2,cy=Y+H/2+CurveY*2; r=RectangleF.FromLTRB(Math.Min(r.Left,cx),Math.Min(r.Top,cy),Math.Max(r.Right,cx),Math.Max(r.Bottom,cy)); } if(Kind=="Balloon"&&Tail) {var tip=Art.TailPoint(this);float tx=tip.X,ty=tip.Y;r=RectangleF.FromLTRB(Math.Min(r.Left,tx),Math.Min(r.Top,ty),Math.Max(r.Right,tx),Math.Max(r.Bottom,ty));} if(Kind=="Magnify"&&MagnifyLinked){float factor=Math.Max(1,Magnification);float cx=SourceInitialized?SourceX:r.Left+r.Width/2,cy=SourceInitialized?SourceY:r.Top+r.Height/2;var source=new RectangleF(cx-r.Width/factor/2,cy-r.Height/factor/2,r.Width/factor,r.Height/factor);r=RectangleF.Union(r,source);} return r; } }
    public Mark Clone() { var m = (Mark)MemberwiseClone(); m.ImageEffectOrder=new List<string>(ImageEffectOrder??new List<string>());m.FrameLayout=FrameLayout==null?null:FrameLayout.Clone();m.ImageZones=ImageZones.Select(zone=>zone.Clone()).ToList();m.Points = new List<PointF>(Points); m.Runs=Runs==null?new List<TextRun>():Runs.Select(r=>new TextRun { Text=r.Text,Style=r.Style }).ToList(); return m; }
    public void Move(float x, float y) { X+=x; Y+=y; if(HasCropClip)CropClip.Offset(x,y); for(int i=0;i<Points.Count;i++) Points[i]=new PointF(Points[i].X+x,Points[i].Y+y); }
}
public class Project {
    [XmlIgnore] public string HistoryAction;
    public DateTime CreatedUtc=DateTime.UtcNow;
    public byte[] Background;
    public Rectangle CanvasBase;
    public List<Mark> Marks = new List<Mark>();
}
static partial class Art {
    public static bool IsArrow(string kind) { return kind=="Line"||kind=="Arrow"||kind=="Double arrow"||kind=="Dashed arrow"||kind=="Curved arrow"||kind=="Dashed curved arrow"; }
    public static GraphicsPath ArrowPath(Mark m) { var path=new GraphicsPath();var a=new PointF(m.X,m.Y);var b=new PointF(m.X+m.W,m.Y+m.H);if(m.ArrowCurved||m.Kind.IndexOf("Curved",StringComparison.OrdinalIgnoreCase)>=0) { var c=new PointF(m.X+m.W/2+2*m.CurveX,m.Y+m.H/2+2*m.CurveY);path.AddBezier(a,new PointF(a.X+2*(c.X-a.X)/3,a.Y+2*(c.Y-a.Y)/3),new PointF(b.X+2*(c.X-b.X)/3,b.Y+2*(c.Y-b.Y)/3),b); } else path.AddLine(a,b);return path; }
    public static Rectangle DrawingBounds(Mark m,Size imageSize) {if(m.Kind=="Spotlight"||(m.Kind=="Grayscale"&&m.AdjustmentWholeCanvas))return new Rectangle(Point.Empty,imageSize);var r=m.Bounds;float pad=Math.Max(6,m.Width*6+4)+(m.Shadow?m.ShadowSize*2+8:0);r.Inflate(pad,pad);return Rectangle.Intersect(Rectangle.FromLTRB((int)Math.Floor(r.Left),(int)Math.Floor(r.Top),(int)Math.Ceiling(r.Right),(int)Math.Ceiling(r.Bottom)),new Rectangle(Point.Empty,imageSize));}
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
        if(m.HasCropClip){DrawCroppedMarkup(b,m,quality,updateRegion);return;}
        DrawUnclipped(b,m,quality,updateRegion);
    }
    static void DrawUnclipped(Bitmap b, Mark m, bool quality,Rectangle? updateRegion) {
        if(updateRegion.HasValue&&!DrawingBounds(m,b.Size).IntersectsWith(updateRegion.Value))return;
        if(m.IsPresentation&&!m.IsPresentationCard&&m.FrameLayout!=null){using(var graphics=Graphics.FromImage(b)){if(updateRegion.HasValue)graphics.SetClip(updateRegion.Value);graphics.SmoothingMode=SmoothingMode.None;PaintPresentation(graphics,m,m.Bounds);}return;}
        if(m.Kind=="Grayscale"){ApplyGrayscale(b,m,updateRegion);return;}
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
        if(quality&&DrawHighQualityVector(b,m,r,updateRegion))return;

        using(var g=Graphics.FromImage(b)) {if(updateRegion.HasValue)g.SetClip(updateRegion.Value);PaintVector(g,b,m);}
    }
    static RectangleF Inflated(RectangleF r,float n) { r.Inflate(n,n);return r; }
    static void PaintVector(Graphics g,Bitmap b,Mark m,bool screenText=false) {if(m.Orientation!=0){PaintOriented(g,b,m,screenText);return;} var r=m.Kind=="Balloon"?m.BodyBounds:m.Bounds;
        using(var pen=new Pen(System.Drawing.Color.FromArgb(m.Color),m.Width)) using(var ink=new SolidBrush(FillAlpha(m,System.Drawing.Color.FromArgb(m.Color)))) using(var fill=new SolidBrush(FillAlpha(m,System.Drawing.Color.FromArgb(m.FillColor)))) {
            g.SmoothingMode=SmoothingMode.HighQuality;g.PixelOffsetMode=PixelOffsetMode.HighQuality; g.TextRenderingHint=screenText?System.Drawing.Text.TextRenderingHint.AntiAliasGridFit:System.Drawing.Text.TextRenderingHint.AntiAlias; pen.LineJoin=LineJoin.Round;
            switch(m.Kind) {
                case "Rectangle": if(m.IsPresentation){PaintPresentation(g,m,r);break;} using(var path=Round(r,m.Radius)) { if(m.Fill) g.FillPath(fill,path); if(m.Width>0)g.DrawPath(pen,path); } break;
                case "Rounded": using(var path=Round(r,12)) { if(m.Fill) g.FillPath(fill,path); if(m.Width>0)g.DrawPath(pen,path); } break;
                case "Ellipse": if(m.Fill) g.FillEllipse(fill,r); if(m.Width>0)g.DrawEllipse(pen,r); break;
                case "Line": case "Arrow": case "Double arrow": case "Dashed arrow": case "Curved arrow": case "Dashed curved arrow":
                    if(m.Kind!="Line") pen.CustomEndCap=new AdjustableArrowCap(m.ArrowStyle=="Basic"?3:4,m.ArrowStyle=="Basic"?3:5,m.ArrowStyle!="Classic");
                    if(m.Kind=="Double arrow") pen.CustomStartCap=new AdjustableArrowCap(4,5,true);
                    if(m.Kind.IndexOf("Dashed",StringComparison.OrdinalIgnoreCase)>=0)pen.DashStyle=DashStyle.Dash; using(var path=ArrowPath(m))g.DrawPath(pen,path); break;
                case "Pen": if(m.Points.Count>1) { pen.StartCap=pen.EndCap=LineCap.Round; g.DrawLines(pen,m.Points.ToArray()); } break;
                case "Highlight": using(var translucent=new SolidBrush(FillAlpha(m,System.Drawing.Color.FromArgb((int)Math.Round(85*System.Drawing.Color.FromArgb(m.Color).A/255d),System.Drawing.Color.FromArgb(m.Color))))) g.FillRectangle(translucent,r); break;
                case "Redact": using(var brush=new SolidBrush(FillAlpha(m,System.Drawing.Color.Black)))g.FillRectangle(brush,r); break;
                case "Eraser": g.FillRectangle(ink,r); break;
                case "Spotlight": using(var region=new Region(new Rectangle(0,0,b.Width,b.Height))) { region.Exclude(r); using(var shade=new SolidBrush(FillAlpha(m,System.Drawing.Color.FromArgb(160,0,0,0)))) g.FillRegion(shade,region); } break;
                case "Step": g.FillEllipse(ink,r); using(var f=new Font("Segoe UI",Math.Max(8,Math.Min(r.Width,r.Height)*.46f),FontStyle.Bold,GraphicsUnit.Pixel)) using(var sf=new StringFormat { Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center }) g.DrawString(m.Text,f,Brushes.White,r,sf); break;
                case "Balloon": using(var path=BalloonPath(m)) {g.FillPath(fill,path);if(m.Width>0)g.DrawPath(pen,path);} DrawText(g,m,new RectangleF(r.X+10*m.RenderScale,r.Y+8*m.RenderScale,r.Width-20*m.RenderScale,Math.Max(m.RenderScale,r.Height-((m.Tail?14:0)+16)*m.RenderScale))); break;
                case "Text": case "Emoji": DrawText(g,m,r); break;
                case "Cursor": var pts=new PointF[]{ new PointF(r.X,r.Y),new PointF(r.X,r.Bottom),new PointF(r.X+r.Width*.3f,r.Y+r.Height*.72f),new PointF(r.X+r.Width*.6f,r.Bottom),new PointF(r.X+r.Width*.8f,r.Y+r.Height*.88f),new PointF(r.X+r.Width*.5f,r.Y+r.Height*.6f),new PointF(r.Right,r.Y+r.Height*.6f)}; using(var brush=new SolidBrush(FillAlpha(m,System.Drawing.Color.White)))g.FillPolygon(brush,pts); g.DrawPolygon(Pens.Black,pts); break;
                case "Bucket": PaintBucket(g,m,r);break;
                case "Image": if(m.Image!=null)DrawLayerImage(g,m.Image,r,m); break;
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
partial class Canvas : Control {
    public Editor OwnerEditor;
    public float Zoom=1, PanX, PanY;
    public PointF Origin { get { return new PointF((Width-OwnerEditor.Background.Width*Zoom)/2+PanX,(Height-OwnerEditor.Background.Height*Zoom)/2+PanY); } }
    public Canvas(Editor e) { OwnerEditor=e; DoubleBuffered=true;SetStyle(ControlStyles.ResizeRedraw,true); Dock=DockStyle.Fill; BackColor=Color.FromArgb(25,27,32); TabStop=true; }
    public PointF UnboundedPoint(Point p) {var o=Origin;return new PointF((p.X-o.X)/Zoom,(p.Y-o.Y)/Zoom);}
    public PointF ImagePoint(Point p) { var o=Origin; return new PointF(Math.Max(0,Math.Min(OwnerEditor.Background.Width,(p.X-o.X)/Zoom)),Math.Max(0,Math.Min(OwnerEditor.Background.Height,(p.Y-o.Y)/Zoom))); }
    public void Fit() { Zoom=Math.Min(1,Math.Min((Width-80f)/OwnerEditor.Background.Width,(Height-80f)/OwnerEditor.Background.Height)); Zoom=Math.Max(.02f,Zoom); PanX=PanY=0; OwnerEditor.RefreshInlinePlacement();Invalidate(); }
    protected override void OnPaint(PaintEventArgs e) {using(new Art.ImageComparison(OwnerEditor.ImageComparisonId))PaintCanvas(e);} void PaintCanvas(PaintEventArgs e) {
        base.OnPaint(e);e.Graphics.Clear(BackColor);if(OwnerEditor.IsWelcomeScreen)return; var ed=OwnerEditor; ed.PrepareDragFrame(); var o=ed.CanvasPanning||ZoomGestureActive?Origin:new PointF((float)Math.Round(Origin.X),(float)Math.Round(Origin.Y)); var g=e.Graphics;
        if(ed.Background==null) return;
        g.TranslateTransform(o.X,o.Y); g.ScaleTransform(Zoom,Zoom);
        DrawCanvasChecker(g,ed.Background.Size);
        g.InterpolationMode=ZoomGestureActive||ed.InteractiveCanvas?InterpolationMode.Bilinear:InterpolationMode.HighQualityBicubic; g.PixelOffsetMode=PixelOffsetMode.HighQuality;
        if(ed.Preview!=null&&!DrawWindowResizePreview(g,ed)&&!DrawTransformSizePreview(g,ed)&&!DrawInteractiveVectors(g,ed)&&!DrawCrispPreview(g,ed,o)&&!DrawNavigationPreview(g,ed)&&!DrawStableRaster(g,ed))g.DrawImage(ed.Preview,0,0,ed.Background.Width,ed.Background.Height);
        ed.DrawTransformSizeOverlay(g);ed.DrawAdjustmentZone(g);ed.DrawHoveredLayer(g);ed.DrawImageSnapGuides(g);ed.DrawCloneCursor(g);
        foreach(var selected in ed.SelectedObjects()) if(selected!=ed.Active) { var sr=selected.Bounds; using(var sp=new Pen(Color.FromArgb(110,168,255),1/Zoom)) { sp.DashStyle=DashStyle.Dash; g.DrawRectangle(sp,sr.X,sr.Y,sr.Width,sr.Height); } }
        if(ed.Marquee.HasValue) { var mr=ed.Marquee.Value; using(var mb=new SolidBrush(Color.FromArgb(35,110,168,255)))g.FillRectangle(mb,mr);using(var mp=new Pen(Color.FromArgb(110,168,255),1/Zoom))g.DrawRectangle(mp,mr.X,mr.Y,mr.Width,mr.Height); }
        Mark sel=ed.Active;
        if(sel!=null&&sel.Kind=="Magnify")ed.DrawMagnifierHandles(g,sel);
        else if(sel!=null&&sel.Kind=="Balloon")ed.DrawBubbleHandles(g,sel);
        else if(sel!=null&&Art.IsArrow(sel.Kind))ed.DrawArrowHandles(g,sel);
        else if(sel!=null) { var r=sel.Bounds; using(var pen=new Pen(Color.FromArgb(110,168,255),1/Zoom)) { pen.DashStyle=DashStyle.Dash; g.DrawRectangle(pen,r.X,r.Y,r.Width,r.Height); } if(!Editor.ImageMovementIsLocked(sel))foreach(var p in Editor.Handles(r))Editor.PaintResizeHandle(g,p,Zoom); }
        if(ed.Crop.HasValue&&ed.IsCutTool) {ed.DrawCutStrip(g);}
        else if(ed.Crop.HasValue) { var r=ed.Crop.Value; using(var reg=new Region(new Rectangle(0,0,ed.Background.Width,ed.Background.Height))) { reg.Exclude(r); using(var br=new SolidBrush(Color.FromArgb(145,0,0,0))) g.FillRegion(br,reg); } using(var pen=new Pen(Color.White,1/Zoom)) g.DrawRectangle(pen,r.X,r.Y,r.Width,r.Height); ed.DrawCropHandles(g); }
        g.ResetTransform();
    }
    protected override void OnResize(EventArgs e){base.OnResize(e);if(OwnerEditor!=null)OwnerEditor.RefreshInlinePlacement();Invalidate();}
    internal bool RedrawsOnResize {get{return GetStyle(ControlStyles.ResizeRedraw);}}
    protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); Focus(); OwnerEditor.CanvasDown(e); }
    protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); OwnerEditor.CanvasMove(e); }
    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); OwnerEditor.CanvasUp(e); }
    protected override void OnMouseWheel(MouseEventArgs e) { base.OnMouseWheel(e); OwnerEditor.ZoomAt(e.Location,e.Delta>0?1.15f:1/1.15f); }
    protected override void OnMouseDoubleClick(MouseEventArgs e) { base.OnMouseDoubleClick(e); if(!OwnerEditor.Crop.HasValue) OwnerEditor.EditText(); }
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
    [DllImport("user32.dll",SetLastError=true)] static extern bool RegisterHotKey(IntPtr h,int id,uint modifiers,uint key);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr h,int id);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h,out NativeRect r);
    [DllImport("dwmapi.dll")] static extern int DwmGetWindowAttribute(IntPtr h,int a,out NativeRect r,int n);
    [StructLayout(LayoutKind.Sequential)] struct NativeRect { public int L,T,R,B; }
    public Editor(string path,bool startInTray=false) {
        DoubleBuffered=true;Text="ShotOut — Screenshot studio"; AutoScaleMode=AutoScaleMode.None; Font=new Font("Segoe UI",13,FontStyle.Regular,GraphicsUnit.Pixel); BackColor=panel; ForeColor=text; MinimumSize=new Size(1100,740); Size=new Size(1320,900); StartPosition=FormStartPosition.CenterScreen; KeyPreview=true; AllowDrop=true;
        Background=Blank(1000,620);ResetCanvasBase(); BuildUI(); Disposed+=delegate {EndDragPreview();Art.ClearLayerImageCache();}; InstallLogo(); InitializeTray(startInTray); LoadPreferences(); RefreshImage();InitializeRecovery();
        Shown+=delegate {RefreshScreenshotBrowser(true); if(!String.IsNullOrEmpty(path)&&File.Exists(path)) OpenPath(path); FitOnOpen(); InitializeReleaseUpdates(); };
        DragEnter+=delegate(object s,DragEventArgs e) { if(e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect=DragDropEffects.Copy; };
        DragDrop+=delegate(object s,DragEventArgs e) { var paths=(string[])e.Data.GetData(DataFormats.FileDrop); if(paths.Length>0) OpenPath(paths[0]); };
        FormClosing+=delegate(object s,FormClosingEventArgs e) { if(HandleTrayClose(e))return; if(!ConfirmAllDocuments()) e.Cancel=true; else SaveSessionPreferences(); };
        FormClosed+=delegate { FinishRecovery(true);DisposeDocuments(); for(int i=1;i<=4;i++) UnregisterHotKey(Handle,i); if(captureTimer!=null) captureTimer.Dispose(); if(Preview!=null) Preview.Dispose(); Background.Dispose(); };
    }
    public static Bitmap Blank(int w,int h) { var b=new Bitmap(w,h); using(var g=Graphics.FromImage(b)) g.Clear(Color.White); return b; }
    Button Button(string label,Action action,int width) { var b=new Button { Text=label,Width=width,Height=34,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(47,51,59),ForeColor=text,Margin=new Padding(4),Cursor=Cursors.Hand,TabStop=false }; b.FlatAppearance.BorderSize=0; b.Click+=delegate { Guard(action); }; return b; }
    void Guard(Action a) { try { a(); } catch(Exception ex) { ErrorDetail.Record(ex);MessageBox.Show(this,ex.Message,"ShotOut",MessageBoxButtons.OK,MessageBoxIcon.Error); } }
    Label Label(string value,int height) { return new Label { Text=value,Height=height,ForeColor=muted,Dock=DockStyle.Top,Padding=new Padding(14,9,4,0) }; }
    void BuildUI() { BuildModernUI(); }
    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); string problem; if(!RegisterCaptureKeys(Preferences.Current,out problem))BeginInvoke((Action)delegate { ReportCaptureShortcutProblem(problem); }); }
    protected override void WndProc(ref Message m) {if(PaintResizeBackground(ref m))return;if(HandleIntegratedChrome(ref m))return; if(m.Msg==0x312) { string action=CaptureActionForShortcut(m.WParam.ToInt32());if(action!=null)Capture(action); } base.WndProc(ref m); }
    public void SetTool(string name) {AcceptCut();
        ClearBackgroundClick();
        FinishInlineText(true);FinishStrokeAdjustment();ResetCloneSource();ClearImageSnapping();CloseImageEffects();
        if(welcome!=null)SetWelcome(false);
        if(name=="Image") { InsertImage(); return; }
        ApplyDrawingDefaults(name);Selected.Clear(); tool=name; RememberVariant(name); if(alignmentChoice!=null&&(name=="Text"||name=="Balloon")){bool previousSync=syncing;syncing=true;int rememberedAlignment;alignmentChoiceIndex=presetAlignments.TryGetValue(name,out rememberedAlignment)?rememberedAlignment:name=="Balloon"?1:0;alignmentChoice.SelectedItem=new[]{"Left","Center","Right"}[alignmentChoiceIndex];syncing=previousSync;}
        if(toolTitle!=null) toolTitle.Text=ToolDisplayName(tool).ToUpperInvariant(); cropGesture=false; Crop=null; Active=null; if(tool=="Crop")StartFullImageCrop(); View.Cursor=tool=="Hand"?Cursors.Hand:tool=="Select"?Cursors.Default:Cursors.Cross; SyncLayers(); View.Invalidate(); UpdateStatus();ShowFirstUseHint();
    }
    void PickColor(bool isFill) { bool fillRole=isFill||PrimaryFillOpacity();using(var dialog=new PaletteDialog(isFill?fill:ink)){dialog.ConfigureOpacity(fillRole?(decimal)fillOpacity.Value:(decimal)Math.Round(ink.A*100d/255),isFill?"Fill opacity":fillRole?"Color opacity":"Stroke / text opacity");if(dialog.ShowDialog(this)==DialogResult.OK)ApplyPaletteColor(isFill,dialog.Chosen,dialog.OpacityPercent);} }
    void ApplyPickedColor(bool isFill,Color color) {bool previous=syncing;syncing=true;if(isFill){fill=color;fillBox.Checked=true;}else ink=color;colorButton.BackColor=ink;fillButton.BackColor=fill;syncing=previous;ChangeStyle();UpdateInspector();}
    void ChangeStyle() { if(syncing||thickness==null||fontSize==null||fillBox==null||boldBox==null||italicBox==null) return;if(inlineTarget!=null){ApplyInlineProperties();return;} if(Active!=null) { Push(); bool textLayoutChanged=Active.Size!=FontSizePixels||Active.Bold!=boldBox.Checked||Active.Italic!=italicBox.Checked; Active.Color=ink.ToArgb(); Active.FillColor=fill.ToArgb(); Active.Width=(float)thickness.Value; float previousTextSize=Active.Size; Active.Size=FontSizePixels; Active.Fill=fillBox.Checked;Active.FillOpacity=(float)fillOpacity.Value; ApplyAlignment(Active); RichLayout.ApplyStyle(Active,boldBox.Checked,italicBox.Checked,underlineBox.Checked); if(Active.Kind=="Rounded") { Active.Kind="Rectangle"; } Active.Radius=(float)cornerRadius.Value; Active.Tail=tailBox.Checked;Active.Shadow=shadowBox.Checked;Active.ShadowSize=(float)shadowSize.Value;Active.ShadowIntensity=(float)shadowIntensity.Value;ApplyMagnifySettings(Active);if(textLayoutChanged)GrowTextBox(Active,previousTextSize);EnsureCanvasFitsMarks(); RefreshImage(); } }
    void SyncStyle() { if(Active==null)return; if(!Selected.Contains(Active)) { Selected.Clear();Selected.Add(Active); } syncing=true; ink=Color.FromArgb(Active.Color); fill=Color.FromArgb(Active.FillColor); colorButton.BackColor=ink; fillButton.BackColor=fill; thickness.Minimum=OptionalOutline(Active.Kind)?0:1; thickness.Value=Math.Max(thickness.Minimum,Math.Min(80,(decimal)Active.Width)); fontSize.Value=TextFonts.Points(Active.Size);SyncFontChoice(Active.FontFamily); fillBox.Checked=Active.Fill;fillOpacity.Value=(decimal)Active.FillOpacity; boldBox.Checked=Active.Bold; italicBox.Checked=Active.Italic; underlineBox.Checked=Active.Underline; cornerRadius.Value=Math.Min(500,Math.Max(0,(decimal)(Active.Kind=="Rounded"?12:Active.Radius))); tailBox.Checked=Active.Tail;shadowBox.Checked=Active.Shadow;shadowSize.Value=(decimal)Active.ShadowSize;shadowIntensity.Value=(decimal)Active.ShadowIntensity;SyncMagnifySettings(Active);SyncAlignment(Active); syncing=false; UpdateInspector(); }
    public void RefreshImage() {TouchTabActivity(); QueuePreview(); }
    void SyncLayers() {SyncLayerRows();UpdateInspector();}
    void UpdateStatus(string message=null) { UpdateSaveHint();documentLabel.Text=fileName+"  ·  "+Background.Width+" × "+Background.Height; SetStatusMessage(message); Text="ShotOut — "+fileName+(dirty?" *":""); if(zoomLabel!=null)zoomLabel.Text=Math.Round(View.Zoom*100)+"%"; UpdateDocumentTitles(); }
    public Project Snapshot() { return new Project { CreatedUtc=documentCreatedUtc,Background=SnapshotImageBytes(),CanvasBase=canvasBase,Marks=Marks.Select(m=>m.Clone()).ToList() }; }
    public void Push([System.Runtime.CompilerServices.CallerMemberName] string operation=null) { FinishCloneStroke();FinishStrokeAdjustment();var before=Snapshot();before.HistoryAction=DescribeHistoryAction(operation);undo.Add(before); if(undo.Count>Preferences.Current.UndoLimit) undo.RemoveAt(0); redo.Clear(); dirty=true;documentCopied=false;TouchTabActivity();UpdateSaveHint(); }
    void Restore(Project p) { documentCreatedUtc=p.CreatedUtc;CloseImageEffects();Background.Dispose(); Background=Art.FromBytes(p.Background);snapshotBitmap=Background;snapshotBytes=p.Background;ReadCanvasBase(p.CanvasBase); Marks=p.Marks.Select(m=>m.Clone()).ToList(); Active=null; Crop=null; step=NextStepNumber(); dirty=true; RefreshImage(); }
    public void Undo() {NavigateHistory(false,1);}
    public void Redo() {NavigateHistory(true,1);}

    public bool ConfirmReplace() { if(!Preferences.Current.ConfirmUnsaved||!dirty)return true; var result=MessageBox.Show(this,"Save your edits before continuing?","Unsaved image",MessageBoxButtons.YesNoCancel,MessageBoxIcon.Question); if(result==DialogResult.Cancel)return false; if(result==DialogResult.Yes)return SaveImage(); return true; }
    public void LoadBitmap(Bitmap b,string name) {documentCopied=false; imageSavePath=projectSavePath=null;documentCreatedUtc=DateTime.UtcNow;Background.Dispose(); Background=b;ResetCanvasBase(); Marks.Clear(); undo.Clear(); redo.Clear(); Active=null; Crop=null; step=1; fileName=name; dirty=false;SetWelcome(false); RefreshImage(); FitOnOpen(); }
    void Open() { using(var d=new OpenFileDialog { Filter="Images and projects|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff;*.shotout;*.snapcanvas|All files|*.*" }) if(d.ShowDialog(this)==DialogResult.OK) OpenPath(d.FileName); }
    void OpenPath(string path) {
        if(Path.GetExtension(path).Equals(".shotout",StringComparison.OrdinalIgnoreCase)||Path.GetExtension(path).Equals(".snapcanvas",StringComparison.OrdinalIgnoreCase)) { Project p; using(var stream=File.OpenRead(path))p=(Project)new XmlSerializer(typeof(Project)).Deserialize(stream);if(p.Background==null||p.Marks==null)throw new InvalidDataException("Invalid project.");var image=Art.FromBytes(p.Background);AddDocument(image,Path.GetFileName(path),false);ReadCanvasBase(p.CanvasBase);Marks=p.Marks.Select(m=>m.Clone()).ToList();step=NextStepNumber();projectSavePath=Path.GetFullPath(path);imageSavePath=null;documentCreatedUtc=p.CreatedUtc;RefreshImage();RecordRecentProject(path); }
        else {using(var im=Image.FromFile(path))AddDocument(new Bitmap(im),Path.GetFileName(path),false,true);imageSavePath=Path.GetFullPath(path);projectSavePath=null;ProjectHistory.RememberAsset(path); }
    }
    public void CopyImage() { FinishBackgroundAdjustment();FinishInlineText(true);using(var b=Art.Render(Background,Marks)) { var data=new DataObject(); data.SetData(DataFormats.Bitmap,true,b); Clipboard.SetDataObject(data,true,10,80); } documentCopied=true;UpdateStatus("Copied image"); }
    void Paste() { PasteFromData(Clipboard.GetDataObject(),null); }
    void PasteFromData(IDataObject data,PointF? at){
        if(data==null){UpdateStatus("Clipboard contains no image or text.");return;}
        if(data.GetDataPresent(AnnotationClipboardFormat)){var copies=ReadCopiedAnnotations(data.GetData(AnnotationClipboardFormat) as string);if(at.HasValue&&copies!=null&&copies.Count>0){float left=copies.Min(m=>m.Bounds.Left),top=copies.Min(m=>m.Bounds.Top);foreach(var mark in copies)mark.Move(at.Value.X-left-15,at.Value.Y-top-15);}PasteCopiedAnnotations(copies);return;}
        if(data.GetDataPresent(DataFormats.Bitmap)){var image=data.GetData(DataFormats.Bitmap) as Image;if(image!=null){if(!at.HasValue&&Marks.Count==0&&fileName=="Untitled"&&!dirty)LoadImageDocument(new Bitmap(image),"Clipboard");else AddImageAt(new Bitmap(image),at??new PointF(20,20));return;}}
        string text=data.GetData(DataFormats.UnicodeText) as string??data.GetData(DataFormats.Text) as string;if(text!=null){var point=at??new PointF(30,30);var mark=NewMark("Text",point);mark.Text=text;mark.W=Math.Max(40,Math.Min(600,Background.Width-point.X));mark.H=Math.Max(30,Math.Min(250,Background.Height-point.Y));GrowTextBox(mark,mark.Size);Push();Marks.Add(mark);Active=mark;EnsureCanvasFitsMarks();RefreshImage();return;}
        UpdateStatus("Clipboard contains no image or text.");
    }
    static bool CanPasteClipboard(){try{return Clipboard.ContainsData(AnnotationClipboardFormat)||Clipboard.ContainsImage()||Clipboard.ContainsText();}catch(System.Runtime.InteropServices.ExternalException){return false;}}

    bool SaveImage(){return SaveCurrentDocument();}
    void SaveProject(){SaveProjectAs();}
    void SaveMenu(){ShowSaveAsMenu();}
    void InsertImage() { using(var d=new OpenFileDialog { Filter="Image|*.png;*.jpg;*.jpeg;*.gif;*.bmp" }) if(d.ShowDialog(this)==DialogResult.OK) using(var im=Image.FromFile(d.FileName)) AddImage(new Bitmap(im)); }
    void AddImage(Bitmap b) {AddImageAt(b,new PointF(20,20));}
    void AddImageAt(Bitmap b,PointF location) { using(b) { Push(); var m=NewMark("Image",location); m.Image=Art.Bytes(b); m.W=b.Width; m.H=b.Height; Marks.Add(m); Active=m; tool="Select";EnsureCanvasFitsMarks(); RefreshImage(); } }
    Mark NewMark(string kind,PointF p) { return new Mark { Kind=kind,X=p.X,Y=p.Y,FontFamily=SelectedFontName,ArrowStyle=defaultArrowStyle,Color=ink.ToArgb(),FillColor=fill.ToArgb(),TextAlignment=NewTextAlignment(kind),Width=(float)thickness.Value,Size=(kind=="Text"||kind=="Balloon")?(float)((manualTextPoints??AdaptiveTextPoints(Background.Size))*4m/3m):FontSizePixels,Fill=fillBox.Checked,FillOpacity=(float)fillOpacity.Value,Bold=boldBox.Checked,Italic=italicBox.Checked,Underline=underlineBox.Checked,Radius=(float)cornerRadius.Value,Tail=tailBox.Checked,Shadow=kind!="Spotlight"&&shadowBox.Checked,ShadowSize=(float)shadowSize.Value,ShadowIntensity=(float)shadowIntensity.Value,Magnification=(float)magnifyPercent.Value/100,MagnifyLinked=magnifyLinked.Checked }; }
    public void CanvasDown(MouseEventArgs e) { CanvasDownWithModifiers(e,ModifierKeys); }
    void CanvasDownWithModifiers(MouseEventArgs e,Keys modifiers) {
        ClearBackgroundClick();FinishInlineText(true);FinishStrokeAdjustment();ClearImageSnapping();
        if(HandleTransformSizeDown(e))return;if(HandleZoneDown(e))return;if(HandleCloneDown(e,modifiers))return;
        if(e.Button==MouseButtons.Middle||(e.Button==MouseButtons.Left&&tool=="Hand")) {if(e.Button==MouseButtons.Left){Selected.Clear();Active=null;SyncLayers();}View.Cursor=Cursors.SizeAll; panning=true; lastScreen=e.Location; View.Capture=true; return; }
        if(e.Button==MouseButtons.Right) { cropGesture=false;View.Capture=false; if(dragging) CancelDrag(); else { Crop=null;var image=HitObject(View.UnboundedPoint(e.Location));if(image==null||image.IsCaptureBase){Active=null;Selected.Clear();}else if(!SelectedObjects().Contains(image)){Active=image;Selected.Clear();Selected.Add(image);}SyncLayers();Context(View.ImagePoint(e.Location)); } return; }
        if(e.Button!=MouseButtons.Left)return;
        start=last=View.ImagePoint(e.Location); View.Capture=true;
        if(tool=="Crop"){BeginCropGesture(View.UnboundedPoint(e.Location));return;}
        if(tool=="Bucket"){View.Capture=false;var point=View.UnboundedPoint(e.Location);if(new RectangleF(PointF.Empty,Background.Size).Contains(point))Guard(delegate{BucketFill(point);});return;}
        bool forceDraw=tool!="Select"&&tool!="Crop"&&!tool.StartsWith("Cut ")&&(modifiers&(Keys.Control|Keys.Alt))!=0;
        int hit=Active==null||ImageInteractionLocked(Active)?-1:HitHandle(start,Active.Bounds);
        Mark clicked=HitObject(start);if(ImageInteractionLocked(clicked))clicked=null;if(clicked!=null&&clicked.Kind=="Grayscale"&&tool!="Select")clicked=null;
        if(!forceDraw&&(tool=="Select"||hit>=0||(clicked!=null&&!tool.StartsWith("Cut ")&&tool!="Crop"))) {
            resizeHandle=hit; resizing=hit>=0&&SelectedObjects().Count<=1;
            if(resizing)resizeOriginal=Active.Clone();
            else if(clicked!=null) {
                if((modifiers&Keys.Shift)!=0) { if(!Selected.Add(clicked))Selected.Remove(clicked); Active=Selected.LastOrDefault(); SyncLayers();View.Invalidate();View.Capture=false;return; }
                if(!Selected.Contains(clicked)) { Selected.Clear();Selected.Add(clicked); } Active=clicked;
            } else { if((modifiers&Keys.Shift)==0)Selected.Clear(); Active=null; Marquee=new RectangleF(start,SizeF.Empty); marqueeDragging=true; SyncLayers();View.Invalidate();return; }
            moving=Active!=null&&!resizing; if(Active!=null) { Push(); SyncStyle(); } SyncLayers();if(moving)BeginImageSnapping(); StartDragPreview(); View.Invalidate(); return;
        }
        if(tool=="Crop"||tool.StartsWith("Cut ")) { cutDirection=0; Crop=new RectangleF(start,SizeF.Empty); dragging=true; return; }
        draft=NewMark(tool,start);if(Art.IsArrow(draft.Kind))draft.ArrowStyle=defaultArrowStyle;
        if(tool=="Text"){var textDraft=draft;draft=null;View.Capture=false;textDraft.W=240;textDraft.H=80;BeginInlineText(textDraft,true);return;}
        if(tool=="Emoji"){draft=null;View.Capture=false;PlaceNativeEmoji(start);return;}
        if(tool=="Step") { float diameter=defaultCircleSize;draft.Text=step.ToString();step=Math.Min(999999,step+1);draft.X-=diameter/2;draft.Y-=diameter/2;draft.W=draft.H=diameter; Push(); Marks.Add(draft); Active=draft; draft=null; View.Capture=false; RefreshImage(); return; }
        if(tool=="Cursor") { draft.W=32; draft.H=44; Push(); Marks.Add(draft); Active=draft; draft=null; View.Capture=false; RefreshImage(); return; }
        if(tool=="Eraser") { int x=Math.Min(Background.Width-1,Math.Max(0,(int)start.X-2)),y=Math.Min(Background.Height-1,Math.Max(0,(int)start.Y-2)); draft.Color=Preview.GetPixel(x,y).ToArgb(); }
        Push(); Marks.Add(draft); Active=draft; draft.Points.Add(start); dragging=true; StartDragPreview();
    }
    static RectangleF Inflate(RectangleF r,float d) { r.Inflate(d,d); return r; }
    public void CanvasMove(MouseEventArgs e) {if(welcomeRequested){View.Cursor=Cursors.Default;return;}if(e.Button==MouseButtons.None&&(moving||resizing||dragging||panning||transformSizeDragging||zoneGesture||clonePainting||cropGesture)){CanvasUp(new MouseEventArgs(MouseButtons.Left,0,e.X,e.Y,0));return;}if(HandleTransformSizeMove(e))return;if(HandleZoneMove(e))return;
        if(inlineTarget!=null)return;
        if(HandleCloneMove(e))return;
        if(pendingBackgroundDrag!=null&&!ContinueBackgroundDrag(e.Location))return;
        if(panning) { View.PanX+=e.X-lastScreen.X; View.PanY+=e.Y-lastScreen.Y; lastScreen=e.Location; View.Invalidate(); return; }
        var p=moving||resizing?View.UnboundedPoint(e.Location):View.ImagePoint(e.Location);
        if(tool=="Crop"&&HandleCropMove(p))return;
        if(marqueeDragging) { Marquee=RectangleF.FromLTRB(Math.Min(start.X,p.X),Math.Min(start.Y,p.Y),Math.Max(start.X,p.X),Math.Max(start.Y,p.Y));View.Invalidate();return; }
        if(moving&&Active!=null) { if(Active.Kind=="Grayscale"&&p!=last)Active.AdjustmentWholeCanvas=false;if(!MoveSnappedImage(p,ModifierKeys))MoveLinkedSelection(p.X-last.X,p.Y-last.Y); last=p; RefreshImage(); return; }
        if(resizing&&Active!=null) { if(Active.Kind=="Grayscale"&&p!=start)Active.AdjustmentWholeCanvas=false;ResizeActive(p); RefreshImage(); return; }
        if(!dragging&&tool=="Hand"){View.Cursor=Cursors.Hand;return;}
        if(!dragging) { int handle=Active==null||ImageInteractionLocked(Active)?-1:HitHandle(p,Active.Bounds); View.Cursor=handle>=100?(handle==103?ArrowRotationCursor.Value:Cursors.Hand):handle<0?(Active!=null&&!ImageInteractionLocked(Active)&&HitMarkup(Active,p,5/View.Zoom)?Cursors.SizeAll:tool=="Select"?Cursors.Default:Cursors.Cross):handle==1||handle==5?Cursors.SizeNS:handle==3||handle==7?Cursors.SizeWE:handle==0||handle==4?Cursors.SizeNWSE:Cursors.SizeNESW; return; }
        if(Crop.HasValue&&tool.StartsWith("Cut ")) {UpdateCutStrip(p);View.Invalidate();return;}
        if(Crop.HasValue) { Crop=RectangleF.FromLTRB(Math.Min(start.X,p.X),Math.Min(start.Y,p.Y),Math.Max(start.X,p.X),Math.Max(start.Y,p.Y)); View.Invalidate(); return; }
        if(draft==null)return;
        if((ModifierKeys&Keys.Shift)!=0) { float dx=p.X-start.X,dy=p.Y-start.Y; if(Art.IsArrow(tool)) { double angle=Math.Round(Math.Atan2(dy,dx)/(Math.PI/4))*(Math.PI/4),len=Math.Sqrt(dx*dx+dy*dy); p=new PointF(start.X+(float)(Math.Cos(angle)*len),start.Y+(float)(Math.Sin(angle)*len)); } else if(SquareMarkup(tool)) { float side=Math.Max(Math.Abs(dx),Math.Abs(dy)); p=new PointF(start.X+side*(dx<0?-1:1),start.Y+side*(dy<0?-1:1)); } }
        draft.W=p.X-start.X; draft.H=p.Y-start.Y; if(draft.Kind=="Magnify") {var area=draft.BodyBounds;draft.SourceX=area.Left+area.Width/2;draft.SourceY=area.Top+area.Height/2;draft.SourceInitialized=true;} if(IsCurved(draft))InitCurve(draft);
        if(tool=="Pen") { AppendPenPoint(p); }
        RefreshImage();
    }
    public void CanvasUp(MouseEventArgs e) {if(EndTransformSizeGesture())return;if(FinishZoneGesture())return; if(pendingBackgroundDrag!=null){ClearBackgroundClick();View.Capture=false;return;}if(inlineTarget!=null)return;if(clonePainting){FinishCloneStroke();return;}ClearImageSnapping();if(e.Button==MouseButtons.Right)return;if(tool=="Crop"&&EndCropGesture())return;if(panning){panning=false;View.Capture=false;View.Cursor=tool=="Hand"?Cursors.Hand:tool=="Select"?Cursors.Default:Cursors.Cross;View.Invalidate();UpdateStatus();return;} if(marqueeDragging) { marqueeDragging=false;foreach(var m in Marks)if(m.Visible&&!m.IsCaptureBase&&Marquee.Value.Width>2/View.Zoom&&Marquee.Value.Height>2/View.Zoom&&Marquee.Value.IntersectsWith(m.Bounds))Selected.Add(m);Active=Marks.LastOrDefault(m=>Selected.Contains(m));Marquee=null;View.Capture=false;SyncStyle();RefreshImage();return; } Mark bubble=draft!=null&&draft.Kind=="Balloon"?draft:null;if(bubble!=null)PrepareBubbleBounds(bubble); if(draft!=null&&(draft.Bounds.Width<1||draft.Bounds.Height<1)) { if(Art.IsArrow(draft.Kind)&&(draft.Bounds.Width>=1||draft.Bounds.Height>=1)) { if(draft.W==0) draft.W=.01f; if(draft.H==0) draft.H=.01f; } else { Marks.Remove(draft); Active=null;Selected.Clear(); } } if(resizing&&Active!=null&&Active.Kind=="Balloon"){Active.BubbleSized=true;GrowTextBox(Active,Active.Size);} if(draft!=null&&draft.Kind=="Magnify"&&draft.MagnifyLinked)PlaceMagnifier(draft); bool finishCut=dragging&&tool.StartsWith("Cut ")&&Crop.HasValue; if(resizing&&Active!=null&&Active.Kind=="Step")RememberDraggedCircleSize(); bool expand=moving||resizing||draft!=null; dragging=moving=resizing=panning=false; draft=null; View.Capture=false; if(expand)EnsureCanvasFitsMarks(); if(finishCut){CommitCrop();return;} RefreshImage();if(bubble!=null&&Marks.Contains(bubble)){BeginInlineText(bubble,false);return;} if(Crop.HasValue)UpdateStatus("Selection ready — Enter / Apply crop to confirm. Esc cancels."); }
    void CancelDrag() {ClearBackgroundClick(); if(clonePainting){CancelCloneStroke();return;}ClearImageSnapping();cropGesture=false;marqueeDragging=false;Marquee=null; if(draft!=null) { Marks.Remove(draft); draft=null; if(undo.Count>0)undo.RemoveAt(undo.Count-1); } dragging=moving=resizing=panning=false; Crop=null; View.Capture=false; RefreshImage(); }
    public void ZoomAt(Point p,float factor) { var o=View.Origin; var image=new PointF((p.X-o.X)/View.Zoom,(p.Y-o.Y)/View.Zoom); float z=Math.Max(.02f,Math.Min(8,View.Zoom*factor));if(z==View.Zoom)return;View.BeginZoomGesture(); View.Zoom=z; View.PanX=p.X-image.X*z-(View.Width-Background.Width*z)/2; View.PanY=p.Y-image.Y*z-(View.Height-Background.Height*z)/2; PositionInlineText();if(inlineTarget!=null)UpdateInlineFont();View.Invalidate(); UpdateZoomStatus(); }
    public void EditText() {if(Active!=null&&Active.Kind=="Emoji"){EditNativeEmoji(Active);return;} if(Active!=null&&(Active.Kind=="Text"||Active.Kind=="Balloon")){BeginInlineText(Active,false);return;}if(Active==null||!(Active.Kind=="Text"||Active.Kind=="Balloon"||Active.Kind=="Step"||Active.Kind=="Emoji"))return; var edited=Active.Clone(); string t=Active.Kind=="Text"||Active.Kind=="Balloon"?RichTextEntry.Ask(this,edited):Prompt.Ask(this,"Edit text",edited.Text,true); if(t!=null) { Push(); Active.Text=t;Active.Runs=edited.Runs;GrowTextBox(Active,Active.Size);EnsureCanvasFitsMarks(); RefreshImage(); } }
    void Delete() { var chosen=SelectedObjects();if(chosen.Count==0)return;Push();foreach(var m in chosen)Marks.Remove(m);Selected.Clear();Active=null;EnsureCanvasFitsMarks();RefreshImage(); }
    void Duplicate() { var chosen=SelectedObjects();if(chosen.Count==0)return;Push();var duplicateLinks=new Dictionary<string,string>();Selected.Clear();foreach(var source in chosen) { var m=source.Clone();if(m.Kind=="Image"){m.ImageAdjustmentId=Guid.NewGuid().ToString("N");m.IsCaptureBase=false;m.ImageMovementLocked=false;}m.IsCloneLayer=false;m.LinkId=DuplicateLink(source.LinkId,duplicateLinks);m.Move(15,15);Marks.Add(m);Selected.Add(m);Active=m; } EnsureCanvasFitsMarks(); RefreshImage(); }
    void Reorder(int delta) { if(Active==null)return; int old=Marks.IndexOf(Active),n=Math.Max(0,Math.Min(Marks.Count-1,old+delta)); if(old==n)return; Push(); Marks.RemoveAt(old); Marks.Insert(n,Active); RefreshImage(); }
    void Context() {Context(null);}
    void Context(PointF? pasteAt) {var menu=CreateAnnotationContextMenu(pasteAt);menu.Show(Cursor.Position);}
    ContextMenuStrip CreateAnnotationContextMenu(PointF? pasteAt) {
        var menu=DarkMenu();var chosen=SelectedObjects();bool selection=chosen.Count>0;
        if(selection)menu.Items.Add("Copy selected annotations",null,delegate{Guard(CopySelection);});
        if(CanPasteClipboard()){var paste=(ToolStripMenuItem)menu.Items.Add("Paste",null,delegate{PopupLifetime.AfterClick(this,delegate{Guard(delegate{PasteFromData(Clipboard.GetDataObject(),pasteAt);});});});paste.ShortcutKeyDisplayString="Ctrl+V";}
        menu.Items.Add("Copy image",null,delegate{Guard(CopyImage);});
        if(undo.Count>0)menu.Items.Add("Undo",null,delegate{Undo();});if(redo.Count>0)menu.Items.Add("Redo",null,delegate{Redo();});
        if(selection){menu.Items.Add(new ToolStripSeparator());menu.Items.Add("Rename layer (F2)",null,delegate{RenameLayer();});menu.Items.Add("Duplicate selected layers",null,delegate{Duplicate();});menu.Items.Add("Delete selected layers",null,delegate{Delete();});}
        using(var advanced=DarkMenu()){if(selection){AddLayerContextActions(advanced);advanced.Items.Add(chosen.All(m=>m.Visible)?"Hide selected layers":"Show selected layers",null,delegate{Push();bool show=chosen.Any(m=>!m.Visible);foreach(var mark in chosen)mark.Visible=show;RefreshImage();});}if(Marks.Count>1)advanced.Items.Add("Flatten all layers",null,delegate{Transform(b=>new Bitmap(b));});foreach(ToolStripItem action in advanced.Items){if(action.Text.StartsWith("Align")&&chosen.Any(ImageMovementIsLocked))action.Enabled=false;if(action.Text=="Flatten into one"&&(chosen.Count<2||chosen.All(m=>new[]{"Grayscale","Blur","Pixelate","Magnify"}.Contains(m.Kind))))action.Enabled=false;if(action.Text.StartsWith("Link layers")&&chosen.All(m=>!String.IsNullOrEmpty(m.LinkId)&&m.LinkId==chosen[0].LinkId))action.Enabled=false;}PruneUnavailableActions(advanced.Items);if(advanced.Items.Count>0){var root=new ToolStripMenuItem("Layer options");while(advanced.Items.Count>0){var item=advanced.Items[0];advanced.Items.RemoveAt(0);root.DropDownItems.Add(item);}menu.Items.Add(new ToolStripSeparator());menu.Items.Add(root);}}
        PruneUnavailableActions(menu.Items);return menu;
    }
    static void PruneUnavailableActions(ToolStripItemCollection items){for(int i=items.Count-1;i>=0;i--){var item=items[i];var option=item as ToolStripMenuItem;if(option!=null&&option.HasDropDownItems){PruneUnavailableActions(option.DropDownItems);if(option.DropDownItems.Count==0)option.Enabled=false;}if(!item.Enabled){items.RemoveAt(i);item.Dispose();}}for(int i=items.Count-1;i>=0;i--)if(items[i] is ToolStripSeparator&&(i==0||i==items.Count-1||items[i-1] is ToolStripSeparator)){var item=items[i];items.RemoveAt(i);item.Dispose();}}

    public void CommitCrop() {
        if(!Crop.HasValue){AcceptCut();return;} Rectangle r=Rectangle.Intersect(Rectangle.Round(Crop.Value),new Rectangle(0,0,Background.Width,Background.Height)); if(r.Width<1||r.Height<1)return;
        if(tool=="Cut rows") { if(r.Height>=Background.Height)return;RememberCut(); Transform(delegate(Bitmap b) { var n=new Bitmap(b.Width,b.Height-r.Height); using(var g=Graphics.FromImage(n)) { if(r.Top>0)g.DrawImage(b,new Rectangle(0,0,b.Width,r.Top),new Rectangle(0,0,b.Width,r.Top),GraphicsUnit.Pixel); if(r.Bottom<b.Height)g.DrawImage(b,new Rectangle(0,r.Top,b.Width,b.Height-r.Bottom),new Rectangle(0,r.Bottom,b.Width,b.Height-r.Bottom),GraphicsUnit.Pixel); } return n; }); }
        else if(tool=="Cut columns") { if(r.Width>=Background.Width)return;RememberCut(); Transform(delegate(Bitmap b) { var n=new Bitmap(b.Width-r.Width,b.Height); using(var g=Graphics.FromImage(n)) { if(r.Left>0)g.DrawImage(b,new Rectangle(0,0,r.Left,b.Height),new Rectangle(0,0,r.Left,b.Height),GraphicsUnit.Pixel); if(r.Right<b.Width)g.DrawImage(b,new Rectangle(r.Left,0,b.Width-r.Right,b.Height),new Rectangle(r.Right,0,b.Width-r.Right,b.Height),GraphicsUnit.Pixel); } return n; }); }
        else ApplyEditableCrop(r);
    }
    public void Transform(Func<Bitmap,Bitmap> operation) { using(var b=Art.Render(Background,Marks)) { Bitmap next=operation(b); if(next==null)return; Push(); Background.Dispose(); Background=next;ResetCanvasBase(); Marks.Clear(); Active=null; Crop=null; RefreshImage(); View.Fit(); } }
    void ImageMenu() {ShowImageEffects(CreateImageEffectsMenu());}
    ContextMenuStrip CreateImageEffectsMenu() {
        var menu=DarkMenu();
        menu.Items.Add("Expand canvas…",null,delegate{Guard(ExpandCanvasDialog);});
        menu.Items.Add("Place original image beside canvas",null,delegate{Guard(delegate{PlaceImageBeside(false);});});
        menu.Items.Add("Place current edited view beside canvas",null,delegate{Guard(delegate{PlaceImageBeside(true);});});
        menu.Items.Add("Resize…",null,delegate{Guard(delegate{var size=ParseSize(Prompt.Ask(this,"Resize width × height",Background.Width+" x "+Background.Height,false));if(size.HasValue)ResizeEditableImage(size.Value);});});
        menu.Items.Add("Rotate 90° clockwise",null,delegate{TransformEditableImage(1);});
        menu.Items.Add("Rotate 90° counterclockwise",null,delegate{TransformEditableImage(3);});
        menu.Items.Add("Flip horizontal",null,delegate{TransformEditableImage(4);});
        menu.Items.Add("Flip vertical",null,delegate{TransformEditableImage(6);});
        menu.Items.Add("Grayscale",null,delegate{AddGrayscaleAdjustment();});
        menu.Items.Add("Background",null,delegate{OpenBackgroundControls();});
        menu.Items.Add("Append image below (manual stitch)…",null,delegate{Guard(delegate{using(var dialog=new OpenFileDialog {Filter="Image|*.png;*.jpg;*.jpeg;*.bmp"})if(dialog.ShowDialog(this)==DialogResult.OK)using(var image=Image.FromFile(dialog.FileName))AppendEditableImage(image);});});
        return menu;
    }
    static Size? ParseSize(string value) { if(value==null)return null; string[] p=value.ToLowerInvariant().Replace('×','x').Split('x'); int w,h; if(p.Length!=2||!int.TryParse(p[0].Trim(),out w)||!int.TryParse(p[1].Trim(),out h)||w<1||h<1||w>16000||h>16000||(long)w*h>40000000)throw new ArgumentException("Use width x height, up to 40 million pixels and 16,000 pixels per side."); return new Size(w,h); }
    void Help(){ShowShortcutGuide();}
    protected override bool ProcessCmdKey(ref Message msg,Keys key) {
        if(HandleUiSizeKey(key))return true;
        if(key==(Keys.Control|Keys.Shift|Keys.T)){Guard(delegate{ReopenClosedTab(null);});return true;}
        if(key==(Keys.Control|Keys.Tab)||key==(Keys.Control|Keys.Shift|Keys.Tab)) { CycleDocument((key&Keys.Shift)!=0?-1:1);return true; }
        if(key==Keys.F1){Help();return true;}if(key==(Keys.Control|Keys.K)){Guard(ShowQuickSearch);return true;}
        if(HandleTransformSizeKey(key)||HandleImageEffectKey(key))return true;
        if(inlineTarget!=null&&inlineInput!=null&&inlineInput.ContainsFocus){if(key==(Keys.Control|Keys.C)&&inlineNew&&inlineInput.TextLength==0){FinishInlineText(true);Guard(CopyImage);return true;}if(HandleInlineKey(key))return true;return base.ProcessCmdKey(ref msg,key);}
        if(ActiveControl is NumberField) { if(key==Keys.Enter||key==Keys.Escape) { View.Focus(); return true; } if(key>=Keys.A&&key<=Keys.Z)View.Focus(); else return base.ProcessCmdKey(ref msg,key); }
        else if(ActiveControl is TextBoxBase)return base.ProcessCmdKey(ref msg,key);
        if(HandleLibraryKey(key))return true;
        if(key==(Keys.Control|Keys.N)||key==(Keys.Control|Keys.T)) {AddWelcomeDocument();return true;}
        if(key==(Keys.Control|Keys.W)) { CloseDocument(currentDocument); return true; }

        if(key==(Keys.Control|Keys.C)) { Guard(CopySelection); return true; }
        if(key==(Keys.Control|Keys.Shift|Keys.C)) { Guard(CopyImage); return true; }
        if(key==(Keys.Control|Keys.V)) { Guard(Paste); return true; }
        if(key==(Keys.Control|Keys.Shift|Keys.S)){Guard(ShowSaveAsMenu);return true;}
        if(key==(Keys.Control|Keys.S)) { Guard(delegate { SaveImage(); }); return true; }
        if(key==(Keys.Control|Keys.O)) { Guard(Open); return true; }
        if(key==(Keys.Control|Keys.Z)) { Undo(); return true; }
        if(key==(Keys.Control|Keys.Y)||key==(Keys.Control|Keys.Shift|Keys.Z)) { Redo(); return true; }
        if(key==(Keys.Control|Keys.D)) { Duplicate(); return true; }
        if(key==Keys.F2) { RenameLayer(); return true; }
        if(key==Keys.Delete) {if(zoneAdjustmentMode)DeleteImageZone();else Delete();return true;}
        if(key==Keys.Escape&&zoneGesture){CancelZoneGeometry();return true;}if(key==Keys.Escape&&tool.StartsWith("Cut ")){CancelCut();return true;} if(key==Keys.Escape) { cropGesture=false; if(clonePainting||dragging||marqueeDragging) CancelDrag(); else { Crop=null; Selected.Clear(); Active=null; SyncLayers(); View.Invalidate(); } return true; }
        if(key==Keys.Enter&&Crop.HasValue) { Guard(CommitCrop); return true; }
        if(key==Keys.PageUp||key==Keys.PageDown) { Reorder(key==Keys.PageUp?1:-1); return true; }
        Keys code=key&Keys.KeyCode;
        if(TryNudgeSelection(key))return true;
        if(key==Keys.OemQuestion||key==Keys.F1) { Help(); return true; }
        if(key==Keys.D0) { View.Fit(); return true; }
        var map=Preferences.Current.ToolMap();
        if(map.ContainsKey(key)) { Guard(delegate { SetTool(map[key]); }); return true; }
        return base.ProcessCmdKey(ref msg,key);
    }
    new void Capture(string mode) {
        if(captureTimer!=null)return;
        bool editorWasVisible=Visible;CloseCapturePreview();Hide(); string captureResult=null;int ms=Math.Max(mode=="Window"?800:250,(int)delay.Value*1000); captureTimer=new System.Windows.Forms.Timer { Interval=ms };
        captureTimer.Tick+=delegate { captureTimer.Stop(); try {
            Rectangle bounds=DesktopDpi.ScreenBounds();
            using(var shot=new Bitmap(bounds.Width,bounds.Height)) {
                using(var g=Graphics.FromImage(shot)) { g.CopyFromScreen(bounds.Location,Point.Empty,bounds.Size,CopyPixelOperation.SourceCopy); if(cursorBox.Checked) DrawCursor(g,bounds.Location); }
                if(mode=="Screen") { captureResult=CompleteCapture(new Bitmap(shot)); }
                else using(var selector=new CaptureOverlay(shot,bounds,mode=="Window")) { if(selector.ShowDialog()==DialogResult.OK) { var area=selector.Selection; captureResult=CompleteCapture(shot.Clone(area,PixelFormat.Format32bppArgb)); } }
            }
        } catch(Exception ex) { MessageBox.Show(ex.Message,"Capture failed"); } finally { FinishCaptureVisibility(captureResult,editorWasVisible); UpdateStatus(captureResult); captureTimer.Dispose(); captureTimer=null; } };
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
    public CaptureOverlay(Bitmap image,Rectangle bounds,bool pickWindow,Func<Point,Rectangle> picker=null) { smart=Preferences.Current.SmartRegion;selectionPicker=picker;shot=image; origin=bounds.Location; window=pickWindow;AutoScaleMode=AutoScaleMode.None;captureBounds=bounds; FormBorderStyle=FormBorderStyle.None; StartPosition=FormStartPosition.Manual; Bounds=bounds; TopMost=true; ShowInTaskbar=false; DoubleBuffered=true; Cursor=Cursors.Cross; KeyPreview=true; self=Handle;InstallElementSelection();InstallCaptureCancellation();Shown+=delegate{if(window||smart)PickWindow(PointToClient(Cursor.Position));}; KeyDown+=delegate(object s,KeyEventArgs e) { if(e.KeyCode==Keys.Escape) {e.Handled=e.SuppressKeyPress=true;CancelCapture();} }; }
    protected override void OnPaint(PaintEventArgs e) { e.Graphics.DrawImageUnscaled(shot,0,0); using(var reg=new Region(ClientRectangle)) { if(!Selection.IsEmpty)reg.Exclude(Selection); using(var shade=new SolidBrush(Color.FromArgb(110,0,0,0)))e.Graphics.FillRegion(shade,reg); } if(!Selection.IsEmpty) { using(var pen=new Pen(Color.FromArgb(111,173,255),2))e.Graphics.DrawRectangle(pen,Selection); } using(var f=new Font("Segoe UI",14)) { string t=CaptureHint(); var s=e.Graphics.MeasureString(t,f); e.Graphics.FillRectangle(Brushes.Black,25,25,s.Width+24,s.Height+18); e.Graphics.DrawString(t,f,Brushes.White,37,34); } }
    protected override void OnMouseDown(MouseEventArgs e) { if(e.Button==MouseButtons.Right){CancelCapture();return;}if(e.Button!=MouseButtons.Left)return;BeginSelection(e.Location,(ModifierKeys&Keys.Alt)==0); }

    void PickWindow(Point p) {
        if(selectionPicker!=null){Selection=Rectangle.Intersect(ClientRectangle,selectionPicker(p));return;}
        Selection=Rectangle.Empty;Point screen=new Point(p.X+origin.X,p.Y+origin.Y);IntPtr h=GetWindow(self,2);int count=0;
        while(h!=IntPtr.Zero&&count++<1000){Rectangle bounds;if(CaptureVisibility.TryBounds(h,screen,out bounds)){var local=bounds;local.Offset(-origin.X,-origin.Y);Selection=Rectangle.Intersect(ClientRectangle,local);TrackElements(p,h,bounds);return;}h=GetWindow(h,2);}
        TrackElements(p,IntPtr.Zero,Rectangle.Empty);
    }
    protected override void OnMouseMove(MouseEventArgs e) {MoveSelection(e.Location,(ModifierKeys&Keys.Alt)==0);}
    protected override void OnMouseUp(MouseEventArgs e) {if(e.Button==MouseButtons.Left)EndSelection(e.Location);}

}
static class Prompt {
    public static string Ask(IWin32Window owner,string title,string value,bool multiline) {
        using(var f=new ApplyPrompt { Text=title,Size=new Size(450,multiline?260:160),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,BackColor=Color.FromArgb(34,37,43),ForeColor=Color.White,Font=new Font("Segoe UI",10) }) {
            var input=new ThemedTextBox { Text=value,Multiline=multiline,AcceptsReturn=multiline,ScrollBars=multiline?ScrollBars.Vertical:ScrollBars.None,Location=new Point(16,18),Size=new Size(400,multiline?140:30),BackColor=Color.FromArgb(47,51,59),ForeColor=Color.White,BorderStyle=BorderStyle.FixedSingle };
            var ok=new Button { Text="Apply",DialogResult=DialogResult.OK,Location=new Point(238,multiline?170:65),Size=new Size(85,32) }; var cancel=new Button { Text="Cancel",DialogResult=DialogResult.Cancel,Location=new Point(331,multiline?170:65),Size=new Size(85,32) }; if(multiline)f.Controls.Add(new Label { Text="Ctrl+Enter to apply",ForeColor=Color.FromArgb(160,170,190),Location=new Point(16,177),Size=new Size(200,24),Font=new Font("Segoe UI",12,FontStyle.Regular,GraphicsUnit.Pixel) }); input.KeyDown+=delegate(object sender,KeyEventArgs e) { if(e.Control&&e.KeyCode==Keys.Enter) { e.SuppressKeyPress=e.Handled=true;ok.PerformClick(); } }; f.Controls.Add(input); f.Controls.Add(ok); f.Controls.Add(cancel); f.AcceptButton=ok; f.CancelButton=cancel; f.Shown+=delegate { input.Focus(); input.SelectAll(); }; return f.ShowDialog(owner)==DialogResult.OK?input.Text:null;
        }
    }
}
static class Tests {
    static void Check(bool ok,string name,List<string> report) { if(!ok)throw new Exception("FAILED: "+name); report.Add("PASS "+name); }
    public static void Run(string folder) {
        Preferences.Testing=true;Preferences.Current.TextOptionsExpanded=Preferences.Current.GeometryExpanded=Preferences.Current.StylingExpanded=true;Directory.CreateDirectory(folder); var report=new List<string>();
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
                ed.Show(); Application.DoEvents(); ed.View.Fit(); ed.RefreshImage(); Application.DoEvents(); using(var preview=new Bitmap(ed.Width,ed.Height)) { ed.DrawToBitmap(preview,new Rectangle(Point.Empty,preview.Size)); preview.Save(Path.Combine(folder,"interface.png")); } File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyModernUI");ed.VerifyModernUI(report,folder); ed.Hide();
                ed.Crop=new RectangleF(20,20,400,300); ed.CommitCrop(); Check(ed.Background.Width==400&&ed.Background.Height==300&&ed.Marks.Count==count,"Crop retains editable layers at correct dimensions",report); ed.Undo(); Check(ed.Background.Width==900&&ed.Marks.Count==count,"Undo crop restores image and annotations",report);
                ed.LoadBitmap(Editor.Blank(100,100),"Test"); ed.SetTool("Cut rows"); ed.Crop=new RectangleF(0,20,100,30); ed.CommitCrop(); Check(ed.Background.Height==70,"Cut rows",report); ed.Undo(); ed.SetTool("Cut columns"); ed.Crop=new RectangleF(20,0,30,100); ed.CommitCrop(); Check(ed.Background.Width==70,"Cut columns",report);
                File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyRefinements");ed.VerifyRefinements(report); File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyFixes");ed.VerifyFixes(report); File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyCtrlDraw");ed.VerifyCtrlDraw(report); File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyArrows");ed.VerifyArrows(report); File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifySettingsAndText");ed.VerifySettingsAndText(report,folder); File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyDocuments");ed.VerifyDocuments(report,folder);ed.VerifyTabTransitions(report,folder); File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyLogoChanges");ed.VerifyLogoChanges(report,folder); File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyDragPerformance");ed.VerifyDragPerformance(report,folder); File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyShadowUpdate");ed.VerifyShadowUpdate(report,folder); File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyShotOut");ed.VerifyShotOut(report,folder); File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyPanelAndOutlines");ed.VerifyPanelAndOutlines(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyWorkspaceUpdate");ed.VerifyWorkspaceUpdate(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyInteractionPolish");ed.VerifyInteractionPolish(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyDesktopUpdate");ed.VerifyDesktopUpdate(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifySizingUpdate");ed.VerifySizingUpdate(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyCaptureConvenience");ed.VerifyCaptureConvenience(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyDpiAndBucket");ed.VerifyDpiAndBucket(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyImageLayerResize");ed.VerifyImageLayerResize(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyChromeOpacity");ed.VerifyChromeOpacity(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyModalFrameFix");ed.VerifyModalFrameFix(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyResizeTools");ed.VerifyResizeTools(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyMarkupQuality");ed.VerifyMarkupQuality(report,folder); File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyEditableCrop");ed.VerifyEditableCrop(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyShiftMarkup");ed.VerifyShiftMarkup(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyCaptureAndEffects");ed.VerifyCaptureAndEffects(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyEditableEffects");ed.VerifyEditableEffects(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyRecovery");ed.VerifyRecovery(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyReactiveMarkup");ed.VerifyReactiveMarkup(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyReleaseTools");ed.VerifyReleaseTools(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyClonePixelQuality");ed.VerifyClonePixelQuality(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyNavigationPerformance");ed.VerifyNavigationPerformance(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyDocumentationTools");ed.VerifyDocumentationTools(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyMenuStability");ed.VerifyMenuStability(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyInlineText");ed.VerifyInlineText(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyAnnotationClipboard");ed.VerifyAnnotationClipboard(report);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyDisplayQuality");ed.VerifyDisplayQuality(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyTextWorkflow");ed.VerifyTextWorkflow(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyLayerAndCirclePolish");ed.VerifyLayerAndCirclePolish(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyTextKeys");ed.VerifyTextKeys(report);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyInlineTextColor");ed.VerifyInlineTextColor(report);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyExactGeometry");ed.VerifyExactGeometry(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifySidebarAndNudging");ed.VerifySidebarAndNudging(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyColorOpacity");ed.VerifyColorOpacity(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyPanPerformance");ed.VerifyPanPerformance(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyPerformanceAudit");ed.VerifyPerformanceAudit(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyPerformanceSafety");ed.VerifyPerformanceSafety(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyHighZoomInteraction");ed.VerifyHighZoomInteraction(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyAdaptiveStroke");ed.VerifyAdaptiveStroke(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyReleasePolish");ed.VerifyReleasePolish(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyImageAdjustments");ed.VerifyImageAdjustments(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyZoneAndBackground");ed.VerifyZoneAndBackground(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyWorkspacePolish");ed.VerifyWorkspacePolish(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyColorNumberCanvas");ed.VerifyColorNumberCanvas(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifyProjectSearch");ed.VerifyProjectSearch(report,folder);File.WriteAllText(Path.Combine(folder,"current-check.txt"),"VerifySavedLibrary");ed.VerifySavedLibrary(report,folder);ed.VerifyBrowserActions(report,folder);ed.VerifyToolsAndClosedTabs(report,folder);ed.VerifySpeechBalloonStability(report,folder);ed.VerifyPrintScreenShortcut(report,folder);ed.VerifyPrintScreenSetup(report,folder);ed.VerifyReleaseUpdates(report,folder);ed.VerifyTabPerformance(report,folder);ed.VerifyTextWindowResize(report,folder);ed.VerifyRecentTabOrder(report,folder);ed.VerifyOutputQuality(report,folder);ed.VerifyWindowResizePolish(report,folder); ed.LoadBitmap(Editor.Blank(100,100),"Test");
            }
            report.Add("All automated checks passed; interactive validation remains outstanding."); File.WriteAllLines(Path.Combine(folder,"test-results.txt"),report);
        } catch(Exception ex) { report.Add(ex.ToString()); File.WriteAllLines(Path.Combine(folder,"test-results.txt"),report); Environment.ExitCode=1; }
    }
}
}











