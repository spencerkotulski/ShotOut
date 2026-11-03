using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
namespace ShotOut {
// Shared by window snapping and element capture. WS_VISIBLE alone includes cloaked helpers.
static class CaptureVisibility {
    [StructLayout(LayoutKind.Sequential)] struct Rect {public int L,T,R,B;}
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] static extern bool IsIconic(IntPtr h);
    [DllImport("user32.dll",EntryPoint="GetWindowLongW")] static extern int Style(IntPtr h,int index);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h,out Rect r);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetClassName(IntPtr h,StringBuilder name,int size);
    [DllImport("user32.dll")] static extern bool GetLayeredWindowAttributes(IntPtr h,out uint key,out byte alpha,out uint flags);
    [DllImport("dwmapi.dll",EntryPoint="DwmGetWindowAttribute")] static extern int Cloaked(IntPtr h,int attribute,out int value,int size);
    [DllImport("dwmapi.dll",EntryPoint="DwmGetWindowAttribute")] static extern int Frame(IntPtr h,int attribute,out Rect value,int size);
    [DllImport("user32.dll")] static extern int GetWindowRgn(IntPtr h,IntPtr region);
    [DllImport("gdi32.dll")] static extern IntPtr CreateRectRgn(int l,int t,int r,int b);
    [DllImport("gdi32.dll")] static extern bool PtInRegion(IntPtr region,int x,int y);
    [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr value);
    internal static bool Eligible(bool visible,bool minimized,int cloaked,int style,bool alphaKnown,byte alpha) {
        if(!visible||minimized||cloaked!=0||(style&0x20)!=0)return false;
        if((style&0x80000)!=0&&(alphaKnown&&alpha==0||((style&0x08000080)==0x08000080)))return false;
        return true;
    }
    internal static bool TryBounds(IntPtr h,Point point,out Rectangle bounds) {
        bounds=Rectangle.Empty;if(!IsWindowVisible(h)||IsIconic(h))return false;
        Rect raw;if(!GetWindowRect(h,out raw)||!Rectangle.FromLTRB(raw.L,raw.T,raw.R,raw.B).Contains(point))return false;
        int cloak;Cloaked(h,14,out cloak,4);
        uint key,flags;byte alpha;bool known=GetLayeredWindowAttributes(h,out key,out alpha,out flags)&&(flags&2)!=0;
        if(!Eligible(true,false,cloak,Style(h,-20),known,alpha))return false;
        var name=new StringBuilder(128);GetClassName(h,name,name.Capacity);string cls=name.ToString();
        if(cls=="Progman"||cls=="WorkerW"||cls=="tooltips_class32")return false;
        // A shaped window can have a large invisible bounding rectangle.
        IntPtr region=CreateRectRgn(0,0,0,0);try{int kind=GetWindowRgn(h,region);if(kind==1||kind>1&&!PtInRegion(region,point.X-raw.L,point.Y-raw.T))return false;}finally{DeleteObject(region);}
        Rect frame;if(Frame(h,9,out frame,Marshal.SizeOf(typeof(Rect)))==0)raw=frame;
        bounds=Rectangle.FromLTRB(raw.L,raw.T,raw.R,raw.B);return bounds.Width>2&&bounds.Height>2&&bounds.Contains(point);
    }
}
}
