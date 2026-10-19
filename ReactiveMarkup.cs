using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
namespace ShotOut {
partial class Editor {
    bool strokeAdjusting, textAdjusting, quickTextSizing;
    Mark strokeTarget;
    Timer strokeTimer;
    bool imageSnapping;
    Mark imageSnapAnchor;
    PointF imageDragStart;
    RectangleF imageDragVisible;
    float? imageGuideX,imageGuideY;
    static bool ReactiveStroke(string kind){return Art.IsArrow(kind)||new[]{"Rectangle","Rounded","Ellipse","Pen","Balloon","Magnify"}.Contains(kind);}
    void ChangeStrokeWidth(){
        if(syncing||Active==null)return;
        if(!ReactiveStroke(Active.Kind)||moving||resizing||dragging){ChangeStyle();return;}
        if(Active.Width==(float)thickness.Value)return;
        if(strokeAdjusting&&strokeTarget!=Active)FinishStrokeAdjustment();
        if(!strokeAdjusting){Push();strokeAdjusting=true;strokeTarget=Active;StartDragPreview();}
        Active.Width=(float)thickness.Value;
        if(strokeTimer==null){strokeTimer=new Timer {Interval=140};strokeTimer.Tick+=delegate{FinishStrokeAdjustment();};Disposed+=delegate{strokeTimer.Dispose();};}
        strokeTimer.Stop();strokeTimer.Start();RefreshImage();
    }
    void FinishStrokeAdjustment(){if(!strokeAdjusting)return;if(strokeTimer!=null)strokeTimer.Stop();if(textAdjusting&&strokeTarget!=null&&Marks.Contains(strokeTarget)){textAdjusting=false;GrowTextBox(strokeTarget,strokeTarget.Size);EnsureCanvasFitsMarks();}textAdjusting=false;strokeAdjusting=false;strokeTarget=null;RefreshImage();}
    Bitmap BuildInteractionBase(){
        if(dragFirst!=Marks.Count-1)return Art.Render(Background,Marks.Take(dragFirst).ToList(),true);
        var bitmap=(Bitmap)Preview.Clone();if(draft!=null)return bitmap;
        var area=Art.DrawingBounds(Marks[dragFirst],Background.Size);if(area.IsEmpty)return bitmap;
        using(var graphics=Graphics.FromImage(bitmap)){graphics.CompositingMode=CompositingMode.SourceCopy;graphics.InterpolationMode=InterpolationMode.NearestNeighbor;graphics.PixelOffsetMode=PixelOffsetMode.Half;graphics.DrawImage(Background,area,area,GraphicsUnit.Pixel);}
        for(int i=0;i<dragFirst;i++)if(Marks[i].Visible)Art.Draw(bitmap,Marks[i],true,area);return bitmap;
    }
    bool TryFinishDragPreview(){
        if(dragBase==null||Preview==null||!Object.ReferenceEquals(dragBackground,Background)||dragScene.Count!=Marks.Count||!dragScene.SequenceEqual(Marks))return false;
        var area=ExpandDragDirty(UnionBounds(dragPreviousBounds,MutableBounds()));
        if(!area.IsEmpty){using(var graphics=Graphics.FromImage(Preview)){graphics.CompositingMode=CompositingMode.SourceCopy;graphics.InterpolationMode=InterpolationMode.NearestNeighbor;graphics.PixelOffsetMode=PixelOffsetMode.Half;graphics.DrawImage(dragBase,area,area,GraphicsUnit.Pixel);}for(int i=dragFirst;i<Marks.Count;i++)if(Marks[i].Visible)Art.Draw(Preview,Marks[i],true,area);}
        EndDragPreview();View.InvalidateMarkupCache();return true;
    }
    void BeginImageSnapping(){var images=SelectedObjects().Where(mark=>mark.Kind=="Image").ToList();imageSnapAnchor=images.Count==1?images[0]:null;imageSnapping=imageSnapAnchor!=null;if(imageSnapping){imageDragStart=new PointF(imageSnapAnchor.X,imageSnapAnchor.Y);imageDragVisible=imageSnapAnchor.VisibleBounds;imageSnapping=!imageDragVisible.IsEmpty;}}
    void ClearImageSnapping(){bool guides=imageGuideX.HasValue||imageGuideY.HasValue;imageSnapping=false;imageSnapAnchor=null;imageGuideX=imageGuideY=null;if(guides&&View!=null)View.Invalidate();}
    static float SnapImageAxis(float position,float size,IEnumerable<float> targets,float threshold,out float? guide){float best=threshold+.001f,offset=0;guide=null;foreach(float target in targets)foreach(float anchor in new[]{position+size/2,position,position+size}){float delta=target-anchor;if(Math.Abs(delta)<best){best=Math.Abs(delta);offset=delta;guide=target;}}return position+offset;}
    bool MoveSnappedImage(PointF point,Keys modifiers){
        if(!imageSnapping||imageSnapAnchor==null||!Marks.Contains(imageSnapAnchor))return false;float x=imageDragVisible.X+point.X-start.X,y=imageDragVisible.Y+point.Y-start.Y;float? oldX=imageGuideX,oldY=imageGuideY;imageGuideX=imageGuideY=null;
        if((modifiers&Keys.Alt)==0){var xs=new List<float>{Background.Width/2f,Background.Width/3f,Background.Width*2f/3,0,Background.Width};var ys=new List<float>{Background.Height/2f,Background.Height/3f,Background.Height*2f/3,0,Background.Height};foreach(var other in Marks.Where(mark=>mark.Visible&&mark.Kind=="Image"&&mark!=imageSnapAnchor)){var rect=other.VisibleBounds;if(rect.IsEmpty)continue;xs.AddRange(new[]{rect.Left,rect.Left+rect.Width/2,rect.Right});ys.AddRange(new[]{rect.Top,rect.Top+rect.Height/2,rect.Bottom});}float threshold=6/Math.Max(.02f,View.Zoom);x=SnapImageAxis(x,imageDragVisible.Width,xs,threshold,out imageGuideX);y=SnapImageAxis(y,imageDragVisible.Height,ys,threshold,out imageGuideY);}
        MoveLinkedSelection(imageDragStart.X+x-imageDragVisible.X-imageSnapAnchor.X,imageDragStart.Y+y-imageDragVisible.Y-imageSnapAnchor.Y);if(oldX!=imageGuideX||oldY!=imageGuideY)View.Invalidate();return true;
    }
    public void DrawImageSnapGuides(Graphics graphics){if(!imageGuideX.HasValue&&!imageGuideY.HasValue)return;using(var pen=new Pen(Color.FromArgb(224,121,255),1/View.Zoom)){pen.DashStyle=DashStyle.Dash;if(imageGuideX.HasValue)graphics.DrawLine(pen,imageGuideX.Value,0,imageGuideX.Value,Background.Height);if(imageGuideY.HasValue)graphics.DrawLine(pen,0,imageGuideY.Value,Background.Width,imageGuideY.Value);}}
    public void VerifyReactiveMarkup(List<string> report,string folder){
        Action<bool,string> check=delegate(bool ok,string label){if(!ok)throw new Exception(label);report.Add("PASS "+label);};
        using(var number=new NumberField {Minimum=0,Maximum=80,Value=4,AcceleratedWheel=true}){int changes=0;number.ValueChanged+=delegate{changes++;};number.ApplyWheel(480);check(number.Value>8&&changes==1,"Fast stroke wheel batches multiple skipped values into one change event");decimal width=number.Value;number.TestArrow(true);check(number.Value==width+1,"Stroke arrow buttons retain one-pixel precision");}
        LoadImageDocument(Blank(3840,2160),"Reactive stroke benchmark");var arrow=new Mark {Kind="Arrow",X=300,Y=800,W=550,H=160,Width=4};Marks.Add(arrow);Active=arrow;Selected.Clear();Selected.Add(arrow);SyncStyle();RefreshImage();var buffer=Preview;int history=undo.Count;double firstStep=0;var clock=System.Diagnostics.Stopwatch.StartNew();for(int i=0;i<8;i++){thickness.Value=6+i*2;PrepareDragFrame();if(i==0)firstStep=clock.Elapsed.TotalMilliseconds;}clock.Stop();double perStep=clock.Elapsed.TotalMilliseconds/8,steady=(clock.Elapsed.TotalMilliseconds-firstStep)/7;check(strokeAdjusting&&Object.ReferenceEquals(buffer,Preview),"Rapid stroke changes reuse preview rather than allocating full screenshot frames");FinishStrokeAdjustment();check(undo.Count==history+1&&arrow.Width==20,"Rapid stroke adjustment is one undoable action");check(Object.ReferenceEquals(buffer,Preview),"Settled stroke rendering preserves existing preview buffer");using(var expected=Art.Render(Background,Marks)){var area=Art.DrawingBounds(arrow,Background.Size);bool same=true;for(int y=area.Top;y<area.Bottom&&same;y++)for(int x=area.Left;x<area.Right;x++)if(expected.GetPixel(x,y)!=Preview.GetPixel(x,y)){same=false;break;}check(same,"Settled stroke redraw matches high-quality export");}Undo();check(Marks.Last().Width==4,"One Undo restores width before wheel burst");Redo();check(Marks.Last().Width==20,"Redo restores final stroke width");
        SetTool("Arrow");View.Zoom=1;View.PanX=View.PanY=0;var origin=View.Origin;Func<float,float,MouseEventArgs> mouse=(x,y)=>new MouseEventArgs(MouseButtons.Left,1,(int)Math.Round(origin.X+x*View.Zoom),(int)Math.Round(origin.Y+y*View.Zoom),0);buffer=Preview;CanvasDownWithModifiers(mouse(1000,1000),Keys.Control);CanvasMove(mouse(1400,1200));PrepareDragFrame();CanvasUp(mouse(1400,1200));check(Object.ReferenceEquals(buffer,Preview)&&Marks.Last().Kind=="Arrow","Drawing and completing an arrow reuses the screenshot buffer");
        report.Add("4K reactive stroke benchmark: "+perStep.ToString("F2")+" ms/change including initial cache setup; steady changes "+steady.ToString("F2")+" ms/change.");System.IO.File.WriteAllText(System.IO.Path.Combine(folder,"reactive-stroke-benchmark.txt"),"Synthetic 4K screenshot. Eight stroke-size changes, including one-time interaction cache: "+perStep.ToString("F2")+" ms/change. First change/setup: "+firstStep.ToString("F2")+" ms. Subsequent changes: "+steady.ToString("F2")+" ms/change. Measures compositing and input processing, not display latency.");
        AddEditableBackdrop(24);AddGrayscaleAdjustment();var colored=new Mark {Kind="Arrow",X=900,Y=1000,W=450,H=140,Width=4,Color=Color.Red.ToArgb()};Marks.Add(colored);Active=colored;Selected.Clear();Selected.Add(colored);SyncStyle();RefreshImage();buffer=Preview;thickness.Value=10;PrepareDragFrame();thickness.Value=14;PrepareDragFrame();FinishStrokeAdjustment();using(var expected=Art.Render(Background,Marks)){var area=Art.DrawingBounds(colored,Background.Size);bool same=true;for(int y=area.Top;y<area.Bottom&&same;y++)for(int x=area.Left;x<area.Right;x++)if(expected.GetPixel(x,y)!=Preview.GetPixel(x,y)){same=false;break;}check(same&&Object.ReferenceEquals(buffer,Preview),"Fast stroke updates preserve presentation background and grayscale compositing");}
        LoadImageDocument(Blank(200,100),"Presentation snapping");AddEditableBackdrop(100);var image=Marks[0];Active=image;Selected.Clear();Selected.Add(image);View.Zoom=1;start=new PointF(120,120);BeginImageSnapping();MoveSnappedImage(new PointF(123,122),Keys.None);check(image.X==100&&image.Y==100&&imageGuideX==200&&imageGuideY==150,"Presentation image snaps to both canvas center guides");MoveSnappedImage(new PointF(143,142),Keys.None);check(image.X==123&&image.Y==122,"Dragging beyond threshold releases center snap without sticking");MoveSnappedImage(new PointF(123,122),Keys.Alt);check(image.X==103&&image.Y==102&&!imageGuideX.HasValue&&!imageGuideY.HasValue,"Alt temporarily bypasses snapping for precise placement");ClearImageSnapping();check(!imageSnapping&&!imageGuideX.HasValue,"Finishing image drag clears alignment guides");float? guide;float third=SnapImageAxis(30,200,new[]{400f/3},6,out guide);check(Math.Abs(third-(400f/3-100))<.01,"Images can snap to off-center thirds");
        Active=new Mark {Kind="Rectangle"};BeginImageSnapping();check(!imageSnapping,"Annotation dragging is not altered by image snapping");Active=image;Selected.Clear();Selected.Add(image);start=new PointF(120,120);image.X=image.Y=100;BeginImageSnapping();MoveSnappedImage(new PointF(123,122),Keys.None);RefreshImage();Show();Application.DoEvents();using(var screenshot=new Bitmap(Width,Height)){DrawToBitmap(screenshot,new Rectangle(Point.Empty,screenshot.Size));screenshot.Save(System.IO.Path.Combine(folder,"image-alignment-guides.png"));}Hide();ClearImageSnapping();LoadImageDocument(Blank(400,200),"Cropped image snapping");SetTool("Crop");Crop=new RectangleF(20,20,200,100);CommitCrop();AddEditableBackdrop(100);Active=Marks[0];Selected.Clear();Selected.Add(Active);View.Zoom=1;start=new PointF(120,120);BeginImageSnapping();MoveSnappedImage(new PointF(123,122),Keys.None);check(Active.X==80&&Active.Y==80&&imageGuideX==200&&imageGuideY==150,"Cropped images snap using visible pixels rather than hidden source bounds");ClearImageSnapping();LoadBitmap(Blank(100,100),"Test");
    }
}
}
