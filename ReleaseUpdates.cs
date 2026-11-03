using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
namespace ShotOut {
static class ReleaseUpdates {
    internal const string Releases="https://github.com/spencerkotulski/ShotOut/releases";
    internal static Version CurrentVersion {get{return typeof(Editor).Assembly.GetName().Version;}}
    internal const string Api="https://api.github.com/repos/spencerkotulski/ShotOut/releases/latest";
    internal sealed class Release {internal Version Version;internal string Url;}
    internal static Release Parse(string json,Version current) {
        var data=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(json);
        if(data==null||!data.ContainsKey("draft")||!data.ContainsKey("prerelease")||(bool)data["draft"]||(bool)data["prerelease"])return null;
        Version version;object tag,url;
        if(!data.TryGetValue("tag_name",out tag)||!Version.TryParse(Convert.ToString(tag).TrimStart('v','V'),out version))return null;
        var normalized=new Version(version.Major,version.Minor,Math.Max(0,version.Build),Math.Max(0,version.Revision));
        if(normalized<=current)return null;
        Uri uri;if(!data.TryGetValue("html_url",out url)||!Uri.TryCreate(Convert.ToString(url),UriKind.Absolute,out uri)||uri.Scheme!="https"||!String.Equals(uri.Host,"github.com",StringComparison.OrdinalIgnoreCase)||!uri.AbsolutePath.StartsWith("/spencerkotulski/ShotOut/releases/tag/",StringComparison.Ordinal)||!String.IsNullOrEmpty(uri.UserInfo)||!uri.IsDefaultPort)throw new InvalidDataException("The update link was not an official ShotOut release.");
        return new Release {Version=version,Url=uri.AbsoluteUri};
    }
    internal static Release Check() {
        ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;
        var request=(HttpWebRequest)WebRequest.Create(Api);request.UserAgent="ShotOut/1.4.3";request.Accept="application/vnd.github+json";request.Headers["X-GitHub-Api-Version"]="2022-11-28";request.Timeout=12000;request.ReadWriteTimeout=12000;request.AllowAutoRedirect=false;
        request.ProtocolVersion=HttpVersion.Version11;
        using(var response=request.GetResponse())using(var reader=new StreamReader(response.GetResponseStream())){var chars=new char[1048577];int total=0,n;while(total<chars.Length&&(n=reader.Read(chars,total,chars.Length-total))>0)total+=n;if(total>1048576)throw new InvalidDataException("Update response was too large.");return Parse(new String(chars,0,total),CurrentVersion);}
    }
    internal static void Open(string url){Process.Start(new ProcessStartInfo(url){UseShellExecute=true});}
}
partial class SettingsDialog {
    internal bool VerifyUpdateSettingsPlacement(){return settingsPages.ContainsKey("Experimental")&&settingsPages.ContainsKey("Updates")&&checks["printScreen"].Parent==settingsPages["Experimental"]&&checks["updates"].Parent==settingsPages["Updates"];}
    void AddUpdateOptions(FlowLayoutPanel page) {
        Toggle(page,"updates","Automatically check for updates",Value.CheckForUpdates);
        SettingsHelp(page,"Checks GitHub when ShotOut opens and once a day while running. GitHub receives a normal web request; no screenshots, files, or account token are sent. Updates are installed only when you choose to download and run the installer.",100);
        var check=Button("Check now",150);page.Controls.Add(check);var label=new Label {Width=550,Height=64,ForeColor=ForeColor,Text="Current version: 1.4.3"};page.Controls.Add(label);
        check.Click+=async delegate {check.Enabled=false;label.Text="Checking GitHub…";try {var release=await Task.Run(()=>ReleaseUpdates.Check());if(IsDisposed)return;label.Text=release==null?"You're up to date (1.4.3).":"New update available: "+release.Version;if(release!=null){var open=Button("View update",160);open.Click+=delegate{try{ReleaseUpdates.Open(release.Url);}catch(Exception e){label.Text=e.Message;}};page.Controls.Add(open);}}catch(Exception){if(!IsDisposed)label.Text="Couldn't check for updates. Check your connection and try again. You can also visit GitHub releases.";}finally{if(!check.IsDisposed)check.Enabled=true;}};
        var browse=Button("GitHub releases",180);browse.Click+=delegate{try{ReleaseUpdates.Open(ReleaseUpdates.Releases);}catch(Exception e){label.Text=e.Message;}};page.Controls.Add(browse);
    }
}
partial class Editor {
    bool checkingRelease;System.Windows.Forms.Timer releaseTimer;Panel releaseBanner;
    void InitializeReleaseUpdates(){if(Preferences.Testing||releaseTimer!=null)return;releaseTimer=new System.Windows.Forms.Timer {Interval=86400000};releaseTimer.Tick+=delegate{CheckReleaseInBackground();};releaseTimer.Start();Disposed+=delegate{releaseTimer.Dispose();};CheckReleaseInBackground();}
    async void CheckReleaseInBackground(){if(checkingRelease||!Preferences.Current.CheckForUpdates)return;checkingRelease=true;try{var release=await Task.Run(()=>ReleaseUpdates.Check());if(!IsDisposed&&release!=null)ShowReleaseBanner(release);}catch(Exception){/* Offline and rate-limited checks stay quiet; manual checks explain failures. */}finally{checkingRelease=false;}}
    void ShowReleaseBanner(ReleaseUpdates.Release release){if(releaseBanner!=null){releaseBanner.Dispose();}releaseBanner=new Panel {Dock=DockStyle.Top,Height=44,BackColor=Color.FromArgb(32,68,104),AccessibleName="Update available"};var close=new Button {Text="×",Dock=DockStyle.Right,Width=40,FlatStyle=FlatStyle.Flat,ForeColor=Color.White};close.Click+=delegate{releaseBanner.Visible=false;};var link=new LinkLabel {Text="New update available · ShotOut "+release.Version+" — View update",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,Padding=new Padding(14,0,0,0),LinkColor=Color.White,ActiveLinkColor=Color.White,VisitedLinkColor=Color.White};link.LinkClicked+=delegate{Guard(delegate{ReleaseUpdates.Open(release.Url);});};releaseBanner.Controls.Add(link);releaseBanner.Controls.Add(close);Controls.Add(releaseBanner);releaseBanner.BringToFront();}
    public void VerifyReleaseUpdates(List<string> report,string folder){
        Func<string,string,string> json=(v,url)=>"{\"draft\":false,\"prerelease\":false,\"tag_name\":\""+v+"\",\"html_url\":\""+url+"\"}";
        string official=ReleaseUpdates.Releases+"/tag/v1.5.0";var current=new Version(1,4,3,0);
        if(ReleaseUpdates.Parse(json("v1.5.0",official),current)==null||ReleaseUpdates.Parse(json("v1.4.3",official),current)!=null||ReleaseUpdates.Parse(json("v1.4.1",official),current)!=null||ReleaseUpdates.Parse(json("v1.5.0-beta",official),current)!=null||ReleaseUpdates.Parse(json("v1.5.0",official).Replace("\"prerelease\":false","\"prerelease\":true"),current)!=null)throw new Exception("Version selection failed");
        bool rejected=false;try{ReleaseUpdates.Parse(json("v1.5.0","https://example.com/bad"),current);}catch(InvalidDataException){rejected=true;}if(!rejected)throw new Exception("Unofficial URL accepted");

        using(var settings=new SettingsDialog(new Preferences(),false)){if(!settings.VerifyUpdateSettingsPlacement())throw new Exception("Settings placement failed");}
        ShowReleaseBanner(new ReleaseUpdates.Release {Version=new Version(1,5,0),Url=official});if(releaseBanner==null||releaseBanner.Height<40)throw new Exception("Banner missing");releaseBanner.Dispose();releaseBanner=null;
        report.Add("PASS Experimental/Updates settings placement; update versions: newer/equal/older/preview; official URL validation; top banner");
    }
}
}




