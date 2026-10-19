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
    public string ScreenshotFolder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),"Screenshots");
    public bool AdaptiveToolbar=true;
    public bool AutoRecover=true;
    public bool OpenEditorAfterCapture;
    public bool ShowLayers=true, AutoCopyCapture=true, SmartRegion=true;
    public bool AllowMultiple, StartOnLogin, IncludeCursor, RememberWindow=true, ZoomToFit=true, ConfirmUnsaved=true;
    public int CaptureDelay, UndoLimit=24, WindowX,WindowY,WindowWidth,WindowHeight;
    public List<ShortcutBinding> Capture=new List<ShortcutBinding> { new ShortcutBinding {Action="Region",Chord=(int)(Keys.Control|Keys.Shift|Keys.D2)},new ShortcutBinding {Action="Screen",Chord=(int)(Keys.Control|Keys.Shift|Keys.D3)},new ShortcutBinding {Action="Window",Chord=(int)(Keys.Control|Keys.Shift|Keys.D4)} };
    public List<ShortcutBinding> Tools=new List<ShortcutBinding>();
    public static Preferences Current=new Preferences();
    public static bool Testing;
    public static string FilePath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"ShotOut","settings.xml"); } }
    public Preferences() { string[] names={"Select","Arrow","Double arrow","Line","Rectangle","Ellipse","Pen","Text","Balloon","Step","Highlight","Blur","Pixelate","Crop","Magnify","Spotlight","Eraser","Emoji","Cursor","Redact","Cut rows","Cut columns","Image","Bucket","Hand","Clone stamp"};string letters="VADLREFTONHBPCM SWJKXUGIQYZ".Replace(" ","");for(int i=0;i<names.Length;i++)Tools.Add(new ShortcutBinding {Action=names[i],Chord=(int)(Keys)Enum.Parse(typeof(Keys),letters[i].ToString())}); }
    public Preferences Clone() { using(var s=new MemoryStream()) { var xml=new XmlSerializer(typeof(Preferences));xml.Serialize(s,this);s.Position=0;var p=(Preferences)xml.Deserialize(s);p.Normalize();return p; } }
    public Dictionary<Keys,string> ToolMap() { return Tools.Where(x=>x.Chord!=0).ToDictionary(x=>(Keys)x.Chord,x=>x.Action); }
    public static string KeyLabel(int chord) { return chord==0?"Disabled":new KeysConverter().ConvertToString((Keys)chord); }
    void Normalize() { Capture=Capture.GroupBy(b=>b.Action).Select(g=>g.Last()).ToList();Tools=Tools.GroupBy(b=>b.Action).Select(g=>g.Last()).ToList();var bucket=Tools.FirstOrDefault(b=>b.Action=="Bucket");if(bucket!=null&&bucket.Chord==(int)Keys.Q&&Tools.Any(b=>b.Action!="Bucket"&&b.Chord==bucket.Chord))bucket.Chord=0;var hand=Tools.FirstOrDefault(b=>b.Action=="Hand");if(hand!=null&&hand.Chord==(int)Keys.Y&&Tools.Any(b=>b.Action!="Hand"&&b.Chord==hand.Chord))hand.Chord=0;var clone=Tools.FirstOrDefault(b=>b.Action=="Clone stamp");if(clone!=null&&clone.Chord==(int)Keys.Z&&Tools.Any(b=>b.Action!="Clone stamp"&&b.Chord==clone.Chord))clone.Chord=0;if(String.IsNullOrWhiteSpace(ScreenshotFolder))ScreenshotFolder=new Preferences().ScreenshotFolder; }
    public void Validate() {
        if(String.IsNullOrWhiteSpace(ScreenshotFolder)||!Path.IsPathRooted(ScreenshotFolder))throw new ArgumentException("Choose an absolute screenshot folder path.");ScreenshotFolder=Path.GetFullPath(ScreenshotFolder);
        if(Capture==null||Tools==null||Capture.Count!=3||Tools.Count!=new Preferences().Tools.Count)throw new ArgumentException("Missing shortcut settings.");
        var defaults=new Preferences();foreach(var b in defaults.Capture)if(Capture.Count(x=>x.Action==b.Action)!=1)throw new ArgumentException("Each capture action needs one binding.");
        foreach(var b in defaults.Tools)if(Tools.Count(x=>x.Action==b.Action)!=1)throw new ArgumentException("Each tool needs one binding.");
        foreach(var b in Capture) { var k=(Keys)b.Chord;var code=k&Keys.KeyCode;if(b.Chord!=0&&(code==Keys.None||code==Keys.ControlKey||code==Keys.ShiftKey||code==Keys.Menu||(k&(Keys.Control|Keys.Alt))==0))throw new ArgumentException("Capture shortcuts need Ctrl or Alt plus a key."); }
        foreach(var b in Tools)if(b.Chord!=0&&(b.Chord<(int)Keys.A||b.Chord>(int)Keys.Z))throw new ArgumentException("Tool shortcuts must be a single letter or disabled.");
        foreach(var group in new[]{Capture,Tools})if(group.Where(x=>x.Chord!=0).GroupBy(x=>x.Chord).Any(x=>x.Count()>1))throw new ArgumentException("Two actions share a shortcut. Assign different keys or disable one first.");
        var reserved=new[]{Keys.Control|Keys.C,Keys.Control|Keys.V,Keys.Control|Keys.X,Keys.Control|Keys.S,Keys.Control|Keys.O,Keys.Control|Keys.Z,Keys.Control|Keys.Y,Keys.Control|Keys.D};if(Capture.Any(x=>reserved.Contains((Keys)x.Chord)))throw new ArgumentException("That capture shortcut is reserved for editing. Add Shift or choose another key.");
        if(CaptureDelay<0||CaptureDelay>15||UndoLimit<1||UndoLimit>100)throw new ArgumentException("Delay must be 0–15 seconds; undo history must be 1–100 states.");
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
class ShortcutField : TextBox {
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
        Value=initial.Clone();AutoScaleMode=AutoScaleMode.None;Text="ShotOut settings";Size=new Size(670,730);MinimumSize=new Size(620,620);StartPosition=FormStartPosition.CenterParent;BackColor=Color.FromArgb(30,32,38);ForeColor=Color.FromArgb(233,236,243);Font=new Font("Segoe UI",13,FontStyle.Regular,GraphicsUnit.Pixel);
        var body=new FlowLayoutPanel { Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new Padding(24,15,24,10) };
        AddTitle(body,"General");Toggle(body,"recovery","Automatically recover unsaved tabs",Value.AutoRecover);Toggle(body,"adaptiveToolbar","Two-column toolbar in smaller windows",Value.AdaptiveToolbar);Toggle(body,"openaftercapture","Open editor automatically after capture",Value.OpenEditorAfterCapture);Toggle(body,"autocopy","Copy completed captures to clipboard",Value.AutoCopyCapture);Toggle(body,"smartregion","Snap to windows during region capture",Value.SmartRegion);Toggle(body,"layers","Show Layers panel",Value.ShowLayers);Toggle(body,"multiple","Allow multiple instances",Value.AllowMultiple);Toggle(body,"startup","Start ShotOut when I sign in to Windows",startup??LoginStartup.Enabled);Toggle(body,"window","Remember window position and size",Value.RememberWindow);Toggle(body,"fit","Zoom to fit when opening images",Value.ZoomToFit);Toggle(body,"confirm","Confirm before discarding unsaved edits",Value.ConfirmUnsaved);Toggle(body,"cursor","Include cursor in captures",Value.IncludeCursor);
        AddScreenshotFolderSetting(body);delay=NumberRow(body,"Default capture delay · seconds",0,15,Value.CaptureDelay);undo=NumberRow(body,"Undo history · states",1,100,Value.UndoLimit);
        AddTitle(body,"Capture shortcuts · work outside the app");body.Controls.Add(new Label {Text="Click a field and press your shortcut. Backspace disables it.",Width=550,Height=30,ForeColor=Color.FromArgb(155,166,187)});foreach(var b in Value.Capture)ShortcutRow(body,b);
        AddTitle(body,"Tool shortcuts · single letters");body.Controls.Add(new Label {Text="Standard Ctrl+C / V / S / Z editing commands stay unchanged.",Width=550,Height=30,ForeColor=Color.FromArgb(155,166,187)});foreach(var b in Value.Tools)ShortcutRow(body,b);
        var buttons=new FlowLayoutPanel {Dock=DockStyle.Bottom,Height=62,Padding=new Padding(24,8,10,8),FlowDirection=FlowDirection.RightToLeft};var save=Button("Save settings",154);save.BackColor=Color.FromArgb(84,119,239);save.Click+=delegate {try {Value.ScreenshotFolder=screenshotFolderEdit.Text.Trim();Value.AutoRecover=checks["recovery"].Checked;Value.AdaptiveToolbar=checks["adaptiveToolbar"].Checked;Value.AutoCopyCapture=checks["autocopy"].Checked;Value.OpenEditorAfterCapture=checks["openaftercapture"].Checked;Value.SmartRegion=checks["smartregion"].Checked;Value.ShowLayers=checks["layers"].Checked;Value.AllowMultiple=checks["multiple"].Checked;Value.StartOnLogin=checks["startup"].Checked;StartupEnabled=Value.StartOnLogin;Value.RememberWindow=checks["window"].Checked;Value.ZoomToFit=checks["fit"].Checked;Value.ConfirmUnsaved=checks["confirm"].Checked;Value.IncludeCursor=checks["cursor"].Checked;Value.CaptureDelay=(int)delay.Value;Value.UndoLimit=(int)undo.Value;foreach(var p in shortcuts)p.Key.Chord=p.Value.Chord;Value.Validate();if(SaveChanges!=null)SaveChanges(this);DialogResult=DialogResult.OK;Close();}catch(Exception e){MessageBox.Show(this,e.Message,"Settings not saved");} };buttons.Controls.Add(save);var cancel=Button("Cancel",100);cancel.DialogResult=DialogResult.Cancel;buttons.Controls.Add(cancel);CancelButton=cancel;
        Controls.Add(body);Controls.Add(buttons);
    }
    static ModernButton Button(string text,int width) { return new ModernButton {Text=text,Width=width,Height=36,Margin=new Padding(5),ForeColor=Color.White,BackColor=Color.FromArgb(43,46,54),Font=new Font("Segoe UI",13,FontStyle.Regular,GraphicsUnit.Pixel)}; }
    static void AddTitle(FlowLayoutPanel p,string title) {p.Controls.Add(new Label {Text=title,Font=new Font("Segoe UI",16,FontStyle.Bold,GraphicsUnit.Pixel),Width=550,Height=45,Padding=new Padding(0,14,0,0)});}
    void Toggle(FlowLayoutPanel p,string id,string caption,bool value) {var c=new ThemeCheckBox {Text=caption,Checked=value,Width=550,Height=34};checks[id]=c;p.Controls.Add(c);}
    static NumberField NumberRow(FlowLayoutPanel p,string text,int min,int max,int value) {var row=new FlowLayoutPanel {Width=550,Height=40};row.Controls.Add(new Label{Text=text,Width=340,Height=34,TextAlign=ContentAlignment.MiddleLeft});var field=new NumberField{Minimum=min,Maximum=max,Value=value,Width=100};row.Controls.Add(field);p.Controls.Add(row);return field;}
    void ShortcutRow(FlowLayoutPanel p,ShortcutBinding b) {var row=new FlowLayoutPanel {Width=550,Height=36};row.Controls.Add(new Label {Text=b.Action,Width=280,Height=30,TextAlign=ContentAlignment.MiddleLeft});var field=new ShortcutField(b.Chord);shortcuts[b]=field;row.Controls.Add(field);p.Controls.Add(row);}
}
partial class Editor {
    CheckBox underlineBox;
    string CaptureLabel(string action) { return Preferences.KeyLabel(Preferences.Current.Capture.First(x=>x.Action==action).Chord); }
    void RefreshShortcutHints(Control root) { foreach(Control c in root.Controls) { if(c is Button&&c.AccessibleName!=null&&c.AccessibleName.StartsWith("Capture region")) {c.AccessibleName="Capture region ("+CaptureLabel("Region")+")";tips.SetToolTip(c,c.AccessibleName);} if(c is Label&&c.Text.Contains("to capture")&&c.Text.Contains("Hover"))c.Text=CaptureLabel("Region")+" to capture · Hover a tool for its shortcut";RefreshShortcutHints(c); } }
    void FitOnOpen() { if(Preferences.Current.ZoomToFit)View.Fit(); }
    void LoadPreferences() {ApplyToolbarPreference();ApplyLayerVisibility();delay.Value=Preferences.Current.CaptureDelay;cursorBox.Checked=Preferences.Current.IncludeCursor;var p=Preferences.Current;if(p.RememberWindow&&p.WindowWidth>=MinimumSize.Width&&p.WindowHeight>=MinimumSize.Height) {var rect=new Rectangle(p.WindowX,p.WindowY,p.WindowWidth,p.WindowHeight);if(Screen.AllScreens.Any(s=>s.WorkingArea.IntersectsWith(rect))) {StartPosition=FormStartPosition.Manual;Bounds=rect;} } }
    void SaveSessionPreferences() {if(Preferences.Testing)return;var p=Preferences.Current;p.CaptureDelay=(int)delay.Value;p.IncludeCursor=cursorBox.Checked;var r=WindowState==FormWindowState.Normal?Bounds:RestoreBounds;p.WindowX=r.X;p.WindowY=r.Y;p.WindowWidth=r.Width;p.WindowHeight=r.Height;try {p.Save(Preferences.FilePath);}catch(Exception ex){MessageBox.Show("Could not save preferences: "+ex.Message,"ShotOut");} }
    bool RegisterCaptureKeys(Preferences p,out string problem) {problem="";for(int i=1;i<=3;i++)UnregisterHotKey(Handle,i);for(int i=0;i<3;i++) {var b=p.Capture[i];if(b.Chord==0)continue;var k=(Keys)b.Chord;uint mods=0x4000;if((k&Keys.Control)!=0)mods|=2;if((k&Keys.Shift)!=0)mods|=4;if((k&Keys.Alt)!=0)mods|=1;if(!RegisterHotKey(Handle,i+1,mods,(uint)(k&Keys.KeyCode))) {problem="Shortcut "+Preferences.KeyLabel(b.Chord)+" ("+b.Action+") is already in use.";for(int n=1;n<=3;n++)UnregisterHotKey(Handle,n);return false;} }return true; }
    void ShowSettings() {
        CloseImageEffects();var initial=Preferences.Current.Clone();initial.CaptureDelay=(int)delay.Value;initial.IncludeCursor=cursorBox.Checked;using(var dialog=new SettingsDialog(initial)) {
            dialog.SaveChanges=delegate(SettingsDialog d) {var old=Preferences.Current;string problem;if(!RegisterCaptureKeys(d.Value,out problem)) {string ignored;RegisterCaptureKeys(old,out ignored);throw new InvalidOperationException(problem);}
                string startup=LoginStartup.Entry,legacyStartup=LoginStartup.LegacyEntry;try {if(d.StartupEnabled?startup!=LoginStartup.Command:startup!=null||legacyStartup!=null)LoginStartup.Set(d.StartupEnabled);d.Value.Save(Preferences.FilePath);}catch {try {if(LoginStartup.Entry!=startup)LoginStartup.Restore(startup);if(LoginStartup.LegacyEntry!=legacyStartup)LoginStartup.RestoreLegacy(legacyStartup);}finally {string ignored;RegisterCaptureKeys(old,out ignored);}throw;}
                Preferences.Current=d.Value;RefreshScreenshotBrowser(true);ApplyRecoveryPreference();ApplyToolbarPreference();ApplyLayerVisibility();delay.Value=d.Value.CaptureDelay;cursorBox.Checked=d.Value.IncludeCursor;RefreshShortcutHints(this);UpdateInspector();UpdateStatus("Settings saved. Startup launches this executable; keep it in its current folder.");};dialog.ShowDialog(this);}
    }
}
}
