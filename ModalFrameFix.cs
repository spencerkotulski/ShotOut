using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace ShotOut {
partial class Editor {
    [DllImport("user32.dll",EntryPoint="GetWindowLongW")] static extern int FrameWindowStyle(IntPtr window,int index);
    public void VerifyModalFrameFix(List<string> report,string folder){
        Action<bool,string> check=delegate(bool ok,string label){if(!ok)throw new Exception(label);report.Add("PASS "+label);};
        var paint=Message.Create(Handle,0x85,new IntPtr(1),IntPtr.Zero);check(HandleIntegratedChrome(ref paint)&&paint.Result==IntPtr.Zero,"Native nonclient painting cannot draw over custom toolbar");
        var calculate=Message.Create(Handle,0x83,IntPtr.Zero,IntPtr.Zero);check(HandleIntegratedChrome(ref calculate),"Simple frame recalculation preserves borderless client area");
        IntPtr pointer=Marshal.AllocHGlobal(Marshal.SizeOf(typeof(ChromeStyleChange)));try{Marshal.StructureToPtr(new ChromeStyleChange {Previous=CreateParams.Style,Next=CreateParams.Style|0xC00000},pointer,false);var change=Message.Create(Handle,0x7C,new IntPtr(-16),pointer);check(HandleIntegratedChrome(ref change),"Runtime style changes are checked for unwanted caption bits");var result=(ChromeStyleChange)Marshal.PtrToStructure(pointer,typeof(ChromeStyleChange));check((result.Next&0xC00000)==0&&(result.Next&0x40000)!=0,"Runtime frame guard removes title bar while retaining resize support");}finally{Marshal.FreeHGlobal(pointer);}
        LoadImageDocument(Blank(600,360),"Text dialog regression");SetTool("Text");Show();Application.DoEvents();
        bool disabled=false,paintSuppressed=false;
        try{
            for(int round=0;round<3;round++){
                using(var timer=new System.Windows.Forms.Timer {Interval=100}){
                    int attempts=0;timer.Tick+=delegate{var dialog=Application.OpenForms.Cast<Form>().OfType<ApplyPrompt>().FirstOrDefault(form=>form.Text=="Edit annotation");if(dialog!=null){disabled|=(FrameWindowStyle(Handle,-16)&0x8000000)!=0;var inactive=Message.Create(Handle,0x86,IntPtr.Zero,IntPtr.Zero);paintSuppressed|=HandleIntegratedChrome(ref inactive);timer.Stop();dialog.Controls.OfType<RichTextBox>().Single().Text="Frame stays clean";dialog.ApplyText();}else if(++attempts>50){timer.Stop();throw new Exception("Text dialog did not open");}};timer.Start();
                    var mark=new Mark {Kind="Text",Text="Test",Size=22};check(RichTextEntry.Ask(this,mark)=="Frame stays clean","Text editor opens and applies round "+(round+1));
                }
                Application.DoEvents();check(Enabled&&(FrameWindowStyle(Handle,-16)&0xC00000)==0,"No native title bar after text dialog round "+(round+1));check(PointToScreen(Point.Empty)==Location&&ClientSize==Size,"Toolbar client area remains aligned with window round "+(round+1));
            }
        }finally{}
        check(disabled&&paintSuppressed,"Modal owner activation is handled without native frame repaint");
        var activation=Message.Create(Handle,0x86,new IntPtr(1),IntPtr.Zero);check(HandleIntegratedChrome(ref activation),"Reactivation uses default activation handling without repainting border");
        ToggleMaximize();Application.DoEvents();ToggleMaximize();Application.DoEvents();check((FrameWindowStyle(Handle,-16)&0xC00000)==0,"Maximize and restore retain clean frame after dialogs");
        using(var preview=new Bitmap(Width,Height)){DrawToBitmap(preview,new Rectangle(Point.Empty,preview.Size));preview.Save(System.IO.Path.Combine(folder,"modal-frame-interface.png"));}
        Hide();LoadBitmap(Blank(100,100),"Test");
    }
}
}
