using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
namespace ShotOut {
static class CaptureAudio {
    internal static readonly string[] Files={"film-camera","camera-shutter","camera-seven"};
    internal static readonly string[] Labels={"Film camera (Nikon F3)","Camera shutter","Camera 7"};
    static readonly object gate=new object();static readonly string alias="ShotOutCapture"+Process.GetCurrentProcess().Id;
    static int generation;static System.Windows.Forms.Timer cleanup;static Control dispatcher;static bool starting;
    static readonly List<Action> pending=new List<Action>();
    internal static Action<int,int> TestPlayback=null;internal static string LastError;internal static int NativeStarts;
    [DllImport("winmm.dll",CharSet=CharSet.Unicode)] static extern uint mciSendString(string command,StringBuilder result,int capacity,IntPtr callback);
    [DllImport("winmm.dll",CharSet=CharSet.Unicode)] static extern bool mciGetErrorString(uint error,StringBuilder result,int capacity);
    static CaptureAudio(){Application.ApplicationExit+=delegate{Stop();};}
    // Native MP3 playback needs an STA thread with a message loop. It is created only on demand.
    static void Queue(Action action){lock(gate){if(dispatcher!=null&&!dispatcher.IsDisposed){dispatcher.BeginInvoke(action);return;}pending.Add(action);if(starting)return;starting=true;var thread=new Thread(delegate(){try{using(var control=new Control()){var handle=control.Handle;lock(gate){dispatcher=control;starting=false;foreach(var queued in pending)control.BeginInvoke(queued);pending.Clear();}Application.Run();}}catch(Exception error){LastError=error.Message;}finally{lock(gate){dispatcher=null;starting=false;}if(cleanup!=null){cleanup.Dispose();cleanup=null;}mciSendString("close "+alias,null,0,IntPtr.Zero);}});thread.IsBackground=true;thread.Name="ShotOut capture audio";thread.SetApartmentState(ApartmentState.STA);thread.Start();}}
    static void ClosePlayer(){if(cleanup!=null){cleanup.Stop();cleanup.Dispose();cleanup=null;}mciSendString("close "+alias,null,0,IntPtr.Zero);}
    internal static byte[] ReadClip(int clip){clip=Math.Max(0,Math.Min(Files.Length-1,clip));using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("ShotOut.Audio."+Files[clip]))using(var output=new MemoryStream()){stream.CopyTo(output);return output.ToArray();}}
    static string ClipPath(int clip){byte[] bytes=ReadClip(clip);string digest;using(var hash=SHA256.Create())digest=BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();string root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ShotOut","Audio");Directory.CreateDirectory(root);string file=Path.Combine(root,Files[clip]+"-"+digest.Substring(0,16)+".mp3");bool valid=false;if(File.Exists(file)){using(var hash=SHA256.Create())valid=BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(file))).Replace("-","").ToLowerInvariant()==digest;}if(!valid)File.WriteAllBytes(file,bytes);return file;}
    static bool Command(string command){uint error=mciSendString(command,null,0,IntPtr.Zero);if(error==0)return true;var message=new StringBuilder(256);mciGetErrorString(error,message,message.Capacity);LastError=message.ToString();return false;}
    internal static void Completed(){var p=Preferences.Current;if(p.CaptureSoundEnabled)Play(p.CaptureSound,p.CaptureSoundVolume);}
    internal static void Play(int clip,int volume){
        clip=Math.Max(0,Math.Min(Files.Length-1,clip));volume=Math.Max(0,Math.Min(100,volume));
        if(Preferences.Testing){if(TestPlayback!=null)TestPlayback(clip,volume);return;}
        int request=Interlocked.Increment(ref generation),chosen=clip,level=volume;
        Queue(delegate {try{if(request!=generation)return;string file=ClipPath(chosen);LastError=null;ClosePlayer();
            if(!Command("open \""+file+"\" type mpegvideo alias "+alias))return;
            Command("set "+alias+" time format milliseconds");Command("setaudio "+alias+" volume to "+level*10);
            var length=new StringBuilder(32);uint result=mciSendString("status "+alias+" length",length,length.Capacity,IntPtr.Zero);int duration;if(result!=0||!Int32.TryParse(length.ToString(),out duration))duration=15000;
            if(!Command("play "+alias+" from 0")){mciSendString("close "+alias,null,0,IntPtr.Zero);return;}
            Interlocked.Increment(ref NativeStarts);cleanup=new System.Windows.Forms.Timer {Interval=Math.Max(250,Math.Min(60000,duration+150))};cleanup.Tick+=delegate{if(request==generation)ClosePlayer();};cleanup.Start();
        }catch(Exception error){LastError=error.Message;ClosePlayer();}});
    }
    internal static void Stop(){Interlocked.Increment(ref generation);lock(gate){if(dispatcher!=null&&!dispatcher.IsDisposed)dispatcher.BeginInvoke((Action)ClosePlayer);}}
}
partial class SettingsDialog {
    ChoiceButton captureSoundChoice;NumberField captureSoundVolume;
    void AddCaptureSoundSettings(FlowLayoutPanel page){
        AddTitle(page,"Sound options");
        var row=new FlowLayoutPanel {Width=550,Height=42,WrapContents=false};row.Controls.Add(new Label {Text="Sound",Width=65,Height=34,TextAlign=ContentAlignment.MiddleLeft});captureSoundChoice=new ChoiceButton {Width=285,Height=34,Margin=Padding.Empty,BackColor=Color.FromArgb(43,46,54),ForeColor=ForeColor,Font=Font,AccessibleName="Screenshot sound"};captureSoundChoice.Items.AddRange(CaptureAudio.Labels);captureSoundChoice.SelectedItem=CaptureAudio.Labels[Value.CaptureSound];row.Controls.Add(captureSoundChoice);
        var preview=Button("Preview",100);preview.Margin=new Padding(8,0,0,0);preview.Click+=delegate {CaptureAudio.Play(Array.IndexOf(CaptureAudio.Labels,captureSoundChoice.SelectedItem),(int)captureSoundVolume.Value);};row.Controls.Add(preview);page.Controls.Add(row);
        captureSoundVolume=NumberRow(page,"Sound volume · %",0,100,Value.CaptureSoundVolume);
        SettingsHelp(page,"Enable Play sound in the task list above to hear this after successful captures. Cancelling stays silent. Preview works even when Play sound is off.",64);
        FormClosed+=delegate{CaptureAudio.Stop();};
    }
}
}
