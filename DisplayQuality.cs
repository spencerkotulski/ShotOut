using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace ShotOut {
static partial class Art {
    public static void DrawDisplay(Bitmap target,Mark mark){
        if(!DrawingBounds(mark,target.Size).IntersectsWith(new Rectangle(Point.Empty,target.Size)))return;
        if(mark.Kind=="Image"||mark.Kind=="Bucket"||mark.Kind=="Grayscale"||mark.Kind=="Spotlight"){Draw(target,mark,false);return;}
        Rectangle? clip=mark.HasCropClip?(Rectangle?)Rectangle.Intersect(Rectangle.Ceiling(mark.CropClip),new Rectangle(Point.Empty,target.Size)):null;
        if(clip.HasValue&&clip.Value.IsEmpty)return;
        if(mark.Shadow)DrawShadow(target,mark,clip);
        using(var graphics=Graphics.FromImage(target)){if(clip.HasValue)graphics.SetClip(clip.Value);PaintVector(graphics,target,mark,true);}
    }
}
partial class Editor {
    Timer copyStatusTimer;
    bool copyStatusVisible;
    string DefaultStatus {get{return ToolDisplayName(tool)+"  ·  "+Math.Round(View.Zoom*100)+"%  ·  "+Marks.Count+" annotations     |     Ctrl+C copy  ·  Ctrl+V paste  ·  Ctrl+Z undo  ·  ? shortcuts";}}
    void SetStatusMessage(string message){
        if(message=="Copied image"){
            if(copyStatusTimer==null){copyStatusTimer=new Timer {Interval=5000};copyStatusTimer.Tick+=delegate{ExpireCopyStatus();};Disposed+=delegate{copyStatusTimer.Dispose();};}
            copyStatusVisible=true;copyStatusTimer.Stop();copyStatusTimer.Start();
        }else if(message!=null){copyStatusVisible=false;if(copyStatusTimer!=null)copyStatusTimer.Stop();}
        status.Text=copyStatusVisible?"Copied image":message??DefaultStatus;
    }
    void ExpireCopyStatus(){if(!copyStatusVisible)return;if(copyStatusTimer!=null)copyStatusTimer.Stop();copyStatusVisible=false;if(!IsDisposed)UpdateStatus();}
    public void VerifyDisplayQuality(List<string> report,string folder){
        Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);report.Add("PASS "+label);};
        UpdateStatus("Copied image");check(status.Text=="Copied image"&&copyStatusTimer.Enabled&&copyStatusTimer.Interval==5000,"Copy confirmation uses exact text and a five-second timer");UpdateStatus();UpdateZoomStatus();check(status.Text=="Copied image","Ordinary editor refresh and zoom preserve the copy confirmation");ExpireCopyStatus();check(status.Text==DefaultStatus&&!copyStatusTimer.Enabled,"Copy confirmation expires into the normal status");UpdateStatus("Copied image");UpdateStatus("Another message");ExpireCopyStatus();check(!copyStatusVisible&&status.Text=="Another message","Explicit new status replaces the copy confirmation without a stale expiry clearing it");
        LoadImageDocument(Blank(500,320),"Screen resolution markup");var text=new Mark {Kind="Text",Text="Sharp text",X=100,Y=100,W=300,H=50,Size=24,Color=Color.Navy.ToArgb()};Marks.Add(text);Marks.Add(new Mark {Kind="Arrow",X=100,Y=170,W=180,H=45,Width=3,Color=Color.RoyalBlue.ToArgb()});RefreshImage();Show();Application.DoEvents();View.FinishZoomGesture();View.Zoom=4;View.PanX=View.PanY=0;
        using(var frame=new Bitmap(View.Width,View.Height)){View.DrawToBitmap(frame,new Rectangle(Point.Empty,frame.Size));check(View.HasCrispPreview,"High zoom redraws markup at screen resolution beyond the former whole-image limit");frame.Save(Path.Combine(folder,"sharp-screen-markup.png"));}
        check(View.CrispCachePixels<=(long)(View.Width+256)*(View.Height+256),"Sharp cache is bounded by the viewport and pan margin");int builds=View.CrispPreviewBuilds;using(var frame=new Bitmap(View.Width,View.Height)){View.DrawToBitmap(frame,new Rectangle(Point.Empty,frame.Size));check(View.CrispPreviewBuilds==builds,"Repeated paints reuse sharp markup without rebuilding");View.Zoom=1;View.DrawToBitmap(frame,new Rectangle(Point.Empty,frame.Size));check(View.HasCrispPreview,"Native-size preview also uses screen-fitted text");}
        Marks.Insert(1,new Mark {Kind="Pixelate",X=40,Y=40,W=200,H=80,Width=12});text.Shadow=true;Marks.Add(new Mark {Kind="Grayscale",AdjustmentWholeCanvas=true,FillOpacity=60});RefreshImage();View.Zoom=2;using(var frame=new Bitmap(View.Width,View.Height)){View.DrawToBitmap(frame,new Rectangle(Point.Empty,frame.Size));check(View.HasCrispPreview,"Sharp upper markup supports pixelation below, shadows, and grayscale above");frame.Save(Path.Combine(folder,"sharp-markup-with-effects.png"));}using(var expected=Art.Render(Background,Marks))check(Art.Bytes(expected).SequenceEqual(Art.Bytes(Preview)),"Display sharpening leaves native export and privacy-effect pixels unchanged");Hide();LoadBitmap(Blank(100,100),"Test");
    }
}
partial class Canvas {
    internal long CrispCachePixels {get{return crispPreview==null?0:(long)crispPreview.Width*crispPreview.Height;}}
}
}
