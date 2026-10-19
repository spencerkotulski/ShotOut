using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
namespace ShotOut {
class CropRatioButton : ModernButton {
    public float Ratio;
    public string Label;
    protected override void OnPaint(PaintEventArgs e){
        base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;float ratio=Ratio>0?Ratio:1.5f;float width=Math.Min(22,16*ratio),height=width/ratio;var rect=new RectangleF((Width-width)/2,3+(16-height)/2,width,height);
        using(var pen=new Pen(ForeColor,1.4f)){if(Ratio==0)pen.DashStyle=DashStyle.Dash;g.DrawRectangle(pen,rect.X,rect.Y,rect.Width,rect.Height);}
        using(var font=new Font("Segoe UI",7.5f))TextRenderer.DrawText(g,Label,font,new Rectangle(0,19,Width,14),ForeColor,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
    }
}
class CropRatioPicker : FlowLayoutPanel {
    readonly List<CropRatioButton> choices=new List<CropRatioButton>();
    readonly string[] labels={"Free","1:1","3:4","4:3","5:4"};
    int selected;
    public event EventHandler SelectedIndexChanged;
    public int SelectedIndex {get{return selected;}set{if(value<0||value>=choices.Count)return;bool changed=value!=selected;selected=value;for(int i=0;i<choices.Count;i++){choices[i].Selected=i==selected;choices[i].Invalidate();}if(changed&&SelectedIndexChanged!=null)SelectedIndexChanged(this,EventArgs.Empty);}}
    public string SelectedItem {get{return labels[selected];}}
    public CropRatioPicker(){WrapContents=false;Padding=Padding.Empty;float[] ratios={0,1,3f/4,4f/3,5f/4};for(int i=0;i<labels.Length;i++){int index=i;var button=new CropRatioButton {Ratio=ratios[i],Label=labels[i],Text="",Width=45,Height=34,Margin=new Padding(0,0,4,0),BackColor=Color.FromArgb(43,46,54),ForeColor=Color.FromArgb(233,236,243),AccessibleName=labels[i]=="Free"?"Free crop ratio":"Crop ratio "+labels[i]};button.Click+=delegate{SelectedIndex=index;};choices.Add(button);Controls.Add(button);}SelectedIndex=0;}
}
partial class Editor {
    CropRatioPicker cropRatio;
    bool cropGesture;
    int cropHandle;
    RectangleF cropOriginal;
    PointF cropAnchor;
    float CropAspect {get {switch(cropRatio.SelectedIndex){case 1:return 1;case 2:return 3f/4;case 3:return 4f/3;case 4:return 5f/4;default:return 0;}}}
    void InstallCropRatio(FlowLayoutPanel panel){
        cropRatio=new CropRatioPicker {Width=245,Height=34,Margin=new Padding(0,3,5,0),AccessibleName="Crop aspect ratio"};
        foreach(Control button in cropRatio.Controls)tips.SetToolTip(button,button.AccessibleName);
        cropRatio.SelectedIndexChanged+=delegate {if(Crop.HasValue){var r=Crop.Value;float ratio=CropAspect;if(ratio>0){float w=Math.Min(r.Width,r.Height*ratio);r=new RectangleF(r.X+(r.Width-w)/2,r.Y+(r.Height-w/ratio)/2,w,w/ratio);}Crop=r;View.Invalidate();}};
        panel.Controls.Add(cropRatio);
    }
    void StartFullImageCrop(){cropGesture=false;Selected.Clear();cropRatio.SelectedIndex=0;Crop=new RectangleF(PointF.Empty,Background.Size);}
    int CropHit(PointF point){
        if(!Crop.HasValue)return -1;var handles=Handles(Crop.Value);float tolerance=7/View.Zoom;
        for(int i=0;i<handles.Length;i++)if(Math.Abs(point.X-handles[i].X)<=tolerance&&Math.Abs(point.Y-handles[i].Y)<=tolerance)return i;
        return Crop.Value.Contains(point)?8:-1;
    }
    void BeginCropGesture(PointF point){if(!Crop.HasValue)StartFullImageCrop();cropHandle=CropHit(point);if(cropHandle<0){View.Capture=false;return;}cropOriginal=Crop.Value;cropAnchor=point;cropGesture=true;View.Invalidate();}
    RectangleF FitCrop(RectangleF rect){
        float factor=Math.Min(1,Math.Min(Background.Width/Math.Max(1,rect.Width),Background.Height/Math.Max(1,rect.Height)));
        rect.Width*=factor;rect.Height*=factor;rect.X=Math.Max(0,Math.Min(Background.Width-rect.Width,rect.X));rect.Y=Math.Max(0,Math.Min(Background.Height-rect.Height,rect.Y));return rect;
    }
    RectangleF CropFromPoints(PointF anchor,PointF point){
        float dx=point.X-anchor.X,dy=point.Y-anchor.Y,ratio=CropAspect;
        if(ratio>0){float width=Math.Min(Math.Abs(dx),Math.Abs(dy)*ratio);dx=(dx<0?-1:1)*width;dy=(dy<0?-1:1)*width/ratio;}
        return FitCrop(RectangleF.FromLTRB(Math.Min(anchor.X,anchor.X+dx),Math.Min(anchor.Y,anchor.Y+dy),Math.Max(anchor.X,anchor.X+dx),Math.Max(anchor.Y,anchor.Y+dy)));
    }
    bool HandleCropMove(PointF point){
        if(!cropGesture){int hit=CropHit(point);View.Cursor=hit==8?Cursors.SizeAll:hit==1||hit==5?Cursors.SizeNS:hit==3||hit==7?Cursors.SizeWE:hit==0||hit==4?Cursors.SizeNWSE:hit==2||hit==6?Cursors.SizeNESW:Cursors.Cross;return true;}
        var r=cropOriginal;float ratio=CropAspect;
        if(cropHandle==8){r.Offset(point.X-cropAnchor.X,point.Y-cropAnchor.Y);Crop=FitCrop(r);}
        else if(cropHandle%2==0){var opposite=Handles(r)[(cropHandle+4)%8];Crop=CropFromPoints(opposite,point);}
        else {
            float left=r.Left,right=r.Right,top=r.Top,bottom=r.Bottom;
            if(cropHandle==1)top=Math.Min(point.Y,bottom-1);if(cropHandle==5)bottom=Math.Max(point.Y,top+1);
            if(cropHandle==7)left=Math.Min(point.X,right-1);if(cropHandle==3)right=Math.Max(point.X,left+1);
            if(ratio>0){if(cropHandle==1||cropHandle==5){float width=(bottom-top)*ratio;left=r.Left+(r.Width-width)/2;right=left+width;}else {float height=(right-left)/ratio;top=r.Top+(r.Height-height)/2;bottom=top+height;}}
            Crop=FitCrop(RectangleF.FromLTRB(left,top,right,bottom));
        }
        View.Invalidate();return true;
    }
    bool EndCropGesture(){if(!cropGesture)return false;cropGesture=false;View.Capture=false;UpdateStatus("Adjust crop handles or drag inside · Enter / Apply crop to confirm · Esc cancels.");View.Invalidate();return true;}
    public void DrawCropHandles(Graphics graphics){
        if(tool!="Crop"||!Crop.HasValue)return;float size=7/View.Zoom;using(var pen=new Pen(Color.FromArgb(95,155,255),1/View.Zoom))foreach(var point in Handles(Crop.Value)){var box=new RectangleF(point.X-size/2,point.Y-size/2,size,size);graphics.FillRectangle(Brushes.White,box);graphics.DrawRectangle(pen,box.X,box.Y,box.Width,box.Height);}
    }
    void ApplyEditableCrop(Rectangle area){
        Push();var next=Background.Clone(area,PixelFormat.Format32bppArgb);Background.Dispose();Background=next;
        foreach(var mark in Marks){bool needsClip=mark.HasCropClip||!area.Contains(Rectangle.Ceiling(mark.Bounds));var clip=mark.HasCropClip?RectangleF.Intersect(mark.CropClip,area):area;mark.Move(-area.X,-area.Y);if(mark.SourceInitialized){mark.SourceX-=area.X;mark.SourceY-=area.Y;}clip.Offset(-area.X,-area.Y);mark.CropClip=clip;mark.HasCropClip=needsClip;}
        ResetCanvasBase();Active=null;Selected.Clear();Crop=null;cropGesture=false;RefreshImage();View.Fit();
    }
    public void VerifyEditableCrop(List<string> report,string folder){
        Action<bool,string> check=delegate(bool ok,string label){if(!ok)throw new Exception(label);report.Add("PASS "+label);};
        LoadImageDocument(Blank(500,400),"Editable crop");var mark=new Mark {Kind="Rectangle",X=70,Y=80,W=180,H=150,Width=4,Fill=true,FillColor=Color.Red.ToArgb()};Marks.Add(mark);var words=new Mark {Kind="Text",X=120,Y=110,W=140,H=50,Text="Editable",Size=30,Color=Color.Black.ToArgb()};Marks.Add(words);SetTool("Crop");Crop=new RectangleF(100,100,200,200);int count=Marks.Count;CommitCrop();
        check(Background.Size==new Size(200,200)&&Marks.Count==count&&Marks.Contains(words)&&words.Text=="Editable","Crop preserves image layers and editable text and shapes");check(mark.X==-30&&words.X==20,"Crop translates layer coordinates without changing geometry");EnsureCanvasFitsMarks();check(Background.Size==new Size(200,200),"Cropped layer overflow does not automatically undo crop");
        Active=words;Selected.Clear();Selected.Add(words);SyncStyle();fontSize.Value=500;FinishStrokeAdjustment();check(words.Size==500&&fontSize.Maximum==500,"Text size can reach 500 pixels");Undo();Undo();check(Background.Size==new Size(500,400)&&Marks.Count==count,"Undo restores pre-crop layers and canvas");Redo();check(Background.Size==new Size(200,200),"Redo reapplies editable crop");
        var snapshot=Snapshot();using(var stream=new System.IO.MemoryStream()){var xml=new System.Xml.Serialization.XmlSerializer(typeof(Project));xml.Serialize(stream,snapshot);stream.Position=0;Restore((Project)xml.Deserialize(stream));}check(Marks.Any(m=>m.HasCropClip),"Editable crop clipping survives project serialization");
        using(var original=Art.Render(Background,Marks)){ResizeCanvas(300,200,0,0);using(var enlarged=Art.Render(Background,Marks))check(enlarged.GetPixel(230,40).A==0,"Expanding canvas does not reveal cropped image or annotation pixels");}
        LoadBitmap(Blank(500,400),"Crop handles");SetTool("Crop");check(Crop.Value==new RectangleF(0,0,500,400),"Crop starts with the entire image selected");View.Zoom=1;Crop=new RectangleF(100,100,200,180);BeginCropGesture(new PointF(300,190));HandleCropMove(new PointF(350,190));EndCropGesture();check(Crop.Value.Width==250&&Crop.Value.Left==100,"Crop right edge adjusts without redrawing selection");BeginCropGesture(new PointF(180,170));HandleCropMove(new PointF(200,190));EndCropGesture();check(Crop.Value.Left==120&&Crop.Value.Top==120,"Crop region moves by dragging inside");
        foreach(int index in new[]{1,2,3,4}){Crop=new RectangleF(0,0,500,400);cropRatio.SelectedIndex=index;check(Math.Abs(Crop.Value.Width/Crop.Value.Height-CropAspect)<.001,"Visual ratio "+cropRatio.SelectedItem+" constrains selection");var corner=new PointF(Crop.Value.Right,Crop.Value.Bottom);BeginCropGesture(corner);HandleCropMove(new PointF(corner.X-30,corner.Y-40));EndCropGesture();check(Math.Abs(Crop.Value.Width/Crop.Value.Height-CropAspect)<.001,"Crop ratio stays locked while resizing "+cropRatio.SelectedItem);}
        var previous=Crop.Value;BeginCropGesture(new PointF(-50,-50));HandleCropMove(new PointF(50,50));EndCropGesture();check(Crop.Value==previous,"Dragging outside the crop does not replace it with a new selection");
        cropRatio.SelectedIndex=0;
        for(int handle=0;handle<8;handle++){Crop=new RectangleF(100,100,200,160);var point=Handles(Crop.Value)[handle];var origin=View.Origin;Func<PointF,MouseEventArgs> mouse=q=>new MouseEventArgs(MouseButtons.Left,1,(int)Math.Round(origin.X+q.X*View.Zoom),(int)Math.Round(origin.Y+q.Y*View.Zoom),0);CanvasDownWithModifiers(mouse(point),Keys.None);var end=new PointF(point.X+12,point.Y+12);CanvasMove(mouse(end));CanvasUp(mouse(end));check(Crop.Value!=new RectangleF(100,100,200,160)&&!cropGesture&&!View.Capture,"Mouse adjusts crop handle "+handle+" and releases capture");}
        Crop=new RectangleF(80,60,300,240);Show();Application.DoEvents();using(var image=new Bitmap(Width,Height)){DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(System.IO.Path.Combine(folder,"adjustable-crop.png"));}Hide();Crop=null;LoadBitmap(Blank(100,100),"Test");
    }
}
static partial class Art {
    static void DrawCroppedMarkup(Bitmap target,Mark mark,bool quality,Rectangle? update){
        var clip=Rectangle.Intersect(Rectangle.Ceiling(mark.CropClip),new Rectangle(Point.Empty,target.Size));if(update.HasValue)clip=Rectangle.Intersect(clip,update.Value);if(clip.Width<1||clip.Height<1)return;
        var unclipped=mark.Clone();unclipped.HasCropClip=false;
        if(mark.Kind!="Blur"&&mark.Kind!="Pixelate"&&mark.Kind!="Magnify"){DrawUnclipped(target,unclipped,quality,clip);return;}
        using(var rendered=(Bitmap)target.Clone()){DrawUnclipped(rendered,unclipped,quality,clip);using(var graphics=Graphics.FromImage(target)){graphics.CompositingMode=CompositingMode.SourceCopy;graphics.DrawImage(rendered,clip,clip,GraphicsUnit.Pixel);}}
    }
}
}
