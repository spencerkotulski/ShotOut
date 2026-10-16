using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
namespace ShotOut {
partial class Editor {
    void PromoteImageLayer(){
        var original=Background;var layer=new Mark {Kind="Image",Name="Screenshot",IsCaptureBase=true,X=0,Y=0,W=original.Width,H=original.Height,Image=Art.Bytes(original),Width=0};
        Background=new Bitmap(original.Width,original.Height,PixelFormat.Format32bppArgb);original.Dispose();
        Marks.Insert(0,layer);Active=null;Selected.Clear();RefreshImage();
    }
    void LoadImageDocument(Bitmap image,string name){LoadBitmap(image,name);PromoteImageLayer();}
    Bitmap OriginalImageCopy(){var image=Marks.FirstOrDefault(mark=>mark.IsCaptureBase&&mark.Image!=null);return image==null?new Bitmap(Background):Art.FromBytes(image.Image);}
    public void VerifyImageLayerResize(List<string> report,string folder){
        Action<bool,string> check=delegate(bool ok,string label){if(!ok)throw new Exception(label);report.Add("PASS "+label);};
        LoadImageDocument(Blank(160,100),"Screenshot layer");check(Marks.Count==1&&Marks[0].Kind=="Image"&&Marks[0].IsCaptureBase,"Initial screenshot is a normal image layer");
        var original=Marks[0];check(Background.GetPixel(20,20).A==0,"Screenshot has no duplicate baked into canvas background");
        original.Visible=false;using(var rendered=Art.Render(Background,Marks))check(rendered.GetPixel(20,20).A==0,"Hiding initial screenshot reveals transparent canvas");original.Visible=true;
        using(var image=OriginalImageCopy())check(image.GetPixel(20,20).ToArgb()==Color.White.ToArgb(),"Place original beside canvas still retrieves original image pixels");
        Active=original;Selected.Clear();Selected.Add(original);Duplicate();check(Marks.Count==2&&Marks.All(mark=>mark.Kind=="Image"),"Initial image layer can be duplicated");Undo();check(Marks.Count==1,"Undo restores screenshot layer after duplicate");
        Active=Marks[0];Selected.Clear();Selected.Add(Active);Push();Active.Move(20,10);RefreshImage();using(var rendered=Art.Render(Background,Marks))check(rendered.GetPixel(5,5).A==0&&rendered.GetPixel(30,20).A==255,"Moving screenshot leaves transparent space instead of an original copy");Undo();
        SetTool("Rectangle");View.Zoom=1;View.PanX=View.PanY=0;
        Func<int,int,MouseEventArgs> mouse=delegate(int x,int y){var origin=View.Origin;return new MouseEventArgs(MouseButtons.Left,1,(int)origin.X+x,(int)origin.Y+y,0);};
        CanvasDown(mouse(30,25));CanvasMove(mouse(70,60));CanvasUp(mouse(70,60));check(Marks.Count==2&&Marks.Last().Kind=="Rectangle"&&Marks[0].X==0,"Drawing tools draw over initial screenshot without dragging it");
        var snapshot=Snapshot();using(var stream=new System.IO.MemoryStream()){var serializer=new System.Xml.Serialization.XmlSerializer(typeof(Project));serializer.Serialize(stream,snapshot);stream.Position=0;var saved=(Project)serializer.Deserialize(stream);Restore(saved);check(Marks[0].IsCaptureBase&&Marks[0].Image!=null,"Screenshot layer survives editable project round trip");}
        Show();Application.DoEvents();check(View.RedrawsOnResize,"Canvas requests complete redraw whenever viewport resizes");
        bool fullInvalidation=false;InvalidateEventHandler handler=delegate(object sender,InvalidateEventArgs args){if(args.InvalidRect.Contains(View.ClientRectangle))fullInvalidation=true;};View.Invalidated+=handler;
        var initialSize=Size;try{Size=new Size(1100,740);Application.DoEvents();Size=initialSize;Application.DoEvents();check(fullInvalidation,"Shrink and expand invalidates full canvas including old centered image position");}finally{View.Invalidated-=handler;}
        Func<Bitmap> frame=delegate{var bitmap=new Bitmap(View.Width,View.Height);View.DrawToBitmap(bitmap,new Rectangle(Point.Empty,bitmap.Size));return bitmap;};
        using(var before=frame()){
            for(int i=0;i<3;i++){Size=new Size(1100,740);Application.DoEvents();Size=initialSize;Application.DoEvents();}
            using(var after=frame()){bool same=before.Size==after.Size;for(int y=0;y<before.Height&&same;y+=3)for(int x=0;x<before.Width;x+=3)if(before.GetPixel(x,y)!=after.GetPixel(x,y)){same=false;break;}check(same,"Repeated shrink and expand preserves canvas image and clears stale fragments");}
        }
        using(var preview=new Bitmap(Width,Height)){DrawToBitmap(preview,new Rectangle(Point.Empty,preview.Size));preview.Save(System.IO.Path.Combine(folder,"image-layer-interface.png"));}
        Hide();while(DocumentCount>1)CloseDocumentCore(DocumentCount-1,false);LoadBitmap(Blank(100,100),"Test");
    }
}
}
