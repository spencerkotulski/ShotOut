using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace ShotOut {
class CaptureSplitButton : ModernButton {
    bool arrowPress;
    public event EventHandler DropDownClicked;
    protected override void OnMouseDown(MouseEventArgs e){arrowPress=e.Button==MouseButtons.Left&&e.X>=Width-28;base.OnMouseDown(e);}
    protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);arrowPress=false;}
    protected override void OnClick(EventArgs e){if(arrowPress){arrowPress=false;OpenMenu();}else base.OnClick(e);}
    void OpenMenu(){if(DropDownClicked!=null)DropDownClicked(this,EventArgs.Empty);}
    protected override bool ProcessCmdKey(ref Message message,Keys key){if(key==(Keys.Alt|Keys.Down)||key==Keys.F4){OpenMenu();return true;}return base.ProcessCmdKey(ref message,key);}
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using(var pen=new Pen(Color.FromArgb(100,115,140),1)){e.Graphics.DrawLine(pen,Width-29,9,Width-29,Height-10);}using(var pen=new Pen(ForeColor,1.6f)){float x=Width-15,y=Height/2f;e.Graphics.DrawLines(pen,new[]{new PointF(x-4,y-2),new PointF(x,y+2),new PointF(x+4,y-2)});}}
    internal void TestClick(bool arrow){arrowPress=arrow;OnClick(EventArgs.Empty);}
}
class CaptionButton : ModernButton {
    public string Command;
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using(var pen=new Pen(ForeColor,1.3f)){float x=Width/2f,y=Height/2f;if(Command=="Minimize")e.Graphics.DrawLine(pen,x-5,y+3,x+5,y+3);else if(Command=="Close"){e.Graphics.DrawLine(pen,x-5,y-5,x+5,y+5);e.Graphics.DrawLine(pen,x+5,y-5,x-5,y+5);}else if(Command=="Restore"){e.Graphics.DrawRectangle(pen,x-3,y-6,9,9);using(var brush=new SolidBrush(BackColor))e.Graphics.FillRectangle(brush,x-6,y-3,9,9);e.Graphics.DrawRectangle(pen,x-6,y-3,9,9);}else e.Graphics.DrawRectangle(pen,x-5,y-5,10,10);}}
}
static partial class Art {
    public static Color FillAlpha(Mark mark,Color color){return Color.FromArgb((int)Math.Round(color.A*Math.Max(0,Math.Min(100,mark.FillOpacity))/100),color);}
}
partial class Editor {
    CaptureSplitButton captureSplit;
    NumberField fillOpacity;
    Control fillOpacityRow;
    bool integratedChrome;
    CaptionButton maximizeButton;
    [StructLayout(LayoutKind.Sequential)] struct WindowRect {public int Left,Top,Right,Bottom;}
    [StructLayout(LayoutKind.Sequential)] struct MonitorInformation {public int Size;public WindowRect Monitor,Work;public uint Flags;}
    [StructLayout(LayoutKind.Sequential)] struct MinMaxInformation {public Point Reserved,MaximumSize,MaximumPosition,MinimumTrackSize,MaximumTrackSize;}
    [DllImport("user32.dll")] static extern bool ReleaseCapture();
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr window,int message,IntPtr wParam,IntPtr lParam);
    [DllImport("user32.dll")] static extern IntPtr MonitorFromWindow(IntPtr window,uint flags);
    [DllImport("user32.dll",CharSet=CharSet.Auto)] static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorInformation info);
    Control CreateCaptureSplit(){
        captureSplit=new CaptureSplitButton {Text="Capture",IconName="Capture",Width=142,Height=36,Margin=new Padding(3),BackColor=Color.FromArgb(36,39,45),ForeColor=text,Cursor=Cursors.Hand,TabStop=true,Font=Font,AccessibleName="Capture region ("+CaptureLabel("Region")+")"};
        captureSplit.Click+=delegate{Guard(delegate{Capture("Region");});};captureSplit.DropDownClicked+=delegate{Guard(CaptureOptions);};tips.SetToolTip(captureSplit,"Capture region ("+CaptureLabel("Region")+") · Arrow: capture options · Alt+Down opens menu");return captureSplit;
    }
    void InstallIntegratedChrome(Panel header){
        integratedChrome=true;FormBorderStyle=FormBorderStyle.None;
        header.MouseDown+=delegate(object sender,MouseEventArgs e){if(e.Button==MouseButtons.Left){ReleaseCapture();SendMessage(Handle,0xA1,new IntPtr(2),IntPtr.Zero);}};
        header.DoubleClick+=delegate{ToggleMaximize();};
        var controls=new FlowLayoutPanel {Dock=DockStyle.Right,Width=132,WrapContents=false,Padding=Padding.Empty};
        foreach(string name in new[]{"Minimize","Maximize","Close"}){string command=name;var button=new CaptionButton {Command=command,Text="",Width=40,Height=36,Margin=new Padding(2,3,2,0),BackColor=header.BackColor,ForeColor=text,AccessibleName=command,TabStop=true};button.Click+=delegate{if(command=="Minimize")WindowState=FormWindowState.Minimized;else if(command=="Close")Close();else ToggleMaximize();};tips.SetToolTip(button,command);controls.Controls.Add(button);if(name=="Maximize")maximizeButton=button;}
        header.Controls.Add(controls);Resize+=delegate{if(maximizeButton!=null){maximizeButton.Command=WindowState==FormWindowState.Maximized?"Restore":"Maximize";maximizeButton.AccessibleName=maximizeButton.Command;tips.SetToolTip(maximizeButton,maximizeButton.Command);maximizeButton.Invalidate();}};
    }
    protected override CreateParams CreateParams {get{var parameters=base.CreateParams;if(integratedChrome){parameters.Style|=0x40000|0x20000|0x10000|0x80000;parameters.Style&=~0xC00000;}return parameters;}}
    [StructLayout(LayoutKind.Sequential)] struct ChromeStyleChange {public int Previous,Next;}
    [DllImport("user32.dll",EntryPoint="DefWindowProcW",CharSet=CharSet.Unicode)] static extern IntPtr DefaultChromeWindowProc(IntPtr window,int message,IntPtr wParam,IntPtr lParam);
    protected override void OnEnabledChanged(EventArgs e){base.OnEnabledChanged(e);if(integratedChrome&&Enabled)Invalidate(true);}
    void ToggleMaximize(){WindowState=WindowState==FormWindowState.Maximized?FormWindowState.Normal:FormWindowState.Maximized;}
    bool HandleIntegratedChrome(ref Message message){
        if(!integratedChrome)return false;
        if(message.Msg==0xA&&message.WParam!=IntPtr.Zero)Invalidate(true);
        if(message.Msg==0x83){message.Result=IntPtr.Zero;return true;}
        if(message.Msg==0x85){message.Result=IntPtr.Zero;return true;}
        if(message.Msg==0x86){message.Result=DefaultChromeWindowProc(Handle,message.Msg,message.WParam,new IntPtr(-1));return true;}
        if(message.Msg==0x7C&&message.WParam.ToInt64()==-16&&message.LParam!=IntPtr.Zero){var style=(ChromeStyleChange)Marshal.PtrToStructure(message.LParam,typeof(ChromeStyleChange));style.Next&=~0xC00000;Marshal.StructureToPtr(style,message.LParam,false);message.Result=IntPtr.Zero;return true;}
        if(message.Msg==0x84&&WindowState!=FormWindowState.Maximized){
            long packed=message.LParam.ToInt64();var point=PointToClient(new Point((short)(packed&0xffff),(short)((packed>>16)&0xffff)));int edge=7;bool left=point.X<edge,right=point.X>=ClientSize.Width-edge,top=point.Y<edge,bottom=point.Y>=ClientSize.Height-edge;int hit=top?(left?13:right?14:12):bottom?(left?16:right?17:15):left?10:right?11:0;if(hit!=0){message.Result=new IntPtr(hit);return true;}
        }
        if(message.Msg==0x24&&message.LParam!=IntPtr.Zero){
            var monitor=MonitorFromWindow(Handle,2);var info=new MonitorInformation {Size=Marshal.SizeOf(typeof(MonitorInformation))};if(GetMonitorInfo(monitor,ref info)){var sizes=(MinMaxInformation)Marshal.PtrToStructure(message.LParam,typeof(MinMaxInformation));sizes.MaximumPosition=new Point(info.Work.Left-info.Monitor.Left,info.Work.Top-info.Monitor.Top);sizes.MaximumSize=new Point(info.Work.Right-info.Work.Left,info.Work.Bottom-info.Work.Top);sizes.MinimumTrackSize=new Point(MinimumSize.Width,MinimumSize.Height);Marshal.StructureToPtr(sizes,message.LParam,false);message.Result=IntPtr.Zero;return true;}
        }
        return false;
    }
    static bool HasFillOpacity(string kind){return new[]{"Rectangle","Rounded","Ellipse","Balloon","Bucket","Highlight","Step","Redact","Eraser","Spotlight","Cursor"}.Contains(kind);}
    void AddFillOpacityInspector(){var row=Row();row.Controls.Add(Caption("Fill opacity · %",145));fillOpacity=Number(0,100,100);fillOpacity.ValueChanged+=delegate{ChangeStyle();};row.Controls.Add(fillOpacity);fillOpacityRow=row;inspector.Controls.Add(row);}
    public void VerifyChromeOpacity(List<string> report,string folder){
        Action<bool,string> check=delegate(bool ok,string label){if(!ok)throw new Exception(label);report.Add("PASS "+label);};
        using(var split=new CaptureSplitButton()){int captures=0,menus=0;split.Click+=delegate{captures++;};split.DropDownClicked+=delegate{menus++;};split.TestClick(false);split.TestClick(true);check(captures==1&&menus==1,"Capture split button separates capture and dropdown actions");}
        check((CreateParams.Style&0xC00000)==0,"Window style has no native caption bar");
        var message=Message.Create(Handle,0x83,new IntPtr(1),IntPtr.Zero);check(HandleIntegratedChrome(ref message)&&message.Result==IntPtr.Zero,"Separate native title bar removed while preserving resizable window styles");
        var old=WindowState;ToggleMaximize();check(WindowState==FormWindowState.Maximized,"Integrated maximize control works");ToggleMaximize();check(WindowState==FormWindowState.Normal,"Integrated restore control works");WindowState=old;
        foreach(string kind in new[]{"Rectangle","Rounded","Ellipse","Balloon","Bucket","Highlight","Step","Redact","Eraser","Spotlight","Cursor"})check(HasFillOpacity(kind),"Fill opacity available for "+kind);
        var shape=new Mark {Kind="Rectangle",X=20,Y=20,W=60,H=40,Fill=true,FillColor=Color.Red.ToArgb(),Color=Color.Green.ToArgb(),Width=2,FillOpacity=50};
        using(var bg=Blank(120,90))using(var rendered=Art.Render(bg,new List<Mark>{shape})){var center=rendered.GetPixel(40,40);check(center.R==255&&Math.Abs(center.G-127)<=2,"Half opacity blends fill with underlying image");var edge=rendered.GetPixel(20,40);check(edge.G>edge.R,"Fill opacity preserves outline");}
        shape.FillOpacity=0;using(var bg=Blank(120,90))using(var rendered=Art.Render(bg,new List<Mark>{shape}))check(rendered.GetPixel(40,40).ToArgb()==Color.White.ToArgb(),"Zero opacity removes only shape fill");
        using(var source=Blank(10,10))using(var mask=Art.FloodMask(source,Point.Empty,0)){var bucket=new Mark {Kind="Bucket",X=10,Y=10,W=10,H=10,Image=Art.Bytes(mask.Image),Color=Color.Red.ToArgb(),FillOpacity=50};using(var background=Blank(40,40))using(var rendered=Art.Render(background,new List<Mark>{bucket})){var color=rendered.GetPixel(15,15);check(color.R==255&&Math.Abs(color.G-127)<=2,"Fill opacity applies to bucket layers");}}
        LoadBitmap(Blank(500,300),"Integrated toolbar");Marks.Add(shape);Active=shape;SyncStyle();fillOpacity.Value=35;check(shape.FillOpacity==35,"Opacity inspector edits selected fill");var snapshot=Snapshot();check(snapshot.Marks[0].FillOpacity==35,"Fill opacity persists in editable snapshots");Undo();check(Marks[0].FillOpacity==0,"Fill opacity changes support Undo");Redo();
        Show();Application.DoEvents();using(var preview=new Bitmap(Width,Height)){DrawToBitmap(preview,new Rectangle(Point.Empty,preview.Size));preview.Save(System.IO.Path.Combine(folder,"integrated-interface.png"));}Hide();LoadBitmap(Blank(100,100),"Test");
    }
}
}
