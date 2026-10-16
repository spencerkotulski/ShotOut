using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
namespace ShotOut {
partial class Editor {
    Bitmap dragBase,snapshotBitmap;
    byte[] snapshotBytes;
    List<Mark> dragScene,dragMutable;
    int dragFirst;
    Rectangle dragPreviousBounds;
    bool dragFramePending;
    byte[] SnapshotImageBytes(){if(snapshotBytes==null||!Object.ReferenceEquals(snapshotBitmap,Background)){snapshotBytes=Art.Bytes(Background);snapshotBitmap=Background;}return snapshotBytes;}
    static Rectangle UnionBounds(Rectangle a,Rectangle b){if(a.IsEmpty)return b;if(b.IsEmpty)return a;return Rectangle.Union(a,b);}
    Rectangle MutableBounds(){var r=Rectangle.Empty;foreach(var m in dragMutable)if(m.Visible)r=UnionBounds(r,Art.DrawingBounds(m,Background.Size));return r;}
    void StartDragPreview(){
        EndDragPreview();if(!(moving||resizing||dragging)||Preview==null)return;
        dragMutable=draft!=null?new List<Mark>{draft}:SelectedObjects();if(dragMutable.Count==0)return;
        dragFirst=Marks.FindIndex(m=>dragMutable.Contains(m));if(dragFirst<0)return;
        dragScene=Marks.ToList();dragBase=Art.Render(Background,Marks.Take(dragFirst).ToList(),false);dragPreviousBounds=MutableBounds();
        if(previewClock!=null)previewClock.Stop();
    }
    bool TryQueueDragFrame(){
        if(dragBase==null||dragScene.Count!=Marks.Count||!dragScene.SequenceEqual(Marks))return false;
        dragFramePending=true;var dirty=UnionBounds(dragPreviousBounds,MutableBounds());InvalidateDragArea(ExpandDragDirty(dirty));return true;
    }
    void InvalidateDragArea(Rectangle dirty){
        if(dirty.IsEmpty)return;var origin=View.Origin;int pad=45;var rect=Rectangle.FromLTRB((int)Math.Floor(origin.X+dirty.Left*View.Zoom)-pad,(int)Math.Floor(origin.Y+dirty.Top*View.Zoom)-pad,(int)Math.Ceiling(origin.X+dirty.Right*View.Zoom)+pad,(int)Math.Ceiling(origin.Y+dirty.Bottom*View.Zoom)+pad);View.Invalidate(rect);
    }
    public void PrepareDragFrame(){
        if(!dragFramePending||dragBase==null)return;dragFramePending=false;
        if(dragScene.Count!=Marks.Count||!dragScene.SequenceEqual(Marks)){EndDragPreview();RenderPreview(false);return;}
        var current=MutableBounds();var dirty=UnionBounds(dragPreviousBounds,current);if(dirty.IsEmpty)return;
        dirty=ExpandDragDirty(dirty);
        using(var g=Graphics.FromImage(Preview)){g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;g.DrawImage(dragBase,dirty,dirty,GraphicsUnit.Pixel);}
        for(int i=dragFirst;i<Marks.Count;i++)if(Marks[i].Visible)Art.Draw(Preview,Marks[i],false,dirty);
        dragPreviousBounds=current;
    }
    Rectangle ExpandDragDirty(Rectangle dirty) {
        // Effects read neighboring/lower layers: include their complete regions before recompositing.
        for(int pass=0;pass<Marks.Count-dragFirst;pass++){var previous=dirty;for(int i=dragFirst;i<Marks.Count;i++){var m=Marks[i];if(!m.Visible||!(m.Kind=="Blur"||m.Kind=="Pixelate"||m.Kind=="Magnify"||m.Kind=="Spotlight"))continue;var bounds=Art.DrawingBounds(m,Background.Size);if(bounds.IntersectsWith(dirty))dirty=UnionBounds(dirty,bounds);}if(dirty==previous)break;}
        return dirty;
    }
    void EndDragPreview(){dragFramePending=false;if(dragBase!=null){dragBase.Dispose();dragBase=null;}dragScene=null;dragMutable=null;}
    public void VerifyDragPerformance(List<string> report,string folder){
        Action<bool,string> check=delegate(bool ok,string label){if(!ok)throw new Exception(label);report.Add("PASS "+label);};
        LoadBitmap(Blank(3840,2160),"4K drag benchmark");
        using(var g=Graphics.FromImage(Background)){g.Clear(Color.FromArgb(238,242,248));using(var font=new Font("Segoe UI",34,FontStyle.Bold,GraphicsUnit.Pixel))for(int y=80;y<2100;y+=110)g.DrawString("Screenshot markup performance — keep moving",font,Brushes.Navy,70,y);}
        for(int i=0;i<24;i++)Marks.Add(new Mark{Kind="Rectangle",X=50+i*120,Y=120+i*45,W=260,H=150,Width=3});
        Marks.Add(new Mark{Kind="Blur",X=1800,Y=300,W=1400,H=700,Width=8});
        var arrow=new Mark{Kind="Arrow",X=300,Y=1100,W=250,H=90,Width=4};Marks.Add(arrow);Active=arrow;Selected.Clear();Selected.Add(arrow);RenderPreview(false);
        var clock=Stopwatch.StartNew();for(int i=0;i<8;i++){arrow.Move(3,2);using(var image=Art.Render(Background,Marks,false)){} }clock.Stop();double baseline=clock.Elapsed.TotalMilliseconds/8;arrow.Move(-24,-16);RenderPreview(false);
        moving=true;clock.Restart();StartDragPreview();clock.Stop();double setup=clock.Elapsed.TotalMilliseconds;var buffer=Preview;
        clock.Restart();for(int i=0;i<8;i++){arrow.Move(3,2);TryQueueDragFrame();PrepareDragFrame();}clock.Stop();double optimized=clock.Elapsed.TotalMilliseconds/8;
        check(Object.ReferenceEquals(buffer,Preview),"Dragging reuses its existing image buffer instead of allocating 4K images per frame");
        using(var expected=Art.Render(Background,Marks,false)){bool same=true;var area=Art.DrawingBounds(arrow,Background.Size);for(int y=area.Top;y<area.Bottom&&same;y++)for(int x=area.Left;x<area.Right;x++)if(expected.GetPixel(x,y)!=Preview.GetPixel(x,y)){same=false;break;}check(same,"Incremental arrow redraw matches full renderer");}
        moving=false;EndDragPreview();
        // Include a translucent upper layer and a source-reading effect above the moved object.
        Marks.Add(new Mark{Kind="Highlight",X=200,Y=1000,W=700,H=400,Color=Color.Gold.ToArgb()});Marks.Add(new Mark{Kind="Pixelate",X=400,Y=1100,W=160,H=130,Width=3});RenderPreview(false);moving=true;StartDragPreview();arrow.Move(12,5);TryQueueDragFrame();PrepareDragFrame();
        using(var expected=Art.Render(Background,Marks,false)){bool same=true;for(int y=990;y<1410&&same;y++)for(int x=190;x<910;x++)if(expected.GetPixel(x,y)!=Preview.GetPixel(x,y)){same=false;break;}check(same,"Dragging preserves upper-layer transparency and pixelation ordering");}
        moving=false;EndDragPreview();check(Object.ReferenceEquals(SnapshotImageBytes(),SnapshotImageBytes()),"Unchanged screenshot bytes are shared by undo snapshots");
        report.Add("4K benchmark (8 frames): full redraw "+baseline.ToString("F2")+" ms/frame; incremental redraw "+optimized.ToString("F2")+" ms/frame; one-time drag cache "+setup.ToString("F2")+" ms.");
        System.IO.File.WriteAllText(System.IO.Path.Combine(folder,"drag-benchmark.txt"),"Full redraw: "+baseline.ToString("F2")+" ms/frame\r\nIncremental redraw: "+optimized.ToString("F2")+" ms/frame\r\nOne-time cache setup: "+setup.ToString("F2")+" ms\r\nSynthetic 4K screenshot; 24 rectangles, a large static blur, and an arrow. This measures compositing, not display latency.");
        LoadBitmap(Blank(100,100),"Test");
    }
}
}
