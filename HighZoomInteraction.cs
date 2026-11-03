using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace ShotOut {
partial class Canvas {
    internal void VerifyFastZoomQuality(List<string> report){
        using(var sample=new Bitmap(128,96,PixelFormat.Format32bppPArgb)){
            for(int y=0;y<96;y++)for(int x=0;x<128;x++)sample.SetPixel(x,y,Color.FromArgb(80+(x+y)%176,(x*17+y*7)%256,(x*3+y*13)%256,(x*5+y*11)%256));var source=new RectangleF(8.25f,6.5f,87.4f,62.3f);var visible=new RectangleF(0,0,Width,Height);
            using(var expected=new Bitmap(Width,Height,PixelFormat.Format32bppPArgb)){
                using(var graphics=Graphics.FromImage(expected)){graphics.Clear(Color.FromArgb(74,86,99));graphics.InterpolationMode=InterpolationMode.Bilinear;graphics.PixelOffsetMode=PixelOffsetMode.HighQuality;graphics.CompositingQuality=CompositingQuality.HighSpeed;graphics.DrawImage(sample,new[]{visible.Location,new PointF(visible.Right,0),new PointF(0,visible.Bottom)},source,GraphicsUnit.Pixel);}
                ClearZoomDisplay();zoomDisplayFrame=new Bitmap(Width,Height,PixelFormat.Format32bppPArgb);using(var graphics=Graphics.FromImage(zoomDisplayFrame))graphics.Clear(Color.FromArgb(74,86,99));TryDrawBilinearZoom(sample,visible,source);var a=expected.LockBits(new Rectangle(Point.Empty,expected.Size),ImageLockMode.ReadOnly,PixelFormat.Format32bppPArgb);var b=zoomDisplayFrame.LockBits(new Rectangle(Point.Empty,zoomDisplayFrame.Size),ImageLockMode.ReadOnly,PixelFormat.Format32bppPArgb);int maximum=0;double total=0;try{var first=new int[Width*Height];var second=new int[first.Length];Marshal.Copy(a.Scan0,first,0,first.Length);Marshal.Copy(b.Scan0,second,0,second.Length);for(int i=0;i<first.Length;i++)for(int shift=0;shift<24;shift+=8){int error=Math.Abs(((first[i]>>shift)&255)-((second[i]>>shift)&255));maximum=Math.Max(maximum,error);total+=error;}}finally{expected.UnlockBits(a);zoomDisplayFrame.UnlockBits(b);}report.Add("Bilinear zoom comparison: max channel rounding difference "+maximum+"; mean "+(total/(Width*Height*3)).ToString("F3"));if(maximum>4)throw new Exception("Fast zoom filtering differs materially from bilinear reference");report.Add("PASS Fast zoom retains smooth bilinear filtering including transparent source pixels");
            }
        }ClearZoomDisplay();ClearFastZoomSamples();
    }
}
partial class Editor {
    public void VerifyHighZoomInteraction(List<string> report,string folder){
        Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);report.Add("PASS "+label);};var timings=new List<string>();
        LoadImageDocument(Blank(3840,2160),"High zoom input and paint");WarmSnapshotImageBytes();var ready=Stopwatch.StartNew();while(snapshotPreparation!=null&&!snapshotPreparation.IsCompleted&&ready.ElapsedMilliseconds<3000){Application.DoEvents();System.Threading.Thread.Sleep(1);}check(PreparedSnapshotBytes()!=null,"Initial Undo image is prepared from an isolated bitmap before editing");var rectangle=new Mark {Kind="Rectangle",X=1900,Y=1050,W=100,H=90,Width=4,Fill=true,FillColor=Color.FromArgb(80,255,0,0).ToArgb()};Marks.Add(rectangle);Marks.Add(new Mark {Kind="Text",X=1860,Y=1100,W=360,H=60,Size=24,Text="Clear text at high zoom",Color=Color.Navy.ToArgb()});RefreshImage();Show();Application.DoEvents();
        foreach(float zoom in new[]{2f,8f}){
            SetTool("Select");Active=rectangle;Selected.Clear();Selected.Add(rectangle);SyncStyle();View.FinishZoomGesture();View.Zoom=zoom;View.PanX=View.PanY=0;View.Refresh();var origin=View.Origin;var point=new Point((int)(origin.X+1918*zoom),(int)(origin.Y+1070*zoom));var clock=Stopwatch.StartNew();CanvasDownWithModifiers(new MouseEventArgs(MouseButtons.Left,1,point.X,point.Y,0),Keys.None);View.Update();double setup=clock.Elapsed.TotalMilliseconds;clock.Restart();for(int i=0;i<8;i++){CanvasMove(new MouseEventArgs(MouseButtons.Left,0,point.X+(i+1)*6,point.Y+(i+1)*3,0));View.Update();}clock.Stop();timings.Add(zoom+"x object drag: setup "+setup.ToString("F2")+" ms; input + native paint "+(clock.Elapsed.TotalMilliseconds/8).ToString("F2")+" ms/frame");CanvasUp(new MouseEventArgs(MouseButtons.Left,0,point.X+48,point.Y+24,0));View.Update();using(var expected=Art.Render(Background,Marks)){bool equal=Art.Bytes(expected).SequenceEqual(Art.Bytes(Preview));if(!equal){expected.Save(Path.Combine(folder,"expected.png"));Preview.Save(Path.Combine(folder,"actual.png"));check(false,"High-zoom dragging retains full-quality composition at "+zoom+"x");}else check(true,"High-zoom dragging retains full-quality composition at "+zoom+"x");}
            SetTool("Arrow");View.Zoom=zoom;View.PanX=View.PanY=0;var start=new Point(View.Width/2,View.Height/2);CanvasDownWithModifiers(new MouseEventArgs(MouseButtons.Left,1,start.X,start.Y,0),Keys.Control);clock.Restart();for(int i=0;i<8;i++){CanvasMove(new MouseEventArgs(MouseButtons.Left,0,start.X+20+i*9,start.Y+20+i*5,0));View.Update();}clock.Stop();timings.Add(zoom+"x arrow drawing: input + native paint "+(clock.Elapsed.TotalMilliseconds/8).ToString("F2")+" ms/frame");CanvasUp(new MouseEventArgs(MouseButtons.Left,0,start.X+83,start.Y+55,0));View.Update();check(Marks.Last().Kind=="Arrow"&&!dragging&&!moving,"Arrow drawing completes without stale interaction state at "+zoom+"x");
            View.Zoom=zoom;View.PanX=View.PanY=0;View.FinishZoomGesture();View.Refresh();View.ZoomRasterMs=View.ZoomBlitMs=0;double inputMs=0;clock.Restart();for(int i=0;i<8;i++){var input=Stopwatch.StartNew();ZoomAt(start,.96f);inputMs+=input.Elapsed.TotalMilliseconds;View.Update();}clock.Stop();timings.Add(zoom+"x wheel zoom: input + native paint "+(clock.Elapsed.TotalMilliseconds/8).ToString("F2")+" ms/frame");timings.Add("Zoom input "+(inputMs/8).ToString("F2")+" ms; raster "+(View.ZoomRasterMs/8).ToString("F2")+" ms; blit "+(View.ZoomBlitMs/8).ToString("F2")+" ms");View.FinishZoomGesture();View.Update();
        }
        View.VerifyFastZoomQuality(report);report.AddRange(timings);File.WriteAllLines(Path.Combine(folder,"high-zoom-input-benchmark.txt"),new[]{"Synthetic 4K scene. Eight input events and synchronous native window paints per gesture; excludes monitor scan-out and physical input latency."}.Concat(timings));Hide();LoadBitmap(Blank(100,100),"Test");
    }
}
}



