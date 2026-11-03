using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
namespace ShotOut {
partial class Editor {
    Color highlightColor=Color.Yellow,ordinaryColor=Color.FromArgb(255,100,100);
    readonly Dictionary<string,decimal> rememberedWeights=new Dictionary<string,decimal>();
    static string WeightFamily(string kind){return Art.IsArrow(kind)?"Arrow":kind=="Rounded"?"Rectangle":kind;}
    static bool HasWeight(string kind){return Art.IsArrow(kind)||new[]{"Rectangle","Rounded","Ellipse","Pen","Balloon","Blur","Pixelate","Magnify"}.Contains(kind);}
    decimal DefaultWeight(string kind){return StrokeDefault(kind,Background.Size);}
    void ApplyDrawingDefaults(string next){
        if(thickness==null||colorButton==null)return;string previous=Active!=null?Active.Kind:tool;
        if(previous=="Highlight")highlightColor=ink;else ordinaryColor=ink;

        bool was=syncing;syncing=true;ink=next=="Highlight"?highlightColor:ordinaryColor;colorButton.BackColor=ink;
        if(HasWeight(next)){decimal width;if(!rememberedWeights.TryGetValue(WeightFamily(next),out width))width=DefaultWeight(next);thickness.Minimum=OptionalOutline(next)?0:1;thickness.Value=width;}
        if(next=="Text"||next=="Balloon")fontSize.Value=manualTextPoints??AdaptiveTextPoints(Background.Size);
        syncing=was;
    }
    public bool StableMarkupPreview {get{return !moving&&!resizing&&!dragging&&!strokeAdjusting&&!clonePainting&&inlineTarget==null;}}
    public void VerifyMarkupQuality(List<string> report,string folder){
        Action<bool,string> check=delegate(bool ok,string label){if(!ok)throw new Exception(label);report.Add("PASS "+label);};
        LoadBitmap(Blank(700,400),"Markup quality");highlightColor=Color.Yellow;SetTool("Highlight");check(NewMark("Highlight",PointF.Empty).Color==Color.Yellow.ToArgb(),"New highlight starts yellow");ApplyPickedColor(false,Color.Lime);SetTool("Rectangle");SetTool("Highlight");check(NewMark("Highlight",PointF.Empty).Color==Color.Lime.ToArgb(),"Custom highlight color survives tool switching");
        rememberedWeights.Clear();foreach(string kind in new[]{"Rectangle","Ellipse","Arrow","Double arrow","Curved arrow"}){SetTool("Select");SetTool(kind);check(NewMark(kind,PointF.Empty).Width==(float)DefaultWeight(kind),"Image-sized default for "+kind);}
        SetTool("Rectangle");thickness.Value=7;SetTool("Ellipse");SetTool("Rectangle");check(thickness.Value==7,"Custom shape width is remembered");
        using(var field=new NumberField {Minimum=8,Maximum=240,Value=22,AcceleratedWheel=true}){field.ApplyWheel(120);check(field.Value==25,"Text wheel increases size by about fifteen percent");field.TestArrow(true);check(field.Value==26,"Text up arrow still changes one pixel");field.TestArrow(false);check(field.Value==25,"Text down arrow still changes one pixel");field.Value=100;field.ApplyWheel(120);check(field.Value==115,"Text wheel accelerates at larger sizes");field.ApplyWheel(-120);check(field.Value==98,"Text wheel shrinks faster too");field.Value=22;field.ApplyWheel(60);check(field.Value==22,"Partial wheel movement accumulates without premature size change");field.ApplyWheel(60);check(field.Value==25,"Two half-notches produce one size step");field.Value=240;field.ApplyWheel(120);check(field.Value==240,"Text wheel respects maximum size");field.Value=8;field.ApplyWheel(-120);check(field.Value==8,"Text wheel respects minimum size");}
        using(var field=new NumberField {Minimum=0,Maximum=80,Value=4}){field.ApplyWheel(120);check(field.Value==5,"Other numeric fields retain one-unit wheel increments");}
        check(Art.VectorSamples(new Size(160,80))==4&&Art.VectorSamples(new Size(4000,2000))<4,"Vector rendering uses 4x sampling with a bounded large-object fallback");
        var text=new Mark {Kind="Text",Text="Sharper text",X=50,Y=40,W=420,H=80,Size=38,Color=Color.Black.ToArgb()};var arrow=new Mark {Kind="Arrow",X=70,Y=150,W=320,H=90,Width=4,Color=Color.RoyalBlue.ToArgb()};Marks.Add(text);Marks.Add(arrow);RefreshImage();
        using(var rendered=Art.Render(Background,Marks)){check(rendered.Size==Background.Size&&rendered.GetPixel(650,350).ToArgb()==Background.GetPixel(650,350).ToArgb(),"Export preserves native screenshot dimensions and untouched pixels");int antialiased=0;for(int y=140;y<260;y++)for(int x=50;x<410;x++){var color=rendered.GetPixel(x,y);if(color.R>70&&color.R<250&&color.B>color.R)antialiased++;}check(antialiased>100,"Diagonal markup retains smooth intermediate edge colors");rendered.Save(System.IO.Path.Combine(folder,"high-quality-markup.png"));}
        Show();Application.DoEvents();View.Zoom=.6f;View.PanX=View.PanY=0;using(var image=new Bitmap(View.Width,View.Height))View.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));check(View.HasCrispPreview,"Reduced zoom renders vectors directly at display resolution");int builds=View.CrispPreviewBuilds;View.PanX+=15;using(var image=new Bitmap(View.Width,View.Height))View.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));check(View.CrispPreviewBuilds==builds,"Panning reuses the high-quality display cache");
        Hide();LoadBitmap(Blank(100,100),"Test");
    }
}
static partial class Art {
    public static int VectorSamples(Size size){long pixels=(long)size.Width*size.Height;return pixels*16<=8000000?4:pixels*4<=8000000?2:1;}
    static bool DrawHighQualityVector(Bitmap target,Mark mark,RectangleF bounds,Rectangle? update){
        if(new[]{"Bucket","Spotlight","Image","Redact","Eraser"}.Contains(mark.Kind))return false;
        var expanded=Inflated(bounds,mark.Width*6+4);var box=Rectangle.Intersect(Rectangle.FromLTRB((int)Math.Floor(expanded.Left),(int)Math.Floor(expanded.Top),(int)Math.Ceiling(expanded.Right),(int)Math.Ceiling(expanded.Bottom)),new Rectangle(Point.Empty,target.Size));
        if(box.Width<1||box.Height<1)return true;int samples=VectorSamples(box.Size);if(samples==1)return false;
        using(var overlay=new Bitmap(box.Width*samples,box.Height*samples,PixelFormat.Format32bppPArgb)){
            using(var graphics=Graphics.FromImage(overlay)){graphics.ScaleTransform(samples,samples);graphics.TranslateTransform(-box.X,-box.Y);PaintVector(graphics,target,mark);}
            using(var graphics=Graphics.FromImage(target)){if(update.HasValue)graphics.SetClip(update.Value);graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;graphics.PixelOffsetMode=PixelOffsetMode.HighQuality;graphics.DrawImage(overlay,box);}
        }
        return true;
    }
    public static Mark ScaleMarkup(Mark original,float scale){var mark=original.Clone();mark.RenderScale*=scale;if(mark.HasCropClip)mark.CropClip=new RectangleF(mark.CropClip.X*scale,mark.CropClip.Y*scale,mark.CropClip.Width*scale,mark.CropClip.Height*scale);mark.X*=scale;mark.Y*=scale;mark.W*=scale;mark.H*=scale;mark.Width*=scale;mark.Size*=scale;mark.Radius*=scale;mark.CurveX*=scale;mark.CurveY*=scale;mark.SourceX*=scale;mark.SourceY*=scale;mark.ShadowSize*=scale;mark.TextInsetLeft*=scale;mark.TextInsetTop*=scale;mark.TextInsetRight*=scale;mark.TextInsetBottom*=scale;mark.Points=mark.Points.Select(point=>new PointF(point.X*scale,point.Y*scale)).ToList();return mark;}
    public static void DrawImageQuality(Graphics graphics,Image image,RectangleF bounds){
        if(bounds.X==(int)bounds.X&&bounds.Y==(int)bounds.Y&&bounds.Width==image.Width&&bounds.Height==image.Height){graphics.DrawImageUnscaled(image,(int)bounds.X,(int)bounds.Y);return;}
        using(var attributes=new ImageAttributes()){attributes.SetWrapMode(WrapMode.TileFlipXY);graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;graphics.PixelOffsetMode=PixelOffsetMode.HighQuality;graphics.DrawImage(image,new[]{new PointF(bounds.Left,bounds.Top),new PointF(bounds.Right,bounds.Top),new PointF(bounds.Left,bounds.Bottom)},new RectangleF(0,0,image.Width,image.Height),GraphicsUnit.Pixel,attributes);}
    }
}
partial class Canvas {
    Bitmap crispPreview,crispSource,crispRaster;
    float crispZoom;
    Rectangle crispArea;
    int crispFirst;
    internal int CrispPreviewBuilds;
    internal bool HasCrispPreview {get{return crispPreview!=null;}}
    internal void InvalidateMarkupCache(){ClearCrispPreview();ClearNavigationPreview();ClearStableRaster();}
    void ClearCrispTile(){if(crispPreview!=null)crispPreview.Dispose();crispPreview=null;crispArea=Rectangle.Empty;}
    void ClearCrispPreview(){ClearCrispTile();if(crispRaster!=null)crispRaster.Dispose();crispRaster=crispSource=null;}
    static bool ReadsNativePixels(Mark mark){return mark.Visible&&(mark.Kind=="Blur"||mark.Kind=="Pixelate"||mark.Kind=="Magnify");}
    static bool IsDisplayVector(Mark mark){return mark.Visible&&mark.Kind!="Image"&&mark.Kind!="Bucket"&&mark.Kind!="Grayscale"&&mark.Kind!="Spotlight"&&!ReadsNativePixels(mark);}
    bool DrawCrispPreview(Graphics graphics,Editor editor,PointF origin){
        if(ZoomGestureActive||editor.CanvasPanning)return false;
        if(!editor.StableMarkupPreview){ClearCrispPreview();return false;}
        int first=editor.Marks.FindLastIndex(ReadsNativePixels)+1;
        if(!editor.Marks.Skip(first).Any(IsDisplayVector)){ClearCrispPreview();return false;}
        // Cache only the visible image and a small pan margin, never an enlarged full screenshot.
        var extent=new Rectangle(0,0,Math.Max(1,(int)Math.Ceiling(editor.Background.Width*Zoom)),Math.Max(1,(int)Math.Ceiling(editor.Background.Height*Zoom)));
        var visible=Rectangle.Intersect(extent,Rectangle.FromLTRB((int)Math.Floor(-origin.X),(int)Math.Floor(-origin.Y),(int)Math.Ceiling(Width-origin.X),(int)Math.Ceiling(Height-origin.Y)));
        if(visible.IsEmpty)return true;
        if(!Object.ReferenceEquals(crispSource,editor.Preview)||crispFirst!=first){ClearCrispPreview();crispFirst=first;if(first>0)crispRaster=Art.Render(editor.Background,editor.Marks.Take(first).ToList());crispSource=editor.Preview;}
        if(crispPreview==null||crispZoom!=Zoom||!crispArea.Contains(visible)){
            ClearCrispTile();crispArea=visible;crispArea.Inflate(128,128);crispArea=Rectangle.Intersect(crispArea,extent);
            crispPreview=new Bitmap(crispArea.Width,crispArea.Height,PixelFormat.Format32bppPArgb);
            using(var painter=Graphics.FromImage(crispPreview))Art.DrawImageQuality(painter,crispRaster??editor.Background,new RectangleF(-crispArea.X,-crispArea.Y,editor.Background.Width*Zoom,editor.Background.Height*Zoom));
            for(int i=first;i<editor.Marks.Count;i++){var original=editor.Marks[i];if(!original.Visible)continue;var mark=Art.ScaleMarkup(original,Zoom);mark.Move(-crispArea.X,-crispArea.Y);Art.DrawDisplay(crispPreview,mark);}
            crispZoom=Zoom;CrispPreviewBuilds++;
        }
        var state=graphics.Save();graphics.ResetTransform();graphics.DrawImageUnscaled(crispPreview,(int)Math.Round(origin.X)+crispArea.X,(int)Math.Round(origin.Y)+crispArea.Y);graphics.Restore(state);return true;
    }
    protected override void Dispose(bool disposing){if(disposing){DisposeNavigation();ClearCrispPreview();ClearStableRaster();}base.Dispose(disposing);}
}
}
