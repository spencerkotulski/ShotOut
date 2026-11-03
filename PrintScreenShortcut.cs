using System;using System.Collections.Generic;using System.Drawing;using System.IO;using System.Linq;using System.Runtime.InteropServices;using System.Windows.Forms;
namespace ShotOut {
static class PrintScreenIntegration {
    internal const string KeyboardSettingsUri="ms-settings:easeofaccess-keyboard";
    internal static bool? WindowsSnippingEnabled(){try{using(var key=Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Control Panel\Keyboard")){var value=key==null?null:key.GetValue("PrintScreenKeyForSnippingEnabled");if(value==null)return null;return Convert.ToInt32(value)!=0;}}catch{return null;}}
    internal static string WindowsSettingStatus(bool? enabled){return enabled==false?"Windows' Print Screen screen-capture setting is already OFF.":enabled==true?"Windows' Print Screen screen-capture setting is ON. Turn it off in Windows keyboard settings.":"Windows' Print Screen setting could not be confirmed; check its keyboard settings page.";}
    internal static string RegistrationFailure(int error,bool? enabled){
        string detail=error==1409?"Print Screen is already registered (Windows error 1409). Another application or a remaining Windows shortcut registration holds the key. Windows does not identify the owner.":error==5?"Windows denied access to register Print Screen (Windows error 5). This is an access restriction, not confirmation that another app owns the key.":"Print Screen could not be registered"+(error==0?" (Windows supplied no error code).":" (Windows error "+error+": "+new System.ComponentModel.Win32Exception(error).Message+").");
        string next=error==1409?"Release Print Screen in other screenshot apps (including tray apps), then retry. If it remains reserved with those apps closed, signing out or restarting Windows may clear a stale registration.":"Use your existing capture shortcut and check the Windows error above.";
        return detail+"\n\n"+WindowsSettingStatus(enabled)+"\n\n"+next+" Your configured ShotOut capture shortcuts remain available.";
    }
}
partial class SettingsDialog {
    internal Func<string> CheckPrintScreen;
    void AddPrintScreenOptions(FlowLayoutPanel page){BuildPrintScreenSetupOptions(page);}
    internal void VerifyPrintScreenOptions(List<string> report,string folder){
        Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);report.Add("PASS "+label);};
        check(!checks["printScreen"].Checked,"Print Screen capture is off by default in Settings");ShowSettingsPage("Experimental");Show();Application.DoEvents();using(var image=new Bitmap(Width,Height)){DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(folder,"capture-settings.png"));}
        check(settingsPages["Experimental"].Controls.OfType<Button>().Any(button=>button.AccessibleName=="Set up Print Screen"),"Experimental settings exposes guided Print Screen setup");
        checks["printScreen"].Checked=true;foreach(var save in Controls.OfType<FlowLayoutPanel>().SelectMany(panel=>panel.Controls.OfType<Button>()).Where(button=>button.Text=="Save settings").ToArray())save.PerformClick();
        check(DialogResult==DialogResult.OK&&Value.UsePrintScreen,"Saving Settings retains the Print Screen checkbox");
    }
}
partial class Editor {
    Func<int,uint,uint,bool> registerCaptureHotKeyOverride;Action<int> unregisterCaptureHotKeyOverride;Func<int> captureHotKeyErrorOverride;int lastCaptureHotKeyError;bool printScreenRegistered;
    void UnregisterCaptureShortcut(int id){if(id==4)printScreenRegistered=false;if(unregisterCaptureHotKeyOverride!=null)unregisterCaptureHotKeyOverride(id);else UnregisterHotKey(Handle,id);}
    bool TryRegisterCaptureShortcut(int id,uint modifiers,uint key){bool ok=registerCaptureHotKeyOverride!=null?registerCaptureHotKeyOverride(id,modifiers,key):RegisterHotKey(Handle,id,modifiers,key);lastCaptureHotKeyError=ok?0:registerCaptureHotKeyOverride==null?Marshal.GetLastWin32Error():captureHotKeyErrorOverride==null?0:captureHotKeyErrorOverride();if(id==4)printScreenRegistered=ok;return ok;}
    string CheckPrintScreenAvailability(){var state=PrintScreenIntegration.WindowsSnippingEnabled();if(printScreenRegistered)return "Print Screen is registered by this ShotOut instance.\n\n"+PrintScreenIntegration.WindowsSettingStatus(state);const int probe=104;bool ok=TryRegisterCaptureShortcut(probe,0x4000,(uint)Keys.PrintScreen);int error=lastCaptureHotKeyError;try{return ok?"Print Screen is available right now. Enable the checkbox and save settings to use it.\n\n"+PrintScreenIntegration.WindowsSettingStatus(state):PrintScreenIntegration.RegistrationFailure(error,state);}finally{if(ok)UnregisterCaptureShortcut(probe);}}
    static string CaptureActionForShortcut(int id){switch(id){case 1:case 4:return "Region";case 2:return "Screen";case 3:return "Window";default:return null;}}
    void ReportCaptureShortcutProblem(string problem){UpdateStatus(problem+" Open Settings > Experimental to review Print Screen, or Capture shortcuts for other bindings.");if(!Preferences.Testing&&Preferences.Current.UsePrintScreen&&problem.StartsWith("Print Screen")&&trayIcon!=null&&trayIcon.Visible)trayIcon.ShowBalloonTip(6000,"Print Screen unavailable","Windows could not register Print Screen. Open Settings > Experimental > Check Print Screen for details. Your other ShotOut shortcuts still work.",ToolTipIcon.Info);}
    bool RegisterCaptureKeys(Preferences preferences,out string problem){
        problem="";for(int id=1;id<=4;id++)UnregisterCaptureShortcut(id);
        for(int i=0;i<3;i++){
            var binding=preferences.Capture[i];if(binding.Chord==0)continue;var key=(Keys)binding.Chord;uint modifiers=0x4000;
            if((key&Keys.Control)!=0)modifiers|=2;if((key&Keys.Shift)!=0)modifiers|=4;if((key&Keys.Alt)!=0)modifiers|=1;
            if(!TryRegisterCaptureShortcut(i+1,modifiers,(uint)(key&Keys.KeyCode))){problem="Shortcut "+Preferences.KeyLabel(binding.Chord)+" ("+binding.Action+") is already in use or could not be registered.";for(int id=1;id<=4;id++)UnregisterCaptureShortcut(id);return false;}
        }
        if(preferences.UsePrintScreen&&!TryRegisterCaptureShortcut(4,0x4000,(uint)Keys.PrintScreen)){
            problem=PrintScreenIntegration.RegistrationFailure(lastCaptureHotKeyError,PrintScreenIntegration.WindowsSnippingEnabled());
            // Keep the ordinary capture keys active when only this optional extra shortcut fails.
            return false;
        }
        return true;
    }
    public void VerifyPrintScreenShortcut(List<string> report,string folder){
        Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);report.Add("PASS "+label);};
        var preferences=new Preferences();var original=preferences.Capture.Select(binding=>binding.Chord).ToArray();var registered=new Dictionary<int,Tuple<uint,uint>>();var released=new List<int>();bool conflict=false;string problem;
        registerCaptureHotKeyOverride=(id,modifiers,key)=>{if((id==4||id==104)&&conflict)return false;registered[id]=Tuple.Create(modifiers,key);return true;};unregisterCaptureHotKeyOverride=id=>{released.Add(id);registered.Remove(id);};
        try{
            captureHotKeyErrorOverride=()=>1409;
            check(!preferences.UsePrintScreen,"New and migrated preferences leave Print Screen opt-in");check(RegisterCaptureKeys(preferences,out problem)&&registered.Count==3&&!registered.ContainsKey(4),"Disabled Print Screen registers only the existing capture shortcuts");
            preferences.UsePrintScreen=true;preferences.Validate();check(RegisterCaptureKeys(preferences,out problem)&&registered.Count==4&&registered[4].Item1==0x4000&&registered[4].Item2==(uint)Keys.PrintScreen,"Print Screen uses ordinary no-repeat hotkey registration without a keyboard hook");
            check(preferences.Capture.Select(binding=>binding.Chord).SequenceEqual(original),"Enabling Print Screen preserves all three custom capture bindings");
            check(CaptureActionForShortcut(4)=="Region"&&CaptureActionForShortcut(1)=="Region"&&CaptureActionForShortcut(2)=="Screen"&&CaptureActionForShortcut(3)=="Window"&&CaptureActionForShortcut(999)==null,"Print Screen routes to region capture and unknown hotkey IDs are ignored");
            conflict=true;check(!RegisterCaptureKeys(preferences,out problem)&&registered.Count==3&&registered.ContainsKey(1)&&!registered.ContainsKey(4)&&problem.Contains("Windows")&&problem.Contains("other screenshot apps"),"A Print Screen conflict reports recovery directions and keeps ordinary capture shortcuts active");
            check(CheckPrintScreenAvailability().Contains("1409")&&registered.Count==3,"Availability checks report a conflicting reservation without disturbing the other hotkeys");
            preferences.UsePrintScreen=false;check(RegisterCaptureKeys(preferences,out problem)&&!registered.ContainsKey(4)&&released.Contains(4),"Disabling Print Screen unregisters the extra hotkey");
            preferences.UsePrintScreen=true;string path=Path.Combine(folder,"print-screen-preferences.xml");preferences.Save(path);check(Preferences.Read(path).UsePrintScreen&&preferences.Clone().UsePrintScreen,"Print Screen preference survives XML persistence and cloning");
            File.WriteAllText(path,File.ReadAllText(path).Replace("<UsePrintScreen>true</UsePrintScreen>",""));check(!Preferences.Read(path).UsePrintScreen,"Existing preference files without the new field keep Print Screen disabled");
            string offMessage=PrintScreenIntegration.RegistrationFailure(1409,false);check(offMessage.Contains("already OFF")&&!offMessage.Contains("Turn it off"),"A disabled Windows toggle is reported accurately without instructions to disable it again");
            check(PrintScreenIntegration.RegistrationFailure(5,false).Contains("access restriction")&&!PrintScreenIntegration.RegistrationFailure(5,false).Contains("already registered"),"Access-denied failures are distinguished from shortcut conflicts");
            check(PrintScreenIntegration.RegistrationFailure(1409,true).Contains("Turn it off")&&PrintScreenIntegration.RegistrationFailure(87,null).Contains("87"),"Enabled or unreadable Windows settings and other native error codes are distinguished");
            conflict=false;preferences.UsePrintScreen=true;RegisterCaptureKeys(preferences,out problem);check(CheckPrintScreenAvailability().Contains("this ShotOut instance"),"Availability checking does not mistake this app's own hotkey for a conflict");
            preferences.UsePrintScreen=false;RegisterCaptureKeys(preferences,out problem);check(CheckPrintScreenAvailability().Contains("available right now")&&!registered.ContainsKey(104),"An availability probe releases its temporary registration");
                        using(var settings=new SettingsDialog(new Preferences(),false))settings.VerifyPrintScreenOptions(report,folder);
        }finally{registerCaptureHotKeyOverride=null;unregisterCaptureHotKeyOverride=null;captureHotKeyErrorOverride=null;printScreenRegistered=false;}
    }
}
}


