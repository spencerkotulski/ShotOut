using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace ShotOut {
class ProportionButton : ModernButton {
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var state=e.Graphics.Save();e.Graphics.TranslateTransform(Width/2f,Height/2f);e.Graphics.RotateTransform(-40);using(var pen=new Pen(ForeColor,1.5f)){using(var path=Art.Round(new RectangleF(-10,-4,13,8),4))e.Graphics.DrawPath(pen,path);using(var path=Art.Round(new RectangleF(-3,-4,13,8),4))e.Graphics.DrawPath(pen,path);}e.Graphics.Restore(state);}
}
partial class Editor {
    Control geometryTitleRow,geometryPositionRow,geometrySizeRow;
    NumberField geometryX,geometryY,geometryWidth,geometryHeight;
    ProportionButton geometryLock;
    bool geometryAdjusting;
    List<Mark> geometryTargets;
    Mark pendingBackgroundDrag;
    List<Mark> pendingBackgroundSelection;
    Point pendingBackgroundScreen;
    void ClearBackgroundClick(){pendingBackgroundDrag=null;pendingBackgroundSelection=null;}
    void BeginBackgroundClick(Mark mark,Point point){pendingBackgroundDrag=mark;pendingBackgroundSelection=SelectedObjects();pendingBackgroundScreen=point;Active=null;Selected.Clear();SyncLayers();View.Invalidate();}
    bool ContinueBackgroundDrag(Point point){
        if(Math.Abs(point.X-pendingBackgroundScreen.X)<SystemInformation.DragSize.Width/2&&Math.Abs(point.Y-pendingBackgroundScreen.Y)<SystemInformation.DragSize.Height/2)return false;
        var mark=pendingBackgroundDrag;var selected=pendingBackgroundSelection;pendingBackgroundDrag=null;pendingBackgroundSelection=null;if(!Marks.Contains(mark))return false;
        Active=mark;Selected.Clear();foreach(var item in selected)if(Marks.Contains(item))Selected.Add(item);Selected.Add(mark);Push();moving=true;SyncStyle();SyncLayers();BeginImageSnapping();StartDragPreview();return true;
    }
    void AddGeometryInspector(){
        var row=Row();row.Height=32;geometrySection=new InspectorDisclosure {SectionTitle="Position / size · px",Width=178,Height=28,Margin=Padding.Empty,BackColor=Color.FromArgb(43,46,54),ForeColor=text,Font=Font,TabStop=true,AccessibleName="Expand or collapse position and size"};geometrySection.Click+=delegate{ToggleInspectorSection(1);};row.Controls.Add(geometrySection);geometryLock=new ProportionButton {Width=34,Height=28,Margin=Padding.Empty,BackColor=Color.FromArgb(43,46,54),ForeColor=text,Selected=true,TabStop=false,AccessibleName="Keep proportions"};geometryLock.Click+=delegate{geometryLock.Selected=!geometryLock.Selected;geometryLock.Invalidate();tips.SetToolTip(geometryLock,geometryLock.Selected?"Keep proportions: on":"Keep proportions: off");};tips.SetToolTip(geometryLock,"Keep proportions: on · Click to resize width and height independently");row.Controls.Add(geometryLock);geometryTitleRow=row;inspector.Controls.Add(row);
        row=Row();row.Controls.Add(Caption("X",20));geometryX=GeometryNumber(-16000,16000,0,"Horizontal position in canvas pixels");geometryX.Margin=new Padding(0,0,6,0);row.Controls.Add(geometryX);row.Controls.Add(Caption("Y",20));geometryY=GeometryNumber(-16000,16000,0,"Vertical position in canvas pixels");row.Controls.Add(geometryY);geometryPositionRow=row;inspector.Controls.Add(row);
        row=Row();row.Controls.Add(Caption("W",20));geometryWidth=GeometryNumber(0,16000,1,"Selection width in pixels");geometryWidth.Margin=new Padding(0,0,6,0);row.Controls.Add(geometryWidth);row.Controls.Add(Caption("H",20));geometryHeight=GeometryNumber(0,16000,1,"Selection height in pixels");row.Controls.Add(geometryHeight);geometrySizeRow=row;inspector.Controls.Add(row);
        geometryX.ValueChanged+=delegate{ChangeGeometry(0);};geometryY.ValueChanged+=delegate{ChangeGeometry(1);};geometryWidth.ValueChanged+=delegate{ChangeGeometry(2);};geometryHeight.ValueChanged+=delegate{ChangeGeometry(3);};
    }
    NumberField GeometryNumber(decimal minimum,decimal maximum,decimal value,string label){var field=Number(minimum,maximum,value);field.Width=86;field.AccessibleName=label;tips.SetToolTip(field,label+" · Enter an exact value, or use the arrows for 1 px");return field;}
    static RectangleF ObjectGeometry(Mark mark){var rect=Art.IsArrow(mark.Kind)?mark.Bounds:mark.BodyBounds;return mark.HasCropClip?RectangleF.Intersect(rect,mark.CropClip):rect;}
    RectangleF SelectionGeometry(List<Mark> targets){var rect=ObjectGeometry(targets[0]);foreach(var mark in targets.Skip(1))rect=RectangleF.Union(rect,ObjectGeometry(mark));return rect;}
    int GeometryInspectorHeight(){
        if(geometryTitleRow==null)return 0;var chosen=SelectedObjects();bool show=chosen.Count>0;geometryTitleRow.Visible=show;geometryPositionRow.Visible=geometrySizeRow.Visible=show&&Preferences.Current.GeometryExpanded;geometrySection.Expanded=Preferences.Current.GeometryExpanded;geometrySection.Invalidate();geometryTitleRow.Enabled=geometryPositionRow.Enabled=geometrySizeRow.Enabled=inlineTarget==null&&!chosen.Any(ImageMovementIsLocked);if(show)SyncGeometryFields(chosen);return show?(Preferences.Current.GeometryExpanded?131:37):0;
    }
    void SyncGeometryFields(List<Mark> targets){if(targets.Count==0)return;var box=SelectionGeometry(targets);bool old=syncing;syncing=true;try{geometryX.Value=(decimal)box.X;geometryY.Value=(decimal)box.Y;geometryWidth.Value=(decimal)Math.Max(0,box.Width);geometryHeight.Value=(decimal)Math.Max(0,box.Height);}finally{syncing=old;}}
    static Mark TransformGeometry(Mark original,RectangleF before,RectangleF after,bool moveSources){
        var mark=original.Clone();float sx=before.Width>.001f?after.Width/before.Width:1,sy=before.Height>.001f?after.Height/before.Height:1;
        Func<float,float> x=value=>after.Left+(value-before.Left)*sx,y=value=>after.Top+(value-before.Top)*sy;
        mark.X=x(mark.X);mark.Y=y(mark.Y);mark.W*=sx;mark.H*=sy;mark.Points=mark.Points.Select(point=>new PointF(x(point.X),y(point.Y))).ToList();mark.CurveX*=sx;mark.CurveY*=sy;
        if(Art.IsArrow(mark.Kind)){if(before.Width<.001f){mark.X=after.X;mark.W=after.Width;}if(before.Height<.001f){mark.Y=after.Y;mark.H=after.Height;}}
        if(mark.HasCropClip)mark.CropClip=RectangleF.FromLTRB(x(mark.CropClip.Left),y(mark.CropClip.Top),x(mark.CropClip.Right),y(mark.CropClip.Bottom));
        if(moveSources&&mark.SourceInitialized&&mark.MagnifyLinked){mark.SourceX=x(mark.SourceX);mark.SourceY=y(mark.SourceY);}
        if(mark.Kind=="Text"||mark.Kind=="Balloon"||mark.Kind=="Emoji")mark.Size=Math.Max(.1f,Math.Min(500*4f/3,mark.Size*Math.Min(sx,sy)));
        mark.TextInsetLeft*=sx;mark.TextInsetRight*=sx;mark.TextInsetTop*=sy;mark.TextInsetBottom*=sy;if(mark.Kind=="Grayscale")mark.AdjustmentWholeCanvas=false;return mark;
    }
    static void AssignGeometry(Mark mark,Mark value){mark.X=value.X;mark.Y=value.Y;mark.W=value.W;mark.H=value.H;mark.Points=value.Points;mark.CurveX=value.CurveX;mark.CurveY=value.CurveY;mark.CropClip=value.CropClip;mark.SourceX=value.SourceX;mark.SourceY=value.SourceY;mark.Size=value.Size;mark.TextInsetLeft=value.TextInsetLeft;mark.TextInsetRight=value.TextInsetRight;mark.TextInsetTop=value.TextInsetTop;mark.TextInsetBottom=value.TextInsetBottom;mark.AdjustmentWholeCanvas=value.AdjustmentWholeCanvas;}
    bool GeometryFitsCanvas(Dictionary<Mark,Mark> proposed){
        float left=canvasBase.Left,top=canvasBase.Top,right=canvasBase.Right,bottom=canvasBase.Bottom;foreach(var original in Marks){Mark mark;mark=proposed.TryGetValue(original,out mark)?mark:original;if(!mark.Visible||mark.Kind=="Grayscale")continue;var rect=mark.VisibleBounds;left=Math.Min(left,rect.Left);top=Math.Min(top,rect.Top);right=Math.Max(right,rect.Right);bottom=Math.Max(bottom,rect.Bottom);}long width=(long)Math.Ceiling(right)-(long)Math.Floor(left),height=(long)Math.Ceiling(bottom)-(long)Math.Floor(top);return width<=16000&&height<=16000&&width*height<=40000000;
    }
    void ChangeGeometry(int operation){
        if(syncing||inlineTarget!=null)return;var targets=SelectedObjects();if(targets.Count==0)return;if(targets.Any(ImageMovementIsLocked)){SyncGeometryFields(targets);return;}float value=(float)(operation==0?geometryX.Value:operation==1?geometryY.Value:operation==2?geometryWidth.Value:geometryHeight.Value);
        if(strokeAdjusting&&(!geometryAdjusting||!targets.SequenceEqual(geometryTargets)))FinishStrokeAdjustment();var before=SelectionGeometry(targets);var after=before;
        if(operation==0)after.X=value;else if(operation==1)after.Y=value;else if(operation==2){after.Width=value;if(geometryLock.Selected&&before.Width>.001f)after.Height=before.Height*value/before.Width;}else{after.Height=value;if(geometryLock.Selected&&before.Height>.001f)after.Width=before.Width*value/before.Height;}
        if(after==before)return;if((after.Width<.001f||after.Height<.001f)&&targets.Any(mark=>!Art.IsArrow(mark.Kind))){SyncGeometryFields(targets);return;}
        bool moveSources=targets.Any(mark=>mark.Kind=="Image");var proposed=targets.ToDictionary(mark=>mark,mark=>TransformGeometry(mark,before,after,moveSources));if(!GeometryFitsCanvas(proposed)){SyncGeometryFields(targets);UpdateStatus("Canvas limit: 16,000 pixels per side and 40 million pixels total.");return;}
        if(!strokeAdjusting){Push();geometryTargets=targets;geometryAdjusting=strokeAdjusting=true;strokeTarget=Active??targets.Last();StartDragPreview();}
        foreach(var pair in proposed)AssignGeometry(pair.Key,pair.Value);SyncGeometryFields(targets);bool old=syncing;syncing=true;try{if(Active!=null&&(Active.Kind=="Text"||Active.Kind=="Balloon"||Active.Kind=="Emoji"))fontSize.Value=TextFonts.Points(Active.Size);}finally{syncing=old;}
        if(strokeTimer==null){strokeTimer=new Timer {Interval=140};strokeTimer.Tick+=delegate{FinishStrokeAdjustment();};Disposed+=delegate{strokeTimer.Dispose();};}strokeTimer.Stop();strokeTimer.Start();RefreshImage();
    }
    public void VerifyExactGeometry(List<string> report,string folder){
        Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);report.Add("PASS "+label);};LoadImageDocument(Blank(1200,800),"Precise layout");var baseImage=Marks[0];var shape=new Mark {Kind="Rectangle",X=100,Y=100,W=100,H=50,Fill=true,FillColor=Color.Red.ToArgb()};Marks.Add(shape);SetTool("Select");Active=shape;Selected.Clear();Selected.Add(shape);SyncStyle();RefreshImage();View.Zoom=1;var origin=View.Origin;Func<int,int,MouseEventArgs> mouse=(x,y)=>new MouseEventArgs(MouseButtons.Left,1,(int)origin.X+x,(int)origin.Y+y,0);
        int history=undo.Count;CanvasDownWithModifiers(mouse(500,400),Keys.None);CanvasUp(mouse(500,400));check(SelectedObjects().Count==0&&Active==null&&layers.SelectedItems.Count==0&&undo.Count==history,"Clicking screenshot background clears stale annotation selection without adding history");using(var copied=Art.Render(Background,Marks))check(copied.Size==Background.Size&&copied.GetPixel(140,120).R>240&&copied.GetPixel(140,120).G<10,"No-selection copy source contains the screenshot and all markup");
        Active=baseImage;Selected.Add(baseImage);SyncStyle();SyncLayers();CanvasDownWithModifiers(mouse(550,450),Keys.None);CanvasUp(mouse(550,450));check(Active==null&&SelectedObjects().Count==0,"Clicking an explicitly selected screenshot deselects it for full-image copying");
        Active=shape;Selected.Add(shape);SetTool("Rectangle");Active=shape;Selected.Add(shape);SyncStyle();CanvasDownWithModifiers(mouse(600,500),Keys.None);CanvasUp(mouse(600,500));check(SelectedObjects().Count==0,"A click without drawing also clears the old markup selection");
        SetTool("Arrow");Active=shape;Selected.Clear();Selected.Add(shape);SyncStyle();int count=Marks.Count;CanvasDownWithModifiers(mouse(600,500),Keys.None);CanvasUp(mouse(600,500));check(Marks.Count==count&&SelectedObjects().Count==0,"A blank arrow-tool click does not leave an invisible arrow selected");SetTool("Hand");Active=shape;Selected.Clear();Selected.Add(shape);SyncStyle();CanvasDownWithModifiers(mouse(600,500),Keys.None);CanvasUp(mouse(600,500));check(SelectedObjects().Count==0,"A left-click with the hand tool clears stale object selection");
        baseImage.ImageMovementLocked=false;SetTool("Select");Active=baseImage;Selected.Clear();Selected.Add(baseImage);SyncStyle();CanvasDownWithModifiers(mouse(600,500),Keys.None);CanvasMove(mouse(620,515));PrepareDragFrame();CanvasUp(mouse(620,515));check(baseImage.X==20&&baseImage.Y==15,"An explicitly selected screenshot can still be moved by dragging");Undo();baseImage=Marks[0];shape=Marks[1];origin=View.Origin;
        SetTool("Select");Active=shape;Selected.Clear();Selected.Add(shape);SyncStyle();history=undo.Count;geometryX.Value=125;geometryY.Value=150;geometryWidth.Value=200;PrepareDragFrame();check(shape.X==125&&shape.Y==150&&shape.W==200&&shape.H==100,"Exact X/Y and proportion-locked width apply to a selected shape");FinishStrokeAdjustment();check(undo.Count==history+1,"Rapid numeric geometry edits form one Undo step");Undo();check(Marks[1].X==100&&Marks[1].W==100,"Undo restores original coordinates and dimensions");Redo();shape=Marks[1];Active=shape;Selected.Clear();Selected.Add(shape);SyncStyle();geometryLock.Selected=false;geometryHeight.Value=80;FinishStrokeAdjustment();check(shape.W==200&&shape.H==80,"Unlocked dimensions resize independently");
        var arrow=new Mark {Kind="Arrow",X=500,Y=250,W=-100,H=-100,Width=4};Marks.Add(arrow);Active=arrow;Selected.Clear();Selected.Add(arrow);SyncStyle();geometryWidth.Value=200;FinishStrokeAdjustment();check(arrow.W==-200&&arrow.H==-100&&arrow.Bounds.Left==400,"Exact sizing preserves reversed arrow direction");
        Selected.Clear();Selected.Add(shape);Selected.Add(arrow);Active=arrow;SyncStyle();float oldX=shape.X;geometryX.Value=50;FinishStrokeAdjustment();check(Math.Abs(SelectionGeometry(SelectedObjects()).X-50)<.01&&shape.X==oldX-75,"Multiple objects move by their shared selection bounds");
        float sharedWidth=SelectionGeometry(SelectedObjects()).Width;geometryWidth.Value=(decimal)(sharedWidth*2);FinishStrokeAdjustment();check(shape.W==400&&arrow.W==-400,"Multiple-object sizing scales members and spacing together");
        var pen=new Mark {Kind="Pen",X=700,Y=400,W=100,H=50,Points=new List<PointF>{new PointF(700,400),new PointF(800,450)}};Marks.Add(pen);Active=pen;Selected.Clear();Selected.Add(pen);SyncStyle();geometryWidth.Value=200;FinishStrokeAdjustment();check(pen.Points[1].X==900&&pen.Points[1].Y==450,"Numeric pen sizing scales actual stroke points");
        var textMark=new Mark {Kind="Text",X=400,Y=100,W=160,H=50,Text="Editable",Size=24};Marks.Add(textMark);Active=textMark;Selected.Clear();Selected.Add(textMark);SyncStyle();geometryLock.Selected=true;geometryWidth.Value=320;FinishStrokeAdjustment();check(textMark.W==320&&textMark.H==100&&textMark.Size==48&&textMark.Text=="Editable","Proportional text sizing preserves editable content and scales the font");var before=textMark.BodyBounds;int beforeHistory=undo.Count;geometryWidth.Value=16000;check(textMark.BodyBounds==before&&undo.Count==beforeHistory,"Oversized numeric transforms are rejected without changing the object or history");
        Show();Application.DoEvents();using(var image=new Bitmap(Width,Height)){DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(folder,"exact-position-size.png"));}Hide();geometryLock.Selected=true;LoadBitmap(Blank(100,100),"Test");
    }
}
}
