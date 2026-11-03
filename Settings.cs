using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml.Serialization;
using Microsoft.Win32;
namespace ShotOut {
public class ShortcutBinding { public string Action; public int Chord; }
public class Preferences {
    public string ProjectFolder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"ShotOut","Projects");
    public string ScreenshotFolder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),"Screenshots");
    public bool RecentTabOrder;
    public int CaptureJpegQuality=100,ExportJpegQuality=100;
    public bool AutoCloseTabs,CloseIdleSavedTabs=true,CloseIdleUnsavedTabs,ShowTabScrollbar=true;public int TabLimit=20,TabIdleMinutes=15;
    public bool AdaptiveToolbar=true;public bool MonthlyScreenshots=true;
    public bool ShowToolHints=true;public List<string> SeenToolHints=new List<string>();
    public bool UsePrintScreen;
    public bool CheckForUpdates=true;
    public bool AutoRecover=true; public bool SmartElements;
    public bool OpenEditorAfterCapture; public bool CaptureSoundEnabled; public int CaptureSound, CaptureSoundVolume=75;
    public bool ShowLayers=true, AutoCopyCapture=true, SmartRegion=true;
    public bool AfterCaptureSave, AfterCaptureSaveAs, AfterCaptureOpenFolder, AfterCaptureOpenViewer;
    public bool AfterCaptureCopyPath, ShowCaptureThumbnail=true;
    public int UiSize=75;public bool ShowToolLabels;
    public string ScreenshotPattern="Screenshot {date} {time}", ScreenshotFormat="PNG", CapturePreset="Custom";
    public List<CaptureTaskPreset> CapturePresets=CaptureTaskPreset.Defaults();
    public bool AllowMultiple, StartOnLogin, IncludeCursor, RememberWindow=true, ZoomToFit=true, ConfirmUnsaved=true;
    public int CaptureDelay, UndoLimit=24, WindowX,WindowY,WindowWidth,WindowHeight;
    public List<ShortcutBinding> Capture=new List<ShortcutBinding> { new ShortcutBinding {Action="Region",Chord=(int)(Keys.Control|Keys.Shift|Keys.D2)},new ShortcutBinding {Action="Screen",Chord=(int)(Keys.Control|Keys.Shift|Keys.D3)},new ShortcutBinding {Action="Window",Chord=(int)(Keys.Control|Keys.Shift|Keys.D4)} };
    public List<ShortcutBinding> Tools=new List<ShortcutBinding>();
    public bool TextOptionsExpanded,GeometryExpanded,StylingExpanded;
    public static Preferences Current=new Preferences();
    public static bool Testing;
    public static string FilePath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"ShotOut","settings.xml"); } }
    public Preferences() { string[] names={"Select","Arrow","Double arrow","Line","Rectangle","Ellipse","Pen","Text","Balloon","Step","Highlight","Blur","Pixelate","Crop","Magnify","Spotlight","Eraser","Emoji","Cursor","Redact","Cut rows","Cut columns","Image","Bucket","Hand","Clone stamp"};string letters="VADLREFTONHBPCM SWJKXUGIQYZ".Replace(" ","");for(int i=0;i<names.Length;i++)Tools.Add(new ShortcutBinding {Action=names[i],Chord=(int)(Keys)Enum.Parse(typeof(Keys),letters[i].ToString())}); }
    public Preferences Clone() { using(var s=new MemoryStream()) { var xml=new XmlSerializer(typeof(Preferences));xml.Serialize(s,this);s.Position=0;var p=(Preferences)xml.Deserialize(s);p.Normalize();return p; } }
    public Dictionary<Keys,string> ToolMap() { return Tools.Where(x=>x.Chord!=0).ToDictionary(x=>(Keys)x.Chord,x=>x.Action); }
    public static string KeyLabel(int chord) { return chord==0?"Disabled":new KeysConverter().ConvertToString((Keys)chord); }
    void Normalize() {if(SeenToolHints==null)SeenToolHints=new List<string>();SeenToolHints=SeenToolHints.Distinct().ToList(); if(CapturePresets==null)CapturePresets=CaptureTaskPreset.Defaults();CapturePresets=CapturePresets.Where(x=>x!=null).GroupBy(x=>x.Name).Select(g=>g.Last()).ToList();foreach(var preset in CaptureTaskPreset.Defaults())if(!CapturePresets.Any(x=>x.Name==preset.Name))CapturePresets.Add(preset);if(!CapturePresets.Any(x=>x.Name==CapturePreset))CapturePreset="Custom"; Capture=Capture.GroupBy(b=>b.Action).Select(g=>g.Last()).ToList();Tools=Tools.GroupBy(b=>b.Action).Select(g=>g.Last()).ToList();foreach(var binding in Capture.Where(binding=>(binding.Chord==(int)(Keys.Control|Keys.K)||binding.Chord==(int)(Keys.Control|Keys.Shift|Keys.T)))){int replacement=new Preferences().Capture.First(item=>item.Action==binding.Action).Chord;binding.Chord=Capture.Any(other=>other!=binding&&other.Chord==replacement)?0:replacement;}var bucket=Tools.FirstOrDefault(b=>b.Action=="Bucket");if(bucket!=null&&bucket.Chord==(int)Keys.Q&&Tools.Any(b=>b.Action!="Bucket"&&b.Chord==bucket.Chord))bucket.Chord=0;var hand=Tools.FirstOrDefault(b=>b.Action=="Hand");if(hand!=null&&hand.Chord==(int)Keys.Y&&Tools.Any(b=>b.Action!="Hand"&&b.Chord==hand.Chord))hand.Chord=0;var clone=Tools.FirstOrDefault(b=>b.Action=="Clone stamp");if(clone!=null&&clone.Chord==(int)Keys.Z&&Tools.Any(b=>b.Action!="Clone stamp"&&b.Chord==clone.Chord))clone.Chord=0;if(String.IsNullOrWhiteSpace(ProjectFolder))ProjectFolder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"ShotOut","Projects");if(String.IsNullOrWhiteSpace(ScreenshotFolder))ScreenshotFolder=new Preferences().ScreenshotFolder; }
    public void Validate() {
        if(CaptureJpegQuality<1||CaptureJpegQuality>100||ExportJpegQuality<1||ExportJpegQuality>100)throw new ArgumentException("JPEG quality must be 1–100%.");
        if(String.IsNullOrWhiteSpace(ProjectFolder)||!Path.IsPathRooted(ProjectFolder))throw new ArgumentException("Choose an absolute project folder path.");ProjectFolder=Path.GetFullPath(ProjectFolder);
        if(String.IsNullOrWhiteSpace(ScreenshotFolder)||!Path.IsPathRooted(ScreenshotFolder))throw new ArgumentException("Choose an absolute screenshot folder path.");ScreenshotFolder=Path.GetFullPath(ScreenshotFolder);
        if(Capture==null||Tools==null||Capture.Count!=3||Tools.Count!=new Preferences().Tools.Count)throw new ArgumentException("Missing shortcut settings.");
        var defaults=new Preferences();foreach(var b in defaults.Capture)if(Capture.Count(x=>x.Action==b.Action)!=1)throw new ArgumentException("Each capture action needs one binding.");
        foreach(var b in defaults.Tools)if(Tools.Count(x=>x.Action==b.Action)!=1)throw new ArgumentException("Each tool needs one binding.");
        foreach(var b in Capture) { var k=(Keys)b.Chord;var code=k&Keys.KeyCode;if(b.Chord!=0&&(code==Keys.None||code==Keys.ControlKey||code==Keys.ShiftKey||code==Keys.Menu||(k&(Keys.Control|Keys.Alt))==0))throw new ArgumentException("Capture shortcuts need Ctrl or Alt plus a key."); }
        foreach(var b in Tools)if(b.Chord!=0&&(b.Chord<(int)Keys.A||b.Chord>(int)Keys.Z))throw new ArgumentException("Tool shortcuts must be a single letter or disabled.");
        foreach(var group in new[]{Capture,Tools})if(group.Where(x=>x.Chord!=0).GroupBy(x=>x.Chord).Any(x=>x.Count()>1))throw new ArgumentException("Two actions share a shortcut. Assign different keys or disable one first.");
        var reserved=new[]{Keys.Control|Keys.C,Keys.Control|Keys.V,Keys.Control|Keys.X,Keys.Control|Keys.S,Keys.Control|Keys.O,Keys.Control|Keys.Z,Keys.Control|Keys.Y,Keys.Control|Keys.D,Keys.Control|Keys.K,Keys.Control|Keys.Shift|Keys.T,Keys.Control|Keys.T};if(Capture.Any(x=>reserved.Contains((Keys)x.Chord)))throw new ArgumentException("That capture shortcut is reserved for editing. Add Shift or choose another key.");
        if(TabLimit<1||TabLimit>200||TabIdleMinutes<0||TabIdleMinutes>1440)throw new ArgumentException("Tab limit must be 1–200 and idle age 0–1440 minutes.");CaptureFileNames.Validate(ScreenshotPattern,ScreenshotFormat);if(UiSize<75||UiSize>200||UiSize%5!=0)throw new ArgumentException("UI size must be 75–200%, in 5% steps.");
        if(CaptureSound<0||CaptureSound>=3||CaptureSoundVolume<0||CaptureSoundVolume>100)throw new ArgumentException("Invalid screenshot sound settings.");if(CaptureDelay<0||CaptureDelay>15||UndoLimit<1||UndoLimit>100)throw new ArgumentException("Delay must be 0–15 seconds; undo history must be 1–100 states.");
    }
    public void Save(string path) { Validate();Directory.CreateDirectory(Path.GetDirectoryName(path));var temp=path+".tmp";using(var s=File.Create(temp))new XmlSerializer(typeof(Preferences)).Serialize(s,this);File.Copy(temp,path,true);File.Delete(temp); }
    public static Preferences Read(string path) { using(var s=File.OpenRead(path)) { var p=(Preferences)new XmlSerializer(typeof(Preferences)).Deserialize(s);p.Normalize();p.Validate();return p; } }
    public static string LegacyFilePath {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"SnapCanvas","settings.xml");}}
    public static void LoadCurrent() { try { if(File.Exists(FilePath))Current=Read(FilePath);else if(File.Exists(LegacyFilePath))Current=Read(LegacyFilePath); } catch(Exception e) { MessageBox.Show("Settings could not be loaded; using defaults.\n"+e.Message,"ShotOut settings"); } }
}
static class AppLaunch {
    [DllImport("user32.dll")] static extern bool ShowWindowAsync(IntPtr hwnd,int cmd);
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hwnd);
    public static bool AllowLaunch(bool multiple) {
        if(multiple)return true;using(var current=Process.GetCurrentProcess())foreach(var other in Process.GetProcessesByName(current.ProcessName))using(other) { if(other.Id==current.Id)continue;try { if(other.SessionId!=current.SessionId)continue;TrayActivation.Signal(other.Id);var h=other.MainWindowHandle;if(h!=IntPtr.Zero) { ShowWindowAsync(h,9);SetForegroundWindow(h); } return false; }catch(InvalidOperationException) { }catch(System.ComponentModel.Win32Exception) { } }return true;
    }
}
static class LoginStartup {
    const string Key="Software\\Microsoft\\Windows\\CurrentVersion\\Run";
    public static string Command { get { return "\""+Application.ExecutablePath+"\" --startup"; } }
    public static string Entry { get { using(var key=Registry.CurrentUser.OpenSubKey(Key))return key==null?null:key.GetValue("ShotOut") as string; } }
    public static string LegacyEntry {get{using(var key=Registry.CurrentUser.OpenSubKey(Key))return key==null?null:key.GetValue("SnapCanvas") as string;}}
    public static bool Enabled { get { return Entry!=null||LegacyEntry!=null; } }
    public static void RestoreLegacy(string value){using(var key=Registry.CurrentUser.CreateSubKey(Key)){if(value==null)key.DeleteValue("SnapCanvas",false);else key.SetValue("SnapCanvas",value,RegistryValueKind.String);}}
    public static void Restore(string value) { using(var key=Registry.CurrentUser.CreateSubKey(Key)) { if(value==null)key.DeleteValue("ShotOut",false);else key.SetValue("ShotOut",value,RegistryValueKind.String); } }
    public static void Set(bool enabled) { using(var key=Registry.CurrentUser.CreateSubKey(Key)) { if(enabled)key.SetValue("ShotOut",Command,RegistryValueKind.String);else key.DeleteValue("ShotOut",false);key.DeleteValue("SnapCanvas",false); } }
}
class ShortcutField : ThemedTextBox {
    public int Chord;
    public ShortcutField(int chord) { Chord=chord;Text=Preferences.KeyLabel(chord);ReadOnly=true;BackColor=Color.FromArgb(43,46,54);ForeColor=Color.White;BorderStyle=BorderStyle.FixedSingle;Width=210;Height=28; }
    protected override bool ProcessCmdKey(ref Message msg,Keys keyData) { var code=keyData&Keys.KeyCode;if(code==Keys.Tab)return base.ProcessCmdKey(ref msg,keyData);if(code==Keys.ControlKey||code==Keys.ShiftKey||code==Keys.Menu)return true;Chord=code==Keys.Back||code==Keys.Delete?0:(int)keyData;Text=Preferences.KeyLabel(Chord);return true; }
}
partial class SettingsDialog : Form {
    public Preferences Value;
    public bool StartupEnabled;
    Dictionary<string,CheckBox> checks=new Dictionary<string,CheckBox>();
    Dictionary<ShortcutBinding,ShortcutField> shortcuts=new Dictionary<ShortcutBinding,ShortcutField>();
    NumberField delay,undo;
    public Action<SettingsDialog> SaveChanges;
    public SettingsDialog(Preferences initial,bool? startup=null) {
        using(var stream=typeof(SettingsDialog).Assembly.GetManifestResourceStream("ShotOut.Icon"))using(var sourceIcon=new Icon(stream)){var windowIcon=(Icon)sourceIcon.Clone();Icon=windowIcon;Disposed+=delegate{windowIcon.Dispose();};}Value=initial.Clone();AutoScaleMode=AutoScaleMode.None;Text="ShotOut settings";Size=new Size(840,720);MinimumSize=new Size(820,600);StartPosition=FormStartPosition.CenterParent;BackColor=Color.FromArgb(30,32,38);ForeColor=Color.FromArgb(233,236,243);Font=new Font("Segoe UI",13,FontStyle.Regular,GraphicsUnit.Pixel);
        var body=BuildSettingsPages(startup);
        var buttons=new FlowLayoutPanel {Dock=DockStyle.Bottom,Height=62,Padding=new Padding(24,8,10,8),FlowDirection=FlowDirection.RightToLeft};var save=Button("Save settings",154);save.BackColor=UiTheme.Accent;save.Click+=delegate {try {Value.ScreenshotFolder=screenshotFolderEdit.Text.Trim();Value.ProjectFolder=projectFolderEdit.Text.Trim();Value.MonthlyScreenshots=checks["monthly"].Checked;Value.AutoRecover=checks["recovery"].Checked;Value.AdaptiveToolbar=checks["adaptiveToolbar"].Checked;Value.ShowToolLabels=checks["toolLabels"].Checked;SaveTabOptions();SaveAfterCaptureOptions();Value.AutoCopyCapture=checks["autocopy"].Checked;Value.AfterCaptureSave=checks["afterSave"].Checked;Value.AfterCaptureSaveAs=checks["afterSaveAs"].Checked;Value.AfterCaptureOpenFolder=checks["afterFolder"].Checked;Value.AfterCaptureOpenViewer=checks["afterViewer"].Checked;Value.OpenEditorAfterCapture=checks["openaftercapture"].Checked;Value.SmartRegion=checks["smartregion"].Checked;Value.SmartElements=checks["smartelements"].Checked;Value.CaptureSoundEnabled=checks["captureSound"].Checked;Value.CaptureSound=Array.IndexOf(CaptureAudio.Labels,captureSoundChoice.SelectedItem);Value.CaptureSoundVolume=(int)captureSoundVolume.Value;Value.ShowToolHints=checks["toolHints"].Checked;Value.ShowLayers=checks["layers"].Checked;Value.AllowMultiple=checks["multiple"].Checked;Value.StartOnLogin=checks["startup"].Checked;StartupEnabled=Value.StartOnLogin;Value.RememberWindow=checks["window"].Checked;Value.ZoomToFit=checks["fit"].Checked;Value.ConfirmUnsaved=checks["confirm"].Checked;Value.IncludeCursor=checks["cursor"].Checked;Value.UsePrintScreen=checks["printScreen"].Checked;Value.CheckForUpdates=checks["updates"].Checked;Value.CaptureDelay=(int)delay.Value;Value.UndoLimit=(int)undo.Value;foreach(var p in shortcuts)p.Key.Chord=p.Value.Chord;Value.Validate();if(SaveChanges!=null)SaveChanges(this);DialogResult=DialogResult.OK;Close();}catch(Exception e){MessageBox.Show(this,e.Message,"Settings not saved");} };buttons.Controls.Add(save);var cancel=Button("Cancel",100);cancel.DialogResult=DialogResult.Cancel;buttons.Controls.Add(cancel);CancelButton=cancel;
        Controls.Add(body);Controls.Add(buttons);
    }
    static ModernButton Button(string text,int width) { return new ModernButton {Text=text,Width=width,Height=36,Margin=new Padding(5),ForeColor=Color.White,BackColor=Color.FromArgb(43,46,54),Font=new Font("Segoe UI",13,FontStyle.Regular,GraphicsUnit.Pixel)}; }
    static void AddTitle(FlowLayoutPanel p,string title) {p.Controls.Add(new Label {Text=title,Font=new Font("Segoe UI",16,FontStyle.Bold,GraphicsUnit.Pixel),Width=550,Height=45,Padding=new Padding(0,14,0,0)});}
    void Toggle(FlowLayoutPanel p,string id,string caption,bool value) {var c=new ThemeCheckBox {Text=caption,Checked=value,Width=550,Height=34};checks[id]=c;p.Controls.Add(c);}
    static NumberField NumberRow(FlowLayoutPanel p,string text,int min,int max,int value) {var row=new FlowLayoutPanel {Width=550,Height=40};row.Controls.Add(new Label{Text=text,Width=340,Height=34,TextAlign=ContentAlignment.MiddleLeft});var field=new NumberField{Minimum=min,Maximum=max,Value=value,Width=100};row.Controls.Add(field);p.Controls.Add(row);return field;}
    void ShortcutRow(FlowLayoutPanel p,ShortcutBinding b) {var row=new FlowLayoutPanel {Width=550,Height=36};row.Controls.Add(new Label {Text=Editor.ToolDisplayName(b.Action),Width=280,Height=30,TextAlign=ContentAlignment.MiddleLeft});var field=new ShortcutField(b.Chord);shortcuts[b]=field;row.Controls.Add(field);p.Controls.Add(row);}
}
partial class Editor {
    CheckBox underlineBox;
    string CaptureLabel(string action) { return Preferences.KeyLabel(Preferences.Current.Capture.First(x=>x.Action==action).Chord); }
    void RefreshShortcutHints(Control root) { foreach(Control c in root.Controls) { if(c is Button&&c.AccessibleName!=null&&c.AccessibleName.StartsWith("Capture region")) {c.AccessibleName="Capture region ("+CaptureLabel("Region")+")";tips.SetToolTip(c,c.AccessibleName);} if(c is Label&&c.Text.Contains("to capture")&&c.Text.Contains("Hover"))c.Text=CaptureLabel("Region")+" to capture · Hover a tool for its shortcut";RefreshShortcutHints(c); } }
    void FitOnOpen() { if(Preferences.Current.ZoomToFit)View.Fit(); }
    void LoadPreferences() {ApplyUiSize();ApplyToolbarPreference();ApplyLayerVisibility();delay.Value=Preferences.Current.CaptureDelay;cursorBox.Checked=Preferences.Current.IncludeCursor;var p=Preferences.Current;if(p.RememberWindow&&p.WindowWidth>=MinimumSize.Width&&p.WindowHeight>=MinimumSize.Height) {var rect=new Rectangle(p.WindowX,p.WindowY,p.WindowWidth,p.WindowHeight);if(Screen.AllScreens.Any(s=>s.WorkingArea.IntersectsWith(rect))) {StartPosition=FormStartPosition.Manual;Bounds=rect;} } }
    void SaveSessionPreferences() {if(Preferences.Testing)return;var p=Preferences.Current;p.CaptureDelay=(int)delay.Value;p.IncludeCursor=cursorBox.Checked;var r=WindowState==FormWindowState.Normal?Bounds:RestoreBounds;p.WindowX=r.X;p.WindowY=r.Y;p.WindowWidth=r.Width;p.WindowHeight=r.Height;try {p.Save(Preferences.FilePath);}catch(Exception ex){MessageBox.Show("Could not save preferences: "+ex.Message,"ShotOut");} }
    void ShowSettings() {
        CloseImageEffects();var initial=Preferences.Current.Clone();initial.CaptureDelay=(int)delay.Value;initial.IncludeCursor=cursorBox.Checked;using(var dialog=new SettingsDialog(initial)) {
            dialog.CheckPrintScreen=CheckPrintScreenAvailability;dialog.ConfigurePrintScreen=ConfigurePrintScreenImmediately;dialog.PrintScreenSetupStatus=PrintScreenFriendlyStatus;
            dialog.SaveChanges=delegate(SettingsDialog d) {var old=Preferences.Current;string problem;if(!RegisterCaptureKeys(d.Value,out problem)) {string ignored;RegisterCaptureKeys(old,out ignored);throw new InvalidOperationException(problem);}
                string startup=LoginStartup.Entry,legacyStartup=LoginStartup.LegacyEntry;try {if(d.StartupEnabled?startup!=LoginStartup.Command:startup!=null||legacyStartup!=null)LoginStartup.Set(d.StartupEnabled);d.Value.Save(Preferences.FilePath);}catch {try {if(LoginStartup.Entry!=startup)LoginStartup.Restore(startup);if(LoginStartup.LegacyEntry!=legacyStartup)LoginStartup.RestoreLegacy(legacyStartup);}finally {string ignored;RegisterCaptureKeys(old,out ignored);}throw;}
                Preferences.Current=d.Value;ScheduleTabMaintenance();HideFirstUseHint();ApplyUiSize();RefreshScreenshotBrowser(true);ApplyRecoveryPreference();ApplyToolbarPreference();ApplyLayerVisibility();delay.Value=d.Value.CaptureDelay;cursorBox.Checked=d.Value.IncludeCursor;RefreshShortcutHints(this);UpdateInspector();UpdateStatus("Settings saved. Startup launches this executable; keep it in its current folder.");};dialog.ShowDialog(this);}
    }
}
}

