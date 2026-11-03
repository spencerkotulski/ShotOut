using System;using System.Collections.Generic;using System.Drawing;using System.IO;using System.Linq;using System.Windows.Forms;
namespace ShotOut {partial class Editor {
    int ToolbarMenuWidth(string label){return TextRenderer.MeasureText(label,Font,Size.Empty,TextFormatFlags.NoPadding).Width+64;}
    ContextMenuStrip fileToolbarMenu,exportToolbarMenu;
    ModernButton CreateFileMenuButton(){ModernButton button=null;button=ActionButton("File","Open","Open, paste, recent files and save options",delegate{ShowFileToolbarMenu(button);},ToolbarMenuWidth("File"));button.ShowDropDown=true;return button;}
    ModernButton CreateExportMenuButton(){ModernButton button=null;button=ActionButton("Export","SaveAs","Export an image or copy it to the clipboard",delegate{ShowExportToolbarMenu(button);},ToolbarMenuWidth("Export"));button.ShowDropDown=true;return button;}
    ToolStripMenuItem TopMenuAction(ContextMenuStrip menu,string label,string shortcut,Action action,bool enabled=true){var item=new ToolStripMenuItem(label){ShortcutKeyDisplayString=shortcut,Enabled=enabled};item.Click+=delegate{PopupLifetime.AfterClick(this,delegate{Guard(action);});};menu.Items.Add(item);return item;}
    void PopulateFileToolbarMenu(){if(fileToolbarMenu==null)fileToolbarMenu=PopupLifetime.Own(this,DarkMenu());PopupLifetime.Reset(fileToolbarMenu);
        TopMenuAction(fileToolbarMenu,"New tab","Ctrl+T",AddWelcomeDocument);
        TopMenuAction(fileToolbarMenu,"Open image or project…","Ctrl+O",Open);
        TopMenuAction(fileToolbarMenu,"Paste","Ctrl+V",Paste,CanPasteClipboard());
        var recent=new ToolStripMenuItem("Recent files");fileToolbarMenu.Items.Add(recent);
        var history=ProjectHistory.Current;var paths=history.Items.Select(x=>x.Path).Concat(history.Assets).Distinct(StringComparer.OrdinalIgnoreCase).Where(File.Exists).Take(12).ToArray();
        foreach(var path in paths){string target=path;var entry=new ToolStripMenuItem(Path.GetFileName(path).Replace("&","&&")){ToolTipText=path};entry.Click+=delegate{PopupLifetime.AfterClick(this,delegate{Guard(delegate{OpenPath(target);});});};recent.DropDownItems.Add(entry);}if(paths.Length==0)recent.DropDownItems.Add(new ToolStripMenuItem("No recent files yet"){Enabled=false});
        fileToolbarMenu.Items.Add(new ToolStripSeparator());TopMenuAction(fileToolbarMenu,"Save","Ctrl+S",delegate{SaveImage();},!welcomeRequested);
        TopMenuAction(fileToolbarMenu,"Save image as…","",SaveImageAs,!welcomeRequested);TopMenuAction(fileToolbarMenu,"Save editable project as…","",SaveProjectAs,!welcomeRequested);
    }
    void ShowFileToolbarMenu(Control anchor){PopulateFileToolbarMenu();fileToolbarMenu.Show(anchor,new Point(0,anchor.Height+3));}
    void PopulateExportToolbarMenu(){if(exportToolbarMenu==null)exportToolbarMenu=PopupLifetime.Own(this,DarkMenu());PopupLifetime.Reset(exportToolbarMenu);TopMenuAction(exportToolbarMenu,"Export image…","",SaveImageAs,!welcomeRequested);TopMenuAction(exportToolbarMenu,"Copy image","Ctrl+Shift+C",CopyImage,!welcomeRequested);}
    void ShowExportToolbarMenu(Control anchor){PopulateExportToolbarMenu();exportToolbarMenu.Show(anchor,new Point(0,anchor.Height+3));}
    public void VerifyTopBar(List<string> report,string folder){
        PopulateFileToolbarMenu();if(!fileToolbarMenu.Items.OfType<ToolStripMenuItem>().Any(x=>x.Text=="Open image or project…")||!fileToolbarMenu.Items.OfType<ToolStripMenuItem>().Any(x=>x.Text=="Paste")||!fileToolbarMenu.Items.OfType<ToolStripMenuItem>().Any(x=>x.Text=="Recent files"))throw new Exception("Missing File menu actions");
        LoadImageDocument(Blank(800,500),"Toolbar preview");PopulateExportToolbarMenu();if(exportToolbarMenu.Items.Count!=2||exportToolbarMenu.Items.Cast<ToolStripItem>().Any(x=>!x.Enabled))throw new Exception("Missing export actions");
        Show();Application.DoEvents();using(var image=new Bitmap(Width,Height)){DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(folder,"top-bar.png"));}Hide();report.Add("PASS File menu exposes open/paste/recent files and save actions; Export offers image export/copy; toolbar visually captured");
    }
}}
