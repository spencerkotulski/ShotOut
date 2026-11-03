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
    void AddScreenshotFolderSetting(FlowLayoutPanel body){AddTitle(body,"Screenshot save folder");var row=new ResizeBufferedFlowPanel {Width=550,Height=42,WrapContents=false};screenshotFolderEdit=new ThemedTextBox {Text=Value.ScreenshotFolder,Width=425,Height=32,BackColor=Color.FromArgb(43,46,54),ForeColor=ForeColor,BorderStyle=BorderStyle.FixedSingle};var browse=Button("Browse…",105);browse.Click+=delegate{using(var dialog=new FolderBrowserDialog {Description="Choose where screenshots are saved",SelectedPath=screenshotFolderEdit.Text})if(dialog.ShowDialog(this)==DialogResult.OK)screenshotFolderEdit.Text=dialog.SelectedPath;};row.Controls.Add(screenshotFolderEdit);row.Controls.Add(browse);body.Controls.Add(row);}
}
partial class Editor {
    ScreenshotListView screenshotFiles;Label screenshotPath;FlowLayoutPanel screenshotPagination;ModernButton screenshotPrevious,screenshotNext,screenshotEdit;Label screenshotPageLabel;
    ImageList screenshotThumbnails;
    readonly object thumbnailGate=new object();
    readonly HashSet<Bitmap> pendingThumbnails=new HashSet<Bitmap>();
    string screenshotBrowsing,tabGalleryHome;
    void EnsureTabGallery(){if(tabGalleryHome!=LibraryHome())RefreshScreenshotBrowser(true);}
    int screenshotPage;volatile int screenshotGeneration;
    static readonly string[] screenshotExtensions={".png",".jpg",".jpeg",".bmp",".gif",".tif",".tiff",".shotout",".snapcanvas"};
    string ScreenshotRootDirectory(bool create){string folder=Preferences.Current.ScreenshotFolder;if(String.IsNullOrWhiteSpace(folder))folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),"Screenshots");folder=Path.GetFullPath(folder);if(create)Directory.CreateDirectory(folder);return folder;}
    string ScreenshotDirectory(bool create){return ScreenshotMonthDirectory(DateTime.UtcNow,create);}
    string ScreenshotMonthDirectory(DateTime capturedUtc,bool create){string root=ScreenshotRootDirectory(create);if(!Preferences.Current.MonthlyScreenshots)return root;string folder=Path.Combine(root,capturedUtc.ToLocalTime().ToString("yyyy-MM",System.Globalization.CultureInfo.InvariantCulture));if(create){Directory.CreateDirectory(folder);return folder;}return Directory.Exists(folder)?folder:root;}
    static IEnumerable<string> ReadScreenshotEntries(string folder,int page){if(!Directory.Exists(folder))return Enumerable.Empty<string>();return Directory.EnumerateDirectories(folder).Concat(Directory.EnumerateFiles(folder).Where(file=>screenshotExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))).Skip(Math.Max(0,page)*200).Take(201).ToArray();}
    void InstallScreenshotBrowser(FlowLayoutPanel card){
        var header=new ResizeBufferedPanel {Dock=DockStyle.Top,Height=280};card.Height=280;header.Controls.Add(card);header.Resize+=delegate{card.Location=new Point(Math.Max(0,(header.Width-card.Width)/2),0);};
        var library=new ResizeBufferedPanel {Dock=DockStyle.Fill,Padding=new Padding(24,4,24,12)};var toolbar=new ResizeBufferedFlowPanel {Dock=DockStyle.Top,Height=42,WrapContents=false};
        toolbar.Controls.Add(ActionButton("","Up","Up to parent folder (Alt+Up)",BrowseScreenshotParent,36));toolbar.Controls.Add(ActionButton("","Refresh","Refresh folder (F5)",delegate{RefreshScreenshotBrowser();},36));screenshotEdit=ActionButton("Edit image","Pen","Edit selected image (E)",delegate{OpenSelectedScreenshots(false);},125);screenshotEdit.Enabled=false;toolbar.Controls.Add(screenshotEdit);screenshotPath=new Label {Width=520,Height=36,ForeColor=muted,TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true};toolbar.Controls.Add(screenshotPath);
        screenshotPagination=new ResizeBufferedFlowPanel {Dock=DockStyle.Bottom,Height=40,WrapContents=false,Visible=false};screenshotPrevious=ActionButton("","ChevronLeft","Previous page",delegate{if(screenshotPage>0){screenshotPage--;RefreshScreenshotBrowser();}},36);screenshotNext=ActionButton("","ChevronRight","Next page",delegate{screenshotPage++;RefreshScreenshotBrowser();},36);screenshotPageLabel=new Label {Width=180,Height=34,ForeColor=muted,TextAlign=ContentAlignment.MiddleLeft};screenshotPagination.Controls.Add(screenshotPrevious);screenshotPagination.Controls.Add(screenshotNext);screenshotPagination.Controls.Add(screenshotPageLabel);
        screenshotFiles=new ScreenshotListView {Dock=DockStyle.Fill,View=System.Windows.Forms.View.LargeIcon,Activation=ItemActivation.Standard,BackColor=View.BackColor,ForeColor=text,BorderStyle=BorderStyle.None,MultiSelect=true,HideSelection=false,ShowItemToolTips=true};screenshotFiles.SelectedIndexChanged+=delegate{var selected=screenshotFiles.SelectedItems.Cast<ListViewItem>().Select(item=>item.Tag as string).Where(path=>path!=null).ToArray();screenshotEdit.Enabled=selected.Length>0&&selected.All(File.Exists);screenshotEdit.Text=selected.Length>0&&selected.All(IsProjectPath)?"Edit project":"Edit image";};screenshotFiles.ItemActivate+=delegate{Guard(delegate{OpenSelectedScreenshots(false);});};InstallScreenshotOpenMenu();
        screenshotThumbnails=new ImageList {ImageSize=new Size(144,100),ColorDepth=ColorDepth.Depth32Bit};screenshotFiles.LargeImageList=screenshotThumbnails;var thumbnailHandle=screenshotThumbnails.Handle;
        Disposed+=delegate{lock(thumbnailGate){foreach(var image in pendingThumbnails)image.Dispose();pendingThumbnails.Clear();}screenshotThumbnails.Dispose();};
        toolbar.ClientSizeChanged+=delegate{screenshotPath.Width=Math.Max(60,toolbar.ClientSize.Width-222);};library.Controls.Add(screenshotFiles);library.Controls.Add(screenshotPagination);library.Controls.Add(toolbar);InstallLibraryTabs(library);welcome.Controls.Add(library);welcome.Controls.Add(header);RefreshScreenshotBrowser(true);
    }
    void BrowseScreenshotParent(){var parent=Directory.GetParent(screenshotBrowsing??LibraryHome());if(parent==null)return;screenshotBrowsing=parent.FullName;screenshotPage=0;RefreshScreenshotBrowser();}
    void OpenScreenshotEntry(string path){if(Directory.Exists(path)){screenshotBrowsing=path;screenshotPage=0;RefreshScreenshotBrowser();}else Guard(delegate{OpenPath(path);});}
    void RefreshScreenshotBrowser(bool reset=false){
        if(screenshotFiles==null||IsDisposed)return;if(reset||String.IsNullOrEmpty(screenshotBrowsing)){screenshotBrowsing=LibraryHome();tabGalleryHome=screenshotBrowsing;screenshotPage=0;}int generation=++screenshotGeneration;string folder=screenshotBrowsing;int page=screenshotPage;bool projects=projectLibrary;string sorting=librarySort;var recent=ProjectHistory.Current.Items.ToArray();screenshotPath.Text=(projects?"Recent projects + ":"")+folder;if(screenshotFiles.Items.Count==0)screenshotFiles.Items.Add("Loading…");
        if(Preferences.Testing){var entries=ReadLibraryEntries(folder,page,projects,sorting,recent);ApplyScreenshotEntries(folder,entries,generation,null);foreach(var path in entries.Take(200))ApplyScreenshotThumbnail(path,CreateScreenshotThumbnail(path),generation);return;}
        Task.Factory.StartNew(delegate{string[] entries=null;string error=null;try{entries=ReadLibraryEntries(folder,page,projects,sorting,recent);}catch(Exception ex){error=ex.Message;}try{if(IsHandleCreated&&!IsDisposed)BeginInvoke((Action)delegate{ApplyScreenshotEntries(folder,entries,generation,error);LoadScreenshotThumbnails(entries,generation);});}catch(InvalidOperationException){};});
    }
    void ApplyScreenshotEntries(string folder,string[] entries,int generation,string error){if(IsDisposed||screenshotFiles==null||screenshotFiles.IsDisposed||generation!=screenshotGeneration)return;if(error==null&&entries!=null&&entries.Length>0&&screenshotFiles.Items.Cast<ListViewItem>().Select(item=>item.Tag as string).SequenceEqual(entries.Take(200))){screenshotPagination.Visible=screenshotPage>0||entries.Length>200;screenshotPrevious.Enabled=screenshotPage>0;screenshotNext.Enabled=entries.Length>200;screenshotPageLabel.Text="Page "+(screenshotPage+1);return;}screenshotFiles.BeginUpdate();try{screenshotThumbnails.Images.Clear();screenshotEdit.Enabled=false;screenshotEdit.Text="Edit image";screenshotFiles.Items.Clear();screenshotPagination.Visible=screenshotPage>0||(entries!=null&&entries.Length>200);screenshotPrevious.Enabled=screenshotPage>0;screenshotNext.Enabled=entries!=null&&entries.Length>200;screenshotPageLabel.Text="Page "+(screenshotPage+1);if(error!=null){screenshotFiles.Items.Add("Unable to open folder: "+error);return;}if(entries==null||entries.Length==0){screenshotFiles.Items.Add("No matching files here yet. Open an image or project, or capture above.");return;}foreach(string path in entries.Take(200)){int index=screenshotThumbnails.Images.Count;using(var placeholder=new Bitmap(144,100))screenshotThumbnails.Images.Add(placeholder);screenshotFiles.Items.Add(new ListViewItem(Path.GetFileName(path)){ImageIndex=index,Tag=path,ToolTipText=path});}}finally{screenshotFiles.EndUpdate();screenshotFiles.ReflowGallery();if(screenshotFiles.Items.Count>0)screenshotFiles.EnsureVisible(0);}}
    public void VerifyPaginationRefresh(List<string> report,string folder){SetWelcome(true);Show();Application.DoEvents();screenshotPage=0;int gen=++screenshotGeneration;var entries=Enumerable.Range(0,201).Select(i=>"item"+i).ToArray();ApplyScreenshotEntries(folder,entries.Take(200).ToArray(),gen,null);if(screenshotNext.Enabled)throw new Exception("Unexpected next page");ApplyScreenshotEntries(folder,entries,gen,null);if(!screenshotNext.Enabled||!screenshotPagination.Visible)throw new Exception("Missing next page after adding item 201");ApplyScreenshotEntries(folder,entries.Take(200).ToArray(),gen,null);if(screenshotNext.Enabled||screenshotPagination.Visible)throw new Exception("Next page remained after removing item 201");report.Add("PASS Gallery pagination updates when unchanged first page crosses 200 items in either direction");Hide();}
    static Bitmap CreateScreenshotThumbnail(string path){
        var thumb=new Bitmap(144,100);using(var graphics=Graphics.FromImage(thumb)){
            graphics.Clear(Color.FromArgb(32,35,42));graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            if(Directory.Exists(path)){using(var brush=new SolidBrush(Color.FromArgb(104,151,225))){graphics.FillRectangle(brush,24,22,38,16);graphics.FillRectangle(brush,20,32,104,53);}}
            else try{string source=path;if(IsProjectPath(path)){source=ProjectHistory.ThumbnailPath(path);if(source==null||!File.Exists(source))throw new InvalidDataException();}using(var stream=new FileStream(source,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete))using(var image=Image.FromStream(stream)){float scale=Math.Min(136f/image.Width,92f/image.Height);int width=Math.Max(1,(int)(image.Width*scale)),height=Math.Max(1,(int)(image.Height*scale));graphics.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;graphics.DrawImage(image,(144-width)/2,(100-height)/2,width,height);}}
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
        using(image){if(IsDisposed||screenshotFiles.IsDisposed||generation!=screenshotGeneration)return;var item=screenshotFiles.Items.Cast<ListViewItem>().FirstOrDefault(entry=>entry.Tag as string==path);if(item==null)return;screenshotThumbnails.Images[item.ImageIndex]=image;screenshotFiles.RedrawItems(item.Index,item.Index,false);}
    }

}
}



