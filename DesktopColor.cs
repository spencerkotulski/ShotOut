using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace ShotOut {
class ScreenColorPicker : Form {
    readonly Bitmap desktop;
    readonly Rectangle desktopBounds;
    Point sample;
    public Color Chosen {get;private set;}
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
    [DllImport("dwmapi.dll")] static extern int DwmFlush();
    public ScreenColorPicker(Bitmap image,Rectangle bounds){
        desktop=image;desktopBounds=bounds;AutoScaleMode=AutoScaleMode.None;FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.Manual;Bounds=bounds;TopMost=true;ShowInTaskbar=false;DoubleBuffered=true;KeyPreview=true;Cursor=Cursors.Cross;AccessibleName="Screen color eyedropper";SampleAt(new Point(Math.Max(0,Math.Min(image.Width-1,System.Windows.Forms.Cursor.Position.X-bounds.X)),Math.Max(0,Math.Min(image.Height-1,System.Windows.Forms.Cursor.Position.Y-bounds.Y))));
    }
    void PinBounds(){if(IsHandleCreated)SetWindowPos(Handle,IntPtr.Zero,desktopBounds.X,desktopBounds.Y,desktopBounds.Width,desktopBounds.Height,0x14);}
    protected override void OnShown(EventArgs e){PinBounds();base.OnShown(e);Activate();}
    protected override void WndProc(ref Message message){if(message.Msg==0x2E0){PinBounds();message.Result=IntPtr.Zero;return;}base.WndProc(ref message);}
    internal void SampleAt(Point point){sample=new Point(Math.Max(0,Math.Min(desktop.Width-1,point.X)),Math.Max(0,Math.Min(desktop.Height-1,point.Y)));Chosen=desktop.GetPixel(sample.X,sample.Y);Invalidate();}
    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);SampleAt(e.Location);}
    internal void CompleteSampling(bool accept){DialogResult=accept?DialogResult.OK:DialogResult.Cancel;Close();}
    protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);if(e.Button==MouseButtons.Right)CompleteSampling(false);else if(e.Button==MouseButtons.Left){SampleAt(e.Location);CompleteSampling(true);}}
    protected override bool ProcessCmdKey(ref Message message,Keys keys){if(keys==Keys.Escape){CompleteSampling(false);return true;}if(keys==Keys.Enter){CompleteSampling(true);return true;}return base.ProcessCmdKey(ref message,keys);}
    protected override void OnPaint(PaintEventArgs e){
        e.Graphics.DrawImageUnscaled(desktop,0,0);var available=ClientRectangle;if(!Preferences.Testing){var monitor=Screen.FromPoint(new Point(sample.X+desktopBounds.X,sample.Y+desktopBounds.Y)).Bounds;monitor.Offset(-desktopBounds.X,-desktopBounds.Y);var visible=Rectangle.Intersect(ClientRectangle,monitor);if(visible.Width>=190&&visible.Height>=165)available=visible;}int x=sample.X+24,y=sample.Y+24;if(x+190>available.Right)x=sample.X-214;if(y+165>available.Bottom)y=sample.Y-189;x=Math.Max(available.Left,Math.Min(available.Right-190,x));y=Math.Max(available.Top,Math.Min(available.Bottom-165,y));
        using(var background=new SolidBrush(Color.FromArgb(30,32,38)))e.Graphics.FillRectangle(background,x,y,190,165);var state=e.Graphics.Save();e.Graphics.SetClip(new Rectangle(x+10,y+6,170,110));e.Graphics.InterpolationMode=InterpolationMode.NearestNeighbor;e.Graphics.PixelOffsetMode=PixelOffsetMode.Half;e.Graphics.DrawImage(desktop,new Rectangle(x+6,y+6,178,105),new Rectangle(sample.X-8,sample.Y-5,17,11),GraphicsUnit.Pixel);e.Graphics.Restore(state);
        e.Graphics.DrawRectangle(Pens.Black,x+89,y+55,11,11);e.Graphics.DrawRectangle(Pens.White,x+90,y+56,9,9);using(var brush=new SolidBrush(Chosen))e.Graphics.FillRectangle(brush,x+9,y+120,22,22);
        TextRenderer.DrawText(e.Graphics,"#"+(Chosen.ToArgb()&0xffffff).ToString("X6"),Font,new Rectangle(x+39,y+116,145,25),Color.White,TextFormatFlags.VerticalCenter);TextRenderer.DrawText(e.Graphics,"Click to select · Esc to cancel",Font,new Rectangle(x+8,y+143,178,19),Color.Gainsboro,TextFormatFlags.EndEllipsis);
    }
    public static Color? Pick(Form picker){
        if(Preferences.Testing)throw new InvalidOperationException("Real desktop sampling is disabled during automated tests.");
        picker.Hide();try{Application.DoEvents();try{DwmFlush();}catch(DllNotFoundException){}catch(EntryPointNotFoundException){}
            var bounds=DesktopDpi.ScreenBounds();using(var image=new Bitmap(bounds.Width,bounds.Height)){using(var graphics=Graphics.FromImage(image))graphics.CopyFromScreen(bounds.Location,Point.Empty,bounds.Size,CopyPixelOperation.SourceCopy);using(var overlay=new ScreenColorPicker(image,bounds))return overlay.ShowDialog(picker)==DialogResult.OK?(Color?)overlay.Chosen:null;}
        }finally{if(!picker.IsDisposed){picker.Show();picker.Activate();}}
    }
}
partial class PaletteDialog {
    FlowLayoutPanel favorites;
    ToolTip colorTips;
    Spectrum spectrum;
    ContextMenuStrip favoritePopup;
    int favoritePopupColor;
    void SelectPaletteColor(Color color){Chosen=Color.FromArgb(255,color.R,color.G,color.B);spectrum.Hue=color.GetHue();spectrum.Value=Math.Max(color.R,Math.Max(color.G,color.B))/255d;spectrum.Saturation=spectrum.Value==0?0:1-Math.Min(color.R,Math.Min(color.G,color.B))/(spectrum.Value*255);spectrum.Invalidate();UpdateHex();}
    internal void AcceptScreenColor(Color? color){if(color.HasValue)SelectPaletteColor(color.Value);}
    bool ReadPaletteColor(){int rgb;string value=hex.Text.Trim().TrimStart('#');if(value.Length!=6||!int.TryParse(value,System.Globalization.NumberStyles.HexNumber,System.Globalization.CultureInfo.InvariantCulture,out rgb)){hex.Focus();hex.SelectAll();return false;}SelectPaletteColor(Color.FromArgb(255,(rgb>>16)&255,(rgb>>8)&255,rgb&255));return true;}
    ModernButton PaletteAction(string text,int x,int width,Action action){var button=new ModernButton {Text=text,Location=new Point(x,326),Size=new Size(width,36),BackColor=Color.FromArgb(43,46,54),ForeColor=ForeColor,Font=Font,AccessibleName=text};button.Click+=delegate{try{action();}catch(Exception ex){MessageBox.Show(this,ex.Message,"ShotOut colors");}};Controls.Add(button);return button;}
    void AddColorTools(){
        colorTips=new ToolTip();Disposed+=delegate{colorTips.Dispose();};
        hex.Width=150;swatch.Location=new Point(184,282);swatch.Size=new Size(108,29);
        var eyedropper=new ModernButton {IconName="Eyedropper",Text="",AccessibleName="Eyedropper",Location=new Point(302,281),Size=new Size(34,34),BackColor=Color.FromArgb(43,46,54),ForeColor=ForeColor,Font=Font,TabStop=true};
        eyedropper.Click+=delegate{RunColorAction(delegate{AcceptScreenColor(ScreenColorPicker.Pick(this));});};colorTips.SetToolTip(eyedropper,"Pick a color anywhere on your screens · Esc cancels");Controls.Add(eyedropper);
        PaletteAction("Save favorite",204,132,delegate{if(!ReadPaletteColor())return;var old=AnnotationLibrary.Current.FavoriteColors.ToArray();AnnotationLibrary.Current.AddFavorite(Chosen);try{AnnotationLibrary.Persist();}catch{AnnotationLibrary.Current.FavoriteColors=new System.Collections.Generic.List<int>(old);throw;}RefreshFavorites();});
        Controls.Add(new Label {Text="Favorite colors",Location=new Point(20,332),Size=new Size(170,26),ForeColor=ForeColor});favorites=new FlowLayoutPanel {Location=new Point(17,370),Size=new Size(324,92),AutoScroll=true,WrapContents=true};Controls.Add(favorites);
        Controls.Add(new Label {Text="Right-click a color to remove it.",Location=new Point(20,468),Size=new Size(316,20),ForeColor=Color.FromArgb(149,157,174)});
        favoritePopup=PopupLifetime.Own(this,new ModernContextMenu {BackColor=Color.FromArgb(38,41,48),ForeColor=ForeColor,Font=Font,Renderer=new ModernMenuRenderer(),ShowImageMargin=false});
        favoritePopup.Opening+=delegate(object sender,System.ComponentModel.CancelEventArgs e){var source=favoritePopup.SourceControl;if(source==null||!(source.Tag is int)){e.Cancel=true;return;}favoritePopupColor=(int)source.Tag;e.Cancel=!AnnotationLibrary.Current.FavoriteColors.Contains(favoritePopupColor);};
        favoritePopup.Items.Add("Remove favorite",null,delegate{int color=favoritePopupColor;PopupLifetime.AfterClick(this,delegate{RunColorAction(delegate{RemoveFavorite(color);});});});
        RefreshFavorites();
    }
    void RunColorAction(Action action){try{action();}catch(Exception ex){MessageBox.Show(this,ex.Message,"ShotOut colors");}}
    void RemoveFavorite(int color){var previous=AnnotationLibrary.Current.FavoriteColors.ToArray();AnnotationLibrary.Current.FavoriteColors.Remove(color);try{AnnotationLibrary.Persist();}catch{AnnotationLibrary.Current.FavoriteColors=new System.Collections.Generic.List<int>(previous);throw;}RefreshFavorites();}
    void RefreshFavorites(){
        var old=new System.Collections.Generic.List<Control>();foreach(Control control in favorites.Controls)old.Add(control);favorites.Controls.Clear();foreach(var control in old)control.Dispose();
        if(AnnotationLibrary.Current.FavoriteColors.Count==0){favorites.Controls.Add(new Label {Text="Save colors here to reuse in any capture.",Width=300,Height=32,ForeColor=Color.FromArgb(149,157,174)});return;}
        foreach(int argb in AnnotationLibrary.Current.FavoriteColors){var color=Color.FromArgb(argb);var button=new ModernButton {Swatch=true,BackColor=color,Size=new Size(30,28),Margin=new Padding(3),AccessibleName="Favorite #"+(argb&0xffffff).ToString("X6"),Tag=argb,ContextMenuStrip=favoritePopup};button.Click+=delegate{SelectPaletteColor(color);};colorTips.SetToolTip(button,button.AccessibleName+" · right-click to remove");favorites.Controls.Add(button);}
    }
}
}
