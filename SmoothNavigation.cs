using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace ShotOut {
partial class Canvas {
    Timer zoomSettleTimer;
    bool zoomGestureActive;
    TextureBrush canvasChecker;
    Bitmap navigationPreview,navigationSource;
    internal int NavigationPreviewBuilds;
    internal bool ZoomGestureActive {get{return zoomGestureActive;}}
    internal void BeginZoomGesture(){
        if(IsDisposed||Disposing)return;
        if(zoomSettleTimer==null){zoomSettleTimer=new Timer {Interval=180};zoomSettleTimer.Tick+=delegate{FinishZoomGesture();};}
        zoomGestureActive=true;zoomSettleTimer.Stop();zoomSettleTimer.Start();
    }
    internal void FinishZoomGesture(){if(zoomSettleTimer!=null)zoomSettleTimer.Stop();if(!zoomGestureActive)return;zoomGestureActive=false;Invalidate();}
    void DrawCanvasChecker(Graphics graphics,Size size){
        if(canvasChecker==null){using(var image=new Bitmap(20,20)){using(var painter=Graphics.FromImage(image)){painter.Clear(Color.White);painter.FillRectangle(Brushes.Gainsboro,0,0,10,10);painter.FillRectangle(Brushes.Gainsboro,10,10,10,10);}canvasChecker=new TextureBrush(image);}}
        graphics.FillRectangle(canvasChecker,0,0,size.Width,size.Height);
    }
    void ClearNavigationPreview(){if(navigationPreview!=null)navigationPreview.Dispose();navigationPreview=navigationSource=null;}
    bool DrawNavigationPreview(Graphics graphics,Editor editor){
        if(!ZoomGestureActive||!editor.StableMarkupPreview||editor.Preview==null)return false;
        if(navigationPreview==null||!Object.ReferenceEquals(navigationSource,editor.Preview)){
            ClearNavigationPreview();float scale=Math.Min(1,(float)Math.Sqrt(1000000d/((long)editor.Preview.Width*editor.Preview.Height)));
            int width=Math.Max(1,(int)(editor.Preview.Width*scale)),height=Math.Max(1,(int)(editor.Preview.Height*scale));
            navigationPreview=new Bitmap(width,height,PixelFormat.Format32bppPArgb);
            using(var painter=Graphics.FromImage(navigationPreview)){painter.CompositingMode=CompositingMode.SourceCopy;painter.InterpolationMode=InterpolationMode.Bilinear;painter.PixelOffsetMode=PixelOffsetMode.HighQuality;painter.DrawImage(editor.Preview,new Rectangle(0,0,width,height));}
            navigationSource=editor.Preview;NavigationPreviewBuilds++;
        }
        graphics.DrawImage(navigationPreview,0,0,editor.Background.Width,editor.Background.Height);return true;
    }
    void DisposeNavigation(){ClearNavigationPreview();if(zoomSettleTimer!=null){zoomSettleTimer.Stop();zoomSettleTimer.Dispose();zoomSettleTimer=null;}if(canvasChecker!=null){canvasChecker.Dispose();canvasChecker=null;}}
}
partial class Editor {
    public bool InteractiveCanvas {get{return panning||!StableMarkupPreview;}}
    void UpdateZoomStatus(){status.Text=tool+"  ·  "+Math.Round(View.Zoom*100)+"%  ·  "+Marks.Count+" annotations     |     Ctrl+C copy image  ·  Ctrl+V paste  ·  Ctrl+Z undo  ·  ? shortcuts";if(zoomLabel!=null)zoomLabel.Text=Math.Round(View.Zoom*100)+"%";}
    public void VerifyNavigationPerformance(List<string> report,string folder){
        Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);report.Add("PASS "+label);};
        LoadImageDocument(Blank(3840,2160),"4K zoom benchmark");
        using(var sample=Blank(3840,2160)){using(var painter=Graphics.FromImage(sample))painter.Clear(Color.FromArgb(238,242,248));Marks[0].Image=Art.Bytes(sample);}
        for(int i=0;i<32;i++)Marks.Add(new Mark {Kind=i%2==0?"Arrow":"Rectangle",X=80+i*90,Y=100+i*42,W=260,H=140,Width=4});
        Marks.Add(new Mark {Kind="Text",Text="Sharp markup after zoom",X=300,Y=1400,W=1300,H=130,Size=70});RefreshImage();SetTool("Select");Show();Application.DoEvents();View.FinishZoomGesture();
        var preview=Preview;var background=Background;int history=undo.Count;byte[] pixels=Art.Bytes(Preview);var pointer=new Point(View.Width/2+53,View.Height/2-37);View.Zoom=.35f;View.PanX=View.PanY=0;var anchor=View.UnboundedPoint(pointer);
        using(var frame=new Bitmap(View.Width,View.Height)){
            var clock=Stopwatch.StartNew();int before=View.CrispPreviewBuilds,navBuilds=View.NavigationPreviewBuilds;
            for(int i=0;i<8;i++){View.Zoom=.30f+i*.015f;View.DrawToBitmap(frame,new Rectangle(Point.Empty,frame.Size));}
            clock.Stop();double previous=clock.Elapsed.TotalMilliseconds/8;check(View.CrispPreviewBuilds>=before+8,"Zoom benchmark exercises full sharp-preview rebuilds");
            View.Zoom=.35f;View.PanX=View.PanY=0;before=View.CrispPreviewBuilds;clock.Restart();
            for(int i=0;i<8;i++){ZoomAt(pointer,1.04f);View.DrawToBitmap(frame,new Rectangle(Point.Empty,frame.Size));}
            clock.Stop();double smooth=clock.Elapsed.TotalMilliseconds/8;
            check(View.NavigationPreviewBuilds==navBuilds+1,"Zoom reuses one bounded premultiplied image cache across wheel steps");check(View.ZoomGestureActive&&View.CrispPreviewBuilds==before,"Continuous zoom skips expensive markup recomposition");
            var actual=View.UnboundedPoint(pointer);check(Math.Abs(actual.X-anchor.X)<.01&&Math.Abs(actual.Y-anchor.Y)<.01,"Zoom preserves the image position under the cursor");
            View.FinishZoomGesture();View.DrawToBitmap(frame,new Rectangle(Point.Empty,frame.Size));check(View.HasCrispPreview&&View.CrispPreviewBuilds==before+1,"Sharp markup returns once at the final zoom level");
            View.PanX+=24;View.DrawToBitmap(frame,new Rectangle(Point.Empty,frame.Size));check(View.CrispPreviewBuilds==before+1,"Panning reuses the settled sharp preview");
            View.BeginZoomGesture();var wait=Stopwatch.StartNew();while(View.ZoomGestureActive&&wait.ElapsedMilliseconds<1000){Application.DoEvents();System.Threading.Thread.Sleep(5);}check(!View.ZoomGestureActive,"Zoom idle timer restores sharp rendering automatically");
            report.Add("4K zoom benchmark: full sharp rebuild "+previous.ToString("F2")+" ms/frame; interactive zoom "+smooth.ToString("F2")+" ms/frame.");File.WriteAllText(Path.Combine(folder,"zoom-benchmark.txt"),"Synthetic 4K screenshot with 33 markup objects, 8 frames.\r\nFull sharp rebuild: "+previous.ToString("F2")+" ms/frame\r\nInteractive zoom: "+smooth.ToString("F2")+" ms/frame\r\nIncludes canvas painting, not physical display latency.");
        }
        check(Object.ReferenceEquals(preview,Preview)&&Object.ReferenceEquals(background,Background)&&undo.Count==history&&pixels.Length==Art.Bytes(Preview).Length&&pixels.SequenceEqual(Art.Bytes(Preview)),"Navigation preserves native image pixels, layers, and undo history");
        int navCacheBuilds=View.NavigationPreviewBuilds;var stable=Snapshot();Marks.Add(new Mark {Kind="Pixelate",X=100,Y=100,W=200,H=150,Width=8});Marks.Add(new Mark {Kind="Grayscale",AdjustmentWholeCanvas=true,FillOpacity=50});RefreshImage();View.BeginZoomGesture();using(var frame=new Bitmap(View.Width,View.Height))View.DrawToBitmap(frame,new Rectangle(Point.Empty,frame.Size));check(View.NavigationPreviewBuilds>navCacheBuilds,"Editing invalidates the interactive zoom image cache");View.FinishZoomGesture();using(var expected=Art.Render(Background,Marks))check(Art.Bytes(expected).SequenceEqual(Art.Bytes(Preview)),"Zoom retains the composited privacy and adjustment effects");Restore(stable);View.FinishZoomGesture();Hide();LoadBitmap(Blank(100,100),"Test");
    }
}
}
