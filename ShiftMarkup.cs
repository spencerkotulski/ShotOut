using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace ShotOut {
partial class Editor {
    static bool SquareMarkup(string kind){return new[]{"Rectangle","Rounded","Ellipse","Step","Highlight","Redact","Blur","Pixelate","Spotlight","Magnify","Balloon","Grayscale"}.Contains(kind);}
    static PointF SnapMarkupLine(PointF anchor,PointF point){float dx=point.X-anchor.X,dy=point.Y-anchor.Y;double angle=Math.Round(Math.Atan2(dy,dx)/(Math.PI/4))*(Math.PI/4),length=Math.Sqrt(dx*dx+dy*dy);return new PointF(anchor.X+(float)(Math.Cos(angle)*length),anchor.Y+(float)(Math.Sin(angle)*length));}
    static RectangleF SquareResize(RectangleF original,RectangleF resized,int handle){
        bool corner=handle%2==0;float side=corner?Math.Max(resized.Width,resized.Height):(handle==1||handle==5?resized.Height:resized.Width);side=Math.Max(4,side);
        if(corner){bool left=handle==0||handle==6,top=handle==0||handle==2;return new RectangleF(left?original.Right-side:original.Left,top?original.Bottom-side:original.Top,side,side);}
        if(handle==1||handle==5)return new RectangleF(original.Left+(original.Width-side)/2,handle==1?original.Bottom-side:original.Top,side,side);
        return new RectangleF(handle==7?original.Right-side:original.Left,original.Top+(original.Height-side)/2,side,side);
    }
    static Control ToolbarDivider(){return new Panel {Width=1,Height=26,Margin=new Padding(7,6,7,0),BackColor=Color.FromArgb(75,78,88),AccessibleName="Toolbar group separator"};}
    static void StyleTopToolbar(Panel header){header.Paint+=delegate(object sender,PaintEventArgs e){using(var pen=new Pen(Color.FromArgb(66,69,79)))e.Graphics.DrawLine(pen,0,header.Height-2,header.Width,header.Height-2);using(var pen=new Pen(Color.FromArgb(15,16,20)))e.Graphics.DrawLine(pen,0,header.Height-1,header.Width,header.Height-1);};}
    public void VerifyShiftMarkup(List<string> report,string folder){
        Action<bool,string> check=delegate(bool ok,string label){if(!ok)throw new Exception(label);report.Add("PASS "+label);};
        LoadBitmap(Blank(600,400),"Shift resize");
        foreach(string kind in new[]{"Rectangle","Rounded","Ellipse","Step","Highlight","Redact","Blur","Pixelate","Spotlight","Magnify","Balloon"}){
            for(int handle=0;handle<8;handle++){var mark=new Mark {Kind=kind,X=100,Y=100,W=80,H=50,Tail=false};Active=mark;resizeOriginal=mark.Clone();resizeHandle=handle;var point=Handles(mark.BodyBounds)[handle];ResizeActiveWithModifiers(new PointF(point.X+20,point.Y+12),Keys.Shift);check(Math.Abs(mark.W-mark.H)<.001,"Shift keeps "+kind+" square at handle "+handle);if(handle%2==0){var opposite=Handles(resizeOriginal.BodyBounds)[(handle+4)%8];check(Math.Abs(Handles(mark.BodyBounds)[(handle+4)%8].X-opposite.X)<.001&&Math.Abs(Handles(mark.BodyBounds)[(handle+4)%8].Y-opposite.Y)<.001,"Square resize anchors opposite corner for "+kind+" handle "+handle);}}
        }
        var stepMark=new Mark {Kind="Step",X=100,Y=100,W=80,H=50};Active=stepMark;resizeOriginal=stepMark.Clone();resizeHandle=4;ResizeActiveWithModifiers(new PointF(200,170),Keys.None);check(stepMark.W==100&&stepMark.H==70,"Releasing Shift restores free resizing");
        foreach(string kind in new[]{"Text","Emoji","Image","Pen","Bucket","Cursor"}){check(!SquareMarkup(kind),kind+" is not forced into square geometry");var mark=new Mark {Kind=kind,X=100,Y=100,W=80,H=50};Active=mark;resizeOriginal=mark.Clone();resizeHandle=4;ResizeActiveWithModifiers(new PointF(200,170),Keys.Shift);check(mark.W==100&&mark.H==70,"Shift does not distort "+kind+" into a square");}
        foreach(int handle in new[]{100,101}){var arrow=new Mark {Kind="Arrow",X=100,Y=100,W=150,H=40};Active=arrow;resizeOriginal=arrow.Clone();resizeHandle=handle;ResizeActiveWithModifiers(new PointF(300,260),Keys.Shift);double angle=Math.Atan2(arrow.H,arrow.W)/(Math.PI/4);check(Math.Abs(angle-Math.Round(angle))<.001,"Shift arrow endpoint snaps to 45 degrees "+handle);}
        SetTool("Select");var number=new Mark {Kind="Step",X=80,Y=70,W=90,H=90,Text="1",Color=Color.RoyalBlue.ToArgb()};Marks.Add(number);Active=number;Selected.Clear();Selected.Add(number);RefreshImage();Show();Application.DoEvents();using(var image=new Bitmap(Width,Height)){DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(System.IO.Path.Combine(folder,"toolbar-and-circle.png"));}Hide();LoadBitmap(Blank(100,100),"Test");
    }
}
}
