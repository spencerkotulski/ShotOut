using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Imaging;using System.Runtime.InteropServices;using System.Windows.Forms;
namespace ShotOut {
// Uses D2D1_DRAW_TEXT_OPTIONS_ENABLE_COLOR_FONT:
// https://learn.microsoft.com/windows/win32/api/d2d1/ne-d2d1-d2d1_draw_text_options
// Render the installed Windows color font into a transparent bitmap. The same
// result is composited by Art.Render for the canvas, clipboard and saved images.
static class ColorEmoji {
    const long CacheLimit=32L*1024*1024;
    sealed class Entry {internal string Key;internal Bitmap Image;}
    static readonly object gate=new object();static readonly LinkedList<Entry> cache=new LinkedList<Entry>();static long cacheBytes;
    internal static string LastError;internal static int RenderCount;
    static ColorEmoji(){Application.ApplicationExit+=delegate{Clear();};}
    internal static void Clear(){lock(gate){foreach(var item in cache)item.Image.Dispose();cache.Clear();cacheBytes=0;}}
    internal static bool Draw(Graphics graphics,Mark mark,RectangleF bounds){
        if(String.IsNullOrEmpty(mark.Text)||bounds.Width<=0||bounds.Height<=0)return false;
        float scale=Math.Min(2f,2048f/Math.Max(bounds.Width,bounds.Height));int width=Math.Max(1,(int)Math.Ceiling(bounds.Width*scale)),height=Math.Max(1,(int)Math.Ceiling(bounds.Height*scale));
        string key=mark.Text+"\0"+mark.Size.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"/"+width+"/"+height+"/"+RichLayout.Alignment(mark);
        lock(gate){try{LinkedListNode<Entry> found=cache.First;while(found!=null&&found.Value.Key!=key)found=found.Next;
            if(found==null){var image=Render(mark.Text,Math.Max(1,mark.Size*scale),width,height,RichLayout.Alignment(mark));found=cache.AddFirst(new Entry{Key=key,Image=image});cacheBytes+=(long)width*height*4;while(cache.Count>1&&(cacheBytes>CacheLimit||cache.Count>32)){var last=cache.Last;cacheBytes-=(long)last.Value.Image.Width*last.Value.Image.Height*4;last.Value.Image.Dispose();cache.RemoveLast();}}
            else{cache.Remove(found);cache.AddFirst(found);}graphics.DrawImage(found.Value.Image,bounds);LastError=null;return true;
        }catch(Exception ex){LastError=ex.Message;return false;}}
    }
    [StructLayout(LayoutKind.Sequential)]struct ColorF {internal float R,G,B,A;internal ColorF(float r,float g,float b,float a){R=r;G=g;B=b;A=a;}}
    [StructLayout(LayoutKind.Sequential)]struct RectF {internal float Left,Top,Right,Bottom;}
    [StructLayout(LayoutKind.Sequential)]struct TargetProperties {internal uint Type,Format,AlphaMode;internal float DpiX,DpiY;internal uint Usage,MinLevel;}
    [DllImport("ole32.dll")]static extern int CoInitializeEx(IntPtr reserved,uint flags);
    [DllImport("ole32.dll")]static extern void CoUninitialize();
    [DllImport("ole32.dll")]static extern int CoCreateInstance(ref Guid clsid,IntPtr outer,uint context,ref Guid iid,out IntPtr result);
    [DllImport("d2d1.dll")]static extern int D2D1CreateFactory(uint type,ref Guid iid,IntPtr options,out IntPtr factory);
    [DllImport("dwrite.dll")]static extern int DWriteCreateFactory(uint type,ref Guid iid,out IntPtr factory);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int CreateBitmapFn(IntPtr self,uint width,uint height,ref Guid format,uint cache,out IntPtr bitmap);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int CreateTargetFn(IntPtr self,IntPtr bitmap,ref TargetProperties properties,out IntPtr target);
    [UnmanagedFunctionPointer(CallingConvention.StdCall,CharSet=CharSet.Unicode)]delegate int CreateFormatFn(IntPtr self,[MarshalAs(UnmanagedType.LPWStr)]string family,IntPtr collection,uint weight,uint style,uint stretch,float size,[MarshalAs(UnmanagedType.LPWStr)]string locale,out IntPtr format);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int SetAlignmentFn(IntPtr self,uint alignment);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int CreateBrushFn(IntPtr self,ref ColorF color,IntPtr properties,out IntPtr brush);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate void SimpleFn(IntPtr self);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate void UIntFn(IntPtr self,uint value);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate void ClearFn(IntPtr self,ref ColorF color);
    [UnmanagedFunctionPointer(CallingConvention.StdCall,CharSet=CharSet.Unicode)]delegate void DrawTextFn(IntPtr self,[MarshalAs(UnmanagedType.LPWStr)]string text,uint length,IntPtr format,ref RectF rect,IntPtr brush,uint options,uint measuringMode);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int EndDrawFn(IntPtr self,out ulong firstTag,out ulong secondTag);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int CopyPixelsFn(IntPtr self,IntPtr rect,uint stride,uint size,[Out]byte[] pixels);
    static T Method<T>(IntPtr pointer,int slot)where T:class{return Marshal.GetDelegateForFunctionPointer(Marshal.ReadIntPtr(Marshal.ReadIntPtr(pointer),slot*IntPtr.Size),typeof(T)) as T;}
    static void Check(int hr){Marshal.ThrowExceptionForHR(hr);}
    static void Release(ref IntPtr pointer){if(pointer!=IntPtr.Zero){Marshal.Release(pointer);pointer=IntPtr.Zero;}}
    internal static Bitmap Render(string text,float size,int width,int height,int alignment){
        IntPtr imaging=IntPtr.Zero,bitmap=IntPtr.Zero,d2d=IntPtr.Zero,target=IntPtr.Zero,dwrite=IntPtr.Zero,format=IntPtr.Zero,brush=IntPtr.Zero;
        int initialized=CoInitializeEx(IntPtr.Zero,0);
        try{
            var clsid=new Guid("cacaf262-9370-4615-a13b-9f5539da4c0a");var iid=new Guid("ec5ec8a9-c395-4314-9c77-54d7a935ff70");Check(CoCreateInstance(ref clsid,IntPtr.Zero,1,ref iid,out imaging));
            var pixelFormat=new Guid("6fddc324-4e03-4bfe-b185-3d77768dc910");Check(Method<CreateBitmapFn>(imaging,17)(imaging,(uint)width,(uint)height,ref pixelFormat,2,out bitmap));
            iid=new Guid("06152247-6f50-465a-9245-118bfd3b6007");Check(D2D1CreateFactory(0,ref iid,IntPtr.Zero,out d2d));
            var properties=new TargetProperties{Type=1,Format=87,AlphaMode=1,DpiX=96,DpiY=96};Check(Method<CreateTargetFn>(d2d,13)(d2d,bitmap,ref properties,out target));
            iid=new Guid("b859ee5a-d838-4b5b-a2e8-1adc7d93db48");Check(DWriteCreateFactory(0,ref iid,out dwrite));Check(Method<CreateFormatFn>(dwrite,15)(dwrite,"Segoe UI Emoji",IntPtr.Zero,400,0,5,size,"en-us",out format));
            Check(Method<SetAlignmentFn>(format,3)(format,(uint)(alignment==1?2:alignment==2?1:0)));
            var ink=new ColorF(0,0,0,1);Check(Method<CreateBrushFn>(target,8)(target,ref ink,IntPtr.Zero,out brush));Method<UIntFn>(target,34)(target,2);
            Method<SimpleFn>(target,48)(target);var transparent=new ColorF(0,0,0,0);Method<ClearFn>(target,47)(target,ref transparent);var rect=new RectF{Right=width,Bottom=height};Method<DrawTextFn>(target,27)(target,text,(uint)text.Length,format,ref rect,brush,4,0);ulong tag1,tag2;Check(Method<EndDrawFn>(target,49)(target,out tag1,out tag2));
            var pixels=new byte[checked(width*height*4)];Check(Method<CopyPixelsFn>(bitmap,7)(bitmap,IntPtr.Zero,(uint)(width*4),(uint)pixels.Length,pixels));
            var result=new Bitmap(width,height,PixelFormat.Format32bppPArgb);try{var bits=result.LockBits(new Rectangle(0,0,width,height),ImageLockMode.WriteOnly,PixelFormat.Format32bppPArgb);try{for(int y=0;y<height;y++)Marshal.Copy(pixels,y*width*4,IntPtr.Add(bits.Scan0,y*bits.Stride),width*4);}finally{result.UnlockBits(bits);}RenderCount++;return result;}catch{result.Dispose();throw;}
        }finally{Release(ref brush);Release(ref format);Release(ref dwrite);Release(ref target);Release(ref d2d);Release(ref bitmap);Release(ref imaging);if(initialized>=0)CoUninitialize();}
    }
}
// Keep a real native edit control focused behind this preview so the Windows
// emoji panel and paste still insert Unicode normally, while showing color glyphs.
sealed class ColorEmojiEntryPreview : Control {
    readonly TextBox input;
    internal ColorEmojiEntryPreview(TextBox entry){input=entry;DoubleBuffered=true;TabStop=false;AccessibleName="Selected emoji preview";input.TextChanged+=delegate{AccessibleDescription=input.Text;Invalidate();};input.GotFocus+=delegate{Invalidate();};input.LostFocus+=delegate{Invalidate();};}
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);input.Focus();input.SelectionStart=input.TextLength;}
    protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(BackColor);using(var border=new Pen(input.Focused?UiTheme.Accent:Color.FromArgb(79,86,101)))e.Graphics.DrawRectangle(border,0,0,Width-1,Height-1);var rect=new RectangleF(6,0,Math.Max(1,Width-12),Height);if(input.TextLength==0)TextRenderer.DrawText(e.Graphics,"Choose an emoji…",input.Font,new Rectangle(8,0,Width-16,Height),Color.Gray,TextFormatFlags.VerticalCenter);else if(!ColorEmoji.Draw(e.Graphics,new Mark{Kind="Emoji",Text=input.Text,Size=input.Font.Size,FontFamily="Segoe UI Emoji"},rect))TextRenderer.DrawText(e.Graphics,input.Text,input.Font,Rectangle.Ceiling(rect),Color.White,TextFormatFlags.VerticalCenter);}
}
}
