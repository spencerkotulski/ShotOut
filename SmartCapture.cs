using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
namespace ShotOut {
partial class CaptureOverlay {
    bool smart,snapClick,freeDrag;
    Func<Point,Rectangle> selectionPicker;
    string CaptureHint(){
        string action=window?"Click a window":smart?"Click a highlighted window or drag a region · Alt: free selection":"Drag a region";
        return (Selection.IsEmpty?"":Selection.Width+" × "+Selection.Height+" · ")+action+" · Esc: cancel";
    }
    internal void BeginSelection(Point point,bool allowSnap){
        if(window){PickWindow(point);AcceptSelection();return;}
        start=point;dragging=true;freeDrag=false;snapClick=smart&&allowSnap;Capture=true;
        if(snapClick)PickWindow(point);else Selection=Rectangle.Empty;Invalidate();
    }
    internal void MoveSelection(Point point,bool allowSnap){
        if(window){PickWindow(point);Invalidate();return;}
        if(!dragging){if(smart&&allowSnap)PickWindow(point);else Selection=Rectangle.Empty;Invalidate();return;}
        if(Math.Max(Math.Abs(point.X-start.X),Math.Abs(point.Y-start.Y))>=4)freeDrag=true;
        if(freeDrag)Selection=Rectangle.Intersect(ClientRectangle,Rectangle.FromLTRB(Math.Min(start.X,point.X),Math.Min(start.Y,point.Y),Math.Max(start.X,point.X),Math.Max(start.Y,point.Y)));
        Invalidate();
    }
    internal void EndSelection(Point point){
        if(window||!dragging)return;MoveSelection(point,snapClick);dragging=false;Capture=false;
        if(!freeDrag&&!snapClick)Selection=Rectangle.Empty;AcceptSelection();
    }
    void AcceptSelection(){if(Selection.Width>2&&Selection.Height>2){DialogResult=DialogResult.OK;Close();}}
}
partial class Editor {
    static void CopyCaptureBitmap(Bitmap image){var data=new DataObject();data.SetData(DataFormats.Bitmap,true,image);Clipboard.SetDataObject(data,true,10,80);}
    string CompleteCapture(Bitmap image,Action<Bitmap> clipboardWriter=null){
        AddDocument(image,"Capture "+(++captureSerial),true,true);
        ShowCapturePreview();
        if(!Preferences.Current.AutoCopyCapture)return "Capture added to editor.";
        try{using(var captured=Art.Render(Background,Marks))(clipboardWriter??CopyCaptureBitmap)(captured);return "Capture copied · Paste with Ctrl+V. Edit here and Copy again for markup.";}
        catch(Exception){return "Capture saved in editor. Clipboard is unavailable; use Copy image to retry.";}
    }
    public void VerifyCaptureConvenience(List<string> report,string folder){
        Action<bool,string> check=delegate(bool ok,string label){if(!ok)throw new Exception(label);report.Add("PASS "+label);};
        bool oldCopy=Preferences.Current.AutoCopyCapture,oldSmart=Preferences.Current.SmartRegion;
        try{
            Preferences.Current.AutoCopyCapture=true;Preferences.Current.SmartRegion=true;
            var preferences=Preferences.Current.Clone();preferences.AutoCopyCapture=false;preferences.SmartRegion=false;preferences.Save(System.IO.Path.Combine(folder,"capture-options.xml"));var saved=Preferences.Read(System.IO.Path.Combine(folder,"capture-options.xml"));check(!saved.AutoCopyCapture&&!saved.SmartRegion,"Capture clipboard and smart selection settings persist");
            LoadBitmap(Blank(100,100),"Previous capture");int count=DocumentCount,copies=0;var captured=Blank(90,60);captured.SetPixel(4,5,Color.Red);
            string message=CompleteCapture(captured,delegate(Bitmap bitmap){copies++;check(bitmap.Size==new Size(90,60)&&bitmap.GetPixel(4,5).R==255,"Automatic clipboard receives exact captured pixels");});check(copies==1&&DocumentCount==count+1&&message.Contains("copied"),"Completed capture opens a tab and copies once");
            Preferences.Current.AutoCopyCapture=false;CompleteCapture(Blank(70,40),delegate{copies++;});check(copies==1,"Disabling automatic copy leaves clipboard alone");
            Preferences.Current.AutoCopyCapture=true;message=CompleteCapture(Blank(60,30),delegate{throw new System.Runtime.InteropServices.ExternalException("Busy");});check(Background.Width==60&&message.Contains("retry"),"Clipboard failure preserves captured image and permits retry");
            using(var bitmap=Blank(300,200)){
                Func<Point,Rectangle> picker=delegate(Point p){return p.X<200?new Rectangle(20,15,170,120):Rectangle.Empty;};
                using(var selector=new CaptureOverlay(bitmap,new Rectangle(-300,0,300,200),false,picker)){
                    selector.MoveSelection(new Point(50,40),true);check(selector.Selection==new Rectangle(20,15,170,120),"Region capture highlights hovered window");
                    selector.BeginSelection(new Point(50,40),true);selector.EndSelection(new Point(52,41));check(selector.DialogResult==DialogResult.OK&&selector.Selection==new Rectangle(20,15,170,120),"Click with small mouse jitter captures highlighted window");
                }
                using(var selector=new CaptureOverlay(bitmap,new Rectangle(0,0,300,200),false,picker)){
                    selector.BeginSelection(new Point(50,40),true);selector.MoveSelection(new Point(100,90),true);selector.EndSelection(new Point(110,95));check(selector.Selection==new Rectangle(50,40,60,55),"Dragging overrides window snapping with exact custom region");
                }
                using(var selector=new CaptureOverlay(bitmap,new Rectangle(0,0,300,200),false,picker)){
                    selector.BeginSelection(new Point(50,40),false);selector.EndSelection(new Point(50,40));check(selector.Selection.IsEmpty&&selector.DialogResult!=DialogResult.OK,"Alt suppresses click-to-window snapping");
                    selector.BeginSelection(new Point(100,90),false);selector.EndSelection(new Point(50,40));check(selector.Selection==new Rectangle(50,40,50,50),"Free region selection supports reverse dragging");
                }
                Preferences.Current.SmartRegion=false;using(var selector=new CaptureOverlay(bitmap,new Rectangle(0,0,300,200),false,picker)){selector.MoveSelection(new Point(50,40),true);check(selector.Selection.IsEmpty,"Disabling smart region restores drag-only capture");}
            }
        }finally{Preferences.Current.AutoCopyCapture=oldCopy;Preferences.Current.SmartRegion=oldSmart;}
        while(DocumentCount>1)CloseDocumentCore(DocumentCount-1,false);LoadBitmap(Blank(100,100),"Test");
    }
}
}
