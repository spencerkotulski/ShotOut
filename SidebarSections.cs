using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace ShotOut {
class InspectorDisclosure : ModernButton {
    public string SectionTitle;
    public bool Expanded;
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);TextRenderer.DrawText(e.Graphics,SectionTitle,Font,new Rectangle(23,0,Width-25,Height),ForeColor,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);float y=Height/2f;using(var pen=new Pen(ForeColor,1.5f)){e.Graphics.DrawLines(pen,Expanded?new[]{new PointF(8,y-2),new PointF(12,y+2),new PointF(16,y-2)}:new[]{new PointF(10,y-4),new PointF(14,y),new PointF(10,y+4)});}}
}
partial class Canvas {
    protected override bool IsInputKey(Keys keyData){var key=keyData&Keys.KeyCode;return key==Keys.Left||key==Keys.Right||key==Keys.Up||key==Keys.Down||base.IsInputKey(keyData);}
    protected override void OnKeyDown(KeyEventArgs e){if(OwnerEditor.TryNudgeSelection(e.KeyData)){e.Handled=e.SuppressKeyPress=true;}base.OnKeyDown(e);}
}
partial class Editor {
    Control textOptionsHeaderRow,stylingHeaderRow;
    InspectorDisclosure textOptionsSection,geometrySection,stylingSection;
    InspectorDisclosure SectionButton(string title,int section){var button=new InspectorDisclosure {SectionTitle=title,Width=217,Height=30,Margin=Padding.Empty,BackColor=Color.FromArgb(43,46,54),ForeColor=text,Font=Font,TabStop=true,AccessibleName="Expand or collapse "+title.ToLowerInvariant()};button.Click+=delegate{ToggleInspectorSection(section);};tips.SetToolTip(button,"Show or hide "+title.ToLowerInvariant());return button;}
    void AddInspectorSections(){var row=Row();row.Height=32;textOptionsSection=SectionButton("Text options",0);row.Controls.Add(textOptionsSection);textOptionsHeaderRow=row;inspector.Controls.Add(row);row=Row();row.Height=32;stylingSection=SectionButton("Advanced styling",2);row.Controls.Add(stylingSection);stylingHeaderRow=row;inspector.Controls.Add(row);}
    void ToggleInspectorSection(int section){var p=Preferences.Current;if(section==0)p.TextOptionsExpanded=!p.TextOptionsExpanded;else if(section==1)p.GeometryExpanded=!p.GeometryExpanded;else p.StylingExpanded=!p.StylingExpanded;UpdateInspector();SaveSessionPreferences();}
    int InspectorSectionHeight(string kind){var p=Preferences.Current;bool textOptions=kind=="Text"||kind=="Balloon";textOptionsHeaderRow.Visible=textOptions;stylingHeaderRow.Visible=false;textOptionsSection.Expanded=p.TextOptionsExpanded;textOptionsSection.Invalidate();return textOptions?37:0;}
    internal bool TryNudgeSelection(Keys keyData){
        Keys key=keyData&Keys.KeyCode;if(key!=Keys.Left&&key!=Keys.Right&&key!=Keys.Up&&key!=Keys.Down)return false;if((keyData&(Keys.Control|Keys.Alt))!=0||inlineTarget!=null)return false;
        Control focused=ActiveControl;while(focused!=null){if(focused is TextBoxBase||focused is NumberField)return false;var container=focused as ContainerControl;if(container==null)break;focused=container.ActiveControl;}
        var chosen=SelectedObjects();if(chosen.Count==0)return false;if(chosen.Any(ImageMovementIsLocked))return true;SyncGeometryFields(chosen);decimal delta=(keyData&Keys.Shift)!=0?10:1;if(key==Keys.Left||key==Keys.Up)delta=-delta;if(key==Keys.Left||key==Keys.Right)geometryX.Value+=delta;else geometryY.Value+=delta;return true;
    }
    public void VerifySidebarAndNudging(List<string> report,string folder){
        Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);report.Add("PASS "+label);};var prefs=Preferences.Current;bool previousText=prefs.TextOptionsExpanded,previousGeometry=prefs.GeometryExpanded,previousStyling=prefs.StylingExpanded;
        try{
            prefs.TextOptionsExpanded=prefs.GeometryExpanded=prefs.StylingExpanded=false;LoadImageDocument(Blank(1000,800),"Compact controls");var mark=new Mark {Kind="Balloon",X=100,Y=100,W=220,H=120,Text="Clear controls",Size=24,Radius=12};Marks.Add(mark);SetTool("Select");Active=mark;Selected.Clear();Selected.Add(mark);SyncStyle();RefreshImage();Show();Application.DoEvents();View.Focus();UpdateInspector();
            check(textRow.Visible&&formatRow.Visible&&!fontRow.Visible&&!listRow.Visible&&!alignmentRow.Visible,"Collapsed text options keep size and formatting accessible");check(geometryTitleRow.Visible&&!geometryPositionRow.Visible&&!geometrySizeRow.Visible,"Collapsed position controls keep their disclosure available");check(!stylingHeaderRow.Visible&&shadowToggleRow.Visible&&radiusRow.Visible,"Styling stays accessible without an advanced dropdown");int rebuilds=LayerRowRebuilds;textOptionsSection.PerformClick();check(fontRow.Visible&&listRow.Visible&&alignmentRow.Visible,"Text options expand with one click");geometrySection.PerformClick();check(geometryPositionRow.Visible&&geometrySizeRow.Visible,"Position and size expand with one click");check(shadowToggleRow.Visible&&radiusRow.Visible&&tailRow.Visible,"Shadow, corner radius and tail stay directly accessible");check(LayerRowRebuilds==rebuilds,"Disclosure changes do not rebuild layers");
            using(var image=new Bitmap(Width,Height)){DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(folder,"expanded-controls.png"));}string path=Path.Combine(folder,"inspector-sections.xml");prefs.Save(path);var restored=Preferences.Read(path);check(restored.TextOptionsExpanded&&restored.GeometryExpanded,"Expanded section states persist in preferences");textOptionsSection.PerformClick();geometrySection.PerformClick();View.Focus();
            int history=undo.Count;check(TryNudgeSelection(Keys.Right)&&mark.X==101,"Right arrow nudges one canvas pixel");TryNudgeSelection(Keys.Left);TryNudgeSelection(Keys.Up);check(mark.X==100&&mark.Y==99,"Left and up arrows nudge precisely");TryNudgeSelection(Keys.Down);TryNudgeSelection(Keys.Shift|Keys.Right);TryNudgeSelection(Keys.Shift|Keys.Down);check(mark.X==110&&mark.Y==110&&geometryX.Value==110&&geometryY.Value==110,"Shift arrows move ten pixels and synchronize collapsed coordinates");FinishStrokeAdjustment();check(undo.Count==history+1,"Repeated arrow nudges share one Undo step");Undo();check(Marks[1].X==100&&Marks[1].Y==100,"Undo restores the original nudge position");Redo();mark=Marks[1];Active=mark;Selected.Clear();Selected.Add(mark);SyncStyle();var second=new Mark {Kind="Rectangle",X=400,Y=200,W=50,H=50};Marks.Add(second);Selected.Add(second);View.Focus();TryNudgeSelection(Keys.Right);FinishStrokeAdjustment();check(mark.X==111&&second.X==401,"Arrow nudging preserves multiple-object spacing");check(!TryNudgeSelection(Keys.Control|Keys.Right),"Control arrows are not intercepted");Selected.Clear();Active=null;check(!TryNudgeSelection(Keys.Left),"Arrow keys without selection do not alter the image");Active=mark;Selected.Add(mark);SyncStyle();geometrySection.PerformClick();geometryX.Focus();check(!TryNudgeSelection(Keys.Up),"Focused numeric controls retain normal arrow behavior");View.Focus();geometrySection.PerformClick();UpdateInspector();using(var image=new Bitmap(Width,Height)){DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(folder,"compact-controls.png"));}
        }finally{FinishStrokeAdjustment();prefs.TextOptionsExpanded=previousText;prefs.GeometryExpanded=previousGeometry;prefs.StylingExpanded=previousStyling;Hide();LoadBitmap(Blank(100,100),"Test");}
    }
}
}
