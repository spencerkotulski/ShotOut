using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace ShotOut {
static partial class Art {
    [ThreadStatic] static byte[] copyRow;
    public static void CopyPixelRegion(Bitmap source,Bitmap target,Rectangle region){
        region=Rectangle.Intersect(region,new Rectangle(Point.Empty,source.Size));region=Rectangle.Intersect(region,new Rectangle(Point.Empty,target.Size));if(region.Width<=0||region.Height<=0||Object.ReferenceEquals(source,target))return;
        int bytes=region.Width*4;if(copyRow==null||copyRow.Length<bytes)copyRow=new byte[bytes];var from=source.LockBits(region,ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);try{var to=target.LockBits(region,ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);try{for(int y=0;y<region.Height;y++){Marshal.Copy(IntPtr.Add(from.Scan0,y*from.Stride),copyRow,0,bytes);Marshal.Copy(copyRow,0,IntPtr.Add(to.Scan0,y*to.Stride),bytes);}}finally{target.UnlockBits(to);}}finally{source.UnlockBits(from);}
    }
    class DecodedLayer {public byte[] Source;public Bitmap Image;public long Bytes;}
    static readonly List<DecodedLayer> decodedLayers=new List<DecodedLayer>();
    const long DecodedLayerBudget=40L*1024*1024;
    static long decodedLayerBytes;
    internal static int LayerImageDecodes;
    internal static long LayerImageCacheBytes {get{return decodedLayerBytes;}}
    internal static void ClearLayerImageCache(){ClearImageToneCache();foreach(var layer in decodedLayers)layer.Image.Dispose();decodedLayers.Clear();decodedLayerBytes=0;}
    static void DrawLayerImage(Graphics graphics,byte[] source,RectangleF bounds,Mark mark=null){
        for(int i=0;i<decodedLayers.Count;i++)if(Object.ReferenceEquals(decodedLayers[i].Source,source)){var layer=decodedLayers[i];if(i>0){decodedLayers.RemoveAt(i);decodedLayers.Insert(0,layer);}DrawAdjustedLayer(graphics,layer.Image,bounds,mark);return;}
        var image=FromBytes(source);LayerImageDecodes++;long bytes=(long)image.Width*image.Height*4;
        if(bytes>DecodedLayerBudget){using(image)DrawAdjustedLayer(graphics,image,bounds,mark);return;}
        while(decodedLayers.Count>0&&(decodedLayerBytes+bytes>DecodedLayerBudget||decodedLayers.Count>=4)){int last=decodedLayers.Count-1;var old=decodedLayers[last];decodedLayers.RemoveAt(last);decodedLayerBytes-=old.Bytes;old.Image.Dispose();}
        decodedLayers.Insert(0,new DecodedLayer {Source=source,Image=image,Bytes=bytes});decodedLayerBytes+=bytes;DrawAdjustedLayer(graphics,image,bounds,mark);
    }
}
partial class Canvas {
    Bitmap stableRaster,stableRasterSource;
    PointF stableRasterOrigin;
    float stableRasterZoom;
    internal int StableRasterBuilds;
    internal bool DisableStableRasterForTests;
    void ClearStableRaster(){if(stableRaster!=null)stableRaster.Dispose();stableRaster=stableRasterSource=null;}
    bool DrawStableRaster(Graphics graphics,Editor editor){
        if(DisableStableRasterForTests||!editor.StableMarkupPreview||editor.CanvasPanning||ZoomGestureActive)return false;var origin=new PointF((float)Math.Round(Origin.X),(float)Math.Round(Origin.Y));
        if(stableRaster==null||!Object.ReferenceEquals(stableRasterSource,editor.Preview)||stableRaster.Size!=ClientSize||stableRasterOrigin!=origin||stableRasterZoom!=Zoom){
            ClearStableRaster();if(Width<1||Height<1)return false;stableRaster=new Bitmap(Width,Height,PixelFormat.Format32bppPArgb);using(var painter=Graphics.FromImage(stableRaster)){painter.Clear(BackColor);DrawCanvasChecker(painter,editor.Background.Size);painter.TranslateTransform(origin.X,origin.Y);painter.ScaleTransform(Zoom,Zoom);painter.InterpolationMode=InterpolationMode.HighQualityBicubic;painter.PixelOffsetMode=PixelOffsetMode.HighQuality;painter.DrawImage(editor.Preview,0,0,editor.Background.Width,editor.Background.Height);}stableRasterSource=editor.Preview;stableRasterOrigin=origin;stableRasterZoom=Zoom;StableRasterBuilds++;
        }
        var state=graphics.Save();try{graphics.ResetTransform();graphics.DrawImageUnscaled(stableRaster,0,0);}finally{graphics.Restore(state);}return true;
    }
}
partial class Editor {
    float penLeft,penTop,penRight,penBottom;
    void AppendPenPoint(PointF point){if(draft.Points.Count==1){var first=draft.Points[0];penLeft=penRight=first.X;penTop=penBottom=first.Y;}draft.Points.Add(point);penLeft=Math.Min(penLeft,point.X);penRight=Math.Max(penRight,point.X);penTop=Math.Min(penTop,point.Y);penBottom=Math.Max(penBottom,point.Y);draft.X=penLeft;draft.Y=penTop;draft.W=Math.Max(1,penRight-penLeft);draft.H=Math.Max(1,penBottom-penTop);}
}
}

