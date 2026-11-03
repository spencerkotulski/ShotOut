using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace ShotOut {
partial class Editor {
    sealed class LayoutUnit {
        public List<Mark> Marks;
        public RectangleF Bounds;
        public void Move(float x,float y){bool image=Marks.Any(mark=>mark.Kind=="Image");foreach(var mark in Marks){mark.Move(x,y);if(image&&mark.MagnifyLinked&&mark.SourceInitialized){mark.SourceX+=x;mark.SourceY+=y;}}}
    }
    List<LayoutUnit> LayoutUnits(){
        var chosen=SelectedObjects();var units=new List<LayoutUnit>();var handled=new HashSet<Mark>();
        foreach(var mark in chosen){if(handled.Contains(mark))continue;var members=String.IsNullOrEmpty(mark.LinkId)?new List<Mark>{mark}:chosen.Where(other=>other.LinkId==mark.LinkId).ToList();foreach(var member in members)handled.Add(member);var visible=members.Where(member=>member.Visible&&member.VisibleBounds.Width>0&&member.VisibleBounds.Height>0).ToList();if(visible.Count==0)continue;var bounds=visible[0].VisibleBounds;foreach(var member in visible.Skip(1))bounds=RectangleF.Union(bounds,member.VisibleBounds);units.Add(new LayoutUnit {Marks=members,Bounds=bounds});}return units;
    }
    ContextMenuStrip arrangePopup;
    void AddArrangeActions(ToolStripItemCollection items){
        int count=LayoutUnits().Count;
        string[] labels={"Align left edges","Align horizontal centers","Align right edges","Align top edges","Align vertical centers","Align bottom edges","Distribute horizontal spacing","Distribute vertical spacing"};
        for(int i=0;i<labels.Length;i++){int operation=i;if(i==6)items.Add(new ToolStripSeparator());var item=items.Add(labels[i],null,delegate{PopupLifetime.AfterClick(this,delegate{Guard(delegate{ArrangeSelection(operation);});});});item.Enabled=count>=(i>=6?3:2);}
    }
    ToolStripMenuItem CreateArrangeMenu(){var root=new ToolStripMenuItem("Align && distribute");AddArrangeActions(root.DropDownItems);root.Enabled=LayoutUnits().Count>=2;return root;}
    void ShowArrangeMenu(){if(arrangePopup==null)arrangePopup=PopupLifetime.Own(this,DarkMenu());PopupLifetime.Reset(arrangePopup);AddArrangeActions(arrangePopup.Items);arrangePopup.Show(arrangeRow,0,arrangeRow.Height);}
    void ArrangeSelection(int operation){if(SelectedObjects().Any(ImageMovementIsLocked)){UpdateStatus("Unlock the image before arranging it.");return;}
        if(operation<0||operation>7)return;FinishCloneStroke();FinishStrokeAdjustment();var units=LayoutUnits();if(units.Count<(operation>=6?3:2))return;
        var bounds=units[0].Bounds;foreach(var unit in units.Skip(1))bounds=RectangleF.Union(bounds,unit.Bounds);var moves=new Dictionary<LayoutUnit,PointF>();
        if(operation<6){foreach(var unit in units){var r=unit.Bounds;float dx=0,dy=0;switch(operation){case 0:dx=bounds.Left-r.Left;break;case 1:dx=bounds.Left+bounds.Width/2-r.Left-r.Width/2;break;case 2:dx=bounds.Right-r.Right;break;case 3:dy=bounds.Top-r.Top;break;case 4:dy=bounds.Top+bounds.Height/2-r.Top-r.Height/2;break;case 5:dy=bounds.Bottom-r.Bottom;break;}moves.Add(unit,new PointF(dx,dy));}}
        else{bool horizontal=operation==6;units=units.OrderBy(unit=>horizontal?unit.Bounds.Left:unit.Bounds.Top).ToList();float first=horizontal?units[0].Bounds.Left:units[0].Bounds.Top,last=horizontal?units.Last().Bounds.Right:units.Last().Bounds.Bottom,total=units.Sum(unit=>horizontal?unit.Bounds.Width:unit.Bounds.Height);float gap=(last-first-total)/(units.Count-1),position=first;for(int i=0;i<units.Count;i++){var unit=units[i];float delta=position-(horizontal?unit.Bounds.Left:unit.Bounds.Top);if(i==0||i==units.Count-1)delta=0;moves.Add(unit,horizontal?new PointF(delta,0):new PointF(0,delta));position+=(horizontal?unit.Bounds.Width:unit.Bounds.Height)+gap;}}
        if(moves.All(move=>Math.Abs(move.Value.X)<.001&&Math.Abs(move.Value.Y)<.001))return;Push();foreach(var move in moves)move.Key.Move(move.Value.X,move.Value.Y);EnsureCanvasFitsMarks();RefreshImage();
    }
}
}
