using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace ShotOut {
static class DesktopDpi {
    [StructLayout(LayoutKind.Sequential)] struct Rect {public int Left,Top,Right,Bottom;}
    delegate bool MonitorCallback(IntPtr monitor,IntPtr dc,ref Rect bounds,IntPtr data);
    [DllImport("user32.dll")] static extern bool SetProcessDpiAwarenessContext(IntPtr context);
    [DllImport("shcore.dll")] static extern int SetProcessDpiAwareness(int awareness);
    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] static extern bool EnumDisplayMonitors(IntPtr dc,IntPtr clip,MonitorCallback callback,IntPtr data);
    public static void Initialize(){
        try{if(SetProcessDpiAwarenessContext(new IntPtr(-4)))return;}catch(EntryPointNotFoundException){}
        try{if(SetProcessDpiAwareness(2)==0)return;}catch(DllNotFoundException){}catch(EntryPointNotFoundException){}
        SetProcessDPIAware();
    }
    public static Rectangle UnionScreens(IEnumerable<Rectangle> screens){Rectangle result=Rectangle.Empty;foreach(var screen in screens)if(screen.Width>0&&screen.Height>0)result=result.IsEmpty?screen:Rectangle.Union(result,screen);return result;}
    public static Rectangle ScreenBounds(){
        var screens=new List<Rectangle>();MonitorCallback callback=delegate(IntPtr monitor,IntPtr dc,ref Rect rect,IntPtr data){screens.Add(Rectangle.FromLTRB(rect.Left,rect.Top,rect.Right,rect.Bottom));return true;};
        EnumDisplayMonitors(IntPtr.Zero,IntPtr.Zero,callback,IntPtr.Zero);var bounds=UnionScreens(screens);return bounds.IsEmpty?SystemInformation.VirtualScreen:bounds;
    }
}
partial class CaptureOverlay {
    Rectangle captureBounds;
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
    void PinCaptureBounds(){if(IsHandleCreated&&!captureBounds.IsEmpty)SetWindowPos(Handle,IntPtr.Zero,captureBounds.X,captureBounds.Y,captureBounds.Width,captureBounds.Height,0x14);}
    protected override void OnShown(EventArgs e){PinCaptureBounds();base.OnShown(e);}
    protected override void WndProc(ref Message message){
        // This window represents one frozen, physical-pixel desktop image; do not scale it to a monitor's suggested rectangle.
        if(message.Msg==0x2E0&&!captureBounds.IsEmpty){PinCaptureBounds();message.Result=IntPtr.Zero;return;}
        base.WndProc(ref message);
    }
}
static partial class Art {
    public sealed class FillMask : IDisposable {public Bitmap Image;public Rectangle Bounds;public int Pixels;public void Dispose(){if(Image!=null)Image.Dispose();}}
    public static FillMask FloodMask(Bitmap source,Point seed,int tolerance){
        int width=source.Width,height=source.Height;if(seed.X<0||seed.Y<0||seed.X>=width||seed.Y>=height)return null;
        var bits=source.LockBits(new Rectangle(Point.Empty,source.Size),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);var pixels=new byte[bits.Stride*height];int stride=bits.Stride;
        try{Marshal.Copy(bits.Scan0,pixels,0,pixels.Length);}finally{source.UnlockBits(bits);}
        int offset=seed.Y*stride+seed.X*4,blue=pixels[offset],green=pixels[offset+1],red=pixels[offset+2],alpha=pixels[offset+3],limit=Math.Max(0,Math.Min(100,tolerance))*255/100;
        var filled=new byte[width*height];
        Func<int,int,bool> matches=delegate(int x,int y){if(filled[y*width+x]!=0)return false;int i=y*stride+x*4;if(alpha==0&&pixels[i+3]==0)return true;return Math.Abs(pixels[i]-blue)<=limit&&Math.Abs(pixels[i+1]-green)<=limit&&Math.Abs(pixels[i+2]-red)<=limit&&Math.Abs(pixels[i+3]-alpha)<=limit;};
        var stack=new Stack<Point>();stack.Push(seed);int left=width,top=height,right=-1,bottom=-1,count=0;
        while(stack.Count>0){var point=stack.Pop();if(!matches(point.X,point.Y))continue;int x1=point.X,x2=point.X;
            while(x1>0&&matches(x1-1,point.Y))x1--;while(x2+1<width&&matches(x2+1,point.Y))x2++;
            bool above=false,below=false;
            for(int x=x1;x<=x2;x++){filled[point.Y*width+x]=1;count++;
                bool matchAbove=point.Y>0&&matches(x,point.Y-1),matchBelow=point.Y+1<height&&matches(x,point.Y+1);
                if(matchAbove&&!above)stack.Push(new Point(x,point.Y-1));if(matchBelow&&!below)stack.Push(new Point(x,point.Y+1));above=matchAbove;below=matchBelow;
            }
            left=Math.Min(left,x1);right=Math.Max(right,x2);top=Math.Min(top,point.Y);bottom=Math.Max(bottom,point.Y);
        }
        if(count==0)return null;var region=Rectangle.FromLTRB(left,top,right+1,bottom+1);var image=new Bitmap(region.Width,region.Height,PixelFormat.Format32bppArgb);
        var target=image.LockBits(new Rectangle(Point.Empty,image.Size),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);try{var data=new byte[target.Stride*image.Height];for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++)if(filled[(y+top)*width+x+left]!=0){int i=y*target.Stride+x*4;data[i]=data[i+1]=data[i+2]=data[i+3]=255;}Marshal.Copy(data,0,target.Scan0,data.Length);}finally{image.UnlockBits(target);}
        return new FillMask {Image=image,Bounds=region,Pixels=count};
    }
    static void PaintBucket(Graphics graphics,Mark mark,RectangleF bounds){
        if(mark.Image==null)return;using(var mask=FromBytes(mark.Image))using(var attributes=new ImageAttributes()){
            var color=Color.FromArgb(mark.Color);var matrix=new ColorMatrix {Matrix00=0,Matrix11=0,Matrix22=0,Matrix33=color.A/255f*Math.Max(0,Math.Min(100,mark.FillOpacity))/100,Matrix40=color.R/255f,Matrix41=color.G/255f,Matrix42=color.B/255f};attributes.SetColorMatrix(matrix);
            graphics.DrawImage(mask,new[]{new PointF(bounds.Left,bounds.Top),new PointF(bounds.Right,bounds.Top),new PointF(bounds.Left,bounds.Bottom)},new RectangleF(0,0,mask.Width,mask.Height),GraphicsUnit.Pixel,attributes);
        }
    }
}
partial class Editor {
    Control bucketToleranceRow;NumberField bucketTolerance;
    void AddBucketInspector(){var row=Row();row.Controls.Add(Caption("Tolerance · %",145));bucketTolerance=Number(0,100,15);tips.SetToolTip(bucketTolerance,"Match similar colors for the next fill. 0 matches exactly.");row.Controls.Add(bucketTolerance);bucketToleranceRow=row;inspector.Controls.Add(row);}
    void BucketFill(PointF point){
        using(var source=Art.Render(Background,Marks))using(var mask=Art.FloodMask(source,new Point((int)point.X,(int)point.Y),(int)bucketTolerance.Value)){
            if(mask==null)return;var mark=NewMark("Bucket",mask.Bounds.Location);mark.Name="Bucket fill";mark.W=mask.Bounds.Width;mark.H=mask.Bounds.Height;mark.Image=Art.Bytes(mask.Image);
            Push();Marks.Add(mark);Active=mark;Selected.Clear();Selected.Add(mark);RefreshImage();UpdateStatus("Filled connected area · Ctrl+Z to undo.");
        }
    }
    public void VerifyDpiAndBucket(List<string> report,string folder){
        Action<bool,string> check=delegate(bool ok,string label){if(!ok)throw new Exception(label);report.Add("PASS "+label);};
        var monitors=new[]{new Rectangle(0,0,2560,1440),new Rectangle(-1920,0,1920,1080),new Rectangle(0,-2160,3840,2160)};
        check(DesktopDpi.UnionScreens(monitors)==Rectangle.FromLTRB(-1920,-2160,3840,1440),"Physical monitor union includes full laptop, external monitor, and negative coordinates");
        using(var bitmap=Blank(200,120))using(var overlay=new CaptureOverlay(bitmap,new Rectangle(-200,-120,200,120),false,delegate{return new Rectangle(0,0,200,120);})){
            overlay.Show();Application.DoEvents();var change=Message.Create(overlay.Handle,0x2E0,IntPtr.Zero,IntPtr.Zero);overlay.TestDpiMessage(ref change);
            check(overlay.ClientSize==new Size(200,120)&&overlay.Bounds.Location==new Point(-200,-120),"Capture overlay retains physical-pixel bounds during DPI changes");overlay.Hide();
        }
        using(var bitmap=Blank(80,60)){
            using(var graphics=Graphics.FromImage(bitmap)){graphics.FillRectangle(Brushes.Black,39,0,2,60);graphics.FillRectangle(Brushes.LightGray,0,30,39,30);}
            using(var mask=Art.FloodMask(bitmap,new Point(5,5),0))check(mask.Pixels==39*30&&mask.Bounds==new Rectangle(0,0,39,30),"Bucket fills only the connected exact-color area");
            using(var mask=Art.FloodMask(bitmap,new Point(5,5),20))check(mask.Pixels==39*60&&mask.Bounds.Width==39,"Bucket tolerance includes similar colors without crossing dark outline");
        }
        LoadBitmap(Blank(100,80),"Bucket test");using(var graphics=Graphics.FromImage(Background))graphics.DrawRectangle(Pens.Black,20,15,50,40);SetTool("Bucket");ink=Color.Red;bucketTolerance.Value=0;BucketFill(new PointF(30,30));
        check(Active.Kind=="Bucket"&&Active.Image!=null&&Marks.Count==1,"Bucket produces editable independent fill layer");
        using(var rendered=Art.Render(Background,Marks))check(rendered.GetPixel(30,30).R==255&&rendered.GetPixel(30,30).G==0&&rendered.GetPixel(5,5).ToArgb()==Color.White.ToArgb(),"Bucket fills inside annotation boundary and preserves outside");
        ink=Color.Blue;ChangeStyle();using(var rendered=Art.Render(Background,Marks))check(rendered.GetPixel(30,30).B==255&&rendered.GetPixel(30,30).R==0,"Bucket layer color can be changed afterward");Undo();Undo();check(Marks.Count==0,"Bucket fill and color change undo independently");Redo();check(Marks.Count==1,"Bucket fill supports redo");
        var preferences=Preferences.Current.Clone();check(preferences.ToolMap().ContainsValue("Bucket"),"Bucket shortcut is available in settings");
        var legacy=new Preferences();legacy.Tools.RemoveAll(binding=>binding.Action=="Bucket");legacy.Tools.First(binding=>binding.Action=="Select").Chord=(int)Keys.Q;string legacyFile=System.IO.Path.Combine(folder,"legacy-hotkeys.xml");using(var stream=System.IO.File.Create(legacyFile))new System.Xml.Serialization.XmlSerializer(typeof(Preferences)).Serialize(stream,legacy);var migrated=Preferences.Read(legacyFile);check(migrated.ToolMap()[Keys.Q]=="Select"&&migrated.Tools.First(binding=>binding.Action=="Bucket").Chord==0,"Adding bucket preserves existing custom Q shortcut");
        Show();Application.DoEvents();using(var preview=new Bitmap(Width,Height)){DrawToBitmap(preview,new Rectangle(Point.Empty,preview.Size));preview.Save(System.IO.Path.Combine(folder,"bucket-interface.png"));}Hide();
        using(var source=Blank(3840,2160)){var watch=System.Diagnostics.Stopwatch.StartNew();using(var mask=Art.FloodMask(source,Point.Empty,0))check(mask.Pixels==3840*2160,"Bucket handles full 4K connected area without recursion");watch.Stop();report.Add("4K bucket mask: "+watch.ElapsedMilliseconds+" ms.");}
        LoadBitmap(Blank(100,100),"Test");
    }
}
partial class CaptureOverlay {internal void TestDpiMessage(ref Message message){WndProc(ref message);}}
}
