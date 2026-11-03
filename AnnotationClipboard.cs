using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
namespace ShotOut {
partial class Editor {
    const string AnnotationClipboardFormat="ShotOut.Annotations.v1";
    string SerializeSelection(List<Mark> chosen){using(var writer=new StringWriter()){new XmlSerializer(typeof(List<Mark>)).Serialize(writer,chosen);return writer.ToString();}}
    List<Mark> ReadCopiedAnnotations(string value){if(String.IsNullOrEmpty(value))return null;using(var reader=XmlReader.Create(new StringReader(value),new XmlReaderSettings {DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=64000000}))return (List<Mark>)new XmlSerializer(typeof(List<Mark>)).Deserialize(reader);}
    Bitmap RenderCopiedAnnotations(List<Mark> chosen){
        var bounds=Rectangle.Empty;foreach(var mark in chosen.Where(m=>m.Visible))bounds=UnionBounds(bounds,Art.DrawingBounds(mark,Background.Size));
        if(bounds.IsEmpty)bounds=new Rectangle(0,0,1,1);if((long)bounds.Width*bounds.Height>40000000)throw new InvalidOperationException("Selection is too large to copy as an image.");
        var copies=chosen.Select(mark=>mark.Clone()).ToList();foreach(var mark in copies)mark.Move(-bounds.Left,-bounds.Top);
        using(var background=new Bitmap(bounds.Width,bounds.Height))return Art.Render(background,copies);
    }
    void CopySelection(){
        FinishInlineText(true);FinishStrokeAdjustment();var chosen=SelectedObjects();if(chosen.Count==0){CopyImage();return;}
        using(var bitmap=RenderCopiedAnnotations(chosen))using(var png=new MemoryStream()){
            bitmap.Save(png,System.Drawing.Imaging.ImageFormat.Png);png.Position=0;var data=new DataObject();data.SetData(AnnotationClipboardFormat,false,SerializeSelection(chosen));data.SetData(DataFormats.Bitmap,true,bitmap);data.SetData("PNG",false,png);Clipboard.SetDataObject(data,true,10,80);
        }UpdateStatus("Copied image");
    }
    void PasteCopiedAnnotations(List<Mark> copies){
        if(copies==null||copies.Count==0)return;FinishInlineText(true);Push();Selected.Clear();var links=new Dictionary<string,string>();foreach(var mark in copies){if(mark.Kind=="Image"){mark.ImageAdjustmentId=Guid.NewGuid().ToString("N");mark.IsCaptureBase=false;mark.ImageMovementLocked=false;}mark.IsCloneLayer=false;mark.LinkId=DuplicateLink(mark.LinkId,links);mark.Move(15,15);Marks.Add(mark);Selected.Add(mark);Active=mark;}SetWelcome(false);EnsureCanvasFitsMarks();RefreshImage();SyncStyle();
    }
    public void VerifyAnnotationClipboard(List<string> report){
        Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);report.Add("PASS "+label);};
        LoadImageDocument(Blank(600,400),"Annotation copy");var image=Marks[0];var text=new Mark {Kind="Text",Text="Copy only this",X=90,Y=80,W=200,H=60,Size=24,FontFamily="Georgia",Color=Color.Blue.ToArgb()};Marks.Add(text);Active=text;Selected.Clear();Selected.Add(text);RefreshImage();var selected=SelectedObjects();var xml=SerializeSelection(selected);var copies=ReadCopiedAnnotations(xml);check(copies.Count==1&&copies[0].Kind=="Text"&&copies[0].Image==null,"Annotation clipboard excludes the unlinked screenshot");
        using(var bitmap=RenderCopiedAnnotations(selected)){check(bitmap.Width<Background.Width&&bitmap.Height<Background.Height,"Copied annotation bitmap uses selection bounds");check(bitmap.GetPixel(0,0).A==0,"Copied annotation background stays transparent");}
        int count=Marks.Count,history=undo.Count;PasteCopiedAnnotations(copies);check(Marks.Count==count+1&&Active.Kind=="Text"&&Active.Text==text.Text&&Active.FontFamily=="Georgia"&&Active.X==text.X+15&&Object.ReferenceEquals(image,Marks[0]),"Pasting preserves editable text without duplicating the screenshot");check(undo.Count==history+1,"Pasting annotations is one undoable action");Undo();check(Marks.Count==count,"Undo removes only pasted annotations");
        Active=Marks.Last();Selected.Clear();Selected.Add(Active);Marks[0].LinkId=Active.LinkId="copy-group";copies=ReadCopiedAnnotations(SerializeSelection(SelectedObjects()));check(copies.Count==2,"Copy includes linked layers when explicitly grouped");PasteCopiedAnnotations(copies);check(SelectedObjects().Count==2&&Active.LinkId!="copy-group","Pasted groups have independent link identities");LoadBitmap(Blank(100,100),"Test");
    }
}
}
