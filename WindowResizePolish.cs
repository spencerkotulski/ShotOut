using System;using System.Drawing;using System.Windows.Forms;using System.Runtime.InteropServices;using System.Collections.Generic;using System.Diagnostics;
namespace ShotOut {
class ResizeBufferedPanel : Panel {public ResizeBufferedPanel(){DoubleBuffered=true;ResizeRedraw=true;}}
class ResizeBufferedFlowPanel : FlowLayoutPanel {public ResizeBufferedFlowPanel(){DoubleBuffered=true;ResizeRedraw=true;}}
partial class Canvas {
    bool DrawWindowResizePreview(System.Drawing.Graphics graphics,Editor editor){if(!editor.LiveWindowResize)return false;graphics.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.Bilinear;graphics.DrawImage(editor.Preview,0,0,editor.Background.Width,editor.Background.Height);return true;}
}
partial class Editor {
    internal bool LiveWindowResize;
    [DllImport("user32.dll")] static extern int FillRect(IntPtr dc,ref RectangleNative rect,IntPtr brush);
    [DllImport("gdi32.dll")] static extern IntPtr CreateSolidBrush(int color);

    [StructLayout(LayoutKind.Sequential)] struct RectangleNative {public int Left,Top,Right,Bottom;}
    bool PaintResizeBackground(ref Message message){if(message.Msg!=0x14||message.WParam==IntPtr.Zero||!LiveWindowResize)return false;var color=BackColor;var brush=CreateSolidBrush(color.R|(color.G<<8)|(color.B<<16));if(brush==IntPtr.Zero)return false;try{var rect=new RectangleNative {Right=ClientSize.Width,Bottom=ClientSize.Height};if(FillRect(message.WParam,ref rect,brush)==0)return false;message.Result=new IntPtr(1);return true;}finally{DeleteObject(brush);}}
    protected override void OnResizeEnd(EventArgs e){LiveWindowResize=false;base.OnResizeEnd(e);if(View!=null)View.Invalidate();}

    [StructLayout(LayoutKind.Sequential)]struct ResizeWindowPosition {public IntPtr Window,InsertAfter;public int X,Y,Width,Height;public uint Flags;}
    // Prevent Windows from stretching/copying old client pixels while child controls move.
    // Do not use WS_EX_COMPOSITED or synchronous redraws on each resize message.
    void PreventResizePixelCopy(ref Message message){if(message.Msg!=0x46||message.LParam==IntPtr.Zero)return;var position=(ResizeWindowPosition)Marshal.PtrToStructure(message.LParam,typeof(ResizeWindowPosition));if((position.Flags&1)!=0)return;if(LiveWindowResize&&(position.Flags&2)==0&&position.Y!=Top)return;position.Flags|=0x100;Marshal.StructureToPtr(position,message.LParam,false);}
    public void VerifyTopEdgeResize(List<string> report,string folder){var pointer=Marshal.AllocHGlobal(Marshal.SizeOf(typeof(ResizeWindowPosition)));try{LiveWindowResize=true;var p=new ResizeWindowPosition {Y=Top-10,Width=Width,Height=Height+10};Marshal.StructureToPtr(p,pointer,false);var msg=Message.Create(Handle,0x46,IntPtr.Zero,pointer);PreventResizePixelCopy(ref msg);p=(ResizeWindowPosition)Marshal.PtrToStructure(pointer,typeof(ResizeWindowPosition));if((p.Flags&0x100)!=0)throw new Exception("Top-edge resize unnecessarily discards existing pixels");p.Y=Top;p.Flags=0;Marshal.StructureToPtr(p,pointer,false);PreventResizePixelCopy(ref msg);p=(ResizeWindowPosition)Marshal.PtrToStructure(pointer,typeof(ResizeWindowPosition));if((p.Flags&0x100)==0)throw new Exception("Bottom-edge repaint changed");report.Add("PASS Top-edge resize preserves existing pixels while bottom-edge retains its prior repaint behavior");}finally{LiveWindowResize=false;Marshal.FreeHGlobal(pointer);}}
    public void VerifyResizeBackground(List<string> report,string folder){using(var image=new Bitmap(20,20))using(var graphics=Graphics.FromImage(image)){graphics.Clear(Color.White);var dc=graphics.GetHdc();try{var msg=Message.Create(Handle,0x14,dc,IntPtr.Zero);LiveWindowResize=true;if(!PaintResizeBackground(ref msg)||msg.Result!=new IntPtr(1))throw new Exception("Resize background not painted");}finally{graphics.ReleaseHdc(dc);LiveWindowResize=false;}if(image.GetPixel(0,0).ToArgb()!=BackColor.ToArgb())throw new Exception("Resize background is not the window color");}report.Add("PASS Exposed resize background paints the dark window color");}
    public void VerifyWindowResizePolish(List<string> report,string folder){
        var pointer=Marshal.AllocHGlobal(Marshal.SizeOf(typeof(ResizeWindowPosition)));try{
            var position=new ResizeWindowPosition {Width=1200,Height=800};Marshal.StructureToPtr(position,pointer,false);var msg=Message.Create(Handle,0x46,IntPtr.Zero,pointer);PreventResizePixelCopy(ref msg);position=(ResizeWindowPosition)Marshal.PtrToStructure(pointer,typeof(ResizeWindowPosition));if((position.Flags&0x100)==0)throw new Exception("Old resize pixels can still be copied");
            position.Flags=1;Marshal.StructureToPtr(position,pointer,false);PreventResizePixelCopy(ref msg);position=(ResizeWindowPosition)Marshal.PtrToStructure(pointer,typeof(ResizeWindowPosition));if(position.Flags!=1)throw new Exception("Move-only messages were changed");
        }finally{Marshal.FreeHGlobal(pointer);}
        System.IO.File.WriteAllText(System.IO.Path.Combine(folder,"resize-stage.txt"),"Load");LoadBitmap(Blank(1920,1080),"Resize check");var pixels=Preview;System.IO.File.WriteAllText(System.IO.Path.Combine(folder,"resize-stage.txt"),"Show");Show();Application.DoEvents();System.IO.File.WriteAllText(System.IO.Path.Combine(folder,"resize-stage.txt"),"Sizing");WindowState=FormWindowState.Normal;Size=new Size(1320,900);Application.DoEvents();ReflowToolbar();int passes=ToolboxLayoutPasses,generation=screenshotGeneration;OnResizeBegin(EventArgs.Empty);var clock=Stopwatch.StartNew();
        for(int i=0;i<15;i++){Size=new Size(1320+i*2,900+i*2);Application.DoEvents();}
        clock.Stop();OnResizeEnd(EventArgs.Empty);if(ToolboxLayoutPasses!=passes)throw new Exception("Unchanged toolbox rebuilt during resize");if(screenshotGeneration!=generation||!Object.ReferenceEquals(pixels,Preview))throw new Exception("Resize reloaded files or rendered document again");
        System.IO.File.WriteAllText(System.IO.Path.Combine(folder,"resize-stage.txt"),"Maximize");for(int i=0;i<3;i++){ToggleMaximize();Application.DoEvents();ToggleMaximize();Application.DoEvents();}
        if(WindowState!=FormWindowState.Normal||Padding!=new Padding(7))throw new Exception("Restore frame geometry failed");
        System.IO.File.WriteAllText(System.IO.Path.Combine(folder,"resize-stage.txt"),"Gallery");AddWelcomeDocument();Application.DoEvents();int layout=screenshotFiles.GalleryLayoutPasses;screenshotFiles.Height+=20;Application.DoEvents();if(screenshotFiles.GalleryLayoutPasses!=layout)throw new Exception("Height-only gallery resize moved all files");
        using(var screenshot=new Bitmap(Width,Height)){DrawToBitmap(screenshot,new Rectangle(Point.Empty,screenshot.Size));screenshot.Save(System.IO.Path.Combine(folder,"restored-window.png"));}Hide();
        report.Add("PASS Resize discards copied pixels; unchanged toolbox/height-only gallery avoid rebuild; document preview and gallery generation preserved; maximize/restore geometry correct");report.Add("15 live resize/layout steps: "+clock.Elapsed.TotalMilliseconds.ToString("F1")+" ms total (local synthetic check)");LoadBitmap(Blank(100,100),"Test");
    }
}}





