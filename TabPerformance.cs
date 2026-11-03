using System;using System.Collections.Generic;using System.Diagnostics;using System.Drawing;using System.Linq;using System.Windows.Forms;
namespace ShotOut {partial class Editor {
    void CacheDocumentPreview(EditorDocument document){
        if(Preview==null||previewPending||dragging||moving||resizing||inlineTarget!=null)return;
        if(document.CachedPreview!=null&&!Object.ReferenceEquals(document.CachedPreview,Preview))document.CachedPreview.Dispose();
        document.CachedPreview=Preview;
        long budget=128L*1024*1024,total=0;
        foreach(var d in documents.OrderByDescending(x=>x.LastActivityUtc))if(d.CachedPreview!=null){long bytes=(long)d.CachedPreview.Width*d.CachedPreview.Height*4;total+=bytes;if(total>budget&&!Object.ReferenceEquals(d.CachedPreview,Preview)){d.CachedPreview.Dispose();d.CachedPreview=null;}}
    }
    void ReleaseDocumentPreview(Bitmap old){if(old==null)return;
        if(currentDocument>=0&&currentDocument<documents.Count&&Object.ReferenceEquals(documents[currentDocument].CachedPreview,old))documents[currentDocument].CachedPreview=null;
        if(!documents.Any(d=>Object.ReferenceEquals(d.CachedPreview,old)))old.Dispose();
    }
    void RestoreDocumentPreview(EditorDocument document){
        var cached=document.CachedPreview;document.CachedPreview=null;
        if(cached!=null){var old=Preview;Preview=cached;if(!Object.ReferenceEquals(old,cached))ReleaseDocumentPreview(old);SyncLayers();UpdateStatus();View.Invalidate();}
        else if(document.Welcome){var old=Preview;Preview=null;ReleaseDocumentPreview(old);SyncLayers();UpdateStatus();View.Invalidate();}
        else RefreshImage();
    }
    public void VerifyTabPerformance(List<string> report,string folder){
        LoadBitmap(Blank(3840,2160),"Tab performance");SetWelcome(false);Marks.Add(new Mark {Kind="Rectangle",X=40,Y=40,W=200,H=120});RefreshImage();int imageTab=currentDocument;var expected=Preview;
        EnsureTabGallery();int generation=screenshotGeneration;var clock=Stopwatch.StartNew();AddWelcomeDocument();clock.Stop();report.Add("New tab: "+clock.Elapsed.TotalMilliseconds.ToString("F1")+" ms");
        if(screenshotGeneration!=generation)throw new Exception("New tab reloaded gallery");clock.Restart();ActivateDocument(imageTab);clock.Stop();
        if(!Object.ReferenceEquals(expected,Preview))throw new Exception("Tab switch rendered the image again");report.Add("Cached 4K tab switch: "+clock.Elapsed.TotalMilliseconds.ToString("F1")+" ms");
        for(int i=0;i<5;i++){ActivateDocument(DocumentCount-1);ActivateDocument(imageTab);if(!Object.ReferenceEquals(expected,Preview))throw new Exception("Cached preview was lost");}
        if(screenshotGeneration!=generation)throw new Exception("Tab switching reloaded gallery");
        Marks[0].X=100;RefreshImage();var changed=Preview;if(Object.ReferenceEquals(changed,expected))throw new Exception("Edit did not invalidate preview");ActivateDocument(DocumentCount-1);ActivateDocument(imageTab);if(!Object.ReferenceEquals(changed,Preview))throw new Exception("Edited preview not retained");
        report.Add("PASS Repeated tab switches reuse current rendered pixels; edits replace cache; gallery is not cleared or refreshed by tab visits");
    }
}}
