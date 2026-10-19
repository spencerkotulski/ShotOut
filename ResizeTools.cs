using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace ShotOut {
partial class Editor {
    FlowLayoutPanel toolRail;
    bool rearrangingTools;
    [DllImport("user32.dll")] static extern IntPtr ChildWindowFromPointEx(IntPtr parent,Point point,uint flags);
    int SingleColumnHeight(){return toolRail==null?700:toolRail.Padding.Vertical+toolRail.Controls.Cast<Control>().Sum(control=>control.Height+control.Margin.Vertical);}
    void WireToolRail(){toolRail.ClientSizeChanged+=delegate{ReflowToolbar();};ReflowToolbar();}
    void ApplyToolbarPreference(){
        int requested=Preferences.Current.AdaptiveToolbar?740:SingleColumnHeight()+66+42+47+29+14;
        int screenHeight=Screen.FromControl(this).WorkingArea.Height;MinimumSize=new Size(1100,Math.Min(requested,Math.Max(600,screenHeight)));ReflowToolbar();
    }
    void ReflowToolbar(){
        if(toolRail==null||rearrangingTools)return;rearrangingTools=true;
        try{
            bool two=Preferences.Current.AdaptiveToolbar&&toolRail.ClientSize.Height<SingleColumnHeight();
            toolRail.SuspendLayout();toolRail.AutoScroll=false;toolRail.AutoScrollPosition=Point.Empty;toolRail.FlowDirection=two?FlowDirection.LeftToRight:FlowDirection.TopDown;toolRail.WrapContents=two;toolRail.Width=two?110:68;
            foreach(Control control in toolRail.Controls){bool separator=(string)control.Tag=="separator";if(separator){control.Width=two?84:29;control.Margin=two?new Padding(0,8,0,8):new Padding(7,8,0,8);}else control.Margin=two?new Padding(0,1,4,1):new Padding(0,1,0,1);toolRail.SetFlowBreak(control,two&&(separator||(string)control.Tag=="group-end"));}
            toolRail.AutoScroll=!two&&toolRail.ClientSize.Height<SingleColumnHeight();toolRail.ResumeLayout(true);
        }finally{rearrangingTools=false;}
    }
    public void VerifyResizeTools(List<string> report,string folder){
        Action<bool,string> check=delegate(bool ok,string label){if(!ok)throw new Exception(label);report.Add("PASS "+label);};
        bool original=Preferences.Current.AdaptiveToolbar;var oldSize=Size;WindowState=FormWindowState.Normal;Show();Application.DoEvents();
        var edges=new[]{new {Point=new Point(2,ClientSize.Height/2),Hit=10},new {Point=new Point(ClientSize.Width-3,ClientSize.Height/2),Hit=11},new {Point=new Point(ClientSize.Width/2,2),Hit=12},new {Point=new Point(ClientSize.Width/2,ClientSize.Height-3),Hit=15},new {Point=new Point(2,2),Hit=13},new {Point=new Point(ClientSize.Width-3,2),Hit=14},new {Point=new Point(2,ClientSize.Height-3),Hit=16},new {Point=new Point(ClientSize.Width-3,ClientSize.Height-3),Hit=17}};
        foreach(var edge in edges){var screen=PointToScreen(edge.Point);long packed=((long)(ushort)screen.Y<<16)|(ushort)screen.X;var message=Message.Create(Handle,0x84,IntPtr.Zero,new IntPtr(packed));check(HandleIntegratedChrome(ref message)&&message.Result.ToInt32()==edge.Hit,"Native resize hit target "+edge.Hit+" works");check(ChildWindowFromPointEx(Handle,edge.Point,0)==Handle,"Resize edge "+edge.Hit+" is not covered by child controls");}
        Preferences.Current.AdaptiveToolbar=true;ApplyToolbarPreference();Size=MinimumSize;Application.DoEvents();ReflowToolbar();Application.DoEvents();
        check(toolRail.WrapContents&&toolRail.FlowDirection==FlowDirection.LeftToRight,"Smaller window switches toolbar to two columns");
        var buttons=toolRail.Controls.OfType<Button>().ToList();check(buttons.Select(button=>button.Left).Distinct().Count()==2,"Compact toolbar has exactly two tool columns");check(buttons.All(button=>button.Top>=0&&button.Bottom<=toolRail.ClientSize.Height)&&!toolRail.VerticalScroll.Visible,"All tools fit without scrolling in compact layout");
        using(var preview=new Bitmap(Width,Height)){DrawToBitmap(preview,new Rectangle(Point.Empty,preview.Size));preview.Save(System.IO.Path.Combine(folder,"compact-toolbar.png"));}
        Preferences.Current.Save(System.IO.Path.Combine(folder,"toolbar-preferences.xml"));check(Preferences.Read(System.IO.Path.Combine(folder,"toolbar-preferences.xml")).AdaptiveToolbar,"Adaptive toolbar preference persists");
        Preferences.Current.AdaptiveToolbar=false;ApplyToolbarPreference();Application.DoEvents();check(!toolRail.WrapContents&&toolRail.FlowDirection==FlowDirection.TopDown,"Compact toolbar can be disabled");check(MinimumSize.Height>=740,"Single-column layout has a larger minimum height when screen allows");
        Preferences.Current.AdaptiveToolbar=true;ApplyToolbarPreference();Size=oldSize;Application.DoEvents();
        LoadBitmap(Blank(500,350),"Hand tool");Marks.Add(new Mark {Kind="Rectangle",X=30,Y=30,W=80,H=50});SetTool("Hand");float panX=View.PanX,panY=View.PanY;int history=undo.Count;var image=Background;var mark=Marks[0].Clone();
        CanvasDown(new MouseEventArgs(MouseButtons.Left,1,150,140,0));CanvasMove(new MouseEventArgs(MouseButtons.Left,0,185,165,0));CanvasUp(new MouseEventArgs(MouseButtons.Left,0,185,165,0));
        check(View.PanX==panX+35&&View.PanY==panY+25,"Hand tool pans canvas with left-button drag");check(!panning&&!View.Capture&&View.Cursor==Cursors.Hand,"Hand tool releases drag cleanly");check(Object.ReferenceEquals(image,Background)&&Marks.Count==1&&Marks[0].X==mark.X&&Marks[0].Y==mark.Y&&undo.Count==history,"Panning leaves image, annotations, and undo history unchanged");
        SetTool("Rectangle");panX=View.PanX;CanvasDown(new MouseEventArgs(MouseButtons.Middle,1,150,140,0));CanvasMove(new MouseEventArgs(MouseButtons.Middle,0,165,140,0));CanvasUp(new MouseEventArgs(MouseButtons.Middle,0,165,140,0));check(View.PanX==panX+15,"Middle-button panning remains available");
        var legacy=new Preferences();legacy.Tools.RemoveAll(binding=>binding.Action=="Hand");legacy.Tools.First(binding=>binding.Action=="Select").Chord=(int)Keys.Y;string path=System.IO.Path.Combine(folder,"pre-hand-hotkeys.xml");using(var stream=System.IO.File.Create(path))new System.Xml.Serialization.XmlSerializer(typeof(Preferences)).Serialize(stream,legacy);var migrated=Preferences.Read(path);check(migrated.ToolMap()[Keys.Y]=="Select"&&migrated.Tools.First(binding=>binding.Action=="Hand").Chord==0,"Adding Hand preserves existing custom Y shortcut");
        Preferences.Current.AdaptiveToolbar=original;ApplyToolbarPreference();Size=oldSize;Hide();LoadBitmap(Blank(100,100),"Test");
    }
}
}
