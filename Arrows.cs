using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
namespace ShotOut {
class ApplyPrompt : Form {
    protected override bool ProcessCmdKey(ref Message msg,Keys keyData) { if(keyData==(Keys.Control|Keys.Enter)) { ApplyText();return true; }return base.ProcessCmdKey(ref msg,keyData); }
    internal void ApplyText() { DialogResult=DialogResult.OK;Close(); }
}
partial class Editor {
    static bool IsCurved(Mark m) { return m.Kind.IndexOf("Curved",StringComparison.OrdinalIgnoreCase)>=0; }
    static void InitCurve(Mark m) { float length=(float)Math.Sqrt(m.W*m.W+m.H*m.H);if(length<1)length=1;m.CurveX=m.H/length*40;m.CurveY=-m.W/length*40; }
    bool HitMarkup(Mark m,PointF p,float tolerance) { if(!Art.IsArrow(m.Kind))return Inflate(m.Bounds,tolerance).Contains(p);using(var path=Art.ArrowPath(m))using(var pen=new Pen(Color.Black,Math.Max(10/View.Zoom,m.Width+2*tolerance)))return path.IsOutlineVisible(p,pen); }
    PointF[] ArrowHandles(Mark m) {
        var middle=new PointF(m.X+m.W/2,m.Y+m.H/2);float length=(float)Math.Sqrt(m.W*m.W+m.H*m.H);if(length<1)length=1;
        var points=new List<PointF>{new PointF(m.X,m.Y),new PointF(m.X+m.W,m.Y+m.H),middle,new PointF(middle.X+m.H/length*30/View.Zoom,middle.Y-m.W/length*30/View.Zoom)};
        if(IsCurved(m))points.Add(new PointF(middle.X+m.CurveX,middle.Y+m.CurveY));return points.ToArray();
    }
    public void DrawArrowHandles(Graphics g,Mark m) {
        var pts=ArrowHandles(m);float size=7/View.Zoom;using(var pen=new Pen(Color.FromArgb(131,170,246),1/View.Zoom)) {
            pen.DashStyle=DashStyle.Dash;g.DrawLine(pen,pts[2],pts[3]);if(pts.Length>4)g.DrawLine(pen,pts[2],pts[4]);
            for(int i=0;i<pts.Length;i++) { var r=new RectangleF(pts[i].X-size/2,pts[i].Y-size/2,size,size);using(var brush=new SolidBrush(i==4?Color.FromArgb(255,194,91):i==3?Color.FromArgb(131,170,246):Color.White)) { if(i<2)g.FillRectangle(brush,r);else g.FillEllipse(brush,r); } }
        }
    }
    void EditArrow(PointF p) {
        var m=resizeOriginal;var center=new PointF(m.X+m.W/2,m.Y+m.H/2);
        switch(resizeHandle) {
            case 100:Active.X=p.X;Active.Y=p.Y;Active.W=m.X+m.W-p.X;Active.H=m.Y+m.H-p.Y;break;
            case 101:Active.X=m.X;Active.Y=m.Y;Active.W=p.X-m.X;Active.H=p.Y-m.Y;break;
            case 102:Active.X=m.X+p.X-center.X;Active.Y=m.Y+p.Y-center.Y;Active.W=m.W;Active.H=m.H;break;
            case 103:
                var handle=ArrowHandles(m)[3];double angle=Math.Atan2(p.Y-center.Y,p.X-center.X)-Math.Atan2(handle.Y-center.Y,handle.X-center.X);
                if((ModifierKeys&Keys.Shift)!=0)angle=Math.Round(angle/(Math.PI/12))*(Math.PI/12);
                float cos=(float)Math.Cos(angle),sin=(float)Math.Sin(angle),dx=m.W/2,dy=m.H/2;
                float rx=dx*cos-dy*sin,ry=dx*sin+dy*cos;Active.X=center.X-rx;Active.Y=center.Y-ry;Active.W=rx*2;Active.H=ry*2;Active.CurveX=m.CurveX*cos-m.CurveY*sin;Active.CurveY=m.CurveX*sin+m.CurveY*cos;break;
            case 104:Active.CurveX=p.X-center.X;Active.CurveY=p.Y-center.Y;break;
        }
    }
    public void VerifyArrows(List<string> report) {
        Action<bool,string> check=delegate(bool ok,string name){if(!ok)throw new Exception(name);report.Add("PASS "+name);};
        LoadBitmap(Blank(400,300),"Arrow test");var arrow=new Mark{Kind="Arrow",X=50,Y=100,W=200,H=0};Marks.Add(arrow);Active=arrow;resizeOriginal=arrow.Clone();resizeHandle=100;EditArrow(new PointF(70,80));check(arrow.X==70&&arrow.Y==80&&arrow.X+arrow.W==250&&arrow.Y+arrow.H==100,"Dragging start keeps end fixed");
        resizeOriginal=arrow.Clone();resizeHandle=101;EditArrow(new PointF(280,120));check(arrow.X==70&&arrow.Y==80&&arrow.W==210&&arrow.H==40,"Dragging end keeps start fixed");
        resizeOriginal=arrow.Clone();resizeHandle=102;var center=new PointF(175,100);EditArrow(new PointF(center.X+10,center.Y+20));check(arrow.X==80&&arrow.Y==100&&arrow.W==210&&arrow.H==40,"Middle handle translates whole arrow");
        resizeOriginal=arrow.Clone();resizeHandle=103;var oldCenter=new PointF(arrow.X+arrow.W/2,arrow.Y+arrow.H/2);EditArrow(new PointF(oldCenter.X+40,oldCenter.Y));check(Math.Abs(arrow.X+arrow.W/2-oldCenter.X)<.01&&Math.Abs(arrow.Y+arrow.H/2-oldCenter.Y)<.01,"Rotation preserves arrow center");
        arrow.Kind="Curved arrow";InitCurve(arrow);resizeOriginal=arrow.Clone();resizeHandle=104;EditArrow(new PointF(oldCenter.X+25,oldCenter.Y-60));check(arrow.CurveX==25&&arrow.CurveY==-60,"Curve handle adjusts bend by dragging");
        using(var bg=Blank(400,300))foreach(string kind in new[]{"Dashed arrow","Curved arrow","Dashed curved arrow"}) { arrow.Kind=kind;using(var image=Art.Render(bg,new List<Mark>{arrow})) { bool changed=false;for(int y=0;y<300&&!changed;y++)for(int x=0;x<400;x++)if(image.GetPixel(x,y).ToArgb()!=Color.White.ToArgb()){changed=true;break;}check(changed,"Export "+kind); } }
        using(var prompt=new ApplyPrompt()) { prompt.ApplyText();check(prompt.DialogResult==DialogResult.OK,"Text apply command returns OK"); }
        LoadBitmap(Blank(100,100),"Test");
    }
}
}
