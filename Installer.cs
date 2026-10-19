using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("ShotOut Setup")]
[assembly: AssemblyProduct("ShotOut")]
[assembly: AssemblyVersion("1.3.4.0")]
[assembly: AssemblyFileVersion("1.3.4.0")]

namespace ShotOutSetup {
static class Program {
    public const string Version="1.3.4";
    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
    [STAThread] static void Main(string[] args){
        SetProcessDPIAware();Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        try{
            if(args.Length==2&&args[0]=="--self-test"){InstallerTests.Run(args[1]);return;}
            if(args.Length==2&&args[0]=="--uninstall-worker"){
                string temporary=Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
                if(!Path.GetFullPath(Application.ExecutablePath).StartsWith(temporary,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Uninstall cleanup must run from the temporary folder.");
                int pid;if(!int.TryParse(args[1],out pid))throw new ArgumentException("Invalid uninstall process.");
                try{using(var parent=Process.GetProcessById(pid)){if(!parent.WaitForExit(10000))throw new IOException("The uninstaller is still closing. Please try again.");}}catch(ArgumentException){}
                if(!EnsureStopped(null))return;var paths=InstallPaths.Current();SetupCore.Uninstall(paths,new WindowsIntegration());MessageBox.Show("ShotOut has been removed. Your saved images, projects, and preferences were kept.","ShotOut",MessageBoxButtons.OK,MessageBoxIcon.Information);return;
            }
            if(args.Contains("--uninstall")||Path.GetFileName(Application.ExecutablePath).Equals("Uninstall.exe",StringComparison.OrdinalIgnoreCase)){
                if(MessageBox.Show("Uninstall ShotOut? Your saved images, projects, and preferences will be kept.","Uninstall ShotOut",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
                if(!EnsureStopped(null))return;string worker=Path.Combine(Path.GetTempPath(),"ShotOut-Uninstall-"+Guid.NewGuid().ToString("N")+".exe");File.Copy(Application.ExecutablePath,worker);
                Process.Start(new ProcessStartInfo(worker,"--uninstall-worker "+Process.GetCurrentProcess().Id){UseShellExecute=true,WindowStyle=ProcessWindowStyle.Hidden});return;
            }
            Application.Run(new SetupWindow());
        }catch(Exception error){MessageBox.Show(error.Message,"ShotOut setup",MessageBoxButtons.OK,MessageBoxIcon.Error);Environment.ExitCode=1;}
    }
    public static bool EnsureStopped(IWin32Window owner){
        for(;;){bool running=false;using(var current=Process.GetCurrentProcess())foreach(var process in Process.GetProcessesByName("ShotOut"))using(process){try{if(process.SessionId==current.SessionId)running=true;}catch(InvalidOperationException){}catch(System.ComponentModel.Win32Exception){}}
            if(!running)return true;if(MessageBox.Show(owner,"ShotOut is running. Exit it from the tray icon's menu, then click Retry. This keeps your unsaved captures safe.","Close ShotOut first",MessageBoxButtons.RetryCancel,MessageBoxIcon.Information)!=DialogResult.Retry)return false;
        }
    }
}
sealed class InstallPaths {
    public string Root,DesktopLink,StartLink;
    public bool Testing;
    public string App {get{return Path.Combine(Root,"ShotOut.exe");}}
    public string Uninstaller {get{return Path.Combine(Root,"Uninstall.exe");}}
    public string Marker {get{return Path.Combine(Root,".shotout-install");}}
    public static InstallPaths Current(){return new InstallPaths {Root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs","ShotOut"),DesktopLink=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"ShotOut.lnk"),StartLink=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),"ShotOut.lnk")};}
    public static InstallPaths Test(string directory){return new InstallPaths {Root=Path.Combine(directory,"installed"),DesktopLink=Path.Combine(directory,"Desktop.lnk"),StartLink=Path.Combine(directory,"StartMenu.lnk"),Testing=true};}
    public void Validate(){string full=Path.GetFullPath(Root).TrimEnd(Path.DirectorySeparatorChar);if(Path.GetPathRoot(full).TrimEnd(Path.DirectorySeparatorChar).Equals(full,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Cannot install into a drive root.");if(!Testing&&!full.Equals(Path.GetFullPath(Current().Root).TrimEnd(Path.DirectorySeparatorChar),StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Unexpected installation location.");Root=full;}
}
interface IIntegration {
    void AddShortcut(string path,string executable);
    void RemoveShortcut(string path,string executable);
    void Register(InstallPaths paths);
    void Unregister(InstallPaths paths);
}
sealed class WindowsIntegration : IIntegration {
    const string UninstallKey="Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\ShotOut";
    const string StartupKey="Software\\Microsoft\\Windows\\CurrentVersion\\Run";
    public static string StartupCommand(string executable){return "\""+executable+"\" --startup";}
    static object Shortcut(string path,Action<object> action){object shell=null,link=null;try{shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell",true));link=shell.GetType().InvokeMember("CreateShortcut",BindingFlags.InvokeMethod,null,shell,new object[]{path});action(link);return null;}finally{if(link!=null&&Marshal.IsComObject(link))Marshal.FinalReleaseComObject(link);if(shell!=null&&Marshal.IsComObject(shell))Marshal.FinalReleaseComObject(shell);}}
    static void Set(object item,string property,object value){item.GetType().InvokeMember(property,BindingFlags.SetProperty,null,item,new[]{value});}
    public static string ShortcutTarget(string path){string target=null;Shortcut(path,delegate(object link){target=(string)link.GetType().InvokeMember("TargetPath",BindingFlags.GetProperty,null,link,null);});return target;}
    public void AddShortcut(string path,string executable){Directory.CreateDirectory(Path.GetDirectoryName(path));Shortcut(path,delegate(object link){Set(link,"TargetPath",executable);Set(link,"WorkingDirectory",Path.GetDirectoryName(executable));Set(link,"IconLocation",executable+",0");Set(link,"Description","ShotOut screenshot capture and markup");link.GetType().InvokeMember("Save",BindingFlags.InvokeMethod,null,link,null);});}
    public void RemoveShortcut(string path,string executable){if(File.Exists(path)&&String.Equals(ShortcutTarget(path),executable,StringComparison.OrdinalIgnoreCase))File.Delete(path);}
    public void Register(InstallPaths paths){
        using(var key=Registry.CurrentUser.CreateSubKey(UninstallKey)){
            key.SetValue("DisplayName","ShotOut");key.SetValue("DisplayVersion",Program.Version);key.SetValue("Publisher","ShotOut");key.SetValue("InstallLocation",paths.Root);key.SetValue("DisplayIcon",paths.App+",0");key.SetValue("UninstallString","\""+paths.Uninstaller+"\" --uninstall");key.SetValue("NoModify",1,RegistryValueKind.DWord);key.SetValue("NoRepair",1,RegistryValueKind.DWord);key.SetValue("EstimatedSize",(int)(new FileInfo(paths.App).Length/1024+new FileInfo(paths.Uninstaller).Length/1024),RegistryValueKind.DWord);
        }
        using(var key=Registry.CurrentUser.OpenSubKey(StartupKey,true)){if(key!=null&&(key.GetValue("ShotOut")!=null||key.GetValue("SnapCanvas")!=null)){key.SetValue("ShotOut",StartupCommand(paths.App));key.DeleteValue("SnapCanvas",false);}}
    }
    public void Unregister(InstallPaths paths){
        using(var key=Registry.CurrentUser.OpenSubKey(StartupKey,true)){if(key!=null){string value=key.GetValue("ShotOut") as string;if(value!=null&&value.StartsWith("\""+paths.App+"\"",StringComparison.OrdinalIgnoreCase))key.DeleteValue("ShotOut",false);}}
        bool own;using(var key=Registry.CurrentUser.OpenSubKey(UninstallKey))own=key!=null&&String.Equals(key.GetValue("InstallLocation") as string,paths.Root,StringComparison.OrdinalIgnoreCase);if(own)Registry.CurrentUser.DeleteSubKeyTree(UninstallKey,false);
    }
}
static class SetupCore {
    const string Marker="ShotOut.ManagedInstall.1";
    public static byte[] AppPayload(){using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("ShotOut.App.deflate"))using(var inflate=new DeflateStream(stream,CompressionMode.Decompress))using(var output=new MemoryStream()){inflate.CopyTo(output);byte[] bytes=output.ToArray();using(var hash=SHA256.Create())using(var expected=new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("ShotOut.App.sha256"))){if(BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant()!=expected.ReadToEnd().Trim())throw new InvalidDataException("The bundled application failed its integrity check. Download the installer again.");}if(bytes.Length<2||bytes[0]!='M'||bytes[1]!='Z')throw new InvalidDataException("Invalid bundled application.");return bytes;}}
    static void WriteAtomic(string path,byte[] bytes){string staged=path+".new";try{using(var output=new FileStream(staged,FileMode.Create,FileAccess.Write,FileShare.None)){output.Write(bytes,0,bytes.Length);output.Flush();}if(File.Exists(path))File.Replace(staged,path,null);else File.Move(staged,path);}finally{if(File.Exists(staged))File.Delete(staged);}}
    public static void Install(InstallPaths paths,IIntegration integration,bool desktop){
        paths.Validate();if(Directory.Exists(paths.Root)&&Directory.EnumerateFileSystemEntries(paths.Root).Any()&&(!File.Exists(paths.Marker)||File.ReadAllText(paths.Marker)!=Marker))throw new IOException("The installation folder contains files from an unmanaged installation. Choose another location for those files before installing ShotOut.");
        byte[] payload=AppPayload();Directory.CreateDirectory(paths.Root);WriteAtomic(paths.Marker,Encoding.UTF8.GetBytes(Marker));WriteAtomic(paths.App,payload);WriteAtomic(paths.Uninstaller,File.ReadAllBytes(Application.ExecutablePath));
        using(var reader=new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("ShotOut.Setup.Readme")))WriteAtomic(Path.Combine(paths.Root,"README.md"),Encoding.UTF8.GetBytes(reader.ReadToEnd()));
        integration.AddShortcut(paths.StartLink,paths.App);if(desktop)integration.AddShortcut(paths.DesktopLink,paths.App);integration.Register(paths);
    }
    public static void Uninstall(InstallPaths paths,IIntegration integration){
        paths.Validate();if(!File.Exists(paths.Marker)||File.ReadAllText(paths.Marker)!=Marker)throw new IOException("This folder is not a managed ShotOut installation. No files were removed.");
        foreach(string name in new[]{"ShotOut.exe","Uninstall.exe","README.md"}){string file=Path.Combine(paths.Root,name);if(File.Exists(file))File.Delete(file);}
        integration.RemoveShortcut(paths.StartLink,paths.App);integration.RemoveShortcut(paths.DesktopLink,paths.App);integration.Unregister(paths);File.Delete(paths.Marker);
        if(!Directory.EnumerateFileSystemEntries(paths.Root).Any())Directory.Delete(paths.Root,false);
    }
}
sealed class SetupWindow : Form {
    CheckBox desktop;
    Label status;
    Button install;
    public SetupWindow(){
        Text="ShotOut Setup · "+Program.Version;ClientSize=new Size(570,330);StartPosition=FormStartPosition.CenterScreen;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;BackColor=Color.FromArgb(27,29,35);ForeColor=Color.FromArgb(235,238,245);Font=new Font("Segoe UI",10);Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        Controls.Add(new Label {Text="Install ShotOut",Font=new Font("Segoe UI",22,FontStyle.Bold),Location=new Point(28,24),Size=new Size(510,48)});
        Controls.Add(new Label {Text="Fast screenshots and markup, ready to paste anywhere.\nInstalls for your Windows account. No extra downloads.",Location=new Point(30,84),Size=new Size(510,55)});
        desktop=new CheckBox {Text="Add a desktop shortcut",Checked=true,Location=new Point(30,149),Size=new Size(450,30)};Controls.Add(desktop);
        status=new Label {Text=InstallPaths.Current().Root,ForeColor=Color.FromArgb(158,169,190),Location=new Point(30,193),Size=new Size(510,53)};Controls.Add(status);
        install=new Button {Text="Install && launch",Location=new Point(335,263),Size=new Size(205,41),FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(55,104,238),ForeColor=Color.White};install.FlatAppearance.BorderSize=0;install.Click+=InstallClicked;Controls.Add(install);AcceptButton=install;
    }
    void InstallClicked(object sender,EventArgs args){
        if(!Program.EnsureStopped(this))return;install.Enabled=desktop.Enabled=false;status.Text="Installing ShotOut…";UseWaitCursor=true;Update();
        try{var paths=InstallPaths.Current();SetupCore.Install(paths,new WindowsIntegration(),desktop.Checked);status.Text="Installed. Opening ShotOut…";Process.Start(new ProcessStartInfo(paths.App){WorkingDirectory=paths.Root,UseShellExecute=true});Close();}
        catch(Exception error){MessageBox.Show(this,error.Message,"Installation could not finish",MessageBoxButtons.OK,MessageBoxIcon.Error);status.Text="You can retry after resolving the problem.";install.Enabled=desktop.Enabled=true;}finally{UseWaitCursor=false;}
    }
    protected override void Dispose(bool disposing){if(disposing&&Icon!=null){var icon=Icon;Icon=null;icon.Dispose();}base.Dispose(disposing);}
}
static class InstallerTests {
    sealed class FakeIntegration : IIntegration {
        public bool Registered;public string Startup;public readonly List<string> Links=new List<string>();
        public void AddShortcut(string path,string exe){Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,exe);Links.Add(path);}
        public void RemoveShortcut(string path,string exe){if(File.Exists(path)&&File.ReadAllText(path)==exe)File.Delete(path);}
        public void Register(InstallPaths paths){Registered=true;Startup=WindowsIntegration.StartupCommand(paths.App);}
        public void Unregister(InstallPaths paths){Registered=false;}
    }
    public static void Run(string folder){
        Directory.CreateDirectory(folder);var report=new List<string>();Action<bool,string> check=delegate(bool ok,string label){if(!ok)throw new Exception(label);report.Add("PASS "+label);};
        try{
            var paths=InstallPaths.Test(Path.Combine(folder,"case-"+Guid.NewGuid().ToString("N")));var integration=new FakeIntegration();byte[] expected=AppPayloadForTest();SetupCore.Install(paths,integration,true);
            check(File.ReadAllBytes(paths.App).SequenceEqual(expected),"Installer extracts exact integrity-checked application payload");check(File.Exists(paths.Uninstaller)&&File.Exists(paths.Marker),"Uninstaller and installation ownership marker are present");check(File.Exists(paths.StartLink)&&File.Exists(paths.DesktopLink)&&integration.Registered,"Start-menu, desktop, and installed-app registration steps are invoked");check(integration.Startup=="\""+paths.App+"\" --startup","Startup migration quotes stable installed path");
            File.WriteAllText(Path.Combine(paths.Root,"user-project.shotout"),"keep");SetupCore.Install(paths,integration,false);check(File.ReadAllText(Path.Combine(paths.Root,"user-project.shotout"))=="keep","Reinstall preserves user-created files");SetupCore.Uninstall(paths,integration);check(!File.Exists(paths.App)&&!File.Exists(paths.Uninstaller)&&!integration.Registered,"Uninstall removes application, uninstaller, and app integration");check(File.ReadAllText(Path.Combine(paths.Root,"user-project.shotout"))=="keep","Uninstall preserves unknown user files");
            var unknown=InstallPaths.Test(Path.Combine(folder,"unknown-"+Guid.NewGuid().ToString("N")));Directory.CreateDirectory(unknown.Root);File.WriteAllText(Path.Combine(unknown.Root,"other.txt"),"safe");bool rejected=false;try{SetupCore.Install(unknown,integration,false);}catch(IOException){rejected=true;}check(rejected&&File.Exists(Path.Combine(unknown.Root,"other.txt")),"Installer refuses to overwrite an unmanaged folder");rejected=false;try{SetupCore.Uninstall(unknown,integration);}catch(IOException){rejected=true;}check(rejected&&File.Exists(Path.Combine(unknown.Root,"other.txt")),"Uninstaller refuses unowned directories");
            string shortcut=Path.Combine(folder,"actual-shortcut.lnk");var windows=new WindowsIntegration();windows.AddShortcut(shortcut,Application.ExecutablePath);check(WindowsIntegration.ShortcutTarget(shortcut)==Application.ExecutablePath,"Windows shortcut creation and target readback work");windows.RemoveShortcut(shortcut,Application.ExecutablePath);check(!File.Exists(shortcut),"Shortcut removal checks target before deleting");
            using(var preview=new SetupWindow()){preview.Show();Application.DoEvents();using(var bitmap=new Bitmap(preview.Width,preview.Height)){preview.DrawToBitmap(bitmap,new Rectangle(Point.Empty,bitmap.Size));bitmap.Save(Path.Combine(folder,"installer-preview.png"));}preview.Hide();}
            report.Add("All installer checks passed. Tests used isolated folders and simulated registry integration; no real installation, startup changes, or app launch were performed.");
        }catch(Exception error){report.Add(error.ToString());Environment.ExitCode=1;}File.WriteAllLines(Path.Combine(folder,"test-results.txt"),report);
    }
    static byte[] AppPayloadForTest(){return SetupCore.AppPayload();}
}
}
