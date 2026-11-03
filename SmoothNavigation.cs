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
    float checkerZoom;
    Bitmap navigationPreview,navigationSource;
    Bitmap panSource,panTile;
    Rectangle panArea;
    ImageAttributes navigationEdges;
    internal int NavigationPreviewBuilds;
    internal int PanPreviewFrames;
    internal long PanCachePixels {get{return panTile==null?0:(long)panTile.Width*panTile.Height;}}
    internal bool ZoomGestureActive {get{return zoomGestureActive;}}
    internal void BeginZoomGesture(){
        if(IsDisposed||Disposing)return;
        if(zoomSettleTimer==null){zoomSettleTimer=new Timer {Interval=180};zoomSettleTimer.Tick+=delegate{FinishZoomGesture();};}
        zoomGestureActive=true;zoomSettleTimer.Stop();zoomSettleTimer.Start();
    }
    internal void FinishZoomGesture(){if(zoomSettleTimer!=null)zoomSettleTimer.Stop();if(!zoomGestureActive)return;zoomGestureActive=false;Invalidate();}
    void DrawCanvasChecker(Graphics graphics,Size size){
        if(canvasChecker==null||checkerZoom!=Zoom){if(canvasChecker!=null)canvasChecker.Dispose();int cell=Math.Max(1,(int)Math.Round(10*Zoom));using(var image=new Bitmap(cell*2,cell*2,PixelFormat.Format32bppPArgb)){using(var painter=Graphics.FromImage(image)){painter.Clear(Color.FromArgb(35,38,44));using(var shade=new SolidBrush(Color.FromArgb(42,45,52))){painter.FillRectangle(shade,0,0,cell,cell);painter.FillRectangle(shade,cell,cell,cell,cell);}}canvasChecker=new TextureBrush(image);}checkerZoom=Zoom;}
        var origin=Origin;var visible=RectangleF.Intersect(new RectangleF(origin.X,origin.Y,size.Width*Zoom,size.Height*Zoom),ClientRectangle);if(visible.Width<=0||visible.Height<=0)return;var state=graphics.Save();try{graphics.ResetTransform();canvasChecker.ResetTransform();canvasChecker.TranslateTransform((float)Math.Round(origin.X),(float)Math.Round(origin.Y));graphics.FillRectangle(canvasChecker,visible);}finally{graphics.Restore(state);}
    }
    void ClearPanTile(){if(panTile!=null)panTile.Dispose();panTile=panSource=null;panArea=Rectangle.Empty;}
    internal void VerifyDarkTransparency(List<string> report,string folder){
        Zoom=1;PanX=PanY=0;using(var frame=new Bitmap(Width,Height)){using(var graphics=Graphics.FromImage(frame)){graphics.Clear(BackColor);DrawCanvasChecker(graphics,OwnerEditor.Background.Size);var cached=canvasChecker;DrawCanvasChecker(graphics,OwnerEditor.Background.Size);if(!Object.ReferenceEquals(cached,canvasChecker))throw new Exception("Checker texture was rebuilt without a zoom change");}var origin=Origin;var first=frame.GetPixel((int)Math.Round(origin.X)+5,(int)Math.Round(origin.Y)+5);var second=frame.GetPixel((int)Math.Round(origin.X)+15,(int)Math.Round(origin.Y)+5);if(first.ToArgb()!=Color.FromArgb(42,45,52).ToArgb()||second.ToArgb()!=Color.FromArgb(35,38,44).ToArgb())throw new Exception("Transparency backdrop is not the subtle charcoal palette");frame.Save(Path.Combine(folder,"dark-transparency-canvas.png"));}report.Add("PASS Dark transparency checks use two charcoal shades and reuse the existing cached texture");
    }
    void ClearNavigationPreview(){if(navigationPreview!=null)navigationPreview.Dispose();navigationPreview=navigationSource=null;ClearPanTile();}
    bool DrawNavigationPreview(Graphics graphics,Editor editor){
        // Panning reads only the visible source pixels from the already-composited image.
        // It never rebuilds vector tiles or creates an enlarged image per mouse move.
        if(editor.CanvasPanning&&Zoom>=1&&editor.Preview!=null){DrawPanPixels(graphics,editor);PanPreviewFrames++;return true;}
        if((!ZoomGestureActive&&!editor.CanvasPanning)||!editor.StableMarkupPreview||editor.Preview==null)return false;
        if(navigationPreview==null||!Object.ReferenceEquals(navigationSource,editor.Preview)){
            ClearNavigationPreview();float scale=Math.Min(1,(float)Math.Sqrt(1000000d/((long)editor.Preview.Width*editor.Preview.Height)));
            int width=Math.Max(1,(int)(editor.Preview.Width*scale)),height=Math.Max(1,(int)(editor.Preview.Height*scale));
            navigationPreview=new Bitmap(width,height,PixelFormat.Format32bppPArgb);
            using(var painter=Graphics.FromImage(navigationPreview)){painter.CompositingMode=CompositingMode.SourceCopy;painter.InterpolationMode=InterpolationMode.Bilinear;painter.PixelOffsetMode=PixelOffsetMode.HighQuality;painter.DrawImage(editor.Preview,new Rectangle(0,0,width,height));}
            navigationSource=editor.Preview;NavigationPreviewBuilds++;
        }
        DrawVisibleNavigation(graphics,navigationPreview,editor.Background.Size);if(editor.CanvasPanning)PanPreviewFrames++;return true;
    }
    void DrawPanPixels(Graphics graphics,Editor editor){
        var origin=Origin;var visible=RectangleF.Intersect(new RectangleF(origin.X,origin.Y,editor.Background.Width*Zoom,editor.Background.Height*Zoom),ClientRectangle);if(visible.Width<=0||visible.Height<=0)return;
        var needed=Rectangle.Intersect(Rectangle.FromLTRB((int)Math.Floor((visible.Left-origin.X)/Zoom),(int)Math.Floor((visible.Top-origin.Y)/Zoom),(int)Math.Ceiling((visible.Right-origin.X)/Zoom),(int)Math.Ceiling((visible.Bottom-origin.Y)/Zoom)),new Rectangle(Point.Empty,editor.Preview.Size));if(needed.Width<=0||needed.Height<=0)return;
        if(panTile==null||!Object.ReferenceEquals(panSource,editor.Preview)||!panArea.Contains(needed)){
            ClearPanTile();panArea=needed;panArea.Inflate(64,64);panArea=Rectangle.Intersect(panArea,new Rectangle(Point.Empty,editor.Preview.Size));panTile=editor.Preview.Clone(panArea,PixelFormat.Format32bppPArgb);panSource=editor.Preview;
        }
        var source=new RectangleF((visible.Left-origin.X)/Zoom-panArea.Left,(visible.Top-origin.Y)/Zoom-panArea.Top,visible.Width/Zoom,visible.Height/Zoom);DrawNavigationRegion(graphics,panTile,visible,source,true);
    }
    void DrawVisibleNavigation(Graphics graphics,Bitmap image,Size canvas){
        var origin=Origin;float width=canvas.Width*Zoom,height=canvas.Height*Zoom;var visible=RectangleF.Intersect(new RectangleF(origin.X,origin.Y,width,height),ClientRectangle);if(visible.Width<=0||visible.Height<=0)return;
        var source=new RectangleF((visible.Left-origin.X)*image.Width/width,(visible.Top-origin.Y)*image.Height/height,visible.Width*image.Width/width,visible.Height*image.Height/height);
        DrawNavigationRegion(graphics,image,visible,source);
    }
    void DrawNavigationRegion(Graphics graphics,Bitmap image,RectangleF visible,RectangleF source,bool pan=false){
        if(navigationEdges==null){navigationEdges=new ImageAttributes();navigationEdges.SetWrapMode(WrapMode.TileFlipXY);}
        if(!pan){DrawZoomDisplay(graphics,image,visible,source);return;}var state=graphics.Save();try{graphics.ResetTransform();graphics.InterpolationMode=pan?InterpolationMode.NearestNeighbor:InterpolationMode.Bilinear;graphics.PixelOffsetMode=pan?PixelOffsetMode.Half:PixelOffsetMode.HighQuality;graphics.CompositingQuality=CompositingQuality.HighSpeed;graphics.DrawImage(image,new[]{visible.Location,new PointF(visible.Right,visible.Top),new PointF(visible.Left,visible.Bottom)},source,GraphicsUnit.Pixel,navigationEdges);}finally{graphics.Restore(state);}
    }
    void DisposeNavigation(){ClearNavigationPreview();ClearZoomDisplay();ClearFastZoomSamples();ClearInteractiveDisplay();if(navigationEdges!=null){navigationEdges.Dispose();navigationEdges=null;}if(zoomSettleTimer!=null){zoomSettleTimer.Stop();zoomSettleTimer.Dispose();zoomSettleTimer=null;}if(canvasChecker!=null){canvasChecker.Dispose();canvasChecker=null;}}
}
partial class Editor {
    internal bool CanvasPanning {get{return panning;}}
    public bool InteractiveCanvas {get{return panning||!StableMarkupPreview;}}
    void UpdateZoomStatus(){SetStatusMessage(null);if(zoomLabel!=null)zoomLabel.Text=Math.Round(View.Zoom*100)+"%";}
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




