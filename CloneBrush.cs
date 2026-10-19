using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace ShotOut {
partial class Editor {
    float cloneStrokeHardness=1,cloneStrokeOpacity=1;
    readonly Dictionary<Point,Bitmap> cloneUnderlay=new Dictionary<Point,Bitmap>();
    readonly Dictionary<Point,byte[]> cloneColors=new Dictionary<Point,byte[]>(),clonePaint=new Dictionary<Point,byte[]>();
    Bitmap cloneTip;
    byte[] cloneTipPixels;
    float[] cloneTipMask;
    float cloneTipHardness=-1;
    static float CloneCoverage(float distance,float radius,float hardness){
        if(hardness>=1)return Math.Max(0,Math.Min(1,radius-distance+.5f));
        float t=Math.Max(0,Math.Min(1,(radius-distance)/Math.Max(.001f,radius*(1-hardness))));
        return t*t*(3-2*t);
    }
    // All working images are 32-bit ARGB with positive, tightly packed rows.
    static void ReadClonePixels(Bitmap image,byte[] pixels){var data=image.LockBits(new Rectangle(Point.Empty,image.Size),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);try{for(int y=0;y<image.Height;y++)Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),pixels,y*image.Width*4,image.Width*4);}finally{image.UnlockBits(data);}}
    static void WriteClonePixels(Bitmap image,byte[] pixels){var data=image.LockBits(new Rectangle(Point.Empty,image.Size),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);try{for(int y=0;y<image.Height;y++)Marshal.Copy(pixels,y*image.Width*4,IntPtr.Add(data.Scan0,y*data.Stride),image.Width*4);}finally{image.UnlockBits(data);}}
    void ClearSoftCloneTiles(){foreach(var image in cloneUnderlay.Values)image.Dispose();cloneUnderlay.Clear();cloneColors.Clear();clonePaint.Clear();}
    void PaintSoftCloneSegment(PointF from,PointF to){
        float radius=(float)cloneDiameter.Value/2;
        var valid=RectangleF.Intersect(new RectangleF(-cloneOffset.X,-cloneOffset.Y,cloneSample.Width,cloneSample.Height),new RectangleF(PointF.Empty,Background.Size));
        var area=Rectangle.Intersect(Rectangle.Ceiling(Inflate(RectangleF.FromLTRB(Math.Min(from.X,to.X),Math.Min(from.Y,to.Y),Math.Max(from.X,to.X),Math.Max(from.Y,to.Y)),radius+1)),Rectangle.Ceiling(valid));
        if(area.IsEmpty||area.Width<1||area.Height<1)return;
        float dx=to.X-from.X,dy=to.Y-from.Y,length=dx*dx+dy*dy;
        for(int ty=area.Top/256;ty<=(area.Bottom-1)/256;ty++)for(int tx=area.Left/256;tx<=(area.Right-1)/256;tx++){
            var key=new Point(tx,ty);var tileBounds=new Rectangle(tx*256,ty*256,256,256);Bitmap tile;
            if(!cloneTiles.TryGetValue(key,out tile)){
                tile=new Bitmap(256,256,PixelFormat.Format32bppArgb);cloneTiles.Add(key,tile);
                using(var graphics=Graphics.FromImage(tile)){graphics.InterpolationMode=InterpolationMode.NearestNeighbor;graphics.PixelOffsetMode=PixelOffsetMode.Half;graphics.TranslateTransform(-tileBounds.X,-tileBounds.Y);graphics.SetClip(valid);graphics.FillRectangle(cloneBrush,tileBounds);}
                var source=new byte[256*256*4];ReadClonePixels(tile,source);using(var graphics=Graphics.FromImage(tile))graphics.Clear(Color.Transparent);cloneColors.Add(key,source);clonePaint.Add(key,new byte[source.Length]);
                var underlay=new Bitmap(256,256,PixelFormat.Format32bppArgb);using(var graphics=Graphics.FromImage(underlay)){graphics.CompositingMode=CompositingMode.SourceCopy;graphics.DrawImageUnscaled(Preview,-tileBounds.X,-tileBounds.Y);}cloneUnderlay.Add(key,underlay);
            }
            var clip=Rectangle.Intersect(area,tileBounds);var colors=cloneColors[key];var pixels=clonePaint[key];bool changed=false;
            for(int y=clip.Top;y<clip.Bottom;y++)for(int x=clip.Left;x<clip.Right;x++){
                float px=x+.5f-from.X,py=y+.5f-from.Y;float t=length==0?0:Math.Max(0,Math.Min(1,(px*dx+py*dy)/length));float ex=px-t*dx,ey=py-t*dy;
                int index=((y-tileBounds.Y)*256+x-tileBounds.X)*4;byte alpha=(byte)Math.Round(colors[index+3]*cloneStrokeOpacity*CloneCoverage((float)Math.Sqrt(ex*ex+ey*ey),radius,cloneStrokeHardness));
                // Maximum coverage makes stroke opacity independent of mouse event rate.
                if(alpha<=pixels[index+3])continue;pixels[index]=colors[index];pixels[index+1]=colors[index+1];pixels[index+2]=colors[index+2];pixels[index+3]=alpha;changed=true;
            }
            if(!changed)continue;WriteClonePixels(tile,pixels);
            using(var graphics=Graphics.FromImage(Preview)){graphics.CompositingMode=CompositingMode.SourceCopy;graphics.DrawImageUnscaled(cloneUnderlay[key],tileBounds.Location);graphics.CompositingMode=CompositingMode.SourceOver;graphics.DrawImageUnscaled(tile,tileBounds.Location);}
        }
        cloneBounds=UnionBounds(cloneBounds,area);View.InvalidateMarkupCache();InvalidateDragArea(area);
    }
    void ClearCloneTip(){if(cloneTip!=null)cloneTip.Dispose();cloneTip=null;cloneTipPixels=null;cloneTipMask=null;cloneTipHardness=-1;}
    void DrawCloneTip(Graphics graphics,RectangleF circle,PointF source){
        int size=(int)cloneDiameter.Value;float hardness=(float)cloneHardness.Value/100;
        if(cloneTip==null||cloneTip.Width!=size){ClearCloneTip();cloneTip=new Bitmap(size,size,PixelFormat.Format32bppArgb);cloneTipPixels=new byte[size*size*4];cloneTipMask=new float[size*size];}
        if(cloneTipHardness!=hardness){for(int y=0;y<size;y++)for(int x=0;x<size;x++){float dx=x+.5f-size/2f,dy=y+.5f-size/2f;cloneTipMask[y*size+x]=CloneCoverage((float)Math.Sqrt(dx*dx+dy*dy),size/2f,hardness);}cloneTipHardness=hardness;}
        using(var target=Graphics.FromImage(cloneTip)){target.Clear(Color.Transparent);target.InterpolationMode=InterpolationMode.NearestNeighbor;target.PixelOffsetMode=PixelOffsetMode.Half;target.DrawImage(cloneSample,new Rectangle(0,0,size,size),(float)Math.Floor(circle.X)+source.X-cloneHover.X,(float)Math.Floor(circle.Y)+source.Y-cloneHover.Y,size,size,GraphicsUnit.Pixel);}
        ReadClonePixels(cloneTip,cloneTipPixels);float opacity=.45f*(float)cloneOpacity.Value/100;
        for(int i=0;i<cloneTipMask.Length;i++)cloneTipPixels[i*4+3]=(byte)Math.Round(cloneTipPixels[i*4+3]*cloneTipMask[i]*opacity);
        WriteClonePixels(cloneTip,cloneTipPixels);graphics.DrawImage(cloneTip,new RectangleF((float)Math.Floor(circle.X),(float)Math.Floor(circle.Y),size,size));
    }
    public void VerifyClonePixelQuality(System.Collections.Generic.List<string> report,string folder){
        Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);report.Add("PASS "+label);};
        LoadImageDocument(Blank(700,340),"Clone pixel fidelity");using(var image=Blank(700,340)){using(var painter=Graphics.FromImage(image)){painter.SmoothingMode=SmoothingMode.None;for(int y=10;y<140;y++)for(int x=10;x<160;x++)if((x+y)%2==0)painter.FillRectangle(Brushes.Black,x,y,1,1);using(var font=new Font("Segoe UI",18,FontStyle.Regular,GraphicsUnit.Pixel)){painter.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;painter.DrawString("Text fidelity",font,Brushes.Black,12,145);}}Marks[0].Image=Art.Bytes(image);}RefreshImage();SetTool("Clone stamp");cloneDiameter.Value=80;cloneOpacity.Value=100;
        foreach(float zoom in new[]{.65f,1.3f,2f})foreach(int hardness in new[]{100,50})foreach(int sourceY in new[]{60,160}){
            View.Zoom=zoom;var origin=View.Origin;Func<int,int,System.Windows.Forms.MouseEventArgs> mouse=(x,y)=>new System.Windows.Forms.MouseEventArgs(System.Windows.Forms.MouseButtons.Left,1,(int)Math.Round(origin.X+x*zoom),(int)Math.Round(origin.Y+y*zoom),0);
            cloneHardness.Value=hardness;HandleCloneDown(mouse(55,sourceY),System.Windows.Forms.Keys.Alt);var target=View.ImagePoint(mouse(285,sourceY+50).Location);var expectedOffset=new Point((int)Math.Round(cloneSource.X-target.X),(int)Math.Round(cloneSource.Y-target.Y));HandleCloneDown(mouse(285,sourceY+50),System.Windows.Forms.Keys.None);HandleCloneMove(mouse(310,sourceY+50));
            Func<Bitmap,bool> exact=bitmap=>{for(int y=(int)target.Y-10;y<(int)target.Y+10;y++)for(int x=(int)target.X-10;x<(int)target.X+10;x++)if(bitmap.GetPixel(x,y)!=cloneSample.GetPixel(x+expectedOffset.X,y+expectedOffset.Y)){System.IO.File.AppendAllText(System.IO.Path.Combine(folder,"pixel-mismatch.txt"),"at "+x+","+y+" actual "+bitmap.GetPixel(x,y)+" expected "+cloneSample.GetPixel(x+expectedOffset.X,y+expectedOffset.Y)+" offset "+cloneOffset+" expectedOffset "+expectedOffset+Environment.NewLine);return false;}return true;};
            check(exact(Preview),"Native clone pixels match their source while painting at "+zoom+" zoom, hardness "+hardness+", "+(sourceY==60?"fine pattern":"text"));FinishCloneStroke();check(exact(Preview),"Committed clone keeps source pixels at "+zoom+" zoom, hardness "+hardness);using(var exported=Art.Render(Background,Marks))using(var png=Art.FromBytes(Art.Bytes(exported)))check(exact(png),"PNG export preserves cloned source pixels at "+zoom+" zoom, hardness "+hardness);
        }
        using(var image=Art.Render(Background,Marks))image.Save(System.IO.Path.Combine(folder,"clone-pixel-fidelity.png"));ResetCloneSource();cloneHardness.Value=100;cloneDiameter.Value=32;
    }
    void VerifyCloneBrush(System.Collections.Generic.List<string> report,string folder){
        Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);report.Add("PASS "+label);};
        LoadImageDocument(Blank(512,256),"Clone brush controls");using(var image=Blank(512,256)){using(var graphics=Graphics.FromImage(image)){graphics.Clear(Color.White);graphics.FillRectangle(Brushes.Red,0,0,150,256);}Marks[0].Image=Art.Bytes(image);}RefreshImage();SetTool("Clone stamp");View.Zoom=1;var origin=View.Origin;Func<int,int,System.Windows.Forms.MouseEventArgs> mouse=(x,y)=>new System.Windows.Forms.MouseEventArgs(System.Windows.Forms.MouseButtons.Left,1,(int)origin.X+x,(int)origin.Y+y,0);
        HandleCloneDown(mouse(50,100),System.Windows.Forms.Keys.Alt);cloneDiameter.Value=80;cloneHardness.Value=0;cloneOpacity.Value=50;cloneHover=new PointF(300,100);
        using(var preview=(Bitmap)Preview.Clone()){using(var graphics=Graphics.FromImage(preview))DrawCloneCursor(graphics);check(preview.GetPixel(300,100).G>195&&preview.GetPixel(300,132).G>preview.GetPixel(300,100).G,"Clone preview reflects opacity and soft hardness");preview.Save(System.IO.Path.Combine(folder,"clone-soft-preview.png"));}
        HandleCloneDown(mouse(300,100),System.Windows.Forms.Keys.None);HandleCloneMove(mouse(350,100));var center=Preview.GetPixel(320,100);var edge=Preview.GetPixel(320,132);check(center.G>110&&center.G<160&&edge.G>220,"Soft clone brush blends center opacity and feathers its edge");HandleCloneMove(mouse(300,100));HandleCloneMove(mouse(350,100));check(Preview.GetPixel(320,100)==center,"Repeated passes within a stroke do not exceed its opacity");FinishCloneStroke();check(Preview.GetPixel(320,100)==center&&Preview.GetPixel(320,132)==edge,"Soft clone live preview matches committed layer pixels");
        HandleCloneDown(mouse(300,100),System.Windows.Forms.Keys.None);HandleCloneMove(mouse(350,100));FinishCloneStroke();check(Marks.Count==2&&Preview.GetPixel(320,100).G<center.G,"Separate soft strokes build opacity on the same clone layer");Undo();check(Preview.GetPixel(320,100)==center,"Undo restores the previous soft clone stroke");cloneOpacity.Value=0;int history=undo.Count;HandleCloneDown(mouse(300,100),System.Windows.Forms.Keys.None);check(!clonePainting&&undo.Count==history&&Preview.GetPixel(320,100)==center,"Zero opacity leaves pixels and undo history untouched");
        cloneOpacity.Value=100;cloneHardness.Value=100;HandleCloneDown(mouse(300,100),System.Windows.Forms.Keys.None);HandleCloneMove(mouse(350,100));FinishCloneStroke();check(Preview.GetPixel(320,132).G<10,"Full hardness retains a crisp opaque brush interior");
        Show();System.Windows.Forms.Application.DoEvents();UpdateInspector();check(cloneOpacity.Visible&&cloneOpacityRow.Width>=210&&cloneOpacity.Bottom<=cloneOpacityRow.ClientSize.Height,"Clone opacity control is fully visible in the inspector");using(var preview=new Bitmap(Width,Height)){DrawToBitmap(preview,new Rectangle(Point.Empty,preview.Size));preview.Save(System.IO.Path.Combine(folder,"clone-brush-controls.png"));}Hide();ResetCloneSource();cloneDiameter.Value=32;
    }

}
}
