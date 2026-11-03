using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace ShotOut {
partial class Editor {
    public void VerifyPanPerformance(List<string> report,string folder){
        Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);report.Add("PASS "+label);};
        LoadImageDocument(Blank(3840,2160),"4K pan benchmark");for(int i=0;i<24;i++)Marks.Add(new Mark {Kind=i%2==0?"Arrow":"Rectangle",X=1450+i*24,Y=760+i*14,W=260,H=150,Width=4});Marks.Add(new Mark {Kind="Text",Text="Sharp text after panning",X=1600,Y=980,W=800,H=80,Size=40});Marks.Add(new Mark {Kind="Pixelate",X=1400,Y=700,W=120,H=60,Width=12});Marks.Add(new Mark {Kind="Text",Text="Final overlay",X=1700,Y=1100,W=650,H=80,Size=38});Marks.Add(new Mark {Kind="Grayscale",AdjustmentWholeCanvas=true,FillOpacity=20});RefreshImage();SetTool("Hand");Show();Application.DoEvents();View.FinishZoomGesture();var originalPreview=Preview;var originalBackground=Background;int history=undo.Count;byte[] originalPixels=Art.Bytes(Preview);var positions=Marks.Select(mark=>mark.Bounds).ToArray();var timing=new List<string>();
        using(var frame=new Bitmap(View.Width,View.Height)){
            foreach(float zoom in new[]{2f,8f}){
                View.Zoom=zoom;View.PanX=View.PanY=0;View.InvalidateMarkupCache();View.DrawToBitmap(frame,new Rectangle(Point.Empty,frame.Size));var clock=Stopwatch.StartNew();int builds=View.CrispPreviewBuilds;
                for(int i=0;i<8;i++){View.PanX+=160;View.DrawToBitmap(frame,new Rectangle(Point.Empty,frame.Size));}clock.Stop();double baseline=clock.Elapsed.TotalMilliseconds/8;check(View.CrispPreviewBuilds>builds,"Pan benchmark exercises sharp-tile boundary rebuilding at "+zoom+"x");View.PanX=View.PanY=0;View.DrawToBitmap(frame,new Rectangle(Point.Empty,frame.Size));builds=View.CrispPreviewBuilds;int navBuilds=View.NavigationPreviewBuilds,panFrames=View.PanPreviewFrames;var button=zoom==2?MouseButtons.Left:MouseButtons.Middle;CanvasDown(new MouseEventArgs(button,1,200,200,0));check(panning&&View.Capture,"Panning starts with "+button+" at "+zoom+"x");clock.Restart();
                for(int i=0;i<8;i++){CanvasMove(new MouseEventArgs(button,0,200+(i+1)*160,200,0));View.DrawToBitmap(frame,new Rectangle(Point.Empty,frame.Size));}clock.Stop();double interactive=clock.Elapsed.TotalMilliseconds/8;
                check(View.CrispPreviewBuilds==builds&&View.NavigationPreviewBuilds==navBuilds&&View.PanPreviewFrames==panFrames+8,"Active pan reuses composited pixels without sharp rebuilds or new navigation bitmaps at "+zoom+"x");check(View.PanCachePixels<=(long)(Math.Ceiling(View.Width/zoom)+130)*(long)(Math.Ceiling(View.Height/zoom)+130),"Pan tile memory stays bounded by the visible source area at "+zoom+"x");CanvasUp(new MouseEventArgs(button,0,1480,200,0));check(!panning&&!View.Capture,"Pan releases cleanly at "+zoom+"x");View.DrawToBitmap(frame,new Rectangle(Point.Empty,frame.Size));check(View.HasCrispPreview&&View.CrispPreviewBuilds==builds+1,"Sharp markup returns once after pan ends at "+zoom+"x");frame.Save(Path.Combine(folder,"settled-pan-"+zoom+"x.png"));timing.Add(zoom+"x: sharp-tile panning "+baseline.ToString("F2")+" ms/frame; interactive panning "+interactive.ToString("F2")+" ms/frame.");
            }
            View.Zoom=.25f;View.PanX=View.PanY=0;View.DrawToBitmap(frame,new Rectangle(Point.Empty,frame.Size));int lowBuilds=View.CrispPreviewBuilds,lowNavigation=View.NavigationPreviewBuilds;CanvasDown(new MouseEventArgs(MouseButtons.Left,1,200,200,0));for(int i=0;i<4;i++){CanvasMove(new MouseEventArgs(MouseButtons.Left,0,210+i*10,220,0));View.DrawToBitmap(frame,new Rectangle(Point.Empty,frame.Size));}check(View.CrispPreviewBuilds==lowBuilds&&View.NavigationPreviewBuilds==lowNavigation+1,"Reduced-zoom panning reuses one bounded navigation image");CanvasUp(new MouseEventArgs(MouseButtons.Left,0,240,220,0));View.DrawToBitmap(frame,new Rectangle(Point.Empty,frame.Size));check(View.HasCrispPreview,"Sharp text returns after reduced-zoom panning too");
            CanvasDown(new MouseEventArgs(MouseButtons.Middle,1,200,200,0));CanvasMove(new MouseEventArgs(MouseButtons.Middle,0,200000,200000,0));View.DrawToBitmap(frame,new Rectangle(Point.Empty,frame.Size));check(frame.GetPixel(frame.Width/2,frame.Height/2).ToArgb()==View.BackColor.ToArgb(),"Panning fully outside the image clears stale image fragments");CanvasUp(new MouseEventArgs(MouseButtons.Middle,0,200000,200000,0));
        }
        check(Object.ReferenceEquals(originalPreview,Preview)&&Object.ReferenceEquals(originalBackground,Background)&&undo.Count==history&&positions.SequenceEqual(Marks.Select(mark=>mark.Bounds))&&originalPixels.SequenceEqual(Art.Bytes(Preview)),"Panning preserves image pixels, privacy effects, annotation positions and history");File.WriteAllLines(Path.Combine(folder,"pan-benchmark.txt"),new[]{"Synthetic 4K screenshot, 29 layers, 8 frames per mode. Includes off-screen painting, not physical input/display latency."}.Concat(timing));report.AddRange(timing);View.PanX=View.PanY=0;Hide();LoadBitmap(Blank(100,100),"Test");
    }
}
}




