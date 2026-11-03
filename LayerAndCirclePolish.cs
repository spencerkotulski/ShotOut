using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace ShotOut {
// Keep native scrolling, hit testing and keyboard selection; buffer owner-drawn rows.
class BufferedLayerList : ListBox {
    internal Editor OwnerEditor;
    protected override void OnMouseDown(MouseEventArgs e){if(OwnerEditor!=null&&(OwnerEditor.HandleImageLockMouse(e)||OwnerEditor.HandleImageEffectMouse(e)))return;base.OnMouseDown(e);}
    protected override void OnMouseDoubleClick(MouseEventArgs e){int at=IndexFromPoint(e.Location);if(at>=0&&(Items[at] is ImageEffectRow||(e.X<24&&((Mark)Items[at]).Kind=="Image")))return;base.OnMouseDoubleClick(e);}
    protected override void OnMouseMove(MouseEventArgs e){if(OwnerEditor!=null&&OwnerEditor.HandleImageEffectMove(e))return;base.OnMouseMove(e);}
    protected override void OnMouseUp(MouseEventArgs e){if(OwnerEditor!=null&&OwnerEditor.HandleImageEffectUp(e))return;base.OnMouseUp(e);}
    bool painting;
    internal int PaintCount;
    public BufferedLayerList(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);}
    protected override void OnDrawItem(DrawItemEventArgs e){if(painting)base.OnDrawItem(e);else if(e.Index>=0)Invalidate(e.Bounds);else Invalidate();}
    protected override void OnPaint(PaintEventArgs e){
        PaintCount++;using(var background=new SolidBrush(BackColor))e.Graphics.FillRectangle(background,e.ClipRectangle);
        painting=true;try{for(int i=Math.Max(0,TopIndex);i<Items.Count;i++){var rect=GetItemRectangle(i);if(rect.Top>=ClientSize.Height)break;if(!rect.IntersectsWith(e.ClipRectangle))continue;var state=GetSelected(i)?DrawItemState.Selected:DrawItemState.None;if(Focused&&i==SelectedIndex)state|=DrawItemState.Focus;OnDrawItem(new DrawItemEventArgs(e.Graphics,Font,rect,i,state,ForeColor,BackColor));}}finally{painting=false;}
        base.OnPaint(e);
    }
}
partial class Editor {
    readonly Dictionary<Mark,string> layerRowKeys=new Dictionary<Mark,string>();
    internal int LayerRowRebuilds;
    static string LayerRowKey(Mark mark){return mark.Kind+"\0"+mark.Name+"\0"+mark.Text+"\0"+mark.Visible+"\0"+mark.LinkId+"\0"+ImageMovementIsLocked(mark);}
    void SyncLayerRows(){
        var desired=DesiredLayerRows();bool structure=layers.Items.Count!=desired.Count;
        if(!structure)for(int i=0;i<desired.Count;i++)if(!Object.ReferenceEquals(layers.Items[i],desired[i])){structure=true;break;}
        var chosen=new HashSet<Mark>(SelectedObjects());bool was=syncing;syncing=true;
        try{
            if(structure){
                ClearLayerHover();Mark top=layers.Items.Count==0?null:layers.Items[Math.Max(0,layers.TopIndex)] as Mark;int oldTop=layers.TopIndex;
                layers.BeginUpdate();try{
                    for(int i=0;i<desired.Count;i++){if(i<layers.Items.Count&&Object.ReferenceEquals(layers.Items[i],desired[i]))continue;int found=layers.Items.IndexOf(desired[i]);if(found>=0)layers.Items.RemoveAt(found);layers.Items.Insert(i,desired[i]);}
                    while(layers.Items.Count>desired.Count)layers.Items.RemoveAt(layers.Items.Count-1);
                    if(layers.Items.Count>0){int at=top==null?-1:layers.Items.IndexOf(top);layers.TopIndex=Math.Max(0,Math.Min(layers.Items.Count-1,at<0?oldTop:at));}
                    LayerRowRebuilds++;
                }finally{layers.EndUpdate();}
            }
            var keys=new Dictionary<Mark,string>();for(int i=0;i<desired.Count;i++){
                var mark=desired[i];bool selected=chosen.Contains(mark);if(layers.GetSelected(i)!=selected)layers.SetSelected(i,selected);
                string key=LayerRowKey(mark),old;keys[mark]=key;if(!structure&&(!layerRowKeys.TryGetValue(mark,out old)||old!=key))layers.Invalidate(layers.GetItemRectangle(i));
            }
            layerRowKeys.Clear();foreach(var pair in keys)layerRowKeys.Add(pair.Key,pair.Value);
        }finally{syncing=was;}
    }
    float defaultCircleSize=40;
    bool circleSizeAdjusting;
    List<Mark> circleSizeTargets;
    void SyncCircleSize(){
        if(stepSize==null)return;var circle=Active!=null&&Active.Kind=="Step"?Active:SelectedObjects().FirstOrDefault(mark=>mark.Kind=="Step");bool old=syncing;syncing=true;try{stepSize.Value=Math.Max(stepSize.Minimum,Math.Min(stepSize.Maximum,(decimal)(circle==null?defaultCircleSize:Math.Max(Math.Abs(circle.W),Math.Abs(circle.H)))));}finally{syncing=old;}
    }
    void ChangeCircleSize(){
        if(syncing||stepSize==null)return;defaultCircleSize=(float)stepSize.Value;var circles=SelectedObjects().Where(mark=>mark.Kind=="Step").ToList();
        if(strokeAdjusting&&(!circleSizeAdjusting||!circles.SequenceEqual(circleSizeTargets)))FinishStrokeAdjustment();
        if(circles.Count==0||circles.All(mark=>mark.W==defaultCircleSize&&mark.H==defaultCircleSize))return;
        if(!strokeAdjusting){Push();circleSizeTargets=circles;circleSizeAdjusting=strokeAdjusting=true;strokeTarget=Active??circles.Last();StartDragPreview();}
        foreach(var circle in circles){var box=circle.BodyBounds;circle.X=box.Left+(box.Width-defaultCircleSize)/2;circle.Y=box.Top+(box.Height-defaultCircleSize)/2;circle.W=circle.H=defaultCircleSize;}
        if(strokeTimer==null){strokeTimer=new Timer {Interval=140};strokeTimer.Tick+=delegate{FinishStrokeAdjustment();};Disposed+=delegate{strokeTimer.Dispose();};}strokeTimer.Stop();strokeTimer.Start();RefreshImage();
    }
    public void VerifyLayerAndCirclePolish(List<string> report,string folder){
        Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);report.Add("PASS "+label);};LoadImageDocument(Blank(1200,800),"Layer stability");
        var first=new Mark {Kind="Step",Text="8",X=100,Y=120,W=40,H=40};var second=new Mark {Kind="Step",Text="9",X=250,Y=140,W=60,H=60};var rectangle=new Mark {Kind="Rectangle",X=450,Y=150,W=100,H=70};Marks.Add(first);Marks.Add(second);Marks.Add(rectangle);Active=first;Selected.Clear();Selected.Add(first);Selected.Add(second);SyncStyle();RefreshImage();var centerA=new PointF(first.X+20,first.Y+20);var centerB=new PointF(second.X+30,second.Y+30);int history=undo.Count;stepSize.Value=80;stepSize.Value=90;PrepareDragFrame();check(first.W==90&&second.W==90&&rectangle.W==100,"Circle size resizes all selected circles and leaves other annotations alone");check(first.X+45==centerA.X&&second.Y+45==centerB.Y&&first.Text=="8"&&second.Text=="9","Circle resizing preserves centers and number labels");FinishStrokeAdjustment();check(undo.Count==history+1,"A circle-size adjustment burst is one Undo step");Undo();check(Marks.Where(mark=>mark.Kind=="Step").Select(mark=>mark.W).SequenceEqual(new[]{40f,60f}),"Undo restores individual circle sizes");Redo();check(Marks.Where(mark=>mark.Kind=="Step").All(mark=>mark.W==90),"Redo restores the resized circles");SetTool("Step");check(stepSize.Value==90,"Circle size remains the default for future circles");
        Show();Application.DoEvents();View.Focus();SyncLayers();int rebuilds=LayerRowRebuilds;for(int i=0;i<6;i++)SyncLayers();check(LayerRowRebuilds==rebuilds,"Unchanged layer syncs keep existing rows instead of rebuilding the list");
        var redact=new Mark {Kind="Redact",X=400,Y=320,W=120,H=90};var eraser=new Mark {Kind="Eraser",X=650,Y=320,W=120,H=90,Color=Color.White.ToArgb()};Marks.Add(redact);Marks.Add(eraser);SetTool("Select");RefreshImage();View.Zoom=1;
        foreach(var mark in new[]{redact,eraser}){Active=mark;Selected.Clear();Selected.Add(mark);SyncStyle();SyncLayers();View.Focus();Application.DoEvents();int rows=LayerRowRebuilds;for(int i=0;i<3;i++){var origin=View.Origin;int x=(int)(origin.X+mark.X+mark.W/2),y=(int)(origin.Y+mark.Y+mark.H/2);CanvasDownWithModifiers(new MouseEventArgs(MouseButtons.Left,1,x,y,0),Keys.None);CanvasMove(new MouseEventArgs(MouseButtons.Left,0,x+9,y+5,0));PrepareDragFrame();CanvasUp(new MouseEventArgs(MouseButtons.Left,0,x+9,y+5,0));}check(LayerRowRebuilds==rows&&Active==mark&&layers.SelectedItems.Contains(mark),"Repeated "+mark.Kind+" drags preserve layer rows and selection");}
        for(int i=0;i<20;i++)Marks.Add(new Mark {Kind="Rectangle",Name="Layer "+i,X=20,Y=20,W=10,H=10});SyncLayers();layers.TopIndex=10;int top=layers.TopIndex;SyncLayers();check(layers.TopIndex==top,"Layer synchronization preserves scroll position");eraser.Name="Renamed eraser";eraser.Visible=false;SyncLayers();check(layers.Items.Contains(eraser)&&!eraser.Visible&&layerRowKeys[eraser].Contains("Renamed eraser"),"Existing rows reflect renames and visibility without rebuilding");
        using(var image=new Bitmap(Width,Height)){DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(folder,"stable-layer-list.png"));}Hide();defaultCircleSize=40;LoadBitmap(Blank(100,100),"Test");
    }
}
}
