using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
namespace ShotOut {
static partial class Art {
    public static RectangleF MagnifySource(Mark m,Size size){var lens=m.BodyBounds;float factor=Math.Max(1,Math.Min(8,m.Magnification));float width=Math.Min(size.Width,lens.Width/factor),height=Math.Min(size.Height,lens.Height/factor);float x=m.MagnifyLinked&&m.SourceInitialized?m.SourceX:lens.Left+lens.Width/2,y=m.MagnifyLinked&&m.SourceInitialized?m.SourceY:lens.Top+lens.Height/2;x=Math.Max(width/2,Math.Min(size.Width-width/2,x));y=Math.Max(height/2,Math.Min(size.Height-height/2,y));return new RectangleF(x-width/2,y-height/2,width,height);}
    static PointF EdgePoint(RectangleF rect,PointF target){var center=new PointF(rect.Left+rect.Width/2,rect.Top+rect.Height/2);float dx=target.X-center.X,dy=target.Y-center.Y;if(Math.Abs(dx)+Math.Abs(dy)<.01)return center;float t=Math.Min(Math.Abs(dx)<.001?float.MaxValue:rect.Width/2/Math.Abs(dx),Math.Abs(dy)<.001?float.MaxValue:rect.Height/2/Math.Abs(dy));return new PointF(center.X+dx*t,center.Y+dy*t);}
    static void Frame(Graphics g,Pen pen,RectangleF rect){if(pen.Width<=1.01f){var state=g.Save();g.SmoothingMode=SmoothingMode.None;g.PixelOffsetMode=PixelOffsetMode.Default;float left=(float)Math.Round(rect.Left),top=(float)Math.Round(rect.Top),right=(float)Math.Round(rect.Right),bottom=(float)Math.Round(rect.Bottom);using(var brush=new SolidBrush(pen.Color)){g.FillRectangle(brush,left,top,right-left,1);g.FillRectangle(brush,left,bottom-1,right-left,1);g.FillRectangle(brush,left,top,1,bottom-top);g.FillRectangle(brush,right-1,top,1,bottom-top);}g.Restore(state);return;}float stroke=Math.Min(pen.Width,Math.Min(rect.Width,rect.Height)/2);var inset=rect;inset.Inflate(-stroke/2,-stroke/2);if(inset.Width>0&&inset.Height>0){pen.Width=stroke;g.DrawRectangle(pen,inset.X,inset.Y,inset.Width,inset.Height);}}
    static void DrawMagnifier(Bitmap target,Mark m,Rectangle? updateRegion){
        var lens=m.BodyBounds;if(lens.Width<1||lens.Height<1)return;var source=MagnifySource(m,target.Size);if(source.Width<1||source.Height<1)return;
        var crop=Rectangle.Intersect(Rectangle.FromLTRB((int)Math.Floor(source.Left),(int)Math.Floor(source.Top),(int)Math.Ceiling(source.Right),(int)Math.Ceiling(source.Bottom)),new Rectangle(Point.Empty,target.Size));
        using(var image=target.Clone(crop,PixelFormat.Format32bppArgb)) {
            if(m.Shadow)DrawShadow(target,m,updateRegion);
            using(var g=Graphics.FromImage(target))using(var pen=new Pen(Color.FromArgb(m.Color),m.Width))using(var attributes=new ImageAttributes()){
                if(updateRegion.HasValue)g.SetClip(updateRegion.Value);g.SmoothingMode=SmoothingMode.AntiAlias;pen.LineJoin=LineJoin.Round;
                if(m.MagnifyLinked){pen.Width=Math.Max(1,m.Width);var center=new PointF(lens.Left+lens.Width/2,lens.Top+lens.Height/2);var origin=new PointF(source.Left+source.Width/2,source.Top+source.Height/2);if(!source.IntersectsWith(lens))g.DrawLine(pen,EdgePoint(source,center),EdgePoint(lens,origin));if(m.Width>0){pen.Width=m.Width;Frame(g,pen,source);}}
                var state=g.Save();g.SetClip(lens,CombineMode.Intersect);g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;attributes.SetWrapMode(WrapMode.TileFlipXY);
                g.DrawImage(image,new[]{new PointF(lens.Left,lens.Top),new PointF(lens.Right,lens.Top),new PointF(lens.Left,lens.Bottom)},new RectangleF(source.X-crop.X,source.Y-crop.Y,source.Width,source.Height),GraphicsUnit.Pixel,attributes);g.Restore(state);
                // Stroke after restoring the image clip, fully inset so all four 1px sides survive.
                g.PixelOffsetMode=PixelOffsetMode.Default;if(m.Width>0){pen.Width=m.Width;Frame(g,pen,lens);}
            }
        }
    }
}
partial class Editor {
    static bool OptionalOutline(string kind){return kind=="Rectangle"||kind=="Rounded"||kind=="Ellipse"||kind=="Balloon"||kind=="Magnify";}
    void ApplyLayerVisibility(){
        bool show=Preferences.Current.ShowLayers;
        if(!show){CloseInlineRename(true);ClearLayerHover();}
        inspector.Parent.SuspendLayout();layers.Visible=layerTitle.Visible=layerButtons.Visible=show;
        inspector.Dock=show?DockStyle.Top:DockStyle.Fill;
        inspector.Parent.ResumeLayout(true);UpdateInspector();FitInspectorControls();
    }
    public void VerifyPanelAndOutlines(List<string> report,string folder){
        Action<bool,string> check=delegate(bool ok,string label){if(!ok)throw new Exception(label);report.Add("PASS "+label);};
        bool original=Preferences.Current.ShowLayers;Show();Application.DoEvents();
        Preferences.Current.ShowLayers=false;ApplyLayerVisibility();Application.DoEvents();
        check(!layers.Visible&&!layerTitle.Visible&&!layerButtons.Visible&&inspector.Dock==DockStyle.Fill,"Hidden Layers gives full sidebar to annotation options");
        Preferences.Current.Save(System.IO.Path.Combine(folder,"panel-settings.xml"));check(!Preferences.Read(System.IO.Path.Combine(folder,"panel-settings.xml")).ShowLayers,"Layers visibility persists in settings");
        Preferences.Current.ShowLayers=true;ApplyLayerVisibility();Application.DoEvents();check(layers.Visible&&layerTitle.Visible&&layerButtons.Visible&&inspector.Dock==DockStyle.Top,"Layers panel can be restored");
        foreach(string kind in new[]{"Rectangle","Rounded","Ellipse","Balloon","Magnify"}){
            SetTool(kind);var mark=new Mark{Kind=kind,X=20,Y=20,W=80,H=60,Width=3,Fill=true,FillColor=Color.White.ToArgb(),Color=Color.Red.ToArgb(),Text=""};Marks.Clear();Marks.Add(mark);Active=mark;SyncStyle();thickness.Value=0;
            check(mark.Width==0&&thickness.Minimum==0,"Zero outline can be selected for "+kind);SyncStyle();check(thickness.Value==0,"Zero outline survives selection for "+kind);
            foreach(bool quality in new[]{false,true})using(var bg=Blank(140,120))using(var image=Art.Render(bg,new List<Mark>{mark},quality)){bool red=false;for(int y=0;y<image.Height&&!red;y++)for(int x=0;x<image.Width;x++){var c=image.GetPixel(x,y);if(c.R>c.G+20){red=true;break;}}check(!red,"No residual hairline in "+kind+(quality?" export":" preview"));}
        }
        foreach(string kind in new[]{"Arrow","Line","Pen","Blur","Pixelate"}){SetTool(kind);check(thickness.Minimum==1&&thickness.Value>=1,"Minimum one retained for "+kind);}
        Preferences.Current.ShowLayers=original;ApplyLayerVisibility();Hide();LoadBitmap(Blank(100,100),"Test");
    }

    int layerHover=-1,layerInsertion=-1;
    NumberField magnifyPercent;CheckBox magnifyLinked;FlowLayoutPanel magnifyZoomRow,magnifyLinkRow;
    void ClearLayerHover(){if(layerHover<0)return;layerHover=-1;layers.Invalidate();View.Invalidate();}
    void HoverLayer(int index){if(layerHover==index)return;int old=layerHover;layerHover=index;if(old>=0&&old<layers.Items.Count)layers.Invalidate(layers.GetItemRectangle(old));if(index>=0&&index<layers.Items.Count)layers.Invalidate(layers.GetItemRectangle(index));View.Invalidate();}
    public void DrawHoveredLayer(Graphics g){if(layerHover<0||layerHover>=layers.Items.Count)return;var mark=layers.Items[layerHover] as Mark;if(mark==null||!mark.Visible||!Marks.Contains(mark))return;using(var pen=new Pen(Color.FromArgb(144,187,255),1.3f/View.Zoom)){pen.DashStyle=DashStyle.Dash;if(Art.IsArrow(mark.Kind))using(var path=Art.ArrowPath(mark))g.DrawPath(pen,path);else if(mark.Kind=="Balloon")using(var path=Art.BalloonPath(mark))g.DrawPath(pen,path);else {var r=mark.Bounds;g.DrawRectangle(pen,r.X,r.Y,r.Width,r.Height);}}}
    void DrawLayerInsertion(DrawItemEventArgs e){if(layerInsertion<0)return;bool bottom=layerInsertion==layers.Items.Count&&e.Index==layers.Items.Count-1;if(e.Index!=layerInsertion&&!bottom)return;float y=bottom?e.Bounds.Bottom-2:e.Bounds.Top+1;using(var pen=new Pen(Color.FromArgb(124,170,255),2))e.Graphics.DrawLine(pen,8,y,e.Bounds.Right-8,y);using(var brush=new SolidBrush(Color.FromArgb(124,170,255)))e.Graphics.FillEllipse(brush,5,y-3,6,6);}
    int InsertionAt(Point p){int index=layers.IndexFromPoint(p);if(index<0)return p.Y<0?0:layers.Items.Count;var r=layers.GetItemRectangle(index);return index+(p.Y>=r.Top+r.Height/2?1:0);}
    void DropLayerAt(Mark mark,int gap){int old=Marks.Count-1-Marks.IndexOf(mark);int target=Math.Max(0,Math.Min(Marks.Count-1,gap>old?gap-1:gap));MoveLayer(mark,Marks.Count-1-target);}
    void WireModernLayers(){
        layers.AllowDrop=true;tips.SetToolTip(layers,"Hover to identify markup · Drag to the blue insertion line · Double-click to rename");
        layers.MouseDown+=delegate(object sender,MouseEventArgs e){layerMouse=e.Location;int index=layers.IndexFromPoint(e.Location);layerDragging=index<0?null:(Mark)layers.Items[index];if(e.Button==MouseButtons.Left&&index>=0&&e.X>=layers.ClientSize.Width-30){Push();layerDragging.Visible=!layerDragging.Visible;Active=layerDragging;layerDragging=null;RefreshImage();}};
        layers.MouseMove+=delegate(object sender,MouseEventArgs e){HoverLayer(layers.IndexFromPoint(e.Location));if(e.Button==MouseButtons.Left&&layerDragging!=null&&(Math.Abs(e.X-layerMouse.X)>SystemInformation.DragSize.Width/2||Math.Abs(e.Y-layerMouse.Y)>SystemInformation.DragSize.Height/2)){try{layers.DoDragDrop("ShotOutLayer",DragDropEffects.Move);}finally{layerDragging=null;layerInsertion=-1;layers.Invalidate();ClearLayerHover();}}};
        layers.MouseLeave+=delegate{ClearLayerHover();};
        layers.DragOver+=delegate(object sender,DragEventArgs e){if(layerDragging==null){e.Effect=DragDropEffects.None;return;}e.Effect=DragDropEffects.Move;var point=layers.PointToClient(new Point(e.X,e.Y));if(point.Y<22&&layers.TopIndex>0)layers.TopIndex--;else if(point.Y>layers.Height-22&&layers.TopIndex<layers.Items.Count-1)layers.TopIndex++;int gap=InsertionAt(point);if(gap!=layerInsertion){layerInsertion=gap;layers.Invalidate();}};
        layers.DragLeave+=delegate{layerInsertion=-1;layers.Invalidate();};
        layers.DragDrop+=delegate(object sender,DragEventArgs e){if(layerDragging==null)return;int gap=InsertionAt(layers.PointToClient(new Point(e.X,e.Y)));DropLayerAt(layerDragging,gap);layerInsertion=-1;layers.Invalidate();};
    }
    void AddMagnifyInspector(){magnifyZoomRow=Row();magnifyZoomRow.Controls.Add(Caption("Magnification · %",145));magnifyPercent=Number(100,800,200);magnifyPercent.ValueChanged+=delegate{ChangeStyle();};magnifyZoomRow.Controls.Add(magnifyPercent);inspector.Controls.Add(magnifyZoomRow);magnifyLinkRow=Row();magnifyLinked=new ThemeCheckBox{Text="Source box + stem",Width=210,Height=34};magnifyLinked.CheckedChanged+=delegate{ChangeStyle();};magnifyLinkRow.Controls.Add(magnifyLinked);inspector.Controls.Add(magnifyLinkRow);}
    int MagnifyInspectorHeight(string kind){bool show=kind=="Magnify";magnifyZoomRow.Visible=magnifyLinkRow.Visible=show;return show?94:0;}
    void SyncMagnifySettings(Mark mark){magnifyPercent.Value=(decimal)Math.Max(100,Math.Min(800,mark.Magnification*100));magnifyLinked.Checked=mark.MagnifyLinked;}
    void ApplyMagnifySettings(Mark mark){if(mark.Kind!="Magnify")return;bool was=mark.MagnifyLinked;mark.Magnification=(float)magnifyPercent.Value/100;mark.MagnifyLinked=magnifyLinked.Checked;if(mark.MagnifyLinked&&!was){var lens=mark.BodyBounds;mark.SourceX=lens.Left+lens.Width/2;mark.SourceY=lens.Top+lens.Height/2;mark.SourceInitialized=true;PlaceMagnifier(mark);}}
    void PlaceMagnifier(Mark mark){var lens=mark.BodyBounds;if(!mark.SourceInitialized){mark.SourceX=lens.Left+lens.Width/2;mark.SourceY=lens.Top+lens.Height/2;mark.SourceInitialized=true;}float dx=0,dy=0;if(lens.Right+lens.Width+30<=Background.Width)dx=lens.Width+30;else if(lens.Left-lens.Width-30>=0)dx=-lens.Width-30;else if(lens.Bottom+lens.Height+30<=Background.Height)dy=lens.Height+30;else if(lens.Top-lens.Height-30>=0)dy=-lens.Height-30;mark.Move(dx,dy);}
    public void DrawMagnifierHandles(Graphics g,Mark mark){float size=7/View.Zoom;foreach(var point in Handles(mark.BodyBounds))g.FillRectangle(Brushes.White,point.X-size/2,point.Y-size/2,size,size);if(mark.MagnifyLinked){var source=Art.MagnifySource(mark,Background.Size);var center=new PointF(source.Left+source.Width/2,source.Top+source.Height/2);using(var brush=new SolidBrush(Color.FromArgb(255,194,91)))g.FillEllipse(brush,center.X-size/2,center.Y-size/2,size,size);}}
    public void VerifyShotOut(List<string> report,string folder){
        Action<bool,string> check=delegate(bool ok,string label){if(!ok)throw new Exception(label);report.Add("PASS "+label);};
        using(var background=Blank(240,160)) {using(var g=Graphics.FromImage(background))for(int x=0;x<240;x+=8)g.FillRectangle(x%16==0?Brushes.LightGray:Brushes.White,x,0,8,160);var lens=new Mark{Kind="Magnify",X=40,Y=30,W=80,H=60,Width=1,Color=Color.Red.ToArgb()};using(var image=Art.Render(background,new List<Mark>{lens})){bool all=true;foreach(var p in new[]{new Point(40,55),new Point(119,55),new Point(75,30),new Point(75,89)}){var color=image.GetPixel(p.X,p.Y);if(color.R<180||color.G>100||color.B>100)all=false;}image.Save(System.IO.Path.Combine(folder,"magnify-border.png"));check(all,"Magnify 1px border covers all four sides");}
            using(var twice=Art.Render(background,new List<Mark>{lens})){lens.Magnification=4;using(var four=Art.Render(background,new List<Mark>{lens})){bool changed=false;for(int y=32;y<87&&!changed;y++)for(int x=42;x<117;x++)if(twice.GetPixel(x,y)!=four.GetPixel(x,y)){changed=true;break;}check(changed,"Magnification changes actual image detail");}}
        }
        LoadBitmap(Blank(800,500),"ShotOut magnifier");using(var g=Graphics.FromImage(Background)){g.Clear(Color.FromArgb(247,249,253));using(var f=new Font("Segoe UI",17,FontStyle.Regular,GraphicsUnit.Pixel)){g.DrawString("Small details. Clear feedback.",f,Brushes.Navy,70,130);g.DrawString("Choose a source, then move the enlarged view.",f,Brushes.SlateGray,70,170);}}
        var first=new Mark{Kind="Rectangle",Name="Source highlight",X=60,Y=115,W=260,H=40,Color=Color.RoyalBlue.ToArgb(),Radius=6};Marks.Add(first);var magnifier=new Mark{Kind="Magnify",Name="Adjustable detail",X=390,Y=230,W=290,H=160,SourceX=190,SourceY=140,SourceInitialized=true,MagnifyLinked=true,Magnification=2.5f,Width=1,Color=Color.RoyalBlue.ToArgb()};Marks.Add(magnifier);Active=magnifier;Selected.Clear();Selected.Add(magnifier);SyncStyle();RefreshImage();float sourceX=magnifier.SourceX;magnifier.Move(15,10);check(magnifier.SourceX==sourceX,"Moving enlarged view keeps source fixed");start=new PointF(sourceX,magnifier.SourceY);resizeOriginal=magnifier.Clone();resizeHandle=300;ResizeActive(new PointF(sourceX+10,magnifier.SourceY+5));check(magnifier.SourceX==sourceX+10,"Source region can be moved independently");RefreshImage();HoverLayer(1);check(layerHover==1,"Hover tracks the layer to highlight");DropLayerAt(first,0);check(Marks.Last()==first,"Insertion line drop respects before/after placement");Undo();Active=Marks.First(m=>m.Kind=="Magnify");SyncStyle();RefreshImage();Show();Application.DoEvents();HoverLayer(1);layerInsertion=1;layers.Invalidate();using(var preview=new Bitmap(Width,Height)){DrawToBitmap(preview,new Rectangle(Point.Empty,preview.Size));preview.Save(System.IO.Path.Combine(folder,"shotout-interface.png"));}layerInsertion=-1;ClearLayerHover();Hide();LoadBitmap(Blank(100,100),"Test");
    }
}
}
