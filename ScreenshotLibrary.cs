using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace ShotOut {
partial class SettingsDialog {
    TextBox screenshotFolderEdit;
    void AddScreenshotFolderSetting(FlowLayoutPanel body){AddTitle(body,"Screenshot save folder");var row=new FlowLayoutPanel {Width=550,Height=42,WrapContents=false};screenshotFolderEdit=new TextBox {Text=Value.ScreenshotFolder,Width=425,Height=32,BackColor=Color.FromArgb(43,46,54),ForeColor=ForeColor,BorderStyle=BorderStyle.FixedSingle};var browse=Button("Browse…",105);browse.Click+=delegate{using(var dialog=new FolderBrowserDialog {Description="Choose where screenshots are saved",SelectedPath=screenshotFolderEdit.Text})if(dialog.ShowDialog(this)==DialogResult.OK)screenshotFolderEdit.Text=dialog.SelectedPath;};row.Controls.Add(screenshotFolderEdit);row.Controls.Add(browse);body.Controls.Add(row);}
}
partial class Editor {
    ListView screenshotFiles;Label screenshotPath;
    ImageList screenshotThumbnails;
    readonly object thumbnailGate=new object();
    readonly HashSet<Bitmap> pendingThumbnails=new HashSet<Bitmap>();
    string screenshotBrowsing;
    int screenshotPage;volatile int screenshotGeneration;
    static readonly string[] screenshotExtensions={".png",".jpg",".jpeg",".bmp",".gif",".tif",".tiff",".shotout",".snapcanvas"};
    string ScreenshotDirectory(bool create){string folder=Preferences.Current.ScreenshotFolder;if(String.IsNullOrWhiteSpace(folder))folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),"Screenshots");folder=Path.GetFullPath(folder);if(create)Directory.CreateDirectory(folder);return folder;}
    static IEnumerable<string> ReadScreenshotEntries(string folder,int page){if(!Directory.Exists(folder))return Enumerable.Empty<string>();return Directory.EnumerateDirectories(folder).Concat(Directory.EnumerateFiles(folder).Where(file=>screenshotExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))).Skip(Math.Max(0,page)*200).Take(201).ToArray();}
    void InstallScreenshotBrowser(FlowLayoutPanel card){
        var header=new Panel {Dock=DockStyle.Top,Height=280};card.Height=280;header.Controls.Add(card);header.Resize+=delegate{card.Location=new Point(Math.Max(0,(header.Width-card.Width)/2),0);};
        var library=new Panel {Dock=DockStyle.Fill,Padding=new Padding(24,4,24,12)};var toolbar=new FlowLayoutPanel {Dock=DockStyle.Top,Height=42,WrapContents=false};toolbar.Controls.Add(ActionButton("Up",null,"Parent folder",delegate{var parent=Directory.GetParent(screenshotBrowsing??ScreenshotDirectory(false));if(parent!=null){screenshotBrowsing=parent.FullName;screenshotPage=0;RefreshScreenshotBrowser();}},46));toolbar.Controls.Add(ActionButton("Refresh",null,"Refresh screenshot folder",delegate{RefreshScreenshotBrowser();},72));toolbar.Controls.Add(ActionButton("Previous",null,"Previous files",delegate{if(screenshotPage>0){screenshotPage--;RefreshScreenshotBrowser();}},78));toolbar.Controls.Add(ActionButton("Next",null,"Next files",delegate{screenshotPage++;RefreshScreenshotBrowser();},54));screenshotPath=new Label {Width=520,Height=36,ForeColor=muted,TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true};toolbar.Controls.Add(screenshotPath);
        screenshotFiles=new ListView {Dock=DockStyle.Fill,View=System.Windows.Forms.View.LargeIcon,Activation=ItemActivation.OneClick,BackColor=View.BackColor,ForeColor=text,BorderStyle=BorderStyle.None,MultiSelect=false,HideSelection=false};screenshotFiles.ItemActivate+=delegate{if(screenshotFiles.SelectedItems.Count==0)return;string path=screenshotFiles.SelectedItems[0].Tag as string;if(path!=null)OpenScreenshotEntry(path);};
        screenshotThumbnails=new ImageList {ImageSize=new Size(144,100),ColorDepth=ColorDepth.Depth32Bit};screenshotFiles.LargeImageList=screenshotThumbnails;var thumbnailHandle=screenshotThumbnails.Handle;
        Disposed+=delegate{lock(thumbnailGate){foreach(var image in pendingThumbnails)image.Dispose();pendingThumbnails.Clear();}screenshotThumbnails.Dispose();};
        toolbar.ClientSizeChanged+=delegate{screenshotPath.Width=Math.Max(60,toolbar.ClientSize.Width-282);};library.Controls.Add(screenshotFiles);library.Controls.Add(toolbar);welcome.Controls.Add(library);welcome.Controls.Add(header);RefreshScreenshotBrowser(true);
    }
    void OpenScreenshotEntry(string path){if(Directory.Exists(path)){screenshotBrowsing=path;screenshotPage=0;RefreshScreenshotBrowser();}else Guard(delegate{OpenPath(path);});}
    void RefreshScreenshotBrowser(bool reset=false){
        if(screenshotFiles==null||IsDisposed)return;if(reset||String.IsNullOrEmpty(screenshotBrowsing)){screenshotBrowsing=ScreenshotDirectory(false);screenshotPage=0;}int generation=++screenshotGeneration;string folder=screenshotBrowsing;int page=screenshotPage;screenshotPath.Text=folder;screenshotFiles.Items.Clear();screenshotThumbnails.Images.Clear();screenshotFiles.Items.Add("Loading…");
        if(Preferences.Testing){var entries=ReadScreenshotEntries(folder,page).ToArray();ApplyScreenshotEntries(folder,entries,generation,null);foreach(var path in entries.Take(200))ApplyScreenshotThumbnail(path,CreateScreenshotThumbnail(path),generation);return;}
        Task.Factory.StartNew(delegate{string[] entries=null;string error=null;try{entries=ReadScreenshotEntries(folder,page).ToArray();}catch(Exception ex){error=ex.Message;}try{if(IsHandleCreated&&!IsDisposed)BeginInvoke((Action)delegate{ApplyScreenshotEntries(folder,entries,generation,error);LoadScreenshotThumbnails(entries,generation);});}catch(InvalidOperationException){};});
    }
    void ApplyScreenshotEntries(string folder,string[] entries,int generation,string error){if(IsDisposed||screenshotFiles==null||screenshotFiles.IsDisposed||generation!=screenshotGeneration)return;screenshotFiles.BeginUpdate();try{screenshotFiles.Items.Clear();if(error!=null){screenshotFiles.Items.Add("Unable to open folder: "+error);return;}if(entries==null||entries.Length==0){screenshotFiles.Items.Add("No screenshots here yet. Capture, paste, or open an image above.");return;}foreach(string path in entries.Take(200)){bool directory=Directory.Exists(path);screenshotFiles.Items.Add(new ListViewItem(Path.GetFileName(path)){Tag=path});}if(entries.Length>200)screenshotFiles.Items.Add("More files available — click Next");}finally{screenshotFiles.EndUpdate();}}
    static Bitmap CreateScreenshotThumbnail(string path){
        var thumb=new Bitmap(144,100);using(var graphics=Graphics.FromImage(thumb)){
            graphics.Clear(Color.FromArgb(32,35,42));graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            if(Directory.Exists(path)){using(var brush=new SolidBrush(Color.FromArgb(104,151,225))){graphics.FillRectangle(brush,24,22,38,16);graphics.FillRectangle(brush,20,32,104,53);}}
            else try{using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete))using(var image=Image.FromStream(stream)){float scale=Math.Min(136f/image.Width,92f/image.Height);int width=Math.Max(1,(int)(image.Width*scale)),height=Math.Max(1,(int)(image.Height*scale));graphics.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;graphics.DrawImage(image,(144-width)/2,(100-height)/2,width,height);}}
            catch{using(var pen=new Pen(Color.FromArgb(130,160,210),3)){graphics.DrawRectangle(pen,48,16,48,68);graphics.DrawLine(pen,58,39,86,39);graphics.DrawLine(pen,58,51,86,51);}}
        }return thumb;
    }
    void LoadScreenshotThumbnails(string[] entries,int generation){
        if(entries==null||generation!=screenshotGeneration||IsDisposed)return;
        Task.Factory.StartNew(delegate{foreach(var path in entries.Take(200)){
            if(generation!=screenshotGeneration||IsDisposed)return;var image=CreateScreenshotThumbnail(path);
            lock(thumbnailGate){if(IsDisposed||generation!=screenshotGeneration){image.Dispose();return;}pendingThumbnails.Add(image);}
            try{if(!IsHandleCreated)throw new InvalidOperationException();BeginInvoke((Action)delegate{lock(thumbnailGate){if(!pendingThumbnails.Remove(image))return;}ApplyScreenshotThumbnail(path,image,generation);});}
            catch(InvalidOperationException){lock(thumbnailGate){if(pendingThumbnails.Remove(image))image.Dispose();}return;}
        }});
    }
    void ApplyScreenshotThumbnail(string path,Bitmap image,int generation){
        using(image){if(IsDisposed||screenshotFiles.IsDisposed||generation!=screenshotGeneration)return;var item=screenshotFiles.Items.Cast<ListViewItem>().FirstOrDefault(entry=>entry.Tag as string==path);if(item==null)return;int index=screenshotThumbnails.Images.Count;screenshotThumbnails.Images.Add(image);item.ImageIndex=index;}
    }

}
}
