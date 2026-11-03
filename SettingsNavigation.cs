using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
namespace ShotOut {
partial class SettingsDialog {
    readonly Dictionary<string,FlowLayoutPanel> settingsPages=new Dictionary<string,FlowLayoutPanel>();
    readonly Dictionary<string,ModernButton> settingsNavigation=new Dictionary<string,ModernButton>();
    Panel settingsContent;FlowLayoutPanel settingsSidebar;
    Control BuildSettingsPages(bool? startup) {
        var host=new Panel {Dock=DockStyle.Fill};settingsContent=new Panel {Dock=DockStyle.Fill};
        settingsSidebar=new FlowLayoutPanel {Dock=DockStyle.Left,Width=192,Padding=new Padding(12,18,12,12),FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,BackColor=Color.FromArgb(25,27,33)};
        host.Controls.Add(settingsContent);host.Controls.Add(settingsSidebar);
        var page=SettingsPage("Capture","Choose what happens when you take a screenshot.");
        AddTitle(page,"Region selection");
        Toggle(page,"smartregion","Snap to visible windows",Value.SmartRegion);
        Toggle(page,"smartelements","Detect controls and messages (experimental)",Value.SmartElements);
        SettingsHelp(page,"Smart selection replaces region capture when enabled. Hover to detect; scroll to expand or shrink. Your chosen scope stays selected while the pointer remains inside it. Shift selects the window; Alt or dragging selects freely.",90);

        AddTitle(page,"Capture behavior");


        Toggle(page,"cursor","Include the mouse cursor",Value.IncludeCursor);
        delay=NumberRow(page,"Capture delay · seconds",0,15,Value.CaptureDelay);

        page=SettingsPage("After capture","Choose tasks to run after every successful screenshot.");
        AddCapturePresets(page);
        AddTitle(page,"After-capture tasks");
        Toggle(page,"afterSave","Save",Value.AfterCaptureSave&&!Value.AfterCaptureSaveAs);
        Toggle(page,"afterSaveAs","Save as…",Value.AfterCaptureSaveAs);
        Toggle(page,"afterFolder","Open containing folder",Value.AfterCaptureOpenFolder);
        Toggle(page,"afterViewer","Open in default photo viewer",Value.AfterCaptureOpenViewer);
        Toggle(page,"autocopy","Copy to clipboard",Value.AutoCopyCapture);
        Toggle(page,"openaftercapture","Open in ShotOut editor",Value.OpenEditorAfterCapture);
        Toggle(page,"copyPath","Copy file path",Value.AfterCaptureCopyPath);
        Toggle(page,"thumbnail","Show capture preview",Value.ShowCaptureThumbnail);
        Toggle(page,"captureSound","Play sound",Value.CaptureSoundEnabled);
        checks["afterSave"].CheckedChanged+=delegate {if(checks["afterSave"].Checked)checks["afterSaveAs"].Checked=false;};
        checks["afterSaveAs"].CheckedChanged+=delegate {if(checks["afterSaveAs"].Checked)checks["afterSave"].Checked=false;};
        SettingsHelp(page,"Save uses Save options below. Save as asks for a name and location instead. Opening the folder, viewer, or copying the file path also saves the capture if needed. Cancelling Save as skips file tasks; clipboard copying still runs.",100);
        AddAfterCaptureSaveOptions(page);
        AddCaptureSoundSettings(page);
        page=SettingsPage("Tabs","Manage tab overflow and optional automatic closing.");AddTabOptions(page);
        page=SettingsPage("Display","Adjust toolbar size for comfortable viewing.");
        AddDisplayOptions(page);
        page=SettingsPage("Editor","Adjust the workspace and editing behavior.");
        AddTitle(page,"Workspace");Toggle(page,"adaptiveToolbar","Use two toolbar columns in smaller windows",Value.AdaptiveToolbar);
        Toggle(page,"layers","Show the Layers panel",Value.ShowLayers);Toggle(page,"window","Remember window position and size",Value.RememberWindow);Toggle(page,"fit","Fit images to the canvas when opened",Value.ZoomToFit);
        Toggle(page,"toolHints","Show first-use tool hints",Value.ShowToolHints);var resetHints=Button("Reset tool hints",180);resetHints.Click+=delegate{Value.SeenToolHints.Clear();resetHints.Text="Hints reset";};page.Controls.Add(resetHints);
        SettingsHelp(page,"New text scales with image size and starts 25% larger. A font size you choose manually is kept as your override.",52);
        AddTitle(page,"Editing");undo=NumberRow(page,"Undo history · states",1,100,Value.UndoLimit);Toggle(page,"confirm","Ask to save unsaved changes",Value.ConfirmUnsaved);
        page=SettingsPage("Files & folders","Choose where screenshots and editable projects are saved.");
        AddTitle(page,"Output quality");exportQualityField=QualityRow(page,"Editor JPEG quality",Value.ExportJpegQuality);SettingsHelp(page,"Applies to JPEG files saved or exported from the editor. PNG stays lossless; projects and clipboard images keep full quality. Quality affects file size and compression, not image dimensions. Screenshot capture speed is unchanged; save time can vary.",100);
        SettingsHelp(page,"Screenshot destination, naming, format, and monthly folders are configured under After capture > Save options.",60);AddProjectFolderSetting(page);
        SettingsHelp(page,"Screenshots are exported images. Projects preserve your editable annotations and layers.",52);
        page=SettingsPage("Startup & recovery","Control startup and recovery of unfinished work.");
        AddTitle(page,"Startup");Toggle(page,"startup","Start ShotOut when I sign in to Windows",startup??LoginStartup.Enabled);Toggle(page,"multiple","Allow multiple app instances",Value.AllowMultiple);
        AddTitle(page,"Recovery");Toggle(page,"recovery","Automatically recover unsaved tabs",Value.AutoRecover);
        page=SettingsPage("Capture shortcuts","These shortcuts work while you are using other apps.");
        SettingsHelp(page,"Click a shortcut field, then press the new key combination. Backspace disables it. Capture shortcuts require Ctrl or Alt.",60);foreach(var binding in Value.Capture)ShortcutRow(page,binding);
        page=SettingsPage("Tool shortcuts","Single-key shortcuts for annotation tools inside ShotOut.");
        SettingsHelp(page,"Click a field and press a key. Backspace disables it. Standard Ctrl+C, Ctrl+V, Ctrl+S and Ctrl+Z commands remain available.",60);foreach(var binding in Value.Tools)ShortcutRow(page,binding);
        page=SettingsPage("Updates","Keep ShotOut up to date through its official GitHub releases.");AddUpdateOptions(page);
        page=SettingsPage("Experimental","Optional features still being refined. Print Screen may conflict with other apps.");AddPrintScreenOptions(page);
        ShowSettingsPage("Capture");return host;
    }
    FlowLayoutPanel SettingsPage(string title,string description) {
        var page=new FlowLayoutPanel {Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new Padding(22,12,22,20),Visible=false,AccessibleName=title+" settings"};
        page.Controls.Add(new Label {Text=title,UseMnemonic=false,Width=550,Height=42,Font=new Font(Font.FontFamily,21,FontStyle.Bold,GraphicsUnit.Pixel),ForeColor=ForeColor});
        SettingsHelp(page,description,48);settingsPages.Add(title,page);settingsContent.Controls.Add(page);
        var button=Button(title.Replace("&","&&"),164);button.Height=46;button.Margin=new Padding(0,0,0,6);button.AccessibleName=title+" settings";button.TabStop=true;button.Click+=delegate {ShowSettingsPage(title);};settingsNavigation.Add(title,button);settingsSidebar.Controls.Add(button);return page;
    }
    static void SettingsHelp(FlowLayoutPanel page,string text,int height){page.Controls.Add(new Label {Text=text,Width=550,Height=height,ForeColor=Color.FromArgb(159,169,186),Margin=new Padding(0,0,0,8)});}
    void ShowSettingsPage(string title){foreach(var pair in settingsPages){pair.Value.Visible=pair.Key==title;settingsNavigation[pair.Key].Selected=pair.Key==title;settingsNavigation[pair.Key].Invalidate();}settingsPages[title].BringToFront();}
}
}
