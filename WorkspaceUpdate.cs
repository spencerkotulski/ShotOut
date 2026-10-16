using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
namespace ShotOut {
class AlignmentPicker : FlowLayoutPanel {
    public readonly List<string> Items=new List<string>{"Left","Center","Right"};
    readonly List<ModernButton> buttons=new List<ModernButton>();
    readonly ToolTip tips=new ToolTip();
    string selected="Left";
    public event EventHandler SelectedIndexChanged;
    public string SelectedItem {get{return selected;}set{if(!Items.Contains(value))return;bool changed=selected!=value;selected=value;foreach(var button in buttons){button.Selected=button.AccessibleName=="Align "+value.ToLowerInvariant();button.Invalidate();}if(changed&&SelectedIndexChanged!=null)SelectedIndexChanged(this,EventArgs.Empty);}}
    public AlignmentPicker(){
        WrapContents=false;Margin=Padding.Empty;Padding=Padding.Empty;
        foreach(string item in Items){string choice=item;var button=new ModernButton {Text="",IconName="Align"+item,Width=40,Height=34,Margin=new Padding(0,0,3,0),BackColor=Color.FromArgb(43,46,54),ForeColor=Color.FromArgb(233,236,243),AccessibleName="Align "+item.ToLowerInvariant(),TabStop=true};button.Click+=delegate{SelectedItem=choice;};tips.SetToolTip(button,button.AccessibleName);buttons.Add(button);Controls.Add(button);}
        SelectedItem="Left";Resize+=delegate{foreach(var button in buttons)button.Width=Math.Max(26,(ClientSize.Width-9)/3);};Disposed+=delegate{tips.Dispose();};
    }
}
partial class Editor {
    Control alignmentRow;
    AlignmentPicker alignmentChoice;
    int cutDirection;
    int alignmentChoiceIndex;
    void AddAlignmentInspector(){
        var row=Row();row.Controls.Add(Caption("Text alignment",82));
        alignmentChoice=new AlignmentPicker {Width=130,Height=34,BackColor=Color.FromArgb(43,46,54),ForeColor=text,Margin=new Padding(0),Font=Font,AccessibleName="Text alignment"};
        alignmentChoice.SelectedItem="Left";
        alignmentChoice.SelectedIndexChanged+=delegate {if(syncing)return;alignmentChoiceIndex=alignmentChoice.Items.IndexOf(alignmentChoice.SelectedItem);ChangeStyle();};
        row.Controls.Add(alignmentChoice);alignmentRow=row;inspector.Controls.Add(row);
    }
    void ApplyAlignment(Mark mark){if(mark.Kind=="Text"||mark.Kind=="Balloon")mark.TextAlignment=alignmentChoiceIndex;}
    void SyncAlignment(Mark mark){alignmentChoiceIndex=RichLayout.Alignment(mark);alignmentChoice.SelectedItem=new[]{"Left","Center","Right"}[alignmentChoiceIndex];}
    public bool IsCutTool {get{return tool.StartsWith("Cut ");}}
    public void VerifyInteractionPolish(List<string> report,string folder){
        Action<bool,string> check=delegate(bool ok,string label){if(!ok)throw new Exception(label);report.Add("PASS "+label);};
        LoadBitmap(Blank(300,220),"Gesture polish");View.Zoom=1;start=new PointF(50,50);cutDirection=0;
        UpdateCutStrip(new PointF(53,51));check(tool=="Cut columns"&&cutDirection==0,"Cut direction remains flexible within first three pixels");
        UpdateCutStrip(new PointF(51,53));check(tool=="Cut rows"&&cutDirection==0,"Cut axis can change before threshold");
        UpdateCutStrip(new PointF(55,51));UpdateCutStrip(new PointF(56,150));check(tool=="Cut columns"&&Crop.Value.Height==220,"Cut column direction stays locked despite crossing diagonal");
        cutDirection=0;UpdateCutStrip(new PointF(51,55));UpdateCutStrip(new PointF(200,56));check(tool=="Cut rows"&&Crop.Value.Width==300,"New gesture can lock rows without flipping back");
        View.Zoom=.5f;cutDirection=0;UpdateCutStrip(new PointF(56,50));check(cutDirection==0,"Cut threshold uses screen pixels at reduced zoom");UpdateCutStrip(new PointF(58,50));check(cutDirection==1,"Cut locks after four screen pixels");Crop=null;View.Zoom=1;
        foreach(bool rich in new[]{false,true})foreach(bool tail in new[]{false,true}){
            var bubble=new Mark {Kind="Balloon",X=20,Y=20,W=240,H=140,Text="Center",Size=24,Width=0,Color=Color.Black.ToArgb(),Tail=tail};if(rich)bubble.Runs.Add(new TextRun {Text="Center",Style=1});
            using(var bg=Blank(300,200))using(var rendered=Art.Render(bg,new List<Mark>{bubble})){
                int left=300,right=0,top=200,bottom=0;for(int y=20;y<160;y++)for(int x=20;x<260;x++){if(rendered.GetPixel(x,y).R<150){left=Math.Min(left,x);right=Math.Max(right,x);top=Math.Min(top,y);bottom=Math.Max(bottom,y);}}
                check(Math.Abs((left+right)/2f-140)<7&&Math.Abs((top+bottom)/2f-(tail?83:90))<7,"Bubble text centered on both axes "+(rich?"rich":"plain")+" tail="+tail);
            }
        }
        SetTool("Balloon");var selected=new Mark {Kind="Balloon",X=40,Y=40,W=230,H=140,Text="Centered text",Width=1,Color=Color.RoyalBlue.ToArgb(),Radius=16};Marks.Add(selected);Active=selected;SyncStyle();RefreshImage();Show();Application.DoEvents();
        var picker=(AlignmentPicker)alignmentChoice;check(picker.Controls.Count==3&&((ModernButton)picker.Controls[1]).Selected,"Alignment icons show centered selection");((ModernButton)picker.Controls[2]).PerformClick();check(selected.TextAlignment==2,"Right alignment icon edits bubble with one click");((ModernButton)picker.Controls[1]).PerformClick();
        using(var preview=new Bitmap(Width,Height)){DrawToBitmap(preview,new Rectangle(Point.Empty,preview.Size));preview.Save(System.IO.Path.Combine(folder,"polished-interface.png"));}Hide();LoadBitmap(Blank(100,100),"Test");
    }
    void UpdateCutStrip(PointF point){
        float dx=point.X-start.X,dy=point.Y-start.Y;
        if(cutDirection==0&&Math.Max(Math.Abs(dx),Math.Abs(dy))*View.Zoom>=4)cutDirection=Math.Abs(dx)>=Math.Abs(dy)?1:2;
        bool columns=cutDirection==1||(cutDirection==0&&Math.Abs(dx)>=Math.Abs(dy));
        if(columns){tool="Cut columns";Crop=new RectangleF(Math.Min(start.X,point.X),0,Math.Abs(dx),Background.Height);}
        else {tool="Cut rows";Crop=new RectangleF(0,Math.Min(start.Y,point.Y),Background.Width,Math.Abs(dy));}
    }
    public void DrawCutStrip(Graphics g){
        var r=Crop.Value;using(var brush=new SolidBrush(Color.FromArgb(95,255,85,100)))g.FillRectangle(brush,r);
        using(var pen=new Pen(Color.FromArgb(255,130,140),1/View.Zoom)){pen.DashStyle=DashStyle.Dash;
            if(tool=="Cut columns"){g.DrawLine(pen,r.Left,0,r.Left,Background.Height);g.DrawLine(pen,r.Right,0,r.Right,Background.Height);}
            else {g.DrawLine(pen,0,r.Top,Background.Width,r.Top);g.DrawLine(pen,0,r.Bottom,Background.Width,r.Bottom);}}
    }
    bool ResizeCanvas(int width,int height,int shiftX,int shiftY){
        if(width==Background.Width&&height==Background.Height&&shiftX==0&&shiftY==0)return false;
        if(width>16000||height>16000||(long)width*height>40000000){UpdateStatus("Canvas limit: 16,000 pixels per side and 40 million pixels total.");return false;}
        EndDragPreview();int oldWidth=Background.Width,oldHeight=Background.Height;var expanded=new Bitmap(width,height,PixelFormat.Format32bppArgb);
        using(var g=Graphics.FromImage(expanded)){g.CompositingMode=CompositingMode.SourceCopy;g.DrawImageUnscaled(Background,shiftX,shiftY);}
        Background.Dispose();Background=expanded;canvasBase.Offset(shiftX,shiftY);
        foreach(var m in Marks){m.Move(shiftX,shiftY);if(m.SourceInitialized){m.SourceX+=shiftX;m.SourceY+=shiftY;}}
        View.PanX+=(width-oldWidth)*View.Zoom/2-shiftX*View.Zoom;View.PanY+=(height-oldHeight)*View.Zoom/2-shiftY*View.Zoom;
        return true;
    }
    bool EnsureCanvasFitsMarks(){
        if(canvasBase.Width<=0||canvasBase.Height<=0)ResetCanvasBase();
        float left=canvasBase.Left,top=canvasBase.Top,right=canvasBase.Right,bottom=canvasBase.Bottom;
        foreach(var mark in Marks.Where(m=>m.Visible)){var r=mark.Bounds;left=Math.Min(left,r.Left);top=Math.Min(top,r.Top);right=Math.Max(right,r.Right);bottom=Math.Max(bottom,r.Bottom);}
        int shiftX=-(int)Math.Floor(left),shiftY=-(int)Math.Floor(top);
        return ResizeCanvas((int)Math.Ceiling(right)+shiftX,(int)Math.Ceiling(bottom)+shiftY,shiftX,shiftY);
    }
    void ExpandCanvasDialog(){
        var size=ParseSize(Prompt.Ask(this,"Canvas width × height (adds space on right and bottom)",Background.Width+" x "+Background.Height,false));
        if(!size.HasValue)return;if(size.Value.Width<Background.Width||size.Value.Height<Background.Height)throw new ArgumentException("Use a size at least as large as the current canvas.");
        if(size.Value==Background.Size)return;Push();ResizeCanvas(size.Value.Width,size.Value.Height,0,0);ResetCanvasBase();RefreshImage();View.Fit();
    }
    void PlaceImageBeside(bool edited){
        using(var image=edited?Art.Render(Background,Marks):OriginalImageCopy()){
            int width=Background.Width+image.Width;if(width>16000||(long)width*Math.Max(Background.Height,image.Height)>40000000)throw new ArgumentException("Combined image exceeds the canvas size limit.");
            Push();var mark=new Mark {Kind="Image",Name=edited?"Edited copy":"Original image",X=Background.Width,Y=0,W=image.Width,H=image.Height,Image=Art.Bytes(image)};
            Marks.Add(mark);Selected.Clear();Selected.Add(mark);Active=mark;tool="Select";EnsureCanvasFitsMarks();RefreshImage();View.Fit();
        }
    }
    public void VerifyWorkspaceUpdate(List<string> report,string folder){
        Action<bool,string> check=delegate(bool ok,string label){if(!ok)throw new Exception(label);report.Add("PASS "+label);};
        LoadBitmap(Blank(120,80),"Expansion test");using(var g=Graphics.FromImage(Background))g.Clear(Color.Blue);
        var annotation=new Mark {Kind="Rectangle",X=5,Y=5,W=20,H=20,Fill=true,FillColor=Color.Red.ToArgb(),Width=0};Marks.Add(annotation);
        PlaceImageBeside(false);check(Background.Width==240&&Marks.Count==2,"Original image can be placed beside marked image on one canvas");
        using(var image=Art.Render(Background,Marks)){check(image.GetPixel(10,10).R==255&&image.GetPixel(130,10).B==255,"Full export contains marked original and clean second image");image.Save(System.IO.Path.Combine(folder,"side-by-side.png"));}
        Undo();check(Background.Width==120&&Marks.Count==1,"Undo restores canvas size and layers together");Redo();check(Background.Width==240&&Marks.Count==2,"Redo restores expanded canvas");
        var imageLayer=Marks.Last();imageLayer.Move(-260,-15);EnsureCanvasFitsMarks();check(imageLayer.X==0&&imageLayer.Y==0&&Background.Width==260&&Background.Height==95,"Moving beyond left and top expands and shifts all content");
        SetTool("Select");Active=imageLayer;Selected.Clear();Selected.Add(imageLayer);Duplicate();check(Marks.Count==3,"Image layers remain duplicatable on expanded canvas");
        LoadBitmap(Blank(100,80),"Added image");AddImage(Blank(300,150));check(Background.Width==320&&Background.Height==170&&Active.W==300,"Inserted images retain native size and automatically expand canvas");
        SetTool("Balloon");check(alignmentChoice.SelectedItem=="Center","Bubble alignment selector defaults to Center");check(NewMark("Balloon",PointF.Empty).TextAlignment==1,"New speech bubbles default to centered text");
        var bubble=new Mark {Kind="Balloon",Text="A",X=10,Y=10,W=180,H=90,Width=0,Color=Color.Black.ToArgb(),Size=22};Marks.Add(bubble);Active=bubble;SyncStyle();alignmentChoice.SelectedItem="Right";check(bubble.TextAlignment==2,"Alignment inspector updates selected bubble");
        var snapshot=Snapshot();check(snapshot.Marks.Last().TextAlignment==2,"Alignment persists in project snapshots");
        foreach(bool rich in new[]{false,true}){int previous=-1;foreach(int alignment in new[]{0,1,2}){var mark=new Mark {Kind="Text",Text="Test",X=10,Y=10,W=200,H=50,Size=22,Color=Color.Black.ToArgb(),TextAlignment=alignment};if(rich)mark.Runs.Add(new TextRun {Text="Test",Style=1});using(var bg=Blank(240,80))using(var drawn=Art.Render(bg,new List<Mark>{mark})){int left=240;for(int y=10;y<60;y++)for(int x=0;x<240;x++){var c=drawn.GetPixel(x,y);if(c.R<180)left=Math.Min(left,x);}check(left>previous,"Text moves right with "+(rich?"rich":"plain")+" alignment "+alignment);previous=left;}}}
        LoadBitmap(Blank(200,140),"Cut gesture");SetTool("Cut rows");View.Zoom=1;View.PanX=View.PanY=0;
        Func<int,int,MouseEventArgs> mouse=delegate(int x,int y){var origin=View.Origin;return new MouseEventArgs(MouseButtons.Left,1,(int)origin.X+x,(int)origin.Y+y,0);};
        CanvasDown(mouse(50,40));CanvasMove(mouse(80,42));check(Crop.Value.Top==0&&Crop.Value.Height==140&&Crop.Value.Width==30,"Sideways drag selects full-height cut strip automatically");CanvasUp(mouse(80,42));check(Background.Width==170&&!Crop.HasValue,"Cut columns applies on release");Undo();
        CanvasDown(mouse(50,40));CanvasMove(mouse(52,70));check(Crop.Value.Left==0&&Crop.Value.Width==200&&Crop.Value.Height==30,"Vertical drag selects full-width cut strip automatically");CanvasUp(mouse(52,70));check(Background.Height==110,"Cut rows applies on release without mode toggling");Undo();
        LoadBitmap(Blank(100,80),"Drag expansion");var movable=new Mark {Kind="Rectangle",X=60,Y=20,W=30,H=30};Marks.Add(movable);SetTool("Select");Active=movable;Selected.Clear();Selected.Add(movable);View.Zoom=1;View.PanX=View.PanY=0;CanvasDown(mouse(70,30));CanvasMove(mouse(150,30));CanvasUp(mouse(150,30));check(Background.Width==170&&movable.X==140,"Dragging beyond canvas edge expands on release without clamping movement");Undo();check(Background.Width==100&&Marks[0].X==60,"One undo restores object movement and canvas expansion");
        SetTool("Text");check(alignmentChoice.SelectedItem=="Left","New text tool defaults to left alignment");LoadBitmap(Blank(100,100),"Test");
    }
}
}
