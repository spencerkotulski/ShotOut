using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
namespace ShotOut {
partial class Editor {
    PictureBox brandLogo;
    TextBox layerNameEditor;
    Mark renamingLayer;
    bool closingRename;
    void InstallLogo() {
        using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("ShotOut.Logo"))using(var image=Image.FromStream(stream))brandLogo.Image=new Bitmap(image);
        using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("ShotOut.Icon"))using(var icon=new Icon(stream))Icon=(Icon)icon.Clone();
        Disposed+=delegate {if(brandLogo.Image!=null)brandLogo.Image.Dispose();if(Icon!=null)Icon.Dispose();};
    }
    void BeginInlineRename() {
        if(Active==null||!Marks.Contains(Active))return;CloseInlineRename(true);renamingLayer=Active;
        int index=Marks.Count-1-Marks.IndexOf(Active);layers.SelectedIndex=index;var bounds=layers.GetItemRectangle(index);
        if(layerNameEditor==null) {
            layerNameEditor=new TextBox {BorderStyle=BorderStyle.FixedSingle,BackColor=Color.FromArgb(43,49,63),ForeColor=Color.White,Font=Font};layers.Controls.Add(layerNameEditor);
            layerNameEditor.KeyDown+=delegate(object sender,KeyEventArgs e) {if(e.KeyCode==Keys.Enter||e.KeyCode==Keys.Escape){e.SuppressKeyPress=e.Handled=true;CloseInlineRename(e.KeyCode==Keys.Enter);View.Focus();}};
            layerNameEditor.Leave+=delegate {CloseInlineRename(true);};layers.MouseWheel+=delegate {CloseInlineRename(true);};
        }
        layerNameEditor.Bounds=new Rectangle(36,bounds.Y+10,Math.Max(40,layers.ClientSize.Width-70),26);
        layerNameEditor.Text=String.IsNullOrEmpty(renamingLayer.Name)?(renamingLayer.Text.Length>0?renamingLayer.Text:renamingLayer.Kind):renamingLayer.Name;
        layerNameEditor.Visible=true;layerNameEditor.BringToFront();layerNameEditor.Focus();layerNameEditor.SelectAll();
    }
    void CloseInlineRename(bool commit) {
        if(renamingLayer==null||closingRename)return;closingRename=true;var mark=renamingLayer;renamingLayer=null;string value=layerNameEditor==null?"":layerNameEditor.Text.Trim();if(layerNameEditor!=null)layerNameEditor.Hide();
        if(commit&&Marks.Contains(mark)&&mark.Name!=value){Push();mark.Name=value;layers.Invalidate();UpdateStatus();}closingRename=false;
    }
    public void VerifyLogoChanges(List<string> report,string folder) {
        Action<bool,string> check=delegate(bool ok,string label){if(!ok)throw new Exception(label);report.Add("PASS "+label);};
        check(brandLogo.Image!=null&&Icon!=null,"Custom logo embedded in application and Windows icon");
        LoadBitmap(Blank(500,350),"Layer controls");SetTool("Rectangle");fillBox.Checked=false;var rectangle=NewMark("Rectangle",new PointF(20,20));rectangle.W=160;rectangle.H=90;Marks.Add(rectangle);Active=rectangle;SyncStyle();RefreshImage();
        check(!rectangle.Fill&&fillButton.Enabled,"Unfilled rectangle can choose fill without enabling it first");ApplyPickedColor(true,Color.CornflowerBlue);check(rectangle.Fill&&fillBox.Checked&&rectangle.FillColor==Color.CornflowerBlue.ToArgb(),"Choosing fill color enables fill");fillBox.Checked=false;check(!rectangle.Fill,"Fill can still be unchecked");SetTool("Ellipse");check(!NewMark("Ellipse",new PointF(1,1)).Fill,"Shapes remain unfilled when fill is disabled");
        Active=rectangle;SyncStyle();RefreshImage();Show();Application.DoEvents();RenameLayer();check(layerNameEditor.Visible&&layerNameEditor.Parent==layers,"Layer rename field appears inside its layer row");layerNameEditor.Text="Inline renamed rectangle";CloseInlineRename(true);check(rectangle.Name=="Inline renamed rectangle","Inline Enter/focus commit saves layer name");RenameLayer();layerNameEditor.Text="Discard me";CloseInlineRename(false);check(rectangle.Name=="Inline renamed rectangle","Escape cancels inline rename");
        using(var preview=new Bitmap(Width,Height)){DrawToBitmap(preview,new Rectangle(Point.Empty,preview.Size));preview.Save(System.IO.Path.Combine(folder,"logo-interface.png"));}Hide();LoadBitmap(Blank(100,100),"Test");
    }
}
}
