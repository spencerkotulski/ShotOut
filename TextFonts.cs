using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace ShotOut {
class FontPicker : ModernButton {
    public readonly System.Collections.Generic.List<string> Items=new System.Collections.Generic.List<string>();
    string selected;ContextMenuStrip menu;
    public event EventHandler SelectedIndexChanged;
    public string SelectedItem {get{return selected;}set{if(selected==value)return;selected=value;Text=value+"   ▾";if(SelectedIndexChanged!=null)SelectedIndexChanged(this,EventArgs.Empty);Invalidate();}}
    protected override void OnClick(EventArgs e){base.OnClick(e);if(menu==null)menu=PopupLifetime.Own(this,new ModernContextMenu {BackColor=Color.FromArgb(38,41,48),ForeColor=ForeColor,Font=Font,Renderer=new ModernMenuRenderer(),ShowImageMargin=false,ShowCheckMargin=true});PopupLifetime.Reset(menu);foreach(string name in Items){string choice=name;var item=new ToolStripMenuItem(name){Checked=name==SelectedItem};item.Click+=delegate{PopupLifetime.AfterClick(this,delegate{SelectedItem=choice;});};menu.Items.Add(item);}menu.MinimumSize=new Size(Width,0);menu.Show(this,new Point(0,Height+3));}
}
static class TextFonts {
    public static bool Available(string name){try{using(var family=new FontFamily(name))return family.Name==name;}catch(ArgumentException){return false;}}
    public static decimal Points(float pixels){return Math.Max(6,Math.Min(500,(decimal)pixels*.75m));}
    public static Font Create(string family,float pixels,FontStyle style){try{return new Font(String.IsNullOrWhiteSpace(family)?"Segoe UI":family,Math.Max(.1f,pixels),style,GraphicsUnit.Pixel);}catch(ArgumentException){return new Font(System.Drawing.FontFamily.GenericSansSerif,Math.Max(.1f,pixels),FontStyle.Regular,GraphicsUnit.Pixel);}}
}
partial class Editor {
    Control fontRow;
    FontPicker fontChoice;
    string SelectedFontName {get{return fontChoice==null||fontChoice.SelectedItem==null?"Segoe UI":fontChoice.SelectedItem.ToString();}}
    float FontSizePixels {get{return (float)(fontSize.Value*4m/3m);}}
    void AddFontInspector(){Disposed+=delegate{if(inlineDisplayFont!=null){inlineDisplayFont.Dispose();inlineDisplayFont=null;}};fontRow=Row();fontRow.Controls.Add(Caption("Font",50));fontChoice=new FontPicker {Width=167,Height=34,BackColor=Color.FromArgb(43,46,54),ForeColor=text,Font=Font,Margin=Padding.Empty,AccessibleName="Font family"};fontChoice.Items.AddRange(new[]{"Segoe UI","Arial","Calibri","Aptos","Verdana","Tahoma","Trebuchet MS","Georgia","Times New Roman","Consolas","Courier New"}.Where(name=>TextFonts.Available(name)));fontChoice.SelectedItem="Segoe UI";fontChoice.SelectedIndexChanged+=delegate{if(syncing)return;if(inlineTarget!=null){ApplyInlineProperties();return;}if(Active==null)return;Push();Active.FontFamily=SelectedFontName;GrowTextBox(Active,Active.Size);EnsureCanvasFitsMarks();RefreshImage();};fontRow.Controls.Add(fontChoice);inspector.Controls.Add(fontRow);}
    void SyncFontChoice(string family){if(fontChoice==null)return;if(String.IsNullOrWhiteSpace(family))family="Segoe UI";if(!fontChoice.Items.Contains(family))fontChoice.Items.Add(family);fontChoice.SelectedItem=family;}
    void StyleFormatButtons(){var controls=new[]{boldBox,italicBox,underlineBox};var styles=new[]{FontStyle.Bold,FontStyle.Italic,FontStyle.Underline};var names=new[]{"Bold (Ctrl+B)","Italic (Ctrl+I)","Underline (Ctrl+U)"};for(int i=0;i<controls.Length;i++){var control=controls[i];var owned=new Font(Font,styles[i]);control.Font=owned;control.AccessibleName=names[i];tips.SetToolTip(control,names[i]);control.Disposed+=delegate{owned.Dispose();};}}
}
}
