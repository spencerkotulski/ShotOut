using System;
using System.Drawing;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
namespace ShotOut {
partial class Editor {
    internal Bitmap InteractionDisplayBase {get{return dragBase;}}
    internal int InteractionDisplayFirst {get{return dragFirst;}}
    internal bool CanDisplayLiveVectors {
        get{if(dragBase==null||inlineTarget!=null||dragMutable==null||!(moving||resizing||dragging||strokeAdjusting))return false;for(int i=dragFirst;i<Marks.Count;i++)if(Marks[i].Visible&&!LiveVectorKind(Marks[i].Kind))return false;return true;}
    }
    static bool LiveVectorKind(string kind){return Art.IsArrow(kind)||kind=="Rectangle"||kind=="Rounded"||kind=="Ellipse"||kind=="Pen"||kind=="Text"||kind=="Balloon"||kind=="Emoji"||kind=="Step"||kind=="Highlight"||kind=="Redact"||kind=="Eraser"||kind=="Cursor";}
}
partial class Canvas {
    Bitmap interactionDisplayBase,interactionDisplayFrame,interactionDisplaySource,zoomDisplayFrame;
    PointF interactionDisplayOrigin;
    float interactionDisplayZoom;
    internal int InteractiveVectorFrames,InteractiveBaseBuilds;internal double ZoomRasterMs,ZoomBlitMs;
    internal void ClearInteractionDisplayCache(){ClearInteractiveDisplay();}
    void ClearInteractiveDisplay(){if(interactionDisplayBase!=null)interactionDisplayBase.Dispose();if(interactionDisplayFrame!=null)interactionDisplayFrame.Dispose();interactionDisplayBase=interactionDisplayFrame=interactionDisplaySource=null;}
    void ClearZoomDisplay(){if(zoomDisplayFrame!=null)zoomDisplayFrame.Dispose();zoomDisplayFrame=null;}
    bool DrawInteractiveVectors(Graphics graphics,Editor editor){
        if(editor.CanvasPanning||ZoomGestureActive||!editor.CanDisplayLiveVectors)return false;var source=editor.InteractionDisplayBase;var origin=new PointF((float)Math.Round(Origin.X),(float)Math.Round(Origin.Y));if(Width<1||Height<1)return false;
        if(interactionDisplayBase==null||!Object.ReferenceEquals(interactionDisplaySource,source)||interactionDisplayBase.Size!=ClientSize||interactionDisplayOrigin!=origin||interactionDisplayZoom!=Zoom){
            ClearInteractiveDisplay();interactionDisplayBase=new Bitmap(Width,Height,PixelFormat.Format32bppPArgb);interactionDisplayFrame=new Bitmap(Width,Height,PixelFormat.Format32bppPArgb);
            using(var painter=Graphics.FromImage(interactionDisplayBase)){painter.Clear(BackColor);DrawCanvasChecker(painter,editor.Background.Size);Art.DrawImageQuality(painter,source,new RectangleF(origin.X,origin.Y,editor.Background.Width*Zoom,editor.Background.Height*Zoom));}interactionDisplaySource=source;interactionDisplayOrigin=origin;interactionDisplayZoom=Zoom;InteractiveBaseBuilds++;
        }
        using(var painter=Graphics.FromImage(interactionDisplayFrame)){painter.CompositingMode=CompositingMode.SourceCopy;painter.DrawImageUnscaled(interactionDisplayBase,0,0);}
        var canvas=new RectangleF(origin.X,origin.Y,editor.Background.Width*Zoom,editor.Background.Height*Zoom);
        for(int i=editor.InteractionDisplayFirst;i<editor.Marks.Count;i++){var original=editor.Marks[i];if(!original.Visible)continue;var mark=Art.ScaleMarkup(original,Zoom);mark.Move(origin.X,origin.Y);mark.CropClip=mark.HasCropClip?RectangleF.Intersect(mark.CropClip,canvas):canvas;mark.HasCropClip=true;Art.DrawDisplay(interactionDisplayFrame,mark);}
        var state=graphics.Save();try{graphics.ResetTransform();graphics.DrawImageUnscaled(interactionDisplayFrame,0,0);}finally{graphics.Restore(state);}InteractiveVectorFrames++;return true;
    }
    bool CanFastZoom(RectangleF visible,Bitmap image){return visible==new RectangleF(0,0,Width,Height)&&(long)image.Width*image.Height<=1000000;}
    void DrawZoomDisplay(Graphics graphics,Bitmap image,RectangleF visible,RectangleF source){
        if(zoomDisplayFrame==null||zoomDisplayFrame.Size!=ClientSize){ClearZoomDisplay();if(Width<1||Height<1)return;zoomDisplayFrame=new Bitmap(Width,Height,PixelFormat.Format32bppPArgb);}
        var profile=Stopwatch.StartNew();using(var painter=Graphics.FromImage(zoomDisplayFrame)){painter.Clear(BackColor);DrawCanvasChecker(painter,OwnerEditor.Background.Size);painter.InterpolationMode=InterpolationMode.Bilinear;painter.PixelOffsetMode=PixelOffsetMode.HighQuality;painter.CompositingQuality=CompositingQuality.HighSpeed;if(!CanFastZoom(visible,image))painter.DrawImage(image,new[]{visible.Location,new PointF(visible.Right,visible.Top),new PointF(visible.Left,visible.Bottom)},source,GraphicsUnit.Pixel,navigationEdges);}
        if(CanFastZoom(visible,image))TryDrawBilinearZoom(image,visible,source);ZoomRasterMs+=profile.Elapsed.TotalMilliseconds;profile.Restart();var state=graphics.Save();try{graphics.ResetTransform();graphics.DrawImageUnscaled(zoomDisplayFrame,0,0);ZoomBlitMs+=profile.Elapsed.TotalMilliseconds;}finally{graphics.Restore(state);}
    }
}
}


