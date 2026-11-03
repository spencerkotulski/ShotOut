using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
namespace ShotOut { partial class Editor {
    // Injectable boundaries allow file-task tests without opening external apps or dialogs.
    internal Func<string,string> AfterCaptureSavePicker;
    internal Action<string,bool> AfterCaptureLauncher;
    internal Action<string> AfterCapturePathWriter;
    string PickAfterCapturePath(string suggested){
        if(AfterCaptureSavePicker!=null)return AfterCaptureSavePicker(suggested);
        using(var dialog=new SaveFileDialog {Title="Save captured screenshot as",InitialDirectory=Path.GetDirectoryName(suggested),FileName=Path.GetFileName(suggested),Filter="PNG image|*.png|JPEG image|*.jpg|Bitmap|*.bmp",FilterIndex=Preferences.Current.ScreenshotFormat=="JPEG"?2:Preferences.Current.ScreenshotFormat=="BMP"?3:1,AddExtension=true,OverwritePrompt=true})
            return dialog.ShowDialog()==DialogResult.OK?dialog.FileName:null;
    }
    void LaunchAfterCaptureFile(string path,bool folder){
        if(AfterCaptureLauncher!=null){AfterCaptureLauncher(path,folder);return;}
        if(folder)Process.Start(new ProcessStartInfo("explorer.exe","/select,\""+path+"\""){UseShellExecute=true});
        else Process.Start(new ProcessStartInfo(path){UseShellExecute=true});
    }
    void CopyCapturedFilePath(string path){
        if(AfterCapturePathWriter!=null){AfterCapturePathWriter(path);return;}
        var data=new DataObject();data.SetText(Path.GetFullPath(path));
        using(var image=Art.Render(Background,Marks)){if(Preferences.Current.AutoCopyCapture)data.SetData(DataFormats.Bitmap,true,image);Clipboard.SetDataObject(data,true,10,80);}
    }
    string RunAfterCaptureTasks(){
        var p=Preferences.Current;
        if(!p.AfterCaptureSave&&!p.AfterCaptureSaveAs&&!p.AfterCaptureOpenFolder&&!p.AfterCaptureOpenViewer&&!p.AfterCaptureCopyPath)return "";
        string path;
        try{
            path=NewScreenshotPath();
            if(p.AfterCaptureSaveAs){path=PickAfterCapturePath(path);if(String.IsNullOrEmpty(path))return "Save as cancelled; capture remains in ShotOut.";}
            WriteCurrentImage(path,p.CaptureJpegQuality);
        }catch(Exception ex){return "Could not save screenshot: "+ex.Message+" Capture remains in ShotOut.";}
        var messages=new List<string>{"Saved screenshot."};
        if(p.AfterCaptureOpenFolder)try{LaunchAfterCaptureFile(path,true);}catch(Exception ex){messages.Add("Could not open containing folder: "+ex.Message);}
        if(p.AfterCaptureOpenViewer)try{LaunchAfterCaptureFile(path,false);}catch(Exception ex){messages.Add("Could not open photo viewer: "+ex.Message);}
        if(p.AfterCaptureCopyPath)try{CopyCapturedFilePath(path);}catch(Exception ex){messages.Add("Could not copy file path: "+ex.Message);}
        return String.Join(" ",messages.ToArray());
    }
}}
