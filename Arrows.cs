using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
namespace ShotOut {
static class ArrowRotationCursor {
    static Cursor value;
    static System.IO.MemoryStream data;
    internal static Cursor Value { get {
        if(value!=null)return value;
        // Native monochrome cursor planes preserve the white outline on every desktop.
        // Cache the cursor and its stream; hovering never allocates drawing resources.
        using(var bitmap=new Bitmap(32,32)) {
            using(var g=Graphics.FromImage(bitmap))using(var outline=new Pen(Color.White,6))using(var ink=new Pen(Color.Black,3)) {
                g.SmoothingMode=SmoothingMode.None;
                g.DrawArc(outline,7,8,18,18,190,160);g.DrawArc(ink,7,8,18,18,190,160);
                foreach(var head in new[]{new[]{new Point(7,23),new Point(3,14),new Point(12,15)},new[]{new Point(25,23),new Point(20,15),new Point(29,14)}}) {
                    g.FillPolygon(Brushes.Black,head);using(var edge=new Pen(Color.White,1))g.DrawPolygon(edge,head);
                }
            }
            data=new System.IO.MemoryStream();var writer=new System.IO.BinaryWriter(data);
            writer.Write((ushort)0);writer.Write((ushort)2);writer.Write((ushort)1);
            writer.Write((byte)32);writer.Write((byte)32);writer.Write((ushort)0);writer.Write((ushort)16);writer.Write((ushort)16);writer.Write(40+8+256);writer.Write(22);
            writer.Write(40);writer.Write(32);writer.Write(64);writer.Write((ushort)1);writer.Write((ushort)1);writer.Write(0);writer.Write(256);writer.Write(0);writer.Write(0);writer.Write(2);writer.Write(0);
            writer.Write(0);writer.Write(0x00ffffff);
            for(int y=31;y>=0;y--)for(int column=0;column<4;column++){byte pixels=0;for(int bit=0;bit<8;bit++){var pixel=bitmap.GetPixel(column*8+bit,y);if(pixel.A!=0&&pixel.R>127)pixels|=(byte)(128>>bit);}writer.Write(pixels);}
            for(int y=31;y>=0;y--)for(int column=0;column<4;column++){byte mask=0;for(int bit=0;bit<8;bit++)if(bitmap.GetPixel(column*8+bit,y).A==0)mask|=(byte)(128>>bit);writer.Write(mask);}
            writer.Flush();data.Position=0;value=new Cursor(data);return value;
        }
    } }
}
class ApplyPrompt : Form {
    protected override bool ProcessCmdKey(ref Message msg,Keys keyData) { if(keyData==(Keys.Control|Keys.Enter)) { ApplyText();return true; }return base.ProcessCmdKey(ref msg,keyData); }
    internal void ApplyText() { DialogResult=DialogResult.OK;Close(); }
}
partial class Editor {
    static bool IsCurved(Mark m) { return m.ArrowCurved||m.Kind.IndexOf("Curved",StringComparison.OrdinalIgnoreCase)>=0; }
    static void InitCurve(Mark m) { float length=(float)Math.Sqrt(m.W*m.W+m.H*m.H);if(length<1)length=1;m.CurveX=m.H/length*40;m.CurveY=-m.W/length*40; }
    bool HitMarkup(Mark m,PointF p,float tolerance) { if(!Art.IsArrow(m.Kind))return Inflate(m.Bounds,tolerance).Contains(p);using(var path=Art.ArrowPath(m))using(var pen=new Pen(Color.Black,Math.Max(10/View.Zoom,m.Width+2*tolerance)))return path.IsOutlineVisible(p,pen); }
    PointF[] ArrowHandles(Mark m) {
        var middle=new PointF(m.X+m.W/2,m.Y+m.H/2);float length=(float)Math.Sqrt(m.W*m.W+m.H*m.H);if(length<1)length=1;
        var bend=IsCurved(m)?new PointF(middle.X+m.CurveX,middle.Y+m.CurveY):middle;
        return new[]{new PointF(m.X,m.Y),new PointF(m.X+m.W,m.Y+m.H),bend,new PointF(middle.X+m.H/length*30/View.Zoom,middle.Y-m.W/length*30/View.Zoom)};
    }
    public void DrawArrowHandles(Graphics g,Mark m) {
        var pts=ArrowHandles(m);float size=ResizeHandlePixels/View.Zoom;using(var pen=new Pen(Color.FromArgb(131,170,246),1/View.Zoom)) {
            pen.DashStyle=DashStyle.Dash;g.DrawLine(pen,new PointF(m.X+m.W/2,m.Y+m.H/2),pts[3]);
            pen.DashStyle=DashStyle.Solid;for(int i=0;i<pts.Length;i++) { var r=new RectangleF(pts[i].X-size/2,pts[i].Y-size/2,size,size);using(var brush=new SolidBrush(i==2?Color.FromArgb(255,194,91):i==3?Color.FromArgb(131,170,246):Color.White)) { if(i<2){g.FillRectangle(brush,r);g.DrawRectangle(pen,r.X,r.Y,r.Width,r.Height);}else{g.FillEllipse(brush,r);g.DrawEllipse(pen,r);} } }
        }
    }
    void EditArrow(PointF p) {
        var m=resizeOriginal;var center=new PointF(m.X+m.W/2,m.Y+m.H/2);
        switch(resizeHandle) {
            case 100:Active.X=p.X;Active.Y=p.Y;Active.W=m.X+m.W-p.X;Active.H=m.Y+m.H-p.Y;break;
            case 101:Active.X=m.X;Active.Y=m.Y;Active.W=p.X-m.X;Active.H=p.Y-m.Y;break;
            case 102:Active.ArrowCurved=true;Active.CurveX=p.X-center.X;Active.CurveY=p.Y-center.Y;break;
            case 103:
                var handle=ArrowHandles(m)[3];double angle=Math.Atan2(p.Y-center.Y,p.X-center.X)-Math.Atan2(handle.Y-center.Y,handle.X-center.X);
                if((ModifierKeys&Keys.Shift)!=0)angle=Math.Round(angle/(Math.PI/12))*(Math.PI/12);
                float cos=(float)Math.Cos(angle),sin=(float)Math.Sin(angle),dx=m.W/2,dy=m.H/2;
                float rx=dx*cos-dy*sin,ry=dx*sin+dy*cos;Active.X=center.X-rx;Active.Y=center.Y-ry;Active.W=rx*2;Active.H=ry*2;Active.CurveX=m.CurveX*cos-m.CurveY*sin;Active.CurveY=m.CurveX*sin+m.CurveY*cos;break;
        }
    }
    public void VerifyArrows(List<string> report) {
        Action<bool,string> check=delegate(bool ok,string name){if(!ok)throw new Exception(name);report.Add("PASS "+name);};
        LoadBitmap(Blank(400,300),"Arrow test");var arrow=new Mark{Kind="Arrow",X=50,Y=100,W=200,H=0};Marks.Add(arrow);Active=arrow;resizeOriginal=arrow.Clone();resizeHandle=100;EditArrow(new PointF(70,80));check(arrow.X==70&&arrow.Y==80&&arrow.X+arrow.W==250&&arrow.Y+arrow.H==100,"Dragging start keeps end fixed");
        resizeOriginal=arrow.Clone();resizeHandle=101;EditArrow(new PointF(280,120));check(arrow.X==70&&arrow.Y==80&&arrow.W==210&&arrow.H==40,"Dragging end keeps start fixed");
        check(ArrowHandles(arrow).Length==4&&ArrowHandles(arrow)[2]==new PointF(175,100),"Straight arrows have a centered bend handle and no move dot");
        Selected.Clear();Selected.Add(arrow);MoveLinkedSelection(10,20);check(arrow.X==80&&arrow.Y==100&&arrow.W==210&&arrow.H==40,"Shaft movement translates both endpoints together");
        resizeOriginal=arrow.Clone();resizeHandle=103;var oldCenter=new PointF(arrow.X+arrow.W/2,arrow.Y+arrow.H/2);EditArrow(new PointF(oldCenter.X+40,oldCenter.Y));check(Math.Abs(arrow.X+arrow.W/2-oldCenter.X)<.01&&Math.Abs(arrow.Y+arrow.H/2-oldCenter.Y)<.01,"Rotation preserves arrow center");
        arrow.Kind="Curved arrow";InitCurve(arrow);resizeOriginal=arrow.Clone();resizeHandle=102;EditArrow(new PointF(oldCenter.X+25,oldCenter.Y-60));check(arrow.CurveX==25&&arrow.CurveY==-60,"Curve handle adjusts bend by dragging");
        using(var bg=Blank(400,300))foreach(string kind in new[]{"Dashed arrow","Curved arrow","Dashed curved arrow"}) { arrow.Kind=kind;using(var image=Art.Render(bg,new List<Mark>{arrow})) { bool changed=false;for(int y=0;y<300&&!changed;y++)for(int x=0;x<400;x++)if(image.GetPixel(x,y).ToArgb()!=Color.White.ToArgb()){changed=true;break;}check(changed,"Export "+kind); } }
        LoadBitmap(Blank(400,300),"Arrow interactions");SetTool("Select");arrow=new Mark{Kind="Arrow",X=50,Y=100,W=200,H=0};Marks.Add(arrow);Active=arrow;Selected.Clear();Selected.Add(arrow);RefreshImage();View.Zoom=1;View.PanX=View.PanY=0;
        Func<PointF,MouseButtons,MouseEventArgs> mouse=(point,button)=>{var origin=View.Origin;return new MouseEventArgs(button,1,(int)(origin.X+point.X),(int)(origin.Y+point.Y),0);};
        CanvasDownWithModifiers(mouse(new PointF(100,100),MouseButtons.Left),Keys.None);CanvasMove(mouse(new PointF(110,120),MouseButtons.Left));CanvasUp(mouse(new PointF(110,120),MouseButtons.Left));check(arrow.X==60&&arrow.Y==120&&arrow.W==200,"Dragging the arrow shaft moves the complete arrow");
        var bend=ArrowHandles(arrow)[2];CanvasDownWithModifiers(mouse(bend,MouseButtons.Left),Keys.None);CanvasMove(mouse(new PointF(bend.X,bend.Y+40),MouseButtons.Left));CanvasUp(mouse(new PointF(bend.X,bend.Y+40),MouseButtons.Left));check(arrow.ArrowCurved&&Math.Abs(arrow.CurveY-40)<.1&&arrow.X==60,"Dragging the centered amber handle bends without translating");
        CanvasMove(mouse(ArrowHandles(arrow)[3],MouseButtons.None));check(View.Cursor==ArrowRotationCursor.Value,"Blue rotation handle uses the circular-arrow hover cursor");
        using(var prompt=new ApplyPrompt()) { prompt.ApplyText();check(prompt.DialogResult==DialogResult.OK,"Text apply command returns OK"); }
        LoadBitmap(Blank(100,100),"Test");
    }
}
}

