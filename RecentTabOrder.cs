using System;using System.Collections.Generic;using System.Linq;using System.Windows.Forms;using System.Drawing;
namespace ShotOut {partial class Editor {
    long tabVisitSerial;List<EditorDocument> recentTabCycle;bool cyclingRecentTabs;
    void RecordTabVisit(){if(cyclingRecentTabs)return;FinishRecentTabCycle();if(currentDocument>=0&&currentDocument<documents.Count)documents[currentDocument].VisitOrder=++tabVisitSerial;}
    void FinishRecentTabCycle(){if(recentTabCycle==null)return;recentTabCycle=null;if(currentDocument>=0&&currentDocument<documents.Count)documents[currentDocument].VisitOrder=++tabVisitSerial;}
    void CycleRecentTab(int direction){
        if(documents.Count<2){FinishRecentTabCycle();return;}
        var active=documents[currentDocument];
        if(recentTabCycle==null){recentTabCycle=new List<EditorDocument>{active};recentTabCycle.AddRange(documents.Where(d=>d!=active).OrderByDescending(d=>d.VisitOrder));}
        recentTabCycle.RemoveAll(d=>!documents.Contains(d));int at=recentTabCycle.IndexOf(active);if(at<0){FinishRecentTabCycle();CycleRecentTab(direction);return;}
        var target=recentTabCycle[(at+direction+recentTabCycle.Count)%recentTabCycle.Count];
        cyclingRecentTabs=true;try{ActivateDocument(documents.IndexOf(target));}finally{cyclingRecentTabs=false;}
    }
    protected override void OnKeyUp(KeyEventArgs e){if(e.KeyCode==Keys.ControlKey||e.KeyCode==Keys.LControlKey||e.KeyCode==Keys.RControlKey)FinishRecentTabCycle();base.OnKeyUp(e);}
    protected override void OnDeactivate(EventArgs e){FinishRecentTabCycle();base.OnDeactivate(e);}
    public void VerifyRecentTabOrder(List<string> report,string folder){
        bool old=Preferences.Current.RecentTabOrder;try{
            while(DocumentCount>1)CloseDocumentCore(DocumentCount-1,false);LoadBitmap(Blank(120,80),"First");int first=currentDocument;AddWelcomeDocument();var second=documents[currentDocument];AddWelcomeDocument();var third=documents[currentDocument];ActivateDocument(first);ActivateDocument(documents.IndexOf(second));ActivateDocument(documents.IndexOf(third));
            Preferences.Current.RecentTabOrder=true;CycleDocument(1);if(documents[currentDocument]!=second)throw new Exception("MRU did not select previous tab");OnKeyUp(new KeyEventArgs(Keys.ControlKey));CycleDocument(1);if(documents[currentDocument]!=third)throw new Exception("Repeated taps did not toggle back");FinishRecentTabCycle();
            CycleDocument(1);CycleDocument(1);if(currentDocument!=first)throw new Exception("Held Ctrl did not reach second previous tab");CycleDocument(-1);if(documents[currentDocument]!=second)throw new Exception("Reverse traversal changed order");FinishRecentTabCycle();
            ActivateDocument(first);CycleDocument(1);if(documents[currentDocument]!=second)throw new Exception("Mouse visit was not recorded");FinishRecentTabCycle();
            AddWelcomeDocument();var newest=documents[currentDocument];CycleDocument(1);if(documents[currentDocument]!=second)throw new Exception("New tab did not retain recent order");FinishRecentTabCycle();CloseDocumentCore(documents.IndexOf(newest),false);CycleDocument(1);if(!documents.Contains(documents[currentDocument]))throw new Exception("Closed tab remained in cycle");OnDeactivate(EventArgs.Empty);if(recentTabCycle!=null)throw new Exception("Deactivation did not finish cycle");
            Preferences.Current.RecentTabOrder=false;ActivateDocument(0);CycleDocument(1);if(currentDocument!=1)throw new Exception("Default strip order changed");
            var prefs=new Preferences{RecentTabOrder=true};var path=System.IO.Path.Combine(folder,"recent-tab-settings.xml");prefs.Save(path);if(!Preferences.Read(path).RecentTabOrder||!prefs.Clone().RecentTabOrder||new Preferences().RecentTabOrder)throw new Exception("Preference persistence/default failed");
            report.Add("PASS Recent-tab cycling: last-tab toggle, held-Ctrl traversal, Shift reverse, mouse/new/closed tabs, Ctrl release, deactivation, default strip order and preference persistence");
        }finally{FinishRecentTabCycle();Preferences.Current.RecentTabOrder=old;LoadBitmap(Blank(100,100),"Test");}
    }
}}

