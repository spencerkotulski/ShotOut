using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace ShotOut {
partial class Canvas {
    Bitmap zoomSampleSource;
    int[] zoomSamples,zoomOutput,zoomSampleX0,zoomSampleX1,zoomSampleWeights;
    static uint BlendZoomPixel(uint a,uint b,uint weight){uint inverse=256-weight;return (((((a&0x00ff00ff)*inverse+(b&0x00ff00ff)*weight)>>8)&0x00ff00ff)|((((((a>>8)&0x00ff00ff)*inverse+((b>>8)&0x00ff00ff)*weight))&0xff00ff00)));}
    void ClearFastZoomSamples(){zoomSampleSource=null;zoomSamples=zoomOutput=zoomSampleX0=zoomSampleX1=zoomSampleWeights=null;}
    bool TryDrawBilinearZoom(Bitmap image,RectangleF visible,RectangleF source){
        if(visible!=new RectangleF(0,0,Width,Height)||(long)image.Width*image.Height>1000000)return false;
        if(!Object.ReferenceEquals(zoomSampleSource,image)){
            zoomSamples=new int[image.Width*image.Height];var data=image.LockBits(new Rectangle(Point.Empty,image.Size),ImageLockMode.ReadOnly,PixelFormat.Format32bppPArgb);try{for(int y=0;y<image.Height;y++)Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),zoomSamples,y*image.Width,image.Width);}finally{image.UnlockBits(data);}zoomSampleSource=image;
        }
        if(zoomOutput==null||zoomOutput.Length!=Width*Height)zoomOutput=new int[Width*Height];if(zoomSampleX0==null||zoomSampleX0.Length!=Width){zoomSampleX0=new int[Width];zoomSampleX1=new int[Width];zoomSampleWeights=new int[Width];}
        for(int x=0;x<Width;x++){double sample=source.X+(x+.5)*source.Width/Width-.5;int floor=(int)Math.Floor(sample);zoomSampleX0[x]=Math.Max(0,Math.Min(image.Width-1,floor));zoomSampleX1[x]=Math.Max(0,Math.Min(image.Width-1,floor+1));zoomSampleWeights[x]=Math.Max(0,Math.Min(256,(int)Math.Round((sample-floor)*256)));}
        var target=zoomDisplayFrame.LockBits(new Rectangle(Point.Empty,zoomDisplayFrame.Size),ImageLockMode.ReadWrite,PixelFormat.Format32bppPArgb);try{
            for(int y=0;y<Height;y++)Marshal.Copy(IntPtr.Add(target.Scan0,y*target.Stride),zoomOutput,y*Width,Width);
            for(int y=0;y<Height;y++){
                double sample=source.Y+(y+.5)*source.Height/Height-.5;int floor=(int)Math.Floor(sample),row0=Math.Max(0,Math.Min(image.Height-1,floor))*image.Width,row1=Math.Max(0,Math.Min(image.Height-1,floor+1))*image.Width;uint weight=(uint)Math.Max(0,Math.Min(256,(int)Math.Round((sample-floor)*256)));int row=y*Width;
                for(int x=0;x<Width;x++){
                    uint a=(uint)zoomSamples[row0+zoomSampleX0[x]],b=(uint)zoomSamples[row0+zoomSampleX1[x]],c=(uint)zoomSamples[row1+zoomSampleX0[x]],d=(uint)zoomSamples[row1+zoomSampleX1[x]];uint color=a==b&&a==c&&a==d?a:BlendZoomPixel(BlendZoomPixel(a,b,(uint)zoomSampleWeights[x]),BlendZoomPixel(c,d,(uint)zoomSampleWeights[x]),weight);uint alpha=color>>24;
                    if(alpha==255)zoomOutput[row+x]=(int)color;else if(alpha!=0){uint under=(uint)zoomOutput[row+x],inverse=255-alpha;uint blue=(color&255)+((under&255)*inverse+127)/255,green=((color>>8)&255)+(((under>>8)&255)*inverse+127)/255,red=((color>>16)&255)+(((under>>16)&255)*inverse+127)/255;zoomOutput[row+x]=(int)(0xff000000|Math.Min(255,red)<<16|Math.Min(255,green)<<8|Math.Min(255,blue));}
                }
            }
            for(int y=0;y<Height;y++)Marshal.Copy(zoomOutput,y*Width,IntPtr.Add(target.Scan0,y*target.Stride),Width);
        }finally{zoomDisplayFrame.UnlockBits(target);}return true;
    }
}
}
