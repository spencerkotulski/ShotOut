using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
namespace ShotOut {
class ModernMenuRenderer : ToolStripProfessionalRenderer {
    public ModernMenuRenderer():base(new DarkColors()){RoundedEdges=false;}
    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e){e.Graphics.Clear(Color.FromArgb(38,41,48));}
    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e){}
    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e){if(!e.Item.Enabled||(!e.Item.Selected&&!e.Item.Pressed))return;var state=e.Graphics.Save();e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var path=Art.Round(new RectangleF(3,2,e.Item.Width-6,e.Item.Height-4),5))using(var brush=new SolidBrush(UiTheme.HoverSurface))e.Graphics.FillPath(brush,path);e.Graphics.Restore(state);}
    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e){var state=e.Graphics.Save();e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var path=Art.Round(new RectangleF(.5f,.5f,e.ToolStrip.Width-1.5f,e.ToolStrip.Height-1.5f),8))using(var pen=new Pen(Color.FromArgb(62,67,79)))e.Graphics.DrawPath(pen,path);e.Graphics.Restore(state);}
    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e){using(var pen=new Pen(Color.FromArgb(56,61,72)))e.Graphics.DrawLine(pen,10,e.Item.Height/2,e.Item.Width-10,e.Item.Height/2);}
    internal static string Icon(string text){string value=text.ToLowerInvariant();if(value.StartsWith("unlock image"))return "Unlocked";if(value.StartsWith("lock image"))return "Locked";if(value=="cut")return "Cut rows";if(value.StartsWith("hide")||value.StartsWith("show"))return "Visibility";if(value.StartsWith("link")||value.StartsWith("unlink"))return "Link";if(value.StartsWith("flatten"))return "Layers";if(value.StartsWith("edit"))return "Pen";if(value.StartsWith("align")||value.StartsWith("distribute"))return "Adjust";if(value=="arrange")return "Layers";if(value=="undo")return "Undo";if(value=="redo")return "Redo";if(value.StartsWith("delete")||value.StartsWith("remove"))return "Delete";if(value.StartsWith("copy")||value.StartsWith("duplicate"))return "Copy";if(value.StartsWith("paste"))return "Paste";if(value.StartsWith("rename"))return "Text";if(value.StartsWith("capture"))return "Capture";if(value.StartsWith("edit image"))return "Pen";if(value.StartsWith("edit project"))return "Project";if(value.StartsWith("open"))return "Open";if(value.StartsWith("save editable project"))return "Project";if(value.StartsWith("save image as")||value.StartsWith("export"))return "SaveAs";if(value.StartsWith("save"))return "Save";if(value.StartsWith("bring forward"))return "Up";if(value.StartsWith("send backward"))return "Down";if(value=="settings")return "Settings";return null;}
    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e){
        bool caption=e.Text==e.Item.Text;var option=e.Item as ToolStripMenuItem;
        var color=!e.Item.Enabled?Color.FromArgb(111,119,133):caption?Color.FromArgb(232,236,244):Color.FromArgb(160,171,189);
        if(caption&&e.Item.Image==null&&(option==null||!option.Checked)){string icon=Icon(e.Item.Text);if(icon!=null)Glyph.Draw(e.Graphics,icon,new Rectangle(Math.Max(6,e.TextRectangle.Left-28),(e.Item.Height-18)/2,18,18),e.Item.Enabled?Color.FromArgb(187,200,221):color);}
        var bounds=TextBounds(e.Item,e.TextRectangle);var flags=e.TextFormat;
        flags&=~(TextFormatFlags.Bottom|TextFormatFlags.WordBreak);flags|=TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.NoPadding|TextFormatFlags.PreserveGraphicsClipping|TextFormatFlags.PreserveGraphicsTranslateTransform;
        TextRenderer.DrawText(e.Graphics,e.Text,e.TextFont,bounds,color,flags);
    }
    internal static Rectangle TextBounds(ToolStripItem item,Rectangle native){return new Rectangle(native.X,0,native.Width,item.Height);}
    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e){e.ArrowColor=e.Item.Enabled?Color.FromArgb(164,177,198):Color.FromArgb(111,119,133);base.OnRenderArrow(e);}
    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e){using(var pen=new Pen(Color.FromArgb(133,176,255),1.6f)){var r=e.ImageRectangle;e.Graphics.DrawLines(pen,new[]{new Point(r.Left+3,r.Top+r.Height/2),new Point(r.Left+r.Width/2,r.Bottom-4),new Point(r.Right-2,r.Top+3)});}}
}
class ModernContextMenu : ContextMenuStrip { static readonly Font MenuFont=new Font("Segoe UI",14,FontStyle.Regular,GraphicsUnit.Pixel);
    static readonly ConditionalWeakTable<ToolStripDropDown,object> roundedChildren=new ConditionalWeakTable<ToolStripDropDown,object>();
    public ModernContextMenu(){Font=MenuFont;Renderer=new ModernMenuRenderer();BackColor=Color.FromArgb(38,41,48);ForeColor=Color.FromArgb(233,237,245);Padding=new Padding(5);ShowImageMargin=false;DropShadowEnabled=true;}
    protected override void OnSizeChanged(EventArgs e){base.OnSizeChanged(e);if(Width<2||Height<2)return;using(var path=Art.Round(new RectangleF(0,0,Width,Height),8)){var previous=Region;Region=new Region(path);if(previous!=null)previous.Dispose();}}
    protected override void OnOpening(CancelEventArgs e){
        StyleMenu(this);base.OnOpening(e);
    }
    static void StyleMenu(ToolStripDropDown drop){
        drop.Font=MenuFont;if(drop.AutoSize)drop.Padding=new Padding(6);var native=drop as ToolStripDropDownMenu;if(native!=null){bool icons=drop.Items.Cast<ToolStripItem>().Any(item=>item.Image!=null||ModernMenuRenderer.Icon(item.Text)!=null);bool checks=drop.Items.OfType<ToolStripMenuItem>().Any(item=>item.Checked);native.ShowImageMargin=icons;native.ShowCheckMargin=!icons&&(checks||native.ShowCheckMargin);}
        foreach(ToolStripItem item in drop.Items){var option=item as ToolStripMenuItem;if(option==null)continue;if(option.AutoSize){option.Padding=new Padding(6,7,10,7);option.Margin=Padding.Empty;}
            var shortcut=Regex.Match(option.Text,@"\s+\(((?:Ctrl\+|Alt\+|Shift\+)*(?:F\d{1,2}|[A-Z0-9]))\)$");if(shortcut.Success){option.ShortcutKeyDisplayString=shortcut.Groups[1].Value;option.Text=option.Text.Substring(0,shortcut.Index);}
            if(option.Text=="Undo")option.ShortcutKeyDisplayString="Ctrl+Z";else if(option.Text=="Redo")option.ShortcutKeyDisplayString="Ctrl+Y";else if(option.Text=="Copy image")option.ShortcutKeyDisplayString="Ctrl+Shift+C";else if(option.Text=="Copy selected annotations")option.ShortcutKeyDisplayString="Ctrl+C";else if(option.Text=="Duplicate selected layers")option.ShortcutKeyDisplayString="Ctrl+D";else if(option.Text=="Delete selected layers")option.ShortcutKeyDisplayString="Delete";StyleChildren(option);
        }
    }
    static void StyleChildren(ToolStripMenuItem item){if(!item.HasDropDownItems)return;var drop=item.DropDown;drop.Renderer=new ModernMenuRenderer();drop.BackColor=Color.FromArgb(38,41,48);drop.ForeColor=Color.FromArgb(233,237,245);roundedChildren.GetValue(drop,delegate(ToolStripDropDown target){target.SizeChanged+=delegate{RoundChild(target);};return new object();});StyleMenu(drop);RoundChild(drop);}
    static void RoundChild(ToolStripDropDown drop){if(drop.Width<2||drop.Height<2)return;using(var path=Art.Round(new RectangleF(0,0,drop.Width,drop.Height),8)){var previous=drop.Region;drop.Region=new Region(path);if(previous!=null)previous.Dispose();}}
}
static class ModernMenus {
    internal static void TextMenu(TextBoxBase input){
        if(input.ContextMenuStrip!=null)return;
        var menu=PopupLifetime.Own(input,new ModernContextMenu());var undo=menu.Items.Add("Undo",null,delegate{input.Undo();});var cut=menu.Items.Add("Cut",null,delegate{input.Cut();});var copy=menu.Items.Add("Copy",null,delegate{input.Copy();});var paste=menu.Items.Add("Paste",null,delegate{input.Paste();});menu.Items.Add(new ToolStripSeparator());menu.Items.Add("Select all",null,delegate{input.SelectAll();});
        ((ToolStripMenuItem)undo).ShortcutKeyDisplayString="Ctrl+Z";((ToolStripMenuItem)cut).ShortcutKeyDisplayString="Ctrl+X";((ToolStripMenuItem)copy).ShortcutKeyDisplayString="Ctrl+C";((ToolStripMenuItem)paste).ShortcutKeyDisplayString="Ctrl+V";menu.Opening+=delegate{undo.Enabled=input.CanUndo&&!input.ReadOnly;copy.Enabled=input.SelectionLength>0;cut.Enabled=copy.Enabled&&!input.ReadOnly;paste.Enabled=!input.ReadOnly;};input.ContextMenuStrip=menu;
    }
}
class ThemedTextBox : TextBox {public ThemedTextBox(){ModernMenus.TextMenu(this);}}
class ThemedRichTextBox : RichTextBox {public ThemedRichTextBox(){ModernMenus.TextMenu(this);}}
}
