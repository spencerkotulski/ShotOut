using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace ShotOut {
partial class Editor {
    Rectangle canvasBase;
    void TightenTextSelection(Mark mark){
        int padding=(int)Math.Ceiling(mark.Size/4)+4,width=(int)Math.Ceiling(mark.W),height=(int)Math.Ceiling(mark.H);
        if(width<1||height<1||(long)(width+padding*2)*(height+padding*2)>8000000)return;
        using(var bitmap=new Bitmap(width+padding*2,height+padding*2,System.Drawing.Imaging.PixelFormat.Format32bppArgb)){
            var measure=mark.Clone();measure.Color=Color.White.ToArgb();
            using(var graphics=Graphics.FromImage(bitmap)){graphics.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAlias;RichLayout.Draw(graphics,measure,new RectangleF(padding,padding,width,height));}
            int left=bitmap.Width,top=bitmap.Height,right=-1,bottom=-1;var bits=bitmap.LockBits(new Rectangle(Point.Empty,bitmap.Size),System.Drawing.Imaging.ImageLockMode.ReadOnly,System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            try{var pixels=new byte[bits.Stride*bitmap.Height];System.Runtime.InteropServices.Marshal.Copy(bits.Scan0,pixels,0,pixels.Length);for(int y=0;y<bitmap.Height;y++)for(int x=0;x<bitmap.Width;x++)if(pixels[y*bits.Stride+x*4+3]>8){left=Math.Min(left,x);top=Math.Min(top,y);right=Math.Max(right,x);bottom=Math.Max(bottom,y);}}finally{bitmap.UnlockBits(bits);}
            if(right<left)return;
            mark.TextInsetLeft=left-padding-2;mark.TextInsetTop=top-padding-2;mark.TextInsetRight=width-(right-padding+1)-2;mark.TextInsetBottom=height-(bottom-padding+1)-2;
            mark.W=right-left+5;mark.H=bottom-top+5;
        }
    }
    void ResetCanvasBase(){canvasBase=new Rectangle(Point.Empty,Background.Size);}
    void ReadCanvasBase(Rectangle bounds){
        var image=new Rectangle(Point.Empty,Background.Size);
        canvasBase=bounds.Width>0&&bounds.Height>0&&image.Contains(bounds)?bounds:image;
    }
    public void VerifySizingUpdate(List<string> report,string folder){
        Action<bool,string> check=delegate(bool ok,string label){if(!ok)throw new Exception(label);report.Add("PASS "+label);};
        foreach(string kind in new[]{"Text","Balloon"})foreach(bool rich in new[]{false,true}){
            LoadBitmap(Blank(400,250),"Tight text");var mark=new Mark {Kind=kind,X=30,Y=30,W=300,H=180,Text="Tight text",Size=20,Color=Color.Black.ToArgb()};if(rich)mark.Runs.Add(new TextRun {Text=mark.Text,Style=1});Marks.Add(mark);Active=mark;SyncStyle();fontSize.Value=100;
            using(var bitmap=new Bitmap(1,1))using(var graphics=Graphics.FromImage(bitmap)){var natural=RichLayout.NaturalSize(graphics,mark);float padding=kind=="Balloon"?20:4;check(mark.W-natural.Width<=padding+3,"Selection closely fits "+kind+" "+(rich?"rich":"plain")+" text at large size");}
            if(kind=="Text")using(var rendered=Art.Render(Background,Marks)){int left=rendered.Width,top=rendered.Height,right=-1,bottom=-1;for(int y=0;y<rendered.Height;y++)for(int x=0;x<rendered.Width;x++){var color=rendered.GetPixel(x,y);if(color.R<180){left=Math.Min(left,x);top=Math.Min(top,y);right=Math.Max(right,x);bottom=Math.Max(bottom,y);}}check(left-mark.X<=5&&top-mark.Y<=5&&mark.Bounds.Right-right<=5&&mark.Bounds.Bottom-bottom<=5,"Selection hugs actual rendered "+(rich?"rich":"plain")+" glyphs on all four sides");}
            check(Background.Width>400,"Larger tight text expands canvas for "+kind);float largeWidth=mark.W; fontSize.Value=20;
            check(mark.W<largeWidth/3&&Background.Width==400&&Background.Height==250,"Smaller text retracts selection and automatic canvas for "+kind);
            Undo();check(Background.Width>400&&Marks[0].Size==100,"Undo restores enlarged text and canvas");Redo();check(Background.Size==new Size(400,250)&&Marks[0].Size==20,"Redo restores tight smaller text and original canvas");
        }
        LoadBitmap(Blank(120,80),"Four sides");using(var graphics=Graphics.FromImage(Background))graphics.Clear(Color.Blue);
        var outside=new Mark {Kind="Image",X=-40,Y=-20,W=200,H=130};using(var bitmap=Blank(200,130))outside.Image=Art.Bytes(bitmap);Marks.Add(outside);EnsureCanvasFitsMarks();check(Background.Size==new Size(200,130)&&canvasBase.Location==new Point(40,20),"Automatic margins track original image offset");
        outside.X=45;outside.Y=25;outside.W=40;outside.H=30;EnsureCanvasFitsMarks();check(Background.Size==new Size(120,80)&&canvasBase.Location==Point.Empty&&outside.X==5&&outside.Y==5,"Automatic margins retract on every side and preserve relative object position");check(Background.GetPixel(0,0).B==255,"Retracting canvas preserves original background pixels");
        outside.X=110;outside.W=50;EnsureCanvasFitsMarks();var snap=Snapshot();using(var stream=new System.IO.MemoryStream()){var xml=new System.Xml.Serialization.XmlSerializer(typeof(Project));xml.Serialize(stream,snap);stream.Position=0;var saved=(Project)xml.Deserialize(stream);Restore(saved);check(canvasBase.Size==new Size(120,80),"Original canvas area persists through project XML");Active=Marks[0];Selected.Clear();Selected.Add(Active);Delete();check(Background.Size==new Size(120,80),"Deleting last outlying annotation retracts canvas");}
        ResizeCanvas(180,120,0,0);ResetCanvasBase();var margin=new Mark {Kind="Rectangle",X=170,Y=10,W=100,H=20};Marks.Add(margin);EnsureCanvasFitsMarks();margin.W=5;EnsureCanvasFitsMarks();check(Background.Size==new Size(180,120),"Manually expanded canvas remains the minimum area");
        int old=currentDocument;StoreDocument();AddWelcomeDocument();ActivateDocument(old);check(canvasBase.Size==new Size(180,120),"Canvas minimum remains local to each tab");
        LoadBitmap(Blank(600,350),"Tight selection");var textMark=new Mark {Kind="Text",X=60,Y=70,W=500,H=250,Text="Fits the text",Size=64,Color=Color.RoyalBlue.ToArgb()};GrowTextBox(textMark,textMark.Size);Marks.Add(textMark);Active=textMark;SyncStyle();RefreshImage();Show();Application.DoEvents();
        using(var preview=new Bitmap(Width,Height)){DrawToBitmap(preview,new Rectangle(Point.Empty,preview.Size));preview.Save(System.IO.Path.Combine(folder,"tight-selection-interface.png"));}Hide();LoadBitmap(Blank(100,100),"Test");
    }
}
}
