using System;using System.Collections.Generic;using System.Drawing;using System.IO;using System.Linq;using System.Windows.Forms;
namespace ShotOut {
partial class SettingsDialog {
TextBox projectFolderEdit;
void AddProjectFolderSetting(FlowLayoutPanel body){AddTitle(body,"Project save folder");var row=new FlowLayoutPanel {Width=550,Height=42,WrapContents=false};projectFolderEdit=new ThemedTextBox {Text=Value.ProjectFolder,Width=425,Height=32,BackColor=Color.FromArgb(43,46,54),ForeColor=ForeColor,BorderStyle=BorderStyle.FixedSingle};var browse=Button("Browse…",105);browse.Click+=delegate{using(var dialog=new FolderBrowserDialog {Description="Choose where ShotOut projects are saved",SelectedPath=projectFolderEdit.Text})if(dialog.ShowDialog(this)==DialogResult.OK)projectFolderEdit.Text=dialog.SelectedPath;};row.Controls.Add(projectFolderEdit);row.Controls.Add(browse);body.Controls.Add(row);}
}
partial class Editor {
string ProjectDirectory(bool create){string folder=Preferences.Current.ProjectFolder;if(String.IsNullOrWhiteSpace(folder))folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"ShotOut","Projects");folder=Path.GetFullPath(folder);if(create)Directory.CreateDirectory(folder);return folder;}
string LibraryHome(){return projectLibrary?ProjectDirectory(false):ScreenshotDirectory(false);}
}
static class SavedFileCatalog {
// Bound filename enumeration, skip junctions, and never decode unopened projects or images.
internal static IEnumerable<string> Enumerate(string[] roots,int limit,Func<bool> cancelled=null){var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var folders=new Stack<string>(roots.Where(path=>!String.IsNullOrWhiteSpace(path)));int count=0,visited=0;while(folders.Count>0&&count<limit&&visited++<4000&&(cancelled==null||!cancelled())){string folder=folders.Pop();if(!seen.Add(folder)||!Directory.Exists(folder))continue;string[] files;string[] children;try{files=Directory.EnumerateFiles(folder).Where(Editor.SearchFileSupported).Take(limit-count).ToArray();children=Directory.EnumerateDirectories(folder).Take(4000).ToArray();}catch(IOException){continue;}catch(UnauthorizedAccessException){continue;}foreach(var file in files){if(cancelled!=null&&cancelled())yield break;yield return file;if(++count>=limit)yield break;}foreach(var child in children)try{if((File.GetAttributes(child)&FileAttributes.ReparsePoint)==0)folders.Push(child);}catch(IOException){}catch(UnauthorizedAccessException){}}}
}
}