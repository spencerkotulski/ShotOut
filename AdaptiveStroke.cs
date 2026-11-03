using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
namespace ShotOut {
partial class Editor {
    internal const float ResizeHandlePixels=10,ResizeHitPixels=10;
    internal static void PaintResizeHandle(Graphics graphics,PointF point,float zoom){float size=ResizeHandlePixels/zoom;var box=new RectangleF(point.X-size/2,point.Y-size/2,size,size);graphics.FillRectangle(Brushes.White,box);using(var border=new Pen(Color.FromArgb(110,168,255),1/zoom))graphics.DrawRectangle(border,box.X,box.Y,box.Width,box.Height);}
    static bool ScaledStroke(string kind){return Art.IsArrow(kind)||kind=="Rectangle"||kind=="Rounded"||kind=="Ellipse"||kind=="Pen"||kind=="Balloon"||kind=="Magnify";}
    static decimal StrokeDefault(string kind,Size image){decimal basis=Art.IsArrow(kind)||kind=="Rectangle"||kind=="Rounded"||kind=="Ellipse"?4:3;if(!ScaledStroke(kind))return basis;return Math.Max(1,Math.Min(32,Math.Round(basis*1.18m*Math.Max(image.Width,image.Height)/1000m,0,MidpointRounding.AwayFromZero)));}
    decimal PreferredStroke(string kind){decimal width;return rememberedWeights.TryGetValue(WeightFamily(kind),out width)?width:DefaultWeight(kind);}
    void RememberStrokeOverride(){if(syncing||thickness==null)return;string kind=Active==null?tool:Active.Kind;if(HasWeight(kind))rememberedWeights[WeightFamily(kind)]=thickness.Value;}
    void RefreshUnselectedStroke(string kind){if(Active!=null||!HasWeight(kind))return;decimal value=PreferredStroke(kind);if(thickness.Value!=value)thickness.Value=value;}
    public void VerifyAdaptiveStroke(List<string> report,string folder){
        Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);report.Add("PASS "+label);};rememberedWeights.Clear();
        foreach(var image in new[]{new Size(100,80),new Size(1000,700),new Size(2991,1374),new Size(3840,2160)}){LoadBitmap(Blank(image.Width,image.Height),"Adaptive defaults");foreach(string kind in new[]{"Arrow","Rectangle","Ellipse","Pen","Balloon","Magnify"}){SetTool(kind);check(NewMark(kind,PointF.Empty).Width==(float)StrokeDefault(kind,image),"Default "+kind+" stroke follows image size "+image.Width+" × "+image.Height);}}
        check(StrokeDefault("Arrow",new Size(100,80))==1&&StrokeDefault("Arrow",new Size(2991,1374))==14&&StrokeDefault("Arrow",new Size(16000,16000))==32,"Adaptive defaults keep a 1 px minimum and a practical 32 px cap");
        SetTool("Arrow");thickness.Value=7;SetTool("Rectangle");SetTool("Arrow");check(thickness.Value==7,"Manual stroke changes remain explicit overrides across tool switches");LoadBitmap(Blank(400,300),"Manual override");SetTool("Arrow");check(NewMark("Arrow",PointF.Empty).Width==7,"A chosen manual stroke stays fixed when image size changes");
        rememberedWeights.Clear();var existing=new Mark {Kind="Rectangle",X=50,Y=50,W=150,H=100,Width=13};Marks.Add(existing);Active=existing;Selected.Clear();Selected.Add(existing);SyncStyle();check(existing.Width==13&&thickness.Value==13,"Selecting an existing annotation preserves its exact stroke");SetTool("Rectangle");check(NewMark("Rectangle",PointF.Empty).Width==2,"Selecting an annotation does not replace the adaptive default");
        thickness.Value=0;check(NewMark("Rectangle",PointF.Empty).Width==0,"Optional zero-width outlines remain available as a manual choice");SetTool("Blur");check(thickness.Value==3,"Blur strength is not scaled like an outline");SetTool("Pixelate");check(thickness.Value==3,"Pixelation strength is not scaled like an outline");
        foreach(float zoom in new[]{.5f,1f,4f}){View.Zoom=zoom;Active=existing;Selected.Clear();Selected.Add(existing);var corner=Handles(existing.Bounds)[0];check(HitHandle(new PointF(corner.X+9/zoom,corner.Y+9/zoom),existing.Bounds)==0,"Resize handles remain easy to hit at "+zoom+"x zoom");}
        LoadBitmap(Blank(2991,1374),"Image-sized defaults");rememberedWeights.Clear();SetTool("Arrow");var arrow=NewMark("Arrow",new PointF(600,800));arrow.W=900;arrow.H=-400;Marks.Add(arrow);Active=arrow;Selected.Clear();Selected.Add(arrow);SyncStyle();RefreshImage();Show();Application.DoEvents();View.Fit();using(var screenshot=new Bitmap(Width,Height)){DrawToBitmap(screenshot,new Rectangle(Point.Empty,screenshot.Size));screenshot.Save(Path.Combine(folder,"adaptive-stroke-handles.png"));}Hide();rememberedWeights.Clear();LoadBitmap(Blank(100,100),"Test");
    }
}
}

