using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
namespace ShotOut {
sealed class TrayClickRouter : IDisposable {
    readonly Timer timer=new Timer();readonly Action single,twice;DateTime ignoreUntil;
    internal bool Pending {get{return timer.Enabled;}}
    public TrayClickRouter(Action capture,Action open){single=capture;twice=open;timer.Interval=SystemInformation.DoubleClickTime+50;timer.Tick+=delegate{Flush();};}
    public void Click(){if(DateTime.UtcNow<ignoreUntil)return;timer.Stop();timer.Start();}
    public void DoubleClick(){timer.Stop();ignoreUntil=DateTime.UtcNow.AddMilliseconds(SystemInformation.DoubleClickTime);twice();}
    internal void Flush(){if(!timer.Enabled)return;timer.Stop();single();}
    public void Dispose(){timer.Dispose();}
}
sealed class CaptureToast : Form {
    readonly Bitmap thumbnail;readonly Timer expiry=new Timer();readonly Action open;
    protected override bool ShowWithoutActivation {get{return true;}}
    protected override CreateParams CreateParams {get{var p=base.CreateParams;p.ExStyle|=0x08000000|0x80;return p;}}
    public CaptureToast(Bitmap image,Rectangle workingArea,Action clicked){
        open=clicked;Text="ShotOut capture preview";AccessibleName="Captured screenshot. Click to edit.";FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;StartPosition=FormStartPosition.Manual;ClientSize=new Size(Math.Min(320,workingArea.Width-24),230);BackColor=Color.FromArgb(32,35,43);ForeColor=Color.White;DoubleBuffered=true;Cursor=Cursors.Hand;
        Location=new Point(workingArea.Right-Width-16,workingArea.Bottom-Height-16);
        thumbnail=new Bitmap(Math.Max(1,Width-20),170);using(var g=Graphics.FromImage(thumbnail)){g.Clear(Color.FromArgb(22,24,29));float scale=Math.Min((float)thumbnail.Width/image.Width,(float)thumbnail.Height/image.Height);int w=Math.Max(1,(int)(image.Width*scale)),h=Math.Max(1,(int)(image.Height*scale));g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(image,new Rectangle((thumbnail.Width-w)/2,(thumbnail.Height-h)/2,w,h));}
        var close=new ModernButton {Text="×",Width=30,Height=28,Location=new Point(Width-36,194),BackColor=BackColor,ForeColor=ForeColor,AccessibleName="Dismiss capture preview"};close.Click+=delegate{Close();};Controls.Add(close);
        expiry.Interval=12000;expiry.Tick+=delegate{Close();};expiry.Start();MouseEnter+=delegate{expiry.Stop();};MouseLeave+=delegate{if(!ClientRectangle.Contains(PointToClient(MousePosition)))expiry.Start();};
    }
    internal void OpenCapture(){open();Close();}
    protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);if(e.Button==MouseButtons.Left)OpenCapture();}
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);e.Graphics.DrawImageUnscaled(thumbnail,10,10);using(var pen=new Pen(Color.FromArgb(85,95,116)))e.Graphics.DrawRectangle(pen,0,0,Width-1,Height-1);TextRenderer.DrawText(e.Graphics,"Screenshot captured · Click to edit",SystemFonts.MessageBoxFont,new Rectangle(12,187,Width-52,34),ForeColor,TextFormatFlags.VerticalCenter|TextFormatFlags.WordBreak);}
    protected override void Dispose(bool disposing){if(disposing){expiry.Dispose();thumbnail.Dispose();}base.Dispose(disposing);}
}
sealed class EffectTile : ModernButton {
    public Bitmap Preview;public string Caption;
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(Preview!=null)e.Graphics.DrawImageUnscaled(Preview,7,9);TextRenderer.DrawText(e.Graphics,Caption,Font,new Rectangle(104,5,Width-111,Height-10),ForeColor,TextFormatFlags.VerticalCenter|TextFormatFlags.WordBreak);}
    protected override void Dispose(bool disposing){if(disposing&&Preview!=null){Preview.Dispose();Preview=null;}base.Dispose(disposing);}
}
partial class Editor {
    CaptureToast capturePreview;
    TrayClickRouter trayClicks;
    FlowLayoutPanel effectsPane;
    ContextMenuStrip effectActions;
    Panel effectsResizeGrip;
    bool effectsResizing;
    int effectsResizeStart,effectsResizeWidth,effectsDefaultWidth,effectsSavedWidth;
    void CloseCapturePreview(){if(capturePreview!=null){var old=capturePreview;capturePreview=null;old.Close();old.Dispose();}}
    void FinishCaptureVisibility(string result,bool wasVisible){if(result!=null?Preferences.Current.OpenEditorAfterCapture:wasVisible)RestoreFromTray();}
    void ShowCapturePreview(){
        CloseCapturePreview();StoreDocument();var document=documents[currentDocument];capturePreview=new CaptureToast(Preview,Screen.FromPoint(Cursor.Position).WorkingArea,delegate{int index=documents.IndexOf(document);if(index>=0)ActivateDocument(index);RestoreFromTray();});
        var toast=capturePreview;toast.FormClosed+=delegate{if(Object.ReferenceEquals(capturePreview,toast))capturePreview=null;};if(!Preferences.Testing)toast.Show();
    }
    void CloseImageEffects(){
        if(effectsPane==null)return;effectsSavedWidth=inspector.Parent.Width;effectsResizing=false;if(effectsResizeGrip!=null){effectsResizeGrip.Capture=false;Controls.Remove(effectsResizeGrip);effectsResizeGrip.Dispose();effectsResizeGrip=null;}inspector.Parent.Width=effectsDefaultWidth;var pane=effectsPane;effectsPane=null;pane.Parent.Controls.Remove(pane);pane.Dispose();if(effectActions!=null){effectActions.Dispose();effectActions=null;}inspector.Visible=true;ApplyLayerVisibility();UpdateInspector();
    }
    void ShowImageEffects(ContextMenuStrip menu){
        CloseImageEffects();effectActions=menu;var host=inspector.Parent;inspector.Visible=layers.Visible=layerTitle.Visible=layerButtons.Visible=false;
        effectsPane=new VerticalInspector {Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,BackColor=host.BackColor,Padding=Padding.Empty};
        var heading=new Label {Text="Image effects",Width=210,Height=36,ForeColor=text,Font=new Font("Segoe UI",16,FontStyle.Bold,GraphicsUnit.Pixel)};effectsPane.Controls.Add(heading);effectsPane.Controls.Add(ActionButton("Back to tool",null,"Return to annotation options",CloseImageEffects,210));
        effectsPane.Controls.Add(new Label {Text="Logo previews · Changes can be undone",Width=210,Height=38,ForeColor=muted,Margin=new Padding(0,6,0,4)});
        foreach(ToolStripItem item in menu.Items.Cast<ToolStripItem>().OrderBy(item=>item.Text.StartsWith("Rotate")||item.Text.StartsWith("Flip")||item.Text=="Grayscale"||item.Text.StartsWith("Add presentation")?0:1)){if(item is ToolStripSeparator||item.Text=="Keyboard shortcuts"||item.Text.StartsWith("New blank canvas"))continue;var action=item;var tile=new EffectTile {Caption=item.Text=="Rotate 90° clockwise"?"Rotate 90° right":item.Text=="Rotate 90° counterclockwise"?"Rotate 90° left":item.Text,Text="",Preview=MakeEffectPreview(item.Text),Width=210,Height=76,Margin=new Padding(0,0,0,6),BackColor=Color.FromArgb(43,46,54),ForeColor=text,Font=Font,AccessibleName=item.Text};tile.Click+=delegate{Guard(delegate{action.PerformClick();});};effectsPane.Controls.Add(tile);}
        var flow=effectsPane;flow.ClientSizeChanged+=delegate{int width=Math.Max(160,flow.ClientSize.Width-8);foreach(Control child in flow.Controls)child.Width=width;};host.Controls.Add(flow);flow.BringToFront();int fitWidth=Math.Max(160,flow.ClientSize.Width-8);foreach(Control child in flow.Controls)child.Width=fitWidth;InstallEffectsResizeGrip();
    }
    int EffectsMaximumWidth(){return Math.Max(effectsDefaultWidth,Math.Min(620,ClientSize.Width-toolRail.Width-380));}
    void BeginEffectsResize(int screenX){if(effectsPane==null||effectsResizeGrip==null)return;effectsResizing=true;effectsResizeStart=screenX;effectsResizeWidth=inspector.Parent.Width;effectsResizeGrip.Capture=true;}
    void MoveEffectsResize(int screenX){if(!effectsResizing||effectsPane==null)return;inspector.Parent.Width=Math.Max(effectsDefaultWidth,Math.Min(EffectsMaximumWidth(),effectsResizeWidth+effectsResizeStart-screenX));}
    void EndEffectsResize(){effectsResizing=false;if(effectsResizeGrip!=null)effectsResizeGrip.Capture=false;}
    void InstallEffectsResizeGrip(){
        var host=inspector.Parent;effectsDefaultWidth=host.Width;host.Width=Math.Max(effectsDefaultWidth,Math.Min(EffectsMaximumWidth(),effectsSavedWidth));
        effectsResizeGrip=new Panel {Dock=DockStyle.Right,Width=7,BackColor=Color.FromArgb(24,26,32),Cursor=Cursors.VSplit,AccessibleName="Drag to resize Image Effects pane"};
        effectsResizeGrip.Paint+=delegate(object sender,PaintEventArgs e){using(var pen=new Pen(Color.FromArgb(88,99,119)))e.Graphics.DrawLine(pen,3,Math.Max(0,effectsResizeGrip.Height/2-16),3,effectsResizeGrip.Height/2+16);};
        effectsResizeGrip.MouseDown+=delegate(object sender,MouseEventArgs e){if(e.Button==MouseButtons.Left)BeginEffectsResize(effectsResizeGrip.PointToScreen(e.Location).X);};
        effectsResizeGrip.MouseMove+=delegate(object sender,MouseEventArgs e){MoveEffectsResize(effectsResizeGrip.PointToScreen(e.Location).X);};
        effectsResizeGrip.MouseUp+=delegate(object sender,MouseEventArgs e){if(e.Button==MouseButtons.Left)EndEffectsResize();};effectsResizeGrip.MouseCaptureChanged+=delegate{if(effectsResizeGrip!=null&&!effectsResizeGrip.Capture)effectsResizing=false;};
        Controls.Add(effectsResizeGrip);Controls.SetChildIndex(effectsResizeGrip,Controls.GetChildIndex(host));tips.SetToolTip(effectsResizeGrip,"Drag to resize Image Effects");
        Resize-=ConstrainEffectsPane;Resize+=ConstrainEffectsPane;
    }
    void ConstrainEffectsPane(object sender,EventArgs e){if(effectsPane!=null)inspector.Parent.Width=Math.Max(effectsDefaultWidth,Math.Min(EffectsMaximumWidth(),inspector.Parent.Width));}
    Bitmap MakeEffectPreview(string effect){
        using(var source=new Bitmap(brandLogo.Image)){
            Bitmap result=new Bitmap(94,58);using(var g=Graphics.FromImage(result)){g.Clear(Color.FromArgb(224,227,234));float ratio=Math.Min(48f/source.Width,44f/source.Height);int w=Math.Max(1,(int)(source.Width*ratio)),h=Math.Max(1,(int)(source.Height*ratio));g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(source,new Rectangle((94-w)/2,(58-h)/2,w,h));}
            if(effect.StartsWith("Rotate 90"))result.RotateFlip(effect.Contains("counterclockwise")?RotateFlipType.Rotate270FlipNone:RotateFlipType.Rotate90FlipNone);
            else if(effect=="Flip horizontal")result.RotateFlip(RotateFlipType.RotateNoneFlipX);else if(effect=="Flip vertical")result.RotateFlip(RotateFlipType.RotateNoneFlipY);
            else if(effect=="Grayscale"){using(var colored=(Bitmap)result.Clone())using(var g=Graphics.FromImage(result))using(var attributes=new ImageAttributes()){attributes.SetColorMatrix(new ColorMatrix(new float[][] {new float[]{.299f,.299f,.299f,0,0},new float[]{.587f,.587f,.587f,0,0},new float[]{.114f,.114f,.114f,0,0},new float[]{0,0,0,1,0},new float[]{0,0,0,0,1}}));g.DrawImage(colored,new Rectangle(0,0,94,58),0,0,colored.Width,colored.Height,GraphicsUnit.Pixel,attributes);}}
            else if(effect.StartsWith("Add presentation")){using(var g=Graphics.FromImage(result))using(var gradient=new LinearGradientBrush(new Rectangle(0,0,94,58),Color.FromArgb(65,87,150),Color.FromArgb(148,113,186),45f)){g.FillRectangle(gradient,0,0,94,58);g.FillRectangle(Brushes.White,12,8,70,42);g.DrawImage(source,new Rectangle(28,11,38,36));}}
            else if(effect.StartsWith("Place ")||effect.StartsWith("Append ")){using(var g=Graphics.FromImage(result)){g.Clear(Color.FromArgb(224,227,234));bool vertical=effect.StartsWith("Append ");g.DrawImage(source,vertical?new Rectangle(34,2,26,25):new Rectangle(10,10,34,36));g.DrawImage(source,vertical?new Rectangle(34,30,26,25):new Rectangle(50,10,34,36));}}
            else if(effect.StartsWith("Expand canvas")){using(var g=Graphics.FromImage(result))using(var pen=new Pen(Color.Gray)){pen.DashStyle=DashStyle.Dash;g.DrawRectangle(pen,3,3,87,51);}}
            else if(effect.StartsWith("New blank")){using(var g=Graphics.FromImage(result)){g.Clear(Color.FromArgb(224,227,234));g.FillRectangle(Brushes.White,18,5,58,48);}}
            else if(effect.StartsWith("Resize")){using(var smaller=new Bitmap(result)){using(var g=Graphics.FromImage(result)){g.Clear(Color.FromArgb(224,227,234));g.DrawImage(smaller,new Rectangle(16,10,62,38));}}}
            if(result.Width!=94||result.Height!=58){var fitted=new Bitmap(94,58);using(var g=Graphics.FromImage(fitted)){g.Clear(Color.FromArgb(224,227,234));float factor=Math.Min(94f/result.Width,58f/result.Height);int w=(int)(result.Width*factor),h=(int)(result.Height*factor);g.DrawImage(result,new Rectangle((94-w)/2,(58-h)/2,w,h));}result.Dispose();result=fitted;}return result;
        }
    }
    public void VerifyCaptureAndEffects(List<string> report,string folder){
        Action<bool,string> check=delegate(bool ok,string label){if(!ok)throw new Exception(label);report.Add("PASS "+label);};
        bool oldOpen=Preferences.Current.OpenEditorAfterCapture;Preferences.Current.OpenEditorAfterCapture=false;Hide();FinishCaptureVisibility("Captured",true);check(!Visible,"Successful capture remains hidden when auto-open is off");Preferences.Current.OpenEditorAfterCapture=true;FinishCaptureVisibility("Captured",false);check(Visible,"Successful capture opens editor when setting is enabled");Hide();FinishCaptureVisibility(null,false);check(!Visible,"Cancelled tray capture keeps editor hidden");FinishCaptureVisibility(null,true);check(Visible,"Cancelled editor capture restores previously visible editor");Preferences.Current.OpenEditorAfterCapture=oldOpen;
        check(!new Preferences().OpenEditorAfterCapture,"Capture defaults to preview without opening the editor");var prefs=Preferences.Current.Clone();prefs.OpenEditorAfterCapture=true;prefs.Save(System.IO.Path.Combine(folder,"preview-settings.xml"));check(Preferences.Read(System.IO.Path.Combine(folder,"preview-settings.xml")).OpenEditorAfterCapture,"Automatic editor preference persists");
        int singles=0,doubles=0;using(var router=new TrayClickRouter(delegate{singles++;},delegate{doubles++;})){router.Click();check(router.Pending&&singles==0,"Tray single click waits for double-click detection");router.Flush();check(singles==1,"Tray single click triggers one region capture");router.Click();router.DoubleClick();router.Click();router.Flush();check(singles==1&&doubles==1&&!router.Pending,"Tray double click opens editor without triggering region capture");}
        bool oldCopy=Preferences.Current.AutoCopyCapture;Preferences.Current.AutoCopyCapture=false;Hide();CompleteCapture(Blank(160,90));check(!Visible&&capturePreview!=null,"Completed capture creates a floating preview without opening editor");var first=documents[currentDocument];CompleteCapture(Blank(180,100));check(documents.Contains(first),"New preview retains earlier capture tabs");var latest=documents[currentDocument];ActivateDocument(documents.IndexOf(first));capturePreview.OpenCapture();check(Visible&&documents[currentDocument]==latest,"Preview click opens its own capture document");Preferences.Current.AutoCopyCapture=oldCopy;CloseCapturePreview();
        ImageMenu();check(effectsPane!=null&&effectsPane.Visible&&!inspector.Visible&&!layers.Visible,"Effects replace annotation controls in sidebar");var tiles=effectsPane.Controls.OfType<EffectTile>().ToList();check(!tiles.Any(tile=>tile.AccessibleName=="Keyboard shortcuts"||tile.AccessibleName.StartsWith("New blank canvas")),"Effects pane omits shortcuts and new blank canvas actions");int normalWidth=effectsDefaultWidth;BeginEffectsResize(500);MoveEffectsResize(400);EndEffectsResize();check(inspector.Parent.Width==Math.Min(normalWidth+100,EffectsMaximumWidth())&&effectsResizeGrip.Visible,"Effects pane widens by dragging its left divider");BeginEffectsResize(500);MoveEffectsResize(-5000);EndEffectsResize();check(inspector.Parent.Width==EffectsMaximumWidth(),"Effects resize leaves room for the canvas");BeginEffectsResize(500);MoveEffectsResize(5000);EndEffectsResize();check(inspector.Parent.Width==normalWidth,"Effects pane has a readable minimum width");BeginEffectsResize(500);MoveEffectsResize(400);EndEffectsResize();
        check(tiles.Count>=10&&tiles.All(t=>t.Preview!=null&&t.Preview.Size==new Size(94,58)),"All image actions have logo preview tiles");using(var gray=MakeEffectPreview("Grayscale")){bool neutral=true;for(int y=0;y<gray.Height;y++)for(int x=0;x<gray.Width;x++){var color=gray.GetPixel(x,y);if(Math.Abs(color.R-color.G)>1||Math.Abs(color.G-color.B)>1)neutral=false;}check(neutral,"Grayscale thumbnail shows grayscale logo pixels");}
        var grayscale=effectActions.Items.Cast<ToolStripItem>().First(item=>item.Text=="Grayscale");int history=undo.Count;grayscale.PerformClick();check(effectsPane==null&&Active!=null&&Active.Kind=="Grayscale","Grayscale opens editable adjustment controls");check(undo.Count==history+1,"Sidebar effects apply through existing undoable image actions");Undo();ImageMenu();
        using(var screenshot=new Bitmap(Width,Height)){DrawToBitmap(screenshot,new Rectangle(Point.Empty,screenshot.Size));screenshot.Save(System.IO.Path.Combine(folder,"effects-sidebar.png"));}SetTool("Rectangle");check(effectsPane==null&&inspector.Visible,"Selecting an annotation tool restores its controls");check(effectsResizeGrip==null&&inspector.Parent.Width==normalWidth,"Annotation tools restore fixed sidebar width and remove resize divider");BeginEffectsResize(500);MoveEffectsResize(300);EndEffectsResize();check(inspector.Parent.Width==normalWidth,"Sidebar cannot be resized outside Image Effects");ImageMenu();check(inspector.Parent.Width==Math.Min(normalWidth+100,EffectsMaximumWidth()),"Image Effects remembers its width during the session");CloseImageEffects();
        using(var sample=Blank(300,180))using(var toast=new CaptureToast(sample,new Rectangle(0,0,1000,800),delegate{})){check(toast.Right==984&&toast.Bottom==784,"Floating capture preview sits inside monitor bottom right");using(var screenshot=new Bitmap(toast.Width,toast.Height)){toast.DrawToBitmap(screenshot,new Rectangle(Point.Empty,screenshot.Size));screenshot.Save(System.IO.Path.Combine(folder,"capture-preview.png"));}}
        Hide();LoadBitmap(Blank(100,100),"Test");
    }
}
}
